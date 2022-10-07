using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using RustyDTO.Interfaces;
using RustyDTO.MutableProperties;

namespace RustyDTO;

public static partial class EntityDependencies
{
    private static readonly EntitySpecialMask[] _masks;

    private static ICollection<DescPropertyType>[] _requiredProperties;
    private static ICollection<DescPropertyType>[] _optionalProperties;
    private static ICollection<MutablePropertyType>[] _mutableProperties;

    private static readonly Memory<int> _mutableMap;
    private static readonly Memory<int> _descMap;

    private static readonly int[] _descCountByType;
    private static readonly int[] _mutableCountByType;
    
    private static readonly int _internalMutableCount;
    private static readonly int _internalDescPropertiesCount;
    
    private static readonly string[] _intAsString;

    public static int DescPropertiesCount => _internalDescPropertiesCount;
    public static int MutablePropertiesCount => _internalMutableCount;
    
    public static int EntityTypesCount { get; }

    [ThreadStatic]
    private static BitArray _entityTypeBitmask = null!;

    static EntityDependencies()
    {
        _internalDescPropertiesCount = Enum.GetValues<DescPropertyType>().Max(static type => (int)type);
        _internalMutableCount = Enum.GetValues<MutablePropertyType>().Max(static type => (int)type);

        var count = EntityTypesCount = Enum.GetValues<RustyEntityType>().Max(static type => (int)type) + 1;

        var maxCount = Math.Max(_internalDescPropertiesCount, _internalMutableCount);
        maxCount = Math.Max(count, maxCount);
        _intAsString = new string[maxCount + 1];
        for (int i = 0; i < _intAsString.Length; i++)
            _intAsString[i] = i.ToString();

        _mutableMap = new int[count * _internalMutableCount];
        _descMap = new int [count * _internalDescPropertiesCount];
        _descCountByType = new int[count];

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
        Optimize();
    }

    private static void Initialize()
    {
        var builder = EntityDependenciesBuilder.Create();
        var itemMask = EntitySpecialMask.Executable | EntitySpecialMask.IsInstance;

        var itemWithGroupMask = EntitySpecialMask.HasParent | itemMask;


        var taskMask = EntitySpecialMask.Executable;

        //Build
        builder.For(RustyEntityType.Build)
            .Mask(itemWithGroupMask | EntitySpecialMask.RequiredInDependencies)
            .Required(DescPropertyType.Payable, DescPropertyType.LongExecution, DescPropertyType.HasDependents,
                DescPropertyType.HasOwner)
            .Optional(DescPropertyType.HasPrestige, DescPropertyType.CitySize, DescPropertyType.HasCraftingQueue, 
                DescPropertyType.CraftSpeed, DescPropertyType.TechSpeed, DescPropertyType.SubGroup);

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
                DescPropertyType.InBuildProcess)
            .Optional(DescPropertyType.PveArmyImprovement, DescPropertyType.SubGroup);

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
            .Mask(itemWithGroupMask | EntitySpecialMask.RequiredInDependencies)
            .Required(DescPropertyType.HasOwner, DescPropertyType.HasDependents, DescPropertyType.HasRewards, DescPropertyType.PveEnemyPower, DescPropertyType.PveEnemyUnits, DescPropertyType.FakePayable);

        //PVE Chapter
        builder.For(RustyEntityType.PveChapter)
            .Mask(EntitySpecialMask.HasChild);

        //CityRoad
        builder.For(RustyEntityType.CityRoad)
            .Required(DescPropertyType.Payable, DescPropertyType.HasDependents)
            .Optional(DescPropertyType.HasPrestige)
            .Mask(itemMask | EntitySpecialMask.HasUniqueID);

        //Superstructure
        builder.For(RustyEntityType.Superstructure)
            .Mask(itemMask | EntitySpecialMask.HasUniqueID)
            .Required(DescPropertyType.HasDependents, DescPropertyType.Payable, DescPropertyType.HexMask)
            .Optional(DescPropertyType.HasPrestige, DescPropertyType.SubGroup);

        //Baff
        builder.For(RustyEntityType.Baff)
            .Required(DescPropertyType.HasNameKey, DescPropertyType.UsageScope)
            .Optional(DescPropertyType.Factor);

        //Gens
        builder.For(RustyEntityType.Gen)
            .Required(DescPropertyType.PurposeOfGen, DescPropertyType.HasNameKey, DescPropertyType.Rarity, DescPropertyType.ChanceActivate, DescPropertyType.IconKey)
            .Optional(DescPropertyType.HasDescKey, DescPropertyType.MultiLinks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string IntAsString(int value)
    {
        return _intAsString[value];
    }

    private static void Optimize()
    {
        var count = _internalDescPropertiesCount;
        var map = _descMap;
        
        for (int index = 0; index < EntityTypesCount; index++)
        {
            int internalCount = 0;

            var required = _requiredProperties[index];
            var optional = _optionalProperties[index];

            var slice = map.Slice(index * count, count).Span;
            for (int i = 0; i < slice.Length; i++)
            {
                int result = -1;
                
                var type = (DescPropertyType)i + 1;
                if (required.Contains(type) || optional.Contains(type))
                {
                    result = i;
                    internalCount++;
                }
                
                slice[i] = result;
            }

            _descCountByType[index] = internalCount;
        }
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

    public static ICollection<MutablePropertyType> GetMutalbeProperties(int entityTypeAsIndex)
    {
        return _mutableProperties[entityTypeAsIndex];
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

    public static EntitySpecialMask GetMask(int entityTypeAsIndex)
    {
        return _masks[entityTypeAsIndex];
    }

    public static int ResolveTypeAsIndex(int entityTypeIndex, DescPropertyType propertyType)
    {
        return _descMap.Span[entityTypeIndex * _internalDescPropertiesCount + propertyType.AsIndex()];
    }

    public static int ResolveTypeAsIndex<TProperty>() where TProperty : IDescProperty
    {
        if (_propertyTypes.TryGetValue(typeof(TProperty), out var index))
            return index;
        return -1;
    }

    public static int ResolveTypeAsIndex<TProperty>(int entityTypeIndex) where TProperty : IDescProperty
    {
        if (_propertyTypes.TryGetValue(typeof(TProperty), out var index))
            return _descMap.Span[entityTypeIndex * _internalDescPropertiesCount + index];
        return -1;
    }

    public static DescPropertyType ResolveType<TProperty>(int entityTypeIndex) where TProperty : IDescProperty
    {
        return (DescPropertyType)ResolveTypeAsIndex<TProperty>(entityTypeIndex);
    }

    public static int ResolveMutableTypeAsIndex<TProperty>(int entityTypeIndex) where TProperty : IMutableProperty
    {
        if (_propertyTypes.TryGetValue(typeof(TProperty), out var index))
            return _mutableMap.Span[entityTypeIndex * _internalMutableCount + index];
        return -1;
    }
    
    public static int ResolveMutableTypeAsIndex(int entityTypeIndex, MutablePropertyType mutablePropertyType)
    {
        return _mutableMap.Span[entityTypeIndex * _internalMutableCount + mutablePropertyType.AsIndex()];
    }

    public static void MutableBuild(int entityType, IMutablePropertyResolver resolver, out IMutableProperty[] mutableProperties)
    {
        var types = _mutableProperties[entityType];
        mutableProperties = new IMutableProperty[types.Count];

        var map = _mutableMap.Slice(entityType * _internalMutableCount, _internalMutableCount).Span;
        for (int i = 0; i < map.Length; i++)
        {
            var index = map[i];
            if(index == -1)
                continue;

            mutableProperties[index] = resolver.Resolve(i);
        }
    }

    public static void MutableBuild(int entityType, JsonElement data, IMutablePropertyResolver resolver, out IMutableProperty[] mutableProperties)
    {
        var types = _mutableProperties[entityType];
        mutableProperties = new IMutableProperty[types.Count];

        var map = _mutableMap.Slice(entityType * _internalMutableCount, _internalMutableCount).Span;
        for (int i = 0; i < map.Length; i++)
        {
            var index = map[i];
            if (index == -1)
                continue;

            if (data.TryGetProperty(IntAsString(i), out var propertyData))
                mutableProperties[index] = resolver.Resolve(i, propertyData);
            else
                mutableProperties[index] = resolver.Resolve(i);
        }
    }

    public static void MutableBuild(int entityType, JsonElement data, IMutablePropertyResolver resolver, out IMutableProperty[] mutableProperties, out IRustyEntity entity)
    {
        var types = _mutableProperties[entityType];
        mutableProperties = new IMutableProperty[types.Count];

        var map = _mutableMap.Slice(entityType * _internalMutableCount, _internalMutableCount).Span;
        for (int i = 0; i < map.Length; i++)
        {
            var index = map[i];
            if (index == -1)
                continue;

            if (data.TryGetProperty(IntAsString(i), out var propertyData))
                mutableProperties[index] = resolver.Resolve(i, propertyData);
            else
                mutableProperties[index] = resolver.Resolve(i);
        }

        var archetypeIndex = ResolveMutableTypeAsIndex<IMutableOwnerArchetype>(entityType);
        entity = ((IMutableOwnerArchetype)mutableProperties[archetypeIndex]).Entity;
    }
    
    public static int GetDescPropertiesCountByType(int entityType)
    {
        return _descCountByType[entityType];
    }

    public static void DescBuild(int entityType, LazyDescProperty[] lazy, ref IDescProperty[] properties)
    {
        var map = _descMap.Slice(entityType * _internalDescPropertiesCount, _internalDescPropertiesCount).Span;
        
        foreach (var lazyRustyProperty in lazy)
        {
            var typeIndex = lazyRustyProperty.Type.AsIndex();
            var index = map[typeIndex];
            if (index == -1)
                continue;

            properties[index] = lazyRustyProperty.Value;
        }
    }

    internal ref struct EntityDependenciesBuilder
    {

        internal static EntityDependenciesBuilder Create()
        {
            return new EntityDependenciesBuilder();
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
                _masks[_index] = mask;
                return this;
            }

            public EntityDependenceBuilder Required(params DescPropertyType[] types)
            {
                _requiredProperties[_index] = Pack(types);
                return this;
            }

            public EntityDependenceBuilder Optional(params DescPropertyType[] types)
            {
                _optionalProperties[_index] = Pack(types);
                return this;
            }

            public EntityDependenceBuilder Mutable(params MutablePropertyType[] types)
            {
                var hash = new HashSet<MutablePropertyType>(types);
                _mutableProperties[_index] = hash;

                var slice = _mutableMap.Slice(_index * _internalMutableCount, _internalMutableCount).Span;
                for (int i = 0; i < slice.Length; i++)
                    slice[i] = hash.Contains((MutablePropertyType) (i + 1)) ? i : -1;

                return this;
            }

            private ICollection<DescPropertyType> Pack(DescPropertyType[] types)
            {
                return types.Length < 4 ? types : new HashSet<DescPropertyType>(types);
            }
        }
    }
}

internal record struct EntityDependenceRecord(EntitySpecialMask Mask, DescPropertyType[] Required, DescPropertyType[] Optional);