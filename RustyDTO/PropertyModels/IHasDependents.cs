using RustyDTO.Interfaces;

namespace RustyDTO.PropertyModels;

public interface IHasDependents : IRustyEntityProperty
{
    public IRustyEntity? Required { get; set; }
    public IRustyEntity? VisibleAfter { get; set; }
}