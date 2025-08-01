// using System.Collections.Immutable;
// using System.Text;
// using JetBrains.Annotations;
// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using Microsoft.CodeAnalysis.Operations;
// #pragma warning disable RSEXPERIMENTAL002
//
// namespace Engine.User.Codegen.Generators;
//
// [Generator]
// public sealed class InterceptorGenerator : IIncrementalGenerator
// {
//     public void Initialize(IncrementalGeneratorInitializationContext ctx)
//     {
//         var locations = ctx.SyntaxProvider.CreateSyntaxProvider(
//             predicate: static (node, _) => InterceptorPredicate(node),
//             transform: static (context, ct) => InterceptorTransform(context, ct)
//         ).Where(candidate => candidate is not null);
//         
//         ctx.RegisterSourceOutput(locations, ExecuteInterceptors);
//     }
//
//     private static void ExecuteInterceptors(
//         SourceProductionContext context,
//         CandidateInvocation? toIntercept)
//     {
//         var sb = new StringBuilder();
//     }
//
//     private static bool InterceptorPredicate(SyntaxNode node) =>
//         node is InvocationExpressionSyntax {
//             Expression: MemberAccessExpressionSyntax {
//                 Name.Identifier.ValueText: "TestFunctionPlease"
//             }
//         };
//     
//     private static CandidateInvocation? InterceptorTransform(GeneratorSyntaxContext ctx, CancellationToken ct)
//     {
//         // Is this an instance method invocation? (we know it must be due to the predicate check, but play it safe)
//         if (ctx.Node is InvocationExpressionSyntax {Expression: MemberAccessExpressionSyntax {Name: { } nameSyntax}} invocation
//             // Get the semantic definition of the method invocation
//             && ctx.SemanticModel.GetOperation(ctx.Node, ct) is IInvocationOperation targetOperation
//             // This is the main check - is the method a ToString invocation on System.Enum.ToString()?
//             && targetOperation.TargetMethod is {Name : "TestFunctionPlease", ContainingType: {Name: "PlayerCharacter", ContainingNamespace: {Name: "User.Game.Player", ContainingNamespace.IsGlobalNamespace: true}}}
//             // Grab the Type of the enum on which this is being invoked 
//             && targetOperation.Instance?.Type is { } type)
//         {
//             // If we get to here, we know we want to generate an interceptor,
//             // so use the experimental GetInterceptableLocation() API to get the data
//             // we need. This returns null if the location is not interceptable, but
//             // should never be non-null for this example.
//             if (ctx.SemanticModel.GetInterceptableLocation(invocation) is { } location)
//             {
//                 // Return the location details and the full type details
//                 return new CandidateInvocation(location, type.ToString());
//             }
//         }
//
//         // Not an interceptor location we're interested in 
//         return null;
//     }
//
//     // Record for holding the interception details
//     public record CandidateInvocation(InterceptableLocation Location, string EnumName);
//
// }