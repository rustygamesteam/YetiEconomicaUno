using LiteDB;
using RustyDTO.Interfaces;
using YetiEconomicaCore.Database;

namespace YetiEconomicaCore.ReactiveImpl;

internal abstract class PropertyResolverFactory<TProperty> : IPropertyResolver where TProperty : IDescProperty
{
    public static PropertyResolverFactory<TProperty> Instance { get; protected set; } = null!;

    public abstract BsonValue? SerializeDefault();
    public BsonValue Serialize(IDescProperty @base) => Serialize((TProperty)@base);
    public IDescProperty Deserialize(int index, BsonValue data) => ToProperty(index, data);


    public abstract BsonValue Serialize(TProperty property);
    public abstract TProperty ToProperty(int index, BsonValue data);
}