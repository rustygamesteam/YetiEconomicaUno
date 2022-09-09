using System.ComponentModel;
using YetiEconomicaCore.Experemental;

namespace YetiEconomicaCore.Database;

internal interface ICollectionWithFilterConfig<TModel>
    where TModel : INotifyPropertyChanged
{
    public int Owner { get; }

    public Func<ModelByChunk<TModel>, bool> Filter { get; }
}
