using System.Collections.Generic;
using RustyDTO;
using RustyDTO.Interfaces;

namespace YetiEconomicaUno.ViewModels.CalculateBalance.Progress;

internal static class ProgressTaskExtension
{
    public static void IncrimentWallet(this Dictionary<IRustyEntity, double> wallet, IRustyEntity resource, double count)
    {
        wallet.TryGetValue(resource, out var tmp);
        wallet[resource] = tmp + count;
    }

    public static void PayWallet(this Dictionary<IRustyEntity, double> wallet, IRustyEntity resource, double count)
    {
        wallet.TryGetValue(resource, out var tmp);
        wallet[resource] = tmp - count;
    }

    public static void IncrimentWallet(this Dictionary<IRustyEntity, double> wallet, ResourceStack exchange)
    {
        wallet.TryGetValue(exchange.Resource, out var tmp);
        wallet[exchange.Resource] = tmp + exchange.Value;
    }

    public static void PayWallet(this Dictionary<IRustyEntity, double> wallet, ResourceStack exchange)
    {
        wallet.TryGetValue(exchange.Resource, out var tmp);
        wallet[exchange.Resource] = tmp - exchange.Value;
    }

    public static void PayWallet(this Dictionary<IRustyEntity, double> wallet, IList<ResourceStack> exchanges)
    {
        foreach (var exchange in exchanges)
            PayWallet(wallet, exchange);
    }
}

