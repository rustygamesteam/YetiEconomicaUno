namespace RustyDTO;

[Flags]
public enum EntitySpecialMask : int
{
    None = 0,
    HasChild = 1 << 0,
    HasParent = 1 << 1,
    Countable = 1 << 2,
    Executable = 1 << 3,
    RequiredInDependencies = 1 << 4,
    IsInstance = 1 << 5,
    IsTask = 1 << 6
}