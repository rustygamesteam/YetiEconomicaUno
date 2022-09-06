using System.Reactive.Disposables;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using YetiEconomicaUno.View.YetiObjects.PropertyBlobls.Abstract;
using ReactiveUIGenerator;
using ReactiveUI;
using RustyDTO.DescPropertyModels;

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls.Impls;

[ViewFor<IHasCraftingQueue>]
public sealed partial class CraftingQueueBlobView : SingleNumberBlob
{
    public override (int minimum, int maximum) Range => (1, 1000);

    protected override void OnActivated(CompositeDisposable disposable)
    {
        base.OnActivated(disposable);

        SetValueBinding("Queue slots", ViewModel.WhenAnyValue(static model => model.Slots), new Binding
        {
            Path = new PropertyPath($"{nameof(ViewModel)}.{nameof(ViewModel.Slots)}"),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        }, disposable);
    }
}