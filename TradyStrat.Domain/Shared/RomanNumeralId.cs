using System.Text.RegularExpressions;

namespace TradyStrat.Domain.Shared;

public readonly record struct RomanNumeralId
{
    public string Value { get; }

    private RomanNumeralId(string value) => Value = value;

    // Canonical lowercase Roman numerals 1-39.
    private static readonly Regex CanonicalLowercase = new(
        @"^m{0,3}(cm|cd|d?c{0,3})(xc|xl|l?x{0,3})(ix|iv|v?i{0,3})$",
        RegexOptions.Compiled);

    public static RomanNumeralId Of(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Roman numeral must not be empty.", nameof(raw));
        var trimmed = raw.Trim();
        if (trimmed.Length == 0 || !CanonicalLowercase.IsMatch(trimmed))
            throw new ArgumentException(
                $"'{raw}' is not a canonical lowercase Roman numeral.", nameof(raw));
        return new RomanNumeralId(trimmed);
    }

    public override string ToString() => Value;
}
