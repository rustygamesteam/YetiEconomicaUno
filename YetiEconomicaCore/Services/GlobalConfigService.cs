using LiteDB;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RustyDTO.Interfaces;
using RustyDTO.Supports;

namespace YetiEconomicaCore.Services;

public class GlobalConfigService
{
    private GlobalConfigService()
    {
        SimpleResources = DatabaseRepository.Instance.GetConfig(nameof(SimpleResourcesConfig), key => new SimpleResourcesConfig(), null);
        ArmyInfluence = DatabaseRepository.Instance.GetConfig(nameof(ArmyPropertyInfluence), key => new ArmyPropertyInfluence(), null);
    }

    private static readonly Lazy<GlobalConfigService> _instance = new(static () => new());
    public static GlobalConfigService Instance => _instance.Value;

    public SimpleResourcesConfig SimpleResources { get; }
    public ArmyPropertyInfluence ArmyInfluence { get; }
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

public class ArmyPropertyInfluence : ReactiveObject
{
    [BsonId]
    public string ID { get; } = nameof(ArmyPropertyInfluence);

    [Reactive]
    public ArmyPowerConfig Config { get; set; }
}