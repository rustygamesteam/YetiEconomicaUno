using System.Collections;
using RustyDTO.Interfaces;

namespace RustyDTO;

public static partial class EntityDependencies
{
    private static EntitySpecialMask[] _masks;

    private static ICollection<DescPropertyType>[] _requiredProperties;
    private static ICollection<DescPropertyType>[] _optionalProperties;
    private static ICollection<MutablePropertyType>[] _mutableProperties;

    public static int DescPropertiesCount { get; }
    public static int MutablePropertiesCount { get; }
    public static int EntityTypesCount { get; }


    [ThreadStatic]
    private static BitArray _entityTypeBitmask = new BitArray(EntityTypesCount);

    static EntityDependencies()
    {
        DescPropertiesCount = Enum.GetValues<DescPropertyType>().Max(static type => (int)type);
        MutablePropertiesCount = Enum.GetValues<MutablePropertyType>().Max(static type => (int)type);

        var count = EntityTypesCount = Enum.GetValues<RustyEntityType>().Max(static type => (int)type) + 1;

        _masks = new EntitySpecialMask[count];
        _requiredProperties = new ICollection<DescPropertyType>[count];
        _optionalProperties = new ICollection<DescPropertyType>[count];
        _mutableProperties = new ICollection<MutablePropertyType>[count];

        for (int i = 0; i < count; i++)
        {
            _requiredProperties[i] = Array.Empty<DescPropertyType>();
            _optionalProperties[i] = Array.Empty<DescPropertyType>();
            _mutableProperties[i] = Array.Empty<MutablePropertyType>();
        }

        Initialize();
    }

    private static void Initialize()
    {
        var builder = EntityDependenciesBuilder.Create(_masks, _requiredProperties, _optionalProperties, _mutableProperties);
        var itemMask = EntitySpecialMask.Executable | EntitySpecialMask.IsInstance;

        var itemWithGroupMask = EntitySpecialMask.HasParent | itemMask;


        var taskMask = EntitySpecialMask.Executable;

        //Build
        builder.For(RustyEntityType.Build)
            .Mask(itemWithGroupMask | EntitySpecialMask.RequiredInDependencies)
            .Required(DescPropertyType.Payable, DescPropertyType.LongExecution, DescPropertyType.HasDependents,
                DescPropertyType.HasOwner, DescPropertyType.BoostSpeed)
            .Optional(DescPropertyType.HasPrestige, DescPropertyType.CitySize, DescPropertyType.HasCraftingQueue);

        //Tool
        builder.For(RustyEntityType.Tool)
            .Mask(itemWithGroupMask)
            .Required(DescPropertyType.Payable, DescPropertyType.LongExecution, DescPropertyType.HasDependents,
                DescPropertyType.HasOwner, DescPropertyType.ToolSettings)
            .Optional(DescPropertyType.MineSize);

        //Tech
        builder.For(RustyEntityType.Tech)
            .Mask(itemMask | EntitySpecialMask.RequiredInDependencies)
            .Required(DescPropertyType.Payable, DescPropertyType.LongExecution, DescPropertyType.HasDependents,
                DescPropertyType.InBuildProcess);

        //UniqueBuild, UniqueTool, ResourceGroup
        builder.For(RustyEntityType.UniqueBuild)
            .Mask(EntitySpecialMask.HasChild)
            .Mutable(MutablePropertyType.Position2D, MutablePropertyType.Manager, MutablePropertyType.UsedInstance);
        builder.For(RustyEntityType.UniqueTool)
            .Mask(EntitySpecialMask.HasChild)
            .Mutable(MutablePropertyType.UsedInstance);
        builder.For(RustyEntityType.ResourceGroup)
            .Mask(EntitySpecialMask.HasChild);

        //CraftTask
        builder.For(RustyEntityType.CraftTask)
            .Mask(taskMask)
            .Required(DescPropertyType.InBuildProcess, DescPropertyType.Payable, DescPropertyType.LongExecution,
                DescPropertyType.HasSingleReward, DescPropertyType.HasDependents)
            .Optional(DescPropertyType.SubGroup);

        //Resource
        builder.For(RustyEntityType.Resource)
            .Mask(EntitySpecialMask.IsInstance)
            .Required(DescPropertyType.HasOwner)
            .Mutable(MutablePropertyType.Count);

        //PlantTask
        builder.For(RustyEntityType.PlantTask)
            .Mask(taskMask)
            .Required(DescPropertyType.LongExecution, DescPropertyType.HasSingleReward, DescPropertyType.HasDependents, DescPropertyType.TakeSpace);


        //ExchageTask
        builder.For(RustyEntityType.ExchageTask)
            .Mask(taskMask)
            .Required(DescPropertyType.HasExchange);


        //FarmObstacleClearing
        builder.For(RustyEntityType.FarmObstacleClearing)
            .Mask(EntitySpecialMask.Executable | EntitySpecialMask.IsInstance)
            .Required(DescPropertyType.Payable, DescPropertyType.FarmExpansion);

        //PVE
        builder.For(RustyEntityType.PVE)
            .Mask(itemMask | EntitySpecialMask.RequiredInDependencies)
            .Required(DescPropertyType.Payable, DescPropertyType.HasDependents, DescPropertyType.HasRewards);

        //CityRoad
        builder.For(RustyEntityType.CityRoad)
            .Required(DescPropertyType.Payable, DescPropertyType.HasDependents)
            .Optional(DescPropertyType.HasPrestige)
            .Mask(itemMask | EntitySpecialMask.HasUniqueID);

        //SuperstructureGroup
        builder.For(RustyEntityType.SuperstructureGroup)
            .Mask(EntitySpecialMask.HasChild);

        //Superstructure
        builder.For(RustyEntityType.Superstructure)
            .Mask(itemWithGroupMask)
            .Required(DescPropertyType.Payable, DescPropertyType.HasDependents, DescPropertyType.HasOwner)
            .Optional(DescPropertyType.HasPrestige);

    }

    public static BitArray ToBitmask(ReadOnlySpan<RustyEntityType> types)
    {
        _entityTypeBitmask ??= new BitArray(EntityTypesCount);

        var helper = _entityTypeBitmask;
        helper.SetAll(false);

        InternalFill(helper, types);

        return helper;
    }

    public static BitArray ToConstantBitmask(ReadOnlySpan<RustyEntityType> types)
    {
        var helper = new BitArray(EntityTypesCount);

        InternalFill(helper, types);

        return helper;
    }

    private static void InternalFill(BitArray array, ReadOnlySpan<RustyEntityType> values)
    {
        for (int i = 0; i < values.Length; i++)
            array.Set((int)values[i], true);
    }

    public static ICollection<DescPropertyType> GetRequiredProperties(RustyEntityType entityType)
    {
        return _requiredProperties[(int) entityType];
    }

    public static ICollection<DescPropertyType> GetRequiredProperties(int entityTypeAsIndex)
    {
        return _requiredProperties[entityTypeAsIndex];
    }

    public static ICollection<DescPropertyType> GetOptionalProperties(RustyEntityType entityType)
    {
        return _optionalProperties[(int)entityType];
    }

    public static ICollection<DescPropertyType> GetOptionalProperties(int entityTypeAsIndex)
    {
        return _optionalProperties[entityTypeAsIndex];
    }

    public static ICollection<MutablePropertyType> GetMutalbeProperties(RustyEntityType entityType)
    {
        return _mutableProperties[(int)entityType];
    }

    public static bool HasSpectialMask(RustyEntityType entityType, EntitySpecialMask condition)
    {
        return (_masks[(int)entityType] & condition) != 0;
    }

    public static bool HasSpectialMask(int entityTypeAsIndex, EntitySpecialMask condition)
    {
        return (_masks[entityTypeAsIndex] & condition) != 0;
    }

    public static int ResolveTypeAsIndex<TProperty>() where TProperty : IDescProperty
    {
        if (_propertyTypes.TryGetValue(typeof(TProperty), out var index))
            return index;
        return -1;
    }

    public static DescPropertyType ResolveType<TProperty>() where TProperty : IDescProperty
    {
        return (DescPropertyType)ResolveTypeAsIndex<TProperty>();
    }

    public static int ResolveMutableTypeAsIndex<TProperty>() where TProperty : IMutableProperty
    {
        if (_propertyTypes.TryGetValue(typeof(TProperty), out var index))
            return index;
        return -1;
    }
}

internal ref struct EntityDependenciesBuilder
{
    private EntitySpecialMask[] _masks;

    private ICollection<DescPropertyType>[] _requiredProperties;
    private ICollection<DescPropertyType>[] _optionalProperties;
    private ICollection<MutablePropertyType>[] _mutableProperties;

    private EntityDependenciesBuilder(EntitySpecialMask[] masks, ICollection<DescPropertyType>[] requiredProperties, ICollection<DescPropertyType>[] optionalProperties, ICollection<MutablePropertyType>[] mutableProperties)
    {
        _masks = masks;
        _requiredProperties = requiredProperties;
        _optionalProperties = optionalProperties;
        _mutableProperties = mutableProperties;
    }

    internal static EntityDependenciesBuilder Create(EntitySpecialMask[] masks, ICollection<DescPropertyType>[] requiredProperties, ICollection<DescPropertyType>[] optionalProperties, ICollection<MutablePropertyType>[] mutableProperties)
    {
        return new EntityDependenciesBuilder(masks, requiredProperties, optionalProperties, mutableProperties);
    }

    public EntityDependenceBuilder For(RustyEntityType entity)
    {
        var index = (int)entity;
        return new EntityDependenceBuilder(this, index);
    }

    internal ref struct EntityDependenceBuilder
    {
        private EntityDependenciesBuilder _builder;
        private int _index;

        public EntityDependenceBuilder(EntityDependenciesBuilder builder, int index)
        {
            _builder = builder;
            _index = index;
        }

        public EntityDependenceBuilder Mask(EntitySpecialMask mask)
        {
            _builder._masks[_index] = mask;
            return this;
        }

        public EntityDependenceBuilder Required(params DescPropertyType[] types)
        {
            _builder._requiredProperties[_index] = Pack(types);
            return this;
        }

        public EntityDependenceBuilder Optional(params DescPropertyType[] types)
        {
            _builder._optionalProperties[_index] = Pack(types);
            return this;
        }

        public EntityDependenceBuilder Mutable(params MutablePropertyType[] types)
        {
            _builder._mutableProperties[_index] = new HashSet<MutablePropertyType>(types);
            return this;
        }

        private ICollection<DescPropertyType> Pack(DescPropertyType[] types)
        {
            return types.Length < 4 ? types : new HashSet<DescPropertyType>(types);
        }
    }
}

internal record struct EntityDependenceRecord(EntitySpecialMask Mask, DescPropertyType[] Required, DescPropertyType[] Optional);