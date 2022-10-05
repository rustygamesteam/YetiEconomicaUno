using RustyDTO.Generator;
using RustyDTO.Interfaces;
using RustyDTO.Supports;

namespace RustyDTO;

[PropertyImpl<IMutableProperty>("RustyDTO.MutableProperties", "Mutable")]
public enum MutablePropertyType : int
{
    None = 0,
    [PropertyHave<int>("Count")]
    Count = 1,
    [PropertyHave<IRustyEntity>("Manager", isNullable: true)]
    Manager = 2,
    [PropertyHave<int>("X")]
    [PropertyHave<int>("Y")]
    Position2D = 3,
    [PropertyHave<HexPosition>("Position")]
    PositionInsideHex = 4,
    [PropertyHave<IRustyEntity>("Entity")]
    OwnerArchetype = 5,
    [PropertyHave<IRustyEntity>("Entity")]
    UsedInstance = 6,
    Genom = 7
}

public static class MutablePropertyTypeEx
{
    public static int AsIndex(this MutablePropertyType type)
    {
        return (int) type - 1;
    }
}