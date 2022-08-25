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
using YetiEconomicaCore.Descriptions;
using YetiEconomicaUno.ViewModels.CalculateBalance.Progress;
using ReactiveUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.CalculateBalance.Tasks;

[ViewFor<CraftTask>]
public sealed partial class CraftTaskView : UserControl
{
    public CraftTaskView()
    {
        this.InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, static vm => vm.SingleReward.Entity.FullName, static view => view.ResourceNameBox.Text)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, static vm => vm.Statistics, static view => view.Times.Source)
                .DisposeWith(disposables);
        });
    }
}