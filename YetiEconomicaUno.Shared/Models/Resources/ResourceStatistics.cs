using System.Collections.Generic;
using System.Linq;

namespace YetiEconomicaUno.Models.Resources;

public record struct ResourceStatistics(string Header, IEnumerable<string> Items)
{
    public bool IsValid => Items.Any();
}