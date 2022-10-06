using RustyDTO.Supports;

namespace RustyDTO.Interfaces;

public interface IRustyEntity : IEquatable<IRustyEntity>
{
    EntityID ID { get; }
    int Index { get; }

    RustyEntityType Type { get; }
    int TypeAsIndex { get; }

    string? DisplayName
    {
        get;
#if REACTIVE
        set;
#endif
    }
    string? DisplayNameWithTear { get; }
    string? FullName { get; }

    IReadOnlySet<MutablePropertyType> MutablePropertyTypes { get; }
    IEnumerable<DescPropertyType> DescProperties { get; }

    bool HasSpecialMask(EntitySpecialMask condition);
    bool IsPropertyRequired(DescPropertyType propertyType);

    bool HasMutable(MutablePropertyType type);
    bool HasProperty(DescPropertyType type);

    bool TryGetProperty<TProperty>(DescPropertyType type, out TProperty property) where TProperty : IDescProperty;
    bool TryGetProperty<TProperty>(out TProperty property) where TProperty : IDescProperty;

    TProperty GetDescUnsafe<TProperty>() where TProperty : IDescProperty;

    IDescProperty GetDescUnsafe(DescPropertyType type);
}

public interface IRustyUserEntity
{
    public IRustyEntity Original { get; }
    
    public void InjectProperty<TProperty>(TProperty property) where TProperty : IMutableProperty;
    
    public TProperty Get<TProperty>() where TProperty : IMutableProperty;
    public bool TryGet<TProperty>(out TProperty result) where TProperty : class, IMutableProperty;
}

#if REACTIVE
public interface IReactiveRustyEntity : IRustyEntity, ReactiveUI.IReactiveObject
{
    System.Reactive.Subjects.ISubject<DynamicData.ItemChange<(DescPropertyType Type, IDescProperty Property)>> PropertiesChangedObserver { get; }
}
#endif