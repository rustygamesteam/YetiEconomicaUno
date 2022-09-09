using LiteDB;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;
using YetiEconomicaCore.Database;
using YetiEconomicaCore.Services;

namespace YetiEconomicaCore.ReactiveImpl;

internal class ReactiveExchange : ReactiveObject, IHasExchange
{
    public ReactiveExchange(int index, IRustyEntity from, IRustyEntity to, double fromRate)
    {
        Index = index;
        FromEntity = from;
        ToEntity = to;
        FromRate = fromRate;
    }

    public int Index { get; }

    public IRustyEntity ToEntity { get; }
    public IRustyEntity FromEntity { get; }

    [Reactive]
    public double FromRate { get; set; }
}

internal class ReactiveExchangeFactory : IPropertyResolver
{
    public static ReactiveExchangeFactory Instance { get; } = new ();
    private ReactiveExchangeFactory() {}
    
    public BsonValue? SerializeDefault()
    {
        return null;
    }

    public BsonValue Serialize(IDescProperty @base)
    {
        var property = (IHasExchange)@base;
        return new BsonDocument
        {
            {"From", property.FromEntity?.ID.Index ?? -1},
            {"To", property.FromEntity?.ID.Index ?? -1},
            {"FromRate", property.FromRate},
        };
    }

    public IDescProperty Deserialize(int index, BsonValue data)
    {
        var entites = RustyEntityService.Instance.Entities;
        return new ReactiveExchange(index, entites.ResolveEntity(data, "From")!, entites.ResolveEntity(data, "To")!, data["FromRate"].AsDouble);
    }
}