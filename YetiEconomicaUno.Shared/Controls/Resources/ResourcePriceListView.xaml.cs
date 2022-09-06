using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using YetiEconomicaUno.ViewModels.Controls.Resources;
using ReactiveUIGenerator;
using DependencyPropertyGenerator;
using System.Reactive;
using System.Reactive.Subjects;
using ReactiveUI;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Collections.Specialized;
using RustyDTO;
using RustyDTO.Interfaces;
using YetiEconomicaCore;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.Controls.Resources;

[ViewFor<ResourceStackListViewModel>]
[DependencyProperty<ICollection<ResourceStack>>("ItemsSource")]
[DependencyProperty<Func<IRustyEntity, bool>>("Filter")]
[DependencyProperty<int>("Duration")]
[DependencyProperty<bool>("HasDuration", DefaultValue = true)]
[DependencyProperty<IRustyEntity>("ExcludeEntity")]
public sealed partial class ResourcePriceListView : UserControl, IDisposable
{
    private readonly HashSet<int> _filterIndexes = new HashSet<int>();

    private BehaviorSubject<HashSet<int>> _onItemsUpdated;
    private IDisposable _itemSourceDispose;

    public ResourcePriceListView()
    {
        _onItemsUpdated = new BehaviorSubject<HashSet<int>>(_filterIndexes);

        this.InitializeComponent();

        this.WhenActivated(disposable =>
        {
            _onItemsUpdated ??= new BehaviorSubject<HashSet<int>>(_filterIndexes);

            ViewModel = new ResourceStackListViewModel();

            RemoveBtn.Click += OnRemove_Clicked;
            InsertFlyout.InvokeItemEvent += SelectResourceFlyout_OnItemInvoked;
            GridView.Items.VectorChanged += Items_VectorChanged;
            Disposable.Create(this, static view => view.GridView.Items.VectorChanged -= view.Items_VectorChanged)
                .DisposeWith(disposable);

            this.WhenAnyValue(static view => view.ItemsSource)
                .BindTo(this, static view => view.GridView.ItemsSource)
                .DisposeWith(disposable);

            this.WhenAnyValue(static view => view.GridView.SelectedIndex)
                .Select(static value => value != -1)
                .BindTo(this, static view => view.RightSelectedPanel.IsEnabled)
                .DisposeWith(disposable);

            InsertFlyout.Filter = _onItemsUpdated
                        .Select(list => (Func<IRustyEntity, bool>)(resource =>
                        {
                            var result = (Filter is null || Filter.Invoke(resource)) && list.Contains(resource.GetIndex()) is false;
                            return result;
                        }));

            this.DisposeWith(disposable);
        });
    }

    partial void OnHasDurationChanged(bool newValue)
    {
        DurationViewbox.Visibility = newValue ? Visibility.Visible : Visibility.Collapsed;
    }

    partial void OnExcludeEntityChanged(IRustyEntity oldValue, IRustyEntity newValue)
    {
        if (oldValue != null)
            _filterIndexes.Remove(oldValue.GetIndex());
        if (newValue != null)
            _filterIndexes.Add(newValue.GetIndex());
        
        _onItemsUpdated.OnNext(_filterIndexes);
    }

    partial void OnItemsSourceChanged(ICollection<ResourceStack> newValue)
    {
        InternalOnItemsSourceChanged();

        IDisposable disposable = null;
        if (newValue is INotifyCollectionChanged notifyCollectionChanged)
        {
            disposable = Observable.FromEventPattern(notifyCollectionChanged, nameof(notifyCollectionChanged.CollectionChanged))
                .Subscribe(OnItemsSourceChange);
        }

        disposable = Interlocked.Exchange(ref _itemSourceDispose, disposable);
        disposable?.Dispose();
    }

    partial void OnFilterChanged()
    {
        _onItemsUpdated.OnNext(_filterIndexes);
    }

    private void OnItemsSourceChange(EventPattern<object> obj)
    {
        InternalOnItemsSourceChanged();
    }

    private void InternalOnItemsSourceChanged()
    {
        lock (_filterIndexes)
        {
            _filterIndexes.Clear();
            foreach (var item in ItemsSource)
                _filterIndexes.Add(item.Resource.GetIndex());
        }

        _onItemsUpdated?.OnNext(_filterIndexes);
    }

    private void Items_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
    {
        AddBtn.IsEnabled = sender.Count < 4;

    }

    private void SelectResourceFlyout_OnItemInvoked(IRustyEntity resource)
    {
        ItemsSource.Add(new ResourceStack(resource, 1));
    }

    private void OnRemove_Clicked(object sernder, RoutedEventArgs args)
    {
        var item = (ResourceStack)GridView.SelectedItem;
        GridView.SelectedItem = null;
        ItemsSource.Remove(item);
    }

    public void Dispose()
    {
        RemoveBtn.Click -= OnRemove_Clicked;
        InsertFlyout.InvokeItemEvent -= SelectResourceFlyout_OnItemInvoked;
        ClearValue(FilterProperty);
        InsertFlyout.Dispose();

        var disposable = Interlocked.Exchange(ref _itemSourceDispose, null);
        disposable?.Dispose();

        disposable = Interlocked.Exchange(ref _onItemsUpdated, null);
        disposable?.Dispose();

        Bindings.StopTracking();
    }
}
