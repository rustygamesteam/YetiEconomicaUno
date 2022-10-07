using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Nito.Comparers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;
using YetiEconomicaCore.Services;

namespace YetiEconomicaUno.ViewModels.Crafts;

public class CraftsViewModel : BaseViewModel
{
    private static CraftService Service { get; } = CraftService.Instance;

    [Reactive]
    public IRustyEntity NewResource { get; set; }

    [Reactive]
    public string SearchMask { get; set; }

    internal ObservableCollectionExtended<IReactiveRustyEntity> ItemSource { get; } = new();

    public CraftsViewModel()
    {

    }

    public void Intialize(CompositeDisposable disposable)
    {
        var sort = ComparerBuilder.For<IRustyEntity>()
            .OrderBy(static entity => entity.GetDescUnsafe<IHasSingleReward>().Entity.GetDescUnsafe<IHasOwner>().Owner.DisplayName, StringComparer.Ordinal)
            .ThenBy(static entity => entity.GetDescUnsafe<IHasSingleReward>().Entity.GetDescUnsafe<IHasOwner>().Tear)
            .ThenBy(static entity => entity.GetDescUnsafe<IHasSingleReward>().Entity.DisplayName, StringComparer.Ordinal);

        var filter = this
            .WhenValueChanged(static x => x.SearchMask)
            .Select(_ => (Func<IRustyEntity, bool>) OnFilter);

        Service.ObservableCrafts
            .Filter(filter)
            .Sort(sort)
            .Bind(ItemSource)
            .Subscribe()
            .DisposeWith(disposable);
    }

    public void AddReciept_OnClicked()
    {
        Service.Create(NewResource);
        NewResource = null;
    }

    private bool OnFilter(IRustyEntity caftEntity)
    {
        return string.IsNullOrWhiteSpace(SearchMask) || caftEntity.GetDescUnsafe<IHasSingleReward>().Entity.FullName.Contains(SearchMask, StringComparison.OrdinalIgnoreCase);
    }
}