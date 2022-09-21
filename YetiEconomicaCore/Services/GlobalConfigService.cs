using LiteDB;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RustyDTO.Interfaces;

namespace YetiEconomicaCore.Services;

public class GlobalConfigService
{
    private GlobalConfigService()
    {
        _simpleResources = DatabaseRepository.Instance.GetConfig(nameof(SimpleResourcesConfig), key => new SimpleResourcesConfig(), null);
    }

    private static readonly Lazy<GlobalConfigService> _instance = new(static () => new());
    public static GlobalConfigService Instance => _instance.Value;


    private readonly SimpleResourcesConfig _simpleResources;

    public SimpleResourcesConfig SimpleResources => _simpleResources;
}

public class SimpleResourcesConfig : ReactiveObject
{
    [BsonId]
    public string ID { get; } = nameof(SimpleResourcesConfig);

    [Reactive]
    public IRustyEntity? Wood { get; set; }
    [Reactive]
    public IRustyEntity? Stone { get; set; }
    [Reactive]
    public IRustyEntity? Ore { get; set; }
}