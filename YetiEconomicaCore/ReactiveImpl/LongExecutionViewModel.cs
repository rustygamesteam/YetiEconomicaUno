using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using LiteDB;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;
using YetiEconomicaCore.Database;

namespace YetiEconomicaCore.ReactiveImpl;

internal class LongExecutionViewModel : ReactiveObject, ILongExecution
{
    public int Index { get; }

    [Reactive]
    public int Duration { get; set; }

    public LongExecutionViewModel(int ownerIndex, int duration)
    {
        Index = ownerIndex;
        Duration = duration;
    }
}

internal class ReactiveLongExecutionFactory : IPropertyResolver
{
    public static ReactiveLongExecutionFactory Instance { get; } = new ();

    public BsonValue? SerializeDefault()
    {
        return new BsonDocument
        {
            { nameof(ILongExecution.Duration), 60 }
        };
    }

    public BsonValue Serialize(IDescProperty @base)
    {
        var property = (ILongExecution)@base;
        return new BsonDocument
        {
            { nameof(ILongExecution.Duration), property.Duration }
        };
    }

    public IDescProperty Deserialize(int index, BsonValue data)
    {
        return new LongExecutionViewModel(index, data[nameof(ILongExecution.Duration)].AsInt32);
    }
}