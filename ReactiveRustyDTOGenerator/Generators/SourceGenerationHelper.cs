namespace ReactiveRustyDTOGenerator.Generators;

public static class SourceGenerationHelper
{
    public const string Attribute = @"#nullable enable

namespace YetiEconomicaCore.ReactiveImpl;

[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
internal sealed class FactoryForPropertyAttribute<TBase> : global::System.Attribute where TBase : global::RustyDTO.Interfaces.IDescProperty
{
    public FactoryForPropertyAttribute(string? defaultValueMethod = null, bool makePartial = false)
    {
    }
}
";
}