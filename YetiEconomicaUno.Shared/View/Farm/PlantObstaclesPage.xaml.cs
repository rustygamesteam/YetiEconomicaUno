using System;
using System.Reactive.Disposables;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using ReactiveUIGenerator;
using RustyDTO.Interfaces;
using YetiEconomicaCore.Services;
using YetiEconomicaUno.ViewModels.Farm;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.Farm;

[ViewFor<PlantObstaclesViewModel>]
public sealed partial class PlantObstaclesPage : Page, IDisposable
{
    private IRustyEntity _selectedEntity;

    public PlantObstaclesPage()
    {
        this.InitializeComponent();

        this.WhenActivated(disposable =>
        {
            var service = PlantsService.Instance;

            DefaultCountBox.Value = service.DefaultCellsCount;
            DefaultCountBox.WhenAnyValue(static box => box.Value)
                .BindTo(service, static plantsService => plantsService.DefaultCellsCount)
                .DisposeWith(disposable);

            ObstaclesList.Items = service.GetObstaclesAsObservable(disposable);
            ObstaclesList.WhenAnyValue(view => view.SelectedItem).Subscribe(item =>
            {
                _selectedEntity = item as IRustyEntity;
                RemoveBtn.IsEnabled = _selectedEntity != null;
            }).DisposeWith(disposable);

            this.DisposeWith(disposable);
        });
    }

    private void Add_OnClick(object sender, RoutedEventArgs args)
    {
        PlantsService.Instance.CreateObstacle();
    }

    private void Remove_OnClick(object sender, RoutedEventArgs args)
    {
        PlantsService.Instance.Remove(_selectedEntity);
    }

    public void Dispose()
    {
        _selectedEntity = null;
        Bindings?.StopTracking();
    }
}