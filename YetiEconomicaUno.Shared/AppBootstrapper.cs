﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LiteDB;
using ReactiveUI;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;
using RustyDTO.Supports;
using Splat;
using YetiEconomicaCore;
using YetiEconomicaCore.Database;
using YetiEconomicaCore.Experemental;
using YetiEconomicaCore.Services;
using YetiEconomicaUno.Extensions;
using YetiEconomicaUno.Services;
using YetiEconomicaUno.View;
using YetiEconomicaUno.View.CalculateBalance;
using YetiEconomicaUno.View.CalculateBalance.Tasks;
using YetiEconomicaUno.View.Farm;
using YetiEconomicaUno.View.YetiObjects;
using YetiEconomicaUno.View.YetiObjects.PropertyBlobls;
using YetiEconomicaUno.View.YetiObjects.PropertyBlobls.Impls;
using YetiEconomicaUno.ViewModels;
using YetiEconomicaUno.ViewModels.CalculateBalance;
using YetiEconomicaUno.ViewModels.CalculateBalance.Progress;
using YetiEconomicaUno.ViewModels.Convertables;
using YetiEconomicaUno.ViewModels.Crafts;
using YetiEconomicaUno.ViewModels.Farm;
using YetiEconomicaUno.ViewModels.YetiObjects;

namespace YetiEconomicaUno;

public static class AppBootstrapper
{
    public static void Initialize()
    {
        var applicationData =Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var databasePath = Path.Combine(applicationData, "YetiEconomica", "database");

        ConfigureBsonMapper();

        var database = new LiteDatabase(databasePath);

        Locator.CurrentMutable.RegisterConstant(new DatabaseRepository(database));
        Locator.CurrentMutable.RegisterConstant(RustyEntityService.Instance);
        RustyEntityService.Instance.Initialize(null);

        Locator.CurrentMutable.RegisterConstant(ResourceService.Instance);
        Locator.CurrentMutable.RegisterConstant(CraftService.Instance);
        Locator.CurrentMutable.RegisterConstant(ConvertablesService.Instance);
        Locator.CurrentMutable.RegisterConstant(PlantsService.Instance);
        Locator.CurrentMutable.RegisterConstant(CalculateMinigamesService.Instance);
        Locator.CurrentMutable.RegisterConstant(CalculateBalanceService.Instance);

        RustyEntityService.Instance.ValidateEntities();

        RegisterMVVM();
    }

    private static void RegisterMVVM()
    {
        RegisterView<FirstPage, FirstViewModel>("First");
        RegisterView<ResourcesPage, ResourcesViewModel>("Resources");
        RegisterView<CraftsPage, CraftsViewModel>("Crafts");
        RegisterView<ResourcesConvertiblePage, ResourcesConvertiblePageViewModel>("Convertable");
        RegisterView<PlantsPage, PlantsPageViewModel>("Plants");
        RegisterView<PlantObstaclesPage, PlantObstaclesViewModel>("PlantObstacles");
        RegisterView<YetiObjectsPage, YetiObjectsPageViewModel>("YetiObjects");
        RegisterView<MineConfigPage, MineConfigViewModel>("MineConfigs");
        RegisterView<CalculateBalancePage, CalculateBalanceViewModel>("CalculateBalance");

        Locator.CurrentMutable.Register(static () => new PlantInfoView(), typeof(IViewFor<CreateYetiObjectTask>));
        Locator.CurrentMutable.Register(static () => new YetiObjectTaskView(), typeof(IViewFor<CreateYetiObjectTask>));
        Locator.CurrentMutable.Register(static () => new ConvertTaskView(), typeof(IViewFor<ConvertTask>));
        Locator.CurrentMutable.Register(static () => new CraftTaskView(), typeof(IViewFor<CraftTask>));
        Locator.CurrentMutable.Register(static () => new FarmPlantTaskView(), typeof(IViewFor<FarmPlantTask>));
        Locator.CurrentMutable.Register(static () => new RecycleResourcesTaskView(), typeof(IViewFor<RecycleResourcesTask>));
        Locator.CurrentMutable.Register(static () => new ResourceGiftView(), typeof(IViewFor<ResourceGiftTask>));
        
        RegisterPropertyView<DependentsBlobView, IHasDependents>(DescPropertyType.HasDependents);
        RegisterPropertyView<InBuildBlobView, IInBuildProcess>(DescPropertyType.InBuildProcess);
        RegisterPropertyView<MineSizeBlobView, IMineSize>(DescPropertyType.MineSize);
        RegisterPropertyView<ToolInfoBlobView, IToolSettings>(DescPropertyType.ToolSettings);
        RegisterPropertyView<TearBlobView, IHasOwner >(DescPropertyType.HasOwner);
        RegisterPropertyView<LongExecutionBlobView, ILongExecution>(DescPropertyType.LongExecution);
        RegisterPropertyView<RewardCountBlobView, IHasSingleReward>(DescPropertyType.HasSingleReward);
        RegisterPropertyView<FarmExpansionBlobView, IFarmExpansion>(DescPropertyType.FarmExpansion);
        RegisterPropertyView<TakeSpaceBlob, ITakeSpace>(DescPropertyType.TakeSpace);
        RegisterPropertyView<CraftingQueueBlobView, IHasCraftingQueue>(DescPropertyType.HasCraftingQueue);
        RegisterPropertyView<PrestigeBlobView, IHasPrestige>(DescPropertyType.HasPrestige);
        RegisterPropertyView<CitySizeBlobView, ICitySize>(DescPropertyType.CitySize);
        RegisterPropertyView<HexMaskBlobView, IHexMask>(DescPropertyType.HexMask);
        RegisterPropertyView<SubGroupBlobView, ISubGroup>(DescPropertyType.SubGroup);

        RegisterPropertyView<CraftSpeedBlobView, ICraftSpeed>(DescPropertyType.CraftSpeed);
        RegisterPropertyView<TechSpeedBlobView, ITechSpeed>(DescPropertyType.TechSpeed);

        RegisterPropertyView<EnemyPowerBlobView, IPveEnemyPower>(DescPropertyType.PveEnemyPower);
        RegisterPropertyView<PveEnemyUnitsBlobView, IPveEnemyUnits>(DescPropertyType.PveEnemyUnits);
        RegisterPropertyView<PveArmyImprovementBlobView, IPveArmyImprovement>(DescPropertyType.PveArmyImprovement);
    }

    public static Type PropertyViewerType { get; } = typeof(PropertyViewer);

    private class PropertyViewer
    {
    }

    private static void ConfigureBsonMapper()
    {
        var mapper = BsonMapper.Global;
        mapper.EnumAsInteger = true;

        mapper.RegisterType(
                static value => new BsonDocument(new Dictionary<string, BsonValue> { {"Index", value.Resource.GetIndex() }, { "Value", value.Value } }),
                static doc => new ResourceStackRecord(RustyEntityService.Instance.GetEntity(doc["Index"].AsInt32), doc["Value"].AsDouble));

        mapper.RegisterType(
            static value => new BsonDocument(new Dictionary<string, BsonValue> { { "Index", value.Resource.GetIndex() }, { "Value", value.Value } }),
            static doc => new ResourceStack(RustyEntityService.Instance.GetEntity(doc["Index"].AsInt32), doc["Value"].AsDouble));

        mapper.RegisterType(static entity => entity?.Index ?? int.MinValue, static value => RustyEntityService.Instance.GetOptionEntity(value.AsInt32));

        var reactiveMembers = new HashSet<string>
        {
            nameof(ReactiveObject.Changed),
            nameof(ReactiveObject.Changing),
            nameof(ReactiveObject.ThrownExceptions)
        };

        var progressTask = typeof(ProgressTask);

        var reactiveModel = typeof(IDescProperty);

        var types = Assembly.GetAssembly(progressTask).DefinedTypes
            .Where(type => !type.IsAbstract && type.IsSubclassOf(progressTask));

        types = types.Concat(Assembly.GetAssembly(typeof(RustyEntityService)).DefinedTypes
            .Where(type => !type.IsAbstract && type.ImplementedInterfaces.Contains(reactiveModel)));

        foreach (var type in types)
            mapper.IgnoreForType(type.AsType());

        mapper.Entity<ProgressTask>()
            .Ctor(FactoryProgressTask);

        mapper.Entity<MineProportions>()
            .IgnoreReactive();
        mapper.Entity<ReactiveVector2Int>()
            .IgnoreReactive();
        mapper.Entity<BalanceConfig>()
            .IgnoreReactive();

        mapper.Entity<ProgressTask>()
            .IgnoreReactive();

        mapper.Entity<ResourceStackForRecord>()
            .Ctor(static doc => ResourceStackForRecord.Parse(doc));
        
        mapper.Entity<ItemOfGroupInfo>()
            .Id(static value => value.Index);
    }

    public static ProgressTask FactoryProgressTask(BsonDocument doc)
    {
        var type = (ProgressType)(doc[nameof(ProgressTask.Type)].AsInt32);
        var mapper = BsonMapper.Global;

        IRustyEntity entity;
        switch (type)
        {
            case ProgressType.YetiObject:
                if (RustyEntityService.Instance.TryGetEntity(doc[nameof(CreateYetiObjectTask.Index)], out entity))
                    return new CreateYetiObjectTask(entity);
                return null;
            case ProgressType.ResourceGift:
                return new ResourceGiftTask(mapper.Deserialize<IEnumerable<ResourceStackRecord>>(doc[nameof(ResourceGiftTask.Resources)]));
            case ProgressType.RecycleResources:
                return new RecycleResourcesTask(mapper.Deserialize<IEnumerable<ResourceStackRecord>>(doc[nameof(RecycleResourcesTask.Resources)]));
            case ProgressType.Craft:
                if (RustyEntityService.Instance.TryGetEntity(doc[nameof(CraftTask.Index)].AsInt32, out entity))
                    return new CraftTask(entity, doc[nameof(CraftTask.Count)].AsInt32);
                return null;
            case ProgressType.Convert:
                if (RustyEntityService.Instance.TryGetEntity(doc["Index"].AsInt32, out entity))
                    return new ConvertTask(entity, doc["Count"].AsInt32);
                return null;
            case ProgressType.FarmPlant:
                return new FarmPlantTask(mapper.Deserialize<IEnumerable<ResourceStackRecord>>(doc[nameof(FarmPlantTask.Targets)]));
            default:
                throw new NotImplementedException("Unknown ProgressTask!");
        }
    }

    #region MVVM

    public static void RegisterPropertyView<TView, TViewModel>(DescPropertyType type) 
        where TView : IViewFor<TViewModel>, new()
        where TViewModel : class, IDescProperty
    {
        Locator.CurrentMutable.Register(static () => new TView(), PropertyViewerType, type.ToString());
    }


    private static void RegisterView<TView, TViewModel>()
        where TView : IViewFor<TViewModel>, new()
        where TViewModel : class, IReactiveObject
    {
        Locator.CurrentMutable.Register(static () => new TView(), typeof(IViewFor<TViewModel>));
    }

    private static void RegisterView<TView, TViewModel>(string name)
        where TView : IViewFor<TViewModel>, new()
        where TViewModel : BaseViewModel, new()
    {
        RegisterView<TViewModel>(static () => new TView());
        RegisterViewModel(() =>
        {
            return new TViewModel().InjectURI(name);
        }, name);
    }

    private static void RegisterView<TViewModel>(string name, Func<object> viewFactory, Func<IRoutableViewModel> viewModelFactory)
        where TViewModel : class, IRoutableViewModel, new()
    {
        RegisterView<TViewModel>(viewFactory);
        RegisterViewModel(viewModelFactory, name);
    }

    private static void RegisterView<TViewModel>(Func<object> factory)
        where TViewModel : class, IRoutableViewModel, new()
    {
        Locator.CurrentMutable.Register(factory, typeof(IViewFor<TViewModel>));
    }

    private static void RegisterViewModel(Func<IRoutableViewModel> func, string name)
    {
        Locator.CurrentMutable.Register(func, name);
    }

    #endregion
}