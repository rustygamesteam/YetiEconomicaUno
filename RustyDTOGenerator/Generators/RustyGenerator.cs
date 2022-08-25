using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace RustyDTOGenerator.Generators;

[Generator]
public class RustyGenerator : IIncrementalGenerator
{
    private static readonly HashSet<string> AttributeNames = new HashSet<string>
    {
        "RustyDTO.RequiredPropertiesAttribute",
        "RustyDTO.OptionalPropertiesAttribute",
        "RustyDTO.EntitySpecialMaskAttribute"
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "EntityDependencies.Attributes.g.cs",
            SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8)));


        var classes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (context, _) => GetSemanticTargetForGeneration(context))
            .Where(static syntax => syntax is not null);

        var compilationAndClasses = context.CompilationProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Combine(classes.Collect());

    }
    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var syntax = (ClassDeclarationSyntax)context.Node;

        return syntax
            .AttributeLists
            .SelectMany(static list => list.Attributes)
            .Any(attributeSyntax => IsGeneratorAttribute(attributeSyntax, context.SemanticModel))
            ? syntax
            : null;
    }

    private static bool IsGeneratorAttribute(AttributeSyntax attributeSyntax, SemanticModel semanticModel)
    {
        if (semanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
        {
            return false;
        }

        var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
        var fullName = attributeContainingTypeSymbol.ToDisplayString();

        return IsGeneratorAttribute(fullName);
    }

    private static bool IsGeneratorAttribute(string fullTypeName)
    {
        return AttributeNames.Contains(fullTypeName);
    }
}