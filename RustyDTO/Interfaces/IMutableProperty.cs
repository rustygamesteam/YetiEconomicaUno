using System.Text.Json;

namespace RustyDTO.Interfaces;

public interface IMutableProperty
{
    
}

public interface IMutablePropertyResolver
{
    public IMutableProperty Resolve(int type);
    public IMutableProperty Resolve(int type, JsonElement dataElement);
}