using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Engine.Tooling.Roslyn.Fixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DefaultGroupMustMatchContainingTypeCodeFixProvider))]
[Shared]
public sealed class DefaultGroupMustMatchContainingTypeCodeFixProvider : CodeFixProvider
{
    private const string Title = "Change to Group<{0}>";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        [AnalyzerCode.DefaultGroupMustMatchContainingType.GetCode()];

    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics.First();
        var span = diagnostic.Location.SourceSpan;

        // We reported on the Group<...> type, so find that node first.
        var targetNode = root.FindNode(span, getInnermostNodeForTie: true);

        // Find the GenericNameSyntax "Group<...>" even if qualified.
        var groupGeneric = FindGroupGenericName(targetNode);
        if (groupGeneric is null) return;

        // We need the containing type symbol to know what to replace with.
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null) return;

        // Climb to the field or property symbol to get its containing type.
        var memberDecl = groupGeneric.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        if (memberDecl is null) return;
        
        // var memberSymbol = semanticModel.GetDeclaredSymbol(memberDecl, context.CancellationToken);
        var memberSymbol = GetMemberSymbol(semanticModel, groupGeneric, context.CancellationToken);
        if (memberSymbol is null) return;

        // Register the fix
        context.RegisterCodeFix(
            CodeAction.Create(
                title: string.Format(Title, memberSymbol.Name),
                createChangedDocument: _ => ReplaceTypeArgumentAsync(
                    context.Document, root, groupGeneric, memberSymbol.Name),
                equivalenceKey: Title),
            diagnostic);
    }

    private static GenericNameSyntax? FindGroupGenericName(SyntaxNode node)
    {
        return node switch
        {
            // Handle: Group<T>, Namespace.Group<T>, Alias.Group<T>
            GenericNameSyntax { Identifier.Text: "Group" } g => g,
            QualifiedNameSyntax { Right: GenericNameSyntax { Identifier.Text: "Group" } gr } => gr,
            // Recurse left just in case we got the qualified root
            QualifiedNameSyntax q => FindGroupGenericName(q.Right) ?? FindGroupGenericName(q.Left),
            IdentifierNameSyntax or NameSyntax =>
                // Look upward for the generic
                node.AncestorsAndSelf().OfType<GenericNameSyntax>().FirstOrDefault(gn => gn.Identifier.Text == "Group"),
            _ => node.DescendantNodesAndSelf()
                .OfType<GenericNameSyntax>()
                .FirstOrDefault(gn => gn.Identifier.Text == "Group")
        };
    }
    
    private static ISymbol? GetMemberSymbol(SemanticModel sm, SyntaxNode anchor, CancellationToken ct)
    {
        // Fields: "Group<T> All = ..." -> need the VariableDeclarator
        var fieldVar = anchor.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
        if (fieldVar is not null)
            return sm.GetDeclaredSymbol(fieldVar, ct);

        // Properties: works directly on the PropertyDeclarationSyntax
        var prop = anchor.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
        if (prop is not null)
            return sm.GetDeclaredSymbol(prop, ct);

        // Events (both forms), if you ever allow them
        var eventDecl = anchor.FirstAncestorOrSelf<EventDeclarationSyntax>();
        if (eventDecl is not null)
            return sm.GetDeclaredSymbol(eventDecl, ct);

        var eventField = anchor.FirstAncestorOrSelf<EventFieldDeclarationSyntax>();
        if (eventField?.Declaration.Variables.FirstOrDefault() is { } eventVar)
            return sm.GetDeclaredSymbol(eventVar, ct);

        // Last resort: enclosing symbol (usually fine for containing type lookup)
        return sm.GetEnclosingSymbol(anchor.SpanStart, ct);
    }

    private static Task<Document> ReplaceTypeArgumentAsync(
        Document document,
        SyntaxNode root,
        GenericNameSyntax groupGeneric,
        string containingTypeName)
    {
        // Create the new single type argument
        var newTypeArg = SyntaxFactory.ParseTypeName(containingTypeName)
                                      .WithTriviaFrom(groupGeneric.TypeArgumentList.Arguments.First());

        var newTypeArgList = SyntaxFactory.TypeArgumentList(
            SyntaxFactory.SeparatedList([newTypeArg]))
            .WithTriviaFrom(groupGeneric.TypeArgumentList);

        var newGroup = groupGeneric.WithTypeArgumentList(newTypeArgList);

        var newRoot = root.ReplaceNode(groupGeneric, newGroup);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}