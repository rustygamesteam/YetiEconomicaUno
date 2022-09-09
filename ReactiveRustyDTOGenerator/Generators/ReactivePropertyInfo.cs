namespace ReactiveRustyDTOGenerator.Generators;

public record struct ReactivePropertyInfo(bool MakePartial, string Name, string BaseInterface, int BodyLength, string? DefaultValueMethod, IEnumerable<(string BaseType, string Name, string DefaultValue, bool IsReadOnly)> Body);