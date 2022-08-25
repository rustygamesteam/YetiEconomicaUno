using RustyDTO.Interfaces;

namespace RustyDTO.PropertyModels;

public interface IHasExchange : IRustyEntityProperty
{
    IRustyEntity ToEntity { get; }
    IRustyEntity FromEntity { get; }

    double FromRate { get; set; }
}