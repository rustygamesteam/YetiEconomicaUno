namespace RustyDTOGenerator.Generators;

public record struct PropertyEnumInfo(string Namespace, string BaseType, string Prefix, string Name, PropertyMember[] Members);
public record struct PropertyMember(string TypeName, string Name, bool IsReadOnly, bool IsNulable);

public record struct DependencyLink(string TypeName, int Value);