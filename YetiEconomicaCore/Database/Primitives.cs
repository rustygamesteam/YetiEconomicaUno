using LiteDB;
using RustyDTO;
using RustyDTO.Interfaces;
using YetiEconomicaCore.Services;

namespace YetiEconomicaCore.Database;

public record struct ResourceStackRecord(IRustyEntity Resource, double Value)
{
    public static implicit operator ResourceStack(ResourceStackRecord record) => new(record.Resource, record.Value);
    public static implicit operator ResourceStackRecord(ResourceStack stack) => new(stack.Resource, stack.Value);

    public override string ToString()
    {
        return $"{Resource.FullName}: {Value:F0}";
    }
}

public record struct ResourceStackForRecord(int Owner, int ResourceIndex, double Value)
{
    public static ResourceStackForRecord Parse(BsonDocument doc)
    {
        return new ResourceStackForRecord(doc[nameof(Owner)].AsInt32, doc[nameof(ResourceIndex)].AsInt32, doc[nameof(Value)].AsDouble);
    }

    public static implicit operator ResourceStack(ResourceStackForRecord record) => new(RustyEntityService.Instance.GetEntity(record.ResourceIndex), record.Value);
}

public record struct ItemOfGroupInfo(int Index, int Owner, int Tear);