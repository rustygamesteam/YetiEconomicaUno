using DependencyPropertyGenerator;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RustyDTO;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.Controls.Resources;

[DependencyProperty<ResourceStack>("Model")]
public sealed partial class ResourceStackView : UserControl
{
    public ResourceStackView()
    {
        this.InitializeComponent();
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        Bindings.StopTracking();
    }
}
