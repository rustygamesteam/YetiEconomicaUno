using DynamicData;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using RustyDTO.PropertyModels;
using YetiEconomicaCore.Services;
using RustyDTO.Interfaces;

namespace YetiEconomicaUno.ViewModels.Convertables;

public record ConvertableViewModel(IRustyEntity ConvertableToResource) : ReactiveRecord
{
    public IHasOwner ResourceOwner => ConvertableToResource.GetUnsafe<IHasOwner>();

    public IObservable<IChangeSet<IRustyEntity>> ObservableExchanges { get; } = ConvertablesService.Instance.ObservableExchangesToResource(ConvertableToResource.Index);
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

