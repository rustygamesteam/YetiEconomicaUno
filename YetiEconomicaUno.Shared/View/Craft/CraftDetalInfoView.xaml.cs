using System;
using Microsoft.UI.Xaml.Controls;
using ReactiveUIGenerator;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using RustyDTO.Interfaces;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.Craft;

[ViewFor<IReactiveRustyEntity>, ViewContract("Detal")]
public sealed partial class CraftDetalInfoView : UserControl
{
    public CraftDetalInfoView()
    {
        this.InitializeComponent();
    }
}
