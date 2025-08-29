namespace Engine.Tooling.Roslyn.Generators;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator]
public sealed class StaticInitGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext ctx)
    {
        // Collect uninitialized static fields of type Signal<T> with [Signal]/[SignalAttribute]
        var fields = ctx.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is FieldDeclarationSyntax { AttributeLists.Count: > 0 },
                static (context, _) =>
                {
                    var fieldDecl = (FieldDeclarationSyntax)context.Node;

                    // Only single-variable declarations: 'public static Signal<T> Foo;'
                    if (fieldDecl.Declaration.Variables.Count != 1)
                        return null;

                    // Variable already initialized
                    if (fieldDecl.Declaration.Variables[0].Initializer is not null)
                        return null;

                    if (context.SemanticModel.GetDeclaredSymbol(fieldDecl.Declaration.Variables[0])
                        is not IFieldSymbol { IsStatic: true } symbol)
                        return null;

                    // Must have [Signal] or [SignalAttribute]
                    if (!symbol.GetAttributes().Any(a => a.AttributeClass?.Name is "SignalAttribute" or "Signal"))
                        return null;

                    // Must be Signal<…>
                    if (symbol.Type is not INamedTypeSymbol { Name: "Signal", Arity: 1 })
                        return null;

                    return symbol;
                })
            .Where(static s => s is not null)!;

        // Collect static parameterless void methods with [OnPrepareResources]/[OnPrepareResourcesAttribute]
        var prepMethods = ctx.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is MethodDeclarationSyntax { AttributeLists.Count: > 0 },
                static (context, _) =>
                {
                    var methodDecl = (MethodDeclarationSyntax)context.Node;

                    // Must be declared static at syntax level (cheap check)
                    if (!methodDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
                        return null;

                    if (context.SemanticModel.GetDeclaredSymbol(methodDecl)
                        is not IMethodSymbol { IsStatic: true } method)
                        return null;

                    // Attribute filter
                    if (!method.GetAttributes().Any(a =>
                            a.AttributeClass?.Name is "OnPrepareResources" or "OnPrepareResourcesAttribute"))
                        return null;

                    // Signature: void M() — no params, returns void
                    if (!method.ReturnsVoid || method.Parameters.Length != 0)
                        return null;

                    return method;
                })
            .Where(static m => m is not null)!;

        // Combine both sets so we can generate a single static ctor per type
        var combined = fields.Collect().Combine(prepMethods.Collect());

        ctx.RegisterSourceOutput(combined, static (spc, pair) =>
        {
            var (fieldList, methodList) = pair;

            // Group by containing type
            var byType = fieldList.Cast<IFieldSymbol>()
                .Select(f => (type: f.ContainingType, field: f))
                .Concat(methodList.Cast<IMethodSymbol>().Select(m => (type: m.ContainingType, field: (IFieldSymbol?)null)))
                .GroupBy(x => x.type, SymbolEqualityComparer.Default);

            foreach (var group in byType)
            {
                var type = group.Key;
                if (type is null) continue;

                // Gather members per type
                var fieldsForType = fieldList.Cast<IFieldSymbol>()
                    .Where(f => SymbolEqualityComparer.Default.Equals(f.ContainingType, type))
                    .ToArray();

                var methodsForType = methodList.Cast<IMethodSymbol>()
                    .Where(m => SymbolEqualityComparer.Default.Equals(m.ContainingType, type))
                    .ToArray();

                // Nothing to do? Skip generating a part
                if (fieldsForType.Length == 0 && methodsForType.Length == 0)
                    continue;

                var ns = type.ContainingNamespace.ToDisplayString();

                var assignments = string.Join("\n",
                    fieldsForType.Select(f => $"            {f.Name} = new();"));

                var invokes = string.Join("\n",
                    methodsForType.Select(m => $"            {m.Name}();"));

                var bodyLines = string.Join("\n", new[]
                {
                    assignments,
                    string.IsNullOrWhiteSpace(assignments) || string.IsNullOrWhiteSpace(invokes) ? null : "",
                    invokes
                }.Where(s => s is not null));

                var src = $$"""
                    // <auto-generated/>  StaticInitGenerator
                    namespace {{ns}}
                    {
                        partial class {{type.Name}}
                        {
                            // Runs before anyone touches a static on this type
                            static {{type.Name}}()
                            {
                    {{bodyLines}}
                            }
                        }
                    }
                    """;

                spc.AddSource($"{type.Name}.StaticInit.g.cs", src);
            }
        });
    }
}