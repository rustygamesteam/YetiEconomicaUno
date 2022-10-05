using System.Buffers;
using System.Collections;
using System.Text.Json;
using RustyDTO;
using RustyDTO.Interfaces;
using RustyDTO.Supports;
using RustyEngine.Internal;
using RustyDTO.DescPropertyModels;

namespace RustyEngine;

public class EntityService : IEntityService
{
    public bool IsInitialize { get; private set; }

#pragma warning disable CS8618
    private Dictionary<int, IRustyEntity> _entities;
    private Dictionary<int, IRustyEntity[]> _entitiesByOwner;
#pragma warning restore CS8618

    public void Initialize(JsonDocument? database)
    {
        if(database is null)
            return;

        var propertyFactory = Engine.Instance.PropertyFactory;

        var root = database.RootElement;
        var entities = root.GetProperty("entities");
        var entitiesCount = entities.GetArrayLength();

        _entities = new Dictionary<int, IRustyEntity>(entitiesCount);
        _entitiesByOwner = new Dictionary<int, IRustyEntity[]>(entitiesCount);

        var onInitialize = new List<IDisposable>(entitiesCount);

        Span<MutablePropertyType> mutableTypes = stackalloc MutablePropertyType[EntityDependencies.MutablePropertiesCount];
        Span<DescPropertyType> propertyTypes = stackalloc DescPropertyType[EntityDependencies.DescPropertiesCount];
        Span<LazyDescProperty> propertySpan = new LazyDescProperty[EntityDependencies.DescPropertiesCount];

        var entityWithParent = new List<IRustyEntity>(entitiesCount);

        foreach (var entityNode in entities.EnumerateArray())
        {
            var id = new EntityID(EntityIndexType.d, entityNode.GetProperty("id").GetInt32());
            var type = entityNode.GetProperty("type").GetInt32();
            var displayName = entityNode.GetProperty("diplayName").GetString();

            var descPropertiesRaw = entityNode.GetProperty("properties");
            var mutablePropertiesRaw = entityNode.GetProperty("mutable");

            int descCount = 0;
            foreach (var propertyJson in descPropertiesRaw.EnumerateObject())
            {
                var typeIndex = MathHelper.ToUnsignedIndexString(propertyJson.Name);

                var index = descCount++;

                propertyTypes[index] = (DescPropertyType)typeIndex;
                propertySpan[index] = propertyFactory.Resolve(typeIndex, propertyJson.Value);
            }

            int mutableCount = 0;
            foreach (var index in mutablePropertiesRaw.EnumerateArray())
                mutableTypes[mutableCount++] = (MutablePropertyType)index.GetInt32();

            var entity = new Entity(id, type, displayName, propertyTypes.Slice(0, descCount), propertySpan.Slice(0, descCount), mutableTypes.Slice(0, mutableCount), out var onComplete);
            _entities.Add(id.Index, entity);
            onInitialize.Add(onComplete);

            if (entity.HasSpecialMask(EntitySpecialMask.HasParent))
                entityWithParent.Add(entity);
        }

        foreach (var disposable in onInitialize)
            disposable.Dispose();

        foreach (var info in entityWithParent.GroupBy(static info => info.GetDescUnsafe<IHasOwner>().Owner.ID.Index))
            _entitiesByOwner.Add(info.Key, info.ToArray());

        IsInitialize = true;
    }

    public IRustyEntity GetEntity(EntityID id)
    {
        return _entities[id.Index];
    }

    public bool TryGetEntity(EntityID id, out IRustyEntity? entity)
    {
        return _entities.TryGetValue(id.Index, out entity);
    }

    public IEnumerable<IRustyEntity> AllEntites()
    {
        return _entities.Values;
    }

    public IEnumerable<IRustyEntity> EntitesWhere(Func<IRustyEntity, bool> where)
    {
        return _entities.Values.Where(where);
    }

    public IEnumerable<IRustyEntity> EntitesWhereType(RustyEntityType type)
    {
        var typeIndex = type.AsIndex();
        foreach (var entity in _entities.Values)
        {
            if (entity.TypeAsIndex == typeIndex)
                yield return entity;
        }
    }

    public IEnumerable<IRustyEntity> EntitesWhereTypes(BitArray types)
    {
        foreach (var entity in _entities.Values)
        {
            if (types.Get(entity.TypeAsIndex))
                yield return entity;
        }
    }

    public IEnumerable<IRustyEntity> GetItemsFor(EntityID ownerID)
    {
        if (_entitiesByOwner.TryGetValue(ownerID.Index, out var range))
            return range;
        return Enumerable.Empty<IRustyEntity>();
    }
}