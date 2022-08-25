using System;
using System.Reactive.Disposables;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using YetiEconomicaUno.ViewModels;
using ReactiveUI;
using DependencyPropertyGenerator;
using ReactiveUIGenerator;
using RustyDTO.Interfaces;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.Controls.Resources
{
    [DependencyProperty<double>("Height", DefaultValue = double.NaN)]
    [DependencyProperty<double>("MinWidth", DefaultValue = 0)]
    [DependencyProperty<IObservable<Func<IRustyEntity, bool>>>("Filter")]
    [ViewFor<SelectResourceFlyoutViewModel>]
    public sealed partial class SelectResourceFlyout : Flyout, IDisposable
    {
        private CompositeDisposable _disposable;

        public event Action<IRustyEntity> InvokeItemEvent;

        public Func<float> HeightCalculateFunc { get; set; }

        private bool _isIntialize;

        public SelectResourceFlyout()
        {
            this.InitializeComponent();
        }

        public void Initalize()
        {
            if(_isIntialize)
                return;
            _isIntialize = true;

            _disposable = new CompositeDisposable();
            ViewModel = new SelectResourceFlyoutViewModel(_disposable);

            this.Bind(ViewModel, vm => vm.SearchMask, flyout => flyout.SearchBox.Text).DisposeWith(_disposable);
            this.OneWayBind(ViewModel, model => model.Source, flyout => flyout.TreeView.ItemsSource).DisposeWith(_disposable);

            TreeView.ItemInvoked += TreeView_ItemInvoked;
        }

        partial void OnFilterChanged(IObservable<Func<IRustyEntity, bool>> newValue)
        {
            Initalize();
            ViewModel.FilterChanged(newValue);
        }

        private void TreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is not IRustyEntity resource)
                return;

            InvokeItemEvent?.Invoke(resource);
            if(FlyoutRoot.Parent is FlyoutPresenter { Parent: Popup popup })
                popup.IsOpen = false;
        }

        private void SelectResourceFlyout_OnOpening(object sender, object e)
        {
            Initalize();
            
            var func = HeightCalculateFunc;
            FlyoutScrollView.Height = func?.Invoke() ?? (double.IsNaN(Height) ? App.MainWindow.Bounds.Height - 160 : Height);

            if (MinWidth > 0)
                FlyoutScrollView.MinWidth = MinWidth;
        }

        public void Dispose()
        {
            if (!_isIntialize)
                return;
            _isIntialize = false;
            InvokeItemEvent = null;
            TreeView.ItemInvoked -= TreeView_ItemInvoked;
            _disposable.Dispose();
            _disposable = null;
        }
    }
}
