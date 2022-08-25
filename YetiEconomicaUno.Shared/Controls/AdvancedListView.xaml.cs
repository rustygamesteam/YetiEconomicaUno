using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using DependencyPropertyGenerator;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomy.Controls;

[DependencyProperty<FrameworkElement>("NewItem")]
[DependencyProperty<FrameworkElement>("HeaderActionElement")]
[DependencyProperty<FrameworkElement>("HeaderRightPanel")]
[DependencyProperty<string>("Header")]
[DependencyProperty<object>("SelectedItem")]
[DependencyProperty<Visibility>("VisibleAddPanel", DefaultValue = Visibility.Visible)]
[DependencyProperty<IList>("Items")]
[DependencyProperty<Visibility>("HeaderActionElementVisible", IsReadOnly = true, DefaultValue = Visibility.Collapsed)]
[DependencyProperty<Visibility>("HeaderRightPanelVisible", IsReadOnly = true, DefaultValue = Visibility.Collapsed)]
[DependencyProperty<bool>("IsRightSelectedPanelEnable", IsReadOnly = true, DefaultValue = false)]
public sealed partial class AdvancedListView : UserControl
{
    public event RoutedEventHandler OnCreateItem;

    public DataTemplate ItemTemplate { get; set; }

    public Collection<FrameworkElement> RightPanel { get; } = new ();
    public Collection<FrameworkElement> RightSelectedPanel { get; } = new ();

    partial void OnHeaderActionElementChanged(FrameworkElement newValue)
    {
        HeaderActionElementVisible = newValue == null ? Visibility.Collapsed : Visibility.Visible;
    }

    partial void OnHeaderRightPanelChanged(FrameworkElement newValue)
    {
        HeaderRightPanelVisible = newValue == null ? Visibility.Collapsed : Visibility.Visible;
    }

    private readonly CompositeDisposable _disposables = new();

    public AdvancedListView()
    {
        this.InitializeComponent();

        this.WhenAnyValue(page => page.Items)
            .WhereNotNull()
            .FirstAsync()
            .Subscribe(list =>
            {
                this.WhenAnyValue(page => page.ListView.SelectedIndex)
                .Select(value => value != -1)
                .BindTo(this, page => page.IsRightSelectedPanelEnable)
                .DisposeWith(_disposables);

            }).DisposeWith(_disposables);
    }

    private void OnAddClicked()
    {
        OnCreateItem?.Invoke(this, new RoutedEventArgs());
    }

    public int SelectedIndex 
    {
        get
        {
            if (Items == null)
                return -1;
            return ListView.SelectedIndex;
        }
    }

    public Visibility RightPanelSeparatorVisible => RightPanel.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

    public bool TryGetSelectedIndex(out int index)
    {
        index = SelectedIndex;
        return index != -1;
    }

    public bool TryGetSelectedValue<T>(out T value)
    {
        if (!TryGetSelectedIndex(out var index))
        {
            value = default;
            return false;
        }

        value = (T)Items[index];
        return true;
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        OnCreateItem = null;
        _disposables.Dispose();
        Bindings.StopTracking();
    }
}