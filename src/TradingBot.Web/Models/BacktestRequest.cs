// <copyright file="BacktestRequest.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace TradingBot.Web.Models;

/// <summary>
/// Represents a request to run a backtest with validation attributes.
/// </summary>
public class BacktestRequest
{
    /// <summary>
    /// Gets or sets the name of the strategy to backtest.
    /// </summary>
    [Required(ErrorMessage = "Strategy is required")]
    public string StrategyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trading symbol to backtest (e.g., "AAPL").
    /// </summary>
    [Required(ErrorMessage = "Symbol is required")]
    [RegularExpression(@"^[A-Z]{1,5}$", ErrorMessage = "Symbol must be 1-5 uppercase letters")]
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start date of the backtest period.
    /// </summary>
    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date of the backtest period.
    /// </summary>
    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Gets or sets the initial capital for the backtest.
    /// </summary>
    [Required(ErrorMessage = "Initial capital is required")]
    [Range(1000, 10000000, ErrorMessage = "Initial capital must be between $1,000 and $10,000,000")]
    public decimal InitialCapital { get; set; } = 100000m;
}
