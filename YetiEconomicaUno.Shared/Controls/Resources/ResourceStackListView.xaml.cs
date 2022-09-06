using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Reactive.Disposables;
using ReactiveUI;
using YetiEconomicaUno.ViewModels.Controls.Resources;
using System.Reactive.Linq;
using DependencyPropertyGenerator;
using System;
using Windows.Foundation.Collections;
using System.Reactive.Subjects;
using System.Threading;
using ReactiveUIGenerator;
using RustyDTO;
using RustyDTO.Interfaces;
using YetiEconomicaCore;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.Controls.Resources;

[DependencyProperty<string>("Header")]
[DependencyProperty<Visibility>("HeaderVisibility", DefaultValue = Visibility.Collapsed)]
[DependencyProperty<int>("MaxCount", DefaultValue = -1)]
[DependencyProperty<ICollection<ResourceStack>>("ItemsSource")]
[DependencyProperty<Func<IRustyEntity, bool>>("Filter")]
[ViewFor<ResourceStackListViewModel>]
public sealed partial class ResourceStackListView : UserControl, IDisposable
{
    partial void OnHeaderChanged(string newValue)
    {
        HeaderVisibility = string.IsNullOrEmpty(newValue) ? Visibility.Collapsed : Visibility.Visible;
        HeaderLabel.Text = newValue;
    }

    partial void OnHeaderVisibilityChanged(Visibility newValue)
    {
        HeaderBox.Visibility = newValue;
    }

    private readonly HashSet<int> _filterIndexes = new HashSet<int>();

    private BehaviorSubject<HashSet<int>> _onItemsUpdated;

    public ResourceStackListView()
    {
        this.InitializeComponent();
        _onItemsUpdated = new BehaviorSubject<HashSet<int>>(_filterIndexes);

        this.WhenActivated(disposable =>
        {
            _onItemsUpdated ??= new BehaviorSubject<HashSet<int>>(_filterIndexes);

            ViewModel = new ResourceStackListViewModel();

            InsertFlyout.InvokeItemEvent += SelectResourceFlyout_OnItemInvoked;
            GridView.Items.VectorChanged += Items_VectorChanged;
            Disposable.Create(() => GridView.Items.VectorChanged -= Items_VectorChanged)
                .DisposeWith(disposable);

            this.WhenAnyValue(static view => view.ItemsSource)
                .BindTo(this, static view => view.GridView.ItemsSource)
                .DisposeWith(disposable);

            this.WhenAnyValue(static view => view.GridView.SelectedIndex)
                .Select(static value => value != -1)
                .BindTo(this, static view => view.RightSelectedPanel.IsEnabled)
                .DisposeWith(disposable);
            
            _onItemsUpdated
                .Select(static list => list.Count)
                .Merge(this.WhenAnyValue(static view => view.MaxCount))
                .Select(_ => MaxCount == -1 || GridView.Items.Count < MaxCount)
                .BindTo(this, static view => view.AddBtn.IsEnabled)
                .DisposeWith(disposable);


            InsertFlyout.Filter = _onItemsUpdated.Select(list => (Func<IRustyEntity, bool>)(resource =>
            {
                var result = (Filter is null || Filter.Invoke(resource)) && list.Contains(resource.GetIndex()) is false;
                return result;
            }));

            this.DisposeWith(disposable);
        });
    }

    partial void OnFilterChanged()
    {
        _onItemsUpdated?.OnNext(_filterIndexes);
    }

    private void Items_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
    {
        switch (@event.CollectionChange)
        {
            case CollectionChange.Reset:
                lock (_filterIndexes)
                {
                    _filterIndexes.Clear();
                    foreach (var item in ItemsSource)
                        _filterIndexes.Add(item.Resource.GetIndex());
                }
                break;
            case CollectionChange.ItemInserted:
                lock (_filterIndexes)
                {
                    if (sender[(int)@event.Index] is ResourceStack stack)
                        _filterIndexes.Add(stack.Resource.GetIndex());
                }
                break;
            case CollectionChange.ItemRemoved:
            case CollectionChange.ItemChanged:
                return;
        }
        _onItemsUpdated.OnNext(_filterIndexes);
    }

    private void SelectResourceFlyout_OnItemInvoked(IRustyEntity resource)
    {
        ItemsSource.Add(new ResourceStack(resource, 1));
    }

    private void OnRemove_Clicked(object sender, RoutedEventArgs args)
    {
        var item = (ResourceStack)GridView.SelectedItem;
        GridView.SelectedItem = null;

        if (ItemsSource.Remove(item))
        {
            _filterIndexes.Remove(item.Resource.GetIndex());
            _onItemsUpdated.OnNext(_filterIndexes);
        }
    }

    public void Dispose()
    {
        InsertFlyout.InvokeItemEvent -= SelectResourceFlyout_OnItemInvoked;
        InsertFlyout.Dispose();

        var disposable = Interlocked.Exchange(ref _onItemsUpdated, null);
        disposable?.Dispose();

        ClearValue(FilterProperty);
    }
}
