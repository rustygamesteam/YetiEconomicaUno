using ReactiveUI;
using Splat;

namespace YetiEconomicaUno.ViewModels
{
    public abstract class BaseViewModel : ReactiveObject, IRoutableViewModel
    {
        public string UrlPathSegment { get; private set; }
        public IScreen HostScreen { get; }

        public BaseViewModel()
        {
            HostScreen = Locator.Current.GetService<IScreen>();
        }

        public IRoutableViewModel InjectURI(string uri)
        {
            UrlPathSegment = uri;
            return this;
        }
    }
}
