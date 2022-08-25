using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using YetiEconomicaUno.ViewModels.CalculateBalance;
using YetiEconomicaUno.ViewModels.CalculateBalance.Progress;
using YetiEconomicaCore.Services;
using YetiEconomicaUno.Controls.Resources;
using YetiEconomicaUno.Helpers;
using ReactiveUI;
using System.Reactive.Linq;
using System.Text;
using YetiEconomicaUno.View.YetiObjects;
using DynamicData.Binding;
using RustyDTO;
using RustyDTO.Interfaces;
using RustyDTO.PropertyModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.CalculateBalance;

public sealed partial class CreateUserTargetDialogPopup : Page, IDisposable
{
    public enum Result
    {
        RequiredInDependencies,
        Craft,
        FarmPlants,
        PlantCellsExpansion,
        Convert,
        UpgradeTool,
        Gift,
        Recycle
    }

    private ObservableCollectionExtended<ResourceStack> _giftExchanges = new();
    private ObservableCollectionExtended<ResourceStack> _recycleExchanges = new();
    private ObservableCollectionExtended<ResourceStack> _plantExchanges = new();

    private readonly HashSet<int> _yetiObjectsHashSet;
    private readonly UserDataDump UserDataDump;
    private IReadOnlyList<IRustyEntity> _plantObstacles;
    private int _nextPlantObstable = -1;

    public CreateUserTargetDialogPopup(HashSet<int> yetiObjectsHashSet, UserDataDump userDataDump)
    {
        _yetiObjectsHashSet = yetiObjectsHashSet;
        UserDataDump = userDataDump;

        this.InitializeComponent();
    }

    internal ProgressTask GetResult()
    {
        return (Result)TypeComboBox.SelectedIndex switch
        {
            Result.RequiredInDependencies => new CreateYetiObjectTask(BuildOrTechSelector.SelectedValue.Index),
            Result.Craft => new CraftTask(CraftService.Instance.GetCraftFor(CraftBox.ResourceBox.SelectedValue).Index, CraftBox.Count),
            Result.Convert => new ConvertTask(ConvertFromBox.SelectedValue, (int)ConvertToCountBox.Value),
            Result.Gift => new ResourceGiftTask(_giftExchanges),
            Result.Recycle => new RecycleResourcesTask(_recycleExchanges),
            Result.FarmPlants => new FarmPlantTask(_plantExchanges),
            Result.UpgradeTool => new CreateYetiObjectTask(BuildOrTechSelector.SelectedValue.Index),
            Result.PlantCellsExpansion => new CreateYetiObjectTask(_plantObstacles[_nextPlantObstable].Index),
            _ => null,
        };
    }

    internal bool HasResult()
    {
        return (Result)TypeComboBox.SelectedIndex switch
        {
            Result.RequiredInDependencies => BuildOrTechSelector.SelectedValue is not null,
            Result.Craft => CraftBox.ResourceBox.SelectedValue is not null && CraftBox.Count > 0,
            Result.Convert => ConvertFromBox.SelectedValue is not null && ConvertToCountBox.Value > 0,
            Result.Gift => _giftExchanges.Count > 0,
            Result.Recycle => _recycleExchanges.Count > 0,
            Result.FarmPlants => _plantExchanges.Count > 0,
            Result.UpgradeTool => ToolSelector.SelectedValue is not null && UserDataDump.TryGetTool(ToolsHelper.GetGroupType(ToolSelector.SelectedValue), out _),
            Result.PlantCellsExpansion => _nextPlantObstable != -1,
            _ => false
        };
    }

    private bool IsInBuildTest(IRustyEntity entity)
    {
        if (!entity.TryGetProperty(out IInBuildProcess inBuildProcess))
            return true;
        
        return HasEntity(inBuildProcess.Build);
    }

    private bool IsDependentsTest(IRustyEntity entity)
    {
        if (!entity.TryGetProperty(out IHasDependents dependents))
            return true;

        return HasEntity(dependents.Required) && HasEntity(dependents.VisibleAfter);
    }

    private void CraftResourceBox_Loaded(object sender, RoutedEventArgs e)
    {
        var craftBox = (ResourceWithCount)sender;

        craftBox.ResourceBox.Filter = Observable.Return<Func<IRustyEntity, bool>>(resource =>
        {
            if (!CraftService.Instance.TryGetCraft(resource, out var craft))
                return false;

            return IsInBuildTest(craft) && IsDependentsTest(craft);
        });

        craftBox.Count = 1;

        var disposable = craftBox.ResourceBox.WhenAnyValue(static value => value.SelectedValue)
            .CombineLatest(craftBox.WhenAnyValue(static view => view.Count))
            .Subscribe(value =>
            {
                var items = CraftService.Instance.GetCraftPrice(value.First, value.Second);
                CraftPriceBox.ItemsSource = items;
                CraftPriceBox.Visibility = value.First == null || value.Second == 0
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            });

        void OnCraftBoxOnUnloaded(object o, RoutedEventArgs args)
        {
            disposable.Dispose();
            craftBox.Unloaded -= OnCraftBoxOnUnloaded;
        }

        craftBox.Unloaded += OnCraftBoxOnUnloaded;
    }

    private void PlantCellsExpainsion_OnLoad(object sender, RoutedEventArgs e)
    {
        if (_plantObstacles == null)
        {
            _plantObstacles = PlantsService.Instance.GetObstaclesAsList();
            _nextPlantObstable = -1;
            for (var index = 0; index < _plantObstacles.Count; index++)
            {
                var obstacle = _plantObstacles[index];
                if (_yetiObjectsHashSet.Contains(obstacle.Index))
                    continue;
                _nextPlantObstable = index;
                break;
            }

            if(_nextPlantObstable == -1)
                PlantCellsExpansionInfo.Text = "Not found next plant cells expanded!";
            else
            {
                var entity = _plantObstacles[_nextPlantObstable];
                var price = entity.GetUnsafe<IPayable>().Price;

                var sb = new StringBuilder();
                sb.Append("Next plant cells expanded: ");
                sb.Append(entity.GetUnsafe<IFarmExpansion>().Count);
                if (price.Count > 0)
                {

                    sb.AppendLine();
                    sb.AppendLine();
                    sb.Append("Price:");
                    foreach (var resourceStack in price)
                    {
                        sb.Append("\n  • ");
                        sb.Append(resourceStack.Resource.FullName);
                        sb.Append(' ');
                        sb.Append(Math.Ceiling(resourceStack.Value));
                    }
                }
                PlantCellsExpansionInfo.Text = sb.ToString();
            }
        }
    }

    private void ConvertArea_Loaded(object sender, RoutedEventArgs e)
    {
        ConvertToBox.Filter = ConvertablesService.Instance.ExchangesToFilter;

        var filterFrom = ConvertToBox.WhenAnyValue(static view => view.SelectedValue).Select(static resourceTo =>
            {
                var fromFilter = new HashSet<int>();
                if (resourceTo == null)
                    return static enity => false;

                foreach (var rustyEntity in RustyEntityService.Instance.EntitesWhereType(RustyEntityType.Exchage))
                {
                    var exchange = rustyEntity.GetUnsafe<IHasExchange>();
                    if (exchange.ToEntity == resourceTo)
                        fromFilter.Add(exchange.FromEntity.Index);
                }

                return (Func<IRustyEntity, bool>)(entity => fromFilter.Contains(entity.Index));
            });

        ConvertFromBox.Filter = filterFrom;
        ConvertToCountBox.Value = 1;

        var exchangeChanged = ConvertFromBox.WhenAnyValue(static view => view.SelectedValue).Select(_ =>
        {
            return ConvertablesService.Instance.GetExchange(ConvertFromBox.SelectedValue, ConvertToBox.SelectedValue);
        });

        var disposable = exchangeChanged
            .CombineLatest(ConvertToCountBox.WhenAnyValue(static view => view.Value))
            .Subscribe(value =>
            {
                ConvertPriceBox.Visibility = value.First is null || value.Second == 0 ? Visibility.Collapsed : Visibility.Visible;
                ConvertPriceBox.ItemsSource = ConvertablesService.Instance.GetPriceInfo(value.First, (int)value.Second);
            });

        
        void ConvertToBox_OnUnloaded(object o, RoutedEventArgs args)
        {
            disposable.Dispose();
            ConvertToBox.Unloaded -= ConvertToBox_OnUnloaded;
        }

        ConvertToBox.Unloaded += ConvertToBox_OnUnloaded;
    }

    private void FarmPlantList_Loaded(object sender, RoutedEventArgs e)
    {
        FarmPlantList.Filter = PlantsService.Instance.IsPlant;
    }

    private void YetiObjectSelector_Loaded(object sender, RoutedEventArgs e)
    {
        ((YetiObjectSelector)sender).Filter = Observable.Return<Func<IRustyEntity, bool>>(entity =>
        {
            if (_yetiObjectsHashSet.Contains(entity.Index))
                return false;
            if (entity.Type is RustyEntityType.Tech && entity.TryGetProperty(out IInBuildProcess inBuild) && !HasEntity(inBuild.Build))
                return false;

            return IsDependentsTest(entity);
        });
    }

    private bool HasEntity(IRustyEntity entity)
    {
        return entity is null || _yetiObjectsHashSet.Contains(entity.Index);
    }

    public void Dispose()
    {
        _plantObstacles = null;
        Bindings.StopTracking();
    }
}