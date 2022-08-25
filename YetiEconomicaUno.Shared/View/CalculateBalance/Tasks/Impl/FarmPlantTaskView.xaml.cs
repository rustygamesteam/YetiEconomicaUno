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
using YetiEconomicaUno.ViewModels.CalculateBalance.Progress;
using YetiEconomicaUno.Controls.Resources;
using YetiEconomicaCore;
using ReactiveUI;
using YetiEconomicaCore.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.CalculateBalance.Tasks;

[ViewFor<FarmPlantTask>]
public sealed partial class FarmPlantTaskView : UserControl
{
    public FarmPlantTaskView()
    {
        this.InitializeComponent();

        this.WhenActivated(disposables =>
        {
            ResourcesList.Filter = PlantsService.Instance.IsPlant;
            Disposable.Create(ResourcesList, static view => view.Filter = null)
                .DisposeWith(disposables);
        });
    }
}