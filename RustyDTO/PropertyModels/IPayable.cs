using RustyDTO.Interfaces;

namespace RustyDTO.PropertyModels;

public interface IPayable : IRustyEntityProperty
{
    ICollection<ResourceStack> Price { get; }
}