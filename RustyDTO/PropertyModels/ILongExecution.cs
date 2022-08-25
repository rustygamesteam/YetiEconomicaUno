using RustyDTO.Interfaces;

namespace RustyDTO.PropertyModels;

public interface ILongExecution : IRustyEntityProperty
{
    public int Duration { get; set; }
}