using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using DynamicData.Aggregation;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using ReactiveUIGenerator;
using YetiEconomicaCore.Services;
using YetiEconomicaUno.ViewModels.Convertables;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.Convertable;

[ViewFor<ConvertableViewModel>]
public sealed partial class ConvertableInfo : UserControl, IDisposable
{
    private CompositeDisposable _disposables;

    public ConvertableInfo()
    {
        this.InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(static view => view.ViewModel)
                .WhereNotNull()
                .Subscribe(OnViewModelInitialize)
                .DisposeWith(disposables);

            this.DisposeWith(disposables);
        });
    }

    private void OnViewModelInitialize(ConvertableViewModel viewModel)
    {
        var disposable = Interlocked.Exchange(ref _disposables, new CompositeDisposable());
        disposable?.Dispose();
        disposable = _disposables;

        var owner = viewModel.ConvertableToResource;
        owner.WhenAnyValue(static entity => entity.FullName)
            .BindTo(this, static view => view.NameBox.Text)
            .DisposeWith(disposable);

        CountBox.Text = "Exchange count: 0";
        viewModel.ObservableExchanges.Count()
            .Select(static value => $"Exchange count: {value}")
            .BindTo(this, static view => view.CountBox.Text)
            .DisposeWith(disposable);
    }

    private void RemoveExchanges_OnClick()
    {
        ViewModel.Remove_OnClicked();
    }

    public void Dispose()
    {
        var disposable = Interlocked.Exchange(ref _disposables, null);
        disposable?.Dispose();
    }
}
