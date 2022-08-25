using RustyDTO.Interfaces;

namespace RustyDTO.PropertyModels;

public interface IInBuildProcess : IRustyEntityProperty
{
    IRustyEntity? Build { get; set; }
}