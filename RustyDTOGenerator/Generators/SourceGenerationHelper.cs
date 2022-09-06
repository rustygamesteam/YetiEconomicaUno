namespace RustyDTOGenerator.Generators;

internal class SourceGenerationHelper
{
    public const string Attribute = @"#nullable enable

namespace RustyDTO.Generator;

[global::System.AttributeUsage(global::System.AttributeTargets.Enum, AllowMultiple = false)]
internal sealed class PropertyImplAttribute<TBase> : global::System.Attribute
{
    public PropertyImplAttribute(string nameSpace, string prefix = null)
    {
    }
}

[global::System.AttributeUsage(global::System.AttributeTargets.Field, AllowMultiple = false)]
internal sealed class OverridePropertyNameAttribute<TType> : global::System.Attribute
{
    public OverridePropertyNameAttribute(string name)
    {
    }
}

[global::System.AttributeUsage(global::System.AttributeTargets.Field, AllowMultiple = true)]
internal sealed class PropertyHaveAttribute<TType> : global::System.Attribute
{
    public PropertyHaveAttribute(string name, bool isReadOnly = false, bool isNullable = false)
    {
    }
}
";

    public static string GenerateProperty(PropertyEnumInfo enumInfo)
    {
        return @$"#nullable enable

namespace {enumInfo.Namespace};

public interface I{enumInfo.Prefix}{enumInfo.Name} : global::{enumInfo.BaseType}
{{
{string.Join("\n\n", enumInfo.Members.Select(member => $"    {member.TypeName}{NulableGenerator(member.IsNulable)} {member.Name} {{ get; {SetGenerator(member.IsReadOnly)}}}"))}
}}
";
    }

    public static string GenerateEntityDependencies(DependencyLink[] links)
    {
        return $@"#nullable enable

namespace RustyDTO;

public static partial class EntityDependencies
{{
    private static IReadOnlyDictionary<Type, int> _propertyTypes = new Dictionary<Type, int>
    {{
{string.Join(",\n", links.Select(link => $@"      {{ typeof(global::{link.TypeName}), {link.Value-1} }}"))}
    }};
}}
";
    }

    private static string NulableGenerator(bool isNullable)
    {
        return isNullable ? "?" : string.Empty;
    }

    private static string SetGenerator(bool isReadOnly)
    {
        return isReadOnly ? string.Empty : "set; ";
    }
}
