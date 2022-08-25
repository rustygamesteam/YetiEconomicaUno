using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using DependencyPropertyGenerator;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.CalculateBalance.Tasks;

[DependencyProperty<object>("Price")]
public sealed partial class PriceBoxInfoControl : UserControl
{
    public PriceBoxInfoControl()
    {
        this.InitializeComponent();
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        Bindings.StopTracking();
    }
}