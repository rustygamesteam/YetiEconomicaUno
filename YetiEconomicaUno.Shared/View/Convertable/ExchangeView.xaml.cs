using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using ReactiveUIGenerator;
using System.Threading;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.Convertable;

[ViewFor<IReactiveRustyEntity>]
public sealed partial class ExchangeView : UserControl, IDisposable
{
    private CompositeDisposable _disposables;

    public ExchangeView()
    {
        this.InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(static view => view.ViewModel)
                .WhereNotNull()
                .Subscribe(OnInjectViewModel)
                .DisposeWith(disposables);

            this.DisposeWith(disposables);
        });
    }

    private void OnInjectViewModel(IRustyEntity entity)
    {
        var disposable = Interlocked.Exchange(ref _disposables, new CompositeDisposable());
        disposable?.Dispose();
        disposable = _disposables;

        var hasExchange = entity.GetDescUnsafe<IHasExchange>();

        hasExchange.FromEntity.WhenAnyValue(static entity => entity.FullName)
            .BindTo(this, static view => view.NameBox.Text)
            .DisposeWith(disposable);
        
        CountBox.Value = hasExchange.FromRate;
        CountBox.WhenAnyValue(static view => view.Value)
            .BindTo(hasExchange, static exchange => exchange.FromRate)
            .DisposeWith(disposable);
    }

    public void Dispose()
    {
        var disposable = Interlocked.Exchange(ref _disposables, null);
        disposable?.Dispose();
    }
}