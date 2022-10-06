using System.Collections;
using System.Reactive.Linq;
using System.Text.Json;
using DynamicData;
using LiteDB;
using ReactiveUI;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;
using RustyDTO.Supports;
using YetiEconomicaCore.Database;
using YetiEconomicaCore.Descriptions;
using YetiEconomicaCore.Experemental;
using YetiEconomicaCore.ReactiveImpl;

namespace YetiEconomicaCore.Services;

public class RustyEntityService : IEntityService, IDatabaseChunkConvertable<ResourceStack, ResourceStackForRecord>
{
    private RustyEntityService()
    {

    }

    private static readonly Lazy<RustyEntityService> _instance = new (static () => new ());
    public static RustyEntityService Instance => _instance.Value;

    internal DynamicEntityDatabase Entities { get; private set; }

    internal DynamicLiteChankListCollection<ResourceStack, ResourceStackForRecord> Prices { get; private set; }
    internal DynamicLiteChankListCollection<ResourceStack, ResourceStackForRecord> FakePrices { get; private set; }
    internal DynamicLiteChankListCollection<ResourceStack, ResourceStackForRecord> Rewards { get; private set; }
    internal DynamicLiteCacheCollection<IHasOwner, ItemOfGroupInfo> ItemsOfGroup { get; private set; }
    internal DynamicLazyPropertiesDatabase Properties { get; private set; }

    internal record PayableInfo(int Index, ICollection<ResourceStack> Price) : ReactiveRecord, IPayable;
    internal record FakePayableInfo(int Index, ICollection<ResourceStack> Price) : ReactiveRecord, IFakePayable;
    internal record RewardsInfo(int Index, ICollection<ResourceStack> Rewards) : ReactiveRecord, IHasRewards;

    private readonly Dictionary<int, PayableInfo> _pricesByOwners = new();
    private readonly Dictionary<int, FakePayableInfo> _fakePricesByOwners = new();
    private readonly Dictionary<int, RewardsInfo> _rewardsByOwners = new();

    public bool IsInitialize { get; private set; }

    public IRustyEntity GetEntity(int index)
    {
        return Entities[index];
    }

    public IRustyEntity? GetOptionEntity(int index)
    {
        var lookup = Entities.Lookup(index);
        return lookup.HasValue ? lookup.Value : null;
    }

    public bool TryGetEntity(int index, out IRustyEntity? entity)
    {
        var lookup = Entities.Lookup(index);
        if (lookup.HasValue)
        {
            entity = lookup.Value;
            return true;
        }

        entity = null;
        return false;
    }

    public IEnumerable<IRustyEntity> AllEntites()
    {
        return Entities;
    }

    public IEnumerable<IRustyEntity> EntitesWhere(Func<IRustyEntity, bool> where)
    {
        return Entities.Where(where);
    }

    public IEnumerable<IRustyEntity> EntitesWhereType(RustyEntityType type)
    {
        var index = (int)type;
        foreach (var entity in Entities)
        {
            if (entity.TypeAsIndex == index)
                yield return entity;
        }
    }
    
    public IEnumerable<IRustyEntity> EntitesWhereTypes(BitArray types)
    {
        foreach (var entity in Entities)
        {
            if (types.Get(entity.TypeAsIndex))
                yield return entity;
        }
    }

    public IObservable<IChangeSet<IReactiveRustyEntity, int>> PreviewToEntity(Func<IRustyEntity, bool>? filter = null)
    {
        return Entities.Preview(filter);
    }

    public IObservable<IChangeSet<IReactiveRustyEntity, int>> ConnectToEntity(Func<IRustyEntity, bool>? filter = null)
    {
        return Entities.Connect(filter);
    }

    public IEnumerable<IRustyEntity> GetPriceOwnersWithResource(IRustyEntity resource)
    {
        var index = resource.ID;
        foreach (var price in Prices.Items)
        {
            if (price.Model.Resource.ID == index)
                yield return Entities[price.Owner];
        }
    }

    internal IPayable GetPrices(int index)
    {
        return InternalGetList(index, _pricesByOwners, Prices, static (index, collection) => new PayableInfo(index, collection));
    }

    internal IFakePayable GetFakePrices(int index)
    {
        return InternalGetList(index, _fakePricesByOwners, FakePrices, static (index, collection) => new FakePayableInfo(index, collection));
    }

    internal IHasRewards GetRewards(int index)
    {
        return InternalGetList(index, _rewardsByOwners, Rewards, static (index, collection) => new RewardsInfo(index, collection));
    }

    private TResult InternalGetList<TResult>(int index, Dictionary<int, TResult> dictionary, DynamicLiteChankListCollection<ResourceStack, ResourceStackForRecord> database, Func<int, ICollection<ResourceStack>, TResult> factory)
        where TResult : ReactiveRecord, IDescProperty
    {
        if (!dictionary.TryGetValue(index, out var result))
        {
            dictionary[index] = result = factory.Invoke(index,
                new DynamicLiteCollectionByFilter<ResourceStack, ResourceStackForRecord>(database,
                    new ResourceStackChunk(index), true));
        }

        return result;
    }

    private void InternalClearList<TResult>(int index, Dictionary<int, TResult> dictionary, Func<TResult, ICollection<ResourceStack>> takeCollection)
        where TResult : ReactiveRecord, IDescProperty
    {
        if (dictionary.TryGetValue(index, out var result))
        {
            takeCollection.Invoke(result).Clear();
            dictionary.Remove(index);
        }
    }

    public void Initialize(JsonDocument? _)
    {
        var database = DatabaseRepository.Instance.Database;

        Properties = new DynamicLazyPropertiesDatabase(database, "properties", GetPropertyResolvers());
        Entities = new(database, "entities");

        ItemsOfGroup = new(database, "items_of", ReactiveHasOwnerFactory.Instance);
        Prices = new(database, "prices", this);
        Rewards = new(database, "rewards", this);

        foreach (var rustyEntity in Entities)
        {
            var index = rustyEntity.ID;
            IPropertiesAccess? access = null;

            var defaultProperties = EntityDependencies.GetRequiredProperties(rustyEntity.Type);
            foreach (var propertyType in defaultProperties)
            {
                if (rustyEntity.HasProperty(propertyType))
                    continue;

                if (Properties.HasResolve(propertyType))
                {
                    access ??= Properties.GetProperties(index.Index);
                    if (access.TryDefaultAttach(propertyType))
                        ((RustyEntity)rustyEntity).InjectProperty(propertyType);
                }
            }
        }

        foreach (var tuple in RequiredEntites())
        {
            if (!Entities.HasEntity(tuple.index))
                Create(tuple.type, tuple.displayName, EntityBuildOptions.CreateWithOwner(tuple.owner), tuple.index);
        }

        IsInitialize = true;
    }

    private IEnumerable<(int index, RustyEntityType type, string displayName, int owner)> RequiredEntites()
    {
        int index = 0;
        yield return (--index, RustyEntityType.ResourceGroup, "Main", 0);
        yield return (--index, RustyEntityType.Resource, "Wood", -1);
        yield return (--index, RustyEntityType.Resource, "Stone", -1);
        yield return (--index, RustyEntityType.Resource, "Ore", -1);
        yield return (--index, RustyEntityType.Resource, "Gold", -1);
        yield return (--index, RustyEntityType.Resource, "Food", -1);

        yield return (--index, RustyEntityType.UniqueTool, "Axe", -1);
        var axeIndex = index;
        yield return (--index, RustyEntityType.UniqueTool, "Pick", -1);
        var pickIndex = index;
        yield return (--index, RustyEntityType.UniqueTool, "Shovel", -1);
        var shovelIndex = index;

        yield return (--index, RustyEntityType.Tool, "Default axe", axeIndex);
        yield return (--index, RustyEntityType.Tool, "Default pick", pickIndex);
        yield return (--index, RustyEntityType.Tool, "Default shovel", shovelIndex);

        yield return (--index, RustyEntityType.UniqueBuild, "Mine", 0);
        var mineGroupIndex = index;
        yield return (--index, RustyEntityType.Build, "Level 1", mineGroupIndex); // Mine - level 1
    }

    private IEnumerable<KeyValuePair<DescPropertyType, IPropertyResolver>> GetPropertyResolvers()
    {
        //yield return new (DescPropertyType.HasOwner, ReactiveHasOwnerFactory.Instance); // 1
        yield return new (DescPropertyType.HasDependents, ReactiveUniversalFactory.ReactiveHasDependentsFactory()); //2
        yield return new (DescPropertyType.LongExecution, ReactiveUniversalFactory.ReactiveLongExecutionFactory()); //4
        //yield return new (DescPropertyType.BoostSpeed, ReactiveBoostInfoFactory.Instance); //5
        yield return new (DescPropertyType.ToolSettings, ReactiveUniversalFactory.ReactiveToolSettingsFactory()); //6
        yield return new (DescPropertyType.InBuildProcess, ReactiveUniversalFactory.ReactiveInBuildProcessFactory()); //7
        yield return new (DescPropertyType.HasSingleReward, ReactiveSingleRewardFactory.Instance); //8
        yield return new (DescPropertyType.HasExchange, ReactiveExchangeFactory.Instance); //9
        yield return new (DescPropertyType.MineSize, ReactiveUniversalFactory.ReactiveMineSizeFactory().With(("x", size => size.X), ("y", size => size.Y))); //11
        yield return new (DescPropertyType.FarmExpansion, ReactiveUniversalFactory.ReactiveFarmExpansionFactory()); //12
        yield return new (DescPropertyType.Link, ReactiveUniversalFactory.ReactiveLinkFactory()); //13
        yield return new (DescPropertyType.TakeSpace, ReactiveUniversalFactory.ReactiveTakeSpaceFactory()); //14
        yield return new (DescPropertyType.HasCraftingQueue, ReactiveUniversalFactory.ReactiveHasCraftingQueueFactory()); //15
        yield return new (DescPropertyType.HasPrestige, ReactiveUniversalFactory.ReactiveHasPrestigeFactory()); //16
        yield return new (DescPropertyType.CitySize, ReactiveUniversalFactory.ReactiveCitySizeFactory().With(("roads", static size => size.RoadsMax), ("builds", static size => size.BuildsMax))); //17
        yield return new (DescPropertyType.HexMask, ReactiveUniversalFactory.ReactiveHexMaskFactory()); //18
        yield return new (DescPropertyType.SubGroup, ReactiveUniversalFactory.ReactiveSubGroupFactory()); //19

        yield return new (DescPropertyType.CraftSpeed, ReactiveUniversalFactory.ReactiveCraftSpeedFactory()); //31
        yield return new (DescPropertyType.TechSpeed, ReactiveUniversalFactory.ReactiveTechSpeedFactory()); //32

        yield return new(DescPropertyType.PveEnemyUnits, ReactiveUniversalFactory.ReactivePveEnemyUnitsFactory());
        yield return new(DescPropertyType.PveEnemyPower, ReactiveUniversalFactory.ReactivePveEnemyPowerFactory());
        yield return new(DescPropertyType.PveArmyImprovement, ReactiveUniversalFactory.ReactivePveArmyImprovementFactory());
    }

    public IEnumerable<IRustyEntity> GetItemsFor(int index)
    {
        foreach (var item in ItemsOfGroup.Items)
        {
            if (item.Owner.Index != index)
                continue;

            yield return Entities[item.Index];
        }
    }

    public IObservable<IChangeSet<IRustyEntity>> GetObservableEntitiesForOwner(int index)
    {
        return ItemsOfGroup.Connect(owner => owner.Owner.ID.Index == index)
            .RemoveKey()
            .Throttle(TimeSpan.FromMilliseconds(100))
            .AutoRefresh(static owner => owner.Owner.ID)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Transform(owner => Entities[owner.Index])
            .RefCount(); //TODO?;
    }

    public void ValidateEntities()
    {
    }

    public void ReplaceOwner(IRustyEntity entity, IRustyEntity newOwner)
    {
        if (newOwner == null)
            return;

        var ownerBox = entity.GetDescUnsafe<IHasOwner>();
        ((ReactiveOwnerViewModel)ownerBox).Owner = newOwner;
    }

    private bool CreateDefaultProperty(Dictionary<DescPropertyType, IDescProperty?> properties, int index, DescPropertyType type, EntityBuildOptions? options = null)
    {
        switch (type)
        {
            case DescPropertyType.Link:
                properties[type] = new ReactiveLink(index, options!.Get<RustyEntity>(EntityBuildKeys.Entity));
                break;
            case DescPropertyType.HasExchange:
                properties[type] = new ReactiveExchange(index, options!.Get<RustyEntity>(EntityBuildKeys.From), options!.Get<RustyEntity>(EntityBuildKeys.To), 1d);
                break;
            case DescPropertyType.HasSingleReward:
                properties[type] = new ReactiveSingleReward(index, options!.Get<RustyEntity>(EntityBuildKeys.Entity), 1);
                break;
            default:
                if (Properties.HasResolve(type))
                {
                    properties[type] = null;
                    break;
                }
                return false;
        }

        return true;
    }

    private void OnEntityCreated(int index, RustyEntityType type, EntityBuildOptions? options = null)
    {
        var defaultProperties = EntityDependencies.GetRequiredProperties(type);
        var properties = new Dictionary<DescPropertyType, IDescProperty?>(defaultProperties.Count);
        if (defaultProperties.Contains(DescPropertyType.HasOwner))
            ItemsOfGroup.AsWriter().Upsert(new ReactiveOwnerViewModel(index, options!.Get<int>(EntityBuildKeys.OwnerIndex), options!.Get<int>(EntityBuildKeys.Tear)));

        foreach (var propertyType in defaultProperties)
            CreateDefaultProperty(properties, index, propertyType, options);

        if (properties.Count != 0)
            Properties.OnEntityAdd(index, properties);
    }

    public void Create(RustyEntityType type, string? displayName, EntityBuildOptions? options = null, int index = 0)
    {
        var defaultProperties = EntityDependencies.GetRequiredProperties(type);
        Entities.Add(index, type, displayName, defaultProperties, index => OnEntityCreated(index, type, options));
    }

    public void Remove(IRustyEntity rustyEntity)
    {
        if(rustyEntity.ID.Index < 0)
            return;

        var index = rustyEntity.ID.Index;
        if(!Entities.HasEntity(index))
            return;

        foreach (var propertyType in rustyEntity.DescProperties)
        {
            switch (propertyType)
            {
                case DescPropertyType.HasOwner:
                    ItemsOfGroup.AsWriter().RemoveAt(index);
                    break;
                case DescPropertyType.Payable:
                    InternalClearList(index, _pricesByOwners, static info => info.Price);
                    break;
                case DescPropertyType.FakePayable:
                    InternalClearList(index, _fakePricesByOwners, static info => info.Price);
                    break;
                case DescPropertyType.HasRewards:
                    InternalClearList(index, _rewardsByOwners, static info => info.Rewards);
                    break;
            }
        }

        if (rustyEntity.HasSpecialMask(EntitySpecialMask.HasChild))
        {
            var children = ItemsOfGroup.ItemsWhere(static owner => owner.Owner.ID.Index, index).ToArray();
            foreach (var child in children)
                Remove(Entities[child.Index]);
        }

        if (rustyEntity.Type is RustyEntityType.Resource)
        {
            foreach (var entity in Entities)
            {
                int tmpIndex = -1;

                if (entity.TryGetProperty(out IHasSingleReward reward))
                    tmpIndex = reward.Entity.ID.Index;
                else if (entity.TryGetProperty(out ILink link))
                    tmpIndex = link.Entity.ID.Index;
                else if (entity.TryGetProperty(out IHasExchange exhange))
                    tmpIndex = exhange.FromEntity.ID.Index;

                if (tmpIndex == index)
                    Remove(entity);
            }

            TryRemoveItemsFor(Prices, index);
            TryRemoveItemsFor(Rewards, index);
        }

        if (rustyEntity.HasSpecialMask(EntitySpecialMask.RequiredInDependencies))
        {
            foreach (var entity in Entities)
            {
                if (entity.TryGetProperty<IHasDependents>(out var dependents))
                {
                    if (dependents.VisibleAfter == rustyEntity)
                        dependents.VisibleAfter = null;

                    if (dependents.Required == rustyEntity)
                        dependents.Required = null;
                }
            }
        }

        if (rustyEntity.Type is RustyEntityType.UniqueBuild)
        {
            foreach (var entity in Entities)
            {
                if (entity.TryGetProperty<IInBuildProcess>(out var inBuildProcess))
                {
                    if (inBuildProcess.Build == rustyEntity)
                        inBuildProcess.Build = null;
                }
            }
        }

        Entities.Remove(index);
        Properties.OnEntityRemove(rustyEntity.ID.Index);
    }

    public bool TryAttachProperty(IRustyEntity entity, DescPropertyType propertyType)
    {
        return ((RustyEntity)entity).TryAddProperty(propertyType);
    }

    public bool TryRemoveProperty(IRustyEntity entity, DescPropertyType propertyType)
    {
        return ((RustyEntity) entity).TryRemoveProperty(propertyType);
    }

    private void TryRemoveItemsFor(DynamicLiteChankListCollection<ResourceStack, ResourceStackForRecord> source, int index)
    {
        var removeItems = source.Items.Where(chunk => chunk.Model.Resource.ID.Index == index)
            .Select(static chunk => chunk.ID).ToArray();
        if (removeItems.Any())
            source.AsWriter().RemoveMany(removeItems);
    }

    #region ResourceStackConverter

    bool IDatabaseChunkConvertable<ResourceStack, ResourceStackForRecord>.TryToModel(long ID,
        ResourceStackForRecord data, out ModelByChunk<ResourceStack>? chunk)
    {
        chunk = ModelByChunk.Create(ID, data.Owner, new ResourceStack(GetEntity(data.ResourceIndex), data.Value));
        return true;
    }

    ResourceStackForRecord IDatabaseChunkConvertable<ResourceStack, ResourceStackForRecord>.ToData(int owner,
        ResourceStack model)
    {
        return new ResourceStackForRecord(owner, model.Resource.ID.Index, model.Value);
    }

    #endregion
}

public static class RustyEntityServiceEx
{
    public static BitArray ToBitmask(this Span<RustyEntityType> types)
    {
        return EntityDependencies.ToBitmask(types);
    }

    public static BitArray ToBitmask(this ReadOnlySpan<RustyEntityType> types)
    {
        return EntityDependencies.ToBitmask(types);
    }

    internal static IRustyEntity? ResolveEntity(this DynamicEntityDatabase entities, BsonValue value, string key)
    {
        var index = value[key].AsInt32;
        if (index == -1)
            return null;

        var lookup = entities.Lookup(index);
        if (lookup.HasValue)
            return lookup.Value;

        return null;
    }
}