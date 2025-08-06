using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Engine.Tooling.Roslyn.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
[SuppressMessage("MicrosoftCodeAnalysisDesign", "RS1017:DiagnosticId for analyzers must be a non-null constant")]
public sealed class LifecycleMethodSignatureAnalyzer : DiagnosticAnalyzer
{
    private static readonly string DiagnosticId = AnalyzerCode.LifecycleMethodSignature.GetCode();

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Method parameters must match the declared input binding",
        messageFormat:
            "Parameters of method '{0}' do not match the binding implied by '{1}'. " +
            "Expected {2}, but found {3}.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    private static readonly ImmutableHashSet<string> TrackedLifecycleAttributeNames =
        ImmutableHashSet.Create(StringComparer.Ordinal,
            "OnCreateAttribute",
            "OnReadyAttribute",
            "OnUpdateAttribute",
            "OnDestroyAttribute",
            "OnModuleReloadAttribute",
            "OnGameplayContextChangeAttribute"
        );
    
    private static readonly ImmutableHashSet<string> TrackedInputAttributeNames =
        ImmutableHashSet.Create(StringComparer.Ordinal,
            "OnInputAttribute",
            "OnInputHeldAttribute",
            "OnInputReleasedAttribute",
            "OnKeyInputAttribute",
            "OnKeyInputHeldAttribute",
            "OnKeyInputReleasedAttribute"
        );
    
    private static readonly ImmutableHashSet<string> AllowedStaticAttributes =
        ImmutableHashSet.Create(StringComparer.Ordinal,
            "OnCreateAttribute",
            "OnReadyAttribute",
            "OnUpdateAttribute"
        );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext ctx)
    {
        var methodDecl  = (MethodDeclarationSyntax)ctx.Node;
        var methodSymbol = ctx.SemanticModel.GetDeclaredSymbol(methodDecl, ctx.CancellationToken);
        if (methodSymbol is null) return;

        foreach (var attr in methodDecl.AttributeLists.SelectMany(l => l.Attributes))
        {
            // Resolve the attribute constructor symbol to get the real name (`OnInputAttribute`, etc.)
            if (ctx.SemanticModel.GetSymbolInfo(attr, ctx.CancellationToken).Symbol is not IMethodSymbol ctorSym)
                continue;

            var attrName = ctorSym.ContainingType.Name;
            var isDeltaAllowed = attrName.Contains("Held") || attrName.Contains("OnUpdate");
            Binding expectedBinding;
            if (AllowedStaticAttributes.Contains(attrName) && methodSymbol.IsStatic)
            {
                expectedBinding = Binding.Type;
            }
            else if (TrackedInputAttributeNames.Contains(attrName))
            {
                var positionalCount = attr.ArgumentList?.Arguments.Count ?? 0;
                if (attr.ArgumentList == null || positionalCount == 0)
                    continue;
                
                var extraDoubleCount = positionalCount - 1;
                expectedBinding  = Binding.Infer(
                    attr.ArgumentList.Arguments[positionalCount - 1].Expression,
                    ctx.SemanticModel,
                    // methodSymbol.Parameters[methodSymbol.Parameters.Length - 1].Type,
                    extraDoubleCount
                );
                if (expectedBinding.Equals(Binding.Unknown))
                    continue;
            }
            else if (TrackedLifecycleAttributeNames.Contains(attrName))
            {
                expectedBinding = Binding.None;
            }
            else
                continue;

            var isMatch = ParametersMatch(methodSymbol.Parameters, expectedBinding, isDeltaAllowed, out var foundSignature);
            if (isMatch)
                continue;
            
            var expectedSig = expectedBinding.GetDisplay(isDeltaAllowed);
            ctx.ReportDiagnostic(Diagnostic.Create(
                Rule,
                attr.GetLocation(),
                methodSymbol.Name,
                attrName.Replace("Attribute", string.Empty),
                expectedSig,
                foundSignature));
        }
    }

    // ===== helpers ===============================================================================================

    private static bool ParametersMatch(
        ImmutableArray<IParameterSymbol> parameters,
        Binding binding,
        bool isHeld,
        out string foundSignature)
    {
        var index = 0;
        if (isHeld && parameters.Length > 0 && parameters[0].Type.SpecialType == SpecialType.System_Double)
        {
            index = 1; // skip the deltaTime parameter
        }

        var effectiveParamCount = parameters.Length - index;
        foundSignature = string.Join(", ",
            parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        if (foundSignature.Length == 0)
            foundSignature = "no parameters";

        return binding.Kind switch
        {
            BindingKind.None => effectiveParamCount == 0,
            BindingKind.Int => effectiveParamCount == 1 && IsInt(parameters[index]),
            BindingKind.Double => effectiveParamCount == 1 && IsDouble(parameters[index]),
            BindingKind.Vector2 => effectiveParamCount == 1 && IsVector(parameters[index], "Vector2"),
            BindingKind.Vector3 => effectiveParamCount == 1 && IsVector(parameters[index], "Vector3"),
            BindingKind.Type => effectiveParamCount == 0 || (effectiveParamCount == 1 && IsType(parameters[index])),
            _ => true
        };
    }

    private static bool IsInt(IParameterSymbol p) =>
        p.Type.SpecialType == SpecialType.System_Int32;
    
    private static bool IsDouble(IParameterSymbol p) =>
        p.Type.SpecialType == SpecialType.System_Double;

    private static bool IsVector(IParameterSymbol p, string simpleName) =>
        string.Equals(p.Type.Name, simpleName, StringComparison.Ordinal);
    
    private static bool IsType(IParameterSymbol p) =>
        string.Equals(p.Type.Name , "Type", StringComparison.Ordinal);

    // ===== binding model =========================================================================================

    private readonly struct Binding : IEquatable<Binding>
    {
        public BindingKind Kind { get; }

        private Binding(BindingKind kind) => Kind = kind;

        public static Binding None    => new(BindingKind.None);
        public static Binding Int     => new(BindingKind.Int);
        public static Binding Double  => new(BindingKind.Double);
        public static Binding Vector2 => new(BindingKind.Vector2);
        public static Binding Vector3 => new(BindingKind.Vector3);
        public static Binding Type    => new(BindingKind.Type);
        public static Binding Unknown => new(BindingKind.Unknown);

        public bool Equals(Binding other)
        {
            return Kind == other.Kind;
        }

        public override bool Equals(object? obj)
        {
            return obj is Binding other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)Kind;
        }

        public static Binding Infer(ExpressionSyntax expression, SemanticModel semanticModel, int argumentCount)
        {
            if (expression is LiteralExpressionSyntax literal && 
                semanticModel.GetConstantValue(literal).HasValue &&
                semanticModel.GetConstantValue(literal).Value is int)
            {
                return Int;
            }

            return argumentCount switch
            {
                0 => None,
                1 => Double,
                2 => Vector2,
                3 => Vector3,
                _ => Unknown
            };
        }

        public string GetDisplay(bool isDeltaAllowed) =>
            Kind switch
            {
                BindingKind.None    => isDeltaAllowed ? "[Double deltaTime]?" : "no parameters",
                BindingKind.Int     => $"{(isDeltaAllowed ? "[Double deltaTime]? " : string.Empty)}(int value)",
                BindingKind.Double  => $"{(isDeltaAllowed ? "[Double deltaTime]? " : string.Empty)}(double value)",
                BindingKind.Vector2 => $"{(isDeltaAllowed ? "[Double deltaTime]? " : string.Empty)}(Vector2 value)",
                BindingKind.Vector3 => $"{(isDeltaAllowed ? "[Double deltaTime]? " : string.Empty)}(Vector3 value)",
                BindingKind.Type =>    $"{(isDeltaAllowed ? "[Double deltaTime]? " : string.Empty)}[Type selfType]?",
                _                   => "unknown"
            };
    }

    private enum BindingKind { None, Int, Double, Vector2, Vector3, Type, Unknown }
}
