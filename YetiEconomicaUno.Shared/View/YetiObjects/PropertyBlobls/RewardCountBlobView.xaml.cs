using System;
using ReactiveUIGenerator;
using ReactiveUI;
using System.Reactive.Disposables;
using RustyDTO;
using RustyDTO.DescPropertyModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls;


[ViewFor<IHasSingleReward>]
public sealed partial class RewardCountBlobView : BaseBlobView
{
    private string _countLabel;

    public RewardCountBlobView()
    {
        this.InitializeComponent();
    }

    public override void CompleteIntialize(CompositeDisposable disposable)
    {
        Initialize(ViewModel, DescPropertyType.HasSingleReward);
        var owner = Entity;

        _countLabel = owner.Type is RustyEntityType.PlantTask ? "Harvest count" : "Count";
        CountBox.Header = _countLabel;

        ViewModel.WhenAnyValue(static vm => vm.Count)
            .Subscribe(count =>
            {
                InfoBox.Text = $"{_countLabel}: {count}";
            })
            .DisposeWith(disposable);
    }

    protected override void FlyoutOpened(CompositeDisposable disposable)
    {
        
    }
}