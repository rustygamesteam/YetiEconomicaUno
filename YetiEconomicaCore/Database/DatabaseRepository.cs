using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData.Binding;
using LiteDB;
using ReactiveUI;

namespace YetiEconomicaCore.Services;

public sealed partial class DatabaseRepository : IDisposable
{
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
            .Subscribe(config =>
            {
                var configBson = BsonMapper.Global.ToDocument(config);
                _configs.Upsert(configBson);
            });

        if (compositeDisposable is not null)
            disposable.DisposeWith(compositeDisposable);

        return config;
    }


    //private ILiteStorage<int> IndexStorage { get; }
    private bool _isDisposable;

    ~DatabaseRepository()
    {
        Dispose();
    }
    
    public DatabaseRepository(LiteDatabase database)
    {
        Instance = this;
        Database = database;

        _configs = database.GetCollection("configs");

        BsonMapper.Global.Entity<BD_version>()
            .Ignore(static version => version.Changed)
            .Ignore(static version => version.Changing)
            .Ignore(static version => version.ThrownExceptions);
        _version = GetConfig("bd_version", key => new BD_version(key), null);

        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;

        Validate(_version);
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