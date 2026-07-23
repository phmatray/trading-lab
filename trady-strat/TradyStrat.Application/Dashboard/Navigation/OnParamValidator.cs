using System.Globalization;

namespace TradyStrat.Application.Dashboard.Navigation;

public static class OnParamValidator
{
    public static async Task<ValidationResult> Validate(
        string? onParam, IEntryNavigationService nav, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(onParam))
            return new ValidationResult.Live();

        if (!DateOnly.TryParseExact(
                onParam, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var date))
            return new ValidationResult.RedirectTo("/");

        var latest = await nav.LatestAsync(ct);
        if (date > latest)
            return new ValidationResult.RedirectTo("/");

        var earliest = await nav.EarliestAsync(ct);
        if (date < earliest)
            return new ValidationResult.RedirectTo($"/?on={Format(earliest)}");

        var resolved = await nav.ResolveOrFallbackAsync(date, ct);
        if (resolved != date)
            return new ValidationResult.RedirectTo($"/?on={Format(resolved)}");

        return new ValidationResult.Historical(date);
    }

    private static string Format(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
