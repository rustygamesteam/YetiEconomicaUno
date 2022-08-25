using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using RustyDTO;
using RustyDTO.Interfaces;
using RustyDTO.PropertyModels;
using YetiEconomicaCore.Database;
using YetiEconomicaCore.Services;
using YetiEconomicaUno.Helpers;

namespace YetiEconomicaUno.ViewModels.CalculateBalance.Progress;

public class CreateYetiObjectTask : ProgressTask
{
    public CreateYetiObjectTask(IRustyEntity entity) : base(ProgressType.YetiObject)
    {
        Index = entity.Index;
        Target = entity;
    }

    public CreateYetiObjectTask(int index) : this(RustyEntityService.Instance.GetEntity(index))
    {
    }

    public int Index { get; }

    [BsonIgnore]
    public IRustyEntity Target { get; }

    public override void Evalute(ref UserData userData, bool updateStatistics = false)
    {
        if (updateStatistics)
            Statistics.Clear();


        foreach (var exchange in Price)
            userData.Wallet.PayWallet(exchange);

        if (Target.TryGetProperty(out IHasRewards rewardsInfo))
        {
            foreach (var reward in rewardsInfo.Rewards)
                userData.Wallet.IncrimentWallet(reward);
        }

        double duration = -1;
        if (Target.TryGetProperty(out ILongExecution longExecution))
        {
            duration = longExecution.Duration;
            if (Target.Type is RustyEntityType.Tech && Target.TryGetProperty<IInBuildProcess>(out var buildProcess) && buildProcess.Build is not null)
            {
                var boost = buildProcess.Build.GetUnsafe<IBoostSpeed>();
                duration /= boost.TechSpeed;
                duration = Math.Ceiling(duration);
            }
        }

        try
        {
            FarmResources(ref userData, updateStatistics ? Statistics : null);

            if (duration > 0)
            {
                var next = userData.Time + TimeSpan.FromSeconds(duration);
                while (next > userData.Time)
                    userData.Next(next);
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }

        if (Target.TryGetProperty<IMineSizeProperty>(out var mineSize))
            userData.MineSize = (mineSize.X, mineSize.Y);

        if (Target.TryGetProperty<IFarmExpansion>(out var farmExpansion))
            userData.FarmCells = farmExpansion.Count;

        if (updateStatistics)
            Statistics.Add(new StatisticLine(StatisticInfo.Process, (int)duration));

        if (Target.Type is RustyEntityType.Tool)
        {
            var type = ToolsHelper.GetGroupType(Target);
            if (type is not ToolsEnum.Unknow)
                userData.Tools[type] = new ToolProgressInfo(Index, new DateTime());
        }
    }

    internal override bool OnYetiObjectRemove(IRustyEntity rustyEntity)
    {
        return Target.Index == rustyEntity.Index;
    }

    [BsonIgnore]
    internal override IEnumerable<ResourceStackRecord> Price
    {
        get
        {
            if(Target.TryGetProperty(out IPayable payable))
                return payable.Price.Select(static x => (ResourceStackRecord)x);
            return Enumerable.Empty<ResourceStackRecord>();
        }
    }
}
