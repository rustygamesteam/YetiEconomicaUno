using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Data;
using YetiEconomicaCore;
using YetiEconomicaCore.Database;

namespace YetiEconomicaUno.Converters;

public class PriceLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            IList<ResourceStackRecord> resourcesList => resourcesList.Select(exchange => $"• {exchange.Resource.FullName}: {exchange.Value}"),
            IEnumerable<ResourceStackRecord> resources => resources.Select(exchange => $"• {exchange.Resource.FullName}: {exchange.Value}"),
            ResourceStackRecord exchange => $"• {exchange.Resource.FullName}: {exchange.Value}",
            string str => new string[] { str },
            _ => Enumerable.Empty<string>()
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}