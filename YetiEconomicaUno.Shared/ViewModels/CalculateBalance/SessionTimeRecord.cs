using LiteDB;
using System;

namespace YetiEconomicaUno.ViewModels.CalculateBalance;

public record struct SessionTimeRecord(int ID, int Hour) : IEquatable<SessionTimeRecord>
{
    bool IEquatable<SessionTimeRecord>.Equals(SessionTimeRecord other)
    {
        return Hour == other.Hour;
    }

    public override string ToString()
    {
        return $"{Hour}:00";
    }

    internal static SessionTimeRecord Create(int hours) => new SessionTimeRecord(hours * 60, hours);
}
