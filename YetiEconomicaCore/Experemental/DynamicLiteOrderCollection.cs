using DynamicData;
using LiteDB;
using ReactiveUI;
using System.Collections;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Xml.Linq;

namespace YetiEconomicaCore.Experemental;

public class DynamicLiteOrderCollection<TModel> : IObservableList<TModel>, ICollection<TModel>, IDisposable
        where TModel : INotifyPropertyChanged, IDatabaseOrderEntity
{
    private readonly CompositeDisposable _disposables = new();

    private readonly ILiteCollection<BsonDocument> _collection;
    private readonly SourceList<TModel> _list;

    private bool _isSkipDBUpdate;
    private IEnumerable<TModel>? _lastItems;

    public int Count => _list.Count;

    public IObservable<int> CountChanged => _list.CountChanged;

    public IEnumerable<TModel> Items => (_lastItems ??= _list.Items);

    public bool IsReadOnly => false;

    public IObservable<IChangeSet<TModel>> Connect(Func<TModel, bool>? predicate = null) => _list.Connect(predicate);
    public IObservable<IChangeSet<TModel>> Preview(Func<TModel, bool>? predicate = null) => _list.Preview(predicate);

    public DynamicLiteOrderCollection(ILiteDatabase database, string table)
    {
        _collection = database.GetCollection(table, BsonAutoId.Int32);
        _list = new SourceList<TModel>().DisposeWith(_disposables);

        var models = new List<TModel>(_collection.Count());
        foreach (var document in _collection.FindAll())
        {
            TModel? result;
            try
            {
                result = BsonMapper.Global.ToObject<TModel>(document);
            }
            catch// (Exception e)
            {
                result = default;
            }

            if (result is null)
            {
                _collection.Delete(document["_id"]);
                continue;
            }

            result.InjectID(document["_id"]);
            models.Add(result);
        }

        _list.Edit(list =>
        {
            list.AddRange(models.OrderBy(static model => model.Order));
        });

        var connect = _list.Connect()
            .AutoRefresh(propertyChangeThrottle: TimeSpan.FromSeconds(1))
            .DisposeMany()
            .ObserveOn(RxApp.MainThreadScheduler);
        if (_list.Count > 0)
            connect = connect.Skip(1);
        connect.Subscribe(OnUpdate).DisposeWith(_disposables);

        _isSkipDBUpdate = false;
    }

    private void OnUpdate(IChangeSet<TModel> changes)
    {
        _lastItems = null;
        if (_isSkipDBUpdate)
            return;

        var mapper = BsonMapper.Global;
        BsonDocument document;

        void OnAdd(TModel model)
        {
            document = mapper.ToDocument(model.GetType(), model);
            var id = _collection.Insert(document);
            model.InjectID(id);
        }

        void OnRemove(TModel model)
        {
            _collection.Delete(model.ID);
        }

        void SortFrom(Change<TModel> change, TModel[] notifyPropertyChangeds)
        {
            for (int i = change.Item.CurrentIndex; i < notifyPropertyChangeds.Length; i++)
                notifyPropertyChangeds[i].Order = i;
        }

        TModel[] items;
        foreach (var change in changes)
        {
            switch(change.Reason)
            {
                case ListChangeReason.AddRange:
                    foreach (var model in change.Range)
                        OnAdd(model);
                    break;
                case ListChangeReason.Add:
                    OnAdd(change.Item.Current);
                    if (change.Item.CurrentIndex < _list.Count - 1)
                        SortFrom(change, (TModel[])Items);
                    break;
                case ListChangeReason.Remove:
                    OnRemove(change.Item.Current);
                    if (change.Item.CurrentIndex < _list.Count)
                        SortFrom(change, (TModel[])Items);
                    break;
                case ListChangeReason.RemoveRange:
                    foreach (var model in change.Range)
                        OnRemove(model);
                    break;
                case ListChangeReason.Clear:
                    _collection.DeleteAll();
                    break;
                case ListChangeReason.Moved:
                    items = (TModel[])Items;
                    for (int i = Math.Max(Math.Min(change.Item.CurrentIndex, change.Item.PreviousIndex), 0), iMax = i + 2; i < iMax; i++)
                        items[i].Order = i;
                    break;
                case ListChangeReason.Refresh:
                case ListChangeReason.Replace:
                    if (change.Range.Count > 0)
                    {
                        foreach (var model in change.Range)
                        {
                            if (model.ID.IsNull)
                                continue;

                            document = mapper.ToDocument(model.GetType(), model);
                            _collection.Upsert(model.ID, document);
                        }
                    }
                    else
                    {
                        var model = change.Item.Current;
                        if (model.ID.IsNull)
                            continue;

                        document = mapper.ToDocument(model.GetType(), model);
                        _collection.Upsert(model.ID, document);
                    }
                    break;
            }
        }
    }

    public void Dispose()
    {
        _list.Dispose();
    }

    public void Add(TModel item)
    {
        _list.Add(item);
    }

    public void RemoveAt(int index)
    {
        _list.Edit(list => list.RemoveAt(index));
    }

    public void Insert(int index, TModel item)
    {
        item.Order = index;
        _list.Edit(list => list.Insert(index, item));
    }

    public void Edit(Action<IExtendedList<TModel>> editAction)
    {
        _list.Edit(editAction);
    }

    public void Move(int from, int to)
    {
        _list.Move(from, to);
    }

    public void Clear()
    {
        _list.Clear();
    }

    public bool Contains(TModel item)
    {
        return _list.Items.Contains(item);
    }

    public void CopyTo(TModel[] array, int arrayIndex)
    {
        _list.Edit(list =>
        {
            list.CopyTo(array, arrayIndex);
        });
    }

    public bool Remove(TModel item) => _list.Remove(item);

    public IEnumerator<TModel> GetEnumerator() => Items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
}
