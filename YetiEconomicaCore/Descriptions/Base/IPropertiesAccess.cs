using RustyDTO;
using RustyDTO.Interfaces;

namespace YetiEconomicaCore.Descriptions;

internal interface IPropertiesAccess
{
    public int Index { get; }
    public IDescProperty Get(DescPropertyType type);
    public bool TryGet(DescPropertyType type, out IDescProperty property);


    public void Attach(DescPropertyType type, IDescProperty property);
    public void Detach(DescPropertyType type);
    bool TryDefaultAttach(DescPropertyType propertyType);
}