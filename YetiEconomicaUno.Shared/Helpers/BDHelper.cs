using ReactiveUI;
using System.Reactive.Disposables;
using YetiEconomicaCore.Services;

namespace YetiEconomicaUno.Helpers;

public static class BDHelper
{
    public static T GetProperty<T>(string source, CompositeDisposable disposables) where T : IReactiveObject, new()
    {
        var database = DatabaseRepository.Instance;
        return database.GetConfig(source, _ => new T(), disposables);
    }
}
