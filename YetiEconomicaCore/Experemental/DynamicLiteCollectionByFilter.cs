using DynamicData;
using ReactiveUI;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using YetiEconomicaCore.Database;
using LiteDB;
using System.Reactive;

namespace YetiEconomicaCore.Experemental;

public class DynamicLiteCollectionByFilter<TModel, TData> : ICollection<TModel>, IObservableList<ModelByChunk<TModel>>, IList, IDisposable, INotifyCollectionChanged, INotifyPropertyChanged
    where TModel : INotifyPropertyChanged
{
    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly DynamicLiteChankListCollection<TModel, TData> _collection;
    private readonly ICollectionWithFilterConfig<TModel> _config;

    private readonly Lazy<ReadOnlyCollection<TModel>> _observableCollectionLazy;
    private readonly Lazy<IObservableList<ModelByChunk<TModel>>> _observableLazy;

    private CompositeDisposable _disposable;

    public bool IsAutoRefrash { get; }
    public bool IsAutoRefrashToView { get; }

    public ReadOnlyCollection<TModel> ObservableCollection => _observableCollectionLazy.Value;

    internal DynamicLiteCollectionByFilter(DynamicLiteChankListCollection<TModel, TData> collection, ICollectionWithFilterConfig<TModel> config, bool isAutoRefrash = false, bool isAutoRefrashToView = false)
    {
        _collection = collection;
        _config = config;

        IsAutoRefrash = isAutoRefrash;
        IsAutoRefrashToView = isAutoRefrashToView;

        _disposable = new CompositeDisposable();

        _observableLazy = new(() =>
        {
            var source = collection.Connect().RemoveKey();
            var list = new SourceList<ModelByChunk<TModel>>(source);
            source.Subscribe().DisposeWith(_disposable);

            return list;
        });

        _observableCollectionLazy = new (() =>
        {
            var binding = _collection.Connect(_config.Filter)
            .Transform(chunk => chunk.Model)
            .Bind(out var observableCollection);

            if (IsAutoRefrash || IsAutoRefrashToView)
                binding = binding
                    .AutoRefresh(propertyChangeThrottle: TimeSpan.FromSeconds(1))
                    .ObserveOn(RxApp.MainThreadScheduler);

            binding.Subscribe(OnChange).DisposeWith(_disposable);

            var collectionChanged = (INotifyCollectionChanged)observableCollection;

            Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                handler => collectionChanged.CollectionChanged += handler,
                handler => collectionChanged.CollectionChanged -= handler)
                .Subscribe(DynamicLiteCollectionByFilter_CollectionChanged)
                .DisposeWith(_disposable);

            return observableCollection;
        });
    }

    private void DynamicLiteCollectionByFilter_CollectionChanged(EventPattern<NotifyCollectionChangedEventArgs> pattern)
    {
        CollectionChanged?.Invoke(pattern.Sender, pattern.EventArgs);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
    }

    private void OnChange(IChangeSet<TModel, BsonValue> diffs)
    {
        foreach(var change in diffs)
        {
            if (change.Reason != ChangeReason.Refresh)
                continue;

            if(IsAutoRefrashToView)
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, change.Current));

            if (IsAutoRefrash)
                _collection.AsWriter().Update(change.Key, change.Current);
        }
    }

    public int Count => ObservableCollection.Count;

    public bool IsReadOnly => true;

    bool IList.IsFixedSize => false;

    bool ICollection.IsSynchronized => true;

    object ICollection.SyncRoot => true;

    public IEnumerable<ModelByChunk<TModel>> Items => _collection.Items;

    public IObservable<int> CountChanged => _collection.CountChanged;

    object? IList.this[int index] 
    { 
        get => ObservableCollection[index]; 
        set => throw new NotImplementedException();
    }

    public void Add(TModel item)
    {
        _collection.AsWriter().Add(item, _config.Owner);
    }

    public bool Remove(TModel model)
    {
        var chunk = _collection.Items.Where(_config.Filter).FirstOrDefault(item => ReferenceEquals(item.Model, model));
        if (chunk.ID == BsonValue.Null)
            return false;
        return _collection.AsWriter().Remove(chunk.ID);
    }

    public void Clear()
    {
        var chunks = _collection.Items.Where(_config.Filter).Select(static chunk => chunk.ID).ToArray();
        _collection.AsWriter().RemoveMany(chunks);
    }

    public bool Contains(TModel model)
    {
        var chunk = _collection.Items.Where(_config.Filter).FirstOrDefault(item => ReferenceEquals(item.Model, model));
        return chunk.ID != BsonValue.Null;
    }

    public void CopyTo(TModel[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<TModel> GetEnumerator()
    {
        return ObservableCollection.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ObservableCollection.GetEnumerator();
    }

    public void Dispose()
    {
        if (_disposable == null)
            return;

        _disposable.Dispose();
        _disposable = null!;
    }

    int IList.Add(object? value)
    {
        throw new NotImplementedException();
    }

    void IList.Clear()
    {
        throw new NotImplementedException();
    }

    bool IList.Contains(object? value)
    {
        throw new NotImplementedException();
    }

    int IList.IndexOf(object? value)
    {
        return ObservableCollection.IndexOf((TModel)value!);
    }

    void IList.Insert(int index, object? value)
    {
        throw new NotImplementedException();
    }

    void IList.Remove(object? value)
    {
        throw new NotImplementedException();
    }

    void IList.RemoveAt(int index)
    {
        throw new NotImplementedException();
    }

    void ICollection.CopyTo(Array array, int index)
    {
        throw new NotImplementedException();
    }


    public IObservable<IChangeSet<ModelByChunk<TModel>>> Connect(Func<ModelByChunk<TModel>, bool>? predicate = null)
    {
        return _observableLazy.Value.Connect(predicate);
    }

    public IObservable<IChangeSet<ModelByChunk<TModel>>> Preview(Func<ModelByChunk<TModel>, bool>? predicate = null)
    {
        return _observableLazy.Value.Preview(predicate);
    }
}
