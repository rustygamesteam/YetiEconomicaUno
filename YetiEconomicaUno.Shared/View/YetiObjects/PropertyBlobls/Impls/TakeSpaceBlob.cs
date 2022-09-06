using System.Reactive.Disposables;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using ReactiveUI;
using ReactiveUIGenerator;
using RustyDTO.DescPropertyModels;
using YetiEconomicaUno.View.YetiObjects.PropertyBlobls.Abstract;

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls.Impls;

[ViewFor<ITakeSpace>]
public sealed partial class TakeSpaceBlob : SingleNumberBlob
{
    public override (int minimum, int maximum) Range => (1, 1000);

    protected override void OnActivated(CompositeDisposable disposable)
    {
        base.OnActivated(disposable);

        SetValueBinding("Take space", ViewModel.WhenAnyValue(static viewModel => viewModel.Space), new Binding
        {
            Path = new PropertyPath($"{nameof(ViewModel)}.{nameof(ViewModel.Space)}"),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        }, disposable);
    }
}