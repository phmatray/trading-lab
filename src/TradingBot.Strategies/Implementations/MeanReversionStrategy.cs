// <copyright file="MeanReversionStrategy.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Models.MarketData;
using TradingBot.Core.Models.Trading;
using TradingBot.Strategies.Base;
using TradingBot.Strategies.Indicators;

namespace TradingBot.Strategies.Implementations;

/// <summary>
/// Mean reversion trading strategy using Bollinger Bands.
/// </summary>
public sealed class MeanReversionStrategy : BaseStrategy
{
    private readonly MeanReversionConfig _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeanReversionStrategy"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="config">Strategy configuration.</param>
    public MeanReversionStrategy(
        ILogger<MeanReversionStrategy> logger,
        MeanReversionConfig config)
        : base(logger, config.Name, config.Symbols, config.Timeframe)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc/>
    public override string Type => "MeanReversion";

    /// <inheritdoc/>
    public override Task<bool> ValidateParametersAsync(CancellationToken cancellationToken = default)
    {
        var isValid = _config.LookbackPeriod > 0 &&
                      _config.StdMultiplier > 0 &&
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
        try
        {
            ValidateDataSufficiency(data, _config.LookbackPeriod, symbol);
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult<Signal?>(null);
        }

        // Calculate Bollinger Bands
        var currentPrice = data[^1].Close;
        var (upperBand, middleBand, lowerBand) = IndicatorLibrary.CalculateBollingerBands(
            data,
            _config.LookbackPeriod,
            _config.StdMultiplier);

        Logger.LogDebug(
            "{Symbol}: Price={Price:F2}, Upper={Upper:F2}, Middle={Middle:F2}, Lower={Lower:F2}",
            symbol,
            currentPrice,
            upperBand,
            middleBand,
            lowerBand);

        // Calculate band width percentage
        var bandWidth = upperBand - lowerBand;
        var upperDistance = upperBand - currentPrice;
        var lowerDistance = currentPrice - lowerBand;

        // Buy signal: Price touches or breaks below lower band
        // This indicates oversold conditions and potential rebound
        if (currentPrice <= lowerBand)
        {
            var confidence = CalculateBuyConfidence(currentPrice, lowerBand, middleBand);

            Logger.LogInformation(
                "BUY signal for {Symbol}: Price={Price:F2} <= Lower Band={Lower:F2}, Confidence={Confidence:F2}",
                symbol,
                currentPrice,
                lowerBand,
                confidence);

            var metadata = new Dictionary<string, object>
            {
                ["upper_band"] = upperBand,
                ["middle_band"] = middleBand,
                ["lower_band"] = lowerBand,
                ["current_price"] = currentPrice,
                ["band_width"] = bandWidth,
                ["distance_to_lower"] = lowerDistance,
            };

            return Task.FromResult<Signal?>(CreateBuySignal(symbol, currentPrice, confidence, metadata));
        }

        // Sell signal: Price touches or breaks above upper band
        // This indicates overbought conditions and potential reversal
        if (currentPrice >= upperBand)
        {
            var confidence = CalculateSellConfidence(currentPrice, upperBand, middleBand);

            Logger.LogInformation(
                "SELL signal for {Symbol}: Price={Price:F2} >= Upper Band={Upper:F2}, Confidence={Confidence:F2}",
                symbol,
                currentPrice,
                upperBand,
                confidence);

            var metadata = new Dictionary<string, object>
            {
                ["upper_band"] = upperBand,
                ["middle_band"] = middleBand,
                ["lower_band"] = lowerBand,
                ["current_price"] = currentPrice,
                ["band_width"] = bandWidth,
                ["distance_to_upper"] = upperDistance,
            };

            return Task.FromResult<Signal?>(CreateSellSignal(symbol, currentPrice, confidence, metadata));
        }

        // Close signal: Price returns to mean (middle band)
        // Only if ExitAtMean is enabled
        if (_config.ExitAtMean)
        {
            var priceNearMean = Math.Abs(currentPrice - middleBand) / middleBand < 0.005m; // Within 0.5%

            if (priceNearMean)
            {
                Logger.LogInformation(
                    "CLOSE signal for {Symbol}: Price={Price:F2} near Mean={Mean:F2}",
                    symbol,
                    currentPrice,
                    middleBand);

                var metadata = new Dictionary<string, object>
                {
                    ["upper_band"] = upperBand,
                    ["middle_band"] = middleBand,
                    ["lower_band"] = lowerBand,
                    ["current_price"] = currentPrice,
                    ["reason"] = "price_returned_to_mean",
                };

                return Task.FromResult<Signal?>(CreateCloseSignal(symbol, currentPrice, 0.8m, metadata));
            }
        }

        // No signal
        return Task.FromResult<Signal?>(null);
    }

    /// <summary>
    /// Calculates buy signal confidence based on distance from lower band.
    /// </summary>
    /// <param name="currentPrice">Current price.</param>
    /// <param name="lowerBand">Lower Bollinger Band.</param>
    /// <param name="middleBand">Middle Bollinger Band (SMA).</param>
    /// <returns>Confidence value (0-1).</returns>
    private decimal CalculateBuyConfidence(decimal currentPrice, decimal lowerBand, decimal middleBand)
    {
        // Calculate how far below the lower band the price is
        // More extreme oversold = higher confidence
        var bandRange = middleBand - lowerBand;
        var distanceBelowBand = lowerBand - currentPrice;

        // Normalize to 0-1 range
        // If price is at the lower band, confidence = 0.6
        // If price is 50% below the band, confidence = 0.9
        var baseConfidence = 0.6m;
        var bonusConfidence = Math.Min(0.4m, (distanceBelowBand / bandRange) * 0.8m);

        var confidence = baseConfidence + bonusConfidence;

        return Math.Clamp(confidence, 0.1m, 1.0m);
    }

    /// <summary>
    /// Calculates sell signal confidence based on distance from upper band.
    /// </summary>
    /// <param name="currentPrice">Current price.</param>
    /// <param name="upperBand">Upper Bollinger Band.</param>
    /// <param name="middleBand">Middle Bollinger Band (SMA).</param>
    /// <returns>Confidence value (0-1).</returns>
    private decimal CalculateSellConfidence(decimal currentPrice, decimal upperBand, decimal middleBand)
    {
        // Calculate how far above the upper band the price is
        // More extreme overbought = higher confidence
        var bandRange = upperBand - middleBand;
        var distanceAboveBand = currentPrice - upperBand;

        // Normalize to 0-1 range
        var baseConfidence = 0.6m;
        var bonusConfidence = Math.Min(0.4m, (distanceAboveBand / bandRange) * 0.8m);

        var confidence = baseConfidence + bonusConfidence;

        return Math.Clamp(confidence, 0.1m, 1.0m);
    }
}
