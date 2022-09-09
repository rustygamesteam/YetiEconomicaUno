using LiteDB;
using System.ComponentModel;
using RustyDTO;
using YetiEconomicaCore.Database;

namespace YetiEconomicaCore.Experemental;

public record struct ModelByChunk<TModel>(BsonValue ID, int Owner, TModel Model) where TModel : INotifyPropertyChanged
{
}

public static class ModelByChunk
{
    public static ModelByChunk<TModel> Create<TModel>(BsonValue ID, int Chunk, TModel Model) where TModel : INotifyPropertyChanged
    {
        return new ModelByChunk<TModel>(ID, Chunk, Model);
    }

    public static IEnumerable<TModel> ForOwner<TModel>(this IEnumerable<ModelByChunk<TModel>> items, int id) where TModel : INotifyPropertyChanged
    {
        foreach (var item in items)
        {
            if (item.Owner == id)
                yield return item.Model;
        }
    }
}

internal record struct ResourceStackChunk(int Owner) : ICollectionWithFilterConfig<ResourceStack>
{
    public Func<ModelByChunk<ResourceStack>, bool> Filter { get; } = chunk => chunk.Owner == Owner;
}