// Copyright (c) 2025 TradingBot. All rights reserved.

using Microsoft.Extensions.Logging;
using TradingBot.Core.Enums;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Engine;

/// <summary>
/// Service for executing and managing trading orders.
/// Simulates order execution for backtesting and paper trading.
/// </summary>
public class OrderExecutionService : IOrderExecutionService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMarketDataService _marketDataService;
    private readonly ILogger<OrderExecutionService> _logger;

    /// <summary>
    /// Commission per trade (default: $1.00).
    /// </summary>
    private readonly decimal _commissionPerTrade = 1.0m;

    /// <summary>
    /// Slippage percentage (default: 0.1% or 0.001).
    /// </summary>
    private readonly decimal _slippagePercent = 0.001m;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderExecutionService"/> class.
    /// </summary>
    /// <param name="orderRepository">Order repository.</param>
    /// <param name="marketDataService">Market data service.</param>
    /// <param name="logger">Logger.</param>
    public OrderExecutionService(
        IOrderRepository orderRepository,
        IMarketDataService marketDataService,
        ILogger<OrderExecutionService> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _marketDataService = marketDataService ?? throw new ArgumentNullException(nameof(marketDataService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public event EventHandler<Order>? OrderFilled;

    /// <inheritdoc/>
    public event EventHandler<Order>? OrderCancelled;

    /// <inheritdoc/>
    public event EventHandler<Order>? OrderRejected;

    /// <inheritdoc/>
    public async Task<Order> SubmitOrderAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        if (order == null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        try
        {
            _logger.LogInformation(
                "Submitting {Type} order for {Quantity} shares of {Symbol} at {Price:C}",
                order.Type,
                order.Quantity,
                order.Symbol,
                order.LimitPrice ?? 0);

            // Validate order
            var validationResult = ValidateOrder(order);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Order validation failed: {Reason}",
                    validationResult.Reason);

                order.Status = OrderStatus.Rejected;
                await _orderRepository.AddAsync(order, cancellationToken);

                OrderRejected?.Invoke(this, order);
                return order;
            }

            // Set order status to submitted
            order.Status = OrderStatus.Submitted;
            order.SubmittedAt = DateTime.UtcNow;

            // Persist order
            await _orderRepository.AddAsync(order, cancellationToken);

            _logger.LogInformation(
                "Order submitted successfully: {OrderId}",
                order.Id);

            // Simulate immediate execution for market orders
            if (order.Type == OrderType.Market)
            {
                await SimulateOrderExecutionAsync(order, cancellationToken);
            }

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error submitting order for {Symbol}",
                order.Symbol);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CancelOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", orderId);
                return false;
            }

            // Can only cancel pending or submitted orders
            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Submitted)
            {
                _logger.LogWarning(
                    "Cannot cancel order {OrderId} with status {Status}",
                    orderId,
                    order.Status);
                return false;
            }

            _logger.LogInformation("Cancelling order {OrderId}", orderId);

            order.Status = OrderStatus.Cancelled;

            await _orderRepository.UpdateAsync(order, cancellationToken);

            OrderCancelled?.Invoke(this, order);

            _logger.LogInformation("Order {OrderId} cancelled successfully", orderId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Order?> GetOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", orderId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Order>> GetOrdersAsync(
        string? symbol = null,
        OrderStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Start with all orders
            var orders = await _orderRepository.GetAllAsync(cancellationToken);

            // Apply filters
            var filtered = orders.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(symbol))
            {
                filtered = filtered.Where(o => o.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
            }

            if (status is not null)
            {
                filtered = filtered.Where(o => o.Status == status);
            }

            if (startDate.HasValue)
            {
                filtered = filtered.Where(o => o.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                filtered = filtered.Where(o => o.CreatedAt <= endDate.Value);
            }

            return filtered.ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Order>> GetOpenOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var openStatuses = new[]
            {
                OrderStatus.Pending,
                OrderStatus.Submitted,
                OrderStatus.PartiallyFilled,
            };

            var allOrders = await _orderRepository.GetAllAsync(cancellationToken);
            return allOrders
                .Where(o => openStatuses.Contains(o.Status))
                .ToList()
                .AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving open orders");
            throw;
        }
    }

    /// <summary>
    /// Validates an order before submission.
    /// </summary>
    /// <param name="order">Order to validate.</param>
    /// <returns>Validation result.</returns>
    private OrderValidationResult ValidateOrder(Order order)
    {
        // Check quantity
        if (order.Quantity <= 0)
        {
            return new OrderValidationResult(false, "Quantity must be greater than zero");
        }

        // Check symbol
        if (string.IsNullOrWhiteSpace(order.Symbol))
        {
            return new OrderValidationResult(false, "Symbol cannot be empty");
        }

        // Check limit price for limit orders
        if (order.Type == OrderType.Limit && order.LimitPrice <= 0)
        {
            return new OrderValidationResult(false, "Limit price must be specified for limit orders");
        }

        // Check stop price for stop orders
        if (order.Type == OrderType.StopLoss && order.StopPrice <= 0)
        {
            return new OrderValidationResult(false, "Stop price must be specified for stop-loss orders");
        }

        return new OrderValidationResult(true, string.Empty);
    }

    /// <summary>
    /// Simulates order execution (for paper trading/backtesting).
    /// </summary>
    /// <param name="order">Order to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SimulateOrderExecutionAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Simulating execution for order {OrderId}", order.Id);

            // Get current market price
            var quote = await _marketDataService.GetQuoteAsync(order.Symbol, cancellationToken);
            if (quote == null)
            {
                _logger.LogWarning(
                    "Cannot execute order {OrderId}: Market data unavailable for {Symbol}",
                    order.Id,
                    order.Symbol);
                return;
            }

            // Calculate fill price with slippage
            var fillPrice = CalculateFillPrice(order, quote.Price);

            // Calculate commission
            var commission = _commissionPerTrade;

            // Update order
            order.Status = OrderStatus.Filled;
            order.FilledAt = DateTime.UtcNow;
            order.FilledQuantity = order.Quantity;
            order.AverageFillPrice = fillPrice;
            order.Commission = commission;

            await _orderRepository.UpdateAsync(order, cancellationToken);

            _logger.LogInformation(
                "Order {OrderId} filled: {Quantity} @ {Price:C} (commission: {Commission:C})",
                order.Id,
                order.Quantity,
                fillPrice,
                commission);

            // Raise event
            OrderFilled?.Invoke(this, order);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error simulating order execution for {OrderId}",
                order.Id);
        }
    }

    /// <summary>
    /// Calculates fill price including slippage.
    /// </summary>
    /// <param name="order">Order being filled.</param>
    /// <param name="marketPrice">Current market price.</param>
    /// <returns>Fill price including slippage.</returns>
    private decimal CalculateFillPrice(Order order, decimal marketPrice)
    {
        // Calculate slippage
        var slippage = marketPrice * _slippagePercent;

        // Apply slippage based on order side
        if (order.Side == OrderSide.Buy)
        {
            return marketPrice + slippage; // Buying costs more with slippage
        }
        else if (order.Side == OrderSide.Sell)
        {
            return marketPrice - slippage; // Selling gets less with slippage
        }

        return marketPrice;
    }

    /// <summary>
    /// Represents an order validation result.
    /// </summary>
    private readonly struct OrderValidationResult
    {
        public OrderValidationResult(bool isValid, string reason)
        {
            IsValid = isValid;
            Reason = reason;
        }

        public bool IsValid { get; }

        public string Reason { get; }
    }
}
