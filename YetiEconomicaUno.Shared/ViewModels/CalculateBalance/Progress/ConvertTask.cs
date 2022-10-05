using LiteDB;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using DynamicData.Binding;
using ReactiveUI;
using RustyDTO.DescPropertyModels;
using YetiEconomicaCore.Database;
using YetiEconomicaCore.Services;
using RustyDTO.Interfaces;
using YetiEconomicaCore;
using YetiEconomicaUno.Services;

namespace YetiEconomicaUno.ViewModels.CalculateBalance.Progress;

public class ConvertTask : ProgressTask
{
    public int Index { get; }

    [Reactive]
    public int Count { get; set; }

    [BsonIgnore]
    public IRustyEntity ExchagEntity { get; }

    public readonly IHasExchange Exchange;


    public ConvertTask(int exchagEntity, int count) : this(RustyEntityService.Instance.GetEntity(exchagEntity), count)
    {
    }

    public ConvertTask(IRustyEntity exchagEntity, int count) : this(exchagEntity)
    {
        Count = count;
    }

    public ConvertTask(IRustyEntity exchagEntity) : base(ProgressType.Convert)
    {
        Index = exchagEntity.GetIndex();
        ExchagEntity = exchagEntity;
        Exchange = exchagEntity.GetDescUnsafe<IHasExchange>();

        this.WhenPropertyChanged(static x => x.Count)
            .Subscribe(static x => x.Sender.RaisePropertyChanged(nameof(Price)));
    }

    public override void Evalute(ref UserData userData, bool updateStatistics = false)
    {
        if (updateStatistics)
            Statistics.Clear();

        if (Exchange.FromEntity is null || Exchange.ToEntity is null || Count == 0)
            return;

        userData.Wallet.PayWallet(Exchange.FromEntity, Count * Exchange.FromRate);
        userData.Wallet.IncrimentWallet(Exchange.ToEntity, Count);

        try
        {
            FarmResources(ref userData, updateStatistics ? Statistics : null);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    internal override bool OnYetiObjectRemove(IRustyEntity rustyEntity)
    {
        return rustyEntity.GetIndex() == Exchange.ToEntity.GetIndex() || rustyEntity.GetIndex() == Exchange.FromEntity.GetIndex() || rustyEntity == ExchagEntity;
    }

    [BsonIgnore]
    internal override IEnumerable<ResourceStackRecord> Price
    {
        get
        {
            yield return new ResourceStackRecord(Exchange.FromEntity, Count * Exchange.FromRate);
        }
    }
}
