using System.Text.Json;
using RustyDTO.Interfaces;

namespace RustyEngine.Internal;

public struct LazyDescPropertyResolver : ILazyDescPropertyResolver
{
    private IDescPropertyResolver _resolver;
    private int _index;
    private int _type;
    private JsonElement _data;

    public LazyDescPropertyResolver(IDescPropertyResolver resolver, int index, int type, JsonElement data)
    {
        _resolver = resolver;
        _index = index;
        _type = type;
        _data = data;
    }

    public IDescProperty Resolve()
    {
        return _resolver.Resolve(_index, _type, _data);
    }
}