using ReactiveUI;
using ReactiveUIGenerator;
using RustyDTO.DescPropertyModels;
using System.Reactive.Disposables;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using YetiEconomicaUno.View.YetiObjects.PropertyBlobls.Abstract;
using RustyDTO;

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls.Impls;


[ViewFor<ICraftSpeed>]
public sealed partial class CraftSpeedBlobView : SingleNumberBlob
{
    public override (double minimum, double maximum) Range { get; } = (0.01, 100);

    protected override void OnActivated(CompositeDisposable disposable)
    {
        base.OnActivated(disposable);

        Initialize(ViewModel, DescPropertyType.CraftSpeed);

        SetValueBinding("Craft speed", ViewModel.WhenAnyValue(static viewModel => viewModel.Factor), new Binding
        {
            Path = new PropertyPath(nameof(ViewModel.Factor)),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        }, disposable);
    }
}