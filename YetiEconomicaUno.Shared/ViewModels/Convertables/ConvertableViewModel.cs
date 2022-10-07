using DynamicData;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using RustyDTO.DescPropertyModels;
using YetiEconomicaCore.Services;
using RustyDTO.Interfaces;
using YetiEconomicaCore;

namespace YetiEconomicaUno.ViewModels.Convertables;

public record ConvertableViewModel(IRustyEntity ConvertableToResource) : ReactiveRecord
{
    public IHasOwner ResourceOwner => ConvertableToResource.GetDescUnsafe<IHasOwner>();

    public IObservable<IChangeSet<IReactiveRustyEntity>> ObservableExchanges { get; } = ConvertablesService.Instance.ObservableExchangesToResource(ConvertableToResource.GetIndex());
    public IReadOnlyCollection<IRustyEntity> Exchanges { get; private set; }

    public IRustyEntity SelectedExchange { get; set; }

    public void InitializeDetals(CompositeDisposable disposable)
    {
        ObservableExchanges.Bind(out var list)
            .Subscribe()
            .DisposeWith(disposable);

        Exchanges = list;
    }

    public void Remove_OnClicked()
    {
        ConvertablesService.Instance.Remove(ConvertableToResource);
    }
}

