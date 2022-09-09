using LiteDB;
using RustyDTO.Interfaces;

namespace YetiEconomicaCore.Database;

internal interface IPropertyResolver
{
    public BsonValue? SerializeDefault();
    public BsonValue Serialize(IDescProperty @base);
    public IDescProperty Deserialize(int index, BsonValue data);
}