using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Engine.Tooling.Roslyn.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
[SuppressMessage("MicrosoftCodeAnalysisDesign", "RS1017:DiagnosticId for analyzers must be a non-null constant")]
public class AtomInheritedClassesMustBePartial : DiagnosticAnalyzer
{
    private static readonly string DiagnosticId = AnalyzerCode.AtomInheritedClassesMustBePartial.GetCode();

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Atoms must be partial",
        "An Atom-derived class must be declared as partial to enable code generation",
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
        
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl, context.CancellationToken);
        if (classSymbol == null)
            return;
        var atomType = context.Compilation.GetTypeByMetadataName("Engine.Core.EntitySystem.Entities.Atom");
        if (atomType == null)
            return;
        
        if (!InheritsFrom(classSymbol, atomType))
            return;

        if (classDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
            return;
        
        var diagnostic = Diagnostic.Create(Rule, classDecl.Identifier.GetLocation());
        context.ReportDiagnostic(diagnostic);
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
