using RustyDTO.Generator;
using RustyDTO.Interfaces;
using RustyDTO.Supports;

namespace RustyDTO;

[PropertyImpl<IDescProperty>("RustyDTO.DescPropertyModels")]
public enum DescPropertyType
{
    None = 0,
    [PropertyHave<int>("Tear", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    [PropertyHave<IRustyEntity>("Owner", isReadOnly: true)]
    HasOwner = 1,

    [PropertyHave<IRustyEntity>("Required", isReadOnly: DescPropertyTypeEx.IsNotReactive, isNullable: true)]
    [PropertyHave<IRustyEntity>("VisibleAfter", isReadOnly: DescPropertyTypeEx.IsNotReactive, isNullable: true)]
    HasDependents = 2,

    [PropertyHave<ICollection<ResourceStack>>("Price", isReadOnly: true)]
    Payable = 3,

    [PropertyHave<int>("Duration", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    LongExecution = 4,

    [PropertyHave<double>("CraftSpeed", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    [PropertyHave<double>("TechSpeed", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    BoostSpeed = 5,

    [PropertyHave<double>("Efficiency", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    [PropertyHave<int>("Strength", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    [PropertyHave<int>("RechargeEvery", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    ToolSettings = 6,

    [PropertyHave<IRustyEntity>("Build", isReadOnly: DescPropertyTypeEx.IsNotReactive, isNullable: true)]
    InBuildProcess = 7,

    [PropertyHave<IRustyEntity>("Entity", isReadOnly: true)]
    [PropertyHave<int>("Count", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    HasSingleReward = 8,

    [PropertyHave<IRustyEntity>("ToEntity", isReadOnly: true)]
    [PropertyHave<IRustyEntity>("FromEntity", isReadOnly: true)]
    [PropertyHave<double>("FromRate", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    HasExchange = 9,

    [PropertyHave<ICollection<ResourceStack>>("Rewards", isReadOnly: true)]
    HasRewards = 10,

    [PropertyHave<int>("X", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    [PropertyHave<int>("Y", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    MineSize = 11,

    [PropertyHave<int>("Count", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    FarmExpansion = 12,

    [PropertyHave<IRustyEntity>("Entity", isReadOnly: true)]
    Link = 13,

    [PropertyHave<int>("Space", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    TakeSpace = 14,

    [PropertyHave<int>("Slots", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    HasCraftingQueue = 15,

    [PropertyHave<int>("Prestige", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    HasPrestige = 16,

    [PropertyHave<int>("BuildsMax", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    [PropertyHave<int>("RoadsMax", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    CitySize = 17,

    [PropertyHave<HexMask>("Mask", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    HexMask = 18,

    [PropertyHave<string>("Group", isReadOnly: DescPropertyTypeEx.IsNotReactive, isNullable: true)]
    SubGroup = 19,

    [PropertyHave<double>("Change")]
    ChanceActivate = 20,

    [PropertyHave<double>("Value")]
    //TODO: Применять на (дамаг, время, рессурс, урон прочности, защита)
    Factor = 21,

    //TODO: Клик игрока, получения рессурса, выполнение задачи, обновление прочности
    UsageScope = 22,

    [PropertyHave<ICollection<IRustyEntity>>("Links", true, false)]
    MultiLinks = 23
}

public static class DescPropertyTypeEx
{
#if REACTIVE
    internal const bool IsNotReactive = false;
#else
    internal const bool IsNotReactive = true;
#endif

    public static int AsIndex(this DescPropertyType type)
    {
        return (int)type - 1;
    }
}