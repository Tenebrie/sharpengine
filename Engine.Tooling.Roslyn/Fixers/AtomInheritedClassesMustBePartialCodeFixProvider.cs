using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Engine.Tooling.Roslyn.Fixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AtomInheritedClassesMustBePartialCodeFixProvider))]
[Shared]
public class AtomInheritedClassesMustBePartialCodeFixProvider : CodeFixProvider
{
    private const string Title = "Make partial";

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [AnalyzerCode.AtomInheritedClassesMustBePartial.GetCode()];

    public sealed override FixAllProvider GetFixAllProvider() => 
        WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the class declaration syntax node with the diagnostic
        var classDeclaration = root?.FindNode(diagnosticSpan)
            .AncestorsAndSelf()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        if (classDeclaration == null)
            return;

        // Register a code action that will invoke the fix
        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => MakeClassPartialAsync(context.Document, classDeclaration, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<Document> MakeClassPartialAsync(Document document, ClassDeclarationSyntax classDecl, CancellationToken cancellationToken)
    {
        // Add the partial modifier if it doesn't already have it
        var newModifiers = classDecl.Modifiers;
        if (!newModifiers.Any(SyntaxKind.PartialKeyword))
        {
            newModifiers = newModifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
        }

        // Create the new class declaration with the partial modifier
        var newClassDecl = classDecl.WithModifiers(newModifiers);

        // Replace the old class declaration with the new one
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;
            
        var newRoot = root.ReplaceNode(classDecl, newClassDecl);
        return document.WithSyntaxRoot(newRoot);
    }
}