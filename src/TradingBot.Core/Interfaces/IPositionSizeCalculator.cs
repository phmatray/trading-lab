// <copyright file="IPositionSizeCalculator.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Service for calculating position sizes using various algorithms.
/// </summary>
public interface IPositionSizeCalculator
{
    /// <summary>
    /// Calculates position size using fixed dollar amount per trade.
    /// </summary>
    /// <param name="fixedAmount">Fixed dollar amount to risk per trade.</param>
    /// <param name="currentPrice">Current asset price.</param>
    /// <param name="leverage">Leverage multiplier (default 1.0).</param>
    /// <returns>Position size in units.</returns>
    decimal CalculateFixedAmount(
        decimal fixedAmount,
        decimal currentPrice,
        decimal leverage = 1.0m);

    /// <summary>
    /// Calculates position size using fixed percentage of account balance.
    /// </summary>
    /// <param name="accountBalance">Current account balance.</param>
    /// <param name="percentOfAccount">Percentage of account to risk (e.g., 2.0 for 2%).</param>
    /// <param name="currentPrice">Current asset price.</param>
    /// <param name="leverage">Leverage multiplier (default 1.0).</param>
    /// <returns>Position size in units.</returns>
    decimal CalculateFixedPercent(
        decimal accountBalance,
        decimal percentOfAccount,
        decimal currentPrice,
        decimal leverage = 1.0m);

    /// <summary>
    /// Calculates position size based on risk per trade and stop-loss distance.
    /// </summary>
    /// <param name="accountBalance">Current account balance.</param>
    /// <param name="riskPercent">Percentage of account to risk (e.g., 1.0 for 1%).</param>
    /// <param name="entryPrice">Entry price.</param>
    /// <param name="stopLossPrice">Stop-loss price.</param>
    /// <param name="leverage">Leverage multiplier (default 1.0).</param>
    /// <returns>Position size in units.</returns>
    decimal CalculateRiskBased(
        decimal accountBalance,
        decimal riskPercent,
        decimal entryPrice,
        decimal stopLossPrice,
        decimal leverage = 1.0m);

    /// <summary>
    /// Calculates position size using Kelly Criterion.
    /// </summary>
    /// <param name="accountBalance">Current account balance.</param>
    /// <param name="winRate">Historical win rate (0.0 to 1.0).</param>
    /// <param name="avgWin">Average winning trade amount.</param>
    /// <param name="avgLoss">Average losing trade amount.</param>
    /// <param name="currentPrice">Current asset price.</param>
    /// <param name="leverage">Leverage multiplier (default 1.0).</param>
    /// <param name="kellyFraction">Fraction of Kelly to use (default 0.25 for quarter-Kelly).</param>
    /// <returns>Position size in units.</returns>
    decimal CalculateKelly(
        decimal accountBalance,
        decimal winRate,
        decimal avgWin,
        decimal avgLoss,
        decimal currentPrice,
        decimal leverage = 1.0m,
        decimal kellyFraction = 0.25m);

    /// <summary>
    /// Calculates position size based on volatility (ATR).
    /// </summary>
    /// <param name="accountBalance">Current account balance.</param>
    /// <param name="riskPercent">Percentage of account to risk (e.g., 2.0 for 2%).</param>
    /// <param name="atr">Average True Range value.</param>
    /// <param name="atrMultiplier">ATR multiplier for position sizing (default 2.0).</param>
    /// <param name="currentPrice">Current asset price.</param>
    /// <param name="leverage">Leverage multiplier (default 1.0).</param>
    /// <returns>Position size in units.</returns>
    decimal CalculateVolatilityBased(
        decimal accountBalance,
        decimal riskPercent,
        decimal atr,
        decimal atrMultiplier,
        decimal currentPrice,
        decimal leverage = 1.0m);
}
