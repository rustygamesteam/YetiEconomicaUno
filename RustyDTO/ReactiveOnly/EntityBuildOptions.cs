using RustyDTO.Interfaces;

#if REACTIVE
namespace RustyDTO;

public enum EntityBuildKeys
{
    OwnerIndex,
    Entity,
    Tear,
    From,
    To
}

public record EntityBuildOptions
{
    private static readonly Dictionary<string, object> _tmp = new();
    private readonly IReadOnlyDictionary<string, object> _values;

    public static EntityBuildOptions Create()
    {
        return new EntityBuildOptions();
    }

    public static EntityBuildOptions CreateWithOwner(int index, int tear = 0)
    {
        return Create().Add(EntityBuildKeys.OwnerIndex, index).Add(EntityBuildKeys.Tear, tear);
    }

    public static EntityBuildOptions CreateWithEntity(IRustyEntity entity, int tear = 0)
    {
        return Create().Add(EntityBuildKeys.Entity, entity);
    }

    private EntityBuildOptions()
    {
        _tmp.Clear();
        _values = _tmp;
    }

    public EntityBuildOptions Add(string key, object value)
    {
        _tmp.Add(key, value);
        return this;
    }

    public EntityBuildOptions Add(EntityBuildKeys key, object value)
    {
        _tmp.Add(key.ToString(), value);
        return this;
    }

    public T Get<T>(string key)
    {
        return (T)_values[key];
    }

    public T Get<T>(EntityBuildKeys key)
    {
        return (T)_values[key.ToString()];
    }
}
#endif