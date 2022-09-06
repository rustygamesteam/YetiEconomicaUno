using DynamicData;
using DynamicData.Binding;
using Microsoft.UI.Xaml.Controls;
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
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;
using YetiEconomicaCore.Services;
using YetiEconomicaUno.View.YetiObjects;
using YetiEconomicaCore;

namespace YetiEconomicaUno.ViewModels.YetiObjects;

public class YetiObjectSelectorViewModel : ReactiveObject, IObservable<Func<IRustyEntity, bool>>, IDisposable
{
    public record struct YetiObjectNode(IRustyEntity Current, bool IsGroup, IList<IRustyEntity> Children) : IEquatable<YetiObjectNode>
    {
        public string DisplayName => Current?.DisplayNameWithTear ?? "None";
        public string DisplayNameWithTear => Current?.DisplayNameWithTear ?? "None";
        public Symbol Icon => IsGroup ? Symbol.Folder : Symbol.Document;

        bool IEquatable<YetiObjectNode>.Equals(YetiObjectNode node)
        {
            return ReferenceEquals(node.Current, Current);
        }
    }

    private static readonly Dictionary<RustyEntityType, YetiObjectTypeMask> Mask = new()
    {
        { RustyEntityType.Build, YetiObjectTypeMask.Builds },
        { RustyEntityType.UniqueBuild, YetiObjectTypeMask.Builds },
        { RustyEntityType.Tool, YetiObjectTypeMask.Tools },
        { RustyEntityType.UniqueTool, YetiObjectTypeMask.Tools },
        { RustyEntityType.Tech, YetiObjectTypeMask.Techs },
        { RustyEntityType.CraftTask, YetiObjectTypeMask.Crafts },
        { RustyEntityType.PlantTask, YetiObjectTypeMask.Plants },
        { RustyEntityType.ExchageTask, YetiObjectTypeMask.Exchages },
        { RustyEntityType.Resource, YetiObjectTypeMask.Resources },
        { RustyEntityType.ResourceGroup, YetiObjectTypeMask.Resources },
    };

    private IDisposable _lastFilterConnect;
    private Func<IRustyEntity, bool> _lastFilter;
    private IObserver<Func<IRustyEntity, bool>> _filterObserver;

    private readonly YetiObjectTypeMask _mask;
    private readonly ObservableCollectionExtended<YetiObjectNode> _source;
    private readonly Dictionary<int, YetiObjectNode> _groups = new ();
    private readonly List<IDisposable> _tmpDisposables = new();

    [Reactive]
    public string SearchMask { get; set; }
    public ReadOnlyObservableCollection<YetiObjectNode> Source { get; }

    private YetiObjectSelector _owner;

    public YetiObjectSelectorViewModel(YetiObjectTypeMask mask, YetiObjectSelector yetiObjectSelector,
        CompositeDisposable disposable)
    {
        _owner = yetiObjectSelector;
        _mask = mask;
        _source = new ObservableCollectionExtended<YetiObjectNode>();
        _source.Add(new YetiObjectNode(null, false, Array.Empty<IRustyEntity>()));

        Source = new (_source);

        var sort = ComparerBuilder.For<IRustyEntity>()
            .OrderBy(static obj => obj.HasProperty(DescPropertyType.HasOwner) ? 0 : 1);

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
        if(_owner is null)
            return;

        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            _filterObserver?.OnNext(OnFilter);
            var value = _owner?.SelectedValue;
            if (value is not null && !Validate(value, Source))
                _owner.SelectedValue = null;
        });
    }

    private static bool Validate(IRustyEntity entity, IReadOnlyCollection<YetiObjectNode> source)
    {
        foreach (var node in source)
        {
            if (node.IsGroup)
            {
                foreach (var child in node.Children)
                {
                    if (child.Equals(entity))
                        return true;
                }
            }
            else if (node.Current is not null && node.Current.Equals(entity))
                return true;
        }

        return false;
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

    private void OnSourceUpdated(IChangeSet<IRustyEntity, int> changes)
    {
        using var notifyDisposable = _source.SuspendNotifications();
        var service = RustyEntityService.Instance;
        foreach(var group in _groups.Values)
            _tmpDisposables.Add(((ObservableCollectionExtended<IRustyEntity>)group.Children).SuspendNotifications());

        foreach (var change in changes)
        {
            var obj = change.Current;
            IHasOwner owner;
            switch (change.Reason)
            {
                case ChangeReason.Add:
                    if (obj.TryGetProperty(out owner))
                        GetGroup(owner.Owner).Children.Add(obj);
                    else if(!obj.HasSpecialMask(EntitySpecialMask.HasChild))
                        _source.Add(new YetiObjectNode(obj, false, Array.Empty<IRustyEntity>()));
                    break;
                case ChangeReason.Remove:
                    if (obj.TryGetProperty(out owner))
                        GetGroup(owner.Owner).Children.Remove(obj);
                    else if(_source.Count > 1 && !obj.HasSpecialMask(EntitySpecialMask.HasChild))
                    {
                        var item = _source.Skip(1).FirstOrDefault(item => item.Current == obj);
                        if(item.Current is not null)
                            _source.Remove(item);
                    }
                    break;
            }
        }

        foreach(var group in _groups.Values)
        {
            if(group.Children.Count == 0) 
                _source.Remove(group);
            else if(!_source.Contains(group))
                _source.Add(group);
        }

        foreach (var disposable in _tmpDisposables)
            disposable.Dispose();
        _tmpDisposables.Clear();
    }

    private YetiObjectNode GetGroup(IRustyEntity owner)
    {
        if(!_groups.TryGetValue(owner.GetIndex(), out var result))
        {
            var childs = new ObservableCollectionExtended<IRustyEntity>();

            _groups[owner.GetIndex()] = result = new YetiObjectNode(owner, true, childs);
            _tmpDisposables.Add(childs.SuspendNotifications());
        }

        return result;
    }

    private bool OnFilter(IRustyEntity rustyEntity)
    {
        if (_lastFilter != null && _lastFilter.Invoke(rustyEntity) is false)
            return false;

        if (string.IsNullOrWhiteSpace(SearchMask))
            return true;
        var result = rustyEntity.FullName.Contains(SearchMask, StringComparison.OrdinalIgnoreCase) || HasChildWithName(rustyEntity, SearchMask);
        return result;
    }

    private static bool HasChildWithName(IRustyEntity entity, string searchMask)
    {
        if (!entity.HasSpecialMask(EntitySpecialMask.HasChild))
            return false;

        var index = entity.GetIndex();
        foreach (var child in RustyEntityService.Instance.GetItemsFor(index))
        {
            if (child.FullName?.Contains(searchMask, StringComparison.OrdinalIgnoreCase) ?? false)
                return true;
        }
        return false;
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
        if (_mask == YetiObjectTypeMask.RequiredInDependencies)
            return @object.HasSpecialMask(EntitySpecialMask.RequiredInDependencies);

        return Mask.TryGetValue(@object.Type, out var mask) && (mask & _mask) != 0;
    }

    IDisposable IObservable<Func<IRustyEntity, bool>>.Subscribe(IObserver<Func<IRustyEntity, bool>> observer)
    {
        _filterObserver = observer;
        return Disposable.Create(this, static vm => Interlocked.Exchange(ref vm._filterObserver, null));
    }

    public void Dispose()
    {
        _owner = null;

        Interlocked.Exchange(ref _lastFilter, null);
        var lastDisposable = Interlocked.Exchange(ref _lastFilterConnect, null);
        lastDisposable?.Dispose();

        _source.Clear();
        _groups.Clear();
    }
}
