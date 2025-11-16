// <copyright file="StrategyConfigurationDto.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace TradingBot.Web.Models;

/// <summary>
/// Data transfer object for weekly cash-managed strategy configuration.
/// Used for user input and validation in web forms.
/// </summary>
public sealed class StrategyConfigurationDto : IValidatableObject
{
    /// <summary>
    /// Gets or sets the strategy name (unique identifier).
    /// </summary>
    [Required(ErrorMessage = "Strategy name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Strategy name must be between 3 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ETP symbol to trade (e.g., "BTCW", "ETHW").
    /// </summary>
    [Required(ErrorMessage = "ETP symbol is required")]
    [RegularExpression(@"^[A-Z]{2,6}$", ErrorMessage = "ETP symbol must be 2-6 uppercase letters")]
    public string EtpSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the underlying asset symbol to track (e.g., "COIN" for Bitcoin).
    /// </summary>
    [Required(ErrorMessage = "Underlying symbol is required")]
    [RegularExpression(@"^[A-Z]{2,6}$", ErrorMessage = "Underlying symbol must be 2-6 uppercase letters")]
    public string UnderlyingSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the minimum cash ratio (0-1, e.g., 0.15 = 15%).
    /// </summary>
    [Required]
    [Range(0.0, 1.0, ErrorMessage = "Minimum cash ratio must be between 0% and 100%")]
    public decimal MinCashRatio { get; set; } = 0.15m;

    /// <summary>
    /// Gets or sets the maximum cash ratio (0-1, e.g., 0.25 = 25%).
    /// </summary>
    [Required]
    [Range(0.0, 1.0, ErrorMessage = "Maximum cash ratio must be between 0% and 100%")]
    public decimal MaxCashRatio { get; set; } = 0.25m;

    /// <summary>
    /// Gets or sets the weekly buy ratio (0-1, e.g., 0.05 = 5% of equity).
    /// </summary>
    [Required]
    [Range(0.0, 1.0, ErrorMessage = "Weekly buy ratio must be between 0% and 100%")]
    public decimal WeeklyBuyRatio { get; set; } = 0.05m;

    /// <summary>
    /// Gets or sets the weekly sell ratio (0-1, e.g., 0.10 = 10% of position).
    /// </summary>
    [Required]
    [Range(0.0, 1.0, ErrorMessage = "Weekly sell ratio must be between 0% and 100%")]
    public decimal WeeklySellRatio { get; set; } = 0.10m;

    /// <summary>
    /// Gets or sets the execution day of week (0=Sunday, 5=Friday).
    /// </summary>
    [Required]
    [Range(0, 6, ErrorMessage = "Execution day must be between 0 (Sunday) and 6 (Saturday)")]
    public int ExecutionDayOfWeek { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether the breakout rule is enabled.
    /// </summary>
    public bool IsBreakoutRuleEnabled { get; set; }

    /// <summary>
    /// Gets or sets the breakout rule weekly price increase threshold (0-1, e.g., 0.10 = 10%).
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "Price threshold must be between 0% and 100%")]
    public decimal? BreakoutPriceThreshold { get; set; } = 0.10m;

    /// <summary>
    /// Gets or sets the breakout rule volume multiplier (e.g., 1.5 = 150% of average).
    /// </summary>
    [Range(0.01, 10.0, ErrorMessage = "Volume multiplier must be between 0.01 and 10")]
    public decimal? BreakoutVolumeMultiplier { get; set; } = 1.5m;

    /// <summary>
    /// Gets or sets the breakout rule buy ratio multiplier (e.g., 2.0 = double buy amount).
    /// </summary>
    [Range(1.01, 10.0, ErrorMessage = "Buy multiplier must be greater than 1 and less than 10")]
    public decimal? BreakoutBuyMultiplier { get; set; } = 2.0m;

    /// <summary>
    /// Validates that MinCashRatio is less than MaxCashRatio.
    /// </summary>
    /// <param name="validationContext">Validation context.</param>
    /// <returns>Validation result.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinCashRatio >= MaxCashRatio)
        {
            yield return new ValidationResult(
                "Minimum cash ratio must be less than maximum cash ratio",
                new[] { nameof(MinCashRatio), nameof(MaxCashRatio) });
        }
    }
}
