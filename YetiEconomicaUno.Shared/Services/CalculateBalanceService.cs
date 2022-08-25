using DynamicData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using YetiEconomicaCore.Services;
using YetiEconomicaUno.Helpers;
using YetiEconomicaUno.ViewModels.CalculateBalance;
using Nito.Comparers;
using YetiEconomicaUno.ViewModels.CalculateBalance.Progress;
using YetiEconomicaCore.Experemental;

namespace YetiEconomicaUno.Services;

public class CalculateBalanceService : IDisposable
{
    private static readonly Lazy<CalculateBalanceService> _instance = new(() => new CalculateBalanceService());
    public static CalculateBalanceService Instance => _instance.Value;

    private readonly CompositeDisposable _disposables = new();

    public TimeTarget TimeTarget { get; }

    private SourceCache<SessionTimeRecord, int> SessionsData { get; } = new(static value => value.ID);
    public ReadOnlyObservableCollection<SessionTimeRecord> Sessions { get; }

    public DynamicLiteOrderCollection<ProgressTask> Progress { get; }
    //public SourceList<ProgressTask> Progress { get; } = new();

    private CalculateBalanceService()
    {
        TimeTarget = BDHelper.GetProperty<TimeTarget>(TimeTarget.PROP_ID, _disposables);

        var database = DatabaseRepository.Instance.Database;
        var sessionsDB = database.GetCollection<SessionTimeRecord>("sessions");

        SessionsData.AddOrUpdate(sessionsDB.FindAll());
        SessionsData.Preview()
            .ForEachChange(change =>
            {
                switch (change.Reason)
                {
                    case ChangeReason.Add:
                        sessionsDB.Upsert(change.Current);
                        break;
                    case ChangeReason.Remove:
                        sessionsDB.Delete(change.Key);
                        break;
                }
            })
            .Subscribe()
            .DisposeWith(_disposables);

        if(SessionsData.Count == 0)
        {
            SessionsData.AddOrUpdate(SessionTimeRecord.Create(9));
            SessionsData.AddOrUpdate(SessionTimeRecord.Create(13));
            SessionsData.AddOrUpdate(SessionTimeRecord.Create(18));
            SessionsData.AddOrUpdate(SessionTimeRecord.Create(20));
        }

        var sort = ComparerBuilder.For<SessionTimeRecord>()
            .OrderBy(static x => x.Hour);

        SessionsData.Connect()
            .Sort(sort)
            .Bind(out var list)
            .Subscribe()
            .DisposeWith(_disposables);

        Sessions = list;

        Progress = new DynamicLiteOrderCollection<ProgressTask>(database, "progress");

        RustyEntityService.Instance.PreviewToEntity()
            .OnItemRemoved(yetiObject =>
            {
                var items = (IList<ProgressTask>)Progress.Items;
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    var item = items[i];
                    if (item.OnYetiObjectRemove(yetiObject))
                        Progress.RemoveAt(i);
                }
            })
            .Subscribe()
            .DisposeWith(_disposables);
    }

    internal int SessionUseInDay(int hour)
    {
        if (Sessions.Count == 0)
            return 0;
        int result = 1;
        int next = Sessions[0].Hour + hour;

        foreach (var session in Sessions)
        {
            if(session.Hour >= next)
            {
                next = session.Hour + hour;
                result++;
            }
        }

        return result;
    }

    public void AddSessionTime(SessionTimeRecord time)
    {
        SessionsData.AddOrUpdate(time);
    }

    public void RemoveSessionTime(SessionTimeRecord time)
    {
        SessionsData.Remove(time);
    }



    public void Dispose()
    {
        _disposables.Dispose();
    }
}
