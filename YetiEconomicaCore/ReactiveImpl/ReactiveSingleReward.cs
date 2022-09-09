using LiteDB;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;
using YetiEconomicaCore.Database;
using YetiEconomicaCore.Services;

namespace YetiEconomicaCore.ReactiveImpl;

public class ReactiveSingleReward : ReactiveObject, IHasSingleReward
{
    public ReactiveSingleReward(int index, IRustyEntity entity, int count)
    {
        Index = index;
        Entity = entity;
        Count = count;
    }

    public int Index { get; }
    public IRustyEntity Entity { get; }

    [Reactive]
    public int Count { get; set; }
}

internal class ReactiveSingleRewardFactory : IPropertyResolver
{
    public static ReactiveSingleRewardFactory Instance { get; } = new ();
    private ReactiveSingleRewardFactory(){}

    public BsonValue? SerializeDefault()
    {
        return null;
    }

    public BsonValue Serialize(IDescProperty @base)
    {
        var property = (IHasSingleReward)@base;
        return new BsonDocument
        {
            { "Entity", property.Entity?.ID.Index ?? -1},
            { "Count", property.Count},
        };
    }

    public IDescProperty Deserialize(int index, BsonValue data)
    {
        var entites = RustyEntityService.Instance.Entities;
        return new ReactiveSingleReward(index, entites.ResolveEntity(data, "Entity")!, data["Count"].AsInt32);
    }
}