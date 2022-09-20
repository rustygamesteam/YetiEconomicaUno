using System.Reactive.Disposables;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using ReactiveUI;
using ReactiveUIGenerator;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using YetiEconomicaUno.View.YetiObjects.PropertyBlobls.Abstract;

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls.Impls;

[ViewFor<ITechSpeed>]
public sealed partial class TechSpeedBlobView : SingleNumberBlob
{
    public override (double minimum, double maximum) Range => (0.01, 1000);

    protected override void OnActivated(CompositeDisposable disposable)
    {
        base.OnActivated(disposable);

        Initialize(ViewModel, DescPropertyType.TechSpeed);

        SetValueBinding("Craft speed", ViewModel.WhenAnyValue(static viewModel => viewModel.Factor), new Binding
        {
            Path = new PropertyPath(nameof(ViewModel.Factor)),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        }, disposable);
    }
}