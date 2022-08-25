using System;
using Microsoft.UI.Xaml.Controls;
using ReactiveUIGenerator;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using RustyDTO.Interfaces;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.Craft;

[ViewFor<IRustyEntity>, ViewContract("Detal")]
public sealed partial class CraftDetalInfoView : UserControl, IDisposable
{
    private CompositeDisposable _viewModelDisposable;

    public CraftDetalInfoView()
    {
        this.InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Subscribe(entity =>
                {
                    _viewModelDisposable?.Dispose();
                    _viewModelDisposable = new CompositeDisposable();
                    RustyEntityView.AutoInitialize(entity, _viewModelDisposable);
                }).DisposeWith(disposables);

            this.DisposeWith(disposables);
        });
    }

    public void Dispose()
    {
        _viewModelDisposable?.Dispose();
        _viewModelDisposable = null;
    }
}
