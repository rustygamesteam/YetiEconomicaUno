using System.Reactive.Disposables;

namespace YetiEconomicaUno.Helpers;

public interface ICompleteViewIntialize
{
    public void CompleteIntialize(CompositeDisposable disposable);
}