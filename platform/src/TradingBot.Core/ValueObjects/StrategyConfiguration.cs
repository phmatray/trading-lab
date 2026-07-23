// <copyright file="StrategyConfiguration.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.SharedKernel;

namespace TradingBot.Core.ValueObjects;

/// <summary>
/// Immutable value object representing the configuration parameters for a weekly cash-managed strategy.
/// </summary>
public sealed class StrategyConfiguration : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyConfiguration"/> class.
    /// </summary>
    /// <param name="minCashRatio">Minimum cash ratio (0-1).</param>
    /// <param name="maxCashRatio">Maximum cash ratio (0-1).</param>
    /// <param name="weeklyBuyRatio">Weekly buy ratio (0-1).</param>
    /// <param name="weeklySellRatio">Weekly sell ratio (0-1).</param>
    /// <param name="executionDayOfWeek">Execution day of week (0-6).</param>
    /// <param name="breakoutRuleConfigJson">Optional breakout rule JSON config.</param>
    public StrategyConfiguration(
        decimal minCashRatio,
        decimal maxCashRatio,
        decimal weeklyBuyRatio,
        decimal weeklySellRatio,
        int executionDayOfWeek,
        string? breakoutRuleConfigJson = null)
    {
        MinCashRatio = minCashRatio;
        MaxCashRatio = maxCashRatio;
        WeeklyBuyRatio = weeklyBuyRatio;
        WeeklySellRatio = weeklySellRatio;
        ExecutionDayOfWeek = executionDayOfWeek;
        BreakoutRuleConfigJson = breakoutRuleConfigJson;
    }

    /// <summary>
    /// Gets the minimum cash ratio (0-1).
    /// </summary>
    public decimal MinCashRatio { get; }

    /// <summary>
    /// Gets the maximum cash ratio (0-1).
    /// </summary>
    public decimal MaxCashRatio { get; }

    /// <summary>
    /// Gets the weekly buy ratio (0-1).
    /// </summary>
    public decimal WeeklyBuyRatio { get; }

    /// <summary>
    /// Gets the weekly sell ratio (0-1).
    /// </summary>
    public decimal WeeklySellRatio { get; }

    /// <summary>
    /// Gets the execution day of week (0=Sunday, 5=Friday).
    /// </summary>
    public int ExecutionDayOfWeek { get; }

    /// <summary>
    /// Gets the optional breakout rule configuration JSON.
    /// </summary>
    public string? BreakoutRuleConfigJson { get; }

    /// <summary>
    /// Validates the configuration and throws if invalid.
    /// </summary>
    public void Validate()
    {
        if (MinCashRatio < 0 || MinCashRatio > 1)
        {
            throw new InvalidOperationException($"{nameof(MinCashRatio)} must be between 0 and 1");
        }

        if (MaxCashRatio < 0 || MaxCashRatio > 1)
        {
            throw new InvalidOperationException($"{nameof(MaxCashRatio)} must be between 0 and 1");
        }

        if (MinCashRatio >= MaxCashRatio)
        {
            throw new InvalidOperationException($"{nameof(MinCashRatio)} must be less than {nameof(MaxCashRatio)}");
        }

        if (WeeklyBuyRatio < 0 || WeeklyBuyRatio > 1)
        {
            throw new InvalidOperationException($"{nameof(WeeklyBuyRatio)} must be between 0 and 1");
        }

        if (WeeklySellRatio < 0 || WeeklySellRatio > 1)
        {
            throw new InvalidOperationException($"{nameof(WeeklySellRatio)} must be between 0 and 1");
        }

        if (ExecutionDayOfWeek < 0 || ExecutionDayOfWeek > 6)
        {
            throw new InvalidOperationException($"{nameof(ExecutionDayOfWeek)} must be between 0 (Sunday) and 6 (Saturday)");
        }
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MinCashRatio;
        yield return MaxCashRatio;
        yield return WeeklyBuyRatio;
        yield return WeeklySellRatio;
        yield return ExecutionDayOfWeek;
        yield return BreakoutRuleConfigJson;
    }
}
