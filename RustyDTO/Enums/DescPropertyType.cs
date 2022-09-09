﻿using RustyDTO.Generator;
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

    [PropertyHave<IRustyEntity>("Required", isReadOnly: DescPropertyTypeEx.IsNotReactive, isNullable: true, defaultValue: -1)]
    [PropertyHave<IRustyEntity>("VisibleAfter", isReadOnly: DescPropertyTypeEx.IsNotReactive, isNullable: true, defaultValue: -1)]
    HasDependents = 2,

    [PropertyHave<ICollection<ResourceStack>>("Price", isReadOnly: true)]
    Payable = 3,

    [PropertyHave<int>("Duration", isReadOnly: DescPropertyTypeEx.IsNotReactive, defaultValue: 60)]
    LongExecution = 4,

    //5 is FREE

    //[PropertyHave<double>("CraftSpeed", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    //[PropertyHave<double>("TechSpeed", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    //BoostSpeed = 5,

    [PropertyHave<double>("Efficiency", isReadOnly: DescPropertyTypeEx.IsNotReactive, defaultValue: 1)]
    [PropertyHave<int>("Strength", isReadOnly: DescPropertyTypeEx.IsNotReactive, defaultValue: 100)]
    [PropertyHave<int>("RechargeEvery", isReadOnly: DescPropertyTypeEx.IsNotReactive, defaultValue: 14)]
    ToolSettings = 6,

    [PropertyHave<IRustyEntity>("Build", isReadOnly: DescPropertyTypeEx.IsNotReactive, isNullable: true)]
    InBuildProcess = 7,

    [PropertyHave<IRustyEntity>("Entity", isReadOnly: true)]
    [PropertyHave<int>("Count", isReadOnly: DescPropertyTypeEx.IsNotReactive, defaultValue: 1)]
    HasSingleReward = 8,

    [PropertyHave<IRustyEntity>("ToEntity", isReadOnly: true)]
    [PropertyHave<IRustyEntity>("FromEntity", isReadOnly: true)]
    [PropertyHave<double>("FromRate", isReadOnly: DescPropertyTypeEx.IsNotReactive, defaultValue: 1)]
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

    [PropertyHave<int>("Space", isReadOnly: DescPropertyTypeEx.IsNotReactive, defaultValue: 1)]
    TakeSpace = 14,

    [PropertyHave<int>("Slots", isReadOnly: DescPropertyTypeEx.IsNotReactive, defaultValue: 1)]
    HasCraftingQueue = 15,

    [PropertyHave<int>("Prestige", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    HasPrestige = 16,

    [PropertyHave<int>("BuildsMax", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    [PropertyHave<int>("RoadsMax", isReadOnly: DescPropertyTypeEx.IsNotReactive)]
    CitySize = 17,

    [PropertyHave<HexMaskFlags>("Mask", isReadOnly: DescPropertyTypeEx.IsNotReactive, defaultValue: HexMaskFlags.All)]
    HexMask = 18,

    [PropertyHave<string>("Group", isReadOnly: DescPropertyTypeEx.IsNotReactive, isNullable: true)]
    SubGroup = 19,

    [PropertyHave<double>("Change", isReadOnly: DescPropertyTypeEx.IsNotReactive, defaultValue: 1d)]
    ChanceActivate = 20,

    [PropertyHave<double>("Value", isReadOnly: DescPropertyTypeEx.IsNotReactive, defaultValue: 1d)]
    [PropertyHave<FactorTargets>("Target", isReadOnly: DescPropertyTypeEx.IsNotReactive, defaultValue: FactorTargets.Damage)]
    Factor = 21,

    [PropertyHave<GameScopes>("Scopes", isReadOnly: DescPropertyTypeEx.IsNotReactive, defaultValue: GameScopes.EntityExecute)]
    UsageScope = 22,

    [PropertyHave<ICollection<IRustyEntity>>("Links", true, false)]
    MultiLinks = 23,

    [PropertyHave<RarityGrades>("Value", isReadOnly: DescPropertyTypeEx.IsNotReactive, defaultValue: RarityGrades.Normal)]
    Rarity = 24,

    [PropertyHave<string>("Value", isReadOnly: DescPropertyTypeEx.IsNotReactive, isNullable: true)]
    HasNameKey = 25,

    [PropertyHave<string>("Value", isReadOnly: DescPropertyTypeEx.IsNotReactive, isNullable: true)]
    HasDescKey = 26,

    [PropertyHave<string>("Value", isReadOnly: DescPropertyTypeEx.IsNotReactive, isNullable: true)]
    Color = 27,

    [PropertyHave<string>("Value", isReadOnly: DescPropertyTypeEx.IsNotReactive, isNullable: true)]
    IconKey = 28,

    [PropertyHave<string>("Value", isReadOnly: DescPropertyTypeEx.IsNotReactive, isNullable: true)]
    SpriteKey = 29,

    [PropertyHave<PurposeOfGensEnum>("Purpose", isReadOnly: DescPropertyTypeEx.IsNotReactive, defaultValue: PurposeOfGensEnum.Body)]
    PurposeOfGen = 30,

    [PropertyHave<double>("Factor", isReadOnly: DescPropertyTypeEx.IsNotReactive, defaultValue: 1d)]
    CraftSpeed = 31,

    [PropertyHave<double>("Factor", isReadOnly: DescPropertyTypeEx.IsNotReactive, defaultValue: 1d)]
    TechSpeed = 32
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