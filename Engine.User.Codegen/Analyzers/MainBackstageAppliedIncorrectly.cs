using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Engine.User.Codegen.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
[SuppressMessage("MicrosoftCodeAnalysisDesign", "RS1017:DiagnosticId for analyzers must be a non-null constant")]
public class MainBackstageAppliedIncorrectly : DiagnosticAnalyzer
{
    private static readonly string DiagnosticId = AnalyzerCode.MainBackstageAppliedIncorrectly.GetCode();

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Invalid usage of [MainBackstage]s",
        "[MainBackstage] can only be applied on a Backstage-derived class",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;

        // walk every attribute on the class
        foreach (var attrList in classDecl.AttributeLists)
        foreach (var attr in attrList.Attributes)
        {
            var name = attr.Name.ToString();
            if (name != "MainBackstage" && name != "MainBackstageAttribute")
                continue;

            // get the symbol for the class!
            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl, context.CancellationToken);
            if (classSymbol == null)
                continue;

            // try to look up the Backstage base type
            var backstageType = context.Compilation
                                       .GetTypeByMetadataName("Engine.Worlds.Entities.Backstage");
            if (backstageType == null)
                continue;

            // if it does *not* inherit from Backstage, report
            if (!InheritsFrom(classSymbol, backstageType))
            {
                var diag = Diagnostic.Create(Rule, attr.GetLocation());
                context.ReportDiagnostic(diag);
            }
        }
    }

    private static bool InheritsFrom(INamedTypeSymbol? type, INamedTypeSymbol baseType)
    {
        while (type != null)
        {
            if (SymbolEqualityComparer.Default.Equals(type, baseType))
                return true;
            type = type.BaseType;
        }
        return false;
    }
}
