using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using ReactiveUIGenerator;
using ReactiveUI;
using System.Reactive.Linq;
using RustyDTO;
using RustyDTO.DescPropertyModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls;

[ViewFor<ICitySize>]
public sealed partial class CitySizeBlobView : BaseBlobView
{

    public CitySizeBlobView()
    {
        this.InitializeComponent();

        this.WhenActivated(disposables =>
        {
            Initialize(ViewModel.Index, DescPropertyType.MineSize);

            ViewModel.WhenAnyValue(static x => x.BuildsMax)
                .CombineLatest(ViewModel.WhenAnyValue(static x => x.RoadsMax))
                .Select(static tuple => $"City size: {{ Builds: {tuple.First}, Roads: {tuple.Second} }}")
                .BindTo(this, static view => view.InfoBox.Text)
                .DisposeWith(disposables);

            this.WhenAnyValue(static view => view.ViewModel)
                .BindTo(this, static view => view.DataContext)
                .DisposeWith(disposables);
        });
    }

    protected override void FlyoutOpened(CompositeDisposable disposable)
    {
    }
}