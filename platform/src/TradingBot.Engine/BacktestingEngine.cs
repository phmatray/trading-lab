// <copyright file="BacktestingEngine.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TradingBot.Core.Enums;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Backtest;
using TradingBot.Core.Models.MarketData;
using TradingBot.Core.Models.Portfolio;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Engine;

/// <summary>
/// Service for running strategy backtests.
/// </summary>
public sealed class BacktestingEngine : IBacktestingEngine
{
    private readonly ILogger<BacktestingEngine> _logger;
    private readonly IMarketDataService _marketDataService;
    private readonly List<BacktestResult> _results;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="BacktestingEngine"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="marketDataService">Market data service.</param>
    public BacktestingEngine(
        ILogger<BacktestingEngine> logger,
        IMarketDataService marketDataService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _marketDataService = marketDataService ?? throw new ArgumentNullException(nameof(marketDataService));
        _results = new List<BacktestResult>();
    }

    /// <inheritdoc/>
    public async Task<BacktestResult> RunBacktestAsync(
        BacktestConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting backtest {BacktestId} for strategy {Strategy} on {Symbol}",
            configuration.BacktestId,
            configuration.StrategyName,
            configuration.Symbol);

        try
        {
            // Load historical data
            var historicalData = await LoadHistoricalDataAsync(
                configuration.Symbol,
                configuration.StartDate,
                configuration.EndDate,
                cancellationToken);

            if (historicalData.Count == 0)
            {
                throw new InvalidOperationException("No historical data available for the specified period");
            }

            // Initialize simulation state
            var equity = configuration.InitialCapital;
            var cash = configuration.InitialCapital;
            var position = 0m;
            var positionCost = 0m;
            var trades = new List<Trade>();
            var equityCurve = new List<(DateTime, decimal)>();

            // Simulate trading day by day
            foreach (var candle in historicalData)
            {
                // Generate signal (simplified - in reality would use actual strategy)
                var signal = GenerateSimulatedSignal(candle, historicalData);

                // Execute trades based on signal
                if (signal == OrderSide.Buy && position == 0)
                {
                    // Open long position
                    var quantity = Math.Floor(cash * 0.95m / candle.Close); // Use 95% of cash
                    if (quantity > 0)
                    {
                        var cost = quantity * candle.Close;
                        var commission = configuration.EnableTransactionCosts
                            ? configuration.CommissionPerTrade
                            : 0m;

                        cash -= cost + commission;
                        position = quantity;
                        positionCost = cost;

                        _logger.LogDebug(
                            "Opened position: {Quantity} @ ${Price} on {Date}",
                            quantity,
                            candle.Close,
                            candle.Timestamp);
                    }
                }
                else if (signal == OrderSide.Sell && position > 0)
                {
                    // Close long position
                    var proceeds = position * candle.Close;
                    var commission = configuration.EnableTransactionCosts
                        ? configuration.CommissionPerTrade
                        : 0m;

                    cash += proceeds - commission;

                    // Record trade
                    var pnl = proceeds - positionCost - (commission * 2);
                    trades.Add(new Trade
                    {
                        Id = Guid.NewGuid(),
                        Symbol = configuration.Symbol,
                        Side = OrderSide.Buy,
                        Quantity = position,
                        EntryPrice = positionCost / position,
                        ExitPrice = candle.Close,
                        EntryTime = candle.Timestamp.AddDays(-5), // Simplified
                        ExitTime = candle.Timestamp,
                        Commission = commission * 2,
                        StrategyName = configuration.StrategyName,
                    });

                    _logger.LogDebug(
                        "Closed position: {Quantity} @ ${Price}, P&L: ${PnL}",
                        position,
                        candle.Close,
                        pnl);

                    position = 0;
                    positionCost = 0;
                }

                // Calculate current equity
                var positionValue = position * candle.Close;
                equity = cash + positionValue;
                equityCurve.Add((candle.Timestamp, equity));
            }

            // Close any remaining position at end
            if (position > 0)
            {
                var lastCandle = historicalData[^1];
                var proceeds = position * lastCandle.Close;
                cash += proceeds;
                equity = cash;

                trades.Add(new Trade
                {
                    Id = Guid.NewGuid(),
                    Symbol = configuration.Symbol,
                    Side = OrderSide.Buy,
                    Quantity = position,
                    EntryPrice = positionCost / position,
                    ExitPrice = lastCandle.Close,
                    EntryTime = lastCandle.Timestamp.AddDays(-5),
                    ExitTime = lastCandle.Timestamp,
                    Commission = configuration.CommissionPerTrade,
                    StrategyName = configuration.StrategyName,
                });
            }

            // Calculate performance metrics
            var performance = CalculatePerformanceMetrics(trades, equity, configuration.InitialCapital);

            stopwatch.Stop();

            var result = new BacktestResult
            {
                BacktestId = configuration.BacktestId,
                StrategyName = configuration.StrategyName,
                Symbol = configuration.Symbol,
                StartDate = configuration.StartDate,
                EndDate = configuration.EndDate,
                InitialCapital = configuration.InitialCapital,
                FinalEquity = equity,
                Trades = trades,
                EquityCurve = equityCurve,
                Performance = performance,
                Duration = stopwatch.Elapsed,
            };

            // Store result
            await _lock.WaitAsync(cancellationToken);
            try
            {
                _results.Add(result);
            }
            finally
            {
                _lock.Release();
            }

            _logger.LogInformation(
                "Backtest {BacktestId} completed: Return={Return:F2}%, Trades={Trades}, Duration={Duration}",
                result.BacktestId,
                result.TotalReturn,
                trades.Count,
                stopwatch.Elapsed);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backtest {BacktestId} failed", configuration.BacktestId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<BacktestResult?> GetBacktestResultAsync(
        string backtestId,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _results.FirstOrDefault(r => r.BacktestId == backtestId);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BacktestResult>> GetAllBacktestResultsAsync(
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _results.OrderByDescending(r => r.CreatedAt).ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<BacktestResult?> GetLatestBacktestResultAsync(
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _results.OrderByDescending(r => r.CreatedAt).FirstOrDefault();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<Candle>> LoadHistoricalDataAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        // In a real implementation, this would load actual historical data
        // For now, we'll generate simulated data
        var candles = new List<Candle>();
        var currentDate = startDate;
        var basePrice = 100m;
        var random = new Random(42); // Fixed seed for reproducibility

        while (currentDate <= endDate)
        {
            // Skip weekends
            if (currentDate.DayOfWeek != DayOfWeek.Saturday &&
                currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                // Generate random price movement
                var change = (decimal)((random.NextDouble() * 4) - 2); // -2% to +2%
                basePrice *= 1 + (change / 100m);

                var open = basePrice;
                var high = basePrice * (1 + ((decimal)random.NextDouble() * 0.02m));
                var low = basePrice * (1 - ((decimal)random.NextDouble() * 0.02m));
                var close = basePrice;

                candles.Add(new Candle
                {
                    Symbol = symbol,
                    Timestamp = currentDate,
                    Timeframe = "1d",
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = random.Next(1000000, 10000000),
                });
            }

            currentDate = currentDate.AddDays(1);
        }

        await Task.CompletedTask;
        return candles;
    }

    private OrderSide? GenerateSimulatedSignal(Candle currentCandle, List<Candle> historicalData)
    {
        // Simplified momentum strategy
        // Buy when price is above 20-day moving average
        // Sell when price is below 20-day moving average
        var index = historicalData.IndexOf(currentCandle);
        if (index < 20)
        {
            return null;
        }

        var ma20 = historicalData
            .Skip(index - 20)
            .Take(20)
            .Average(c => c.Close);

        if (currentCandle.Close > ma20)
        {
            return OrderSide.Buy;
        }

        if (currentCandle.Close < ma20)
        {
            return OrderSide.Sell;
        }

        return null;
    }

    private PerformanceMetrics CalculatePerformanceMetrics(
        List<Trade> trades,
        decimal finalEquity,
        decimal initialCapital)
    {
        var totalReturn = initialCapital > 0
            ? ((finalEquity - initialCapital) / initialCapital) * 100m
            : 0m;

        var winningTrades = trades.Count(t => t.RealizedPnL > 0);
        var losingTrades = trades.Count(t => t.RealizedPnL < 0);
        var totalTrades = trades.Count;

        var grossWins = winningTrades > 0
            ? trades.Where(t => t.RealizedPnL > 0).Sum(t => t.RealizedPnL)
            : 0m;

        var grossLosses = losingTrades > 0
            ? Math.Abs(trades.Where(t => t.RealizedPnL < 0).Sum(t => t.RealizedPnL))
            : 0m;

        return new PerformanceMetrics
        {
            TotalReturn = totalReturn,
            AnnualizedReturn = 0m, // TODO: Calculate based on time period
            SharpeRatio = 0m, // TODO: Calculate from returns
            SortinoRatio = 0m, // TODO: Calculate from returns
            CalmarRatio = 0m, // TODO: Calculate from returns
            MaxDrawdown = 0m, // TODO: Track historical drawdowns
            ProfitFactor = grossLosses > 0 ? grossWins / grossLosses : 0m,
            TotalTrades = totalTrades,
            WinningTrades = winningTrades,
            LosingTrades = losingTrades,
            AverageWin = winningTrades > 0
                ? trades.Where(t => t.RealizedPnL > 0).Average(t => t.RealizedPnL)
                : 0m,
            AverageLoss = losingTrades > 0
                ? trades.Where(t => t.RealizedPnL < 0).Average(t => t.RealizedPnL)
                : 0m,
        };
    }
}
