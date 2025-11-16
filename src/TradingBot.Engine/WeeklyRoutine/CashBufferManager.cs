// <copyright file="CashBufferManager.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Enums;
using TradingBot.Core.Events;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Strategies;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Engine.WeeklyRoutine;

/// <summary>
/// Service for managing cash buffer adjustments to maintain target ratio range.
/// Ensures portfolio maintains healthy liquidity between MIN_CASH_RATIO and MAX_CASH_RATIO.
/// </summary>
public sealed class CashBufferManager : ICashBufferManager
{
    private readonly IWeeklyCashManagedStrategyRepository _strategyRepository;
    private readonly IMarketDataService _marketDataService;
    private readonly IPortfolioManager _portfolioManager;
    private readonly IOrderExecutionService _orderExecutionService;
    private readonly IRiskManager _riskManager;
    private readonly ILogger<CashBufferManager> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CashBufferManager"/> class.
    /// </summary>
    /// <param name="strategyRepository">Strategy repository.</param>
    /// <param name="marketDataService">Market data service.</param>
    /// <param name="portfolioManager">Portfolio manager.</param>
    /// <param name="orderExecutionService">Order execution service.</param>
    /// <param name="riskManager">Risk manager.</param>
    /// <param name="logger">Logger.</param>
    public CashBufferManager(
        IWeeklyCashManagedStrategyRepository strategyRepository,
        IMarketDataService marketDataService,
        IPortfolioManager portfolioManager,
        IOrderExecutionService orderExecutionService,
        IRiskManager riskManager,
        ILogger<CashBufferManager> logger)
    {
        _strategyRepository = strategyRepository;
        _marketDataService = marketDataService;
        _portfolioManager = portfolioManager;
        _orderExecutionService = orderExecutionService;
        _riskManager = riskManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<CashBufferAdjustmentResult> AdjustCashBufferAsync(
        Guid strategyId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Adjusting cash buffer for strategy {StrategyId}",
            strategyId);

        var strategy = await _strategyRepository.GetByIdAsync(strategyId, cancellationToken);
        if (strategy == null)
        {
            throw new InvalidOperationException($"Strategy {strategyId} not found");
        }

        var account = await _portfolioManager.GetAccountAsync(cancellationToken);
        var cashRatioBefore = account.Cash / account.Equity;

        var result = new CashBufferAdjustmentResult
        {
            CashRatioBefore = cashRatioBefore,
            CashRatioAfter = cashRatioBefore,
        };

        // Check if adjustment is needed
        if (cashRatioBefore >= strategy.MinCashRatio && cashRatioBefore <= strategy.MaxCashRatio)
        {
            _logger.LogDebug(
                "Cash ratio {CashRatio:P2} is within target range [{MinRatio:P2}, {MaxRatio:P2}]. No adjustment needed.",
                cashRatioBefore,
                strategy.MinCashRatio,
                strategy.MaxCashRatio);

            result.Reason = $"Cash ratio {cashRatioBefore:P2} is within target range";
            return result;
        }

        // Case 1: Cash ratio below minimum - need to sell to rebuild buffer
        if (cashRatioBefore < strategy.MinCashRatio)
        {
            _logger.LogWarning(
                "Cash ratio {CashRatio:P2} is below minimum {MinRatio:P2}. Selling to rebuild buffer.",
                cashRatioBefore,
                strategy.MinCashRatio);

            var position = await _portfolioManager.GetPositionAsync(strategy.EtpSymbol, cancellationToken);
            if (position == null || position.Quantity <= 0)
            {
                _logger.LogWarning(
                    "No position found for {EtpSymbol}. Cannot sell to rebuild cash buffer.",
                    strategy.EtpSymbol);

                result.Reason = "No position available to sell";
                return result;
            }

            // Sell WEEKLY_SELL_RATIO of position to rebuild buffer
            var sellQuantity = Math.Floor(position.Quantity * strategy.WeeklySellRatio);
            if (sellQuantity < 1)
            {
                _logger.LogWarning(
                    "Calculated sell quantity {SellQuantity} is less than 1 share. Skipping adjustment.",
                    sellQuantity);

                result.Reason = "Calculated sell quantity too small (< 1 share)";
                return result;
            }

            var quote = await _marketDataService.GetQuoteAsync(strategy.EtpSymbol, cancellationToken);
            var sellOrder = new Order
            {
                Id = Guid.NewGuid(),
                Symbol = strategy.EtpSymbol,
                Side = OrderSide.Sell,
                Type = OrderType.Market,
                Quantity = sellQuantity,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow,
                StrategyName = strategy.Name,
            };

            var executedOrder = await _orderExecutionService.SubmitOrderAsync(sellOrder, cancellationToken);

            result.Adjusted = true;
            result.Action = "Sell";
            result.OrderId = executedOrder.Id;
            result.AdjustmentAmount = -(sellQuantity * quote.Price); // Negative for sell
            result.CashRatioAfter = (account.Cash + (sellQuantity * quote.Price)) / account.Equity;
            result.Reason = $"Sold {sellQuantity} shares to rebuild cash buffer from {cashRatioBefore:P2} to minimum {strategy.MinCashRatio:P2}";

            _logger.LogInformation(
                "Cash buffer adjustment executed: Sold {SellQuantity} shares of {Symbol}. Cash ratio: {Before:P2} → {After:P2}",
                sellQuantity,
                strategy.EtpSymbol,
                cashRatioBefore,
                result.CashRatioAfter);

            // Record adjustment and raise domain event
            strategy.RecordCashBufferAdjustment(
                executedOrder.Id,
                "Sell",
                cashRatioBefore,
                result.CashRatioAfter);

            await _strategyRepository.UpdateAsync(strategy, cancellationToken);

            return result;
        }

        // Case 2: Cash ratio above maximum - need to buy if COIN > MA20 (bullish only)
        if (cashRatioBefore > strategy.MaxCashRatio)
        {
            _logger.LogInformation(
                "Cash ratio {CashRatio:P2} is above maximum {MaxRatio:P2}.",
                cashRatioBefore,
                strategy.MaxCashRatio);

            // Only buy if bullish (COIN > MA20)
            if (strategy.CurrentUnderlyingPrice == null || strategy.CurrentMA20 == null)
            {
                _logger.LogWarning(
                    "Current prices not available for strategy {StrategyId}. Cannot determine market direction.",
                    strategyId);

                result.Reason = "Prices not available to determine market direction";
                return result;
            }

            if (strategy.CurrentUnderlyingPrice <= strategy.CurrentMA20)
            {
                _logger.LogInformation(
                    "COIN {CurrentPrice:C} is not above MA20 {MA20:C}. Not buying excess cash (only buys when bullish).",
                    strategy.CurrentUnderlyingPrice,
                    strategy.CurrentMA20);

                result.Reason = $"Cash high ({cashRatioBefore:P2}) but market bearish (COIN {strategy.CurrentUnderlyingPrice:C} ≤ MA20 {strategy.CurrentMA20:C})";
                return result;
            }

            // Calculate excess cash to invest
            var excessCash = account.Cash - (strategy.MaxCashRatio * account.Equity);
            if (excessCash < 1)
            {
                _logger.LogWarning(
                    "Excess cash {ExcessCash:C} is too small to invest. Skipping adjustment.",
                    excessCash);

                result.Reason = "Excess cash amount too small (< $1)";
                return result;
            }

            // Buy with excess cash
            var quote = await _marketDataService.GetQuoteAsync(strategy.EtpSymbol, cancellationToken);
            var buyQuantity = Math.Floor(excessCash / quote.Price);

            if (buyQuantity < 1)
            {
                _logger.LogWarning(
                    "Calculated buy quantity {BuyQuantity} is less than 1 share. Skipping adjustment.",
                    buyQuantity);

                result.Reason = "Calculated buy quantity too small (< 1 share)";
                return result;
            }

            var buyOrder = new Order
            {
                Id = Guid.NewGuid(),
                Symbol = strategy.EtpSymbol,
                Side = OrderSide.Buy,
                Type = OrderType.Market,
                Quantity = buyQuantity,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow,
                StrategyName = strategy.Name,
            };

            var executedOrder = await _orderExecutionService.SubmitOrderAsync(buyOrder, cancellationToken);

            result.Adjusted = true;
            result.Action = "Buy";
            result.OrderId = executedOrder.Id;
            result.AdjustmentAmount = buyQuantity * quote.Price; // Positive for buy
            result.CashRatioAfter = (account.Cash - (buyQuantity * quote.Price)) / account.Equity;
            result.Reason = $"Bought {buyQuantity} shares to use excess cash. Cash ratio: {cashRatioBefore:P2} → target {strategy.MaxCashRatio:P2}";

            _logger.LogInformation(
                "Cash buffer adjustment executed: Bought {BuyQuantity} shares of {Symbol}. Cash ratio: {Before:P2} → {After:P2}",
                buyQuantity,
                strategy.EtpSymbol,
                cashRatioBefore,
                result.CashRatioAfter);

            // Record adjustment and raise domain event
            strategy.RecordCashBufferAdjustment(
                executedOrder.Id,
                "Buy",
                cashRatioBefore,
                result.CashRatioAfter);

            await _strategyRepository.UpdateAsync(strategy, cancellationToken);

            return result;
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<decimal> CalculateCurrentCashRatioAsync(
        Guid strategyId,
        CancellationToken cancellationToken = default)
    {
        var account = await _portfolioManager.GetAccountAsync(cancellationToken);
        return account.Cash / account.Equity;
    }

    /// <inheritdoc/>
    public async Task<decimal> CalculateTotalEquityAsync(
        Guid strategyId,
        CancellationToken cancellationToken = default)
    {
        var account = await _portfolioManager.GetAccountAsync(cancellationToken);
        return account.Equity;
    }

    /// <inheritdoc/>
    public async Task<bool> NeedsAdjustmentAsync(
        Guid strategyId,
        CancellationToken cancellationToken = default)
    {
        var strategy = await _strategyRepository.GetByIdAsync(strategyId, cancellationToken);
        if (strategy == null)
        {
            throw new InvalidOperationException($"Strategy {strategyId} not found");
        }

        var cashRatio = await CalculateCurrentCashRatioAsync(strategyId, cancellationToken);
        return !IsWithinBounds(cashRatio, strategy.MinCashRatio, strategy.MaxCashRatio);
    }

    /// <inheritdoc/>
    public async Task<Guid?> ExecuteAdjustmentAsync(
        Guid strategyId,
        CancellationToken cancellationToken = default)
    {
        var result = await AdjustCashBufferAsync(strategyId, cancellationToken);
        return result.OrderId;
    }

    /// <inheritdoc/>
    public async Task<decimal> CalculateAdjustmentAmountAsync(
        Guid strategyId,
        CancellationToken cancellationToken = default)
    {
        var strategy = await _strategyRepository.GetByIdAsync(strategyId, cancellationToken);
        if (strategy == null)
        {
            throw new InvalidOperationException($"Strategy {strategyId} not found");
        }

        var account = await _portfolioManager.GetAccountAsync(cancellationToken);
        var cashRatio = account.Cash / account.Equity;

        // If within bounds, no adjustment needed
        if (IsWithinBounds(cashRatio, strategy.MinCashRatio, strategy.MaxCashRatio))
        {
            return 0m;
        }

        // Calculate target (midpoint of min/max)
        var targetRatio = (strategy.MinCashRatio + strategy.MaxCashRatio) / 2m;
        var targetCash = targetRatio * account.Equity;
        var adjustmentAmount = targetCash - account.Cash;

        return adjustmentAmount;
    }

    /// <inheritdoc/>
    public bool IsWithinBounds(decimal cashRatio, decimal minCashRatio, decimal maxCashRatio)
    {
        return cashRatio >= minCashRatio && cashRatio <= maxCashRatio;
    }
}
