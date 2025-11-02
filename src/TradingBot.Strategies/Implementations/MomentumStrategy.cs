// <copyright file="MomentumStrategy.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Models.MarketData;
using TradingBot.Core.Models.Trading;
using TradingBot.Strategies.Base;
using TradingBot.Strategies.Indicators;

namespace TradingBot.Strategies.Implementations;

/// <summary>
/// Momentum-based trading strategy using RSI, MACD, and SMA indicators.
/// </summary>
public sealed class MomentumStrategy : BaseStrategy
{
    private readonly MomentumConfig _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="MomentumStrategy"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="config">Strategy configuration.</param>
    public MomentumStrategy(
        ILogger<MomentumStrategy> logger,
        MomentumConfig config)
        : base(logger, config.Name, config.Symbols, config.Timeframe)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc/>
    public override string Type => "Momentum";

    /// <inheritdoc/>
    public override Task<bool> ValidateParametersAsync(CancellationToken cancellationToken = default)
    {
        var isValid = _config.RsiPeriod > 0 &&
                      _config.RsiOversold >= 0 &&
                      _config.RsiOversold <= 100 &&
                      _config.RsiOverbought >= 0 &&
                      _config.RsiOverbought <= 100 &&
                      _config.RsiOversold < _config.RsiOverbought &&
                      _config.MacdFast > 0 &&
                      _config.MacdSlow > _config.MacdFast &&
                      _config.MacdSignal > 0 &&
                      _config.SmaPeriod > 0 &&
                      _config.Symbols.Count > 0;

        if (!isValid)
        {
            Logger.LogError("Invalid parameters for strategy {Name}", Name);
        }

        return Task.FromResult(isValid);
    }

    /// <inheritdoc/>
    protected override Task<Signal?> GenerateSignalInternalAsync(
        string symbol,
        IReadOnlyList<Candle> data,
        CancellationToken cancellationToken)
    {
        // Calculate required period (maximum of all indicator periods)
        var requiredPeriod = Math.Max(_config.RsiPeriod, Math.Max(_config.MacdSlow + _config.MacdSignal, _config.SmaPeriod));

        try
        {
            ValidateDataSufficiency(data, requiredPeriod, symbol);
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult<Signal?>(null);
        }

        // Calculate indicators
        var currentPrice = data[^1].Close;
        var rsi = IndicatorLibrary.CalculateRSI(data, _config.RsiPeriod);
        var (macd, signal, histogram) = IndicatorLibrary.CalculateMACD(
            data,
            _config.MacdFast,
            _config.MacdSlow,
            _config.MacdSignal);
        var sma = IndicatorLibrary.CalculateSMA(data, _config.SmaPeriod);

        Logger.LogDebug(
            "{Symbol}: Price={Price:F2}, RSI={Rsi:F2}, MACD={Macd:F4}, Signal={Signal:F4}, SMA={Sma:F2}",
            symbol,
            currentPrice,
            rsi,
            macd,
            signal,
            sma);

        // Buy signal conditions:
        // 1. RSI is oversold (< threshold)
        // 2. MACD histogram is bullish (positive and increasing)
        // 3. Price is above SMA (uptrend confirmation)
        var isBullish = rsi < _config.RsiOversold &&
                        histogram > 0 &&
                        macd > signal &&
                        currentPrice > sma;

        if (isBullish)
        {
            var confidence = CalculateBuyConfidence(rsi, histogram, currentPrice, sma);

            Logger.LogInformation(
                "BUY signal for {Symbol}: RSI={Rsi:F2}, MACD Histogram={Histogram:F4}, Confidence={Confidence:F2}",
                symbol,
                rsi,
                histogram,
                confidence);

            var metadata = new Dictionary<string, object>
            {
                ["rsi"] = rsi,
                ["macd"] = macd,
                ["macd_signal"] = signal,
                ["macd_histogram"] = histogram,
                ["sma"] = sma,
                ["current_price"] = currentPrice,
            };

            return Task.FromResult<Signal?>(CreateBuySignal(symbol, currentPrice, confidence, metadata));
        }

        // Sell signal conditions:
        // 1. RSI is overbought (> threshold)
        // 2. MACD histogram is bearish (negative)
        // 3. Price is below SMA (downtrend confirmation)
        var isBearish = rsi > _config.RsiOverbought &&
                        histogram < 0 &&
                        macd < signal &&
                        currentPrice < sma;

        if (isBearish)
        {
            var confidence = CalculateSellConfidence(rsi, histogram, currentPrice, sma);

            Logger.LogInformation(
                "SELL signal for {Symbol}: RSI={Rsi:F2}, MACD Histogram={Histogram:F4}, Confidence={Confidence:F2}",
                symbol,
                rsi,
                histogram,
                confidence);

            var metadata = new Dictionary<string, object>
            {
                ["rsi"] = rsi,
                ["macd"] = macd,
                ["macd_signal"] = signal,
                ["macd_histogram"] = histogram,
                ["sma"] = sma,
                ["current_price"] = currentPrice,
            };

            return Task.FromResult<Signal?>(CreateSellSignal(symbol, currentPrice, confidence, metadata));
        }

        // No signal
        return Task.FromResult<Signal?>(null);
    }

    /// <summary>
    /// Calculates buy signal confidence based on indicator strength.
    /// </summary>
    /// <param name="rsi">RSI value.</param>
    /// <param name="histogram">MACD histogram value.</param>
    /// <param name="currentPrice">Current price.</param>
    /// <param name="sma">SMA value.</param>
    /// <returns>Confidence value (0-1).</returns>
    private decimal CalculateBuyConfidence(decimal rsi, decimal histogram, decimal currentPrice, decimal sma)
    {
        // RSI component: more oversold = higher confidence
        var rsiConfidence = (_config.RsiOversold - rsi) / _config.RsiOversold;

        // MACD component: stronger histogram = higher confidence
        var macdConfidence = Math.Min(1.0m, histogram / 1.0m); // Normalize histogram

        // Trend component: price further above SMA = higher confidence
        var trendConfidence = Math.Min(1.0m, (currentPrice - sma) / sma * 10m);

        // Weighted average
        var confidence = (rsiConfidence * 0.4m) + (macdConfidence * 0.3m) + (trendConfidence * 0.3m);

        return Math.Clamp(confidence, 0.1m, 1.0m);
    }

    /// <summary>
    /// Calculates sell signal confidence based on indicator strength.
    /// </summary>
    /// <param name="rsi">RSI value.</param>
    /// <param name="histogram">MACD histogram value.</param>
    /// <param name="currentPrice">Current price.</param>
    /// <param name="sma">SMA value.</param>
    /// <returns>Confidence value (0-1).</returns>
    private decimal CalculateSellConfidence(decimal rsi, decimal histogram, decimal currentPrice, decimal sma)
    {
        // RSI component: more overbought = higher confidence
        var rsiConfidence = (rsi - _config.RsiOverbought) / (100m - _config.RsiOverbought);

        // MACD component: stronger negative histogram = higher confidence
        var macdConfidence = Math.Min(1.0m, Math.Abs(histogram) / 1.0m);

        // Trend component: price further below SMA = higher confidence
        var trendConfidence = Math.Min(1.0m, (sma - currentPrice) / sma * 10m);

        // Weighted average
        var confidence = (rsiConfidence * 0.4m) + (macdConfidence * 0.3m) + (trendConfidence * 0.3m);

        return Math.Clamp(confidence, 0.1m, 1.0m);
    }
}
