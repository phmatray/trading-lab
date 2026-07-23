// <copyright file="PortfolioHistoryFilter.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Enums;

namespace TradingBot.Web.Models;

/// <summary>
/// Filtering criteria for portfolio history page.
/// </summary>
public sealed class PortfolioHistoryFilter
{
    /// <summary>
    /// Gets or sets the start date for filtering trades.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for filtering trades.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the symbol filter.
    /// </summary>
    public string? Symbol { get; set; }

    /// <summary>
    /// Gets or sets the strategy name filter.
    /// </summary>
    public string? StrategyName { get; set; }

    /// <summary>
    /// Gets or sets the minimum P and L filter.
    /// </summary>
    public decimal? MinPnL { get; set; }

    /// <summary>
    /// Gets or sets the maximum P and L filter.
    /// </summary>
    public decimal? MaxPnL { get; set; }

    /// <summary>
    /// Gets or sets the side filter (buy/sell).
    /// </summary>
    public OrderSide? Side { get; set; }

    /// <summary>
    /// Gets or sets the page number for pagination.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size for pagination.
    /// </summary>
    public int PageSize { get; set; } = 25;
}
