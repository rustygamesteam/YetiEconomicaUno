using DynamicData;
using DynamicData.Binding;
using Nito.Comparers;
using ReactiveUI.Fody.Helpers;
using RustyDTO.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using RustyDTO;
using YetiEconomicaCore.Services;

namespace YetiEconomicaUno.ViewModels.YetiObjects;

public record struct EnumWithHeader(string Name, RustyEntityType Type);

public class YetiObjectsPageViewModel : BaseViewModel
{
    private static RustyEntityService Service { get; } = RustyEntityService.Instance;

    [Reactive]
    public string NewName { get; set; }
    [Reactive]
    public RustyEntityType NewType { get; set; }

    [Reactive]
    public string SearchMask { get; set; }

    private YetiObjectsPageFilter _filter;

    public static EnumWithHeader[] Filters { get; } = new EnumWithHeader[]
    {
        new("Build", RustyEntityType.UniqueBuild),
        new("Tool", RustyEntityType.UniqueTool),
        new("Tech", RustyEntityType.Tech),
        new("PVE", RustyEntityType.PVE),
        new("SubBuild", RustyEntityType.Superstructure)
    };

    private class YetiObjectsPageFilter : IObservable<Func<IRustyEntity, bool>>
    {
        private readonly BitArray _bitArray;
        private string _mask;
        private readonly Func<IRustyEntity, bool> _func;

        private List<IObserver<Func<IRustyEntity, bool>>> _observers = new List<IObserver<Func<IRustyEntity, bool>>>();

        public YetiObjectsPageFilter(BitArray bitArray)
        {
            _func = Validate;
            _bitArray = bitArray;
        }

        IDisposable IObservable<Func<IRustyEntity, bool>>.Subscribe(IObserver<Func<IRustyEntity, bool>> observer)
        {
            observer.OnNext(_func);

            _observers.Add(observer);
            var data = (observer, _observers);

            return Disposable.Create(data, static data => data._observers.Remove(data.observer));
        }

        public void Next(string mask)
        {
            _mask = mask;
            Notify();
        }

        public void Next(IEnumerable<RustyEntityType> selected)
        {
            _bitArray.SetAll(false);
            foreach (var type in selected)
                _bitArray.Set((int)type, true);
            Notify();
        }

        private void Notify()
        {
            foreach (var observer in _observers)
                observer.OnNext(_func);
        }

        private bool Validate(IRustyEntity entity)
        {
            return _bitArray.Get(entity.TypeAsIndex) && (string.IsNullOrWhiteSpace(_mask) || entity.DisplayName.Contains(_mask, StringComparison.OrdinalIgnoreCase));
        }
    }

    internal ObservableCollectionExtended<IRustyEntity> ItemSource { get; } = new();

    public void Initialize(CompositeDisposable disposable)
    {
        var sort = ComparerBuilder.For<IRustyEntity>()
            .OrderBy(static data => data.Type)
            .ThenBy(static data => data.DisplayName);

        var mask = EntityDependencies.ToConstantBitmask(Filters.Select(header => header.Type).ToArray());
        _filter = new YetiObjectsPageFilter(mask);

        Service.ConnectToEntity(model => mask.Get(model.TypeAsIndex))
            .Filter(_filter)
            .Sort(sort)
            .Bind(ItemSource)
            .Subscribe()
            .DisposeWith(disposable);

        this
            .WhenValueChanged(static x => x.SearchMask)
            .Subscribe(_filter.Next)
            .DisposeWith(disposable);
    }

    public void UpdateBitmask(IEnumerable<EnumWithHeader> selected)
    {
        _filter.Next(selected.Select(static enumWithHeader => enumWithHeader.Type));
    }

    private bool OnFilter(IRustyEntity data)
    {
        return string.IsNullOrWhiteSpace(SearchMask) || data.DisplayName.Contains(SearchMask, StringComparison.OrdinalIgnoreCase);
    }

    public void TryAdd()
    {
        if (string.IsNullOrEmpty(NewName))
            return;
        Service.Create(NewType, NewName);
        NewName = string.Empty;
    }
}
