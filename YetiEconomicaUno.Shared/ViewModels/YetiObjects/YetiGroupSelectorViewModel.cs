using DynamicData;
using DynamicData.Binding;
using Nito.Comparers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using RustyDTO;
using YetiEconomicaCore.Services;
using YetiEconomicaUno.View.YetiObjects;
using RustyDTO.Interfaces;
using static YetiEconomicaUno.ViewModels.YetiObjects.YetiObjectSelectorViewModel;

namespace YetiEconomicaUno.ViewModels.YetiObjects;

public class YetiGroupSelectorViewModel : ReactiveObject, IObservable<Func<IRustyEntity, bool>>, IDisposable
{
    private static readonly Dictionary<RustyEntityType, YetiGroupTypeMask> Mask = new()
    {
        { RustyEntityType.UniqueBuild, YetiGroupTypeMask.Builds },
        { RustyEntityType.UniqueTool, YetiGroupTypeMask.Tools },
        { RustyEntityType.ResourceGroup, YetiGroupTypeMask.Resources },
        { RustyEntityType.CraftTask, YetiGroupTypeMask.Crafts },
    };

    private IDisposable _lastFilterConnect;
    private Func<IRustyEntity, bool> _lastFilter;
    private IObserver<Func<IRustyEntity, bool>> _filterObserver;

    private readonly YetiGroupTypeMask _mask;
    private readonly ObservableCollectionExtended<YetiObjectNode> _source;

    [Reactive]
    public string SearchMask { get; set; }
    public ReadOnlyObservableCollection<YetiObjectNode> Source { get; }

    public YetiGroupSelectorViewModel(YetiGroupTypeMask mask, CompositeDisposable disposable)
    {
        _mask = mask;
        _source = new ObservableCollectionExtended<YetiObjectNode>();
        _source.Add(new YetiObjectSelectorViewModel.YetiObjectNode(null, false, Array.Empty<IRustyEntity>()));

        Source = new(_source);

        var sort = ComparerBuilder.For<IRustyEntity>()
            .OrderBy(static obj => obj.HasSpecialMask(EntitySpecialMask.HasChild) ? 0 : 1);

        RustyEntityService.Instance.ConnectToEntity(FilterByMask)
            .Filter(this)
            .Sort(sort)
            .Subscribe(OnSourceUpdated)
            .DisposeWith(disposable);

        this.WhenValueChanged(static x => x.SearchMask)
           .Throttle(TimeSpan.FromMilliseconds(100))
           .Subscribe(OnTextFilterChanged)
           .DisposeWith(disposable);

        this.DisposeWith(disposable);
    }

    public void FilterNext()
    {
        App.MainWindow.DispatcherQueue.TryEnqueue(() => _filterObserver?.OnNext(OnFilter));
    }

    internal void FilterChanged(IObservable<Func<IRustyEntity, bool>> filter)
    {
        IDisposable disposable = null;

        if (filter == null)
            OnFilterUpdated(null);
        else
            disposable = filter.Subscribe(OnFilterUpdated);

        var lastDisposable = Interlocked.Exchange(ref _lastFilterConnect, disposable);
        lastDisposable?.Dispose();
    }

    private void OnSourceUpdated(IChangeSet<IReactiveRustyEntity, int> changes)
    {
        using var notifyDisposable = _source.SuspendNotifications();
        var service = RustyEntityService.Instance;
        YetiGroupTypeMask resultMask;

        foreach (var change in changes)
        {
            var obj = change.Current;
            switch (change.Reason)
            {
                case ChangeReason.Add:
                    if(Mask.TryGetValue(obj.Type, out resultMask) && (resultMask & _mask) != 0)
                        _source.Add(new YetiObjectNode(obj, false, Array.Empty<IRustyEntity>()));
                    break;
                case ChangeReason.Remove:
                    if (Mask.TryGetValue(obj.Type, out resultMask) && (resultMask & _mask) != 0)
                    {
                        var item = _source.Skip(1).FirstOrDefault(item => item.Current == obj);
                        if (item.Current is not null)
                            _source.Remove(item);
                    }
                    break;
            }
        }
    }

    private bool OnFilter(IRustyEntity rustyEntity)
    {
        if (_lastFilter != null && _lastFilter.Invoke(rustyEntity) is false)
            return false;

        if (string.IsNullOrWhiteSpace(SearchMask))
            return true;
        var result = rustyEntity.DisplayName.Contains(SearchMask, StringComparison.OrdinalIgnoreCase);
        return result;
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

    private bool FilterByMask(IRustyEntity @object)
    {
        return Mask.TryGetValue(@object.Type, out var result) && (result & _mask) != 0;
    }

    IDisposable IObservable<Func<IRustyEntity, bool>>.Subscribe(IObserver<Func<IRustyEntity, bool>> observer)
    {
        _filterObserver = observer;
        return Disposable.Create(this, static vm => Interlocked.Exchange(ref vm._filterObserver, null));
    }

    public void Dispose()
    {
        _source.Clear();
    }
}
