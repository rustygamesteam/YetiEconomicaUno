using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using RustyDTO.Interfaces;
using YetiEconomicaCore.Services;
using YetiEconomicaUno.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomy.Controls.Resources;

internal sealed partial class ResourceView : UserControl, IViewFor<ResourceViewModel>, IDisposable
{
    public static KeyValuePair<int, string>[] Tears { get; } = Enumerable.Range(0, 8).Select<int, KeyValuePair<int, string>>(static i => new(i, $"Tear {i + 1}")).ToArray();

    public static DependencyProperty ViewModelProperty { get; } = DependencyProperty.Register(nameof(ViewModel), typeof(ResourceViewModel), typeof(ResourceView), PropertyMetadata.Create(defaultValue: default));
    public ResourceViewModel ViewModel => (ResourceViewModel)GetValue(ViewModelProperty);

    public IRustyEntity Model
    {
        set
        {
            SetValue(ViewModelProperty, new ResourceViewModel(value));
        }
    }

    public static IReadOnlyCollection<IRustyEntity> Groups { get; } = ResourceService.Instance.Groups;

    ResourceViewModel IViewFor<ResourceViewModel>.ViewModel
    {
        get => ViewModel;
        set
        {

        }
    }

    object IViewFor.ViewModel
    {
        get => ViewModel;
        set
        {

        }
    }

    public ResourceView()
    {
        this.InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, static vm => vm.UseInCraftsCount, static view => view.UseInCraftsInfo.Text, static value => $"Usage in crafts: {value}")
                .DisposeWith(disposables);
            this.OneWayBind(ViewModel, static vm => vm.UseInBuildsCount, static view => view.UseInSingleCreate.Text, static value => $"Usage in created entity: {value}")
                .DisposeWith(disposables);
            this.OneWayBind(ViewModel, static vm => vm.UseInConvertable, static view => view.UseInExchanges.Text, static value => $"Usage in convertables: {value}")
                .DisposeWith(disposables);

            this.DisposeWith(disposables);
        });
    }

    public void Dispose()
    {
        Bindings.StopTracking();

        ClearValue(ViewModelProperty);
    }
}
