using System.Linq;
using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUIGenerator;
using System.Reactive.Linq;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls;

[ViewFor<IInBuildProcess>]
public sealed partial class InBuildBlobView : BaseBlobView
{
    public InBuildBlobView()
    {
        this.InitializeComponent();
    }

    public override void CompleteIntialize(CompositeDisposable disposable)
    {
        Initialize(ViewModel.Index, DescPropertyType.InBuildProcess);

        ViewModel.WhenAnyValue(dependents => dependents.Build)
            .Select(build => $"Build: {build?.FullName ?? "None"}")
            .BindTo(this, static view => view.InfoBox.Text)
            .DisposeWith(disposable);
    }

    protected override void FlyoutOpened(CompositeDisposable disposable)
    {
        this.Bind(ViewModel, static vm => vm.Build, static view => view.InBuildBox.SelectedValue)
            .DisposeWith(disposable);
    }
}