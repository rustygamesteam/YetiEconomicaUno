using RustyDTO;
using RustyDTO.Interfaces;
using RustyDTO.Supports;

namespace RustyEngine.Internal;

internal class Entity : IRustyEntity
{
    public EntityID ID { get; }
    public int Index { get; }
    public RustyEntityType Type { get; }
    public int TypeAsIndex { get; }

    public string? DisplayName
    {
        get;
#if REACTIVE
        set;
#endif
    }
    public string? DisplayNameWithTear { get; }
    public string? FullName { get; }

    public IReadOnlySet<MutablePropertyType> MutablePropertyTypes { get; }

    private DescPropertyType[] _properyTypes;
    private IDescProperty[] _properties;

    public IEnumerable<DescPropertyType> DescProperties => _properyTypes;
    
    public Entity(int index, int type, string? displayName, Span<DescPropertyType> propertyTypes, Span<LazyDescProperty> lazyProperties, out IDisposable onInitialize)
    {
        ID = EntityID.CreateByDB(index);
        Index = index;
        Type = (RustyEntityType)type;
        TypeAsIndex = type;
        DisplayName = displayName;

        var mutables = EntityDependencies.GetMutalbeProperties(TypeAsIndex);
        if (mutables is IReadOnlySet<MutablePropertyType> set)
            MutablePropertyTypes = set;
        else
            MutablePropertyTypes = new HashSet<MutablePropertyType>(mutables);

        _properyTypes = propertyTypes.ToArray();
        _properties = new IDescProperty[EntityDependencies.GetDescPropertiesCountByType(TypeAsIndex)];

        onInitialize = new CompleteInitialize(TypeAsIndex, _properties, lazyProperties.ToArray());
    }

    private struct CompleteInitialize : IDisposable
    {
        private int _typeAsIndex;
        private IDescProperty[] _properties;
        private LazyDescProperty[] _lazy;

        public CompleteInitialize(int typeAsIndex, IDescProperty[] properties, LazyDescProperty[] lazyRustyProperties)
        {
            _typeAsIndex = typeAsIndex;
            _properties = properties;
            _lazy = lazyRustyProperties;
        }

        public void Dispose()
        {
            EntityDependencies.DescBuild(_typeAsIndex, _lazy, ref _properties);
        }
    }

    public bool Equals(IRustyEntity? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Index == other.Index;
    }

    public override int GetHashCode()
    {
        return Index.GetHashCode();
    }

    public bool HasSpecialMask(EntitySpecialMask condition)
    {
        return EntityDependencies.HasSpectialMask(TypeAsIndex, condition);
    }

    public bool IsPropertyRequired(DescPropertyType propertyType)
    {
        return EntityDependencies.GetRequiredProperties(TypeAsIndex).Contains(propertyType);
    }

    public bool HasMutable(MutablePropertyType type)
    {
        return MutablePropertyTypes.Contains(type);
    }

    public bool HasProperty(DescPropertyType type)
    {
        var index = EntityDependencies.ResolveTypeAsIndex(TypeAsIndex, type);
        return index != -1 && _properties[index] is not null;
    }

    public bool TryGetProperty<TProperty>(DescPropertyType type, out TProperty property) where TProperty : IDescProperty
    {
        var index = EntityDependencies.ResolveTypeAsIndex(TypeAsIndex, type);
        if (index == -1)
        {
            property = default!;
            return false;
        }
        
        var raw = _properties[index];
        if (raw is null)
        {
            property = default!;
            return false;
        }

        property = (TProperty)raw;
        return true;
    }

    public IDescProperty GetDescUnsafe(DescPropertyType type)
    {
        var index = EntityDependencies.ResolveTypeAsIndex(TypeAsIndex, type);
        return _properties[index];
    }

    public bool TryGetProperty<TProperty>(out TProperty property) where TProperty : IDescProperty
    {
        var index = EntityDependencies.ResolveTypeAsIndex<TProperty>(TypeAsIndex);
        if (index == -1)
        {
            property = default!;
            return false;
        }
        
        var raw = _properties[index];
        if (raw is null)
        {
            property = default!;
            return false;
        }

        property = (TProperty)raw;
        return true;
    }

    public TProperty GetDescUnsafe<TProperty>() where TProperty : IDescProperty
    {
        var index = EntityDependencies.ResolveTypeAsIndex<TProperty>(TypeAsIndex);
        return (TProperty)_properties[index];
    }
}

public static class EntityEx
{
    public static int AsIndex(this RustyEntityType type)
    {
        return (int)type;
    }
}