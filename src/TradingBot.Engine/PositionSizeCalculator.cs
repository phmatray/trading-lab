// <copyright file="PositionSizeCalculator.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;

namespace TradingBot.Engine;

/// <summary>
/// Service for calculating position sizes using various algorithms.
/// </summary>
public sealed class PositionSizeCalculator : IPositionSizeCalculator
{
    private readonly ILogger<PositionSizeCalculator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PositionSizeCalculator"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public PositionSizeCalculator(ILogger<PositionSizeCalculator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public decimal CalculateFixedAmount(
        decimal fixedAmount,
        decimal currentPrice,
        decimal leverage = 1.0m)
    {
        if (fixedAmount <= 0)
        {
            throw new ArgumentException("Fixed amount must be positive", nameof(fixedAmount));
        }

        if (currentPrice <= 0)
        {
            throw new ArgumentException("Current price must be positive", nameof(currentPrice));
        }

        if (leverage < 1.0m)
        {
            throw new ArgumentException("Leverage must be at least 1.0", nameof(leverage));
        }

        var positionSize = (fixedAmount * leverage) / currentPrice;

        _logger.LogDebug(
            "Fixed amount position size: {PositionSize:F4} units (${FixedAmount:N2} @ ${Price:F2}, leverage {Leverage}x)",
            positionSize,
            fixedAmount,
            currentPrice,
            leverage);

        return positionSize;
    }

    /// <inheritdoc/>
    public decimal CalculateFixedPercent(
        decimal accountBalance,
        decimal percentOfAccount,
        decimal currentPrice,
        decimal leverage = 1.0m)
    {
        if (accountBalance <= 0)
        {
            throw new ArgumentException("Account balance must be positive", nameof(accountBalance));
        }

        if (percentOfAccount <= 0 || percentOfAccount > 100)
        {
            throw new ArgumentException(
                "Percent of account must be between 0 and 100",
                nameof(percentOfAccount));
        }

        if (currentPrice <= 0)
        {
            throw new ArgumentException("Current price must be positive", nameof(currentPrice));
        }

        if (leverage < 1.0m)
        {
            throw new ArgumentException("Leverage must be at least 1.0", nameof(leverage));
        }

        var amountToRisk = accountBalance * (percentOfAccount / 100m);
        var positionSize = (amountToRisk * leverage) / currentPrice;

        _logger.LogDebug(
            "Fixed percent position size: {PositionSize:F4} units ({Percent}% of ${Balance:N2} = ${Amount:N2} @ ${Price:F2}, leverage {Leverage}x)",
            positionSize,
            percentOfAccount,
            accountBalance,
            amountToRisk,
            currentPrice,
            leverage);

        return positionSize;
    }

    /// <inheritdoc/>
    public decimal CalculateRiskBased(
        decimal accountBalance,
        decimal riskPercent,
        decimal entryPrice,
        decimal stopLossPrice,
        decimal leverage = 1.0m)
    {
        if (accountBalance <= 0)
        {
            throw new ArgumentException("Account balance must be positive", nameof(accountBalance));
        }

        if (riskPercent <= 0 || riskPercent > 100)
        {
            throw new ArgumentException(
                "Risk percent must be between 0 and 100",
                nameof(riskPercent));
        }

        if (entryPrice <= 0)
        {
            throw new ArgumentException("Entry price must be positive", nameof(entryPrice));
        }

        if (stopLossPrice <= 0)
        {
            throw new ArgumentException("Stop-loss price must be positive", nameof(stopLossPrice));
        }

        if (leverage < 1.0m)
        {
            throw new ArgumentException("Leverage must be at least 1.0", nameof(leverage));
        }

        var stopDistance = Math.Abs(entryPrice - stopLossPrice);
        if (stopDistance == 0)
        {
            throw new ArgumentException(
                "Entry price and stop-loss price cannot be the same",
                nameof(stopLossPrice));
        }

        var amountToRisk = accountBalance * (riskPercent / 100m);
        var positionSize = (amountToRisk * leverage) / stopDistance;

        _logger.LogDebug(
            "Risk-based position size: {PositionSize:F4} units (risk ${Risk:N2} = {RiskPercent}% of ${Balance:N2}, " +
            "entry ${Entry:F2}, stop ${Stop:F2}, distance ${Distance:F2}, leverage {Leverage}x)",
            positionSize,
            amountToRisk,
            riskPercent,
            accountBalance,
            entryPrice,
            stopLossPrice,
            stopDistance,
            leverage);

        return positionSize;
    }

    /// <inheritdoc/>
    public decimal CalculateKelly(
        decimal accountBalance,
        decimal winRate,
        decimal avgWin,
        decimal avgLoss,
        decimal currentPrice,
        decimal leverage = 1.0m,
        decimal kellyFraction = 0.25m)
    {
        if (accountBalance <= 0)
        {
            throw new ArgumentException("Account balance must be positive", nameof(accountBalance));
        }

        if (winRate < 0 || winRate > 1)
        {
            throw new ArgumentException("Win rate must be between 0 and 1", nameof(winRate));
        }

        if (avgWin <= 0)
        {
            throw new ArgumentException("Average win must be positive", nameof(avgWin));
        }

        if (avgLoss <= 0)
        {
            throw new ArgumentException("Average loss must be positive", nameof(avgLoss));
        }

        if (currentPrice <= 0)
        {
            throw new ArgumentException("Current price must be positive", nameof(currentPrice));
        }

        if (leverage < 1.0m)
        {
            throw new ArgumentException("Leverage must be at least 1.0", nameof(leverage));
        }

        if (kellyFraction <= 0 || kellyFraction > 1)
        {
            throw new ArgumentException(
                "Kelly fraction must be between 0 and 1",
                nameof(kellyFraction));
        }

        // Kelly Criterion formula: K% = W - ((1-W) / (Avg Win / Avg Loss))
        // Where W = win rate, R = win/loss ratio
        var winLossRatio = avgWin / avgLoss;
        var kellyPercent = winRate - ((1 - winRate) / winLossRatio);

        // Apply Kelly fraction (typically use 1/4 or 1/2 Kelly to reduce risk)
        kellyPercent *= kellyFraction;

        // Ensure Kelly doesn't suggest negative or excessive position
        kellyPercent = Math.Max(0, Math.Min(kellyPercent, 0.25m)); // Cap at 25% of account

        var amountToAllocate = accountBalance * kellyPercent;
        var positionSize = (amountToAllocate * leverage) / currentPrice;

        _logger.LogDebug(
            "Kelly Criterion position size: {PositionSize:F4} units (Kelly {KellyPercent:P2} x {Fraction} = {Allocation:P2} of ${Balance:N2}, " +
            "win rate {WinRate:P2}, avg win ${AvgWin:F2}, avg loss ${AvgLoss:F2}, leverage {Leverage}x)",
            positionSize,
            kellyPercent / kellyFraction,
            kellyFraction,
            kellyPercent,
            accountBalance,
            winRate,
            avgWin,
            avgLoss,
            leverage);

        return positionSize;
    }

    /// <inheritdoc/>
    public decimal CalculateVolatilityBased(
        decimal accountBalance,
        decimal riskPercent,
        decimal atr,
        decimal atrMultiplier,
        decimal currentPrice,
        decimal leverage = 1.0m)
    {
        if (accountBalance <= 0)
        {
            throw new ArgumentException("Account balance must be positive", nameof(accountBalance));
        }

        if (riskPercent <= 0 || riskPercent > 100)
        {
            throw new ArgumentException(
                "Risk percent must be between 0 and 100",
                nameof(riskPercent));
        }

        if (atr <= 0)
        {
            throw new ArgumentException("ATR must be positive", nameof(atr));
        }

        if (atrMultiplier <= 0)
        {
            throw new ArgumentException("ATR multiplier must be positive", nameof(atrMultiplier));
        }

        if (currentPrice <= 0)
        {
            throw new ArgumentException("Current price must be positive", nameof(currentPrice));
        }

        if (leverage < 1.0m)
        {
            throw new ArgumentException("Leverage must be at least 1.0", nameof(leverage));
        }

        var amountToRisk = accountBalance * (riskPercent / 100m);
        var volatilityAdjustedRisk = atr * atrMultiplier;
        var positionSize = (amountToRisk * leverage) / volatilityAdjustedRisk;

        _logger.LogDebug(
            "Volatility-based position size: {PositionSize:F4} units (risk ${Risk:N2} = {RiskPercent}% of ${Balance:N2}, " +
            "ATR ${Atr:F2} x {Multiplier} = ${AdjustedRisk:F2}, leverage {Leverage}x)",
            positionSize,
            amountToRisk,
            riskPercent,
            accountBalance,
            atr,
            atrMultiplier,
            volatilityAdjustedRisk,
            leverage);

        return positionSize;
    }
}
