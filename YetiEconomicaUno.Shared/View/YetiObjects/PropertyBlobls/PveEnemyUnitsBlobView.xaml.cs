using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using ReactiveUIGenerator;
using ReactiveUI;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using System.Reactive.Disposables;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls;


[ViewFor<IPveEnemyUnits>]
public sealed partial class PveEnemyUnitsBlobView : BaseBlobView
{
    public PveEnemyUnitsBlobView()
    {
        this.InitializeComponent();
        this.WhenActivated(disposables =>
        {
            Initialize(ViewModel, DescPropertyType.PveEnemyUnits);

            ViewModel.WhenAnyValue(prop => prop.AvailableUnits)
                .Subscribe(mask =>
                {
                    if (!MaskBox.IsInitialize)
                        MaskBox.InitializeCache(typeof(ArmyTypeFlags), ViewModel.AvailableUnits);
                    InfoBox.Text = $"Enemy units: {MaskBox.SelectedValuesAsString()}";
                    InfoBox.FontSize = MaskBox.ResultCount > 1 ? 12 : 14;
                })
                .DisposeWith(disposables);
        });
    }
}