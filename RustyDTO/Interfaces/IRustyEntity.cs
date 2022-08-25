using DynamicData;
using System.Reactive.Subjects;

namespace RustyDTO.Interfaces;

public interface IRustyEntity : IEquatable<IRustyEntity>
#if REACTIVE
, ReactiveUI.IReactiveObject
#endif
{
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

    IEnumerable<EntityPropertyType> Properties { get; }

    bool HasSpecialMask(EntitySpecialMask condition);
    bool IsPropertyRequired(EntityPropertyType propertyType);

    bool HasProperty(EntityPropertyType type);

    bool TryGetProperty<TProperty>(EntityPropertyType type, out TProperty property) where TProperty : IRustyEntityProperty;
    bool TryGetProperty<TProperty>(out TProperty property) where TProperty : IRustyEntityProperty;

    TProperty GetUnsafe<TProperty>() where TProperty : IRustyEntityProperty;

    IRustyEntityProperty GetUnsafe(EntityPropertyType type);

#if REACTIVE
    ISubject<ItemChange<(EntityPropertyType Type, IRustyEntityProperty Property)>> PropertiesChangedObserver { get; }
#endif
}