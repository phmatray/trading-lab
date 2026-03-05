// <copyright file="BreakoutRuleConfig.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.SharedKernel;

namespace TradingBot.Core.ValueObjects;

/// <summary>
/// Immutable value object representing breakout rule configuration.
/// When enabled, doubles buy amount if weekly price increase and volume conditions are met.
/// </summary>
public sealed class BreakoutRuleConfig : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BreakoutRuleConfig"/> class.
    /// </summary>
    /// <param name="isEnabled">Whether the breakout rule is enabled.</param>
    /// <param name="weeklyPriceIncreaseThreshold">Minimum weekly price increase percentage (e.g., 0.10 = 10%).</param>
    /// <param name="volumeMultiplier">Minimum volume as multiple of 20-day average (e.g., 1.5 = 150% of avg).</param>
    /// <param name="buyRatioMultiplier">Multiplier for buy ratio when breakout detected (e.g., 2.0 = double).</param>
    public BreakoutRuleConfig(
        bool isEnabled,
        decimal weeklyPriceIncreaseThreshold = 0.10m,
        decimal volumeMultiplier = 1.5m,
        decimal buyRatioMultiplier = 2.0m)
    {
        IsEnabled = isEnabled;
        WeeklyPriceIncreaseThreshold = weeklyPriceIncreaseThreshold;
        VolumeMultiplier = volumeMultiplier;
        BuyRatioMultiplier = buyRatioMultiplier;
    }

    /// <summary>
    /// Gets a value indicating whether the breakout rule is enabled.
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Gets the weekly price increase threshold (0-1, e.g., 0.10 = 10%).
    /// </summary>
    public decimal WeeklyPriceIncreaseThreshold { get; }

    /// <summary>
    /// Gets the volume multiplier (e.g., 1.5 = 150% of 20-day average volume).
    /// </summary>
    public decimal VolumeMultiplier { get; }

    /// <summary>
    /// Gets the buy ratio multiplier (e.g., 2.0 = double the buy amount).
    /// </summary>
    public decimal BuyRatioMultiplier { get; }

    /// <summary>
    /// Validates the configuration and throws if invalid.
    /// </summary>
    public void Validate()
    {
        if (WeeklyPriceIncreaseThreshold < 0 || WeeklyPriceIncreaseThreshold > 1)
        {
            throw new InvalidOperationException($"{nameof(WeeklyPriceIncreaseThreshold)} must be between 0 and 1");
        }

        if (VolumeMultiplier <= 0)
        {
            throw new InvalidOperationException($"{nameof(VolumeMultiplier)} must be positive");
        }

        if (BuyRatioMultiplier <= 1)
        {
            throw new InvalidOperationException($"{nameof(BuyRatioMultiplier)} must be greater than 1");
        }
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IsEnabled;
        yield return WeeklyPriceIncreaseThreshold;
        yield return VolumeMultiplier;
        yield return BuyRatioMultiplier;
    }
}
