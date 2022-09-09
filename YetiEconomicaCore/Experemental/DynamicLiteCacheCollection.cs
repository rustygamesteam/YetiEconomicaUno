using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using LiteDB;
using ReactiveUI;
using YetiEconomicaCore.Database;

namespace YetiEconomicaCore.Experemental
{
    public class DynamicLiteCacheCollection<TModel, TData> : IObservableCache<TModel, int>, IDatabaseCacheCollectionWriter<TModel>, IDisposable
        where TModel : INotifyPropertyChanged
    {
        private readonly ILiteCollection<TData> _collection;
        private readonly IDatabaseConvertable<TModel, TData> _converter;

        private bool _isSkipDBUpdate;
        private ILiteQueryable<TData>? _query;

        private readonly SourceCache<TModel, int> _cache;
        private readonly CompositeDisposable _disposable = new ();

        public ILiteQueryable<TData> Query => _query ??= _collection.Query();

        private IEnumerable<TModel>? _lastItems;
        public int Count => _cache.Count;
        public IObservable<int> CountChanged => _cache.CountChanged;
        public IEnumerable<TModel> Items => (_lastItems ??= _cache.Items);
        public IEnumerable<int> Keys => _cache.Keys;
        public IEnumerable<KeyValuePair<int, TModel>> KeyValues => _cache.KeyValues;

        public TModel this[int key] => _cache.Lookup(key).Value;

        public DynamicLiteCacheCollection(ILiteDatabase database, string table, IDatabaseConvertable<TModel, TData> converter, IComparer<TData>? dataComparer = null)
        {
            _collection = database.GetCollection<TData>(table);
            _converter = converter;

            _cache = new SourceCache<TModel, int>(_converter.GetID).DisposeWith(_disposable);
            _cache.AddOrUpdate(_collection.ToModels(converter, dataComparer));

            var connect = _cache.Connect().AutoRefresh(propertyChangeThrottle: TimeSpan.FromSeconds(1)).ObserveOn(RxApp.MainThreadScheduler);
            if (_cache.Count > 0)
                connect = connect.Skip(1);

            connect.Subscribe(OnUpdate).DisposeWith(_disposable);
        }

        public IEnumerable<TModel> ItemsWhere<TTarget>(Func<TModel, TTarget> getTarget, TTarget target) where TTarget : IEquatable<TTarget>
        {
            foreach (var item in Items)
            {
                if (getTarget(item).Equals(target))
                    yield return item;
            }
        }

        public TModel? TryGet(int key, TModel? @default = default)
        {
            var lookup = this.Lookup(key);
            return lookup.HasValue ? lookup.Value : @default;
        }

        public bool TryGet(int key, out TModel result, TModel? @default = default)
        {
            var lookup = this.Lookup(key);
            result = lookup.HasValue ? lookup.Value : @default!;
            return lookup.HasValue;
        }

        public BsonValue MaxIndex()
        {
            return _collection.Max();
        }

        private void OnEdit(Change<TModel, int> dif)
        {
            var key = _converter.GetID(dif.Current);
            switch (dif.Reason)
            {
                case ChangeReason.Refresh:
                case ChangeReason.Update:
                    _collection.Update(key, ToData(dif.Current));
                    break;
                case ChangeReason.Add:
                    _collection.Upsert(key, ToData(dif.Current));
                    break;
                case ChangeReason.Remove:
                    _collection.Delete(key);
                    break;
            }
        }

        private void OnUpdate(IChangeSet<TModel, int> updateSet)
        {
            _lastItems = null;
            if (_isSkipDBUpdate)
                return;
            
            foreach (var change in updateSet)
            {
                var obj = change.Current;
                if (obj is null)
                    continue;

                OnEdit(change);
            }
        }

        private TData ToData(TModel model)
        {
            return _converter.ToData(model);
        }

        public IObservable<IChangeSet<TModel, int>> Connect(Func<TModel, bool>? predicate = null, bool suppressEmptyChangeSets = true)
        {
            return _cache.Connect(predicate, suppressEmptyChangeSets);
        }

        public IObservable<IChangeSet<TModel, int>> Preview(Func<TModel, bool>? predicate = null)
        {
            return _cache.Preview(predicate);
        }

        public IObservable<Change<TModel, int>> Watch(int key)
        {
            return _cache.Watch(key);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }

        public TModel? SafeGet(int index)
        {
            var lookup = Lookup(index);
            return lookup.HasValue ? lookup.Value : default;
        }

        public Optional<TModel> Lookup(int key)
        {
            return _cache.Lookup(key);
        }

        bool IDatabaseCacheCollectionWriter<TModel>.Upsert(TModel data)
        {
            var count = _cache.Count;
            _cache.AddOrUpdate(data);
            return _cache.Count != count;
        }

        bool IDatabaseCacheCollectionWriter<TModel>.Update(TModel data)
        {
            _cache.AddOrUpdate(data);
            return true;
        }

        bool IDatabaseCacheCollectionWriter<TModel>.RemoveAt(int index)
        {
            var count = _cache.Count;
            _cache.RemoveKey(index);
            return _cache.Count != count;
        }

        int IDatabaseCacheCollectionWriter<TModel>.RemoveMany(IEnumerable<int> indexes)
        {
            _isSkipDBUpdate = true;
            try
            {
                int count = 0;
                foreach(var index in indexes)
                {
                    if (_collection.Delete(index))
                        count++;
                }
                var changes = count > 0;
                if(changes)
                    _cache.RemoveKeys(indexes);

                return count;
            }
            catch
            {
                //Ignore
            }
            finally
            {
                _isSkipDBUpdate = false;
            }

            return 0;
        }
    }
}
