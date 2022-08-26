using RustyDTO.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using YetiEconomicaUno.Services;
using YetiEconomicaUno.ViewModels.CalculateBalance.Progress;

namespace YetiEconomicaUno.ViewModels.CalculateBalance;

public record struct ToolProgressInfo(int Index, DateTime NextUpdate);

public ref struct UserData
{
    public DateTime Begin { get; }
    public Dictionary<IRustyEntity, double> Wallet { get; }
    public Dictionary<ToolsEnum, ToolProgressInfo> Tools { get; }

    public HashSet<int> UserBag { get; }

    public int SessionIndex;
    public TimeSpan Offset = TimeSpan.Zero;
    public DateTime Time => Begin + Offset;

    public int FarmCells { get; set; }
    public (int x, int y) MineSize { get; set; }

    public UserData(DateTime begin, int session, int farmCells, (int x, int y) mineSize, Dictionary<IRustyEntity, double> wallet, Dictionary<ToolsEnum, ToolProgressInfo> tools, HashSet<int> userBag)
    {
        Begin = begin;
        SessionIndex = session;
        Wallet = wallet;
        Tools = tools;
        FarmCells = farmCells;
        MineSize = mineSize;
        UserBag = userBag;
    }

    public void Next()
    {
        var sessions = CalculateBalanceService.Instance.Sessions;
        if (!DetectNextSession(sessions))
            SessionIndex = (SessionIndex + 1) % sessions.Count;


        var time = Time;
        if (SessionIndex == 0)
            Offset = time.AddDays(1).Date.AddHours(sessions[0].Hour) - Begin;
        else if (time.Date.AddHours(sessions[SessionIndex].Hour) > time)
            Offset = time.Date.AddHours(sessions[SessionIndex].Hour) - Begin;

        CollectBaseResourcesTask.TrySimpleEvalute(ref this);
    }

    public void Next(DateTime next)
    {
        try
        {
            var wait = next - Time;
            if (wait < TimeSpan.FromMinutes(15))
            {
                Offset += wait;

                DetectNextSession(CalculateBalanceService.Instance.Sessions);
                CollectBaseResourcesTask.TrySimpleEvalute(ref this);
            }
            else
                Next();
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    private bool DetectNextSession(IList<SessionTimeRecord> sessions)
    {
        try
        {
            var time = Time;

            if (time.Date.AddHours(sessions[SessionIndex].Hour) < time)
            {
                while (time.Date.AddHours(sessions[SessionIndex].Hour) < time)
                {
                    SessionIndex = (SessionIndex + 1) % sessions.Count;
                    if (SessionIndex == 0)
                        return true;
                }
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    internal UserDataDump GetDump() => new UserDataDump(Wallet.ToImmutableDictionary(), Tools.ToImmutableDictionary(), UserBag.ToImmutableHashSet(), FarmCells, MineSize, Begin, Time);
}

public record struct UserDataDump(IReadOnlyDictionary<IRustyEntity, double> Wallet, IReadOnlyDictionary<ToolsEnum, ToolProgressInfo> Tools, IReadOnlySet<int> UserBag, int FarmCells, (int X, int Y) MineSize, DateTime StartTime, DateTime EndTime)
{
    internal bool TryGetTool(ToolsEnum toolsEnum, out ToolProgressInfo toolInfo)
    {
        return Tools.TryGetValue(toolsEnum, out toolInfo);
    }
}
