using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Standardized progress reporting for use case operations.
/// Provides consistent progress updates across all use cases with optional step tracking.
/// </summary>
public sealed class UseCaseProgress : ValueObject
{
    /// <summary>Human-readable progress message.</summary>
    public string Message { get; init; }

    /// <summary>Current step number (1-based), if applicable.</summary>
    public int? CurrentStep { get; init; }

    /// <summary>Total number of steps, if applicable.</summary>
    public int? TotalSteps { get; init; }

    /// <summary>Percentage complete (0-100), if applicable.</summary>
    public int? PercentComplete { get; init; }

    public UseCaseProgress(
        string Message,
        int? CurrentStep = null,
        int? TotalSteps = null,
        int? PercentComplete = null)
    {
        this.Message = Message;
        this.CurrentStep = CurrentStep;
        this.TotalSteps = TotalSteps;
        this.PercentComplete = PercentComplete;
    }

    /// <summary>
    /// Creates a simple progress update with just a message.
    /// </summary>
    /// <param name="message">The progress message.</param>
    /// <returns>A progress update with only a message.</returns>
    public static UseCaseProgress Simple(string message) => new(message);

    /// <summary>
    /// Creates a progress update with step tracking and automatic percentage calculation.
    /// </summary>
    /// <param name="message">The progress message.</param>
    /// <param name="current">Current step number (1-based).</param>
    /// <param name="total">Total number of steps.</param>
    /// <returns>A progress update with step tracking and calculated percentage.</returns>
    public static UseCaseProgress WithSteps(string message, int current, int total) =>
        new(message, current, total, CalculatePercentage(current, total));

    /// <summary>
    /// Creates a progress update with explicit percentage.
    /// </summary>
    /// <param name="message">The progress message.</param>
    /// <param name="percent">Percentage complete (0-100).</param>
    /// <returns>A progress update with explicit percentage.</returns>
    public static UseCaseProgress WithPercentage(string message, int percent) =>
        new(message, PercentComplete: percent);

    private static int CalculatePercentage(int current, int total)
    {
        if (total <= 0)
        {
            return 0;
        }

        return (int)Math.Round((decimal)current / total * 100);
    }

    /// <summary>
    /// Converts to a simple string progress message (for backwards compatibility).
    /// </summary>
    /// <returns>The progress message with optional percentage.</returns>
    public override string ToString()
    {
        if (PercentComplete.HasValue)
        {
            return $"{Message} ({PercentComplete}%)";
        }

        if (CurrentStep.HasValue && TotalSteps.HasValue)
        {
            return $"{Message} ({CurrentStep}/{TotalSteps})";
        }

        return Message;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Message;
        yield return CurrentStep ?? 0;
        yield return TotalSteps ?? 0;
        yield return PercentComplete ?? 0;
    }
}
