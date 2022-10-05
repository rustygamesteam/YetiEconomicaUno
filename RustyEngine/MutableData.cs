using RustyDTO.Interfaces;
using System.Text.Json;
using RustyEngine;
using RustyEngine.Internal;

namespace RustyDTO;

public class MutableData
{
    private int _entityType;
    private IMutableProperty[] _mutableProperties;

    public MutableData(RustyEntityType entityType) : this(entityType.AsIndex())
    {

    }

    public MutableData(JsonElement value)
    {
        _entityType = value.GetProperty("type").GetInt32();
        EntityDependencies.MutalbeBuild(_entityType, value, Engine.Instance.MutableResolver, out _mutableProperties);
    }

    internal MutableData(int entityType)
    {
        _entityType = entityType;
        EntityDependencies.MutalbeBuild(entityType, Engine.Instance.MutableResolver, out _mutableProperties);
    }

    public TProperty Get<TProperty>() where TProperty : IMutableProperty
    {
        var index = EntityDependencies.ResolveMutableTypeAsIndex<TProperty>(_entityType);
        return (TProperty)_mutableProperties[index];
    }

    public bool TryGet<TProperty>(out TProperty result) where TProperty : class, IMutableProperty
    {
        var index = EntityDependencies.ResolveMutableTypeAsIndex<TProperty>(_entityType);
        if (index == -1)
        {
            result = null!;
            return false;
        }

        result = (TProperty) _mutableProperties[index];
        return result is not null;
    }
}