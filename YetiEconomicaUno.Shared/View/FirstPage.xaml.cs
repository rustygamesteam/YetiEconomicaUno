using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using ReactiveUIGenerator;
using System;
using YetiEconomicaUno.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace YetiEconomicaUno.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    [ViewFor<FirstViewModel>()]
    internal sealed partial class FirstPage : Page
    {
        public FirstPage()
        {
            this.InitializeComponent();


            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
