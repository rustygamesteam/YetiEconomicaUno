using RustyDTO.Generator;
using RustyDTO.Interfaces;
using RustyDTO.Supports;

namespace RustyDTO;

[PropertyImpl<IMutableProperty>("RustyDTO.MutableProperties", "Mutable")]
public enum MutablePropertyType : int
{
    None = 0,
    [PropertyHave<int>("Count", defaultValue: 0)]
    Count = 1,
    [PropertyHave<IRustyEntity>("Manager", isNullable: true)]
    Manager = 2,
    [PropertyHave<int>("X", defaultValue: 0)]
    [PropertyHave<int>("Y", defaultValue: 0)]
    Position2D = 3,
    [PropertyHave<HexPosition>("Position", defaultValue: HexPosition.None)]
    PositionInsideHex = 4,
    [PropertyHave<IRustyEntity>("Entity", true)]
    OwnerArchetype = 5,
    [PropertyHave<IRustyEntity>("Entity")]
    UsedInstance = 6,
    Genom = 7,
    [PropertyHave<string>("ID", true)]
    Author = 8,
    [PropertyHave<string>("First", true)]
    [PropertyHave<string>("Second", true)]
    Parents = 9
}

public static class MutablePropertyTypeEx
{
    public static int AsIndex(this MutablePropertyType type)
    {
        return (int) type - 1;
    }
}