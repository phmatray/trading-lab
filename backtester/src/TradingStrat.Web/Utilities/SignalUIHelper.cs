using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Web.Utilities;

/// <summary>
/// Utility class for signal-related UI formatting.
/// Provides consistent styling and presentation across signal displays.
/// </summary>
public static class SignalUIHelper
{
    /// <summary>
    /// Gets the CSS class for signal color coding.
    /// Buy = green (metric-positive), Sell = red (metric-negative), Hold = gray.
    /// </summary>
    /// <param name="signal">The signal type to get color class for.</param>
    /// <returns>Tailwind CSS class for the signal color.</returns>
    public static string GetSignalColorClass(SignalType signal) => signal switch
    {
        SignalType.Buy => "metric-positive",
        SignalType.Sell => "metric-negative",
        _ => "text-gray-600"
    };

    /// <summary>
    /// Gets the emoji representation for a signal.
    /// Buy = 📈, Sell = 📉, Hold = ➡️.
    /// </summary>
    /// <param name="signal">The signal type to get emoji for.</param>
    /// <returns>Emoji string representing the signal.</returns>
    public static string GetSignalEmoji(SignalType signal) => signal switch
    {
        SignalType.Buy => "📈",
        SignalType.Sell => "📉",
        _ => "➡️"
    };

    /// <summary>
    /// Gets a formatted display string combining emoji and signal name.
    /// </summary>
    /// <param name="signal">The signal type to format.</param>
    /// <returns>Formatted string like "📈 Buy" or "📉 Sell".</returns>
    public static string GetSignalDisplayText(SignalType signal) =>
        $"{GetSignalEmoji(signal)} {signal}";
}
