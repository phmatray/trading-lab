using System.Globalization;

namespace TradyStrat.Common.Formatting;

/// <summary>Single source of truth for displaying money, quantities and percentages.
/// Uses an explicit NumberFormatInfo (NO-BREAK SPACE group separator, comma decimal)
/// so output is deterministic regardless of ICU version. Euro amounts show 0 decimals
/// when whole or ≥ 1 000, else 2. Signed amounts: real minus (U+2212), then €, no space.
/// Percent: 1 decimal + narrow no-break space before %.</summary>
public static class NumberFormat
{
    private const char Minus = '−';      // MINUS SIGN
    private const char NarrowNbsp = ' '; // NARROW NO-BREAK SPACE
    private const decimal NoDecimalsThreshold = 1000m;

    private static readonly NumberFormatInfo Nfi = new()
    {
        NumberGroupSeparator   = " ",    // NO-BREAK SPACE
        NumberDecimalSeparator = ",",
        NumberGroupSizes       = [3],
    };

    private static int DecimalsFor(decimal amount)
        => (amount == decimal.Truncate(amount) || Math.Abs(amount) >= NoDecimalsThreshold) ? 0 : 2;

    /// <summary>Body of a euro amount (no currency symbol). Amount must be ≥ 0;
    /// use <see cref="SignedEur"/> for signed display.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="amount"/> is negative.</exception>
    public static string EurBody(decimal amount)
    {
        if (amount < 0m)
            throw new ArgumentOutOfRangeException(nameof(amount),
                "Use SignedEur for negative amounts.");
        return amount.ToString("N" + DecimalsFor(amount), Nfi);
    }

    /// <summary>"€" + <see cref="EurBody(decimal)"/>. Amount must be ≥ 0;
    /// use <see cref="SignedEur"/> for signed display.</summary>
    public static string Eur(decimal amount) => "€" + EurBody(amount);

    public static string SignedEur(decimal amount)
    {
        var sign = amount < 0 ? Minus.ToString() : "+";
        var abs = Math.Abs(amount);
        return sign + "€" + abs.ToString("N" + DecimalsFor(abs), Nfi);
    }

    public static string Qty(decimal quantity)
        => quantity == decimal.Truncate(quantity)
            ? quantity.ToString("N0", Nfi)
            : quantity.ToString("#,##0.################", Nfi);

    public static string Pct(decimal value)
    {
        var sign = value < 0m ? Minus.ToString() : "";
        return sign + Math.Abs(value).ToString("0.0", Nfi) + NarrowNbsp + "%";
    }

    public static string Price(decimal value, string currencySymbol)
        => currencySymbol + value.ToString("N2", Nfi);
}
