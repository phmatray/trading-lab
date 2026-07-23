// <copyright file="BaseStrategy.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.MarketData;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Strategies.Base;

/// <summary>
/// Abstract base class for all trading strategies.
/// </summary>
public abstract class BaseStrategy : IStrategy
{
    private bool _isEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseStrategy"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="name">Strategy name.</param>
    /// <param name="symbols">Symbols to trade.</param>
    /// <param name="timeframe">Timeframe for the strategy.</param>
    protected BaseStrategy(
        ILogger logger,
        string name,
        IReadOnlyList<string> symbols,
        string timeframe)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Symbols = symbols ?? throw new ArgumentNullException(nameof(symbols));
        Timeframe = timeframe ?? throw new ArgumentNullException(nameof(timeframe));
        _isEnabled = false;
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
    public bool IsEnabled => _isEnabled;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger { get; }

    /// <inheritdoc/>
    public virtual Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Initializing strategy: {Name} ({Type})", Name, Type);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<Signal?> GenerateSignalAsync(
        string symbol,
        IReadOnlyList<Candle> data,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            Logger.LogDebug("Strategy {Name} is disabled, skipping signal generation", Name);
            return null;
        }

        if (!Symbols.Contains(symbol))
        {
            Logger.LogWarning("Symbol {Symbol} not configured for strategy {Name}", symbol, Name);
            return null;
        }

        try
        {
            return await GenerateSignalInternalAsync(symbol, data, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating signal for {Symbol} in strategy {Name}", symbol, Name);
            return null;
        }
    }

    /// <inheritdoc/>
    public abstract Task<bool> ValidateParametersAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public void Enable()
    {
        _isEnabled = true;
        Logger.LogInformation("Strategy {Name} enabled", Name);
    }

    /// <inheritdoc/>
    public void Disable()
    {
        _isEnabled = false;
        Logger.LogInformation("Strategy {Name} disabled", Name);
    }

    /// <summary>
    /// Internal method for generating signals. Must be implemented by derived classes.
    /// </summary>
    /// <param name="symbol">Trading symbol.</param>
    /// <param name="data">Historical candle data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Trading signal or null.</returns>
    protected abstract Task<Signal?> GenerateSignalInternalAsync(
        string symbol,
        IReadOnlyList<Candle> data,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates that there is sufficient data for the required period.
    /// </summary>
    /// <param name="data">Historical candle data.</param>
    /// <param name="requiredPeriod">Minimum number of candles required.</param>
    /// <param name="symbol">Trading symbol.</param>
    /// <exception cref="InvalidOperationException">Thrown when insufficient data.</exception>
    protected void ValidateDataSufficiency(
        IReadOnlyList<Candle> data,
        int requiredPeriod,
        string symbol)
    {
        if (data.Count < requiredPeriod)
        {
            Logger.LogWarning(
                "Insufficient data for {Symbol}: need {Required} candles, got {Actual}",
                symbol,
                requiredPeriod,
                data.Count);

            throw new InvalidOperationException(
                $"Insufficient data for {symbol}: need {requiredPeriod} candles, got {data.Count}");
        }
    }

    /// <summary>
    /// Creates a buy signal.
    /// </summary>
    /// <param name="symbol">Trading symbol.</param>
    /// <param name="price">Suggested entry price.</param>
    /// <param name="confidence">Signal confidence (0-1).</param>
    /// <param name="metadata">Additional metadata.</param>
    /// <returns>Buy signal.</returns>
    protected Signal CreateBuySignal(
        string symbol,
        decimal price,
        decimal confidence,
        Dictionary<string, object>? metadata = null)
    {
        return new Signal
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            Type = Core.Enums.SignalType.Buy,
            Timestamp = DateTime.UtcNow,
            StrategyName = Name,
            Confidence = confidence,
            SuggestedPrice = price,
            Metadata = metadata ?? new Dictionary<string, object>(),
        };
    }

    /// <summary>
    /// Creates a sell signal.
    /// </summary>
    /// <param name="symbol">Trading symbol.</param>
    /// <param name="price">Suggested exit price.</param>
    /// <param name="confidence">Signal confidence (0-1).</param>
    /// <param name="metadata">Additional metadata.</param>
    /// <returns>Sell signal.</returns>
    protected Signal CreateSellSignal(
        string symbol,
        decimal price,
        decimal confidence,
        Dictionary<string, object>? metadata = null)
    {
        return new Signal
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            Type = Core.Enums.SignalType.Sell,
            Timestamp = DateTime.UtcNow,
            StrategyName = Name,
            Confidence = confidence,
            SuggestedPrice = price,
            Metadata = metadata ?? new Dictionary<string, object>(),
        };
    }

    /// <summary>
    /// Creates a close signal.
    /// </summary>
    /// <param name="symbol">Trading symbol.</param>
    /// <param name="price">Suggested exit price.</param>
    /// <param name="confidence">Signal confidence (0-1).</param>
    /// <param name="metadata">Additional metadata.</param>
    /// <returns>Close signal.</returns>
    protected Signal CreateCloseSignal(
        string symbol,
        decimal price,
        decimal confidence,
        Dictionary<string, object>? metadata = null)
    {
        return new Signal
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            Type = Core.Enums.SignalType.Close,
            Timestamp = DateTime.UtcNow,
            StrategyName = Name,
            Confidence = confidence,
            SuggestedPrice = price,
            Metadata = metadata ?? new Dictionary<string, object>(),
        };
    }
}
