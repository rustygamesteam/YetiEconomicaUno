using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using ReactiveUIGenerator;
using YetiEconomicaCore;
using YetiEconomicaCore.Services;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Threading;
using System.Reactive.Linq;
using YetiEconomicaUno.Converters;
using YetiEconomicaUno.ViewModels.Farm;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.Farm;

[ViewFor<PlantInfoViewModel>]
public sealed partial class PlantInfoView : UserControl, IDisposable
{
    private CompositeDisposable _disposables;

    public PlantInfoView()
    {
        this.InitializeComponent();

        this.WhenActivated(disposable =>
        {

            this.WhenAnyValue(static view => view.ViewModel)
                .WhereNotNull()
                .Subscribe(ViewModel_OnInitialize)
                .DisposeWith(disposable);

            this.DisposeWith(disposable);
        });
    }

    private void ViewModel_OnInitialize(PlantInfoViewModel viewModel)
    {
        var disposable = Interlocked.Exchange(ref _disposables, new CompositeDisposable());
        disposable?.Dispose();

        viewModel.Resource.WhenAnyValue(static vm => vm.FullName)
            .BindTo(this, static view => view.FullName.Text)
            .DisposeWith(_disposables);
        viewModel.WhenAnyValue(static vm => vm.Duration)
            .Select(static value => $"Duration growth: {DurationLabelConverter.GetDuration(value)}")
            .BindTo(this, static view => view.Duration.Text)
            .DisposeWith(_disposables);
        viewModel.WhenAnyValue(static vm => vm.Harvest)
            .Select(static value => $"Harvest count: {value}")
            .BindTo(this, static view => view.Harvest.Text)
            .DisposeWith(_disposables);
    }

    private void RemoveFarm_OnClick(object sender, RoutedEventArgs args)
    {
        PlantsService.Instance.Remove(ViewModel.Entity);
    }

    public void Dispose()
    {
        var disposable = Interlocked.Exchange(ref _disposables, null);
        disposable?.Dispose();

        ClearValue(ViewModelProperty);
        ClearValue(DataContextProperty);
    }
}
