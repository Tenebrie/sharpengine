using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Engine.Roslyn.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CustomNullableFieldAnalyzer : DiagnosticAnalyzer
{
    // Our own version of CS8618 that we control
    private static readonly DiagnosticDescriptor NonNullableFieldRule = new(
        "TN8618", // Our own ID that mimics CS8618
        "Non-nullable field must contain a non-null value when exiting constructor",
        "Non-nullable field '{0}' is uninitialized. Consider adding the 'required' modifier or declaring the field as nullable.",
        "Compiler",
        DiagnosticSeverity.Warning,
        true,
        "Non-nullable fields must be initialized before exiting the constructor or be marked as required.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [NonNullableFieldRule];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(compilationContext =>
        {
            compilationContext.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
        });
    }

    private void AnalyzeClass(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, context.CancellationToken);
        
        if (classSymbol == null) return;

        // Get all non-nullable fields that should be checked
        var fieldsToCheck = classSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => ShouldReportUninitializedField(f) && !IsComponentField(f))
            .ToList();

        if (!fieldsToCheck.Any()) return;

        // Check initialization in constructors
        var constructors = classDeclaration.Members.OfType<ConstructorDeclarationSyntax>().ToList();
        var onInitMethods = classDeclaration.Members.OfType<MethodDeclarationSyntax>()
            .Where(m => HasOnInitAttribute(m, semanticModel)).ToList();

        foreach (var field in fieldsToCheck)
        {
            var isInitialized = constructors.Any(constructor => IsFieldInitializedInMethod(field, constructor.Body, semanticModel));

            if (!isInitialized)
            {
                if (onInitMethods.Any(onInitMethod => IsFieldInitializedInMethod(field, onInitMethod.Body, semanticModel)))
                {
                    isInitialized = true;
                }
            }

            // Report diagnostic if field is not initialized anywhere
            if (isInitialized) continue;
            
            // Find the field declaration syntax to report at the right location
            var fieldDeclaration = GetFieldDeclarationSyntax(field, classDeclaration);
            if (fieldDeclaration == null) continue;
            
            var diagnostic = Diagnostic.Create(
                NonNullableFieldRule,
                fieldDeclaration.GetLocation(),
                field.Name);
                    
            context.ReportDiagnostic(diagnostic);
        }
    }

    private bool HasOnInitAttribute(MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        return method.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(attr =>
            {
                var attrSymbol = semanticModel.GetSymbolInfo(attr).Symbol as IMethodSymbol;
                return attrSymbol?.ContainingType?.Name is "OnInit" or "OnInitAttribute";
            });
    }

    private static VariableDeclaratorSyntax? GetFieldDeclarationSyntax(IFieldSymbol field, ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration.Members
            .OfType<FieldDeclarationSyntax>()
            .SelectMany(fd => fd.Declaration.Variables)
            .FirstOrDefault(v => v.Identifier.ValueText == field.Name);
    }

    private bool ShouldReportUninitializedField(IFieldSymbol field)
    {
        // Skip static fields, const fields, readonly fields with initializers
        if (field.IsStatic || field.IsConst || field.IsReadOnly) return false;
        
        // Skip if field has an initializer
        if (field.HasConstantValue || field.DeclaringSyntaxReferences.Any(HasInitializer)) return false;
        
        // Skip if field is nullable
        if (field.Type.CanBeReferencedByName && field.NullableAnnotation == NullableAnnotation.Annotated) return false;
        
        // Skip if field type is nullable value type
        if (field.Type is { IsValueType: true, OriginalDefinition.SpecialType: SpecialType.System_Nullable_T }) return false;
        
        // Skip if field is marked as required
        if (field.GetAttributes().Any(a => a.AttributeClass?.Name == "RequiredAttribute")) return false;
        
        // Report for non-nullable reference types and non-nullable value types
        return field.Type.IsReferenceType || field.Type.IsValueType;
    }

    private static bool IsComponentField(IFieldSymbol field)
    {
        return field.GetAttributes()
            .Any(a => a.AttributeClass?.Name is "Component" or "ComponentAttribute" 
                      or "Signal" or "SignalAttribute");
    }

    private bool HasInitializer(SyntaxReference syntaxRef)
    {
        if (syntaxRef.GetSyntax() is VariableDeclaratorSyntax variable)
        {
            return variable.Initializer != null;
        }
        return false;
    }

    private bool IsFieldInitializedInMethod(IFieldSymbol field, BlockSyntax? methodBody, SemanticModel semanticModel)
    {
        if (methodBody == null) return false;
        
        // Look for assignments to the field in the method body
        var assignments = methodBody.DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .Where(assignment => IsAssignmentToField(assignment, field, semanticModel));
        
        return assignments.Any();
    }

    private static bool IsAssignmentToField(AssignmentExpressionSyntax assignment, IFieldSymbol field, SemanticModel semanticModel)
    {
        switch (assignment.Left)
        {
            // Handle simple field assignment: fieldName = value
            case IdentifierNameSyntax identifier:
                return identifier.Identifier.ValueText == field.Name;
            // Handle member access assignment: this.fieldName = value
            case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: IdentifierNameSyntax memberName }:
                return memberName.Identifier.ValueText == field.Name;
            default:
            {
                // For more complex cases, we could use semantic model to resolve the symbol
                // This is a more robust approach but might be overkill for simple cases
                var symbolInfo = semanticModel.GetSymbolInfo(assignment.Left);
                return SymbolEqualityComparer.Default.Equals(symbolInfo.Symbol, field);
            }
        }
    }
}