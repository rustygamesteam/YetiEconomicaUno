using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Diagnostics;
using H.Generators.Extensions;
using Microsoft.CodeAnalysis.CSharp;

namespace RustyDTOGenerator.Generators;

[Generator]
public class RustyGenerator : IIncrementalGenerator
{
    public const string Name = nameof(RustyGenerator);
    public const string Id = Name;

    private const string MainAttribute = "RustyDTO.Generator.PropertyImpl";
    private const string PropertyInfoAttribute = "RustyDTO.Generator.PropertyHave";
    private const string OverridePropertyNameAttribute = "RustyDTO.Generator.OverridePropertyName";
    private const string SkipCodegenAttribute = "RustyDTO.Generator.SkipCodegen";
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "DTOGenerator.g.cs",
            SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8)));
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "DTOSupport.g.cs",
            SourceText.From(SourceGenerationHelper.SupportAttribute, Encoding.UTF8)));

        var classes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is EnumDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (context, _) => GetSemanticTargetForGeneration(context))
            .Where(static syntax => syntax is not null);


        var resolvers = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (context, _) => GetSemanticResolversForGeneration(context))
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
        ImmutableArray<EnumDeclarationSyntax> enumDeclaration,
        SourceProductionContext context)
    {
        if (!options.IsDesignTime() && options.GetGlobalOption("DebuggerBreak", prefix: Name) != null)
            Debugger.Launch();

        if (enumDeclaration.IsDefaultOrEmpty)
            return;

        IReadOnlyCollection<PropertyEnumInfo>? classes;
        try
        {
            GetTypesToGenerate(compilation, enumDeclaration, out classes, out var links);

            string result;
            foreach (var info in classes)
            {
                result = SourceGenerationHelper.GenerateProperty(info);
                context.AddTextSource(
                    hintName: $"{info.Namespace}.{info.Name}.generated.cs",
                    text: result);

                if(info.Options.IsSkipImpl)
                    continue;
                
                switch (info.HelpType)
                {
                    case PropertyType.Mutable:
                        result = SourceGenerationHelper.GenerateMutableImpl(info);
                        context.AddTextSource(
                            hintName: $"{info.Namespace}.{info.Name}.impl.generated.cs",
                            text: result);
                        break;
                    case PropertyType.Desc:
                        result = SourceGenerationHelper.GenerateDescImpl(info);
                        context.AddTextSource(
                            hintName: $"{info.Namespace}.{info.Name}.impl.generated.cs",
                            text: result);
                        break;
                }
            }

            result = SourceGenerationHelper.GenerateEntityDependencies(links);
            context.AddTextSource(
                hintName: $"EntityDependencies.generated.cs",
                text: result);
        }
        catch (Exception exception)
        {
            classes = null;

            context.ReportException(
                id: "001",
                exception: exception,
                prefix: Id);
        }


        if (classes is null)
            return;

        try
        {
            StringBuilder sb = new StringBuilder(2048);
            SourceGenerationHelper.GenerateJsonDefaultResolver(sb, "RustyDTO.CodeGen.Impl", classes);

            if(sb.Length > 10)
                context.AddTextSource(
                    hintName: $"RustyPropertyJsonSerializerContext.g.cs",
                    text: sb.ToString());
            SourceGenerationHelper.GenerateSimpleDescPropertyResolver(sb, "RustyDTO.CodeGen.Impl", classes);
            if (sb.Length > 10)
                context.AddTextSource(
                    hintName: $"SimpleDescPropertyResolver.g.cs",
                    text: sb.ToString());
            SourceGenerationHelper.GenerateSimpleMutablePropertyResolver(sb, "RustyDTO.CodeGen.Impl", classes);
            if (sb.Length > 10)
                context.AddTextSource(
                    hintName: $"SimpleMutablePropertyResolver.g.cs",
                    text: sb.ToString());

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
        var values = new List<PropertyEnumInfo>(64);

        var members = new List<PropertyMember>(8);

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

                PropertyOptions options = new PropertyOptions
                {
                    Index = (int)enumFieldSymbol.ConstantValue! - 1,
                    EnumName = fullClassName
                };

                foreach (var attributeData in attributes)
                {
                    var attributeClass = attributeData.AttributeClass!;
                    var fullname = attributeClass.ToDisplayString();

                    if (fullname.StartsWith(PropertyInfoAttribute, StringComparison.Ordinal))
                    {
                        var memberName = attributeData.ConstructorArguments[0].Value as string;
                        if (memberName is null)
                            continue;

                        var typeArg = attributeClass.TypeArguments.ElementAtOrDefault(0)!;
                        var type = typeArg.ToDisplayString();
                        if (type.Contains('.'))
                            type = "global::" + type;


                        var isReadOnly = (bool)attributeData.ConstructorArguments[1].Value!;
                        var isNullable = (bool)attributeData.ConstructorArguments[2].Value!;
                        var defaultValueConstant = attributeData.ConstructorArguments[3];
                        var serializedName = (string?)attributeData.ConstructorArguments[4].Value;

                        string? defaultValue = defaultValueConstant.Kind switch
                        {
                            TypedConstantKind.Primitive => defaultValueConstant.Value?.ToString()?.ToLower(),
                            TypedConstantKind.Enum => $"(global::{defaultValueConstant.Type!.ToDisplayString()})({defaultValueConstant.Value!})",
                            _ => null
                        };

                        members.Add(new PropertyMember(typeArg.TypeKind, type, memberName, isReadOnly, isNullable, defaultValue, serializedName));
                    }
                    else if (fullname.StartsWith(OverridePropertyNameAttribute, StringComparison.Ordinal))
                    {
                        name = (string)attributeData.ConstructorArguments[0].Value!;
                    }
                    else if (fullname.StartsWith(SkipCodegenAttribute, StringComparison.Ordinal))
                    {
                        options.IsSkipImpl = (bool)attributeData.ConstructorArguments[0].Value!;
                        options.IsSkipResolver = (bool)attributeData.ConstructorArguments[1].Value!;
                    }
                }

                if(members.Count == 0)
                    continue;

                var helpType = baseType.Contains("Mutable") ? PropertyType.Mutable : PropertyType.Desc;
                values.Add(new PropertyEnumInfo(nameSpace, baseType, prefix, name, members.ToArray(), helpType, $"I{prefix}{name}", options));
                links.Add(new DependencyLink($"{nameSpace}.I{prefix}{name}", (int)enumFieldSymbol.ConstantValue!, fullClassName));
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

    private static ClassDeclarationSyntax? GetSemanticResolversForGeneration(GeneratorSyntaxContext context)
    {
        var syntax = (ClassDeclarationSyntax)context.Node;

        return syntax
            .AttributeLists
            .SelectMany(static list => list.Attributes)
            .Any(attributeSyntax => IsAttribute(attributeSyntax, context.SemanticModel, "RustyDTO.Generator.DefaultResolver"))
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

    private static bool IsAttribute(AttributeSyntax attributeSyntax, SemanticModel semanticModel, string attributeName)
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