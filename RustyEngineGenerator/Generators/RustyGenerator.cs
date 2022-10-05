using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace System.Runtime.CompilerServices.Generators;

[Generator]
internal class RustyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "RustyEngine.g.cs",
            SourceText.From(SourceGenerationHelper.AttributeSource, Encoding.UTF8)));

        var classes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ExplicitInterfaceSpecifierSyntax interfaceDeclaration,
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
        ImmutableArray<ExplicitInterfaceSpecifierSyntax> classSyntaxes,
        SourceProductionContext context)
    {

    }

    private static ExplicitInterfaceSpecifierSyntax GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var node = (ExplicitInterfaceSpecifierSyntax) context.Node;
        //var type = node.?.Types.ToFullString();
        return node;
    }
}