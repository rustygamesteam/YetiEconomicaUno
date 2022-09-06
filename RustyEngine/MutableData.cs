using RustyDTO.Interfaces;
using System.Text.Json;

namespace RustyDTO;

public class MutableData
{
    private IMutableProperty[] _mutableProperties = new IMutableProperty[EntityDependencies.MutablePropertiesCount];

    public MutableData(JsonElement value)
    {
        throw new NotImplementedException();
    }

    public TProperty Get<TProperty>() where TProperty : IMutableProperty
    {
        var index = EntityDependencies.ResolveMutableTypeAsIndex<TProperty>();
        return (TProperty)_mutableProperties[index];
    }
}