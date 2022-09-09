using System.Text;

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
    public PropertyHaveAttribute(string name, bool isReadOnly = false, bool isNullable = false, object? defaultValue = null)
    {
    }
}
";

    private static readonly StringBuilder _sb = new StringBuilder(1024);
    private static string InternalGenerateProperty(PropertyMember[] members)
    {
        _sb.Length = 0;
        var sb = _sb;
        foreach (var member in members)
        {
            if (member.DefaultValue != null)
            {
                sb.Append("\t[global::System.ComponentModel.DefaultValue(");
                sb.Append(member.DefaultValue);
                sb.Append(")]\n");
            }

            sb.Append('\t');
            sb.Append(member.TypeName);
            if (member.IsNulable)
                sb.Append('?');
            sb.Append(' ');
            sb.Append(member.Name);
            sb.Append(" { get; ");
            if (!member.IsReadOnly)
                sb.Append("set; ");
            sb.Append("}\n\n");
        }

        if (members.Length > 0)
            sb.Length -= 2;

        return sb.ToString();
    }

    public static string GenerateProperty(PropertyEnumInfo enumInfo)
    {
        var members = InternalGenerateProperty(enumInfo.Members);

        return @$"#nullable enable

namespace {enumInfo.Namespace};

public interface I{enumInfo.Prefix}{enumInfo.Name} : global::{enumInfo.BaseType}
{{
{members}
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
