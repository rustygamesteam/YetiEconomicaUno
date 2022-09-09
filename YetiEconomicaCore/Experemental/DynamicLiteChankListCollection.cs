using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using LiteDB;
using ReactiveUI;
using YetiEconomicaCore.Database;

namespace YetiEconomicaCore.Experemental;

public class DynamicLiteChankListCollection<TModel, TData> : IObservableCache<ModelByChunk<TModel>, BsonValue>, IDatabaseChunkListCollectionWriter<TModel>, IDisposable
    where TModel : INotifyPropertyChanged
{
    private readonly ILiteCollection<BsonDocument> _collection;
    private readonly IDatabaseChunkConvertable<TModel, TData> _converter;
    private readonly SourceCache<ModelByChunk<TModel>, BsonValue> _sourceList;
    private readonly HashSet<int> _refreshIndexes = new HashSet<int>();

    private bool _isSkipDBUpdate;
    private ILiteQueryable<BsonDocument>? _query;

    private readonly CompositeDisposable _disposable = new();

    public ILiteQueryable<BsonDocument> Query => _query ??= _collection.Query();

    private IEnumerable<ModelByChunk<TModel>>? _lastItems;
    public int Count => _sourceList.Count;
    public IObservable<int> CountChanged => _sourceList.CountChanged;
    public IEnumerable<ModelByChunk<TModel>> Items => (_lastItems ??= _sourceList.Items);

    public IEnumerable<BsonValue> Keys => _sourceList.Keys;

    public IEnumerable<KeyValuePair<BsonValue, ModelByChunk<TModel>>> KeyValues => _sourceList.KeyValues;

    public DynamicLiteChankListCollection(ILiteDatabase database, string table, IDatabaseChunkConvertable<TModel, TData> databaseConvertable)
    {
        _collection = database.GetCollection(table, autoId: BsonAutoId.Int32);

        _converter = databaseConvertable;
        _sourceList = new SourceCache<ModelByChunk<TModel>, BsonValue> (static modelByChunk => modelByChunk.ID).DisposeWith(_disposable);
        _sourceList.AddOrUpdate(_collection.ToModels(databaseConvertable));

        _sourceList.Preview().ObserveOn(RxApp.MainThreadScheduler).Subscribe(OnUpdate).DisposeWith(_disposable);
    }

    private void OnEdit(Change<ModelByChunk<TModel>, BsonValue> diff)
    {
        var item = diff.Current;

        switch (diff.Reason)
        {
            case ChangeReason.Remove:
                _collection.Delete(item.ID);
                break;
            case ChangeReason.Update:
                var doc = BsonMapper.Global.ToDocument(_converter.ToData(item.Owner, item.Model));
                _collection.Upsert(item.ID, doc);
                break;
        }
    }

    private void OnUpdate(IChangeSet<ModelByChunk<TModel>, BsonValue> updateSet)
    {
        _lastItems = null;
        if (_isSkipDBUpdate)
            return;

        _refreshIndexes.Clear();
        foreach (var change in updateSet)
            OnEdit(change);
    }

    public Optional<ModelByChunk<TModel>> Lookup(BsonValue key) => _sourceList.Lookup(key);

    public IObservable<IChangeSet<ModelByChunk<TModel>, BsonValue>> Connect(Func<ModelByChunk<TModel>, bool>? predicate = null, bool suppressEmptyChangeSets = true) => _sourceList.Connect(predicate, suppressEmptyChangeSets);

    public IObservable<Change<ModelByChunk<TModel>, BsonValue>> Watch(BsonValue key) => _sourceList.Watch(key);

    public IObservable<IChangeSet<ModelByChunk<TModel>, BsonValue>> Preview(Func<ModelByChunk<TModel>, bool>? predicate = null) => _sourceList.Preview(predicate);

    public void Dispose()
    {
        _disposable.Dispose();
    }

    bool IDatabaseChunkListCollectionWriter<TModel>.Contains(BsonValue key)
    {
        return _collection.FindById(key) != null;
    }

    bool IDatabaseChunkListCollectionWriter<TModel>.Contains(ModelByChunk<TModel> model)
    {
        return _collection.FindById(model.ID) != null;
    }

    bool IDatabaseChunkListCollectionWriter<TModel>.Update(BsonValue key, TModel data)
    {
        var item = _sourceList.Lookup(key);
        if (!item.HasValue)
            return false;

        _sourceList.AddOrUpdate(item.Value with { Model = data });
        return true;
    }

    bool IDatabaseChunkListCollectionWriter<TModel>.Add(TModel data, int owner)
    {
        try
        {
            _isSkipDBUpdate = true;
            var doc = BsonMapper.Global.ToDocument(_converter.ToData(owner, data));
            var id = _collection.Insert(doc);
            _sourceList.AddOrUpdate(ModelByChunk.Create(id, owner, data));
        }
        catch
        {
            //ignore
        }
        finally
        {
            _isSkipDBUpdate = false;
        }
        return true;
    }

    bool IDatabaseChunkListCollectionWriter<TModel>.AddRange(IEnumerable<TModel> range, int owner)
    {
        try
        {
            _isSkipDBUpdate = true;
            foreach(var data in range)
            {
                var doc = BsonMapper.Global.ToDocument(_converter.ToData(owner, data));
                var id = _collection.Insert(doc);
                _sourceList.AddOrUpdate(new ModelByChunk<TModel>(id, owner, data));
            }
        }
        catch
        {
            //ignore
        }
        finally
        {
            _isSkipDBUpdate = false;
        }
        return true;
    }

    bool IDatabaseChunkListCollectionWriter<TModel>.Remove(BsonValue id)
    {
        var count = _sourceList.Count; 
        _sourceList.RemoveKey(id);
        return count != _sourceList.Count;
    }

    int IDatabaseChunkListCollectionWriter<TModel>.RemoveMany(IEnumerable<BsonValue> range)
    {
        var count = _sourceList.Count;
        _sourceList.RemoveKeys(range);
        return count - _sourceList.Count;
    }

    void IDatabaseChunkListCollectionWriter<TModel>.Clear()
    {
        _sourceList.Clear();
    }

    public bool TryGetFirst(int index, out TModel? result)
    {
        result = Items.ForOwner(index).FirstOrDefault();
        return result is not null;
    }
}