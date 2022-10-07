using Microsoft.UI.Xaml.Controls;
using YetiEconomicaUno.ViewModels.Farm;

using ReactiveUIGenerator;
using ReactiveUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.Farm;

[ViewFor<PlantInfoViewModel>, ViewContract("Detal")]
public sealed partial class PlantDetalInfoView : UserControl
{
    public PlantDetalInfoView()
    {
        this.InitializeComponent();
    }
}
