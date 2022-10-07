using System.Reactive.Disposables;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using ReactiveUI;
using ReactiveUIGenerator;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using YetiEconomicaUno.View.YetiObjects.PropertyBlobls.Abstract;

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls.Impls;

[ViewFor<ITakeSpace>]
public sealed partial class TakeSpaceBlob : SingleNumberBlob
{
    public override (double minimum, double maximum) Range => (1, 1000);

    public override void CompleteIntialize(CompositeDisposable disposable)
    {
        base.CompleteIntialize(disposable);
        
        Initialize(ViewModel, DescPropertyType.TakeSpace);

        SetValueBinding("Take space", ViewModel.WhenAnyValue(static viewModel => viewModel.Space), new Binding
        {
            Path = new PropertyPath(nameof(ViewModel.Space)),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        }, disposable);
    }
}