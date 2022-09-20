using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Diagnostics;
using H.Generators.Extensions;
using System.Linq;
using System;

namespace ReactiveRustyDTOGenerator.Generators;

[Generator]
public class SourceGenerator : IIncrementalGenerator
{
    public const string Name = "ReactiveRustyDTOGenerator";
    public const string Id = Name;
    
    private const string DefaultValueAttribute = "System.ComponentModel.DefaultValue";
    private const string Attribute = "YetiEconomicaCore.ReactiveImpl.FactoryForPropertyAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "ReactiveRustyDTOGenerator.g.cs",
            SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8)));

        var classes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (context, _) => GetSemanticTargetForGeneration(context))
            .Where(static syntax => syntax is not null);

        var compilationAndClasses = context.CompilationProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Combine(classes.Collect());

        context.RegisterSourceOutput(
            compilationAndClasses,
            static (context, source) => Execute(source.Left.Left, source.Left.Right, source.Right!, context));
    }

    private static void Execute(Compilation compilation,
        AnalyzerConfigOptionsProvider options,
        ImmutableArray<ClassDeclarationSyntax> classSyntaxes,
        SourceProductionContext context)
    {
        if (!options.IsDesignTime() && options.GetGlobalOption("DebuggerBreak", prefix: Name) != null)
            Debugger.Launch();

        if (classSyntaxes.IsDefaultOrEmpty)
            return;

        try
        {
            var sbFactory = new StringBuilder(1024);

            sbFactory.Append("namespace YetiEconomicaCore.ReactiveImpl;\n\n");
            sbFactory.Append("internal static partial class ReactiveUniversalFactory\n");
            sbFactory.Append("{\n");

            var sb = new StringBuilder(1024);
            var result = GetTypesToGenerate(compilation, classSyntaxes, context.CancellationToken);
            
            foreach (var info in result)
            {
                sb.Length = 0;
                sb.Append("namespace YetiEconomicaCore.ReactiveImpl;\n\n");
                sb.Append("internal sealed ");
                if (info.MakePartial)
                    sb.Append("partial ");
                sb.Append("class ");
                sb.Append(info.Name);
                sb.Append(" : global::ReactiveUI.ReactiveObject, ");
                sb.Append(info.BaseInterface);
                sb.Append("\n{\n");

                sb.Append("\tpublic ");
                sb.Append(info.Name);
                sb.Append("(int index, global::LiteDB.BsonValue data)\n");
                sb.Append("\t{\n");

                sb.Append("\t\tIndex = index;\n");

                sb.Append("\t\tvar mapper = global::LiteDB.BsonMapper.Global;\n");

                if (info.BodyLength == 1)
                {
                    var tuple = info.Body.First();
                    sb.Append("\t\t");
                    sb.Append(tuple.Name);
                    sb.Append(" = mapper.Deserialize<");
                    sb.Append(tuple.BaseType);
                    sb.Append(">(data);\n");
                }
                else
                {
                    foreach (var tuple in info.Body)
                    {
                        sb.Append("\t\t");
                        sb.Append(tuple.Name);
                        sb.Append(" = mapper.Deserialize<");
                        sb.Append(tuple.BaseType);
                        sb.Append(">(data[nameof(");
                        sb.Append(tuple.Name);
                        sb.Append(")]);\n");
                    }
                }
                sb.Append("\t}\n\n");

                sb.Append("\tpublic int Index { get; }\n");

                foreach (var tuple in info.Body)
                {
                    sb.AppendLine();
                    if (!tuple.IsReadOnly)
                        sb.Append("\t[global::ReactiveUI.Fody.Helpers.ReactiveAttribute]\n");
                    sb.Append("\tpublic ");
                    sb.Append(tuple.BaseType);
                    sb.Append(' ');
                    sb.Append(tuple.Name);
                    sb.Append(" { get; ");
                    if(!tuple.IsReadOnly)
                        sb.Append("set; ");
                    sb.Append('}');
                }

                sb.Append("\n}");
                
                context.AddTextSource(hintName: $"{info.Name}.generated.cs", text: sb.ToString());

                GenerateFactoryMethod(sbFactory, info);
            }

            sbFactory.Append('}');

            context.AddTextSource(hintName: $"ReactiveUniversalFactory.generated.cs", text: sbFactory.ToString());
        }
        catch (Exception exception)
        {
            context.ReportException(
                id: "001",
                exception: exception,
                prefix: Id);
        }
    }

    private static void GenerateFactoryMethod(StringBuilder sbFactory, ReactivePropertyInfo info)
    {
        sbFactory.Append("\tpublic static ReactiveFactory<global::YetiEconomicaCore.ReactiveImpl.");
        sbFactory.Append(info.Name);
        sbFactory.Append("> ");
        sbFactory.Append(info.Name);
        sbFactory.Append("Factory()\n");
        sbFactory.Append("\t{\n");
        sbFactory.Append("\t\tvar defValue = ");
        if (info.DefaultValueMethod is null)
        {
            if (info.BodyLength == 0)
                sbFactory.Append("global::LiteDB.BsonValue.Null;\n");
            else if (info.BodyLength == 1)
            {
                var item = info.Body.First();

                sbFactory.Append("new global::LiteDB.BsonValue(");
                sbFactory.Append(item.DefaultValue);
                sbFactory.Append(");\n");
            }
            else
            {
                sbFactory.Append("new global::LiteDB.BsonDocument\n\t\t{\n");
                foreach (var tuple in info.Body)
                {
                    sbFactory.Append("\t\t\t{ nameof(global::");
                    sbFactory.Append(info.BaseInterface);
                    sbFactory.Append('.');
                    sbFactory.Append(tuple.Name);
                    sbFactory.Append("), ");
                    sbFactory.Append(tuple.DefaultValue);
                    sbFactory.Append(" },\n");
                }

                sbFactory.Length -= 2;
                sbFactory.Append("};\n");
            }
        }
        else
        {
            sbFactory.Append(info.DefaultValueMethod);
            sbFactory.Append("();\n");
        }

        sbFactory.Append("\t\treturn new ReactiveFactory<global::YetiEconomicaCore.ReactiveImpl.");
        sbFactory.Append(info.Name);
        sbFactory.Append(">(defValue, (index, data) => new global::YetiEconomicaCore.ReactiveImpl.");
        sbFactory.Append(info.Name);
        sbFactory.Append("(index, data));\n");
        sbFactory.Append("\t}\n\n");
    }

    private static IEnumerable<ReactivePropertyInfo> GetTypesToGenerate(
    Compilation compilation,
        IEnumerable<ClassDeclarationSyntax> classes,
        CancellationToken cancellationToken)
    {
        foreach (var group in classes.GroupBy(@class => GetFullClassName(compilation, @class)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var @class = group.First();
            var semanticModel = compilation.GetSemanticModel(@class.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(@class, cancellationToken) is not INamedTypeSymbol classSymbol)
                continue;

            var fullClassName = classSymbol.ToString()!;
            var @namespace = fullClassName.Substring(0, fullClassName.LastIndexOf('.'));
            var className = fullClassName.Substring(fullClassName.LastIndexOf('.') + 1);

            var attributes = @classSymbol.GetAttributes()
                .Where(IsGeneratorAttribute);

            foreach (var attribute in attributes)
            {
                if ((attribute?.AttributeClass) is null)
                    continue;

                var defaultValueMethod = attribute.ConstructorArguments[0].Value?.ToString();
                var makePartial = (bool)attribute.ConstructorArguments[1].Value;

                var type = attribute.AttributeClass.TypeArguments[0];
                var baseType = type.ToDisplayString();
                var membersRaw = type.GetMembers().Where(static symbol => symbol.Kind == SymbolKind.Property);
                var members = membersRaw.Cast<IPropertySymbol>().Select(static symbol =>
                {
                    var name = symbol.ToDisplayString();
                    var lastIndex = name.LastIndexOf('.');
                    if (lastIndex != -1)
                        name = name.Substring(lastIndex + 1);

                    var type = symbol.Type.ToDisplayString();
                    if (type.IndexOf('.') != -1)
                        type = "global::" + type;

                    string defualValue;
                    var defualValueAttribute = symbol.GetAttributes().Where(data =>
                    {
                        var displayName = data.AttributeClass?.ToDisplayString();
                        if (displayName is null)
                            return false;

                        return displayName.StartsWith(DefaultValueAttribute, StringComparison.Ordinal);

                    }).FirstOrDefault();


                    if (defualValueAttribute is null)
                        defualValue = $"({type})default";
                    else
                    {
                        var defaultArgument = defualValueAttribute.ConstructorArguments[0];
                        var defaultValueAsString = defaultArgument.Value?.ToString();
                        if (defaultValueAsString is null)
                            defaultValueAsString = $"({type})default";
                        else if (defaultArgument.Kind is TypedConstantKind.Primitive)
                            defaultValueAsString = defaultValueAsString.ToLower();

                        defualValue = defaultValueAsString;
                    }

                    return (type, name, defualValue, symbol.IsReadOnly);
                });

                var name = $"Reactive{baseType.Substring(baseType.LastIndexOf('.') + 2)}";

                yield return new ReactivePropertyInfo(makePartial, name, baseType, membersRaw.Count(), defaultValueMethod, members);
            }
        }
    }

    private static bool IsGeneratorAttribute(AttributeData attributeData)
    {
        var attributeClass = attributeData.AttributeClass?.ToDisplayString() ?? string.Empty;

        return IsGeneratorAttribute(attributeClass);
    }

    private static string? GetFullClassName(Compilation compilation, ClassDeclarationSyntax classDeclarationSyntax)
    {
        var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
        if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        return classSymbol.ToString();
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
        return fullTypeName.StartsWith(Attribute, StringComparison.Ordinal);
    }
}