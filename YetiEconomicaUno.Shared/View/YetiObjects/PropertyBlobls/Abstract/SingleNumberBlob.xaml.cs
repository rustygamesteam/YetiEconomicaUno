using System;
using Microsoft.UI.Xaml.Controls;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.UI.Xaml.Data;
using ReactiveUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects.PropertyBlobls.Abstract;

public abstract partial class SingleNumberBlob : BaseBlobView, IActivatableView
{
    public abstract (int minimum, int maximum) Range { get; }

    public SingleNumberBlob()
    {
        this.InitializeComponent();
        this.WhenActivated(OnActivated);
    }

    protected virtual void OnActivated(CompositeDisposable disposable)
    {
        var range = Range;

        NumberBox.Minimum = range.minimum;
        NumberBox.Maximum = range.maximum;

        Disposable.Create(() =>
        {
            NumberBox.DataContext = null;
            DataContext = null;
        }).DisposeWith(disposable);
    }

    protected void SetValueBinding(string header, IObservable<int> valueObservable, Binding binding, CompositeDisposable disposable)
    {
        NumberBox.Header = header;
        NumberBox.SetBinding(NumberBox.ValueProperty, binding);

        valueObservable.Select(value => $"{header}: {value}")
            .BindTo(this, view => view.InfoBox.Text)
            .DisposeWith(disposable);
    }

    protected override void FlyoutOpened(CompositeDisposable disposable)
    {

    }
}