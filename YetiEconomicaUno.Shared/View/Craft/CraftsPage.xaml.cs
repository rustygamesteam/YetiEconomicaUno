using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using YetiEconomicaUno.ViewModels.Crafts;
using ReactiveUIGenerator;
using Microsoft.UI.Xaml.Data;
using RustyDTO.Interfaces;
using RustyDTO.PropertyModels;
using YetiEconomicaUno.View.YetiObjects;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace YetiEconomicaUno.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [ViewFor<CraftsViewModel>]
    public sealed partial class CraftsPage : Page
    {
        private CompositeDisposable _disposable;
        private readonly BehaviorSubject<Func<IRustyEntity, bool>> _onFilter;

        public CraftsPage()
        {
            this.InitializeComponent();
            _onFilter = new BehaviorSubject<Func<IRustyEntity, bool>>(OnFilter);

            this.WhenActivated(disposable =>
            {
                _disposable = disposable;
                _onFilter.DisposeWith(disposable);

                Observable.FromEventPattern(this.ViewModel.ItemSource, nameof(INotifyCollectionChanged.CollectionChanged))
                        .Subscribe(_ => _onFilter.OnNext(_onFilter.Value))
                        .DisposeWith(disposable);

                ViewModel.Intialize(disposable);

                this.Bind(ViewModel, static vm => vm.SearchMask, static view => view.SearchBox.Text).DisposeWith(disposable);
                this.OneWayBind(ViewModel, static vm => vm.ItemSource, static view => view.DetailsView.ItemsSource).DisposeWith(disposable);
            });
        }

        private void ResourceSelector_OnLoaded(object sender, RoutedEventArgs e)
        {
            var selector = (YetiObjectSelector)sender;
            selector.Loaded -= ResourceSelector_OnLoaded;

            selector.SetBinding(YetiObjectSelector.SelectedValueProperty, new Binding
            {
                Source = ViewModel,
                Path = new PropertyPath(nameof(ViewModel.NewResource)),
                Mode = BindingMode.TwoWay
            });

            selector.Filter = _onFilter;

            Disposable.Create(selector, static selector => {
                selector.ClearValue(YetiObjectSelector.SelectedValueProperty);
                selector.Filter = null;
            }).DisposeWith(_disposable);
        }

        private void Add_OnClicked()
        {
            ViewModel.AddReciept_OnClicked();
        }

        private bool OnFilter(IRustyEntity resource)
        {
            return ViewModel.ItemSource.Select(static craft => craft.GetUnsafe<IHasSingleReward>().Entity).Contains(resource) is false;
        }

        private void Page_OnUnload(object sender, RoutedEventArgs e)
        {
            Bindings.StopTracking();
        }

    }
}
