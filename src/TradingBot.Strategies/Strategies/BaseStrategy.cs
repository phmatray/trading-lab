// <copyright file="BaseStrategy.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Enums;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.MarketData;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Strategies.Strategies;

/// <summary>
/// Base abstract class for all trading strategies.
/// </summary>
public abstract class BaseStrategy : IStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseStrategy"/> class.
    /// </summary>
    /// <param name="name">Strategy name.</param>
    /// <param name="symbols">Symbols to trade.</param>
    /// <param name="timeframe">Timeframe for strategy.</param>
    /// <param name="logger">Logger instance.</param>
    protected BaseStrategy(
        string name,
        IReadOnlyList<string> symbols,
        string timeframe,
        ILogger logger)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Symbols = symbols ?? throw new ArgumentNullException(nameof(symbols));
        Timeframe = timeframe ?? throw new ArgumentNullException(nameof(timeframe));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public abstract string Type { get; }

    /// <inheritdoc/>
    public IReadOnlyList<string> Symbols { get; }

    /// <inheritdoc/>
    public string Timeframe { get; }

    /// <inheritdoc/>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger { get; }

    /// <inheritdoc/>
    public virtual Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Initializing strategy {Name} ({Type})", Name, Type);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public abstract Task<Signal?> GenerateSignalAsync(
        string symbol,
        IReadOnlyList<Candle> data,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<bool> ValidateParametersAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public void Enable()
    {
        IsEnabled = true;
        Logger.LogInformation("Strategy {Name} enabled", Name);
    }

    /// <inheritdoc/>
    public void Disable()
    {
        IsEnabled = false;
        Logger.LogInformation("Strategy {Name} disabled", Name);
    }

    /// <summary>
    /// Validates that there is sufficient data for the required period.
    /// </summary>
    /// <param name="data">Historical candle data.</param>
    /// <param name="requiredPeriod">Required number of candles.</param>
    /// <param name="symbol">Symbol being validated.</param>
    /// <exception cref="ArgumentException">Thrown when insufficient data.</exception>
    protected void ValidateDataSufficiency(
        IReadOnlyList<Candle> data,
        int requiredPeriod,
        string symbol)
    {
        if (data == null || data.Count == 0)
        {
            throw new ArgumentException($"No data available for {symbol}", nameof(data));
        }

        if (data.Count < requiredPeriod)
        {
            throw new ArgumentException(
                $"Insufficient data for {symbol}: need {requiredPeriod} candles, got {data.Count}",
                nameof(data));
        }
    }

    /// <summary>
    /// Creates a signal with common metadata.
    /// </summary>
    /// <param name="symbol">Trading symbol.</param>
    /// <param name="signalType">Type of signal.</param>
    /// <param name="confidence">Signal confidence (0-1).</param>
    /// <param name="suggestedPrice">Suggested entry price.</param>
    /// <param name="additionalMetadata">Additional metadata.</param>
    /// <returns>Configured signal.</returns>
    protected Signal CreateSignal(
        string symbol,
        SignalType signalType,
        decimal confidence,
        decimal? suggestedPrice = null,
        Dictionary<string, object>? additionalMetadata = null)
    {
        var metadata = new Dictionary<string, object>
        {
            ["StrategyType"] = Type,
            ["Timeframe"] = Timeframe,
        };

        if (additionalMetadata != null)
        {
            foreach (var kvp in additionalMetadata)
            {
                metadata[kvp.Key] = kvp.Value;
            }
        }

        var signal = new Signal
        {
            Id = Guid.NewGuid(),
            StrategyName = Name,
            Symbol = symbol,
            Type = signalType,
            Timestamp = DateTime.UtcNow,
            Confidence = confidence,
            SuggestedPrice = suggestedPrice,
            Metadata = metadata,
        };

        Logger.LogInformation(
            "Generated {SignalType} signal for {Symbol} with confidence {Confidence:F2}",
            signalType,
            symbol,
            confidence);

        return signal;
    }
}
