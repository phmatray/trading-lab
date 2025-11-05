// Copyright (c) 2025 TradingBot. All rights reserved.

using Microsoft.Extensions.Logging;
using TradingBot.Core.Enums;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Engine;

/// <summary>
/// Processes trading signals and converts them into orders.
/// </summary>
public class SignalProcessor : IDisposable
{
    private readonly IStrategyEngine _strategyEngine;
    private readonly IOrderExecutionService _orderExecutionService;
    private readonly IPortfolioManager _portfolioManager;
    private readonly IRiskManager _riskManager;
    private readonly ILogger<SignalProcessor> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalProcessor"/> class.
    /// </summary>
    /// <param name="strategyEngine">Strategy engine.</param>
    /// <param name="orderExecutionService">Order execution service.</param>
    /// <param name="portfolioManager">Portfolio manager.</param>
    /// <param name="riskManager">Risk manager.</param>
    /// <param name="logger">Logger.</param>
    public SignalProcessor(
        IStrategyEngine strategyEngine,
        IOrderExecutionService orderExecutionService,
        IPortfolioManager portfolioManager,
        IRiskManager riskManager,
        ILogger<SignalProcessor> logger)
    {
        _strategyEngine = strategyEngine ?? throw new ArgumentNullException(nameof(strategyEngine));
        _orderExecutionService = orderExecutionService ?? throw new ArgumentNullException(nameof(orderExecutionService));
        _portfolioManager = portfolioManager ?? throw new ArgumentNullException(nameof(portfolioManager));
        _riskManager = riskManager ?? throw new ArgumentNullException(nameof(riskManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Subscribe to signal events
        _strategyEngine.SignalGenerated += OnSignalGeneratedAsync;
    }

    /// <summary>
    /// Processes a trading signal and creates an order.
    /// </summary>
    /// <param name="signal">Signal to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessSignalAsync(Signal signal, CancellationToken cancellationToken)
    {
        if (signal == null)
        {
            throw new ArgumentNullException(nameof(signal));
        }

        try
        {
            // Get current account state
            var account = await _portfolioManager.GetAccountAsync(cancellationToken);

            // Get risk settings
            var riskSettings = await _riskManager.GetRiskSettingsAsync(cancellationToken);

            // Calculate position size based on risk settings
            var positionSize = CalculatePositionSize(
                signal,
                account.Equity,
                riskSettings.MaxPositionSizePercent);

            _logger.LogDebug(
                "Calculated position size: {PositionSize} shares for {Symbol}",
                positionSize,
                signal.Symbol);

            // Validate position size against risk limits
            var positionValue = positionSize * (signal.SuggestedPrice ?? 0);
            var isValidSize = await _riskManager.ValidatePositionSizeAsync(
                positionValue,
                account.Equity,
                cancellationToken);

            if (!isValidSize)
            {
                _logger.LogWarning(
                    "Signal rejected: Position size ${Value:N2} exceeds risk limits for {Symbol}",
                    positionValue,
                    signal.Symbol);
                return;
            }

            // Create order from signal
            var order = CreateOrderFromSignal(signal, positionSize);

            // Submit the order
            var submittedOrder = await _orderExecutionService.SubmitOrderAsync(order, cancellationToken);

            if (submittedOrder.Status == OrderStatus.Rejected)
            {
                _logger.LogWarning(
                    "Order rejected for {Symbol}: {Reason}",
                    signal.Symbol,
                    "Validation failed");
                return;
            }

            _logger.LogInformation(
                "Order submitted successfully: {OrderId} for {Symbol}",
                submittedOrder.Id,
                signal.Symbol);

            // Create stop-loss order if configured
            if (riskSettings.StopLossPercent > 0)
            {
                await CreateStopLossOrderAsync(
                    signal,
                    positionSize,
                    riskSettings.StopLossPercent,
                    cancellationToken);
            }

            // Create take-profit order if configured
            if (riskSettings.TakeProfitPercent > 0)
            {
                await CreateTakeProfitOrderAsync(
                    signal,
                    positionSize,
                    riskSettings.TakeProfitPercent,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process signal for {Symbol}",
                signal.Symbol);
            throw;
        }
    }

    /// <summary>
    /// Disposes resources used by the signal processor.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Unsubscribe from events
        _strategyEngine.SignalGenerated -= OnSignalGeneratedAsync;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Handles signal generated events from the strategy engine.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="signal">Generated signal.</param>
    private async void OnSignalGeneratedAsync(object? sender, Signal signal)
    {
        try
        {
            _logger.LogInformation(
                "Processing {SignalType} signal for {Symbol} from strategy {Strategy}",
                signal.Type,
                signal.Symbol,
                signal.StrategyName);

            await ProcessSignalAsync(signal, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing signal for {Symbol}",
                signal.Symbol);
        }
    }

    /// <summary>
    /// Calculates the position size based on signal and risk parameters.
    /// </summary>
    /// <param name="signal">Trading signal.</param>
    /// <param name="accountEquity">Current account equity.</param>
    /// <param name="maxPositionSizePercent">Maximum position size as percentage of equity.</param>
    /// <returns>Position size in shares.</returns>
    private decimal CalculatePositionSize(
        Signal signal,
        decimal accountEquity,
        decimal maxPositionSizePercent)
    {
        // Calculate maximum position value
        var maxPositionValue = accountEquity * (maxPositionSizePercent / 100m);

        // Use signal confidence to adjust position size (50% - 100% of max)
        var adjustedPositionValue = maxPositionValue * ((0.5m + signal.Confidence) / 2m);

        // Calculate number of shares
        var price = signal.SuggestedPrice ?? 1m;
        if (price <= 0)
        {
            _logger.LogWarning(
                "Invalid price for {Symbol}, using confidence-based default",
                signal.Symbol);
            price = 100m; // Default fallback
        }

        var positionSize = Math.Floor(adjustedPositionValue / price);

        return Math.Max(1m, positionSize); // At least 1 share
    }

    /// <summary>
    /// Creates an order from a trading signal.
    /// </summary>
    /// <param name="signal">Trading signal.</param>
    /// <param name="quantity">Order quantity.</param>
    /// <returns>Created order.</returns>
    private Order CreateOrderFromSignal(Signal signal, decimal quantity)
    {
        var orderSide = signal.Type switch
        {
            SignalType.Buy => OrderSide.Buy,
            SignalType.Sell => OrderSide.Sell,
            _ => throw new ArgumentException($"Unsupported signal type: {signal.Type}"),
        };

        return new Order
        {
            Id = Guid.NewGuid(),
            Symbol = signal.Symbol,
            Type = OrderType.Market, // Default to market order
            Side = orderSide,
            Quantity = quantity,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            StrategyName = signal.StrategyName,
            SignalId = signal.Id,
        };
    }

    /// <summary>
    /// Creates a stop-loss order for a position.
    /// </summary>
    /// <param name="signal">Original signal.</param>
    /// <param name="quantity">Position quantity.</param>
    /// <param name="stopLossPercent">Stop-loss percentage.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CreateStopLossOrderAsync(
        Signal signal,
        decimal quantity,
        decimal stopLossPercent,
        CancellationToken cancellationToken)
    {
        try
        {
            var entryPrice = signal.SuggestedPrice ?? 0;
            if (entryPrice <= 0)
            {
                _logger.LogWarning(
                    "Cannot create stop-loss: Invalid entry price for {Symbol}",
                    signal.Symbol);
                return;
            }

            // Calculate stop price based on order side
            var stopPrice = signal.Type == SignalType.Buy
                ? entryPrice * (1 - (stopLossPercent / 100m)) // Below entry for buy
                : entryPrice * (1 + (stopLossPercent / 100m)); // Above entry for sell

            var stopOrder = new Order
            {
                Id = Guid.NewGuid(),
                Symbol = signal.Symbol,
                Type = OrderType.StopLoss,
                Side = signal.Type == SignalType.Buy ? OrderSide.Sell : OrderSide.Buy,
                Quantity = quantity,
                StopPrice = stopPrice,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                StrategyName = signal.StrategyName + "-StopLoss",
                SignalId = signal.Id,
            };

            await _orderExecutionService.SubmitOrderAsync(stopOrder, cancellationToken);

            _logger.LogInformation(
                "Stop-loss order created for {Symbol} at {StopPrice:C}",
                signal.Symbol,
                stopPrice);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create stop-loss order for {Symbol}",
                signal.Symbol);
        }
    }

    /// <summary>
    /// Creates a take-profit order for a position.
    /// </summary>
    /// <param name="signal">Original signal.</param>
    /// <param name="quantity">Position quantity.</param>
    /// <param name="takeProfitPercent">Take-profit percentage.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CreateTakeProfitOrderAsync(
        Signal signal,
        decimal quantity,
        decimal takeProfitPercent,
        CancellationToken cancellationToken)
    {
        try
        {
            var entryPrice = signal.SuggestedPrice ?? 0;
            if (entryPrice <= 0)
            {
                _logger.LogWarning(
                    "Cannot create take-profit: Invalid entry price for {Symbol}",
                    signal.Symbol);
                return;
            }

            // Calculate limit price based on order side
            var limitPrice = signal.Type == SignalType.Buy
                ? entryPrice * (1 + (takeProfitPercent / 100m)) // Above entry for buy
                : entryPrice * (1 - (takeProfitPercent / 100m)); // Below entry for sell

            var takeProfitOrder = new Order
            {
                Id = Guid.NewGuid(),
                Symbol = signal.Symbol,
                Type = OrderType.Limit,
                Side = signal.Type == SignalType.Buy ? OrderSide.Sell : OrderSide.Buy,
                Quantity = quantity,
                LimitPrice = limitPrice,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                StrategyName = signal.StrategyName + "-TakeProfit",
                SignalId = signal.Id,
            };

            await _orderExecutionService.SubmitOrderAsync(takeProfitOrder, cancellationToken);

            _logger.LogInformation(
                "Take-profit order created for {Symbol} at {LimitPrice:C}",
                signal.Symbol,
                limitPrice);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create take-profit order for {Symbol}",
                signal.Symbol);
        }
    }
}
