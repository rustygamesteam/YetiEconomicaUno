using Microsoft.UI.Xaml.Data;
using System;
using System.Text;

namespace YetiEconomicaUno.Converters;

public class DurationLabelConverter : IValueConverter
{
    private static readonly StringBuilder StringBuilder = new(128);

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not (int seconds and > 0))
        {
            if (parameter is string formatMask)
                return formatMask.Contains("{0}") ? string.Format(formatMask, "instantly") : $"{formatMask} instantly";
            return "Instantly";
        }


        StringBuilder.Length = 0;

        string mask = null;
        if (parameter is string format)
        {
            if (format.Contains("{0}"))
            {
                mask = format;
            }
            else
            {
                StringBuilder.Append(format);
                StringBuilder.Append(' ');
            }
        }

        FillDuration(StringBuilder, seconds);

        return mask != null ? string.Format(mask, StringBuilder.ToString()) : StringBuilder.ToString();
    }

    public static void FillDuration(StringBuilder sb, int seconds)
    {
        var minutes = seconds / 60;
        var hours = minutes / 60;
        var days = hours / 24;

        seconds %= 60;
        minutes %= 60;
        hours %= 24;

        bool hasValue = false;

        if (days > 0)
        {
            sb.Append(days);
            sb.Append(" days");
            hasValue = true;
        }

        if (hours > 0)
        {
            if (hasValue)
                sb.Append(' ');
            sb.Append(hours);
            sb.Append(" hours");
            hasValue = true;
        }

        if (minutes > 0)
        {
            if (hasValue)
                sb.Append(' ');
            sb.Append(minutes);
            sb.Append(" minutes");
            hasValue = true;
        }

        if (days == 0 && (hours == 0 || minutes == 0) && seconds > 0)
        {
            if (hasValue)
                sb.Append(' ');
            sb.Append(seconds);
            sb.Append(" seconds");
        }
    }

    internal static string GetDuration(int seconds)
    {
        StringBuilder.Length = 0;
        FillDuration(StringBuilder, seconds);
        return StringBuilder.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}