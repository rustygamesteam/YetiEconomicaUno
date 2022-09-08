﻿using RustyDTO.Interfaces;
using System.Text.Json;
using RustyDTO.Supports;

namespace RustyDTO;

public class MutableData
{
    private IMutableProperty[] _mutableProperties = new IMutableProperty[EntityDependencies.MutablePropertiesCount];

    public MutableData(JsonElement value)
    {
        throw new NotImplementedException();
    }

    public MutableData(RustyEntityType mutablePropertyTypes)
    {
        throw new NotImplementedException();
    }

    public TProperty Get<TProperty>() where TProperty : IMutableProperty
    {
        var index = EntityDependencies.ResolveMutableTypeAsIndex<TProperty>();
        return (TProperty)_mutableProperties[index];
    }
}