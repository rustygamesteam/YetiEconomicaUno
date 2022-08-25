using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using YetiEconomicaUno.ViewModels.YetiObjects;
using ReactiveUIGenerator;
using DependencyPropertyGenerator;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using RustyDTO.Interfaces;
using static YetiEconomicaUno.ViewModels.YetiObjects.YetiObjectSelectorViewModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects;

public enum YetiGroupTypeMask
{
    None = 0,
    Builds = 1 << 0,
    Tools = 1 << 1,
    Resources = 1 << 2,
    Crafts = 1 << 3,
    ExchangesTo = 1 << 4,
    All = int.MaxValue
}

[DependencyProperty<string>("Header")]
[DependencyProperty<IRustyEntity>("SelectedValue")]
[DependencyProperty<IObservable<Func<IRustyEntity, bool>>>("Filter")]
[DependencyProperty<YetiGroupTypeMask>("Mask", DefaultValue = YetiGroupTypeMask.All)]
[ViewFor<YetiGroupSelectorViewModel>]
public sealed partial class YetiGroupSelector : UserControl, IDisposable
{
    public event Action<IRustyEntity> SelectedValueChanged;
    private Popup _popup = null;

    public YetiGroupSelector()
    {
        this.InitializeComponent(); 
        this.WhenActivated(disposable =>
        {
            ViewModel = new YetiGroupSelectorViewModel(Mask, disposable);

            this.DisposeWith(disposable);

            this.Bind(ViewModel, static vm => vm.SearchMask, static view => view.SearchBox.Text)
                .DisposeWith(disposable);
            this.Bind(ViewModel, static vm => vm.Source, static view => view.TreeView.ItemsSource)
                .DisposeWith(disposable);
            this.WhenAnyValue(static view => view.SelectedValue)
                .BindTo(TreeView, static view => view.SelectedItem)
                .DisposeWith(disposable);

            LabelBox.WhenAnyValue(static view => view.ActualWidth)
                .Throttle(TimeSpan.FromMilliseconds(50))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(value =>
                {
                    var minSize = Math.Max(value + 60, 160);
                    FlyoutRoot.MinWidth = minSize;
                    MinWidth = minSize;
                })
                .DisposeWith(disposable);

            Flyout.Opened += Flyout_Opened;
            TreeView.ItemInvoked += TreeView_ItemInvoked;
        });
    }

    private void Flyout_Opened(object sender, object e)
    {
        if (FlyoutRoot.Parent is FlyoutPresenter {Parent: Popup popup})
        {
            _popup = popup;
            popup.MinWidth = ActualWidth;
        }
        else
            _popup = null;
    }

    private void TreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        SelectedValue = ((YetiObjectNode)args.InvokedItem).Current;
    }

    private void Flyout_InvokeItemEvent(IRustyEntity value)
    {
        SelectedValue = value;
    }

    partial void OnSelectedValueChanged(IRustyEntity newValue)
    {
        if (_popup != null)
            _popup.IsOpen = false;
        SelectedValueChanged?.Invoke(newValue);
    }

    partial void OnFilterChanged(IObservable<Func<IRustyEntity, bool>> newValue)
    {
        ViewModel.FilterChanged(newValue);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (HorizontalAlignment != HorizontalAlignment.Stretch)
            return;

        SplitButton.Width = double.IsNaN(SplitButton.Width) ? ActualWidth : Math.Max(SplitButton.Width, ActualWidth);
    }

    public void Dispose()
    {
        _popup = null;

        Flyout.Opened -= Flyout_Opened;
        TreeView.ItemInvoked -= TreeView_ItemInvoked;
        SelectedValueChanged = null;
        Bindings.StopTracking();

        ClearValue(FilterProperty);
        ClearValue(ViewModelProperty);
    }
}
