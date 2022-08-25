using RustyDTO.Interfaces;

namespace RustyDTO.PropertyModels;

public interface IBoostSpeed : IRustyEntityProperty
{
    double CraftSpeed { get; set;}
    double TechSpeed { get; set; }
}
