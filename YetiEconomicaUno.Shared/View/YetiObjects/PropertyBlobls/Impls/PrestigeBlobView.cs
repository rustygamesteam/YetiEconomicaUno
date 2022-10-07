using System.Reactive.Disposables;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using ReactiveUI;
using ReactiveUIGenerator;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using YetiEconomicaUno.View.YetiObjects.PropertyBlobls.Abstract;

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls.Impls;

[ViewFor<IHasPrestige>]
public sealed partial class PrestigeBlobView : SingleNumberBlob
{
    public override (double minimum, double maximum) Range => (1, 1000000);

    public override void CompleteIntialize(CompositeDisposable disposable)
    {
        base.CompleteIntialize(disposable);

        Initialize(ViewModel, DescPropertyType.HasPrestige);

        SetValueBinding("Prestige", ViewModel.WhenAnyValue(static viewModel => viewModel.Prestige), new Binding
        {
            Path = new PropertyPath(nameof(ViewModel.Prestige)),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        }, disposable);
    }
}