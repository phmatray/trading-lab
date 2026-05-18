using System.Globalization;

namespace TradyStrat.Application.Time;

public static class RelativeTimeFormatter
{
    public static string Format(DateTime asOfUtc, DateTime nowUtc)
    {
        var delta = nowUtc - asOfUtc;
        if (delta.TotalSeconds < 60)  return "just now";
        if (delta.TotalMinutes < 60)  return $"{(int)delta.TotalMinutes} min ago";
        if (delta.TotalHours < 24)    return $"{(int)delta.TotalHours}h ago";
        var calendarDelta = nowUtc.Date.DayNumber() - asOfUtc.Date.DayNumber();
        if (calendarDelta == 1)       return "yesterday";
        if (calendarDelta < 7)        return $"{calendarDelta} days ago";
        return asOfUtc.ToString("dd MMM", CultureInfo.InvariantCulture).ToLowerInvariant();
    }

    private static int DayNumber(this DateTime dt) => DateOnly.FromDateTime(dt).DayNumber;
}
