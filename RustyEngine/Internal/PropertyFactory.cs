using System.Text.Json;
using RustyDTO;
using RustyDTO.Interfaces;

namespace RustyEngine.Internal;

public class PropertyFactory
{
    private IDescPropertyConverter[] _converters;

    public PropertyFactory()
    {
        _converters = new IDescPropertyConverter[EntityDependencies.DescPropertiesCount];

        //TODO!
        //_converters[DescPropertyType.HasOwner.AsIndex()] = ;
    }

    public LazyDescProperty Resolve(int type, JsonElement raw)
    {
        return _converters[type - 1].Resolve(raw);
    }

    public IMutableProperty ResolveMutable(int type)
    {
        throw new NotImplementedException();
    }
}