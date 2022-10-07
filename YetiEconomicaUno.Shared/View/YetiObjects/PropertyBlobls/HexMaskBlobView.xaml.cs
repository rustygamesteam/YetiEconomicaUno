using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
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
using RustyDTO.DescPropertyModels;
using ReactiveUI;
using RustyDTO;
using RustyDTO.Supports;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls;

[ViewFor<IHexMask>]
public sealed partial class HexMaskBlobView : BaseBlobView
{
    public HexMaskBlobView()
    {
        this.InitializeComponent();
    }

    public override void CompleteIntialize(CompositeDisposable disposable)
    {
        Initialize(ViewModel, DescPropertyType.HexMask);

        ViewModel.WhenAnyValue(prop => prop.Mask)
            .Subscribe(mask =>
            {
                if (!MaskBox.IsInitialize)
                    MaskBox.InitializeCache(typeof(HexMaskFlags), ViewModel.Mask);
                InfoBox.Text = $"Mask: {MaskBox.SelectedValuesAsString()}";
                InfoBox.FontSize = MaskBox.ResultCount > 1 ? 12 : 14;
            })
            .DisposeWith(disposable);
    }

    protected override void FlyoutOpened(CompositeDisposable disposable)
    {
    }
}