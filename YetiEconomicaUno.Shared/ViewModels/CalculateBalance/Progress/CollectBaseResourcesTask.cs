using System;
using RustyDTO.DescPropertyModels;
using YetiEconomicaCore.Services;
using YetiEconomicaUno.Helpers;
using YetiEconomicaUno.Services;

namespace YetiEconomicaUno.ViewModels.CalculateBalance.Progress;

internal class CollectBaseResourcesTask
{
    public static bool TrySimpleEvalute(ref UserData userData)
    {
        var yetiService = RustyEntityService.Instance;
        var simpleResources = SimpleResources.Instance;

        var time = userData.Time;

        bool isUpdate = false;
        var tools = userData.Tools;
        foreach (var tool in tools.Keys)
        {
            var toolProgress = tools[tool];
            if (toolProgress.NextUpdate > time)
                continue;

            isUpdate = true;

            var toolInfo = yetiService.GetEntity(toolProgress.Index).GetDescUnsafe<IToolSettings>();
            tools[tool] = toolProgress with { NextUpdate = time + TimeSpan.FromHours(toolInfo.RechargeEvery) };

            var session = toolInfo.Efficiency * toolInfo.Strength;
            switch (tool)
            {
                case ToolsEnum.Axe:

                    var wood = simpleResources.Wood;
                    if (wood is not null)
                        userData.Wallet.IncrimentWallet(wood, session);
                    break;
                case ToolsEnum.Pick:
                    var mineService = CalculateMinigamesService.Instance;
                    var multiplayer = mineService.GetMineValuesByClick(userData.MineSize);

                    var stone = simpleResources.Stone;
                    if (stone is not null)
                        userData.Wallet.IncrimentWallet(stone, session * multiplayer.Stone);

                    var ore = simpleResources.Ore;
                    if (ore is not null)
                        userData.Wallet.IncrimentWallet(ore, session * multiplayer.Ore);
                    break;
            }
        }
        return isUpdate;
    }
}
