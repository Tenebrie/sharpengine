using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Engine.Tooling.Roslyn.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
[SuppressMessage("MicrosoftCodeAnalysisDesign", "RS1017:DiagnosticId for analyzers must be a non-null constant")]
public sealed class AlwaysStaticLifecycleMethodAnalyzer : DiagnosticAnalyzer
{
    private static readonly string DiagnosticId = AnalyzerCode.AlwaysStaticLifecycleMethod.GetCode();

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Lifecycle method must be a static method",
        messageFormat:
            "Lifecycle attribute '{0}' can't be applied to instance methods (only static)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    private static readonly ImmutableHashSet<string> AlwaysStaticAttributeNames =
        ImmutableHashSet.Create(StringComparer.Ordinal,
            "OnPrepareResourcesAttribute",
            "OnLoadResourcesAttribute"
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
            if (ctx.SemanticModel.GetSymbolInfo(attr, ctx.CancellationToken).Symbol is not IMethodSymbol ctorSym)
                continue;

            var attrName = ctorSym.ContainingType.Name;
            if (!AlwaysStaticAttributeNames.Contains(attrName) || methodSymbol.IsStatic) continue;

            var diagnostic = Diagnostic.Create(
                Rule,
                attr.GetLocation(),
                attrName.Replace("Attribute", string.Empty)
            );
            ctx.ReportDiagnostic(diagnostic);
        }
    }
}
