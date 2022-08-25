using RustyDTO.Interfaces;

namespace RustyDTO.PropertyModels;

public interface IToolInfo : IRustyEntityProperty
{
    double Efficiency { get; set; }
    int Strength { get; set; }
    int RechargeEvery { get; set; }
}
