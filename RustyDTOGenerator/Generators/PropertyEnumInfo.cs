using Microsoft.CodeAnalysis;

namespace RustyDTOGenerator.Generators;

public record struct PropertyEnumInfo(string Namespace, string BaseType, string Prefix, string Name, PropertyMember[] Members, int Index, PropertyType HelpType, string InterfaceName);
public record struct PropertyMember(TypeKind Kind, string TypeName, string Name, bool IsReadOnly, bool IsNulable, string? DefaultValue, string? SerializedName);

public record struct DependencyLink(string TypeName, int Value);

public enum PropertyType
{
    Desc,
    Mutable
}