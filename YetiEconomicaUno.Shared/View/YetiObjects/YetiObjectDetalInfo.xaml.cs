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
using RustyDTO.Interfaces;
using RustyDTO.PropertyModels;
using YetiEconomicaUno.View.YetiObjects.PropertyBlobls;

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
                    case ChangeReason.Add:
                        ItemsList.Items.Add(change.Current);
                        break;
                    case ChangeReason.Remove:
                        ItemsList.Items.Remove(change.Current);
                        break;
                    case ChangeReason.Moved:
                        ItemsList.Items.Replace(change.Previous, change.Current);
                        ItemsList.Items.Replace(change.Current, change.Previous);
                        break;
                }
            }

            if (hasChild && ItemsList.Items.Count > 0)
                _lastTear = Math.Min(ItemsList.Items.OfType<YetiGradeObjectView>().Max(static x => x.ViewModel.GetUnsafe<IHasOwner>().Tear + 1), TearBlobView.Tears.Length);
        }).DisposeWith(_reInitialize);
        Disposable.Create(ItemsList, static list => list.Items.Clear()).DisposeWith(_reInitialize);
    }

    private static IObservable<IChangeSet<YetiGradeObjectView, int>> Initialize(IRustyEntity viewModel, RustyEntityService service, CompositeDisposable disposables)
    {
        var sort = ComparerBuilder.For<IRustyEntity>()
            .OrderBy(static data => data.GetUnsafe<IHasOwner>().Tear)
            .ThenBy(static data => data.DisplayName);

        switch (viewModel.Type)
        {
            case RustyEntityType.PVE:
            case RustyEntityType.Tech:
                var set = new ChangeSet<YetiGradeObjectView, int>(1)
                {
                    new Change<YetiGradeObjectView, int>(
                        ChangeReason.Add, 
                        viewModel.Index, 
                        new YetiGradeObjectView(viewModel)
                            .AutoInitialize(viewModel, disposables))
                };
                return Observable.Return(set);
            case RustyEntityType.UniqueBuild:
                return service.ConnectToEntity(static data => data.Type is RustyEntityType.Build)
                    .Filter(data => data.GetUnsafe<IHasOwner>().Owner.Index == viewModel.Index)
                    .Sort(sort)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Transform(data => new YetiGradeObjectView(data)
                        .Initialize(data, disposables)
                        .InjectName(data, disposables)
                        .InjectAsBuild(viewModel, data, disposables)
                        .InjectPriceWithDurantion(data, disposables));
            case RustyEntityType.UniqueTool:
                return service.ConnectToEntity(static data => data.Type is RustyEntityType.Tool)
                    .Filter(data => data.GetUnsafe<IHasOwner>().Owner.Index == viewModel.Index)
                    .Sort(sort)
                    .Transform(data => new YetiGradeObjectView(data)
                        .Initialize(data, disposables)
                        .InjectName(data, disposables)
                        .InjectAsTool(viewModel, data, disposables)
                        .InjectPriceWithDurantion(data, disposables));
            default:
                throw new NotImplementedException();
        }
    }

    private void GradeAdd_OnClick(object sender, RoutedEventArgs args)
    {
        var nextTear = Math.Min(_lastTear, TearBlobView.Tears.Length);
        switch (ViewModel.Type)
        {
            case RustyEntityType.UniqueBuild:
                RustyEntityService.Instance.Create(RustyEntityType.Build, "New build", EntityBuildOptions.CreateWithOwner(ViewModel.Index, nextTear));
                break;
            case RustyEntityType.UniqueTool:
                RustyEntityService.Instance.Create(RustyEntityType.Tool, "New tool", EntityBuildOptions.CreateWithOwner(ViewModel.Index, nextTear));
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
