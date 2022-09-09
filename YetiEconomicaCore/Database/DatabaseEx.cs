using LiteDB;
using System.ComponentModel;
using YetiEconomicaCore.Experemental;

namespace YetiEconomicaCore.Database;

internal static class DatabaseEx
{
    public static IEnumerable<TModel> ToModels<TModel, TData>(this ILiteCollection<TData> liteCollection, IDatabaseConvertable<TModel, TData> convertable, IComparer<TData>? comparer = null)
        where TModel : INotifyPropertyChanged
    {
        var items = liteCollection.FindAll();
        if (comparer != null)
            items = items.OrderBy(static x => x, comparer);

        var all = items.GetEnumerator();
        while(all.MoveNext())
        {
            if (convertable.TryToModel(all.Current, out var model))
                yield return model!;
        }
    }

    public static IEnumerable<ModelByChunk<TModel>> ToModels<TModel, TData>(this ILiteCollection<BsonDocument> liteCollection, IDatabaseChunkConvertable<TModel, TData> convertable)
        where TModel : INotifyPropertyChanged
    {
        var mapper = BsonMapper.Global;

        var all = liteCollection.FindAll().GetEnumerator();
        while (all.MoveNext())
        {
            var doc = all.Current;
            var id = doc["_id"].AsInt32;
            var data = mapper.ToObject<TData>(doc);
            if (convertable.TryToModel(id, data, out var model))
                yield return model!.Value;
        }
    }

    public static IDatabaseCacheCollectionWriter<TModel> AsWriter<TModel, TData>(this DynamicLiteCacheCollection<TModel, TData> collection)
        where TModel : INotifyPropertyChanged
    {
        return collection;
    }

    public static IDatabaseChunkListCollectionWriter<TModel> AsWriter<TModel, TData>(this DynamicLiteChankListCollection<TModel, TData> collection)
        where TModel : INotifyPropertyChanged
    {
        return collection;
    }
}
