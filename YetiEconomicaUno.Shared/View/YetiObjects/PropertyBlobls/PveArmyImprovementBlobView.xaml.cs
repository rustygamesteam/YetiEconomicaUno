using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using ReactiveUI;
using ReactiveUIGenerator;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using RustyDTO.Supports;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls;

[ViewFor<IPveArmyImprovement>]
public sealed partial class PveArmyImprovementBlobView : BaseBlobView
{
    private IDisposable _lastDisposable;

    public PveArmyImprovementBlobView()
    {
        this.InitializeComponent();

        InfoBox.FontSize = 12;
    }

    public override void CompleteIntialize(CompositeDisposable disposable)
    {
        Initialize(ViewModel, DescPropertyType.PveArmyImprovement);
        this.WhenAnyValue(static view => view.ViewModel)
            .Subscribe(viewModel =>
            {
                _lastDisposable?.Dispose();

                void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
                {
                    OnViewModelUpdated();
                }

                viewModel.PropertyChanged += ViewModelOnPropertyChanged;
                _lastDisposable = Disposable.Create(viewModel, improvement => improvement.PropertyChanged -= ViewModelOnPropertyChanged);

                OnViewModelUpdated();
            })
            .DisposeWith(disposable);

        Disposable.Create(this, static view => view._lastDisposable?.Dispose())
            .DisposeWith(disposable);
    }

    private void OnViewModelUpdated()
    {
        if (!ForUnitsBox.IsInitialize)
            ForUnitsBox.InitializeCache(typeof(ArmyTypeFlags), ViewModel.ForUnits);
        if (!ForPopertyBox.IsInitialize)
            ForPopertyBox.InitializeCache(typeof(ArmyPropertyFlags), ViewModel.ForProperty);

        InfoBox.Text = $@"Army improvement: {{ for: {ForUnitsBox.SelectedValuesAsString()}, prop: {ForPopertyBox.SelectedValuesAsString()}, force: {ViewModel.Force:###.##} }}";
    }
}