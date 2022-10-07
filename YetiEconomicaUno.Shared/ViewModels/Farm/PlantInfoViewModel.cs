using ReactiveUI;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;

namespace YetiEconomicaUno.ViewModels.Farm;

public class PlantInfoViewModel : ReactiveObject
{
    private readonly IHasSingleReward _singleReward;
    private readonly ILongExecution _longExecution;

    public IReactiveRustyEntity Entity { get; }
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

    public PlantInfoViewModel(IReactiveRustyEntity entity)
    {
        Entity = entity;
        _singleReward = entity.GetDescUnsafe<IHasSingleReward>();
        _longExecution = entity.GetDescUnsafe<ILongExecution>();
    }

}
