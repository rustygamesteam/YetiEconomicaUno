using DynamicData.Binding;
using LiteDB;
using Nito.Comparers.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RustyDTO.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using RustyDTO.DescPropertyModels;
using YetiEconomicaCore;
using YetiEconomicaCore.Database;
using YetiEconomicaCore.Experemental;
using YetiEconomicaCore.Services;
using YetiEconomicaUno.ViewModels.CalculateBalance.Internal;

namespace YetiEconomicaUno.ViewModels.CalculateBalance.Progress;

public abstract class ProgressTask : ReactiveObject, IDatabaseOrderEntity
{
    protected static readonly Dictionary<IRustyEntity, double> PriceHelper = new();

    [BsonIgnore]
    public Subject<Unit> EvaluteObservable { get; } = new Subject<Unit>();
 
    #region IDatabaseOrderEntity
    private BsonValue _id;

    [BsonIgnore]
    BsonValue IDatabaseEntity.ID => _id;
    void IDatabaseEntity.InjectID(BsonValue id)
    {
        _id = id;
    }
    #endregion

    private HashSet<int> _bag;

    public record struct StatisticLine(StatisticInfo Type, int Value)
    {
        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }
    }

    public ProgressType Type { get; }

    protected ProgressTask(ProgressType type)
    {
        Type = type;
    }

    internal abstract bool OnYetiObjectRemove(IRustyEntity rustyEntity);

    [Reactive]
    public int Order { get; set; }

    [BsonIgnore]
    public ObservableCollectionExtended<StatisticLine> Statistics { get; } = new();

    [BsonIgnore]
    internal abstract IEnumerable<ResourceStackRecord> Price { get; }


    public bool HasInBag(IRustyEntity entity)
    {
        return _bag?.Contains(entity.GetIndex()) ?? false;
    }

    protected void CopyBag(ref UserData userData)
    {
        _bag ??= new HashSet<int>(32);

        _bag.Clear();
        foreach (var index in userData.UserBag)
            _bag.Add(index);
    }

    public abstract void Evalute(ref UserData userData, bool updateStatistics = false);

    protected bool FarmResources(ref UserData userData, ObservableCollection<StatisticLine> statistics)
    {
        PriceHelper.Clear();
        var request = userData.Wallet.Where(pair => pair.Value < 0).Select(pair => new ResourceStackRecord
        {
            Resource = pair.Key,
            Value = -pair.Value
        });

        return FarmResources(ref userData, request, statistics);
    }
    private ref struct StatistcsInfo
    {
        public TimeSpan Craft;
        public TimeSpan Farm;
        public TimeSpan CollectSimpleResourcesTime;
        public int FarmCicles;
    }

    protected bool FarmResources(ref UserData userData, IEnumerable<ResourceStackRecord> resources, ObservableCollection<StatisticLine> statistics)
    {
        if (resources.Count() == 0)
            return true;

        StatistcsInfo statisticsInfo = default;

        var tree = ResourceDependenciesTree.Build(resources);
        var treeEnumerable = tree.GetEnumerable();
        if (treeEnumerable.Any(node => node.Type == ResourceNodeType.Invalid))
            return false;

        var treeByDepth = treeEnumerable.GroupBy(static node => node.Depth).ToList();

        var startTime = userData.Time;

        for (int i = treeByDepth.Count - 1; i >= 0; i--)
        {
            var lastNodes = ResourceDependenciesTree.Merge(treeByDepth[i]);

            CalculateStepResult result;
            DateTime endTime;

            var beginTime = userData.Time;
            CalculateStepHelper.SimpleResources.Clear();
        TRY_AGAIN:
            result = CalculateStep(userData, lastNodes, out var offset, ref statisticsInfo);
            endTime = userData.Time + offset;

            if (offset.TotalSeconds > 0.1 && userData.Time < endTime)
            {
                while (offset.TotalSeconds > 0.1 && userData.Time < endTime)
                {
                    userData.Next(endTime);

                    if (CalculateStepHelper.HasResources(ref userData))
                    {
                        CalculateStepHelper.SimpleResources.Clear();
                        statisticsInfo.CollectSimpleResourcesTime += userData.Time - beginTime;
                    }
                }
            }
            else if (CalculateStepHelper.SimpleResources.Count > 0)
            {
                while (!CalculateStepHelper.HasResources(ref userData))
                    userData.Next();

                statisticsInfo.CollectSimpleResourcesTime += userData.Time - beginTime;
            }

            while (result != CalculateStepResult.Complete)
                goto TRY_AGAIN;
        }


        if (statistics != null)
            WriteStatistic(ref userData, startTime, statistics, statisticsInfo);

        return true;
    }

    private static CalculateStepResult CalculateStep(UserData userData, IEnumerable<ResourceDependenciesTree.WorkerNode> lastNodes, out TimeSpan offset, ref StatistcsInfo statistcsInfo)
    {
        CalculateStepHelper.CraftsQueue.Clear();
        CalculateStepResult result = CalculateStepResult.Complete;

        int craftTimeMax = 0;
        int farmTimeMax = 0;

        int farmCellUse = 0;

        foreach (var node in lastNodes)
        {
            if (node.IsComplete)
                continue;

            double count;
            switch (node.Type)
            {
                case ResourceNodeType.Simple:
                    node.Complete();
                    CalculateStepHelper.SimpleResources.IncrimentWallet(node.Resource, node.Count);
                    break;
                case ResourceNodeType.Farm:
                    var targetCount = node.Count;
                    if (targetCount <= 0)
                    {
                        node.Complete();
                        continue;
                    }
                    var farmData = PlantsService.Instance.GetPlantEntity(node.Resource);
                    var reward = farmData.GetDescUnsafe<IHasSingleReward>();
                    var duration = farmData.GetDescUnsafe<ILongExecution>().Duration;

                    var ciclesTotlaCount = (int)Math.Ceiling(targetCount / reward.Count);
                    var cicles = Math.Min(ciclesTotlaCount, userData.FarmCells - farmCellUse);
                    if (cicles <= 0)
                    {
                        result |= CalculateStepResult.WaitFram;
                        continue;
                    }

                    farmCellUse += cicles;
                    farmTimeMax = Math.Max(farmTimeMax, duration);
                    userData.Wallet.IncrimentWallet(node.Resource, reward.Count * cicles);
                    node.Count -= reward.Count * cicles;

                    if (cicles >= ciclesTotlaCount)
                        node.Complete();
                    else
                        result |= CalculateStepResult.WaitFram;

                    break;
                case ResourceNodeType.Convert:
                    foreach (var child in node.Childs)
                        userData.Wallet.PayWallet(child.Current);
                    userData.Wallet.IncrimentWallet(node.Resource, node.Count);

                    node.Complete();
                    break;
                case ResourceNodeType.Craftable:
                    if (node.Count <= 0 || userData.Wallet.TryGetValue(node.Resource, out count) && count >= node.Count)
                    {
                        node.Complete();
                        continue;
                    }

                    CraftService.Instance.TryGetCraft(node.Resource, out var craft);
                    var buildInfo = craft.GetDescUnsafe<IInBuildProcess>();
                    if (buildInfo.Build is not null && CalculateStepHelper.CraftsQueue.Contains(buildInfo.Build.GetIndex()))
                    {
                        result |= CalculateStepResult.WaitCraft;
                        continue;
                    }

                    var rewardInfo = craft.GetDescUnsafe<IHasSingleReward>();

                    double craftTime = craft.GetDescUnsafe<ILongExecution>().Duration;
                    if (buildInfo.Build is not null && buildInfo.Build.TryGetProperty(out ICraftSpeed craftSpeed))
                        craftTime /= craftSpeed.Factor;

                    craftTimeMax = Math.Max(craftTimeMax, Math.Max((int)craftTime, 1));
                    userData.Wallet.IncrimentWallet(node.Resource, rewardInfo.Count);
                    node.Count -= rewardInfo.Count;

                    foreach (var craftItem in node.Childs)
                        userData.Wallet.PayWallet(craftItem.Current);

                    if (userData.Wallet[node.Resource] > rewardInfo.Count)
                        node.Complete();
                    else
                        result |= CalculateStepResult.WaitCraft;
                    break;
                case ResourceNodeType.Invalid:
                    node.Complete();
                    break;

            }
        }

        if (CalculateStepHelper.SimpleResources.Count > 0 && !CalculateStepHelper.HasResources(ref userData))
            result |= CalculateStepResult.WaitSimpleResources;

        statistcsInfo.Farm += TimeSpan.FromSeconds(farmTimeMax);
        statistcsInfo.Craft += TimeSpan.FromSeconds(craftTimeMax);
        if (farmCellUse > 0)
            statistcsInfo.FarmCicles++;

        offset = TimeSpan.FromSeconds(Math.Max(farmTimeMax, craftTimeMax));

        return result;
    }

    private void WriteStatistic(ref UserData userData, DateTime startTime, ObservableCollection<StatisticLine> statistics, StatistcsInfo statisticsInfo)
    {
        var idleTime = userData.Time - startTime;

        if (idleTime.TotalSeconds > 0)
            Statistics.Add(new StatisticLine(StatisticInfo.Idle, (int)idleTime.TotalSeconds));

        if (statisticsInfo.CollectSimpleResourcesTime.TotalSeconds > 0)
            statistics.Add(new StatisticLine(StatisticInfo.CollectResources, (int)statisticsInfo.CollectSimpleResourcesTime.TotalSeconds));

        if (statisticsInfo.Farm.TotalSeconds > 0)
            statistics.Add(new StatisticLine(StatisticInfo.FarmPlants, (int)statisticsInfo.Farm.TotalSeconds));

        if (statisticsInfo.FarmCicles > 0)
            statistics.Add(new StatisticLine(StatisticInfo.FarmCycles, (int)statisticsInfo.FarmCicles));

        if (statisticsInfo.Craft.TotalSeconds > 0)
            statistics.Add(new StatisticLine(StatisticInfo.PreCraft, (int)statisticsInfo.Craft.TotalSeconds));
    }

    [Flags]
    private enum CalculateStepResult
    {
        Complete = 0,
        WaitCraft = 1 << 0,
        WaitFram = 1 << 1,
        WaitSimpleResources = 1 << 2
    }

    private static class CalculateStepHelper
    {
        public static Dictionary<IRustyEntity, double> SimpleResources = new();
        public static HashSet<int> CraftsQueue = new();

        internal static bool HasResources(ref UserData userData)
        {
            return SimpleResources.Count > 0 && HasResources(ref userData, SimpleResources);
        }

        private static bool HasResources(ref UserData userData, Dictionary<IRustyEntity, double> resources)
        {
            if (resources == null || resources.Count == 0)
                return true;
            foreach (var pair in resources)
            {
                if (!userData.Wallet.TryGetValue(pair.Key, out var count) || count < pair.Value)
                    return false;
            }
            return true;
        }
    }
}
