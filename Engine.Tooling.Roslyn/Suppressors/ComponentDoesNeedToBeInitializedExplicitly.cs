﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Engine.Tooling.Roslyn.Suppressors;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ComponentDoesNotNeedToBeInitializedExplicitly : DiagnosticSuppressor
{
    private static readonly SuppressionDescriptor Rule =
        new(
            id: SuppressorCode.ComponentDoesNotNeedToBeInitializedExplicitly.GetCode(),
            suppressedDiagnosticId: "CS8618",
            justification: "Component fields are populated by the engine."
        );

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => [Rule];

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diag in context.ReportedDiagnostics)
        {
            if (diag.Id != "CS8618")
                continue;

            var tree = diag.Location.SourceTree;
            if (tree == null) continue;

            var model = context.GetSemanticModel(tree);
            var node = tree.GetRoot(context.CancellationToken)
                .FindNode(diag.Location.SourceSpan);
            if (model.GetDeclaredSymbol(node, context.CancellationToken) is not IFieldSymbol field)
                continue;

            if (!field.GetAttributes()
                    .Any(a => a.AttributeClass?.Name
                        is "Component" or "ComponentAttribute"
                        or "Signal" or "SignalAttribute")) continue;
            {
                context.ReportSuppression(
                    Suppression.Create(Rule, diag));
            }
        }
    }
}
