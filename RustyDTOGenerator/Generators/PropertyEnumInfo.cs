using Microsoft.CodeAnalysis;

namespace RustyDTOGenerator.Generators;

public record struct PropertyEnumInfo(string Namespace, string BaseType, string Prefix, string Name, PropertyMember[] Members, PropertyType HelpType, string InterfaceName, PropertyOptions Options);
public record struct PropertyMember(TypeKind Kind, string TypeName, string Name, bool IsReadOnly, bool IsNulable, string? DefaultValue, string? SerializedName);

public record struct DependencyLink(string TypeName, int Value, string EnumName);

public struct PropertyOptions
{
    public int Index;
    public bool IsSkipImpl;
    public bool IsSkipResolver;
    public bool IsForceResolver;
    public string EnumName;
}

public enum PropertyType
{
    Desc,
    Mutable
}