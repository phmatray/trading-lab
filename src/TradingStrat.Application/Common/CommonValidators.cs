using TradingStrat.Domain.Common;

namespace TradingStrat.Application.Common;

/// <summary>
/// Reusable validation helpers for commands.
/// Reduces duplication in command constructors by providing common validation patterns.
/// </summary>
public static class CommonValidators
{
    /// <summary>
    /// Validates date range (start &lt;= end, neither in future).
    /// </summary>
    /// <param name="startDate">The start date to validate.</param>
    /// <param name="endDate">The end date to validate.</param>
    /// <param name="startParamName">The parameter name for start date (for error messages).</param>
    /// <param name="endParamName">The parameter name for end date (for error messages).</param>
    /// <exception cref="ArgumentException">Thrown when date range is invalid.</exception>
    public static void ValidateDateRange(
        DateTime? startDate,
        DateTime? endDate,
        string startParamName = "StartDate",
        string endParamName = "EndDate")
    {
        if (startDate.HasValue)
        {
            ValidationGuard.Require(
                startDate.Value <= DateTime.Today,
                "Start date cannot be in the future",
                startParamName);
        }

        if (endDate.HasValue)
        {
            ValidationGuard.Require(
                endDate.Value <= DateTime.Today,
                "End date cannot be in the future",
                endParamName);
        }

        if (startDate.HasValue && endDate.HasValue)
        {
            ValidationGuard.Require(
                startDate.Value <= endDate.Value,
                "Start date must be before or equal to end date",
                startParamName);
        }
    }

    /// <summary>
    /// Validates and normalizes a ticker symbol.
    /// Ensures not null/whitespace and returns uppercase trimmed value.
    /// </summary>
    /// <param name="Ticker">The ticker symbol to validate and normalize.</param>
    /// <returns>The normalized ticker (uppercase, trimmed).</returns>
    /// <exception cref="ArgumentException">Thrown when ticker is null or whitespace.</exception>
    public static string NormalizeTicker(string Ticker)
    {
        ValidationGuard.Require(Ticker).NotNullOrWhiteSpace();
        return Ticker.ToUpperInvariant().Trim();
    }

    /// <summary>
    /// Validates commission parameters.
    /// </summary>
    /// <param name="commissionPercentage">The commission percentage (0-1 range, e.g., 0.001 = 0.1%).</param>
    /// <param name="minimumCommission">The minimum commission amount.</param>
    /// <exception cref="ArgumentException">Thrown when commission parameters are invalid.</exception>
    public static void ValidateCommission(
        decimal commissionPercentage,
        decimal minimumCommission)
    {
        ValidationGuard.Require(commissionPercentage)
            .GreaterThanOrEqual(0m, "Commission percentage cannot be negative")
            .LessThan(1m, "Commission percentage must be less than 100%");

        ValidationGuard.Require(minimumCommission)
            .GreaterThanOrEqual(0m, "Minimum commission cannot be negative");
    }

    /// <summary>
    /// Validates capital amount (must be positive).
    /// </summary>
    /// <param name="capital">The capital amount to validate.</param>
    /// <param name="paramName">The parameter name (for error messages).</param>
    /// <exception cref="ArgumentException">Thrown when capital is not positive.</exception>
    public static void ValidateCapital(decimal capital, string paramName = "Capital")
    {
        ValidationGuard.Require(capital)
            .GreaterThan(0m, $"{paramName} must be positive");
    }

    /// <summary>
    /// Validates percentage (0-100 range).
    /// </summary>
    /// <param name="percentage">The percentage value to validate.</param>
    /// <param name="paramName">The parameter name (for error messages).</param>
    /// <exception cref="ArgumentException">Thrown when percentage is outside 0-100 range.</exception>
    public static void ValidatePercentage(
        decimal percentage,
        string paramName = "Percentage")
    {
        ValidationGuard.Require(percentage)
            .GreaterThanOrEqual(0m, $"{paramName} cannot be negative")
            .LessThanOrEqual(100m, $"{paramName} cannot exceed 100");
    }

    /// <summary>
    /// Validates ratio (0-1 range).
    /// </summary>
    /// <param name="ratio">The ratio value to validate.</param>
    /// <param name="paramName">The parameter name (for error messages).</param>
    /// <param name="customMessage">Optional custom error message.</param>
    /// <exception cref="ArgumentException">Thrown when ratio is outside 0-1 range.</exception>
    public static void ValidateRatio(
        decimal ratio,
        string paramName = "Ratio",
        string? customMessage = null)
    {
        ValidationGuard.Require(ratio)
            .GreaterThanOrEqual(0m, customMessage ?? $"{paramName} must be between 0 and 1")
            .LessThanOrEqual(1m, customMessage ?? $"{paramName} must be between 0 and 1");
    }

    /// <summary>
    /// Validates that a value is positive (greater than zero).
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name (for error messages).</param>
    /// <exception cref="ArgumentException">Thrown when value is not positive.</exception>
    public static void ValidatePositive(decimal value, string paramName = "Value")
    {
        ValidationGuard.Require(value)
            .GreaterThan(0m, $"{paramName} must be positive");
    }

    /// <summary>
    /// Validates that a value is non-negative (greater than or equal to zero).
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name (for error messages).</param>
    /// <exception cref="ArgumentException">Thrown when value is negative.</exception>
    public static void ValidateNonNegative(decimal value, string paramName = "Value")
    {
        ValidationGuard.Require(value)
            .GreaterThanOrEqual(0m, $"{paramName} cannot be negative");
    }
}
