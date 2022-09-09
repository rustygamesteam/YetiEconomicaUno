using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData.Binding;
using LiteDB;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RustyDTO;

namespace YetiEconomicaCore.Services;

public class DatabaseRepository : IDisposable
{
    private const int DEFAULT_VERSION = 2;

    public readonly LiteDatabase Database;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static DatabaseRepository Instance { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    private ILiteCollection<BsonDocument> _configs;

    public TConfig GetConfig<TConfig>(string key, Func<string, TConfig> factory, CompositeDisposable? compositeDisposable) where TConfig : IReactiveObject
    {
        var document = _configs.FindById(key);
        TConfig config;
        if (document == null)
            config = factory.Invoke(key);
        else
            config = BsonMapper.Global.ToObject<TConfig>(document);

        var disposable = config.WhenAnyPropertyChanged()
            .Throttle(TimeSpan.FromSeconds(1))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(config => _configs.Upsert(BsonMapper.Global.ToDocument(config)));

        if (compositeDisposable is not null)
            disposable.DisposeWith(compositeDisposable);

        return config;
    }


    //private ILiteStorage<int> IndexStorage { get; }
    private bool _isDisposable;

    private BD_version _version;
    ~DatabaseRepository()
    {
        Dispose();
    }
    
    public DatabaseRepository(LiteDatabase database)
    {
        Instance = this;
        Database = database;

        _configs = database.GetCollection("configs");
        _version = GetConfig("bd_version", key => new BD_version(key), null);

        BsonMapper.Global.Entity<BD_version>()
            .Ignore(static version => version.Changed)
            .Ignore(static version => version.Changing)
            .Ignore(static version => version.ThrownExceptions);

        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;

        Validate(_version);
    }

    private class BD_version : ReactiveObject
    {
        [BsonCtor]
        public BD_version(string key)
        {
            Key = key;
            Version = DEFAULT_VERSION;
        }

        [BsonId]
        public string Key { get; }

        [Reactive]
        public int Version { get; set; }
    }

    private void Validate(BD_version versionConfig)
    {
        var entitise = Database.GetCollection("entities", BsonAutoId.Int32);
        var properties = Database.GetCollection("properties", BsonAutoId.Int32);
        var items_of = Database.GetCollection("items_of", BsonAutoId.Int32);

        if (versionConfig.Version == 0)
        {
            var groups = new List<RustyEntity>();

            const int exchangeIndex = (int)RustyEntityType.ExchageTask;
            const int exchangeGroupIndex = 10; //(int)RustyEntityType.ExchageGroup;
            const int hasExchangeIndex = (int)DescPropertyType.HasExchange;
            string exchangeInfo = ((int)DescPropertyType.HasExchange).ToString();
            string linkInfo = ((int)DescPropertyType.Link).ToString();

            var exchanges = entitise.FindAll().Where(static document => document["Type"].AsInt32 == exchangeIndex).Where(
                static document =>
                {
                    var props = document["Properties"];
                    return props.AsArray.Count > 1 || props[0].AsInt32 != hasExchangeIndex;
                }).GroupBy(document =>
            {
                var id = document["_id"].AsInt32;
                return items_of.FindById(id)["Owner"];
            });

            foreach (var exchangeGroup in exchanges)
            {
                entitise.Delete(exchangeGroup.Key);
                var to = properties.FindById(exchangeGroup.Key)[linkInfo];
                properties.Delete(exchangeGroup.Key);

                foreach (var exchange in exchangeGroup)
                {
                    var exchangeID = new BsonValue(exchange["_id"].AsInt32);
                    items_of.Delete(exchangeID);

                    exchange["Properties"] = new BsonArray(new BsonValue[] { hasExchangeIndex });
                    entitise.Update(exchangeID, exchange);

                    var props = properties.FindById(exchangeID)[exchangeInfo];
                    var info = new BsonDocument
                    {
                        { "From", props["FromEntity"]},
                        { "To", to },
                        { "FromRate", props["Count"]},
                    };
                    properties.Update(exchangeID, new BsonDocument
                    {
                        { exchangeInfo, info }
                    });
                }
            }

            var invalidGroups = entitise.FindAll().Where(static document => document["Type"].AsInt32 == exchangeGroupIndex).Select(static document => document["_id"]);
            foreach (var id in invalidGroups)
            {
                entitise.Delete(id);
                properties.Delete(id);
            }

            versionConfig.Version++;
        }
        if (versionConfig.Version == 1)
        {
            var hasChildIndex = new BsonValue(10);

            foreach (var entity in entitise.FindAll())
            {
                var props = entity["Properties"].AsArray;
                if (props.Contains(hasChildIndex))
                {
                    props.Remove(hasChildIndex);
                    entitise.Update(entity);
                }
            }

            versionConfig.Version++;
        }
        if (versionConfig.Version == 2)
        {
            var expandexIndex = new BsonValue(999);
            foreach (var entity in entitise.FindAll())
            {
                var props = entity["Properties"].AsArray;
                if (props.Contains(expandexIndex))
                {
                    props.Remove(expandexIndex);
                    entitise.Update(entity);
                }
            }

            foreach (var doc in properties.FindAll())
            {
                if (doc.Remove("999"))
                    properties.Update(doc);
            }

            versionConfig.Version++;
        }

        if (versionConfig.Version == 3)
        {
            var boostSpeedIndex = new BsonValue(5);

            var craftSpeedIndex = new BsonValue(31);
            var techSpeedIndex = new BsonValue(32);

            foreach (var entity in entitise.FindAll())
            {
                var props = entity["Properties"].AsArray;
                if (props.Contains(boostSpeedIndex))
                {
                    props.Remove(boostSpeedIndex);
                    props.Add(craftSpeedIndex);
                    props.Add(techSpeedIndex);

                    entitise.Update(entity);
                }
            }
            
            foreach (var doc in properties.FindAll())
            {
                if (doc.TryGetValue("5", out var info))
                {
                    doc.Add("31", info["CraftSpeed"]);
                    doc.Add("32", info["TechSpeed"]);

                    doc.Remove("5");
                }
            }

            versionConfig.Version++;
        }
    }

    private void CurrentDomain_DomainUnload(object? sender, EventArgs e)
    {
        Dispose();
    }

    private void CurrentDomain_ProcessExit(object? sender, EventArgs e)
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_isDisposable)
            return;
        _isDisposable = true;
        Database.Dispose();
    }
}