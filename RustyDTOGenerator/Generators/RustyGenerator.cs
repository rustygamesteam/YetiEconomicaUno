using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Diagnostics;
using H.Generators.Extensions;

namespace RustyDTOGenerator.Generators;

[Generator]
public class RustyGenerator : IIncrementalGenerator
{
    public const string Name = nameof(RustyGenerator);
    public const string Id = Name;

    private const string MainAttribute = "RustyDTO.Generator.PropertyImpl";
    private const string PropertyInfoAttribute = "RustyDTO.Generator.PropertyHave";
    private const string OverridePropertyNameAttribute = "RustyDTO.Generator.OverridePropertyName";


    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "DTOGenerator.g.cs",
            SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8)));


        var classes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is EnumDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (context, _) => GetSemanticTargetForGeneration(context))
            .Where(static syntax => syntax is not null);

        var compilationAndClasses = context.CompilationProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Combine(classes.Collect());

        context.RegisterSourceOutput(
            compilationAndClasses,
            static (context, source) => Execute(source.Left.Left, source.Left.Right, source.Right!, context));
    }

    private static void Execute(
        Compilation compilation,
        AnalyzerConfigOptionsProvider options,
        ImmutableArray<EnumDeclarationSyntax> classSyntaxes,
        SourceProductionContext context)
    {
        if (!options.IsDesignTime() && options.GetGlobalOption("DebuggerBreak", prefix: Name) != null)
            Debugger.Launch();

        if (classSyntaxes.IsDefaultOrEmpty)
            return;

        try
        {
            GetTypesToGenerate(compilation, classSyntaxes, out var classes, out var links);

            string result;
            foreach (var info in classes)
            {
                result = SourceGenerationHelper.GenerateProperty(info);
                context.AddTextSource(
                    hintName: $"{info.Namespace}.{info.Name}.generated.cs",
                    text: result);
            }

            result = SourceGenerationHelper.GenerateEntityDependencies(links);
            context.AddTextSource(
                hintName: $"EntityDependencies.generated.cs",
                text: result);
        }
        catch (Exception exception)
        {
            context.ReportException(
                id: "001",
                exception: exception,
                prefix: Id);
        }
    }

    private static void GetTypesToGenerate(
        Compilation compilation,
        IEnumerable<EnumDeclarationSyntax> enums, out IReadOnlyCollection<PropertyEnumInfo> classes, out DependencyLink[] dependencies)
    {

        var links = new List<DependencyLink>(64);
        var members = new List<PropertyMember>(8);
        var values = new List<PropertyEnumInfo>(8);

        foreach (var @enum in enums)
        {
            var semanticModel = compilation.GetSemanticModel(@enum.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(@enum) is not INamedTypeSymbol classSymbol)
                continue;

            var fullClassName = classSymbol.ToString();
            var className = fullClassName.Substring(fullClassName.LastIndexOf('.') + 1);


            var attribute = @classSymbol.GetAttributes()
                .Where(IsGeneratorAttribute)
                .FirstOrDefault();

            if (attribute?.AttributeClass is null || attribute.ConstructorArguments.Length == 0)
                continue;

            var baseType = attribute.AttributeClass.TypeArguments[0].ToDisplayString();
            var nameSpace = attribute.ConstructorArguments[0].Value!.ToString();
            var prefix = attribute.ConstructorArguments[1].Value?.ToString() ?? string.Empty;
            foreach (var enumMember in @enum.Members)
            {
                semanticModel = compilation.GetSemanticModel(enumMember.SyntaxTree);
                var declaredSymbol = semanticModel.GetDeclaredSymbol(enumMember);
                if (declaredSymbol is not IFieldSymbol enumFieldSymbol)
                    continue;

                var attributes = enumFieldSymbol.GetAttributes();
                if(attributes.Length == 0)
                    continue;

                string name = enumFieldSymbol.Name;
                members.Clear();

                
                foreach (var attributeData in attributes)
                {
                    var attributeClass = attributeData.AttributeClass!;
                    var fullname = attributeClass.ToDisplayString();

                    if (fullname.StartsWith(PropertyInfoAttribute, StringComparison.Ordinal))
                    {
                        var memberName = attributeData.ConstructorArguments[0].Value as string;
                        if (memberName is null)
                            continue;

                        var type = attributeClass.TypeArguments.ElementAtOrDefault(0)!.ToDisplayString();
                        if (type.Contains('.'))
                            type = "global::" + type;


                        var isReadOnly = (bool)attributeData.ConstructorArguments[1].Value;
                        var isNullable = (bool)attributeData.ConstructorArguments[2].Value;
                        var defaultValueConstant = attributeData.ConstructorArguments[3];
                        
                        string? defaultValue = defaultValueConstant.Kind switch
                        {
                            TypedConstantKind.Primitive => defaultValueConstant.Value?.ToString()?.ToLower(),
                            TypedConstantKind.Enum => $"(global::{defaultValueConstant.Type!.ToDisplayString()})({defaultValueConstant.Value!.ToString()})",
                            _ => null
                        };

                        members.Add(new PropertyMember(type, memberName, isReadOnly, isNullable, defaultValue));
                    }
                    else if (fullname.StartsWith(OverridePropertyNameAttribute, StringComparison.Ordinal))
                    {
                        name = attributeData.ConstructorArguments[0].Value as string;
                    }
                }

                if(members.Count == 0)
                    continue;

                values.Add(new PropertyEnumInfo(nameSpace, baseType, prefix, name, members.ToArray()));
                links.Add(new DependencyLink($"{nameSpace}.I{prefix}{name}", (int)enumFieldSymbol.ConstantValue));
            }
        }

        classes = values;
        dependencies = links.ToArray();
    }

    private static TypedConstant? GetPropertyFromAttributeData(AttributeData data, string name)
    {
        return data.NamedArguments
            .FirstOrDefault(pair => pair.Key == name)
            .Value;
    }

    private static EnumDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var syntax = (EnumDeclarationSyntax)context.Node;

        return syntax
            .AttributeLists
            .SelectMany(static list => list.Attributes)
            .Any(attributeSyntax => IsGeneratorAttribute(attributeSyntax, context.SemanticModel))
            ? syntax
            : null;
    }

    private static bool IsGeneratorAttribute(AttributeData attributeData)
    {
        var attributeClass = attributeData.AttributeClass?.ToDisplayString() ?? string.Empty;

        return IsGeneratorAttribute(attributeClass);
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
        return fullTypeName.StartsWith(MainAttribute, StringComparison.Ordinal);
    }

    
    private static bool IsEnumPropertyAttribute(AttributeSyntax attributeSyntax, SemanticModel semanticModel)
    {
        if (semanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
        {
            return false;
        }

        var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
        var fullName = attributeContainingTypeSymbol.ToDisplayString();

        return fullName.StartsWith(PropertyInfoAttribute, StringComparison.Ordinal);
    }
}