using RustyDTO.Interfaces;
using System.Text.Json;
using RustyDTO.Supports;
using RustyEngine;

namespace RustyDTO;

internal class RustyUserEntity : IRustyUserEntity
{
    private IRustyEntity _entity;

    public IRustyEntity Original => _entity;
    private IMutableProperty[] _mutableProperties;
    
    internal RustyUserEntity(IRustyEntity original)
    {
        _entity = original;
        EntityDependencies.MutableBuild(_entity.TypeAsIndex, Engine.Instance.MutableResolver, out _mutableProperties);
    }
    
    internal RustyUserEntity(IRustyEntity original, JsonElement value)
    {
        _entity = original;
        EntityDependencies.MutableBuild(original.TypeAsIndex, value, Engine.Instance.MutableResolver, out _mutableProperties);
    }

    internal RustyUserEntity(Engine engine, EntityID id, JsonElement value, out IDisposable onComplete)
    {
        int type;
        
        if (id.IsUserEntity)
            type = value.GetProperty("type").GetInt32();
        else
        {
            _entity = engine.EntityService.GetEntity(id.Index);
            type = _entity.TypeAsIndex;
        }

        onComplete = new CompleteInitialize(type, this, value, engine.MutableResolver);
    }
    
    private struct CompleteInitialize : IDisposable
    {
        private int _type;
        private RustyUserEntity _entity;
        private JsonElement _data;
        private IMutablePropertyResolver _resolver;

        public CompleteInitialize(int type, RustyUserEntity entity, JsonElement data, IMutablePropertyResolver resolver)
        {
            _type = type;
            _entity = entity;
            _data = data;
            _resolver = resolver;
        }

        public void Dispose()
        {
            if(_entity._entity is null)
                EntityDependencies.MutableBuild(_type, _data, _resolver, out _entity._mutableProperties, out _entity._entity);
            else
                EntityDependencies.MutableBuild(_type, _data, _resolver, out _entity._mutableProperties);
        }
    }

    public TProperty Get<TProperty>() where TProperty : IMutableProperty
    {
        var index = EntityDependencies.ResolveMutableTypeAsIndex<TProperty>(_entity.TypeAsIndex);
        return (TProperty)_mutableProperties[index];
    }

    public void InjectProperty<TProperty>(TProperty property) where TProperty : IMutableProperty
    {
        var index = EntityDependencies.ResolveMutableTypeAsIndex<TProperty>(_entity.TypeAsIndex);
        _mutableProperties[index] = property;
    }

    public bool TryGet<TProperty>(out TProperty result) where TProperty : class, IMutableProperty
    {
        var index = EntityDependencies.ResolveMutableTypeAsIndex<TProperty>(_entity.TypeAsIndex);
        if (index == -1)
        {
            result = null!;
            return false;
        }

        result = (TProperty) _mutableProperties[index];
        return result is not null;
    }
}