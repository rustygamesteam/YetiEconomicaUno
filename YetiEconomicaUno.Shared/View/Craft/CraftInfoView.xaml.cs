using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveUIGenerator;
using System.Reactive.Disposables;
using ReactiveUI;
using System.Reactive.Linq;
using System.Threading;
using RustyDTO.Interfaces;
using RustyDTO.PropertyModels;
using YetiEconomicaCore.Services;
using YetiEconomicaUno.Converters;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.Craft;

[ViewFor<IRustyEntity>]
public sealed partial class CraftInfoView : UserControl, IDisposable
{
    private CompositeDisposable _disposables;

    public CraftInfoView()
    {
        this.InitializeComponent();

        this.WhenActivated(disposable =>
        {
            RemoveBtn.Click += Remove_OnClick;

            this.WhenAnyValue(static view => view.ViewModel)
                .WhereNotNull()
                .Subscribe(OnViewModelInitialize)
                .DisposeWith(disposable);

            this.DisposeWith(disposable);
        });
    }

    private void OnViewModelInitialize(IRustyEntity viewModel)
    {
        var disposable = Interlocked.Exchange(ref _disposables, new CompositeDisposable());
        disposable?.Dispose();
        disposable = _disposables;

        var hasReward = viewModel.GetUnsafe<IHasSingleReward>();
        hasReward.Entity.WhenAnyValue(static entity => entity.FullName)
            .BindTo(this, static view => view.FullName.Text)
            .DisposeWith(disposable);
        hasReward.WhenAnyValue(static reward => reward.Count)
            .Select(static count => $"Count: {count}")
            .BindTo(this, static view => view.CraftCount.Text)
            .DisposeWith(disposable);

        var longExecution = viewModel.GetUnsafe<ILongExecution>();
        longExecution.WhenAnyValue(static longExecution => longExecution.Duration)
            .Select(static duration => $"Duration: {DurationLabelConverter.GetDuration(duration)}")
            .BindTo(this, static view => view.Duration.Text)
            .DisposeWith(disposable);

        var price = viewModel.GetUnsafe<IPayable>().Price;
        price.WhenAnyValue(static price => price.Count)
            .Select(static count => $"From resources: {count}")
            .BindTo(this, static view => view.ResourcesForCrafting.Text)
            .DisposeWith(disposable);
    }

    private void Remove_OnClick(object sender, RoutedEventArgs args)
    {
        CraftService.Instance.Remove(ViewModel);
    }

    public void Dispose()
    {
        RemoveBtn.Click -= Remove_OnClick;
        var disposable = Interlocked.Exchange(ref _disposables, null);
        disposable?.Dispose();

        ClearValue(ViewModelProperty);
    }
}
