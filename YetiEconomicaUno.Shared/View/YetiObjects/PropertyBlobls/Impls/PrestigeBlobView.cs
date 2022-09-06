using System.Reactive.Disposables;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using ReactiveUI;
using ReactiveUIGenerator;
using RustyDTO.DescPropertyModels;
using YetiEconomicaUno.View.YetiObjects.PropertyBlobls.Abstract;

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls.Impls;

[ViewFor<IHasPrestige>]
public sealed partial class PrestigeBlobView : SingleNumberBlob
{
    public override (int minimum, int maximum) Range => (1, 1000000);

    protected override void OnActivated(CompositeDisposable disposable)
    {
        base.OnActivated(disposable);

        SetValueBinding("Prestige", ViewModel.WhenAnyValue(static viewModel => viewModel.Prestige), new Binding
        {
            Path = new PropertyPath($"{nameof(ViewModel)}.{nameof(ViewModel.Prestige)}"),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        }, disposable);
    }
}