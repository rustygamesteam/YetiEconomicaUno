using RustyDTO.Interfaces;

namespace RustyDTO.PropertyModels;

public interface IMineSizeProperty : IRustyEntityProperty
{
    public int X { get; set; }
    public int Y { get; set; }
}