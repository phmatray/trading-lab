// <copyright file="BacktestingEngine.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TradingBot.Core.Enums;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Backtest;
using TradingBot.Core.Models.MarketData;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Analytics;

/// <summary>
/// Core backtesting engine for simulating strategy execution on historical data.
/// </summary>
public sealed class BacktestingEngine : IBacktestingEngine
{
    private readonly ILogger<BacktestingEngine> _logger;
    private readonly IMarketDataService _marketDataService;
    private readonly IHistoricalDataCache _cache;
    private readonly PerformanceCalculator _performanceCalculator;
    private readonly Dictionary<string, BacktestResult> _results = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BacktestingEngine"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="marketDataService">Market data service.</param>
    /// <param name="cache">Historical data cache.</param>
    public BacktestingEngine(
        ILogger<BacktestingEngine> logger,
        IMarketDataService marketDataService,
        IHistoricalDataCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _marketDataService = marketDataService ?? throw new ArgumentNullException(nameof(marketDataService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _performanceCalculator = new PerformanceCalculator();
    }

    /// <inheritdoc/>
    public async Task<BacktestResult> RunBacktestAsync(
        BacktestConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting backtest {BacktestId} for {Strategy} on {Symbol} from {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}",
            configuration.BacktestId,
            configuration.StrategyName,
            configuration.Symbol,
            configuration.StartDate,
            configuration.EndDate);

        var stopwatch = Stopwatch.StartNew();

        // Load historical data
        var historicalData = await LoadHistoricalDataAsync(
            configuration.Symbol,
            configuration.StartDate,
            configuration.EndDate,
            cancellationToken);

        if (historicalData.Count == 0)
        {
            throw new InvalidOperationException($"No historical data available for {configuration.Symbol}");
        }

        _logger.LogInformation("Loaded {Count} candles for backtesting", historicalData.Count);

        // Initialize simulation state
        var equity = configuration.InitialCapital;
        var trades = new List<Trade>();
        var equityCurve = new List<(DateTime Date, decimal Equity)>();
        var openPositions = new Dictionary<string, SimulatedPosition>();
        var costModel = new TransactionCostModel
        {
            CommissionPerTrade = configuration.CommissionPerTrade,
            SlippagePercent = configuration.SlippagePercent,
            Enabled = configuration.EnableTransactionCosts,
        };
        var costSimulator = new TransactionCostSimulator(costModel);

        // Simulate trading day by day
        for (int i = 0; i < historicalData.Count; i++)
        {
            var candle = historicalData[i];

            // Update open positions with current price
            UpdateOpenPositions(openPositions, candle.Close);

            // Calculate current equity
            var unrealizedPnL = openPositions.Values.Sum(p => p.UnrealizedPnL);
            var currentEquity = equity + unrealizedPnL;

            // Record equity point
            equityCurve.Add((candle.Timestamp, currentEquity));

            // In a real implementation, execute strategy logic here
            // For now, this is a placeholder - strategies would be injected

            // Check for stop-loss and take-profit triggers
            ProcessStopOrders(openPositions, candle, trades, ref equity, costSimulator);

            // Progress reporting
            if (i % 100 == 0 || i == historicalData.Count - 1)
            {
                var progress = (decimal)(i + 1) / historicalData.Count * 100m;
                _logger.LogDebug("Backtest progress: {Progress:F1}% ({Current}/{Total} candles)", progress, i + 1, historicalData.Count);
            }
        }

        // Close any remaining open positions at final price
        if (openPositions.Count > 0)
        {
            var finalCandle = historicalData[^1];
            CloseAllPositions(openPositions, finalCandle.Close, finalCandle.Timestamp, trades, ref equity, costSimulator);
        }

        stopwatch.Stop();

        // Calculate performance metrics
        var finalEquity = equity;
        var performanceMetrics = _performanceCalculator.CalculateMetrics(
            trades,
            configuration.InitialCapital,
            finalEquity,
            equityCurve);

        var result = new BacktestResult
        {
            BacktestId = configuration.BacktestId,
            StrategyName = configuration.StrategyName,
            Symbol = configuration.Symbol,
            StartDate = configuration.StartDate,
            EndDate = configuration.EndDate,
            InitialCapital = configuration.InitialCapital,
            FinalEquity = finalEquity,
            Trades = trades,
            EquityCurve = equityCurve,
            Performance = performanceMetrics,
            Duration = stopwatch.Elapsed,
        };

        // Store result
        _results[configuration.BacktestId] = result;

        _logger.LogInformation(
            "Backtest {BacktestId} completed in {Duration:F2}s. Return: {Return:F2}%, Trades: {Trades}, Win Rate: {WinRate:F1}%",
            configuration.BacktestId,
            stopwatch.Elapsed.TotalSeconds,
            result.TotalReturn,
            result.Trades.Count,
            performanceMetrics.WinRate);

        return result;
    }

    /// <inheritdoc/>
    public Task<BacktestResult?> GetBacktestResultAsync(
        string backtestId,
        CancellationToken cancellationToken = default)
    {
        _results.TryGetValue(backtestId, out var result);
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<BacktestResult>> GetAllBacktestResultsAsync(
        CancellationToken cancellationToken = default)
    {
        var results = _results.Values.ToList();
        return Task.FromResult<IReadOnlyList<BacktestResult>>(results);
    }

    /// <inheritdoc/>
    public Task<BacktestResult?> GetLatestBacktestResultAsync(
        CancellationToken cancellationToken = default)
    {
        var latest = _results.Values.OrderByDescending(r => r.CreatedAt).FirstOrDefault();
        return Task.FromResult(latest);
    }

    private async Task<IReadOnlyList<Candle>> LoadHistoricalDataAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        // Try to load from cache first
        var cachedData = await _cache.GetAsync(
            symbol,
            startDate,
            endDate,
            "1d",
            cancellationToken);

        if (cachedData != null && cachedData.Count > 0)
        {
            _logger.LogDebug("Loaded {Count} candles from cache", cachedData.Count);
            return cachedData;
        }

        // Load from market data service
        _logger.LogDebug("Loading historical data from market data service");
        var data = await _marketDataService.GetHistoricalDataAsync(
            symbol,
            startDate,
            endDate,
            "1d",
            cancellationToken);

        // Cache for future use
        if (data != null && data.Count > 0)
        {
            await _cache.SetAsync(symbol, startDate, endDate, "1d", data, cancellationToken);
            return data;
        }

        return Array.Empty<Candle>();
    }

    private static void UpdateOpenPositions(
        Dictionary<string, SimulatedPosition> positions,
        decimal currentPrice)
    {
        foreach (var position in positions.Values)
        {
            position.CurrentPrice = currentPrice;
        }
    }

    private static void ProcessStopOrders(
        Dictionary<string, SimulatedPosition> positions,
        Candle candle,
        List<Trade> trades,
        ref decimal equity,
        TransactionCostSimulator costSimulator)
    {
        var positionsToClose = new List<string>();

        foreach (var kvp in positions)
        {
            var position = kvp.Value;

            // Check stop-loss
            if (position.StopLoss.HasValue)
            {
                var stopTriggered = position.Side == OrderSide.Buy
                    ? candle.Low <= position.StopLoss.Value
                    : candle.High >= position.StopLoss.Value;

                if (stopTriggered)
                {
                    ClosePosition(position, position.StopLoss.Value, candle.Timestamp, trades, ref equity, costSimulator, "Stop-Loss");
                    positionsToClose.Add(kvp.Key);
                    continue;
                }
            }

            // Check take-profit
            if (position.TakeProfit.HasValue)
            {
                var takeProfitTriggered = position.Side == OrderSide.Buy
                    ? candle.High >= position.TakeProfit.Value
                    : candle.Low <= position.TakeProfit.Value;

                if (takeProfitTriggered)
                {
                    ClosePosition(position, position.TakeProfit.Value, candle.Timestamp, trades, ref equity, costSimulator, "Take-Profit");
                    positionsToClose.Add(kvp.Key);
                }
            }
        }

        foreach (var key in positionsToClose)
        {
            positions.Remove(key);
        }
    }

    private static void CloseAllPositions(
        Dictionary<string, SimulatedPosition> positions,
        decimal price,
        DateTime timestamp,
        List<Trade> trades,
        ref decimal equity,
        TransactionCostSimulator costSimulator)
    {
        foreach (var position in positions.Values)
        {
            ClosePosition(position, price, timestamp, trades, ref equity, costSimulator, "End of Backtest");
        }

        positions.Clear();
    }

    private static void ClosePosition(
        SimulatedPosition position,
        decimal exitPrice,
        DateTime exitTime,
        List<Trade> trades,
        ref decimal equity,
        TransactionCostSimulator costSimulator,
        string reason)
    {
        var exitOrder = new Order
        {
            Id = Guid.NewGuid(),
            Symbol = position.Symbol,
            Type = OrderType.Market,
            Side = position.Side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy,
            Quantity = position.Quantity,
            Status = OrderStatus.Filled,
            CreatedAt = exitTime,
            FilledAt = exitTime,
            StrategyName = position.StrategyName,
        };

        var commission = costSimulator.CalculateCommission(exitOrder);

        var trade = new Trade
        {
            Id = Guid.NewGuid(),
            Symbol = position.Symbol,
            Side = position.Side,
            Quantity = position.Quantity,
            EntryPrice = position.EntryPrice,
            EntryTime = position.EntryTime,
            ExitPrice = exitPrice,
            ExitTime = exitTime,
            Commission = commission,
            StrategyName = position.StrategyName,
        };

        // RealizedPnL is a computed property, so we use it after creation
        trades.Add(trade);
        equity += trade.RealizedPnL;
    }

    private sealed class SimulatedPosition
    {
        public required string Symbol { get; set; }

        public required OrderSide Side { get; set; }

        public required decimal Quantity { get; set; }

        public required decimal EntryPrice { get; set; }

        public required DateTime EntryTime { get; set; }

        public decimal CurrentPrice { get; set; }

        public decimal? StopLoss { get; set; }

        public decimal? TakeProfit { get; set; }

        public required string StrategyName { get; set; }

        public decimal UnrealizedPnL =>
            Side == OrderSide.Buy
                ? (CurrentPrice - EntryPrice) * Quantity
                : (EntryPrice - CurrentPrice) * Quantity;
    }
}
