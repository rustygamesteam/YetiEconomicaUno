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
        this.WhenActivated(disposables =>
        {
            Initialize(ViewModel.Index, DescPropertyType.HasSingleReward);
            var owner = Entity;

            _countLabel = owner.Type is RustyEntityType.PlantTask ? "Harvest count" : "Count";
            CountBox.Header = _countLabel;

            ViewModel.WhenAnyValue(dependents => dependents.Count)
                .Subscribe(count => InfoBox.Text = $"{_countLabel}: {count}")
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