using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Nito.Comparers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RustyDTO.Interfaces;
using RustyDTO.PropertyModels;
using YetiEconomicaCore.Services;

namespace YetiEconomicaUno.ViewModels.Convertables;

public class ResourcesConvertiblePageViewModel : BaseViewModel
{
    private static ConvertablesService Service { get; } = ConvertablesService.Instance;

    [Reactive]
    public IRustyEntity NewConvrtable { get; set; }

    [Reactive]
    public string SearchMask { get; set; }

    internal ObservableCollectionExtended<ConvertableViewModel> ItemSource { get; } = new();

    public ResourcesConvertiblePageViewModel()
    {
    }

    public void Initialize(CompositeDisposable disposable)
    {
        var sort = ComparerBuilder.For<IRustyEntity>()
            .OrderBy(static resource => resource.GetUnsafe<IHasOwner>().Owner.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static resource => resource.GetUnsafe<IHasOwner>().Tear)
            .ThenBy(static resource => resource.DisplayName, StringComparer.OrdinalIgnoreCase);

        Service.ExchangesTo.AsObservableChangeSet()
            .Sort(sort)
            .Transform(entity => new ConvertableViewModel(entity))
            .Bind(ItemSource)
            .Subscribe()
            .DisposeWith(disposable);
    }

    public void AddConvrtable_OnClicked()
    {
        Service.Add(NewConvrtable);
        NewConvrtable = null;
    }

    private bool OnFilter(IRustyEntity data)
    {
        return string.IsNullOrWhiteSpace(SearchMask) || data.GetUnsafe<ILinkTo>().Entity.FullName.Contains(SearchMask, StringComparison.OrdinalIgnoreCase);
    }
}
