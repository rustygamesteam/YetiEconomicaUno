﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using YetiEconomicaUno.ViewModels.YetiObjects;
using ReactiveUIGenerator;
using Microsoft.UI.Xaml.Data;
using System.Reactive.Disposables;
using DynamicData;
using ReactiveUI;
using System;
using System.Reactive.Linq;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[ViewFor<YetiObjectsPageViewModel>]
public sealed partial class YetiObjectsPage : Page, IDisposable
{
    private CompositeDisposable _disposables;

    public YetiObjectsPage()
    {
        this.InitializeComponent();

        this.WhenActivated(disposable =>
        {
            _disposables = disposable;
            ViewModel.Initialize(disposable);
            this.OneWayBind(ViewModel, static vm => vm.ItemSource, static view => view.DetailsView.ItemsSource).DisposeWith(disposable);
            this.Bind(ViewModel, static vm => vm.SearchMask, static view => view.SearchBox.Text).DisposeWith(disposable);

            FilterListView.ItemsSource = YetiObjectsPageViewModel.Filters;

            Observable.FromEventPattern<SelectionChangedEventHandler, SelectionChangedEventArgs>(
                    handler => FilterListView.SelectionChanged += handler,
                    handler => FilterListView.SelectionChanged -= handler)
                .Subscribe(_ =>
                {
                    var items = FilterListView.SelectedItems;

                    if (items.Count == YetiObjectsPageViewModel.Filters.Length)
                        FilterLabel.Text = "All";
                    else
                    {
                        FilterLabel.Text = items.Count switch
                        {
                            0 => "All",
                            > 3 => "Mixed...",
                            _ => string.Join(", ", items.Cast<EnumWithHeader>().Select(item => item.Name))
                        };
                    }

                    var source = items.Count == 0 ? YetiObjectsPageViewModel.Filters : items.Cast<EnumWithHeader>();
                    ViewModel.UpdateBitmask(source);
                })
                .DisposeWith(disposable);

            //FilterLabel.

            this.DisposeWith(disposable);

        });
    }

    private void NewName_OnLoaded(object sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)sender;
        textBox.Loaded -= NewName_OnLoaded;

        textBox.Text = string.Empty;


        textBox.SetBinding(TextBox.TextProperty, new Binding
        {
            Source = ViewModel,
            Mode = BindingMode.TwoWay,
            Path = new PropertyPath(nameof(ViewModel.NewName)),
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        Disposable.Create(textBox, textBox => textBox.ClearValue(TextBox.TextProperty))
            .DisposeWith(_disposables);

        /*void TextBox_OnTextChanged(object sender, TextChangedEventArgs args)
        {
            ViewModel.NewName = textBox.Text;
        }

        textBox.TextChanged += TextBox_OnTextChanged;
        Disposable.Create(textBox, textBox => textBox.TextChanged -= TextBox_OnTextChanged)
            .DisposeWith(_disposables);*/
    }

    private void NewType_OnLoaded(object sender, RoutedEventArgs e)
    {
        var comboBox = (ComboBox)sender;
        comboBox.Loaded -= NewType_OnLoaded;

        var values = YetiObjectsPageViewModel.Filters;

        comboBox.DisplayMemberPath = "Name";
        comboBox.SelectedValuePath = "Type";
        comboBox.ItemsSource = values;

        comboBox.SetBinding(ComboBox.SelectedValueProperty, new Binding
        {
            Source = ViewModel,
            Mode = BindingMode.TwoWay,
            Path = new PropertyPath(nameof(ViewModel.NewType))
        });
        comboBox.SelectedItem = values[0];

        Disposable.Create(comboBox, comboBox => comboBox.ClearValue(ComboBox.SelectedValueProperty))
            .DisposeWith(_disposables);
    }

    private void Add_OnClicked()
    {
        ViewModel.TryAdd();
    }

    private void RefrashView()
    {
        YetiObjectDetalInfo.RefrashCommand.Execute().Subscribe();
    }

    private void ShowGraphByDependents_OnClick(object sender, RoutedEventArgs args)
    {
        LinkGraph.CreateByDependents();
    }
    private void ShowGraphByProduct_OnClick(object sender, RoutedEventArgs args)
    {
        LinkGraph.CreateByProduct();
    }

    public void Dispose()
    {
        Bindings.StopTracking();
    }
}
