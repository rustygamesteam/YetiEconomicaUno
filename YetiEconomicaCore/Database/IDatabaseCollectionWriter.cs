using LiteDB;
using System.ComponentModel;
using YetiEconomicaCore.Experemental;

namespace YetiEconomicaCore.Database;

internal interface IDatabaseCacheCollectionWriter<in TModel>
{
    /// <summary>
    /// Add or update item
    /// </summary>
    /// <param name="data"></param>
    /// <returns>true if item is added</returns>
    bool Upsert(TModel data);
    bool Update(TModel data);

    bool RemoveAt(int index);
    int RemoveMany(IEnumerable<int> indexes);
}

internal interface IDatabaseChunkListCollectionWriter<TModel>   
    where TModel : INotifyPropertyChanged
{
    bool Contains(BsonValue key);
    bool Contains(ModelByChunk<TModel> model);

    bool Add(TModel data, int owner);
    bool AddRange(IEnumerable<TModel> range, int owner);

    bool Remove(BsonValue key);
    int RemoveMany(IEnumerable<BsonValue> rangeKeys);

    bool Update(BsonValue key, TModel data);

    void Clear();
}