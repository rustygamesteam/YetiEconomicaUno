using DynamicData;
using DynamicData.Binding;
using Nito.Comparers;
using ReactiveUI.Fody.Helpers;
using RustyDTO.Interfaces;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using RustyDTO;
using YetiEconomicaCore.Services;

namespace YetiEconomicaUno.ViewModels.YetiObjects;

public class YetiObjectsPageViewModel : BaseViewModel
{
    private static RustyEntityService Service { get; } = RustyEntityService.Instance;

    [Reactive]
    public string NewName { get; set; }
    [Reactive]
    public RustyEntityType NewType { get; set; }

    [Reactive]
    public string SearchMask { get; set; }

    internal ObservableCollectionExtended<IRustyEntity> ItemSource { get; } = new();

    public void Initialize(CompositeDisposable disposable)
    {
        var sort = ComparerBuilder.For<IRustyEntity>()
            .OrderBy(static data => data.Type)
            .ThenBy(static data => data.DisplayName);

        var mask = EntityDependencies.ToConstantBitmask(stackalloc[] {RustyEntityType.UniqueBuild, RustyEntityType.UniqueTool, RustyEntityType.Tech, RustyEntityType.PVE});

        Service.ConnectToEntity(model => mask.Get(model.TypeAsIndex))
            .Filter(this
                .WhenValueChanged(static x => x.SearchMask)
                .Select(_ => (Func<IRustyEntity, bool>)OnFilter))
            .Sort(sort)
            .Bind(ItemSource)
            .Subscribe()
            .DisposeWith(disposable);
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
