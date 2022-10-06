using System.Collections;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using LiteDB;
using Nito.Comparers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;

namespace YetiEconomicaCore.Services;

public class PlantsService
{
    private static readonly Lazy<PlantsService> _instance = new (static () => new ());
    public static PlantsService Instance => _instance.Value;

    public int DefaultCellsCount
    {
        get => _config.DefaultCellsCount;
        set => _config.DefaultCellsCount = value;
    }

    public IObservable<Func<IRustyEntity, bool>> PlantFilter { get; }
    public IObservable<Func<IRustyEntity, bool>> ResourceIsPlantFilter { get; }
    public IObservable<IChangeSet<IReactiveRustyEntity>> ObservablePlants { get; }

    private Dictionary<IRustyEntity, IRustyEntity> _plants = new();


    private IReadOnlyList<IRustyEntity> _obstaclesByIndex;

    private readonly PlantServiceConfig _config;

    private class PlantServiceConfig : ReactiveObject
    {
        [BsonCtor]
        public PlantServiceConfig(string id)
        {
            ID = id;
        }

        [BsonId]
        public string ID { get; }

        [Reactive]
        public int DefaultCellsCount { get; set; } = 6;
    }

    private PlantsService()
    {
        _config = DatabaseRepository.Instance.GetConfig("plant_service", key => new PlantServiceConfig(key), null);
        ObservablePlants = RustyEntityService.Instance.ConnectToEntity(static entity => entity.Type is RustyEntityType.PlantTask).RemoveKey();
        PlantFilter = Observable.Return<Func<IRustyEntity, bool>>(static entity => entity.Type is RustyEntityType.PlantTask);
        ResourceIsPlantFilter = Observable.Return(IsPlant);


        var sortByIndex = ComparerBuilder.For<IRustyEntity>()
            .OrderBy(static entity => entity.GetIndex());

        RustyEntityService.Instance.ConnectToEntity(static entity => entity.Type is RustyEntityType.FarmObstacleClearing)
            .RemoveKey()
            .Sort(sortByIndex)
            .Bind(out var list)
            .Subscribe();
        _obstaclesByIndex = list;

        ObservablePlants.Subscribe(Plants_OnChanged);
    }

    private void Plants_OnChanged(IChangeSet<IReactiveRustyEntity> diffs)
    {
        void OnAdd(IRustyEntity entity)
        {
            _plants.Add(entity.GetDescUnsafe<IHasSingleReward>().Entity, entity);
        }

        void OnRemove(IRustyEntity entity)
        {
            _plants.Remove(entity.GetDescUnsafe<IHasSingleReward>().Entity);
        }

        foreach (var diff in diffs)
        {
            switch (diff.Reason)
            {
                case ListChangeReason.AddRange:
                    foreach (var entity in diff.Range)
                        OnAdd(entity);
                    break;
                case ListChangeReason.RemoveRange:
                    foreach (var entity in diff.Range)
                        OnRemove(entity);
                    break;
                case ListChangeReason.Clear:
                    _plants.Clear();
                    break;
                case ListChangeReason.Add:
                    OnAdd(diff.Item.Current);
                    break;
                case ListChangeReason.Remove:
                    OnRemove(diff.Item.Current);
                    break;
            }
        }
    }

    public bool TryGetPlant(IRustyEntity resource, out IRustyEntity? plant)
    {
        return _plants.TryGetValue(resource, out plant);
    }

    public bool IsPlant(IRustyEntity resource)
    {
        return _plants.ContainsKey(resource);
    }

    public void CreateObstacle()
    {
        var service = RustyEntityService.Instance;
        service.Create(RustyEntityType.FarmObstacleClearing, null);
    }

    public void Create(IRustyEntity source)
    {
        if (source is null)
            return;
        RustyEntityService.Instance.Create(RustyEntityType.PlantTask, null, EntityBuildOptions.CreateWithEntity(source));
    }

    public void Remove(IRustyEntity source)
    {
        if (source is null)
            return;
        RustyEntityService.Instance.Remove(source);
    }

    public IRustyEntity GetPlantEntity(IRustyEntity resource)
    {
        return _plants[resource];
    }

    public IList GetObstaclesAsObservable(CompositeDisposable disposable)
    {
        var sortByIndex = ComparerBuilder.For<IRustyEntity>()
            .OrderBy(static entity => entity.GetIndex());
        RustyEntityService.Instance.ConnectToEntity(entity => entity.Type is RustyEntityType.FarmObstacleClearing)
            .Sort(sortByIndex)
            .Bind(out var list)
            .Subscribe()
            .DisposeWith(disposable);

        return list;
    }

    public IReadOnlyList<IRustyEntity> GetObstaclesAsList()
    {
        return _obstaclesByIndex;
    }

    public int GetObstacleIndex(IRustyEntity rustyEntity)
    {
        return _obstaclesByIndex.IndexOf(rustyEntity);
    }
}
