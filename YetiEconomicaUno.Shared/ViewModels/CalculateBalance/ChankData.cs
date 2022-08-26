using RustyDTO.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using YetiEconomicaCore.Database;

namespace YetiEconomicaUno.ViewModels.CalculateBalance;

internal record struct ChankData
{
    public ResourceStackRecord[] Wallet { get; }
    public KeyValuePair<ToolsEnum, ToolProgressInfo>[] Tools { get; }
    public ImmutableHashSet<int> UserBag { get; }

    public DateTime Time { get; }

    public int SessionIndex { get; }

    public int FarmCells { get; }
    public (int x, int y) MineSize { get; }

    public ResourceStackRecord[] GetWallet()
    {
        return Wallet ?? Array.Empty<ResourceStackRecord>();
    }

    public KeyValuePair<ToolsEnum, ToolProgressInfo>[] GetTools()
    {
        return Tools ?? Array.Empty<KeyValuePair<ToolsEnum, ToolProgressInfo>>();
    }

    public ChankData(DateTime time = default, int sessionIndex = 0)
    {
        Time = time;
        SessionIndex = sessionIndex;

        FarmCells = 1;
        MineSize = new(4, 5);

        Wallet = Array.Empty<ResourceStackRecord>();
        Tools = Array.Empty<KeyValuePair<ToolsEnum, ToolProgressInfo>>();
    }

    public ChankData(Dictionary<IRustyEntity, double> wallet, Dictionary<ToolsEnum, ToolProgressInfo> tools, HashSet<int> userBag, DateTime time, int sessionIndex, int farmSize, (int x, int y) mineSize)
    {
        Time = time;
        SessionIndex = sessionIndex;

        Wallet = wallet.Select(static pair => new ResourceStackRecord(pair.Key, pair.Value)).ToArray();
        Tools = tools.ToArray();
        UserBag = userBag.ToImmutableHashSet();

        FarmCells = farmSize;
        MineSize = mineSize;
    }

    public ChankData(UserData userData) : this(userData.Wallet, userData.Tools, userData.UserBag, userData.Time, userData.SessionIndex, userData.FarmCells, userData.MineSize)
    {

    }

    internal UserData ToUserData()
    {
        return new UserData(Time, SessionIndex, FarmCells, MineSize, CalculateBalanceViewModel.TakeWallet(GetWallet()), GetTools().ToDictionary(static pair => pair.Key, static pair => pair.Value), UserBag.ToHashSet());
    }

}
