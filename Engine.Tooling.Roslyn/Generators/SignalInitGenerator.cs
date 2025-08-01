namespace Engine.Tooling.Roslyn.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator]
public sealed class SignalInitGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext ctx)
    {
        var fields = ctx.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is FieldDeclarationSyntax f && f.AttributeLists.Count > 0,
                static (context, _) =>
                {
                    var fieldDecl = (FieldDeclarationSyntax)context.Node;
                    // Only single-variable declarations: 'public static Signal<T> Foo;'
                    if (fieldDecl.Declaration.Variables.Count != 1)
                        return null;

                    // Variable already initialized
                    if (fieldDecl.Declaration.Variables[0].Initializer is not null)
                        return null;

                    var symbol = context.SemanticModel.GetDeclaredSymbol(
                        fieldDecl.Declaration.Variables[0]) as IFieldSymbol;

                    if (symbol is null || !symbol.IsStatic) return null;
                    if (!symbol.GetAttributes().Any(a =>
                           a.AttributeClass?.Name is "SignalAttribute" or "Signal")) return null;

                    // Must be Signal<…>
                    if (symbol.Type is not INamedTypeSymbol { Name: "Signal", Arity: 1 })
                        return null;

                    return symbol;
                })
            .Where(static s => s is not null)!;
        
        var grouped = fields.Collect();

        ctx.RegisterSourceOutput(grouped, static (spc, list) =>
        {
            var byType = list!
                .GroupBy(f => f!.ContainingType, SymbolEqualityComparer.Default);

            foreach (var grp in byType)
            {
                var type = grp.Key;
                var ns   = type.ContainingNamespace.ToDisplayString();

                var assignments = string.Join("\n",
                    grp.Select(f => $"        {f.Name} = new();"));

                var src = $$"""
                    // <auto-generated/>  SignalInitGenerator
                    namespace {{ns}}
                    {
                        partial class {{type.Name}}
                        {
                            // Runs before anyone touches a static on this type
                            static {{type.Name}}()
                            {
                    {{assignments}}
                            }
                        }
                    }
                    """;

                spc.AddSource($"{type.Name}.SignalInit.g.cs", src);
            }
        });
    }
}