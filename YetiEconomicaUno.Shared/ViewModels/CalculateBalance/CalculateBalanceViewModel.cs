using DynamicData;
using Microsoft.UI.Xaml.Controls;
using Nito.Comparers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RustyDTO.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using RustyDTO;
using RustyDTO.PropertyModels;
using YetiEconomicaCore.Database;
using YetiEconomicaCore.Services;
using YetiEconomicaUno.Helpers;
using YetiEconomicaUno.Services;
using YetiEconomicaUno.ViewModels.CalculateBalance.Progress;

namespace YetiEconomicaUno.ViewModels.CalculateBalance;

public class CalculateBalanceViewModel : BaseViewModel
{
    public const int ChunkSize = 5;
    public static DateTime BeginTime { get; } = new DateTime().AddHours(CalculateBalanceService.Instance.Sessions[0].Hour);
    public static Dictionary<IRustyEntity, double> Wallet = new();
    public static Dictionary<ToolsEnum, ToolProgressInfo> ToolCache = new();
    public static HashSet<int> UserBag = new(64);

    private static readonly UserDataDump _emptyDump;

    private bool _isRecalculate;
    private ChankData[] _chanks = Array.Empty<ChankData>();
    private ReadOnlyObservableCollection<ProgressTask> _progress;

    public ReadOnlyObservableCollection<ProgressTask> Progress => _progress;

    [Reactive]
    public UserDataDump PreSelectDataDump { get; private set; }
    [Reactive]
    public UserDataDump SelectDataDump { get; private set; }
    [Reactive]
    public UserDataDump LastDataDump { get; private set; }

    public int SelectedIndex { get; private set; }

    [Reactive]
    public int TimeResult { get; set; }


    public ICollection<ResourceStackRecord> WalletForSelectedItem { get; } = new ObservableCollection<ResourceStackRecord>();

    static CalculateBalanceViewModel()
    {
        var yetiObjects = RustyEntityService.Instance;
        var toolGroups = yetiObjects.EntitesWhereType(RustyEntityType.UniqueTool);

        foreach (var toolGroup in toolGroups)
        {
            var type = ToolsHelper.GetType(toolGroup);
            if (type is ToolsEnum.Unknow)
                continue;

            var tool = yetiObjects.GetItemsFor(toolGroup.Index).MinBy(data => data.GetUnsafe<IHasOwner>().Tear);
            if (tool == null)
                continue;

            ToolCache[type] = new ToolProgressInfo(tool.Index, default);
        }

        _emptyDump = new UserDataDump(Wallet.ToDictionary(static pair => pair.Key, static pair => pair.Value),
            ToolCache.ToDictionary(static pair => pair.Key, static pair => pair.Value),
            UserBag.ToImmutableHashSet(),
            PlantsService.Instance.DefaultCellsCount, CalculateMinigamesService.Instance.DefaultMineCells,
            BeginTime, BeginTime);
    }

    public CalculateBalanceViewModel()
    {
        Wallet.Clear();
        ToolCache.Clear();
        UserBag.Clear();

        foreach (var toolProgressInfo in _emptyDump.Tools)
            ToolCache.Add(toolProgressInfo.Key, toolProgressInfo.Value);

        LastDataDump = _emptyDump;
    }

    public void Initialize(CompositeDisposable disposables, ListView listView)
    {
        var sort = ComparerBuilder.For<ProgressTask>()
            .OrderBy(static task => task.Order);

        var updateSignel = new Subject<Unit>()
            .DisposeWith(disposables);
        var onOrderUpdate = updateSignel
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(RxApp.MainThreadScheduler);

        CalculateBalanceService.Instance.Progress
            .Connect()
            .AutoRefresh(static x => x.Order)
            .Subscribe(_ => updateSignel.OnNext(Unit.Default))
            .DisposeWith(disposables);

        CalculateBalanceService.Instance.Progress
            .Connect()
            .Sort(sort, resort: onOrderUpdate)
            .Bind(out _progress)
            .AutoRefresh(propertyChangeThrottle: TimeSpan.FromSeconds(1))
            .Subscribe(OnProgressUpdate)
            .DisposeWith(disposables);

        listView.ItemsSource = _progress;
        listView.WhenAnyValue(static view => view.SelectedIndex)
            .Subscribe(CallculateDebugWallet)
            .DisposeWith(disposables);
    }

    private void UpdateChunks()
    {
        lock (_progress)
        {
            var chunkCount = (int)Math.Ceiling((double)_progress.Count / ChunkSize);
            var oldCount = _chanks.Length;
            if (oldCount < chunkCount)
            {
                Array.Resize(ref _chanks, chunkCount);
                for (int i = oldCount; i < chunkCount; i++)
                    _chanks[i] = new ChankData(Wallet, ToolCache, UserBag, _emptyDump.StartTime, 0, _emptyDump.FarmCells, _emptyDump.MineSize);
            }
        }
    }

    private void OnProgressUpdate(IChangeSet<ProgressTask> changes)
    {
        UpdateChunks();

        int minIndex = int.MaxValue;
        foreach (var diff in changes)
        {
            switch (diff.Reason)
            {
                case ListChangeReason.AddRange:
                case ListChangeReason.RemoveRange:
                    minIndex = Math.Min(minIndex, diff.Range.Index);
                    break;
                case ListChangeReason.Refresh:
                case ListChangeReason.Remove:
                case ListChangeReason.Add:
                    minIndex = Math.Min(minIndex, diff.Item.CurrentIndex);
                    break;
                case ListChangeReason.Clear:
                    minIndex = 0;
                    break;
                case ListChangeReason.Moved:
                    minIndex = Math.Min(minIndex, Math.Min(diff.Item.PreviousIndex, diff.Item.CurrentIndex));
                    break;
            }
        }


        RxApp.MainThreadScheduler.Schedule(() =>
        {
            if (minIndex != int.MaxValue)
            {
                RecalculateItems(minIndex);
                if(SelectedIndex != -1)
                    CallculateDebugWallet(SelectedIndex);
            }

            if (Progress.Count == 0)
            {
                PreSelectDataDump = _emptyDump;
                SelectDataDump = _emptyDump;
                LastDataDump = _emptyDump;
            }
        });
    }

    private void RecalculateItems(int index)
    {
        if (_isRecalculate)
            return;
        _isRecalculate = true;

        index = Math.Max(0, index);

        try
        {
            var chunkIndex = index / ChunkSize;

            UserData user;

            var from = chunkIndex * ChunkSize;
            if (from != index)
                from++;
            else if (chunkIndex > 0)
            {
                user = _chanks[chunkIndex - 1].ToUserData();
                RecalculateItems_ByRange(ref user, (chunkIndex - 1) * ChunkSize + 1, chunkIndex * ChunkSize + 1);
                from++;
            }
            else
                Wallet.Clear();

            user = _chanks[chunkIndex].ToUserData();
            RecalculateItems_ByRange(ref user, from, _progress.Count);
            LastDataDump = user.GetDump();
        }
        finally
        {
            _isRecalculate = false;
        }
    }

    private void RecalculateItems_ByRange(ref UserData user, int from, int to)
    {
        //Собираем рессурсы если первая сессия
        CollectBaseResourcesTask.TrySimpleEvalute(ref user);

        for (int i = from, iMax = Math.Min(_progress.Count, to); i < iMax; i++)
        {
            _progress[i].Evalute(ref user, true);

            if (i % ChunkSize == 0)
                _chanks[i / ChunkSize] = new ChankData(user);
        }
    }

    internal static Dictionary<IRustyEntity, double> TakeWallet(IList<ResourceStackRecord> resources)
    {
        Wallet.Clear();
        if (resources != null)
            foreach (var exchange in resources)
                Wallet[exchange.Resource] = exchange.Value;
        return Wallet;
    }

    public bool IsCanMoveFromTo(int from, int to)
    {
        var up = from < to ? from : to;
        var down = from < to ? to : from;

        if (down == 0 || down == Progress.Count)
            return false;

        return IsCanMove(Progress[down], Progress[up]);
    }

    private bool IsCanMove(ProgressTask down, ProgressTask up)
    {
        if (up.Type is ProgressType.YetiObject)
        {
            var build = ((CreateYetiObjectTask)up).Target;

            if (down.Type is ProgressType.Craft)
            {
                var entity = ((CraftTask)down).CraftEntity;
                return IsRequestEntity(entity, build);
            }
            else if (down.Type is ProgressType.YetiObject)
            {
                var entity = ((CreateYetiObjectTask)up).Target;
                return IsRequestEntity(entity, build);
            }
            else if (down.Type is ProgressType.FarmPlant)
            {
                foreach (var resourceStack in ((FarmPlantTask)down).Targets)
                {
                    if (PlantsService.Instance.TryGetPlant(resourceStack.Resource, out var plant) && plant.TryGetProperty(out IHasDependents dependents))
                    {
                        if (build == dependents.Required || build == dependents.VisibleAfter)
                            return false;
                    }
                }
                return true;
            }
        }
        return true;
    }

    private static bool IsRequestEntity(IRustyEntity source, IRustyEntity target)
    {
        if (source.TryGetProperty(out IInBuildProcess buildProcess) && buildProcess.Build is not null &&
            buildProcess.Build.Index == target.Index)
            return false;

        if (source.TryGetProperty(out IHasDependents dependents))
        {
            if (dependents.Required is not null && dependents.Required.Index == target.Index)
                return false;
            if (dependents.VisibleAfter is not null && dependents.VisibleAfter.Index == target.Index)
                return false;
        }

        return true;
    }

    internal void MoveFromTo(int from, int to)
    {
        CalculateBalanceService.Instance.Progress.Move(from, to);
    }

    internal void CallculateDebugWallet(int index)
    {
        SelectedIndex = index;
        WalletForSelectedItem.Clear();
        if (index == -1)
        {
            PreSelectDataDump = _emptyDump;
            SelectDataDump = _emptyDump;
            return;
        }

        bool preSelectResolve = false;
        var preSelect = index - 1;
        if (preSelect == -1)
        {
            preSelectResolve = true;
            PreSelectDataDump = _emptyDump;
        }

        var preSelectChunkIndex = preSelect / ChunkSize;
        var chunkIndex = index / ChunkSize;

        static void WriteResource(ICollection<ResourceStackRecord> list, IRustyEntity resource, double count)
        {
            if (Math.Abs(count) < 0.001)
                return;

            list.Add(new ResourceStackRecord(resource, count));
        }

        if (preSelect % ChunkSize == 0)
        {
            preSelectResolve = true;
            PreSelectDataDump = _chanks[preSelectChunkIndex].ToUserData().GetDump();
        }

        if (index % ChunkSize == 0)
        {
            foreach (var exhcange in _chanks[chunkIndex].Wallet)
                WriteResource(WalletForSelectedItem, exhcange.Resource, exhcange.Value);

            SelectDataDump = _chanks[chunkIndex].ToUserData().GetDump();
            TimeResult = (int)(_chanks[chunkIndex].Time - BeginTime).TotalSeconds;
        }
        else
        {
            var user = _chanks[chunkIndex].ToUserData();

            if (preSelectResolve is false)
            {
                var target = Math.Min(index, _progress.Count);
                for (int i = chunkIndex * ChunkSize + 1; i < target; i++)
                    _progress[i].Evalute(ref user);
                PreSelectDataDump = user.GetDump();
                for (int i = target, iMax = Math.Min(target + 1, _progress.Count); i < iMax; i++)
                    _progress[i].Evalute(ref user);
                SelectDataDump = user.GetDump();
            }
            else
            {
                for (int i = chunkIndex * ChunkSize + 1, iMax = Math.Min(index + 1, _progress.Count); i < iMax; i++)
                    _progress[i].Evalute(ref user);
                SelectDataDump = user.GetDump();
            }


            foreach (var pair in Wallet)
                WriteResource(WalletForSelectedItem, pair.Key, pair.Value);
            TimeResult = (int)(user.Time - BeginTime).TotalSeconds;
        }
    }
}
