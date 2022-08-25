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

namespace YetiEconomicaUno.ViewModels.Crafts;

public class CraftsViewModel : BaseViewModel
{
    private static CraftService Service { get; } = CraftService.Instance;

    [Reactive]
    public IRustyEntity NewResource { get; set; }

    [Reactive]
    public string SearchMask { get; set; }

    internal ObservableCollectionExtended<IRustyEntity> ItemSource { get; } = new();

    public CraftsViewModel()
    {

    }

    public void Intialize(CompositeDisposable disposable)
    {
        var sort = ComparerBuilder.For<IRustyEntity>()
            .OrderBy(static entity => entity.GetUnsafe<IHasSingleReward>().Entity.GetUnsafe<IHasOwner>().Owner.DisplayName, StringComparer.Ordinal)
            .ThenBy(static entity => entity.GetUnsafe<IHasSingleReward>().Entity.GetUnsafe<IHasOwner>().Tear)
            .ThenBy(static entity => entity.GetUnsafe<IHasSingleReward>().Entity.DisplayName, StringComparer.Ordinal);

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
        return string.IsNullOrWhiteSpace(SearchMask) || caftEntity.GetUnsafe<IHasSingleReward>().Entity.FullName.Contains(SearchMask, StringComparison.OrdinalIgnoreCase);
    }
}