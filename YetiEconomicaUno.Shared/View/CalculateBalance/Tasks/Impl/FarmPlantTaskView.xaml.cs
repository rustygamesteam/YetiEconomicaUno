using System;
using System.Reactive.Disposables;
using Microsoft.UI.Xaml.Controls;
using ReactiveUIGenerator;
using YetiEconomicaUno.ViewModels.CalculateBalance.Progress;
using ReactiveUI;
using RustyDTO.DescPropertyModels;
using YetiEconomicaCore.Services;
using RustyDTO.Interfaces;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.CalculateBalance.Tasks;

[ViewFor<FarmPlantTask>]
public sealed partial class FarmPlantTaskView : UserControl, IDisposable
{
    private CompositeDisposable _taskDisposable;

    public FarmPlantTaskView()
    {
        this.InitializeComponent();

        this.WhenActivated(disposables =>
        {
            ResourcesList.Filter = CanPlanting;

            this.WhenAnyValue(static view => view.ViewModel)
                .Subscribe(OnInjectTask)
                .DisposeWith(disposables);

            Disposable.Create(ResourcesList, static view => view.Filter = null)
                .DisposeWith(disposables);

            this.DisposeWith(disposables);
        });
    }

    private void OnInjectTask(FarmPlantTask task)
    {
        _taskDisposable?.Dispose();
        _taskDisposable = new CompositeDisposable();

        task.EvaluteObservable.Subscribe(args =>
        {
            ResourcesList.Filter = CanPlanting;
        }).DisposeWith(_taskDisposable);
    }

    private bool CanPlanting(IRustyEntity resource)
    {
        if (PlantsService.Instance.TryGetPlant(resource, out var plant))
        {
            var dependents = plant.GetDescUnsafe<IHasDependents>();
            if (dependents.Required is null && dependents.VisibleAfter is null)
                return true;

            ViewModel.HasInBag(plant);
        }

        return false;
    }

    public void Dispose()
    {
        _taskDisposable?.Dispose();
        _taskDisposable = null;
    }
}