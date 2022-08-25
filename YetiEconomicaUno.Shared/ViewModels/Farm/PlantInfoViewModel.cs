using ReactiveUI;
using RustyDTO.Interfaces;
using RustyDTO.PropertyModels;
using YetiEconomicaCore;

namespace YetiEconomicaUno.ViewModels.Farm;

public class PlantInfoViewModel : ReactiveObject
{
    private readonly IHasSingleReward _singleReward;
    private readonly ILongExecution _longExecution;

    public IRustyEntity Entity { get; }
    public IRustyEntity Resource => _singleReward.Entity;

    public int Harvest
    {
        get => _singleReward.Count;
        set => _singleReward.Count = value;
    }

    public int Duration
    {
        get => _longExecution.Duration;
        set => _longExecution.Duration = value;
    }

    public PlantInfoViewModel(IRustyEntity entity)
    {
        Entity = entity;
        _singleReward = entity.GetUnsafe<IHasSingleReward>();
        _longExecution = entity.GetUnsafe<ILongExecution>();
    }

}
