using System;
using System.Linq;
using System.Reactive.Disposables;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using YetiEconomicaUno.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive.Linq;
using YetiEconomy.Controls;
using ReactiveUIGenerator;
using RustyDTO.Interfaces;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [ViewFor<ResourcesViewModel>]
    public sealed partial class ResourcesPage : Page
    {
        
        ~ResourcesPage()
        {

        }

        public ResourcesPage()
        {
            this.WhenActivated(disposable =>
            {
                ViewModel!.Activate(disposable);

                this.WhenAnyValue(static x => x.GroupList.SelectedItem)
                    .Select(static x => (IRustyEntity)x)
                    .ToPropertyEx(ViewModel, static vm => vm.SelectedGroup)
                    .DisposeWith(disposable);

                Observable.FromEventPattern(ResourcesFilter, nameof(ResourcesFilter.SelectionChanged))
                    .Select(_ => ResourcesFilter.SelectedItems)
                    .Subscribe(ViewModel.OnFilterUpdate)
                    .DisposeWith(disposable);

                this.WhenAnyValue(static x => x.ResourcesList.SelectedItem)
                    .Select(static x => (IRustyEntity)x)
                    .ToPropertyEx(ViewModel, static vm => vm.SelectedResource)
                    .DisposeWith(disposable);

                this.BindCommand(ViewModel, static model => model.RemoveResourceGroupCommand, static page => page.RemoveGroupBtn)
                    .DisposeWith(disposable);
                this.BindCommand(ViewModel, static model => model.CreateGroupCommand, static page => page.GroupList, nameof(AdvancedListView.OnCreateItem))
                    .DisposeWith(disposable);


                this.BindCommand(ViewModel, static model => model.RemoveResourceCommand, static page => page.RemoveResourceBtn)
                    .DisposeWith(disposable);

                this.BindCommand(ViewModel, static model => model.CreateResourceCommand, static page => page.ResourcesList, nameof(AdvancedListView.OnCreateItem))
                    .DisposeWith(disposable);
            });

            this.InitializeComponent();
        }

        private void Page_OnUnload(object sender, RoutedEventArgs e)
        {
            Bindings.StopTracking();
        }


        private void OnImportClicked()
        {

        }
    }
}
