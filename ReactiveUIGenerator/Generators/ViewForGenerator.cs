using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics;
using H.Generators.Extensions;
using ReactiveUIGenerator.Models;

using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace ReactiveUIGenerator.Generators;


[Generator]
internal class ViewForGenerator : IIncrementalGenerator
{
    public const string Name = nameof(ViewForGenerator);
    public const string Id = Name;

    private const string ViewForAttributeFullName = "ReactiveUIGenerator.ViewForAttribute";

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

    private static bool IsGeneratorAttribute(string fullTypeName)
    {
        return
            fullTypeName.StartsWith(ViewForAttributeFullName, StringComparison.Ordinal);
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

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "ViewForAttribute.g.cs",
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

    private static void Execute(
        Compilation compilation,
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
            var classes = GetTypesToGenerate(compilation, classSyntaxes, context.CancellationToken);

            foreach (var @class in classes)
            {
                var result = SourceGenerationHelper.GenerateViewFor(@class, @class.ViewFor);
                var viewModelShortName = @class.ViewFor.ViewModelType.Split('.').Last();
                context.AddTextSource(
                        hintName: $"{@class.Name}.Properties.IViewFor.{viewModelShortName}.generated.cs",
                        text: result);
            }
        }
        catch (Exception exception)
        {
            context.ReportException(
                id: "001",
                exception: exception,
                prefix: Id);
        }
    }

    private static IReadOnlyCollection<ClassData> GetTypesToGenerate(
        Compilation compilation,
        IEnumerable<ClassDeclarationSyntax> classes,
        CancellationToken cancellationToken)
    {
        var values = new List<ClassData>();
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
            var classModifiers = classSymbol.DeclaredAccessibility switch
            {
                //Accessibility.Public => "public ",
                //Accessibility.Private => "private ",
                //Accessibility.Protected or Accessibility.ProtectedOrFriend => "protected ",
                _ => string.Empty
            };

            var attribute = @classSymbol.GetAttributes()
                .Where(IsGeneratorAttribute)
                .FirstOrDefault();

            if (attribute?.AttributeClass == null)
                continue;
            
            var attributeClass = attribute.AttributeClass.ToDisplayString() ?? string.Empty;
            if (attributeClass.StartsWith(ViewForAttributeFullName, StringComparison.Ordinal))
            {
                string? typeName = GetGenericTypeArgumentFromAttributeData(attribute, 0)?.ToDisplayString();

                if(typeName == null)
                {
                    if (attribute.ConstructorArguments.Count() > 0)
                    {
                        var data = attribute.ConstructorArguments.Single().Value as INamedTypeSymbol;
                        if (data == null)
                            continue;

                        typeName = data.ToDisplayString();
                    }
                    else
                        continue;
                }
                values.Add(new ClassData(@namespace, className, fullClassName, classModifiers, new ViewForData(typeName!)));
            }
        }
        return values;
    }

    private static ITypeSymbol? GetGenericTypeArgumentFromAttributeData(AttributeData data, int position)
    {
        return data.AttributeClass!.TypeArguments.ElementAtOrDefault(position);
    }

    private static TypedConstant? GetPropertyFromAttributeData(AttributeData data, string name)
    {
        return data.NamedArguments
            .FirstOrDefault(pair => pair.Key == name)
            .Value;
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
}
