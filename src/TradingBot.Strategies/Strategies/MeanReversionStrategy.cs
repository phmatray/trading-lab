// <copyright file="MeanReversionStrategy.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Enums;
using TradingBot.Core.Models.MarketData;
using TradingBot.Core.Models.Trading;
using TradingBot.Strategies.Indicators;

namespace TradingBot.Strategies.Strategies;

/// <summary>
/// Mean reversion trading strategy using Bollinger Bands and RSI.
/// Generates buy signals when price touches lower band (oversold) with RSI confirmation.
/// Generates sell signals when price touches upper band (overbought) with RSI confirmation.
/// </summary>
public sealed class MeanReversionStrategy : BaseStrategy
{
    private readonly int _bollingerPeriod;
    private readonly double _bollingerStdDev;
    private readonly int _rsiPeriod;
    private readonly decimal _rsiOversold;
    private readonly decimal _rsiOverbought;
    private readonly decimal _confidenceThreshold;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeanReversionStrategy"/> class.
    /// </summary>
    /// <param name="name">Strategy name.</param>
    /// <param name="symbols">Symbols to trade.</param>
    /// <param name="timeframe">Timeframe for strategy.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="bollingerPeriod">Bollinger Bands period (default: 20).</param>
    /// <param name="bollingerStdDev">Bollinger Bands standard deviation multiplier (default: 2.0).</param>
    /// <param name="rsiPeriod">RSI period (default: 14).</param>
    /// <param name="rsiOversold">RSI oversold threshold (default: 30).</param>
    /// <param name="rsiOverbought">RSI overbought threshold (default: 70).</param>
    /// <param name="confidenceThreshold">Minimum confidence for signals (default: 0.6).</param>
    public MeanReversionStrategy(
        string name,
        IReadOnlyList<string> symbols,
        string timeframe,
        ILogger<MeanReversionStrategy> logger,
        int bollingerPeriod = 20,
        double bollingerStdDev = 2.0,
        int rsiPeriod = 14,
        decimal rsiOversold = 30m,
        decimal rsiOverbought = 70m,
        decimal confidenceThreshold = 0.6m)
        : base(name, symbols, timeframe, logger)
    {
        if (bollingerPeriod <= 0)
        {
            throw new ArgumentException("Bollinger period must be greater than zero", nameof(bollingerPeriod));
        }

        if (bollingerStdDev <= 0)
        {
            throw new ArgumentException("Bollinger standard deviation must be greater than zero", nameof(bollingerStdDev));
        }

        if (rsiPeriod <= 0)
        {
            throw new ArgumentException("RSI period must be greater than zero", nameof(rsiPeriod));
        }

        if (rsiOversold < 0 || rsiOversold > 100)
        {
            throw new ArgumentException("RSI oversold threshold must be between 0 and 100", nameof(rsiOversold));
        }

        if (rsiOverbought < 0 || rsiOverbought > 100)
        {
            throw new ArgumentException("RSI overbought threshold must be between 0 and 100", nameof(rsiOverbought));
        }

        if (rsiOversold >= rsiOverbought)
        {
            throw new ArgumentException("RSI oversold must be less than RSI overbought", nameof(rsiOversold));
        }

        if (confidenceThreshold < 0 || confidenceThreshold > 1)
        {
            throw new ArgumentException("Confidence threshold must be between 0 and 1", nameof(confidenceThreshold));
        }

        _bollingerPeriod = bollingerPeriod;
        _bollingerStdDev = bollingerStdDev;
        _rsiPeriod = rsiPeriod;
        _rsiOversold = rsiOversold;
        _rsiOverbought = rsiOverbought;
        _confidenceThreshold = confidenceThreshold;
    }

    /// <inheritdoc/>
    public override string Type => "MeanReversion";

    /// <inheritdoc/>
    public override async Task<Signal?> GenerateSignalAsync(
        string symbol,
        IReadOnlyList<Candle> data,
        CancellationToken cancellationToken = default)
    {
        // Validate we have enough data (need max of bollinger period and RSI period + 1)
        var requiredPeriod = Math.Max(_bollingerPeriod, _rsiPeriod + 1);
        ValidateDataSufficiency(data, requiredPeriod, symbol);

        try
        {
            // Calculate indicators
            var bollingerBands = IndicatorLibrary.CalculateBollingerBands(data, _bollingerPeriod, _bollingerStdDev);
            var rsi = IndicatorLibrary.CalculateRSI(data, _rsiPeriod);
            var currentPrice = data[^1].Close;

            // Check for oversold condition (price near/below lower band, RSI oversold)
            var distanceToLowerBand = currentPrice - bollingerBands.Lower;
            var distanceToUpperBand = bollingerBands.Upper - currentPrice;
            var bandWidth = bollingerBands.Upper - bollingerBands.Lower;
            var percentFromLower = bandWidth > 0 ? (distanceToLowerBand / bandWidth) * 100m : 0m;
            var percentFromUpper = bandWidth > 0 ? (distanceToUpperBand / bandWidth) * 100m : 0m;

            // Oversold: price within 10% of lower band and RSI below threshold
            var isOversold = percentFromLower <= 10m && rsi <= _rsiOversold;

            // Overbought: price within 10% of upper band and RSI above threshold
            var isOverbought = percentFromUpper <= 10m && rsi >= _rsiOverbought;

            if (!isOversold && !isOverbought)
            {
                return await Task.FromResult<Signal?>(null);
            }

            // Calculate confidence based on how extreme the conditions are
            decimal confidence;
            SignalType signalType;
            Dictionary<string, object> metadata;

            if (isOversold)
            {
                // Stronger oversold = higher confidence
                var rsiStrength = (_rsiOversold - rsi) / _rsiOversold; // 0 to 1
                var bandStrength = 1m - (percentFromLower / 10m); // 0 to 1
                confidence = 0.5m + ((rsiStrength + bandStrength) / 2m * 0.5m);
                signalType = SignalType.Buy;

                metadata = new Dictionary<string, object>
                {
                    ["RSI"] = rsi,
                    ["UpperBand"] = bollingerBands.Upper,
                    ["MiddleBand"] = bollingerBands.Middle,
                    ["LowerBand"] = bollingerBands.Lower,
                    ["PercentFromLower"] = percentFromLower,
                    ["Condition"] = "Oversold",
                    ["BollingerPeriod"] = _bollingerPeriod,
                    ["BollingerStdDev"] = _bollingerStdDev,
                    ["RSIPeriod"] = _rsiPeriod,
                };

                Logger.LogInformation(
                    "Oversold condition detected for {Symbol}: Price={Price:F2}, Lower Band={LowerBand:F2}, RSI={RSI:F2}",
                    symbol,
                    currentPrice,
                    bollingerBands.Lower,
                    rsi);
            }
            else
            {
                // Stronger overbought = higher confidence
                var rsiStrength = (rsi - _rsiOverbought) / (100m - _rsiOverbought); // 0 to 1
                var bandStrength = 1m - (percentFromUpper / 10m); // 0 to 1
                confidence = 0.5m + ((rsiStrength + bandStrength) / 2m * 0.5m);
                signalType = SignalType.Sell;

                metadata = new Dictionary<string, object>
                {
                    ["RSI"] = rsi,
                    ["UpperBand"] = bollingerBands.Upper,
                    ["MiddleBand"] = bollingerBands.Middle,
                    ["LowerBand"] = bollingerBands.Lower,
                    ["PercentFromUpper"] = percentFromUpper,
                    ["Condition"] = "Overbought",
                    ["BollingerPeriod"] = _bollingerPeriod,
                    ["BollingerStdDev"] = _bollingerStdDev,
                    ["RSIPeriod"] = _rsiPeriod,
                };

                Logger.LogInformation(
                    "Overbought condition detected for {Symbol}: Price={Price:F2}, Upper Band={UpperBand:F2}, RSI={RSI:F2}",
                    symbol,
                    currentPrice,
                    bollingerBands.Upper,
                    rsi);
            }

            // Clamp confidence to [0, 1]
            confidence = Math.Min(Math.Max(confidence, 0m), 1m);

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
            var signal = CreateSignal(
                symbol,
                signalType,
                confidence,
                currentPrice,
                metadata);

            return await Task.FromResult(signal);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating mean reversion signal for {Symbol}", symbol);
            throw;
        }
    }

    /// <inheritdoc/>
    public override Task<bool> ValidateParametersAsync(CancellationToken cancellationToken = default)
    {
        var isValid = _bollingerPeriod > 0 &&
                      _bollingerStdDev > 0 &&
                      _rsiPeriod > 0 &&
                      _rsiOversold >= 0 &&
                      _rsiOversold <= 100 &&
                      _rsiOverbought >= 0 &&
                      _rsiOverbought <= 100 &&
                      _rsiOversold < _rsiOverbought &&
                      _confidenceThreshold >= 0 &&
                      _confidenceThreshold <= 1;

        if (!isValid)
        {
            Logger.LogWarning(
                "Invalid parameters: BollingerPeriod={BollingerPeriod}, BollingerStdDev={BollingerStdDev}, " +
                "RSIPeriod={RSIPeriod}, RSIOversold={RSIOversold}, RSIOverbought={RSIOverbought}, " +
                "ConfidenceThreshold={ConfidenceThreshold}",
                _bollingerPeriod,
                _bollingerStdDev,
                _rsiPeriod,
                _rsiOversold,
                _rsiOverbought,
                _confidenceThreshold);
        }

        return Task.FromResult(isValid);
    }
}
