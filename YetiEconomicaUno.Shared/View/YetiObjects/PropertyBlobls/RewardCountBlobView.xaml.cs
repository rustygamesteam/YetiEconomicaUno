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
using YetiEconomicaCore.Descriptions;
using ReactiveUI;
using YetiEconomicaCore;
using System.Reactive.Disposables;
using RustyDTO;
using RustyDTO.PropertyModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls;


[ViewFor<IHasSingleReward>]
public sealed partial class RewardCountBlobView : BaseBlobView
{
    private string _countLabel;

    public RewardCountBlobView()
    {
        this.InitializeComponent();
        this.WhenActivated(disposables =>
        {
            Initialize(ViewModel.Index, EntityPropertyType.HasSingleReward);
            var owner = Entity;

            _countLabel = owner.Type is RustyEntityType.Plant ? "Harvest count" : "Count";
            CountBox.Header = _countLabel;

            ViewModel.WhenAnyValue(dependents => dependents.Count)
                .Subscribe(count => InfoBox.Text = $"{_countLabel}: {count}")
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