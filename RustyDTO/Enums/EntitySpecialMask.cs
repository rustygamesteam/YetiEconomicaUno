namespace RustyDTO;

[Flags]
public enum EntitySpecialMask : int
{
    None = 0,
    HasChild = 1 << 0,
    HasParent = 1 << 1,
    Executable = 1 << 2,
    RequiredInDependencies = 1 << 3,
    IsInstance = 1 << 4,
    HasUniqueID = 1 << 5,
    IsCustomContent = 1 << 6,
    IsTask = Executable | ~IsInstance, 
}

public static class EntitySpecialMaskEx
{
    public static bool IsHas(this EntitySpecialMask flags, EntitySpecialMask mask)
    {
        return (flags & mask) != 0;
    }

    public static bool IsDontHas(this EntitySpecialMask flags, EntitySpecialMask mask)
    {
        return (flags & mask) == 0;
    }
}