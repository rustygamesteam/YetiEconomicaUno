using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUIGenerator;
using ReactiveUI;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using YetiEconomicaUno.Converters;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls;

[ViewFor<ILongExecution>]
public sealed partial class LongExecutionBlobView : BaseBlobView
{
    public LongExecutionBlobView()
    {
        this.InitializeComponent();

        this.WhenActivated(disposables =>
        {
            Initialize(ViewModel.Index, DescPropertyType.LongExecution);
            
            ViewModel.WhenAnyValue(dependents => dependents.Duration)
                .Subscribe(duration =>
                {
                    var durationString = DurationLabelConverter.GetDuration(duration);
                    DurationBox.Header = $"Duration ({durationString})";
                    InfoBox.Text = $"Duration: {durationString}";
                })
                .DisposeWith(disposables);
        });
    }

    protected override void FlyoutOpened(CompositeDisposable disposable)
    {

    }
}