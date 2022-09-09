using RustyDTO.Interfaces;

namespace YetiEconomicaCore.ReactiveImpl;

internal sealed partial class ReactiveLink
{
    public ReactiveLink(int index, IRustyEntity entity)
    {
        Index = index;
        Entity = entity;
    }
}