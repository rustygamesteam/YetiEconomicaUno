using System.Reactive.Linq;
using DynamicData;
using Nito.Comparers;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;

namespace YetiEconomicaCore.Services;

public class CraftService
{

    private static readonly Lazy<CraftService> _instance = new (static () => new ());
    public static CraftService Instance => _instance.Value;

    public IObservable<Func<IRustyEntity, bool>> CraftFilter { get; }
    public IObservable<IChangeSet<IRustyEntity>> ObservableCrafts { get; }

    private Dictionary<IRustyEntity, IRustyEntity> _resourceCrafts = new();

    private CraftService()
    {
        var sort = ComparerBuilder.For<IRustyEntity>()
            .OrderBy(static x => x.DisplayName);

        CraftFilter = Observable.Return<Func<IRustyEntity, bool>>(static entity => entity.Type is RustyEntityType.CraftTask);
        ObservableCrafts = RustyEntityService.Instance.ConnectToEntity(static entity => entity.Type is RustyEntityType.CraftTask).RemoveKey();

        ObservableCrafts.Subscribe(Crafts_OnChanged);
    }

    private void Crafts_OnChanged(IChangeSet<IRustyEntity> diffs)
    {
        void OnAdd(IRustyEntity entity)
        {
            _resourceCrafts.Add(entity.GetDescUnsafe<IHasSingleReward>().Entity, entity);
        }

        void OnRemove(IRustyEntity entity)
        {
            _resourceCrafts.Remove(entity.GetDescUnsafe<IHasSingleReward>().Entity);
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
                    _resourceCrafts.Clear();
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

    public IRustyEntity GetCraftFor(IRustyEntity resource)
    {
        return _resourceCrafts[resource];
    }

    public bool HasCraft(IRustyEntity resource)
    {
        return _resourceCrafts.ContainsKey(resource);
    }

    public bool TryGetCraft(IRustyEntity resource, out IRustyEntity craft)
    {
        return _resourceCrafts.TryGetValue(resource, out craft!) && craft.GetDescUnsafe<IPayable>().Price.Count > 0;
    }

    public void Create(IRustyEntity source)
    {
        if (source is null)
            return;
        RustyEntityService.Instance.Create(RustyEntityType.CraftTask, null, EntityBuildOptions.CreateWithEntity(source));
    }

    public void Remove(IRustyEntity source)
    {
        if (source is null)
            return;
        RustyEntityService.Instance.Remove(source);
    }

    public IEnumerable<string> GetCraftPrice(IRustyEntity resource, int count)
    {
        if(resource == null || !TryGetCraft(resource, out var craft))
            yield break;

        foreach (var price in craft.GetDescUnsafe<IPayable>().Price)
            yield return $"{price.Resource.FullName}: {price.Value * count:F0}";
    }
}