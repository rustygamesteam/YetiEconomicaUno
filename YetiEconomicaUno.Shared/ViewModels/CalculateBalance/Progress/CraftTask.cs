using LiteDB;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData.Binding;
using ReactiveUI;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using YetiEconomicaCore.Database;
using YetiEconomicaCore.Services;
using RustyDTO.Interfaces;
using YetiEconomicaCore;

namespace YetiEconomicaUno.ViewModels.CalculateBalance.Progress;

public class CraftTask : ProgressTask
{
    [BsonCtor]
    public CraftTask(IRustyEntity entity) : base(ProgressType.Craft)
    {
        Index = entity.GetIndex();
        CraftEntity = entity;
        SingleReward = entity.GetDescUnsafe<IHasSingleReward>();

        this.WhenPropertyChanged(static x => x.Count)
            .Subscribe(static x => x.Sender.RaisePropertyChanged(nameof(Price)));
    }

    public CraftTask(int index, int count) : this(RustyEntityService.Instance.GetEntity(index))
    {
        Count = count;
    }

    public CraftTask(IRustyEntity entity, int count) : this(entity)
    {
        Count = count;
    }

    public int Index { get; }

    [BsonIgnore]
    public IRustyEntity CraftEntity { get; }

    [BsonIgnore]
    public IHasSingleReward SingleReward { get; }

    [Reactive]
    public int Count { get; set; }

    public override void Evalute(ref UserData userData, bool updateStatistics = false)
    {
        IDisposable? updateStatisticsComplete = null;
        if (updateStatistics)
        {
            updateStatisticsComplete = Statistics.SuspendNotifications();
            Statistics.Clear();
        }

        var reward = CraftEntity.GetDescUnsafe<IHasSingleReward>();
        userData.Wallet.IncrimentWallet(reward.Entity, reward.Count * Count);
        foreach (var exchange in Price)
            userData.Wallet.PayWallet(new ResourceStack(exchange.Resource, exchange.Value * Count));

        var buildInfo = CraftEntity.GetDescUnsafe<IInBuildProcess>();
        double craftTime = CraftEntity.GetDescUnsafe<ILongExecution>().Duration;
        if (buildInfo.Build is not null)
        {
            craftTime /= buildInfo.Build.GetDescUnsafe<IBoostSpeed>().CraftSpeed;
            craftTime = Math.Ceiling(craftTime);
        }

        try
        {
            FarmResources(ref userData, updateStatistics ? Statistics : null);

            var next = userData.Time + TimeSpan.FromSeconds(craftTime);
            while (next > userData.Time)
                userData.Next(next);
        }
        catch (Exception ex)
        {
            throw ex;
        }

        if (updateStatistics)
            Statistics.Add(new StatisticLine(StatisticInfo.Process, (int)craftTime));

        updateStatisticsComplete?.Dispose();
    }

    internal override bool OnYetiObjectRemove(IRustyEntity rustyEntity)
    {
        return rustyEntity == CraftEntity;
    }

    [BsonIgnore]
    internal override IEnumerable<ResourceStackRecord> Price => CraftEntity.GetDescUnsafe<IPayable>().Price.Select(price => new ResourceStackRecord(price.Resource, price.Value * Count));
}
