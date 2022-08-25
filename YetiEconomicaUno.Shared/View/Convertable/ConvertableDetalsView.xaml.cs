using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using YetiEconomicaUno.ViewModels.Convertables;
using ReactiveUI;
using System.Reactive.Disposables;
using YetiEconomicaCore;
using System.Reactive;
using YetiEconomicaCore.Services;
using System.Collections.Specialized;
using System.Reactive.Linq;
using System.Collections;
using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using ReactiveUIGenerator;
using RustyDTO.Interfaces;
using RustyDTO.PropertyModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.Convertable;

[ViewFor<ConvertableViewModel>, ViewContract("Detal")]
public sealed partial class ConvertableDetalsView : UserControl, IDisposable
{
    private IRustyEntity _exclude;
    private CompositeDisposable _viewModelDisposable;

    public ConvertableDetalsView()
    {
        this.InitializeComponent();

        this.WhenActivated(disposable =>
        {
            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Subscribe(ViewModel_OnInitialize).DisposeWith(disposable);

            this.OneWayBind(ViewModel, static vm => (IList)vm.Exchanges, static view => view.AdvancedList.Items)
                .DisposeWith(disposable);
            
            this.WhenAnyValue(view => view.AdvancedList.SelectedItem)
                .Subscribe(item =>
                {
                    ViewModel.SelectedExchange = (IRustyEntity)item;
                    RemoveBtn.IsEnabled = item is not null;
                }).DisposeWith(disposable);

            this.DisposeWith(disposable);
        });
    }

    private void ViewModel_OnInitialize(ConvertableViewModel viewModel)
    {
        _viewModelDisposable?.Dispose();
        var disposable = _viewModelDisposable = new CompositeDisposable();

        viewModel.InitializeDetals(disposable);
        _exclude = viewModel.ConvertableToResource;

        var filter = new BehaviorSubject<Func<IRustyEntity, bool>>(OnFilter).DisposeWith(disposable);
        var notifyCollection = (INotifyCollectionChanged)viewModel.Exchanges;
        Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                action => notifyCollection.CollectionChanged += action,
                action => notifyCollection.CollectionChanged -= action)
            .Subscribe(_ => filter.OnNext(filter.Value)).DisposeWith(disposable);

        Flyout.Filter = filter;
    }

    private bool OnFilter(IRustyEntity resource)
    {
        return resource != _exclude && !ViewModel.Exchanges.Select(static x => x.GetUnsafe<IHasExchange>().FromEntity).Contains(resource);
    }

    private void RemoveExchagne_OnClicked()
    {
        ConvertablesService.Instance.Remove(ViewModel.SelectedExchange);
    }

    private void SelectResourceFlyout_OnItemInvoked(IRustyEntity resource)
    {
        ConvertablesService.Instance.Create(ViewModel.ConvertableToResource, resource);
    }

    public void Dispose()
    {
        Flyout?.Dispose();

        _viewModelDisposable?.Dispose();
        _viewModelDisposable = null;

        Bindings?.StopTracking();
    }
}
