using LiteDB;

namespace YetiEconomicaCore.Experemental;

public interface IDatabaseEntity
{
    BsonValue ID { get; }

    void InjectID(BsonValue id);
}

public interface IDatabaseOrderEntity : IDatabaseEntity
{
    int Order { get; set; }
}