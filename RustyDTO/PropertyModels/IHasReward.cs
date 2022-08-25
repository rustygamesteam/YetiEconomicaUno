using RustyDTO.Interfaces;

namespace RustyDTO.PropertyModels;

public interface IHasRewards : IRustyEntityProperty
{
    ICollection<ResourceStack> Rewards { get; }
}