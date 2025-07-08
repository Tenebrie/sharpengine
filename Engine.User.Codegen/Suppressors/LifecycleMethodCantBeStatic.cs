using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Engine.User.Codegen.Suppressors;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LifecycleMethodCantBeStatic : DiagnosticSuppressor
{
    private static readonly string DiagnosticId = SuppressorCode.LifecycleMethodCantBeStatic.GetCode();
    
    private static readonly SuppressionDescriptor Rule =
        new(
            id: DiagnosticId,
            suppressedDiagnosticId: "CA1822",
            justification: "Lifecycle method - invoked via engine reflection."
        );

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => [Rule];

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diag in context.ReportedDiagnostics)
        {
            if (diag.Id != "CA1822")
                continue;

            var tree = diag.Location.SourceTree;
            if (tree == null) continue;

            var model = context.GetSemanticModel(tree);
            var node = tree.GetRoot(context.CancellationToken)
                .FindNode(diag.Location.SourceSpan);
            if (model.GetDeclaredSymbol(node, context.CancellationToken) is not IMethodSymbol method)
                continue;

            if (!method.GetAttributes()
                    .Any(a => LifecycleAttribute.Includes(a.AttributeClass?.Name))) continue;
            {
                context.ReportSuppression(
                    Suppression.Create(Rule, diag));
            }
        }
    }
}
