using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Engine.Tooling.Roslyn.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
[SuppressMessage("MicrosoftCodeAnalysisDesign", "RS1017:DiagnosticId for analyzers must be a non-null constant")]
public sealed class DefaultGroupMustMatchContainingTypeAnalyzer : DiagnosticAnalyzer
{
    private static readonly string DiagnosticId = AnalyzerCode.DefaultGroupMustMatchContainingType.GetCode();

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "DefaultGroup must be Group<ContainingType>",
        messageFormat: "DefaultGroup for '{0}' must be declared as Group<{0}>, but found {1}",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

     public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
    }

    private static void AnalyzeField(SymbolAnalysisContext ctx)
    {
        var symbol = (IFieldSymbol)ctx.Symbol;
        AnalyzeMember(ctx, symbol, symbol.Type, symbol.ContainingType);
    }

    private static void AnalyzeProperty(SymbolAnalysisContext ctx)
    {
        var symbol = (IPropertySymbol)ctx.Symbol;
        AnalyzeMember(ctx, symbol, symbol.Type, symbol.ContainingType);
    }

    private static void AnalyzeMember(
        SymbolAnalysisContext ctx,
        ISymbol member,
        ITypeSymbol memberType,
        INamedTypeSymbol? containingType)
    {
        if (containingType is null) return;

        var defaultGroupAttr = member
            .GetAttributes()
            .FirstOrDefault(a => IsDefaultGroupAttribute(a.AttributeClass));
        if (defaultGroupAttr is null) return;

        if (memberType is not INamedTypeSymbol named || named.Arity != 1 || !IsGroupType(named))
            return;

        var typeArg = named.TypeArguments[0];

        if (!SymbolEqualityComparer.Default.Equals(typeArg, containingType))
        {
            var foundDisplay = named.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            var typeLocation = TryGetGroupTypeLocation(member, ctx.CancellationToken)
                               ?? member.Locations.FirstOrDefault();

            var diagnostic = Diagnostic.Create(
                Rule,
                typeLocation,
                containingType.Name,
                foundDisplay);

            ctx.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsDefaultGroupAttribute(INamedTypeSymbol? attrType)
    {
        if (attrType is null) return false;
        return attrType.Name.Equals("DefaultGroupAttribute", StringComparison.Ordinal)
               || attrType.ToDisplayString().EndsWith(".DefaultGroupAttribute", StringComparison.Ordinal);
    }

    private static bool IsGroupType(INamedTypeSymbol named)
    {
        return named.Name.Equals("Group", StringComparison.Ordinal) && named.Arity == 1;
    }

    private static Location? TryGetGroupTypeLocation(ISymbol member, CancellationToken ct)
    {
        foreach (var decl in member.DeclaringSyntaxReferences)
        {
            var node = decl.GetSyntax(ct);
            switch (node)
            {
                case VariableDeclaratorSyntax v:
                    // field: "Group<T> a, b;" (same Type for all declarators)
                    return (v.Parent as VariableDeclarationSyntax)?.Type.GetLocation();

                case PropertyDeclarationSyntax p:
                    return p.Type.GetLocation();

                case FieldDeclarationSyntax f:
                    return f.Declaration.Type.GetLocation();
            }
        }
        return null;
    }
}
