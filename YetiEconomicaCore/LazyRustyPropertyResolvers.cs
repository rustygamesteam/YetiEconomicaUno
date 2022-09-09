using RustyDTO;
using RustyDTO.Interfaces;
using YetiEconomicaCore.Services;

namespace YetiEconomicaCore;

internal record struct ResolveByIndex(int index, DescPropertyType type) : ILazyDescPropertyResolver
{
    public IDescProperty Resolve()
    {
        return RustyEntityService.Instance.Properties.ResolveProperty(index, type);
    }
}

internal record struct ResolveByFunc(Func<IDescProperty> Func) : ILazyDescPropertyResolver
{
    public IDescProperty Resolve()
    {
        return Func.Invoke();
    }
}