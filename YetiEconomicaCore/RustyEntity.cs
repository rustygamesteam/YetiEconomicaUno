using System.Diagnostics;
using System.Reactive.Subjects;
using DynamicData;
using LiteDB;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;
using RustyDTO.Supports;
using YetiEconomicaCore.Services;

namespace YetiEconomicaCore;

[DebuggerDisplay("ID: {ID}; Type: {Type}; Name: {FullName}")]
internal class RustyEntity : ReactiveObject, IEquatable<RustyEntity>, IReactiveRustyEntity
{
    public EntityID ID { get; }    
    public int Index { get; }

    private readonly int _indexType;
    public RustyEntityType Type { get; }
    public int TypeAsIndex => _indexType;

    static RustyEntity()
    {
        BsonMapper.Global.RegisterType(
            serialize: entity => new BsonValue(entity.Index),
            deserialize: value => (RustyEntity)RustyEntityService.Instance.GetEntity(value.AsInt32));
    }

    public ISubject<ItemChange<(DescPropertyType Type, IDescProperty Property)>> PropertiesChangedObserver { get; } = new Subject<ItemChange<(DescPropertyType, IDescProperty)>>();

    [Reactive]
    public string? DisplayName { get; set; }

    [BsonIgnore]
    public string? DisplayNameWithTear
    {
        get
        {
            if (DisplayName is null)
            {
                if (Type == RustyEntityType.FarmObstacleClearing)
                    return "Plant obstacle";

                string? name = "Unknown";
                if (TryGetProperty(out IHasSingleReward reward))
                    name = reward.Entity.DisplayNameWithTear;
                else if (TryGetProperty(out IHasExchange exchange))
                    name = exchange.FromEntity.DisplayNameWithTear;
                else if (TryGetProperty(out ILink link))
                    name = link.Entity.DisplayNameWithTear;

                return name;
            }
            return TryGetProperty<IHasOwner>(out var owner) ? $"{DisplayName} [{owner.Tear + 1}]" : DisplayName;
        }
    }

    [BsonIgnore]
    public string? FullName
    {
        get
        {
            if (DisplayName is null)
            {
                if (Type == RustyEntityType.FarmObstacleClearing)
                    return $"Plant obstacle [{PlantsService.Instance.GetObstacleIndex(this) + 1}]";

                string? name = "Unknown";
                if (TryGetProperty(out IHasSingleReward reward))
                    name = reward.Entity.FullName;
                else if (TryGetProperty(out IHasExchange exchange))
                    name = exchange.FromEntity.FullName;
                else if (TryGetProperty(out ILink link))
                    name = link.Entity.FullName;

                return name;
            }
            if (TryGetProperty<IHasOwner>(out var property))
                return $"{property.Owner.DisplayName}/{DisplayName} [{property.Tear + 1}]";

            return DisplayName;
        }
    }

    public IReadOnlySet<MutablePropertyType> MutablePropertyTypes { get; }

    private LazyDescProperty[] _properties;
    public IEnumerable<DescPropertyType> DescProperties => _properties.Where(static property => property.IsValid).Select(static property => property.Type);

    public bool IsPropertyRequired(DescPropertyType propertyType)
    {
        return EntityDependencies.GetRequiredProperties(Type).Contains(propertyType);
    }

    public bool HasMutable(MutablePropertyType type)
    {
        return MutablePropertyTypes.Contains(type);
    }

    public bool HasSpecialMask(EntitySpecialMask condition)
    {
        return EntityDependencies.HasSpectialMask(Type, condition);
    }

    public IEnumerable<DescPropertyType> GetExpandableProperies()
    {
        var optional = EntityDependencies.GetOptionalProperties(Type);
        if(optional is null)
            yield break;
        
        foreach (var propertyType in optional)
        {
            if(HasProperty(propertyType))
                continue;
            yield return propertyType;
        }
    }

    public RustyEntity(int index, RustyEntityType type, string? displayName, IEnumerable<DescPropertyType> properties, IEnumerable<MutablePropertyType> mutablePropertyTypes)
    {
        ID = EntityID.CreateByDB(index);
        Index = index;
        _indexType = (int)type;
        Type = type;
        DisplayName = displayName;
        MutablePropertyTypes = new HashSet<MutablePropertyType>(mutablePropertyTypes);
        _properties = new LazyDescProperty[EntityDependencies.DescPropertiesCount];
        InjectProperties(properties);
    }

    public bool TryAddProperty(DescPropertyType type)
    {
        var properties = RustyEntityService.Instance.Properties;
        if (properties.TryResolve(Index, type, out var lazy) && properties.GetProperties(Index).TryDefaultAttach(type))
        {
            var propertyIndex = type.AsIndex();
            _properties[propertyIndex] = lazy;
            this.RaisePropertyChanged(nameof(DescProperties));
            PropertiesChangedObserver.OnNext(new ItemChange<(DescPropertyType Type, IDescProperty)>(ListChangeReason.Add, (type, _properties[propertyIndex].Value), -1));
            return true;
        }

        return false;
    }

    internal bool TryRemoveProperty(DescPropertyType type)
    {
        ref var property = ref _properties[type.AsIndex()];
        if (property.IsValid)
        {
            var value = property.Value;
            property = default;

            RustyEntityService.Instance.Properties.GetProperties(Index).Detach(type);
            this.RaisePropertyChanged(nameof(DescProperties));
            PropertiesChangedObserver.OnNext(new ItemChange<(DescPropertyType Type, IDescProperty)>(ListChangeReason.Remove, (type, value), -1));
        }

        return false;
    }

    internal void InjectProperty(DescPropertyType propertyType)
    {
        var service = RustyEntityService.Instance;
        var index = Index;

        var propertyIndex = propertyType.AsIndex();

        if (!service.Properties.TryResolve(index, propertyType, out var lazy))
        {
            lazy = propertyType switch
            {
                DescPropertyType.HasOwner => new LazyDescProperty(propertyType, new ResolveByFunc(() => service.ItemsOfGroup[index])),
                DescPropertyType.Payable => new LazyDescProperty(propertyType, new ResolveByFunc(() => service.GetPrices(index))),
                DescPropertyType.FakePayable => new LazyDescProperty(propertyType, new ResolveByFunc(() => service.GetFakePrices(index))),
                DescPropertyType.HasRewards => new LazyDescProperty(propertyType, new ResolveByFunc(() => service.GetRewards(index))),

                _ => throw new NotImplementedException(),
            };
        }

        var isValidLast = _properties[propertyIndex].IsValid;
        _properties[propertyIndex] = lazy;
        if(!isValidLast)
            this.RaisePropertyChanged(nameof(DescProperties));
    }

    private void InjectProperties(IEnumerable<DescPropertyType> properties)
    {
        foreach (var propertyType in properties)
            InjectProperty(propertyType);
    }

    public bool HasProperty(DescPropertyType type) => _properties[type.AsIndex()].IsValid;
    public bool TryGetProperty<TProperty>(out TProperty property) where TProperty : IDescProperty
    {
        var index = EntityDependencies.ResolveTypeAsIndex<TProperty>();

        var lazy = _properties[index];
        if (lazy.IsValid is false)
        {
            property = default!;
            return false;
        }

        property = (TProperty)lazy.Value;
        return true;
    }

    public bool TryGetProperty<TProperty>(DescPropertyType type, out TProperty property) where TProperty : IDescProperty
    {
        var lazy = _properties[type.AsIndex()];

        if (lazy.IsValid is false)
        {
            property = default!;
            return false;
        }

        property = (TProperty)lazy.Value;
        return true;
    }

    public TProperty GetDescUnsafe<TProperty>() where TProperty : IDescProperty
    {
        var index = EntityDependencies.ResolveTypeAsIndex<TProperty>();
        return (TProperty)_properties[index]!.Value;
    }

    public IDescProperty GetDescUnsafe(DescPropertyType type)
    {
        return _properties[type.AsIndex()].Value;
    }

    public bool Equals(RustyEntity? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Index == other.Index;
    }

    public bool Equals(IRustyEntity? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Index == other.Index;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((RustyEntity) obj);
    }

    public override int GetHashCode()
    {
        return Index;
    }
}

public static class RustyEntityEx
{
    public static int GetIndex(this IRustyEntity entity)
    {
        return entity.Index;
    }
}