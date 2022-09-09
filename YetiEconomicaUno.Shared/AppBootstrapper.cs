using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LiteDB;
using ReactiveUI;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;
using Splat;
using YetiEconomicaCore;
using YetiEconomicaCore.Database;
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
        //RegisterPropertyView<SpeedBootBlobView, IBoostSpeed>(DescPropertyType.BoostSpeed);
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
        

    }

    public static Type PropertyViewerType { get; } = typeof(PropertyViewer);

    private class PropertyViewer
    {
    }

    private static void ConfigureBsonMapper()
    {
        var mapper = BsonMapper.Global;

        mapper.RegisterType(
                static value => new BsonDocument(new Dictionary<string, BsonValue> { {"Index", value.Resource.GetIndex() }, { "Value", value.Value } }),
                static doc => new ResourceStackRecord(RustyEntityService.Instance.GetEntity(doc["Index"].AsInt32), doc["Value"].AsDouble));

        mapper.RegisterType(
            static value => new BsonDocument(new Dictionary<string, BsonValue> { { "Index", value.Resource.GetIndex() }, { "Value", value.Value } }),
            static doc => new ResourceStack(RustyEntityService.Instance.GetEntity(doc["Index"].AsInt32), doc["Value"].AsDouble));

        mapper.RegisterType(static entity => entity?.ID.Index ?? -1, static value => RustyEntityService.Instance.GetOptionEntity(value.AsInt32));

        mapper.RegisterType(
            serialize: static s => ((int)s),
            deserialize: static bson => (RustyEntityType)bson.AsInt32);

        mapper.RegisterType(
            serialize: static s => (int)s,
            deserialize: static bson => (ProgressType)bson.AsInt32);


        var reactiveMembers = new HashSet<string>
        {
            nameof(ReactiveObject.Changed),
            nameof(ReactiveObject.Changing),
            nameof(ReactiveObject.ThrownExceptions)
        };

        var getEntityMapperFunc = typeof(BsonMapper).GetMethod("GetEntityMapper", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        var progressTask = typeof(ProgressTask);
        var types = Assembly.GetAssembly(progressTask).DefinedTypes
            .Where(type => type.IsSubclassOf(progressTask) && !type.IsAbstract);

        foreach (var type in types)
        {
            var entity = (EntityMapper)getEntityMapperFunc.Invoke(mapper, new object[] { type.AsType() });

            var members = entity.Members;
            for(int i = members.Count - 1; i >= 0; i--)
            {
                if (reactiveMembers.Contains(members[i].MemberName))
                    members.RemoveAt(i);
            }
        }

        mapper.Entity<ProgressTask>()
            .Ctor(FactoryProgressTask);

        mapper.Entity<MineProportions>()
            .IgnoreReactive();
        mapper.Entity<ReactiveVector2Int>()
            .IgnoreReactive();
        mapper.Entity<TimeTarget>()
            .IgnoreReactive();

        mapper.Entity<ProgressTask>()
            .IgnoreReactive();

        mapper.Entity<SessionTimeRecord>()
            .Id(static x => x.ID)
            .Ctor(static doc => new SessionTimeRecord(doc["_id"], doc[nameof(SessionTimeRecord.Hour)]));

        mapper.Entity<ResourceStackForRecord>()
            .Ctor(static doc => ResourceStackForRecord.Parse(doc));
        
        mapper.Entity<ItemOfGroupInfo>()
            .Id(static value => value.Index);
    }

    private static ProgressTask FactoryProgressTask(BsonDocument doc)
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