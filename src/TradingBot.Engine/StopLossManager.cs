// <copyright file="StopLossManager.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Enums;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Engine;

/// <summary>
/// Service for managing stop-loss and trailing stop orders.
/// </summary>
public sealed class StopLossManager : IStopLossManager
{
    private readonly ILogger<StopLossManager> _logger;
    private readonly IOrderExecutionService _orderService;
    private readonly IPortfolioManager _portfolioManager;
    private readonly Dictionary<Guid, TrailingStop> _trailingStops = new();
    private readonly Dictionary<Guid, Guid> _positionToOrderMap = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="StopLossManager"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="orderService">Order execution service.</param>
    /// <param name="portfolioManager">Portfolio manager.</param>
    public StopLossManager(
        ILogger<StopLossManager> logger,
        IOrderExecutionService orderService,
        IPortfolioManager portfolioManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _portfolioManager = portfolioManager ?? throw new ArgumentNullException(nameof(portfolioManager));
    }

    /// <inheritdoc/>
    public async Task<Guid> CreateStopLossAsync(
        Guid positionId,
        decimal stopPrice,
        CancellationToken cancellationToken = default)
    {
        if (stopPrice <= 0)
        {
            throw new ArgumentException("Stop price must be positive", nameof(stopPrice));
        }

        var position = await _portfolioManager.GetPositionAsync(positionId.ToString(), cancellationToken);
        if (position == null)
        {
            throw new InvalidOperationException($"Position {positionId} not found");
        }

        if (position.Side == OrderSide.Buy && stopPrice >= position.EntryPrice)
        {
            throw new ArgumentException("Stop price must be below entry price for long positions", nameof(stopPrice));
        }

        if (position.Side == OrderSide.Sell && stopPrice <= position.EntryPrice)
        {
            throw new ArgumentException("Stop price must be above entry price for short positions", nameof(stopPrice));
        }

        var stopOrder = new Order
        {
            Id = Guid.NewGuid(),
            Symbol = position.Symbol,
            Type = OrderType.StopLoss,
            Side = position.Side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy,
            Quantity = Math.Abs(position.Quantity),
            StopPrice = stopPrice,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            StrategyName = position.StrategyName,
        };

        var submittedOrder = await _orderService.SubmitOrderAsync(stopOrder, cancellationToken);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _positionToOrderMap[positionId] = submittedOrder.Id;
        }
        finally
        {
            _lock.Release();
        }

        _logger.LogInformation(
            "Created stop-loss order {OrderId} for position {PositionId} at ${StopPrice:F2}",
            submittedOrder.Id,
            positionId,
            stopPrice);

        return submittedOrder.Id;
    }

    /// <inheritdoc/>
    public async Task UpdateStopLossAsync(
        Guid stopLossOrderId,
        decimal newStopPrice,
        CancellationToken cancellationToken = default)
    {
        if (newStopPrice <= 0)
        {
            throw new ArgumentException("Stop price must be positive", nameof(newStopPrice));
        }

        var order = await _orderService.GetOrderAsync(stopLossOrderId, cancellationToken);
        if (order == null)
        {
            throw new InvalidOperationException($"Order {stopLossOrderId} not found");
        }

        if (order.Type != OrderType.StopLoss)
        {
            throw new InvalidOperationException("Order is not a stop-loss order");
        }

        await _orderService.CancelOrderAsync(stopLossOrderId, cancellationToken);

        await _lock.WaitAsync(cancellationToken);
        Guid positionId;
        try
        {
            var entry = _positionToOrderMap.FirstOrDefault(kvp => kvp.Value == stopLossOrderId);
            if (entry.Key == default)
            {
                throw new InvalidOperationException("Cannot find position for stop-loss order");
            }

            positionId = entry.Key;
        }
        finally
        {
            _lock.Release();
        }

        await CreateStopLossAsync(positionId, newStopPrice, cancellationToken);

        _logger.LogInformation(
            "Updated stop-loss order {OrderId} to ${NewStopPrice:F2}",
            stopLossOrderId,
            newStopPrice);
    }

    /// <inheritdoc/>
    public async Task<Guid> CreateTrailingStopAsync(
        Guid positionId,
        decimal trailingPercent,
        CancellationToken cancellationToken = default)
    {
        if (trailingPercent <= 0 || trailingPercent > 50)
        {
            throw new ArgumentException("Trailing percent must be between 0 and 50", nameof(trailingPercent));
        }

        var position = await _portfolioManager.GetPositionAsync(positionId.ToString(), cancellationToken);
        if (position == null)
        {
            throw new InvalidOperationException($"Position {positionId} not found");
        }

        var currentPrice = position.CurrentPrice;
        var isLong = position.Side == OrderSide.Buy;

        var initialStopPrice = isLong
            ? currentPrice * (1 - (trailingPercent / 100m))
            : currentPrice * (1 + (trailingPercent / 100m));

        var stopOrderId = await CreateStopLossAsync(positionId, initialStopPrice, cancellationToken);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var trailingStop = new TrailingStop
            {
                PositionId = positionId,
                StopOrderId = stopOrderId,
                TrailingPercent = trailingPercent,
                CurrentStopPrice = initialStopPrice,
                HighestPrice = isLong ? currentPrice : 0,
                LowestPrice = isLong ? 0 : currentPrice,
                IsLong = isLong,
            };

            _trailingStops[positionId] = trailingStop;

            _logger.LogInformation(
                "Created trailing stop for position {PositionId} with {TrailingPercent}% trail at ${StopPrice:F2}",
                positionId,
                trailingPercent,
                initialStopPrice);
        }
        finally
        {
            _lock.Release();
        }

        return stopOrderId;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateTrailingStopAsync(
        Guid positionId,
        decimal currentPrice,
        CancellationToken cancellationToken = default)
    {
        if (currentPrice <= 0)
        {
            throw new ArgumentException("Current price must be positive", nameof(currentPrice));
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_trailingStops.TryGetValue(positionId, out var trailingStop))
            {
                return false;
            }

            var updated = false;

            if (trailingStop.IsLong)
            {
                if (currentPrice > trailingStop.HighestPrice)
                {
                    trailingStop.HighestPrice = currentPrice;
                    var newStopPrice = currentPrice * (1 - (trailingStop.TrailingPercent / 100m));

                    if (newStopPrice > trailingStop.CurrentStopPrice)
                    {
                        trailingStop.CurrentStopPrice = newStopPrice;
                        trailingStop.LastUpdated = DateTime.UtcNow;
                        updated = true;

                        _logger.LogInformation(
                            "Trailing stop updated for position {PositionId}: new stop ${StopPrice:F2} (price: ${CurrentPrice:F2})",
                            positionId,
                            newStopPrice,
                            currentPrice);

                        await UpdateStopLossAsync(trailingStop.StopOrderId, newStopPrice, cancellationToken);
                    }
                }
            }
            else
            {
                if (currentPrice < trailingStop.LowestPrice || trailingStop.LowestPrice == 0)
                {
                    trailingStop.LowestPrice = currentPrice;
                    var newStopPrice = currentPrice * (1 + (trailingStop.TrailingPercent / 100m));

                    if (newStopPrice < trailingStop.CurrentStopPrice || trailingStop.CurrentStopPrice == 0)
                    {
                        trailingStop.CurrentStopPrice = newStopPrice;
                        trailingStop.LastUpdated = DateTime.UtcNow;
                        updated = true;

                        _logger.LogInformation(
                            "Trailing stop updated for position {PositionId}: new stop ${StopPrice:F2} (price: ${CurrentPrice:F2})",
                            positionId,
                            newStopPrice,
                            currentPrice);

                        await UpdateStopLossAsync(trailingStop.StopOrderId, newStopPrice, cancellationToken);
                    }
                }
            }

            return updated;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task RemoveStopLossAsync(
        Guid stopLossOrderId,
        CancellationToken cancellationToken = default)
    {
        await _orderService.CancelOrderAsync(stopLossOrderId, cancellationToken);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var trailingStop = _trailingStops.Values.FirstOrDefault(ts => ts.StopOrderId == stopLossOrderId);

            if (trailingStop != null)
            {
                _trailingStops.Remove(trailingStop.PositionId);
                _logger.LogInformation("Removed trailing stop for position {PositionId}", trailingStop.PositionId);
            }

            var mapEntry = _positionToOrderMap.FirstOrDefault(kvp => kvp.Value == stopLossOrderId);
            if (mapEntry.Key != default)
            {
                _positionToOrderMap.Remove(mapEntry.Key);
            }
        }
        finally
        {
            _lock.Release();
        }

        _logger.LogInformation("Removed stop-loss order {OrderId}", stopLossOrderId);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Guid>> CheckTriggeredStopsAsync(
        CancellationToken cancellationToken = default)
    {
        var triggeredOrders = new List<Guid>();

        var orders = await _orderService.GetOrdersAsync(
            symbol: null,
            status: OrderStatus.Pending,
            startDate: null,
            endDate: null,
            cancellationToken: cancellationToken);

        var stopOrders = orders.Where(o => o.Type == OrderType.StopLoss).ToList();

        foreach (var order in stopOrders)
        {
            await _lock.WaitAsync(cancellationToken);
            Guid? positionId;
            try
            {
                var entry = _positionToOrderMap.FirstOrDefault(kvp => kvp.Value == order.Id);
                positionId = entry.Key != default ? entry.Key : null;
            }
            finally
            {
                _lock.Release();
            }

            if (positionId == null)
            {
                continue;
            }

            var position = await _portfolioManager.GetPositionAsync(positionId.Value.ToString(), cancellationToken);
            if (position == null || !order.StopPrice.HasValue)
            {
                continue;
            }

            var currentPrice = position.CurrentPrice;

            var isTriggered = order.Side == OrderSide.Sell
                ? currentPrice <= order.StopPrice.Value
                : currentPrice >= order.StopPrice.Value;

            if (isTriggered)
            {
                triggeredOrders.Add(order.Id);
                _logger.LogWarning(
                    "Stop-loss triggered for {Symbol}: current price ${CurrentPrice:F2}, stop price ${StopPrice:F2}",
                    order.Symbol,
                    currentPrice,
                    order.StopPrice.Value);
            }
        }

        return triggeredOrders.AsReadOnly();
    }
}
