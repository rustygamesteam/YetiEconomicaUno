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
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Splat;
using System.Diagnostics.Metrics;
using ReactiveUI;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace YetiEconomicaUno
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IScreen
    {
        public RoutingState Router { get; }

        public MainPage()
        {
            Router = new RoutingState();

            this.InitializeComponent();
        }

        private void NavView_OnSelected(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            try
            {
                var viewModel = Locator.Current.GetService<IRoutableViewModel>(args.SelectedItemContainer.Tag as string);
                if (viewModel == null)
                    return;

                Router.Navigate.Execute(viewModel);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
