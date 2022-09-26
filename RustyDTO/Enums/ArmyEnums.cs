namespace RustyDTO;

[Flags]
public enum ArmyTypeFlags : int
{
    Swordsmen = 1 << 0,
    Spearman = 1 << 1,
    Cavalry = 1 << 2,
    All = ~0
}

[Flags]
public enum ArmyPropertyFlags : int
{
    Damage = 1 << 0,
    Defense = 1 << 1,
    Speed = 1 << 2,
    All = ~0
}

public enum ArmyType : byte
{
    Swordsmen = 0,
    Spearman = 1,
    Cavalry = 2
}

public static class ArmyEnumsEx
{
    public static int ToInt(this ArmyType type)
    {
        return (int)type;
    }
}