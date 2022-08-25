using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using YetiEconomicaUno.ViewModels.Convertables;
using ReactiveUIGenerator;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Collections.Specialized;
using YetiEconomicaUno.View.YetiObjects;
using RustyDTO.Interfaces;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [ViewFor<ResourcesConvertiblePageViewModel>]
    public sealed partial class ResourcesConvertiblePage : Page
    {
        CompositeDisposable _disposable;
        private readonly BehaviorSubject<Func<IRustyEntity, bool>> _onFilter;

        public ResourcesConvertiblePage()
        {
            this.InitializeComponent();
            _onFilter = new BehaviorSubject<Func<IRustyEntity, bool>>(OnFilter);

            this.WhenActivated(disposable =>
            {
                ViewModel.Initialize(disposable);

                Observable.FromEventPattern(this.ViewModel.ItemSource, nameof(INotifyCollectionChanged.CollectionChanged))
                        .Subscribe(_ => _onFilter.OnNext(_onFilter.Value))
                        .DisposeWith(disposable);

                this.OneWayBind(ViewModel, static vm => vm.ItemSource, static view => view.DetailsView.ItemsSource)
                    .DisposeWith(disposable);

                this.Bind(ViewModel, static vm => vm.SearchMask, static view => view.SearchBox.Text).DisposeWith(disposable);

                _disposable = disposable;
            });
        }

        private bool OnFilter(IRustyEntity resource)
        {
            return ViewModel.ItemSource.Select(static item => item.ConvertableToResource).Contains(resource) is false;
        }

        private void ResourceSelector_OnLoaded(object sender, RoutedEventArgs e)
        {
            var selector = (YetiObjectSelector)sender;
            selector.WhenAnyValue(static selector => selector.SelectedValue)
                .BindTo(ViewModel, static vm => vm.NewConvrtable)
                .DisposeWith(_disposable);

            selector.Filter = _onFilter;

            selector.Loaded -= ResourceSelector_OnLoaded;
        }

        private void AddItem_OnClicked()
        {
            ViewModel.AddConvrtable_OnClicked();
        }
    }
}
