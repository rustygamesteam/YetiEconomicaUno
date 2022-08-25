using RustyDTO.Interfaces;

namespace RustyDTO.PropertyModels;

public interface IHasSingleReward : IRustyEntityProperty
{
    IRustyEntity Entity { get; }
    int Count { get; set; }
}