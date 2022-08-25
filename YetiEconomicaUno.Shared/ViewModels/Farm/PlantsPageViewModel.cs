using DynamicData.Binding;
using ReactiveUI.Fody.Helpers;
using System;
using YetiEconomicaCore.Services;
using System.Reactive.Disposables;
using Nito.Comparers;
using System.Reactive.Linq;
using DynamicData;
using RustyDTO.Interfaces;
using RustyDTO.PropertyModels;

namespace YetiEconomicaUno.ViewModels.Farm;

public class PlantsPageViewModel : BaseViewModel
{
    private static PlantsService Service { get; } = PlantsService.Instance;

    [Reactive]
    public IRustyEntity NewResource { get; set; }
    [Reactive]
    public string SearchMask { get; set; }
    internal ObservableCollectionExtended<PlantInfoViewModel> ItemSource { get; } = new();


    public void Initialize(CompositeDisposable disposables)
    {
        var sort = ComparerBuilder.For<IRustyEntity>()
            .OrderBy(static data => data.FullName);

        Service.ObservablePlants
            .Filter(this
                .WhenValueChanged(static x => x.SearchMask)
                .Select(_ => (Func<IRustyEntity, bool>)OnFilter))
            .Sort(sort)
            .Transform(static entity => new PlantInfoViewModel(entity))
            .Bind(ItemSource)
            .Subscribe()
            .DisposeWith(disposables);
    }

    internal void TryAdd()
    {
        if (NewResource == null)
            return;
        Service.Create(NewResource);
        NewResource = null;
    }

    private bool OnFilter(IRustyEntity data)
    {
        return string.IsNullOrWhiteSpace(SearchMask) || data.GetUnsafe<IHasSingleReward>().Entity.FullName.Contains(SearchMask, StringComparison.OrdinalIgnoreCase);
    }
}
