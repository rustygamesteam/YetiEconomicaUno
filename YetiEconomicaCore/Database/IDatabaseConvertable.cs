using System.ComponentModel;
using YetiEconomicaCore.Experemental;

namespace YetiEconomicaCore.Database;

public interface IDatabaseConvertable<TModel, TData>
{
    bool TryToModel(TData data, out TModel? model);
    TData ToData(TModel model);

    int GetID(TModel model);
}

public interface IDatabaseChunkConvertable<TModel, TData> where TModel : INotifyPropertyChanged
{
    bool TryToModel(long id, TData data, out ModelByChunk<TModel>? chunk);
    TData ToData(int owner, TModel model);
}