using Microsoft.UI.Xaml.Controls;
using YetiEconomicaUno.ViewModels.Farm;
using ReactiveUIGenerator;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System;
using ReactiveUI;
using System.Linq;
using System.Collections.Specialized;
using System.Reactive.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using RustyDTO.Interfaces;
using YetiEconomicaUno.View.YetiObjects;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.Farm;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[ViewFor<PlantsPageViewModel>]
public sealed partial class PlantsPage : Page
{
    private CompositeDisposable _disposable;
    private readonly BehaviorSubject<Func<IRustyEntity, bool>> _onFilter;

    public PlantsPage()
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

            this.OneWayBind(ViewModel, static vm => vm.ItemSource, static view => view.DetailsView.ItemsSource).DisposeWith(disposable);

            ViewModel.Initialize(disposable);

            this.Bind(ViewModel, static vm => vm.SearchMask, static view => view.SearchBox.Text).DisposeWith(disposable);
        });
    }

    private void ResourceSelector_OnLoaded(object sender, RoutedEventArgs e)
    {
        var selector = (YetiObjectSelector)sender;

        selector.SetBinding(YetiObjectSelector.SelectedValueProperty, new Binding
        {
            Source = ViewModel,
            Path = new PropertyPath(nameof(ViewModel.NewResource)),
            Mode = BindingMode.TwoWay
        });

        selector.Filter = _onFilter;
        selector.Loaded -= ResourceSelector_OnLoaded;

        Disposable.Create(selector, selector => {
            selector.ClearValue(YetiObjectSelector.SelectedValueProperty);
            selector.Filter = null;
        }).DisposeWith(_disposable);
    }

    private bool OnFilter(IRustyEntity resource)
    {
        return ViewModel.ItemSource.Select(static model => model.Resource).Contains(resource) is false;
    }

    private void Add_OnClicked()
    {
        ViewModel.TryAdd();
    }
}
