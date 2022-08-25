using RustyDTO.Interfaces;

namespace RustyDTO.PropertyModels;

public interface IFarmExpansion : IRustyEntityProperty
{
    int Count { get; set; }
}