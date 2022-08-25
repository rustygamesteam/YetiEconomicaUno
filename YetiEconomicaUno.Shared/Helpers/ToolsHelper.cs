using RustyDTO.Interfaces;
using RustyDTO.PropertyModels;
using YetiEconomicaUno.ViewModels.CalculateBalance;

namespace YetiEconomicaUno.Helpers;

public static class ToolsHelper
{
    public static ToolsEnum GetType(IRustyEntity owner)
    {
        return owner.DisplayName switch
        {
            "Axe" => ToolsEnum.Axe,
            "Pick" => ToolsEnum.Pick,
            "Shovel" => ToolsEnum.Shovel,
            _ => ToolsEnum.Unknow
        };
    }

    public static ToolsEnum GetGroupType(IRustyEntity rustyEntity)
    {
        if(!rustyEntity.TryGetProperty<IHasOwner>(out var owner))
            return ToolsEnum.Unknow;

        return GetType(owner.Owner);
    }
}
