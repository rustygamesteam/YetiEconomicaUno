using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using DependencyPropertyGenerator;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Colors = Microsoft.UI.Colors;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.Flyouts;

[DependencyProperty<UIElement>("Content")]
[DependencyProperty<Symbol>("Icon")]
[DependencyProperty<string>("Label")]
[RoutedEvent<RoutedEventHandler>("Click", RoutedEventStrategy.Direct)]
public sealed partial class FlyoutWithBtn : Flyout
{
    public FlyoutWithBtn()
    {
        this.InitializeComponent();
        FlyoutBtn.Click += FlyoutBtnOnClick;
    }

    private void FlyoutBtnOnClick(object sender, RoutedEventArgs e)
    {
        Click.Invoke(sender, e);
    }

    partial void OnIconChanged(Symbol newValue)
    {
        FlyoutBtn.Icon = new SymbolIcon(newValue);
    }

    partial void OnLabelChanged(string newValue)
    {
        FlyoutBtn.Label = newValue;
    }

    partial void OnContentChanged(UIElement newValue)
    {
        ContentPresenter.Content = newValue;
    }

    public void SetBtnVisibility(bool value)
    {
        FlyoutBtnView.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }
}