using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DependencyPropertyGenerator;
using YetiEconomicaUno.ViewModels.YetiObjects;
using ReactiveUIGenerator;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.UI.Xaml.Controls.Primitives;
using RustyDTO.Interfaces;
using static YetiEconomicaUno.ViewModels.YetiObjects.YetiObjectSelectorViewModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects;

public enum YetiObjectTypeMask
{
    None = 0,
    Builds = 1 << 0,
    Tools = 1 << 1,
    Techs = 1 << 2,
    Plants = 1 << 3,
    Crafts = 1 << 4,
    Resources = 1 << 5,
    Exchages = 1 << 6,
    RequiredInDependencies = 1 << 7,
    BuildsWithTech = Builds | Techs,
    All = int.MaxValue
}

[DependencyProperty<string>("Header")]
[DependencyProperty<IRustyEntity>("SelectedValue")]
[DependencyProperty<IObservable<Func<IRustyEntity, bool>>>("Filter")]
[DependencyProperty<YetiObjectTypeMask>("Mask", DefaultValue = YetiObjectTypeMask.All)]
[ViewFor<YetiObjectSelectorViewModel>]
public sealed partial class YetiObjectSelector : UserControl, IDisposable
{
    public event Action<IRustyEntity> SelectedValueChanged;
    private Popup _popup = null;

    public YetiObjectSelector()
    {
        this.InitializeComponent();

        this.WhenActivated(disposable =>
        {
            ViewModel = new YetiObjectSelectorViewModel(Mask, this, disposable);

            this.Bind(ViewModel, static vm => vm.SearchMask, static view => view.SearchBox.Text)
                .DisposeWith(disposable);
            this.Bind(ViewModel, static vm => vm.Source, static view => view.TreeView.ItemsSource)
                .DisposeWith(disposable);

            Flyout.Opened += Flyout_Opened;
            TreeView.ItemInvoked += TreeView_ItemInvoked;
            
            LabelBox.WhenAnyValue(static view => view.Text)
                .Throttle(TimeSpan.FromMilliseconds(10))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(_ => Math.Max(LabelBox.ActualWidth + 60, 120))
                .BindTo(this, static view => view.MinWidth)
                .DisposeWith(disposable);

            this.DisposeWith(disposable);
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
        if (args.InvokedItem is YetiObjectNode node)
        {
            if(node.IsGroup is false)
                SelectedValue = node.Current;
        }
        else
            SelectedValue = (IRustyEntity)args.InvokedItem;
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
        if (ViewModel != null)
        {
            ViewModel.FilterChanged(newValue);
            return;
        }
        
        this.WhenAnyValue(static x => x.ViewModel)
            .WhereNotNull()
            .Subscribe(x => x.FilterChanged(newValue));
        
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

        try
        {
            ClearValue(FilterProperty);
            ClearValue(ViewModelProperty);
        }
        catch
        {
        }
    }
}
