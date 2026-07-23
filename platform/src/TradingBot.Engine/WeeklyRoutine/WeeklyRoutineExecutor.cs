// <copyright file="WeeklyRoutineExecutor.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Enums;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Strategy;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Engine.WeeklyRoutine;

/// <summary>
/// Service for orchestrating weekly cash-managed strategy routine execution.
/// Coordinates buy/sell logic, cash buffer management, and state updates.
/// </summary>
public sealed class WeeklyRoutineExecutor : IWeeklyRoutineExecutor
{
    private readonly IWeeklyCashManagedStrategyRepository _strategyRepository;
    private readonly IMA20IndicatorService _ma20Service;
    private readonly IMarketDataService _marketDataService;
    private readonly IPortfolioManager _portfolioManager;
    private readonly IOrderExecutionService _orderExecutionService;
    private readonly IRiskManager _riskManager;
    private readonly ICashBufferManager _cashBufferManager;
    private readonly ILogger<WeeklyRoutineExecutor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeeklyRoutineExecutor"/> class.
    /// </summary>
    /// <param name="strategyRepository">Strategy repository.</param>
    /// <param name="ma20Service">MA20 indicator service.</param>
    /// <param name="marketDataService">Market data service.</param>
    /// <param name="portfolioManager">Portfolio manager.</param>
    /// <param name="orderExecutionService">Order execution service.</param>
    /// <param name="riskManager">Risk manager.</param>
    /// <param name="cashBufferManager">Cash buffer manager.</param>
    /// <param name="logger">Logger.</param>
    public WeeklyRoutineExecutor(
        IWeeklyCashManagedStrategyRepository strategyRepository,
        IMA20IndicatorService ma20Service,
        IMarketDataService marketDataService,
        IPortfolioManager portfolioManager,
        IOrderExecutionService orderExecutionService,
        IRiskManager riskManager,
        ICashBufferManager cashBufferManager,
        ILogger<WeeklyRoutineExecutor> logger)
    {
        _strategyRepository = strategyRepository;
        _ma20Service = ma20Service;
        _marketDataService = marketDataService;
        _portfolioManager = portfolioManager;
        _orderExecutionService = orderExecutionService;
        _riskManager = riskManager;
        _cashBufferManager = cashBufferManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<WeeklyRoutineResult> ExecuteWeeklyRoutineAsync(
        WeeklyCashManagedStrategy strategy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Executing weekly routine for strategy {StrategyId} - {StrategyName}",
            strategy.Id,
            strategy.Name);

        var result = new WeeklyRoutineResult
        {
            Notes = string.Empty,
        };

        try
        {
            // Get current account state
            var account = await _portfolioManager.GetAccountAsync(cancellationToken);
            var cashRatioBefore = account.Cash / account.Equity;

            _logger.LogDebug(
                "Account state before execution: Equity={Equity:C}, Cash={Cash:C}, CashRatio={CashRatio:P2}",
                account.Equity,
                account.Cash,
                cashRatioBefore);

            // Execute buy logic if conditions are met
            var shouldBuy = await ShouldExecuteBuyAsync(strategy.Id, cancellationToken);
            if (shouldBuy)
            {
                var buyOrderId = await ExecuteBuyOrderAsync(strategy, account, cancellationToken);
                if (buyOrderId.HasValue)
                {
                    result.BuyOrderId = buyOrderId.Value;
                    _logger.LogInformation(
                        "Buy order placed: {OrderId} for strategy {StrategyId}",
                        buyOrderId.Value,
                        strategy.Id);
                }
            }
            else
            {
                _logger.LogDebug(
                    "Buy conditions not met for strategy {StrategyId}. COIN={CurrentPrice}, MA20={MA20}, CashRatio={CashRatio:P2}",
                    strategy.Id,
                    strategy.CurrentUnderlyingPrice,
                    strategy.CurrentMA20,
                    cashRatioBefore);
            }

            // T079: Execute sell logic if conditions are met
            var shouldSell = await ShouldExecuteSellAsync(strategy.Id, cancellationToken);
            if (shouldSell)
            {
                var sellOrderId = await ExecuteSellOrderAsync(strategy, account, cancellationToken);
                if (sellOrderId.HasValue)
                {
                    result.SellOrderId = sellOrderId.Value;
                    _logger.LogInformation(
                        "Sell order placed: {OrderId} for strategy {StrategyId}",
                        sellOrderId.Value,
                        strategy.Id);
                }
            }
            else
            {
                _logger.LogDebug(
                    "Sell conditions not met for strategy {StrategyId}. DaysBelowMA20={DaysBelowMA20}",
                    strategy.Id,
                    strategy.DaysBelowMA20);
            }

            // T099: Execute cash buffer adjustment after primary buy/sell logic
            _logger.LogDebug(
                "Checking cash buffer adjustment for strategy {StrategyId}",
                strategy.Id);

            var cashBufferAdjustment = await _cashBufferManager.AdjustCashBufferAsync(
                strategy.Id,
                cancellationToken);

            if (cashBufferAdjustment.Adjusted)
            {
                _logger.LogInformation(
                    "Cash buffer adjusted for strategy {StrategyId}: {Action} order {OrderId}. " +
                    "Cash ratio: {Before:P2} → {After:P2}. Reason: {Reason}",
                    strategy.Id,
                    cashBufferAdjustment.Action,
                    cashBufferAdjustment.OrderId,
                    cashBufferAdjustment.CashRatioBefore,
                    cashBufferAdjustment.CashRatioAfter,
                    cashBufferAdjustment.Reason);

                result.CashBufferOrderId = cashBufferAdjustment.OrderId;
            }
            else
            {
                _logger.LogDebug(
                    "No cash buffer adjustment needed for strategy {StrategyId}. Reason: {Reason}",
                    strategy.Id,
                    cashBufferAdjustment.Reason);
            }

            // Get updated account state
            var accountAfter = await _portfolioManager.GetAccountAsync(cancellationToken);
            result.CashRatioAfter = accountAfter.Cash / accountAfter.Equity;

            // Update strategy last execution timestamp
            strategy.LastExecutionTimestamp = DateTime.UtcNow;
            await _strategyRepository.UpdateAsync(strategy, cancellationToken);
            await _strategyRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Weekly routine completed for strategy {StrategyId}. Cash ratio: {Before:P2} → {After:P2}",
                strategy.Id,
                cashRatioBefore,
                result.CashRatioAfter);

            result.Notes = $"Execution completed. Buy order: {(result.BuyOrderId.HasValue ? "placed" : "not placed")}";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error executing weekly routine for strategy {StrategyId}",
                strategy.Id);

            result.Notes = $"Execution failed: {ex.Message}";
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task ExecuteDailyRoutineAsync(
        WeeklyCashManagedStrategy strategy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Executing daily routine for strategy {StrategyId} - {StrategyName}",
            strategy.Id,
            strategy.Name);

        try
        {
            // T062: Fetch current prices for underlying and ETP
            var underlyingPrice = await GetCurrentPriceAsync(strategy.UnderlyingSymbol, cancellationToken);
            var etpPrice = await GetCurrentPriceAsync(strategy.EtpSymbol, cancellationToken);

            if (underlyingPrice == null || etpPrice == null)
            {
                _logger.LogWarning(
                    "Unable to fetch prices for daily update: Underlying={UnderlyingPrice}, ETP={EtpPrice}",
                    underlyingPrice,
                    etpPrice);
                return;
            }

            _logger.LogDebug(
                "Fetched prices: {UnderlyingSymbol}={UnderlyingPrice:C}, {EtpSymbol}={EtpPrice:C}",
                strategy.UnderlyingSymbol,
                underlyingPrice.Value,
                strategy.EtpSymbol,
                etpPrice.Value);

            // T062: Calculate MA20 for the underlying asset
            var ma20 = await _ma20Service.CalculateMA20Async(strategy.UnderlyingSymbol, cancellationToken);

            if (ma20 == null)
            {
                _logger.LogWarning(
                    "Unable to calculate MA20 for {Symbol} - insufficient historical data",
                    strategy.UnderlyingSymbol);
                return;
            }

            _logger.LogDebug(
                "Calculated MA20 for {Symbol}: {MA20:C}",
                strategy.UnderlyingSymbol,
                ma20.Value);

            // T062: Update strategy with daily data (prices, MA20, days_below_ma20)
            // T064: This calls the domain method which updates all fields and raises events
            strategy.UpdateDailyData(underlyingPrice.Value, etpPrice.Value, ma20.Value);

            // Persist changes to database
            await _strategyRepository.UpdateAsync(strategy, cancellationToken);
            await _strategyRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Daily routine completed for strategy {StrategyId}: MA20={MA20:C}, Price={Price:C}, DaysBelowMA20={DaysBelowMA20}",
                strategy.Id,
                ma20.Value,
                underlyingPrice.Value,
                strategy.DaysBelowMA20);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error executing daily routine for strategy {StrategyId} - {StrategyName}",
                strategy.Id,
                strategy.Name);

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ShouldExecuteBuyAsync(Guid strategyId, CancellationToken cancellationToken = default)
    {
        var strategy = await _strategyRepository.GetByIdAsync(strategyId, cancellationToken);
        if (strategy == null)
        {
            return false;
        }

        // Check condition 1: COIN > MA20 (bullish)
        if (strategy.CurrentUnderlyingPrice == null || strategy.CurrentMA20 == null)
        {
            return false;
        }

        if (strategy.CurrentUnderlyingPrice.Value <= strategy.CurrentMA20.Value)
        {
            return false;
        }

        // Check condition 2: cash_ratio > MIN_CASH_RATIO
        var account = await _portfolioManager.GetAccountAsync(cancellationToken);
        var cashRatio = account.Cash / account.Equity;

        if (cashRatio <= strategy.MinCashRatio)
        {
            return false;
        }

        // Check condition 3: available cash > 0
        if (account.Cash <= 0)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> ShouldExecuteSellAsync(Guid strategyId, CancellationToken cancellationToken = default)
    {
        // T077: Check sell conditions
        var strategy = await _strategyRepository.GetByIdAsync(strategyId, cancellationToken);
        if (strategy == null)
        {
            return false;
        }

        // Condition 1: days_below_ma20 >= 2 (threshold met)
        if (strategy.DaysBelowMA20 < 2)
        {
            return false;
        }

        // Condition 2: position exists and has quantity > 0
        var position = await _portfolioManager.GetPositionAsync(strategy.EtpSymbol, cancellationToken);
        if (position == null || position.Quantity <= 0)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public async Task<decimal> CalculateBuyAmountAsync(Guid strategyId, CancellationToken cancellationToken = default)
    {
        var strategy = await _strategyRepository.GetByIdAsync(strategyId, cancellationToken);
        if (strategy == null)
        {
            return 0m;
        }

        var account = await _portfolioManager.GetAccountAsync(cancellationToken);

        // Calculate base buy amount: WEEKLY_BUY_RATIO × total_equity
        var baseBuyAmount = strategy.WeeklyBuyRatio * account.Equity;

        // Cap at available cash
        var buyAmount = Math.Min(baseBuyAmount, account.Cash);

        return buyAmount;
    }

    /// <inheritdoc/>
    public async Task<decimal> CalculateSellQuantityAsync(Guid strategyId, CancellationToken cancellationToken = default)
    {
        // T078: Calculate sell quantity as WEEKLY_SELL_RATIO × position_size
        var strategy = await _strategyRepository.GetByIdAsync(strategyId, cancellationToken);
        if (strategy == null)
        {
            return 0m;
        }

        var position = await _portfolioManager.GetPositionAsync(strategy.EtpSymbol, cancellationToken);
        if (position == null || position.Quantity <= 0)
        {
            return 0m;
        }

        // Calculate sell quantity: WEEKLY_SELL_RATIO × position_size
        var sellQuantity = Math.Floor(strategy.WeeklySellRatio * position.Quantity);

        return sellQuantity;
    }

    /// <summary>
    /// Executes a buy order with risk validation and order execution service integration.
    /// T053: Integration with OrderExecutionService.
    /// T054: Integration with RiskManager.
    /// </summary>
    /// <param name="strategy">The strategy to execute buy for.</param>
    /// <param name="account">Current account state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Order ID if buy executed, null otherwise.</returns>
    private async Task<Guid?> ExecuteBuyOrderAsync(
        WeeklyCashManagedStrategy strategy,
        Core.Models.Portfolio.Account account,
        CancellationToken cancellationToken)
    {
        // Calculate buy amount
        var buyAmount = await CalculateBuyAmountAsync(strategy.Id, cancellationToken);

        if (buyAmount <= 0)
        {
            _logger.LogDebug(
                "Buy amount is zero or negative for strategy {StrategyId}, skipping order",
                strategy.Id);
            return null;
        }

        _logger.LogInformation(
            "Preparing buy order for strategy {StrategyId}: Symbol={Symbol}, Amount={Amount:C}",
            strategy.Id,
            strategy.EtpSymbol,
            buyAmount);

        try
        {
            // Get current ETP price
            var currentPrice = await GetCurrentPriceAsync(strategy.EtpSymbol, cancellationToken);
            if (currentPrice == null)
            {
                _logger.LogWarning(
                    "Unable to get current price for {Symbol}, cannot execute buy order",
                    strategy.EtpSymbol);
                return null;
            }

            // Calculate quantity (shares to buy)
            var quantity = Math.Floor(buyAmount / currentPrice.Value);

            if (quantity <= 0)
            {
                _logger.LogWarning(
                    "Calculated quantity is zero for {Symbol} at price {Price:C}, buy amount {Amount:C}",
                    strategy.EtpSymbol,
                    currentPrice.Value,
                    buyAmount);
                return null;
            }

            // Create order entity
            var order = new Order
            {
                Id = Guid.NewGuid(),
                Symbol = strategy.EtpSymbol,
                Type = OrderType.Market,
                Side = OrderSide.Buy,
                Quantity = quantity,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                StrategyName = strategy.Name,
            };

            _logger.LogDebug(
                "Created buy order: {OrderId}, Symbol={Symbol}, Quantity={Quantity}, Price={Price:C}",
                order.Id,
                order.Symbol,
                quantity,
                currentPrice.Value);

            // T054: Check risk settings before submitting order
            var riskSettings = await _riskManager.GetRiskSettingsAsync(cancellationToken);
            var orderValue = quantity * currentPrice.Value;

            // Validate that order doesn't exceed position size limit
            var maxPositionValue = account.Equity * (riskSettings.MaxPositionSizePercent / 100m);

            if (orderValue > maxPositionValue)
            {
                _logger.LogWarning(
                    "Order value {OrderValue:C} exceeds max position size {MaxPositionSize:C} ({Percent}% of equity). Order rejected.",
                    orderValue,
                    maxPositionValue,
                    riskSettings.MaxPositionSizePercent);
                return null;
            }

            _logger.LogDebug(
                "Order {OrderId} passed risk validation (value: {OrderValue:C}, limit: {MaxPositionValue:C}, {Percent}% of equity)",
                order.Id,
                orderValue,
                maxPositionValue,
                riskSettings.MaxPositionSizePercent);

            // T053: Submit order via OrderExecutionService
            var submittedOrder = await _orderExecutionService.SubmitOrderAsync(order, cancellationToken);

            _logger.LogInformation(
                "Buy order submitted successfully: {OrderId}, Symbol={Symbol}, Quantity={Quantity}, Status={Status}",
                submittedOrder.Id,
                submittedOrder.Symbol,
                submittedOrder.Quantity,
                submittedOrder.Status);

            return submittedOrder.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error executing buy order for strategy {StrategyId}, Symbol={Symbol}",
                strategy.Id,
                strategy.EtpSymbol);

            // Don't throw - return null to indicate order was not placed
            // This allows the routine to continue with other operations
            return null;
        }
    }

    /// <summary>
    /// Executes a sell order with risk validation and order execution service integration.
    /// T080: Integration with OrderExecutionService for sell order execution.
    /// T081: Structured logging for sell decisions.
    /// </summary>
    /// <param name="strategy">The strategy to execute sell for.</param>
    /// <param name="account">Current account state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Order ID if sell executed, null otherwise.</returns>
    private async Task<Guid?> ExecuteSellOrderAsync(
        WeeklyCashManagedStrategy strategy,
        Core.Models.Portfolio.Account account,
        CancellationToken cancellationToken)
    {
        // Calculate sell quantity
        var sellQuantity = await CalculateSellQuantityAsync(strategy.Id, cancellationToken);

        if (sellQuantity <= 0)
        {
            _logger.LogDebug(
                "Sell quantity is zero or negative for strategy {StrategyId}, skipping order",
                strategy.Id);
            return null;
        }

        // T081: Structured logging for sell decision
        _logger.LogInformation(
            "Preparing sell order for strategy {StrategyId}: Symbol={Symbol}, Quantity={Quantity}, DaysBelowMA20={DaysBelowMA20}",
            strategy.Id,
            strategy.EtpSymbol,
            sellQuantity,
            strategy.DaysBelowMA20);

        try
        {
            // Get current ETP price
            var currentPrice = await GetCurrentPriceAsync(strategy.EtpSymbol, cancellationToken);
            if (currentPrice == null)
            {
                _logger.LogWarning(
                    "Unable to get current price for {Symbol}, cannot execute sell order",
                    strategy.EtpSymbol);
                return null;
            }

            // Create sell order entity
            var order = new Order
            {
                Id = Guid.NewGuid(),
                Symbol = strategy.EtpSymbol,
                Type = OrderType.Market,
                Side = OrderSide.Sell,
                Quantity = sellQuantity,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                StrategyName = strategy.Name,
            };

            _logger.LogDebug(
                "Created sell order: {OrderId}, Symbol={Symbol}, Quantity={Quantity}, Price={Price:C}",
                order.Id,
                order.Symbol,
                sellQuantity,
                currentPrice.Value);

            // T080: Submit order via OrderExecutionService
            var submittedOrder = await _orderExecutionService.SubmitOrderAsync(order, cancellationToken);

            _logger.LogInformation(
                "Sell order submitted successfully: {OrderId}, Symbol={Symbol}, Quantity={Quantity}, Status={Status}",
                submittedOrder.Id,
                submittedOrder.Symbol,
                submittedOrder.Quantity,
                submittedOrder.Status);

            return submittedOrder.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error executing sell order for strategy {StrategyId}, Symbol={Symbol}",
                strategy.Id,
                strategy.EtpSymbol);

            // Don't throw - return null to indicate order was not placed
            // This allows the routine to continue with other operations
            return null;
        }
    }

    /// <summary>
    /// Gets the current price for a symbol.
    /// </summary>
    /// <param name="symbol">The symbol to get price for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current price or null if unavailable.</returns>
    private async Task<decimal?> GetCurrentPriceAsync(string symbol, CancellationToken cancellationToken)
    {
        try
        {
            var quote = await _marketDataService.GetQuoteAsync(symbol, cancellationToken);
            return quote?.Price;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Error getting current price for {Symbol}",
                symbol);
            return null;
        }
    }
}
