using System;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using YetiEconomicaCore.Services;
using DynamicData.PLinq;
using Nito.Comparers.Linq;
using ReactiveUI.Fody.Helpers;
using System.Threading;
using RustyDTO.Interfaces;
using RustyDTO.PropertyModels;

namespace YetiEconomicaUno.ViewModels;

public class SelectResourceFlyoutViewModel : ReactiveObject, IObservable<Func<IRustyEntity, bool>>
{
    [Reactive]
    public string SearchMask { get; set; }

    private readonly ObservableCollectionExtended<ResourcesByGroup> _source;
    public ReadOnlyObservableCollection<ResourcesByGroup> Source { get; }

    private IDisposable _lastFilterConnect;
    private Func<IRustyEntity, bool> _lastFilter;
    private IObserver<Func<IRustyEntity, bool>> _filterObserver;

    private readonly ObservableCollectionExtended<IRustyEntity> _sourceFirst = new();

    public SelectResourceFlyoutViewModel(CompositeDisposable disposable)
    {
        ResourceService.Instance.ObservableResources.Connect()
            .Filter(this)
            .Bind(_sourceFirst)
            .Subscribe(OnChangeFilter)
            .DisposeWith(disposable);

        _source = new ObservableCollectionExtended<ResourcesByGroup>();
        Source = new ReadOnlyObservableCollection<ResourcesByGroup>(_source);

         this.WhenValueChanged(static x => x.SearchMask)
            .Throttle(TimeSpan.FromMilliseconds(100))
            .Subscribe(OnTextFilterChanged)
            .DisposeWith(disposable);
    }

    private void OnChangeFilter(IChangeSet<IRustyEntity> obj)
    {
        var result = _sourceFirst.GroupBy(static resource => resource.GetUnsafe<IHasOwner>().Owner.Index)
            .Select(static entities => new ResourcesByGroup(RustyEntityService.Instance.GetEntity(entities.Key), entities));

        using var supress = _source.SuspendNotifications();
        _source.Clear();
        _source.AddRange(result);
    }

    IDisposable IObservable<Func<IRustyEntity, bool>>.Subscribe(IObserver<Func<IRustyEntity, bool>> observer)
    {
        _filterObserver = observer;
        return Disposable.Create(this, static vm => Interlocked.Exchange(ref vm._filterObserver, null));
    }

    private bool OnFilter(IRustyEntity resource)
    {
        var result = (_lastFilter?.Invoke(resource) ?? true) && (string.IsNullOrWhiteSpace(SearchMask) || resource.FullName.Contains(SearchMask, StringComparison.OrdinalIgnoreCase));
        return result;
    }

    internal void FilterChanged(IObservable<Func<IRustyEntity, bool>> filter)
    {
        IDisposable disposable = null;

        if(filter == null)
            OnFilterUpdated(null);
        else
            disposable = filter.Subscribe(OnFilterUpdated);

        var lastDisposable = Interlocked.Exchange(ref _lastFilterConnect, disposable);
        lastDisposable?.Dispose();
    }

    private void OnTextFilterChanged(string text)
    {
        FilterNext();
    }

    private void OnFilterUpdated(Func<IRustyEntity, bool> func)
    {
        Interlocked.Exchange(ref _lastFilter, func);
        FilterNext();
    }

    public void FilterNext()
    {
        App.MainWindow.DispatcherQueue.TryEnqueue(() => _filterObserver?.OnNext(OnFilter));
    }
}