using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Engine.User.Codegen.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
[SuppressMessage("MicrosoftCodeAnalysisDesign", "RS1017:DiagnosticId for analyzers must be a non-null constant")]
public sealed class LifecycleAttributeOnPrivateMethod : DiagnosticAnalyzer
{
    private static readonly string DiagnosticId = AnalyzerCode.LifecycleAttributeOnPrivateMethod.GetCode();

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Lifecycle attribute applied to private method",
        "Lifecycle attributes cannot be applied to a private method; the method must be public, internal, or protected",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var methodDecl = (MethodDeclarationSyntax)context.Node;

        if (!methodDecl.AttributeLists
                       .SelectMany(a => a.Attributes)
                       .Any(attr =>
                       {
                           var name = attr.Name.ToString();
                           return LifecycleAttribute.Includes(name);
                       }))
            return;

        // Resolve the symbol and check accessibility
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDecl, context.CancellationToken);
        if (methodSymbol is null)
            return;

        if (IsAllowedAccessibility(methodSymbol.DeclaredAccessibility)) return;
        
        // Place the diagnostic on the attribute for clarity
        var attrLocation = methodDecl.AttributeLists
            .SelectMany(a => a.Attributes)
            .First(attr => LifecycleAttribute.Includes(attr.Name.ToString()))
            .GetLocation();

        var diagnostic = Diagnostic.Create(Rule, attrLocation);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsAllowedAccessibility(Accessibility accessibility) =>
        accessibility is Accessibility.Public
                      or Accessibility.Internal
                      or Accessibility.Protected
                      or Accessibility.ProtectedOrInternal;
}