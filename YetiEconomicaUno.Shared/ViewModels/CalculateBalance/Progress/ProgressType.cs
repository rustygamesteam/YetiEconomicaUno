using System;
using System.Collections.Generic;
using System.Text;

namespace YetiEconomicaUno.ViewModels.CalculateBalance.Progress;

public enum ProgressType : int
{
    None = 0,
    YetiObject = 1,
    FarmPlant = 2,
    ResourceGift = 3,
    Convert = 6,
    RecycleResources = 7,
    Craft = 8,
}

public enum StatisticInfo
{
    None,
    CollectResources,
    PreCraft,
    Process,
    FarmPlants,
    FarmCycles,
    Idle
}