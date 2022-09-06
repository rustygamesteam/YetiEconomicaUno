using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUIGenerator;
using ReactiveUI;
using RustyDTO;
using RustyDTO.DescPropertyModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls;

[ViewFor<IToolSettings>]
public sealed partial class ToolInfoBlobView : BaseBlobView
{
    public ToolInfoBlobView()
    {
        this.InitializeComponent();
        this.WhenActivated(disposables =>
        {
            Initialize(ViewModel.Index, DescPropertyType.ToolSettings);

            ViewModel.WhenAnyValue(static x => x.Efficiency)
                .CombineLatest(ViewModel.WhenAnyValue(static x => x.Strength), ViewModel.WhenAnyValue(static x => x.RechargeEvery))
                .Select(static tuple => $"Efficiency: {tuple.First} | Strength: {tuple.Second} | Recharge every hours: {tuple.Third}")
                .BindTo(this, static view => view.InfoBox.Text)
                .DisposeWith(disposables);

            this.WhenAnyValue(static view => view.ViewModel)
                .BindTo(this, static view => view.DataContext)
                .DisposeWith(disposables);
        });
    }

    protected override void FlyoutOpened(CompositeDisposable disposable)
    {
    }
}