
using System.Collections;

namespace RustyDTO;

public static partial class EntityDependencies
{
    private static EntitySpecialMask[] _masks;

    private static ICollection<EntityPropertyType>[] _requiredProperties;
    private static ICollection<EntityPropertyType>[] _optionalProperties;

    public static int PropertiesCount { get; }
    public static int EntityTypesCount { get; }


    [ThreadStatic]
    private static BitArray _entityTypeBitmask = new BitArray(EntityTypesCount);

    static EntityDependencies()
    {
        PropertiesCount = Enum.GetValues<EntityPropertyType>().Max(static type => (int)type) + 1;
        var count = EntityTypesCount = Enum.GetValues<RustyEntityType>().Max(static type => (int)type) + 1;

        _masks = new EntitySpecialMask[count];
        _requiredProperties = new ICollection<EntityPropertyType>[count];
        _optionalProperties = new ICollection<EntityPropertyType>[count];

        for (int i = 0; i < count; i++)
        {
            _requiredProperties[i] = Array.Empty<EntityPropertyType>();
            _optionalProperties[i] = Array.Empty<EntityPropertyType>();
        }

        Initialize();
    }


    private static void Initialize()
    {
        var builder = EntityDependenciesBuilder.Create(_masks, _requiredProperties, _optionalProperties);

        var itemMask = EntitySpecialMask.Executable | EntitySpecialMask.IsInstance;

        var itemWithGroupMask = EntitySpecialMask.HasParent | itemMask;


        var taskMask = EntitySpecialMask.Executable | EntitySpecialMask.IsTask;

        //Build
        builder.For(RustyEntityType.Build)
            .Mask(itemWithGroupMask | EntitySpecialMask.RequiredInDependencies)
            .Required(EntityPropertyType.Payable, EntityPropertyType.LongExecution, EntityPropertyType.HasDependents,
                EntityPropertyType.HasOwner, EntityPropertyType.BoostSpeed)
            .Optional(EntityPropertyType.MineSize);

        //Tool
        builder.For(RustyEntityType.Tool)
            .Mask(itemWithGroupMask)
            .Required(EntityPropertyType.Payable, EntityPropertyType.LongExecution, EntityPropertyType.HasDependents,
                EntityPropertyType.HasOwner, EntityPropertyType.ToolSettings);

        //Tech
        builder.For(RustyEntityType.Tech)
            .Mask(itemMask | EntitySpecialMask.RequiredInDependencies)
            .Required(EntityPropertyType.Payable, EntityPropertyType.LongExecution, EntityPropertyType.HasDependents,
                EntityPropertyType.InBuildProcess);

        //UniqueBuild, UniqueTool, ResourceGroup
        builder.For(RustyEntityType.UniqueBuild)
            .Mask(EntitySpecialMask.HasChild);
        builder.For(RustyEntityType.UniqueTool)
            .Mask(EntitySpecialMask.HasChild);
        builder.For(RustyEntityType.ResourceGroup)
            .Mask(EntitySpecialMask.HasChild);

        //Craft
        builder.For(RustyEntityType.Craft)
            .Mask(taskMask)
            .Required(EntityPropertyType.InBuildProcess, EntityPropertyType.Payable, EntityPropertyType.LongExecution,
                EntityPropertyType.HasSingleReward, EntityPropertyType.HasDependents);

        //Resource
        builder.For(RustyEntityType.Resource)
            .Mask(EntitySpecialMask.Countable | EntitySpecialMask.IsInstance)
            .Required(EntityPropertyType.HasOwner);

        //Plant
        builder.For(RustyEntityType.Plant)
            .Mask(taskMask)
            .Required(EntityPropertyType.LongExecution, EntityPropertyType.HasSingleReward, EntityPropertyType.HasDependents);


        //Exchage
        builder.For(RustyEntityType.Exchage)
            .Mask(taskMask)
            .Required(EntityPropertyType.HasExchange);


        //FarmObstacleClearing
        builder.For(RustyEntityType.FarmObstacleClearing)
            .Mask(EntitySpecialMask.Executable | EntitySpecialMask.IsInstance)
            .Required(EntityPropertyType.Payable, EntityPropertyType.FarmExpansion);

        //PVE
        builder.For(RustyEntityType.PVE)
            .Mask(itemMask | EntitySpecialMask.RequiredInDependencies)
            .Required(EntityPropertyType.Payable, EntityPropertyType.HasDependents, EntityPropertyType.HasRewards);

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

    public static ICollection<EntityPropertyType> GetRequiredProperties(RustyEntityType entityType)
    {
        return _requiredProperties[(int) entityType];
    }

    public static ICollection<EntityPropertyType> GetOptionalProperties(RustyEntityType entityType)
    {
        return _optionalProperties[(int)entityType];
    }

    public static bool HasSpectialMask(RustyEntityType entityType, EntitySpecialMask condition)
    {
        return (_masks[(int)entityType] & condition) != 0;
    }
}

internal ref struct EntityDependenciesBuilder
{
    private EntitySpecialMask[] _masks;

    private ICollection<EntityPropertyType>[] _requiredProperties;
    private ICollection<EntityPropertyType>[] _optionalProperties;

    private EntityDependenciesBuilder(EntitySpecialMask[] masks, ICollection<EntityPropertyType>[] requiredProperties, ICollection<EntityPropertyType>[] optionalProperties)
    {
        _masks = masks;
        _requiredProperties = requiredProperties;
        _optionalProperties = optionalProperties;
    }

    internal static EntityDependenciesBuilder Create(EntitySpecialMask[] masks, ICollection<EntityPropertyType>[] requiredProperties, ICollection<EntityPropertyType>[] optionalProperties)
    {
        return new EntityDependenciesBuilder(masks, requiredProperties, optionalProperties);
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

        public EntityDependenceBuilder Required(params EntityPropertyType[] types)
        {
            _builder._requiredProperties[_index] = Pack(types);
            return this;
        }

        public EntityDependenceBuilder Optional(params EntityPropertyType[] types)
        {
            _builder._optionalProperties[_index] = Pack(types);
            return this;
        }

        private ICollection<EntityPropertyType> Pack(EntityPropertyType[] types)
        {
            return types.Length < 4 ? types : new HashSet<EntityPropertyType>(types);
        }
    }
}

internal record struct EntityDependenceRecord(EntitySpecialMask Mask, EntityPropertyType[] Required, EntityPropertyType[] Optional);