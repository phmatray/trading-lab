// <copyright file="MomentumStrategy.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Enums;
using TradingBot.Core.Models.MarketData;
using TradingBot.Core.Models.Trading;
using TradingBot.Strategies.Indicators;

namespace TradingBot.Strategies.Strategies;

/// <summary>
/// Momentum trading strategy using moving average crossovers.
/// Generates buy signals when fast MA crosses above slow MA.
/// Generates sell signals when fast MA crosses below slow MA.
/// </summary>
public sealed class MomentumStrategy : BaseStrategy
{
    private readonly int _fastPeriod;
    private readonly int _slowPeriod;
    private readonly decimal _confidenceThreshold;

    /// <summary>
    /// Initializes a new instance of the <see cref="MomentumStrategy"/> class.
    /// </summary>
    /// <param name="name">Strategy name.</param>
    /// <param name="symbols">Symbols to trade.</param>
    /// <param name="timeframe">Timeframe for strategy.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="fastPeriod">Fast moving average period (default: 10).</param>
    /// <param name="slowPeriod">Slow moving average period (default: 30).</param>
    /// <param name="confidenceThreshold">Minimum confidence for signals (default: 0.6).</param>
    public MomentumStrategy(
        string name,
        IReadOnlyList<string> symbols,
        string timeframe,
        ILogger<MomentumStrategy> logger,
        int fastPeriod = 10,
        int slowPeriod = 30,
        decimal confidenceThreshold = 0.6m)
        : base(name, symbols, timeframe, logger)
    {
        if (fastPeriod <= 0)
        {
            throw new ArgumentException("Fast period must be greater than zero", nameof(fastPeriod));
        }

        if (slowPeriod <= 0)
        {
            throw new ArgumentException("Slow period must be greater than zero", nameof(slowPeriod));
        }

        if (fastPeriod >= slowPeriod)
        {
            throw new ArgumentException("Fast period must be less than slow period", nameof(fastPeriod));
        }

        if (confidenceThreshold < 0 || confidenceThreshold > 1)
        {
            throw new ArgumentException("Confidence threshold must be between 0 and 1", nameof(confidenceThreshold));
        }

        _fastPeriod = fastPeriod;
        _slowPeriod = slowPeriod;
        _confidenceThreshold = confidenceThreshold;
    }

    /// <inheritdoc/>
    public override string Type => "Momentum";

    /// <inheritdoc/>
    public override async Task<Signal?> GenerateSignalAsync(
        string symbol,
        IReadOnlyList<Candle> data,
        CancellationToken cancellationToken = default)
    {
        // Validate we have enough data
        ValidateDataSufficiency(data, _slowPeriod + 1, symbol);

        try
        {
            // Calculate current moving averages
            var fastMA = IndicatorLibrary.CalculateSMA(data, _fastPeriod);
            var slowMA = IndicatorLibrary.CalculateSMA(data, _slowPeriod);

            // Calculate previous moving averages (for crossover detection)
            var previousData = data.Take(data.Count - 1).ToList();
            if (previousData.Count < _slowPeriod)
            {
                Logger.LogDebug("Insufficient data for previous MA calculation for {Symbol}", symbol);
                return await Task.FromResult<Signal?>(null);
            }

            var prevFastMA = IndicatorLibrary.CalculateSMA(previousData, _fastPeriod);
            var prevSlowMA = IndicatorLibrary.CalculateSMA(previousData, _slowPeriod);

            // Detect crossovers
            var bullishCrossover = prevFastMA <= prevSlowMA && fastMA > slowMA;
            var bearishCrossover = prevFastMA >= prevSlowMA && fastMA < slowMA;

            if (!bullishCrossover && !bearishCrossover)
            {
                return await Task.FromResult<Signal?>(null);
            }

            // Calculate confidence based on the strength of the crossover
            var maDifference = Math.Abs(fastMA - slowMA);
            var currentPrice = data[^1].Close;
            var percentDifference = (maDifference / currentPrice) * 100m;
            var confidence = Math.Min(0.5m + (percentDifference * 5m), 1.0m);

            // Only generate signal if confidence exceeds threshold
            if (confidence < _confidenceThreshold)
            {
                Logger.LogDebug(
                    "Signal confidence {Confidence:F2} below threshold {Threshold:F2} for {Symbol}",
                    confidence,
                    _confidenceThreshold,
                    symbol);
                return await Task.FromResult<Signal?>(null);
            }

            // Generate signal
            var signalType = bullishCrossover ? SignalType.Buy : SignalType.Sell;
            var metadata = new Dictionary<string, object>
            {
                ["FastMA"] = fastMA,
                ["SlowMA"] = slowMA,
                ["PrevFastMA"] = prevFastMA,
                ["PrevSlowMA"] = prevSlowMA,
                ["PercentDifference"] = percentDifference,
                ["FastPeriod"] = _fastPeriod,
                ["SlowPeriod"] = _slowPeriod,
            };

            var signal = CreateSignal(
                symbol,
                signalType,
                confidence,
                currentPrice,
                metadata);

            Logger.LogInformation(
                "Momentum {CrossoverType} detected for {Symbol}: Fast MA={FastMA:F2}, Slow MA={SlowMA:F2}",
                bullishCrossover ? "bullish crossover" : "bearish crossover",
                symbol,
                fastMA,
                slowMA);

            return await Task.FromResult(signal);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating momentum signal for {Symbol}", symbol);
            throw;
        }
    }

    /// <inheritdoc/>
    public override Task<bool> ValidateParametersAsync(CancellationToken cancellationToken = default)
    {
        var isValid = _fastPeriod > 0 &&
                      _slowPeriod > 0 &&
                      _fastPeriod < _slowPeriod &&
                      _confidenceThreshold >= 0 &&
                      _confidenceThreshold <= 1;

        if (!isValid)
        {
            Logger.LogWarning(
                "Invalid parameters: FastPeriod={FastPeriod}, SlowPeriod={SlowPeriod}, ConfidenceThreshold={ConfidenceThreshold}",
                _fastPeriod,
                _slowPeriod,
                _confidenceThreshold);
        }

        return Task.FromResult(isValid);
    }
}
