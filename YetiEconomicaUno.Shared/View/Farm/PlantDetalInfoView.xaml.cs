using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using Microsoft.UI.Xaml.Controls;
using YetiEconomicaUno.ViewModels.Farm;

using ReactiveUIGenerator;
using ReactiveUI;
using YetiEconomicaCore;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.Farm;

[ViewFor<PlantInfoViewModel>, ViewContract("Detal")]
public sealed partial class PlantDetalInfoView : UserControl, IDisposable
{
    private CompositeDisposable _viewModelDisposable;

    public PlantDetalInfoView()
    {
        this.InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel)
                .Subscribe(OnViewModelChanged)
                .DisposeWith(disposables);

            this.DisposeWith(disposables);
        });
    }

    private void OnViewModelChanged(PlantInfoViewModel viewModel)
    {
        _viewModelDisposable?.Dispose();
        _viewModelDisposable = new CompositeDisposable();

        RustyEntityView.AutoInitialize(viewModel.Entity, _viewModelDisposable);
    }
    public void Dispose()
    {
        _viewModelDisposable?.Dispose();
        _viewModelDisposable = null;
    }
}
