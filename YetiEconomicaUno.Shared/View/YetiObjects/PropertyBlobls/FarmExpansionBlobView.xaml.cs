using System.Linq;
using ReactiveUIGenerator;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls;

[ViewFor<IFarmExpansion>]
public sealed partial class FarmExpansionBlobView : BaseBlobView
{
    public FarmExpansionBlobView()
    {
        this.InitializeComponent();
    }

    public override void CompleteIntialize(CompositeDisposable disposable)
    {
        Initialize(ViewModel.Index, DescPropertyType.FarmExpansion);

        ViewModel.WhenAnyValue(static x => x.Count)
            .Select(static count => $"Plant cells: {count}")
            .BindTo(this, static view => view.InfoBox.Text)
            .DisposeWith(disposable);

        this.WhenAnyValue(static view => view.ViewModel)
            .BindTo(this, static view => view.DataContext)
            .DisposeWith(disposable);
    }

    protected override void FlyoutOpened(CompositeDisposable disposable)
    {
    }
}