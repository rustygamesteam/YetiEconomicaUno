using RustyDTO;
using RustyDTO.Interfaces;
using RustyDTO.Supports;

namespace RustyEngine.Internal;

internal class Entity : IRustyEntity
{
    public EntityID ID { get; }
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

    public Entity(EntityID id, int type, string? displayName, Span<DescPropertyType> propertyTypes, Span<LazyDescProperty> lazyProperties, Span<MutablePropertyType> mutableProperties, out IDisposable onInitalize)
    {
        ID = id;
        Type = (RustyEntityType)type;
        TypeAsIndex = type;
        DisplayName = displayName;

        var hashSet = new HashSet<MutablePropertyType>(mutableProperties.Length);
        foreach (var mutableProperty in mutableProperties)
            hashSet.Add(mutableProperty);
        MutablePropertyTypes = hashSet;

        _properyTypes = propertyTypes.ToArray();
        _properties = new IDescProperty[EntityDependencies.DescPropertiesCount];

        onInitalize = new CompleteInitliaze(_properties, lazyProperties.ToArray());
    }

    private struct CompleteInitliaze : IDisposable
    {
        private IDescProperty[] _properties;
        private LazyDescProperty[] _lazy;

        public CompleteInitliaze(IDescProperty[] properties, LazyDescProperty[] lazyRustyProperties)
        {
            _properties = properties;
            _lazy = lazyRustyProperties;
        }

        public void Dispose()
        {
            foreach (var lazyRustyProperty in _lazy)
                _properties[lazyRustyProperty.Type.AsIndex()] = lazyRustyProperty.Value;
        }
    }

    public bool Equals(IRustyEntity? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return ID == other.ID;
    }

    public override int GetHashCode()
    {
        return ID.GetHashCode();
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
        return _properties[type.AsIndex()] is not null;
    }

    public bool TryGetProperty<TProperty>(DescPropertyType type, out TProperty property) where TProperty : IDescProperty
    {
        var raw = _properties[type.AsIndex()];
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
        return _properties[type.AsIndex()];
    }

    public bool TryGetProperty<TProperty>(out TProperty property) where TProperty : IDescProperty
    {
        var index = EntityDependencies.ResolveTypeAsIndex<TProperty>();
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
        var index = EntityDependencies.ResolveTypeAsIndex<TProperty>();
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