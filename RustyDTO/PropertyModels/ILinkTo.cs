using RustyDTO.Interfaces;

namespace RustyDTO.PropertyModels;

public interface ILinkTo : IRustyEntityProperty
{
    IRustyEntity Entity { get; }
}