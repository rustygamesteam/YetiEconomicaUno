using RustyDTO.Interfaces;

namespace RustyDTO.PropertyModels;

public interface IHasOwner : IRustyEntityProperty
{
    public int Tear { get; set; }
    public IRustyEntity Owner { get; }
}
