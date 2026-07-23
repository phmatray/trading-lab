// <copyright file="StrategyConfigurationDto.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace TradingBot.Core.DTOs;

/// <summary>
/// Data transfer object for weekly cash-managed strategy configuration.
/// Used by web forms for creating and updating strategy parameters.
/// </summary>
public sealed class StrategyConfigurationDto
{
    /// <summary>
    /// Gets or sets the strategy unique identifier (null for new strategies).
    /// </summary>
    public Guid? StrategyId { get; set; }

    /// <summary>
    /// Gets or sets the ETP symbol to trade (e.g., "BTCW").
    /// </summary>
    [Required(ErrorMessage = "ETP symbol is required")]
    [StringLength(10, MinimumLength = 1, ErrorMessage = "ETP symbol must be 1-10 characters")]
    public required string EtpSymbol { get; set; }

    /// <summary>
    /// Gets or sets the underlying asset symbol (e.g., "COIN").
    /// </summary>
    [Required(ErrorMessage = "Underlying symbol is required")]
    [StringLength(10, MinimumLength = 1, ErrorMessage = "Underlying symbol must be 1-10 characters")]
    public required string UnderlyingSymbol { get; set; }

    /// <summary>
    /// Gets or sets the minimum cash ratio (0.0 to 1.0, default 0.15 for 15%).
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "Minimum cash ratio must be between 0 and 1")]
    public decimal MinCashRatio { get; set; } = 0.15m;

    /// <summary>
    /// Gets or sets the maximum cash ratio (0.0 to 1.0, default 0.25 for 25%).
    /// Must be greater than MinCashRatio.
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "Maximum cash ratio must be between 0 and 1")]
    public decimal MaxCashRatio { get; set; } = 0.25m;

    /// <summary>
    /// Gets or sets the weekly buy ratio (percentage of equity to invest, 0.0 to 1.0, default 0.05 for 5%).
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "Weekly buy ratio must be between 0 and 1")]
    public decimal WeeklyBuyRatio { get; set; } = 0.05m;

    /// <summary>
    /// Gets or sets the weekly sell ratio (percentage of position to sell, 0.0 to 1.0, default 0.10 for 10%).
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "Weekly sell ratio must be between 0 and 1")]
    public decimal WeeklySellRatio { get; set; } = 0.10m;

    /// <summary>
    /// Gets or sets the day of week for weekly execution (default: Friday).
    /// </summary>
    [Range(0, 6, ErrorMessage = "Execution day must be 0 (Sunday) to 6 (Saturday)")]
    public DayOfWeek ExecutionDayOfWeek { get; set; } = DayOfWeek.Friday;

    /// <summary>
    /// Gets or sets a value indicating whether the strategy is enabled/active.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the breakout rule is enabled.
    /// </summary>
    public bool BreakoutRuleEnabled { get; set; }

    /// <summary>
    /// Gets or sets the breakout weekly price increase threshold (default 0.10 for 10%).
    /// Only used if BreakoutRuleEnabled is true.
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "Breakout price threshold must be between 0 and 1")]
    public decimal BreakoutPriceThreshold { get; set; } = 0.10m;

    /// <summary>
    /// Gets or sets the breakout volume multiplier (default 1.5x average).
    /// Only used if BreakoutRuleEnabled is true.
    /// </summary>
    [Range(1.0, 10.0, ErrorMessage = "Breakout volume multiplier must be between 1.0 and 10.0")]
    public decimal BreakoutVolumeMultiplier { get; set; } = 1.5m;

    /// <summary>
    /// Gets or sets the breakout buy ratio multiplier (default 2.0x for doubling).
    /// Only used if BreakoutRuleEnabled is true.
    /// </summary>
    [Range(1.0, 5.0, ErrorMessage = "Breakout buy multiplier must be between 1.0 and 5.0")]
    public decimal BreakoutBuyMultiplier { get; set; } = 2.0m;

    /// <summary>
    /// Validates that MaxCashRatio is greater than MinCashRatio.
    /// </summary>
    /// <returns>Validation result.</returns>
    public ValidationResult? ValidateCashRatios()
    {
        if (MaxCashRatio <= MinCashRatio)
        {
            return new ValidationResult(
                "Maximum cash ratio must be greater than minimum cash ratio",
                new[] { nameof(MaxCashRatio) });
        }

        return ValidationResult.Success;
    }
}
