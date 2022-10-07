using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices.WindowsRuntime;
using ReactiveUI;
using ReactiveUIGenerator;
using System.Reactive.Linq;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls;

[ViewFor<IHasOwner>]
public sealed partial class TearBlobView : BaseBlobView
{
    public static KeyValuePair<int, string>[] Tears { get; } = Enumerable.Range(0, 8).Select<int, KeyValuePair<int, string>>(static i => new(i, $"Tear {i + 1}")).ToArray();
    public static KeyValuePair<int, string>[] PVETears { get; } = Enumerable.Range(0, 20).Select<int, KeyValuePair<int, string>>(static i => new(i, $"Tear {i + 1}")).ToArray();

    public static KeyValuePair<int, string>[] GetTears(RustyEntityType type) => type is RustyEntityType.PVE ? PVETears : Tears;

    public TearBlobView()
    {
        this.InitializeComponent();
    }

    public override void CompleteIntialize(CompositeDisposable disposable)
    {
        Initialize(ViewModel.Index, DescPropertyType.HasOwner);

        ViewModel.WhenAnyValue(static x => x.Tear)
            .Select(static tear => $"Tear: {tear + 1}")
            .BindTo(this, static view => view.InfoBox.Text)
            .DisposeWith(disposable);
    }

    protected override void FlyoutOpened(CompositeDisposable disposable)
    {
        TearBox.ItemsSource = GetTears(Entity.Type);
        this.Bind(ViewModel, static vm => vm.Tear, static view => view.TearBox.SelectedValue)
            .DisposeWith(disposable);
    }
}