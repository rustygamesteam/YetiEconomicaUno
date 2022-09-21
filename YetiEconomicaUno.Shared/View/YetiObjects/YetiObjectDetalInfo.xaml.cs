using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveUIGenerator;
using System.Reactive.Linq;
using ReactiveUI;
using System.Reactive.Disposables;
using YetiEconomicaCore.Services;
using DynamicData;
using System.Threading;
using System.Reactive;
using Nito.Comparers;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;
using YetiEconomicaUno.View.YetiObjects.PropertyBlobls;
using YetiEconomicaCore;
using System.Windows.Markup;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace YetiEconomicaUno.View.YetiObjects;

[ViewFor<IRustyEntity>, ViewContract("Detal")]
public sealed partial class YetiObjectDetalInfo : UserControl
{
    private CompositeDisposable _disposable;
    private CompositeDisposable _reInitialize;

    private int _lastTear;

    public static ReactiveCommand<Unit, Unit> RefrashCommand { get; } = ReactiveCommand.Create(() => { });

    public YetiObjectDetalInfo()
    {
        this.InitializeComponent();

        this.WhenActivated(disposable =>
        {
            _disposable = disposable;

            this.WhenAnyValue(static view => view.ViewModel)
                .WhereNotNull()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(ViewModel_OnInitialize)
                .DisposeWith(disposable);

            RefrashCommand.Subscribe(OnRefrash)
                .DisposeWith(disposable);
        });
    }

    private void OnRefrash(Unit _)
    {
        if(ViewModel != null)
            ViewModel_OnInitialize(ViewModel);
    }

    private void ViewModel_OnInitialize(IRustyEntity viewModel)
    {
        var service = RustyEntityService.Instance;

        var dispose = Interlocked.Exchange(ref _reInitialize, new CompositeDisposable());
        dispose?.Dispose();

        var hasChild = viewModel.HasSpecialMask(EntitySpecialMask.HasChild);
        ItemsList.SelectionMode = hasChild ? ListViewSelectionMode.Single : ListViewSelectionMode.None;
        AddBtn.Visibility = hasChild ? Visibility.Visible : Visibility.Collapsed;
        _lastTear = 0;

        Initialize(viewModel, service, _reInitialize)
            .Subscribe(changeSet =>
        {
            _lastTear = 0;
            foreach (var change in changeSet)
            {
                switch(change.Reason)
                {
                    case ListChangeReason.AddRange:
                        foreach (var view in change.Range)
                            ItemsList.Items.Add(view);
                        break;
                    case ListChangeReason.RemoveRange:
                        foreach (var view in change.Range)
                            ItemsList.Items.Remove(view);
                        break;
                    case ListChangeReason.Add:
                        ItemsList.Items.Add(change.Item.Current);
                        break;
                    case ListChangeReason.Remove:
                        ItemsList.Items.Remove(change.Item.Current);
                        break;
                    case ListChangeReason.Clear:
                        ItemsList.Items.Clear();
                        break;
                    case ListChangeReason.Moved:
                        if (change.Range.Count == 0)
                        {
                            var item = change.Item;
                            ItemsList.Items.Replace(item.Previous.Value, item.Current);
                            ItemsList.Items.Replace(item.Current, item.Previous.Value);
                        }
                        break;
                }
            }
            
            if (hasChild && ItemsList.Items.Count > 0)
                _lastTear = Math.Min(ItemsList.Items.OfType<YetiGradeObjectView>().Max(static x => x.ViewModel.GetDescUnsafe<IHasOwner>().Tear + 1), TearBlobView.Tears.Length);
        }).DisposeWith(_reInitialize);
        Disposable.Create(ItemsList, static list => list.Items.Clear()).DisposeWith(_reInitialize);
    }

    private static IObservable<IChangeSet<YetiGradeObjectView>> Initialize(IRustyEntity viewModel, RustyEntityService service, CompositeDisposable disposables)
    {
        var sort = ComparerBuilder.For<IRustyEntity>()
            .OrderBy(static data => data.GetDescUnsafe<IHasOwner>().Tear)
            .ThenBy(static data => data.DisplayName);

        if (EntityDependencies.HasSpectialMask(viewModel.TypeAsIndex, EntitySpecialMask.HasChild))
        {
            return service.GetObservableEntitiesForOwner(viewModel.GetIndex())
                .Sort(sort)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Transform(entity => new YetiGradeObjectView(entity)
                    .InjectName(entity, disposables)
                    .AutoInitialize(entity, disposables));
        }
        else
        {
            return Observable.Return(new ChangeSet<YetiGradeObjectView>()
            {
                new Change<YetiGradeObjectView>(
                    ListChangeReason.Add,
                    new YetiGradeObjectView(viewModel)
                        .AutoInitialize(viewModel, disposables))
            });
        }
    }

    private void GradeAdd_OnClick(object sender, RoutedEventArgs args)
    {
        var nextTear = Math.Min(_lastTear, TearBlobView.Tears.Length);
        switch (ViewModel.Type)
        {
            case RustyEntityType.UniqueBuild:
                RustyEntityService.Instance.Create(RustyEntityType.Build, "New build", EntityBuildOptions.CreateWithOwner(ViewModel.GetIndex(), nextTear));
                break;
            case RustyEntityType.UniqueTool:
                RustyEntityService.Instance.Create(RustyEntityType.Tool, "New tool", EntityBuildOptions.CreateWithOwner(ViewModel.GetIndex(), nextTear));
                break;
        }
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _reInitialize?.Dispose();
        Unloaded -= UserControl_Unloaded;

        //Bindings.StopTracking();
    }
}
