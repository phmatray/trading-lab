namespace TradyStrat.Shared.Domain;

public static class IndicatorKindParser
{
    public static IndicatorKind? From(string? citationLabel)
    {
        if (string.IsNullOrWhiteSpace(citationLabel)) return null;
        var label = citationLabel.Trim();

        if (label.StartsWith("RSI", StringComparison.OrdinalIgnoreCase))
            return IndicatorKind.Rsi;
        if (label.Equals("Bollinger", StringComparison.OrdinalIgnoreCase))
            return IndicatorKind.Bollinger;
        if (label.Equals("Ichimoku", StringComparison.OrdinalIgnoreCase))
            return IndicatorKind.Ichimoku;
        if (label.Contains("200", StringComparison.Ordinal) &&
            label.Contains("SMA", StringComparison.OrdinalIgnoreCase))
            return IndicatorKind.Sma200;
        if (label.Contains("50", StringComparison.Ordinal) &&
            label.Contains("SMA", StringComparison.OrdinalIgnoreCase))
            return IndicatorKind.Sma50;
        return null;
    }
}
