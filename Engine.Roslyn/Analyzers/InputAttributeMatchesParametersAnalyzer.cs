using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Engine.Roslyn.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InputAttributeMatchesParametersAnalyzer : DiagnosticAnalyzer
{
    private static readonly string DiagnosticId = AnalyzerCode.InputAttributeMatchesParametersAnalyzer.GetCode();

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Method parameters must match the declared input binding",
        messageFormat:
            "Parameters of method '{0}' do not match the binding implied by '{1}'. " +
            "Expected {2}, but found {3}.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private static readonly ImmutableHashSet<string> TrackedLifecycleAttributeNames =
        ImmutableHashSet.Create(StringComparer.Ordinal,
            "OnInitAttribute",
            "OnUpdateAttribute",
            "OnDestroyAttribute");
    
    private static readonly ImmutableHashSet<string> TrackedInputAttributeNames =
        ImmutableHashSet.Create(StringComparer.Ordinal,
            "OnInputAttribute",
            "OnInputHeldAttribute",
            "OnInputReleasedAttribute",
            "OnKeyInputAttribute",
            "OnKeyInputHeldAttribute",
            "OnKeyInputReleasedAttribute");

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    // ===== analysis ===============================================================================================

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
            var isHeld = attrName.Contains("Held") || attrName.Contains("OnUpdate");
            string foundSignature;
            Binding expectedBinding;
            if (TrackedInputAttributeNames.Contains(attrName))
            {
                // ----- derive the binding from the *number* of extra doubles in the attribute -------------------------
                // We ignore the first argument (the InputAction enum value).  Any named arguments are irrelevant.
                var positionalCount = attr.ArgumentList?.Arguments.Count ?? 0;
                if (positionalCount == 0)
                    continue; // malformed attribute – let the compiler complain separately.

                var extraDoubleCount = positionalCount - 1; // 0,1,2,3
                expectedBinding  = Binding.FromExtraCount(extraDoubleCount);
                if (expectedBinding.Equals(Binding.Unknown))
                    continue; // Someone added a 4-double ctor – ignore until the spec updates.
                
                // ----- check the method parameters ---------------------------------------------------------------------
                
                if (ParametersMatch(methodSymbol.Parameters, expectedBinding, isHeld, ctx.Compilation,
                        out foundSignature)) continue;
            }
            else if (TrackedLifecycleAttributeNames.Contains(attrName))
            {
                expectedBinding = Binding.None;
                if (ParametersMatch(methodSymbol.Parameters, expectedBinding, isHeld, ctx.Compilation,
                        out foundSignature)) continue;
            }
            else
                continue;
            
            var expectedSig = expectedBinding.GetDisplay(isHeld);
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
        Compilation compilation,
        out string foundSignature)
    {
        var index = 0;
        if (isHeld && parameters.Length > 0 && parameters[0].Type.SpecialType == SpecialType.System_Double)
        {
            index = 1; // skip the deltaTime parameter
        }

        var remaining = parameters.Length - index;
        foundSignature = string.Join(", ",
            parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));

        switch (binding.Kind)
        {
            case BindingKind.None:
                return remaining == 0;

            case BindingKind.Double:
                return remaining == 1 && IsDouble(parameters[index]);

            case BindingKind.Vector2:
                return remaining == 1 && IsVector(parameters[index], "Vector2");

            case BindingKind.Vector3:
                return remaining == 1 && IsVector(parameters[index], "Vector3");

            default:
                return true;
        }
    }

    private static bool IsDouble(IParameterSymbol p) =>
        p.Type.SpecialType == SpecialType.System_Double;

    private static bool IsVector(IParameterSymbol p, string simpleName) =>
        string.Equals(p.Type.Name, simpleName, StringComparison.Ordinal);

    // ===== binding model =========================================================================================

    private readonly struct Binding : IEquatable<Binding>
    {
        public BindingKind Kind { get; }

        private Binding(BindingKind kind) => Kind = kind;

        public static Binding None    => new(BindingKind.None);
        public static Binding Double  => new(BindingKind.Double);
        public static Binding Vector2 => new(BindingKind.Vector2);
        public static Binding Vector3 => new(BindingKind.Vector3);
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

        public static Binding FromExtraCount(int extra) => extra switch
        {
            0 => None,
            1 => Double,
            2 => Vector2,
            3 => Vector3,
            _ => Unknown
        };

        public string GetDisplay(bool isHeld) =>
            Kind switch
            {
                BindingKind.None    => isHeld ? "[deltaTime?]" : "no parameters",
                BindingKind.Double  => $"{(isHeld ? "[deltaTime?] " : string.Empty)}double",
                BindingKind.Vector2 => $"{(isHeld ? "[deltaTime?] " : string.Empty)}Vector2",
                BindingKind.Vector3 => $"{(isHeld ? "[deltaTime?] " : string.Empty)}Vector3",
                _                   => "unknown"
            };
    }

    private enum BindingKind { None, Double, Vector2, Vector3, Unknown }
}
