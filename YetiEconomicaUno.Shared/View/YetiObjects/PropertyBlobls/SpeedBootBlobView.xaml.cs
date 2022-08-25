using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUIGenerator;
using ReactiveUI;
using RustyDTO;
using RustyDTO.PropertyModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls;

[ViewFor<IBoostSpeed>]
public sealed partial class SpeedBootBlobView : BaseBlobView
{
    public SpeedBootBlobView()
    {
        this.InitializeComponent();
        this.WhenActivated(disposables =>
        {
            Initialize(ViewModel.Index, EntityPropertyType.BoostSpeed);

            ViewModel.WhenAnyValue(static x => x.CraftSpeed)
                .CombineLatest(ViewModel.WhenAnyValue(static x => x.TechSpeed))
                .Select(static tuple => $"Speed craft/tech: {tuple.First * 100}% / {tuple.Second * 100}%")
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