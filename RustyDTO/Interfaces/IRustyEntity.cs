﻿#if REACTIVE
using DynamicData;
using RustyDTO.Supports;
using System.Reactive.Subjects;
#endif

namespace RustyDTO.Interfaces;

public interface IRustyEntity : IEquatable<IRustyEntity>
#if REACTIVE
, ReactiveUI.IReactiveObject
#endif
{
    EntityID ID { get; }

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

#if REACTIVE
    ISubject<ItemChange<(DescPropertyType Type, IDescProperty Property)>> PropertiesChangedObserver { get; }
#endif
}