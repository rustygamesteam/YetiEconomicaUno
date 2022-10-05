using System.Collections;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Kernel;
using LiteDB;
using RustyDTO;
using RustyDTO.Interfaces;

namespace YetiEconomicaCore.Experemental;

internal class DynamicEntityDatabase : IDisposable, IEnumerable<IRustyEntity>
{
    private readonly ILiteCollection<BsonDocument> _entities;
    private readonly SourceCache<IReactiveRustyEntity, int> _cache;

    private CompositeDisposable _disposable;
    private IList<IRustyEntity>? _items;

    public DynamicEntityDatabase(ILiteDatabase database, string table)
    {
        _disposable = new CompositeDisposable();

        _entities = database.GetCollection(table, BsonAutoId.Int32);
        _cache = new SourceCache<IReactiveRustyEntity, int>(static entity => entity.GetIndex());
        _cache.Edit(updater =>
        {
            foreach (var document in _entities.FindAll())
            {
                var entity = ParseFromDocument(document);
                updater.AddOrUpdate(new KeyValuePair<int, IReactiveRustyEntity>(entity.ID.Index, entity));
            }
        });

        _cache.Connect().AutoRefresh(propertyChangeThrottle: TimeSpan.FromSeconds(1)).SkipInitial().Subscribe(OnEntitiesChange)
            .DisposeWith(_disposable);
    }

    public bool HasEntity(int index)
    {
        return Lookup(index).HasValue;
    }

    public IRustyEntity this[int index]
    {
        get => _cache.Lookup(index).Value;
    }

    public Optional<IReactiveRustyEntity> Lookup(int index)
    {
        return _cache.Lookup(index);
    }

    public IObservable<IChangeSet<IReactiveRustyEntity, int>> Connect(Func<IRustyEntity, bool>? filter = null)
    {
        return _cache.Connect(filter);
    }

    public IObservable<IChangeSet<IReactiveRustyEntity, int>> Preview(Func<IRustyEntity, bool>? filter = null)
    {
        return _cache.Preview(filter);
    }

    public void Add(int index, RustyEntityType type, string? displayName, ICollection<DescPropertyType> properties, Action<int>? fillProperties)
    {
        var document = FillDocument(type, displayName, properties);
        if (index == 0)
            index = _entities.Insert(document);
        else 
            _entities.Insert(index, document);

        fillProperties?.Invoke(index);
        var entity = new RustyEntity(index, type, displayName, properties, EntityDependencies.GetMutalbeProperties(type));
        _cache.AddOrUpdate(entity);
    }

    public void Remove(int index)
    {
        if (_entities.Delete(index))
            _cache.RemoveKey(index);
    }

    private IList<IRustyEntity> GetItems()
    {
        return _items ??= (IList<IRustyEntity>)_cache.Items;
    }

    private void OnEntitiesChange(IChangeSet<IReactiveRustyEntity, int> diffs)
    {
        foreach (var diff in diffs)
        {
            switch (diff.Reason)
            {
                case ChangeReason.Remove:
                case ChangeReason.Add:
                    _items = null;
                    break;
                case ChangeReason.Refresh:
                    var entity = diff.Current;

                    _entities.Upsert(FillDocument(entity.Type, entity.DisplayName, entity.DescProperties, entity.ID.Index));
                    break;
            }
        }
    }

    private IReactiveRustyEntity ParseFromDocument(BsonDocument document)
    {
        var index = document["_id"].AsInt32;
        var type = (RustyEntityType)document["Type"].AsInt32;
        var displayName = document["Name"].AsString;
        var properties = document["Properties"].AsArray.Select(value => (DescPropertyType)value.AsInt32);

        var mutable = EntityDependencies.GetMutalbeProperties(type);

        return new RustyEntity(index, type, displayName, properties, mutable);
    }

    private BsonDocument FillDocument(RustyEntityType type, string? displayName,
        IEnumerable<DescPropertyType> properties, int? id = null)
    {
        var raw = new Dictionary<string, BsonValue>(4)
        {
            { "Type", (int)type },
            { "Name", displayName },
            { "Properties", new BsonArray(properties?.Select(type => new BsonValue((int)type)) ?? Enumerable.Empty<BsonValue>()) }
        };
        if (id != null)
            raw["_id"] = id;

        return new BsonDocument(raw);
    }

    public void Dispose()
    {
        _cache.Dispose();
        _disposable.Dispose();
    }

    IEnumerator<IRustyEntity> IEnumerable<IRustyEntity>.GetEnumerator()
    {
        return GetItems().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetItems().GetEnumerator();
    }
}