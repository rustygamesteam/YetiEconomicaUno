namespace RustyDTOGenerator.Generators;

public class SourceGenerationHelper
{
    public const string Attribute = @"#nullable enable

namespace RustyDTO;

internal record struct EntityInfo(global::RustyDTO.RustyEntityType EntityType, global::RustyDTO.EntitySpecialMask Mask = global::RustyDTO.EntitySpecialMask.None)
{
    global::RustyDTO.EntityPropertyType[]? Requireds { get; init; } = null;
    global::RustyDTO.EntityPropertyType[]? Optionals { get; init; } = null;
}

[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
internal sealed class InjectEntityInfoAttribute : global::System.Attribute
{
    public EntityInfo Info { get; }

    public InjectEntityInfoAttribute(EntityInfo info)
    {
        Info = info;
    }
}";
}
