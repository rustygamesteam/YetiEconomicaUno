using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using LiteDB;
using ReactiveUI;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;
using YetiEconomicaCore.Database;

namespace YetiEconomicaCore.Services;

public class ConvertablesService : ReactiveObject
{
    private static readonly Lazy<ConvertablesService> _instance = new(() => new());
    public static ConvertablesService Instance => _instance.Value;

    private Dictionary<IRustyEntity, List<ResourceStackRecord>> _resourceExchanges = new();
    public ObservableCollectionExtended<IRustyEntity> ExchangesTo { get; } = new();
    public IObservable<Func<IRustyEntity, bool>> ExchangesToFilter { get; }


    private ConvertablesService()
    {
        var service = RustyEntityService.Instance;

        service.ConnectToEntity(static entity => entity.Type is RustyEntityType.ExchageTask).RemoveKey()
           .Subscribe(Exchanges_OnChange);

        ExchangesToFilter = Observable.Return<Func<IRustyEntity, bool>>(ExchangesTo.Contains);
    }

    private void Exchanges_OnChange(IChangeSet<IReactiveRustyEntity> diffs)
    {
        void OnAdd(IRustyEntity entity)
        {
            var exchange = entity.GetDescUnsafe<IHasExchange>();
            if (!_resourceExchanges.TryGetValue(exchange.ToEntity, out var list))
            {
                list = new List<ResourceStackRecord>();
                ExchangesTo.Add(exchange.ToEntity);
                _resourceExchanges[exchange.ToEntity] = list;
            }

            list.Add(new ResourceStackRecord(exchange.FromEntity, exchange.FromRate));
        }
        void OnRemove(IRustyEntity entity)
        {
            var exchange = entity.GetDescUnsafe<IHasExchange>();

            var resource = exchange.ToEntity;
            if (_resourceExchanges.TryGetValue(resource, out var list))
            {
                var fromResource = exchange.FromEntity;
                var index = list.Select(static x => x.Resource).IndexOf(fromResource);
                if(index != - 1)
                    list.RemoveAt(index);
            }
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
                case ListChangeReason.Add:
                    OnAdd(diff.Item.Current);
                    break;
                case ListChangeReason.Remove:
                    OnRemove(diff.Item.Current);
                    break;
                case ListChangeReason.Clear:
                    _resourceExchanges.Clear();
                    break;
            }
        }
    }

    public IObservable<IChangeSet<IReactiveRustyEntity>> ObservableExchangesToResource(int index)
    {
        return RustyEntityService.Instance.ConnectToEntity(entity =>
                entity.Type is RustyEntityType.ExchageTask && entity.GetDescUnsafe<IHasExchange>().ToEntity.ID.Index == index)
            .RemoveKey();
    }


    public void Create(IRustyEntity fromResource, IRustyEntity toResource)
    {
        var service = RustyEntityService.Instance;
        var options = EntityBuildOptions.Create()
            .Add(EntityBuildKeys.From, fromResource)
            .Add(EntityBuildKeys.To, toResource);

        service.Create(RustyEntityType.ExchageTask, null, options);
    }

    public void Add(IRustyEntity resourcEntity)
    {
        if(resourcEntity is null)
            return;

        ExchangesTo.Add(resourcEntity);
    }

    public void Remove(IRustyEntity entity)
    {
        if (entity is null)
            return;

        var service = RustyEntityService.Instance;

        if (entity.Type is RustyEntityType.Resource)
        {
            if (ExchangesTo.Remove(entity) && _resourceExchanges.TryGetValue(entity, out var list))
            {
                list.Clear();
                var remove = service.EntitesWhereType(RustyEntityType.ExchageTask)
                    .Where(other => other.GetDescUnsafe<IHasExchange>().ToEntity == entity)
                    .ToArray();

                foreach (var rustyEntity in remove)
                    service.Remove(rustyEntity);
            }
            return;
        }

        service.Remove(entity);
    }

    public IEnumerable<string> GetPriceInfo(IRustyEntity exchange, int count)
    {
        if (exchange is null || count == 0)
            yield break;

        var exchangeInfo = exchange.GetDescUnsafe<IHasExchange>();
        yield return $"• {exchangeInfo.FromEntity.FullName}: {count * exchangeInfo.FromRate:0.##}";
    }

    public bool TryGetFirstExchnage(IRustyEntity exchanges, out ResourceStackRecord exchangePrice)
    {
        if (_resourceExchanges.TryGetValue(exchanges, out var list) && list.Count > 0)
        {
            exchangePrice = list[0];
            return true;
        }

        exchangePrice = default;
        return false;
    }

    public IEnumerable<string> GetConvertibleToResource(IRustyEntity value)
    {
        var service = RustyEntityService.Instance;

        return service.EntitesWhereType(RustyEntityType.ExchageTask)
            .Where(entity => entity.GetDescUnsafe<IHasExchange>().ToEntity == value).Select(
                static entity =>
                {
                    var exchange = entity.GetDescUnsafe<IHasExchange>();
                    return $"• {exchange.FromEntity.FullName}: {exchange.FromRate:0.##}";
                });
    }

    public IEnumerable<string> GetConvertibleFromResource(IRustyEntity value)
    {
        var service = RustyEntityService.Instance;

        return service.EntitesWhereType(RustyEntityType.ExchageTask)
            .Where(entity => entity.GetDescUnsafe<IHasExchange>().FromEntity == value).Select(
                static entity =>
                {
                    var exchange = entity.GetDescUnsafe<IHasExchange>();
                    return $"• {exchange.ToEntity.FullName}: {1d / exchange.FromRate:0.##}";
                });
    }

    public IRustyEntity? GetExchange(IRustyEntity from, IRustyEntity to)
    {
        if (from is null || to is null)
            return null;

        return RustyEntityService.Instance.EntitesWhereType(RustyEntityType.ExchageTask)
            .FirstOrDefault(
                entity =>
                {
                    var exchange = entity.GetDescUnsafe<IHasExchange>();
                    return exchange.FromEntity == from && exchange.ToEntity == to;
                });
    }
}
