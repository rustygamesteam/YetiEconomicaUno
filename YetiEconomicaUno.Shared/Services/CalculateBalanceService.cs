using DynamicData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using YetiEconomicaCore.Services;
using YetiEconomicaUno.Helpers;
using YetiEconomicaUno.ViewModels.CalculateBalance;
using Nito.Comparers;
using YetiEconomicaUno.ViewModels.CalculateBalance.Progress;
using YetiEconomicaCore.Experemental;
using System.Reactive.Subjects;
using DynamicData.Binding;
using LiteDB;
using ReactiveUI;

namespace YetiEconomicaUno.Services;

public class CalculateBalanceService : IDisposable
{

    private static readonly Lazy<CalculateBalanceService> _instance = new(() => new CalculateBalanceService());
    public static CalculateBalanceService Instance => _instance.Value;

    public static BehaviorSubject<string> CurrentModel { get; private set; }

    private CompositeDisposable _disposables;

    public BalanceConfig Config { get; }

    private SourceCache<SessionTimeRecord, int> SessionsData { get; } = new(static value => value.ID);
    public ReadOnlyObservableCollection<SessionTimeRecord> Sessions { get; private set; }

    public SourceList<ProgressTask> Progress { get; }

    private ILiteCollection<BsonDocument> _progress;
    private readonly ILiteCollection<ModelItem> _balanceModelsDatabase;
    //public SourceList<ProgressTask> Progress { get; } = new();

    private ObservableCollection<string> _balanceModels = new ObservableCollectionExtended<string>();
    public ReadOnlyObservableCollection<string> BalanceModels { get; }

    public record ModelItem(string Name);

    private CalculateBalanceService()
    {
        Config = BDHelper.GetProperty<BalanceConfig>(BalanceConfig.PROP_ID, _disposables);
        var database = DatabaseRepository.Instance.Database;
        _balanceModelsDatabase = database.GetCollection<ModelItem>("balance_models");

        _progress = database.GetCollection("progress", BsonAutoId.Int32);
        _progress.EnsureIndex(document => document["Model"], false);

        Progress = new SourceList<ProgressTask>();
        BalanceModels = new ReadOnlyObservableCollection<string>(_balanceModels);

        if (_balanceModelsDatabase.Count() == 0)
            _balanceModelsDatabase.Insert(new ModelItem("Main"));

        _balanceModels.AddRange(_balanceModelsDatabase.FindAll().Select(static item => item.Name));

        if (string.IsNullOrEmpty(Config.CurrentModel))
            Config.CurrentModel = _balanceModelsDatabase.Query().First().Name;

        CurrentModel = new BehaviorSubject<string>(Config.CurrentModel);

        CurrentModel.Subscribe(OnModelChange);
    }

    public void CreateModel(string name)
    {
        if(_balanceModels.Contains(name))
            return;
        _balanceModels.Add(name);
        _balanceModelsDatabase.Insert(new ModelItem(name));

        var mapper = BsonMapper.Global;

        foreach (var progressItem in Progress.Items)
        {
            var doc = mapper.ToDocument(progressItem);
            doc["Model"] = name;
            doc.Remove("_id");

            _progress.Upsert(doc);
        }

        var sessionsDB = DatabaseRepository.Instance.Database.GetCollection("sessions");
        foreach (var record in SessionsData.Items)
        {
            var doc = mapper.ToDocument(record);
            doc["Model"] = name;
            doc.Remove("_id");

            sessionsDB.Upsert(doc);
        }

        CurrentModel.OnNext(name);
    }
    
    public void RemoveModel()
    {
        var model = CurrentModel.Value;
        var next = _balanceModels.Where(s => s != model).First();
        CurrentModel.OnNext(next);

        _balanceModels.Remove(model);
        _balanceModelsDatabase.Delete(model);

        var sessionsDB = DatabaseRepository.Instance.Database.GetCollection("sessions");
        sessionsDB.DeleteMany(document => document["Model"].AsString == model);
        _progress.DeleteMany(document => document["Model"].AsString == model);
    }

    private void OnModelChange(string modelName)
    {
        Config.CurrentModel = modelName;

        _disposables?.Dispose();
        _disposables = new CompositeDisposable();

        var database = DatabaseRepository.Instance.Database;
        var sessionsDB = database.GetCollection<BsonDocument>("sessions", BsonAutoId.Int32);

        sessionsDB.EnsureIndex(record => record["Model"], false);

        SessionsData.Edit(updater =>
        {
            var mapper = BsonMapper.Global;

            updater.Clear();
            updater.AddOrUpdate(sessionsDB.Query()
                .Where(record => record["Model"].AsString == modelName).ToEnumerable()
                .Select(document => mapper.ToObject<SessionTimeRecord>(document)));
        });

        SessionsData.Preview()
            .ForEachChange(change =>
            {

                switch (change.Reason)
                {
                    case ChangeReason.Add:
                        var document = BsonMapper.Global.ToDocument(change.Current);
                        document.Remove("_id");
                        var id = sessionsDB.Insert(document);
                        change.Current.Index = id.AsInt32;
                        break;
                    case ChangeReason.Remove:
                        sessionsDB.Delete(change.Current.Index);
                        break;
                }
            })
            .Subscribe()
            .DisposeWith(_disposables);

        if (SessionsData.Count == 0)
        {
            SessionsData.AddOrUpdate(SessionTimeRecord.Create(9, modelName));
            SessionsData.AddOrUpdate(SessionTimeRecord.Create(13, modelName));
            SessionsData.AddOrUpdate(SessionTimeRecord.Create(18, modelName));
            SessionsData.AddOrUpdate(SessionTimeRecord.Create(20, modelName));
        }

        var sort = ComparerBuilder.For<SessionTimeRecord>()
            .OrderBy(static x => x.Hour);

        SessionsData.Connect()
            .Sort(sort)
            .Bind(out var list)
            .Subscribe()
            .DisposeWith(_disposables);

        Sessions = list;

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

        Progress.Edit(tasks =>
        {
            var mapper = BsonMapper.Global;
            var items = _progress.Query().Where(document => document["Model"].AsString == CurrentModel.Value)
                .OrderBy(static document => document["Order"].AsInt32).ToEnumerable()
                .Select(document =>
                {
                    var task = AppBootstrapper.FactoryProgressTask(document);
                    ((IDatabaseOrderEntity)task).InjectID(document["_id"]);
                    return task;
                });

            tasks.Clear();
            tasks.AddRange(items);
        });

        Progress.Preview().Subscribe(set =>
        {
            var mapper = BsonMapper.Global;
            foreach (var change in set)
            {
                BsonValue id;
                BsonDocument doc;

                switch (change.Reason)
                {
                    case ListChangeReason.AddRange:
                        foreach (var progressTask in change.Range)
                        {
                            doc = mapper.ToDocument(progressTask);
                            doc.Remove("_type");
                            id = _progress.Insert(doc);
                            ((IDatabaseEntity)progressTask).InjectID(id);
                        }
                        break;
                    case ListChangeReason.RemoveRange:
                        foreach (var progressTask in change.Range)
                        {
                            id = ((IDatabaseEntity)progressTask).ID;
                            _progress.Delete(id);
                        }
                        break;
                    case ListChangeReason.Add:
                        doc = mapper.ToDocument(change.Item.Current);
                        doc.Remove("_type");
                        id = _progress.Insert(doc);
                        ((IDatabaseEntity)change.Item.Current).InjectID(id);
                        break;
                    case ListChangeReason.Remove:
                        id = ((IDatabaseEntity)change.Item.Current).ID;
                        _progress.Delete(id);
                        break;
                    case ListChangeReason.Clear:
                        _progress.DeleteMany(document => document["Model"] == CurrentModel.Value);
                        break;
                }
            }
        }).DisposeWith(_disposables);

        Progress.Connect()
            .AutoRefresh(propertyChangeThrottle: TimeSpan.FromSeconds(1), scheduler: RxApp.MainThreadScheduler)
            .Subscribe(set =>
            {
                var mapper = BsonMapper.Global;

                foreach (var change in set)
                {
                    if (change.Reason is ListChangeReason.Refresh)
                    {
                        if (change.Type is ChangeType.Item)
                        {
                            var id = ((IDatabaseEntity)change.Item.Current).ID;
                            _progress.Update(id, mapper.ToDocument(typeof(ProgressTask), change.Item.Current));
                        }
                        else
                        {
                            foreach (var progressTask in change.Range)
                            {
                                var id = ((IDatabaseEntity)progressTask).ID;
                                _progress.Update(id, mapper.ToDocument(typeof(ProgressTask), progressTask));
                            }
                        }
                    }
                }
            }).DisposeWith(_disposables);
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
