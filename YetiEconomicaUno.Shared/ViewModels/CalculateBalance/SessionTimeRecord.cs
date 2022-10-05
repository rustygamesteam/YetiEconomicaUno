using System;
using LiteDB;
using YetiEconomicaUno.Services;

namespace YetiEconomicaUno.ViewModels.CalculateBalance;

public record SessionTimeRecord(int ID, string Model, int Hour) : IEquatable<SessionTimeRecord>
{
    [BsonId]
    public int Index { get; set; }

    bool IEquatable<SessionTimeRecord>.Equals(SessionTimeRecord other)
    {
        return Hour == other.Hour;
    }

    public override string ToString()
    {
        return $"{Hour}:00";
    }

    internal static SessionTimeRecord Create(int hours) => new SessionTimeRecord(hours * 60, CalculateBalanceService.CurrentModel.Value, hours);

    internal static SessionTimeRecord Create(int hours, string model) => new SessionTimeRecord(hours * 60, model, hours);
}