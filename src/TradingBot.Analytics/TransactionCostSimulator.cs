// <copyright file="TransactionCostSimulator.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Enums;
using TradingBot.Core.Models.Backtest;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Analytics;

/// <summary>
/// Simulates realistic transaction costs for backtesting.
/// </summary>
public sealed class TransactionCostSimulator
{
    private readonly TransactionCostModel _costModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionCostSimulator"/> class.
    /// </summary>
    /// <param name="costModel">Transaction cost model.</param>
    public TransactionCostSimulator(TransactionCostModel costModel)
    {
        _costModel = costModel ?? throw new ArgumentNullException(nameof(costModel));
    }

    /// <summary>
    /// Calculates the total commission for an order.
    /// </summary>
    /// <param name="order">The order.</param>
    /// <returns>Total commission amount.</returns>
    public decimal CalculateCommission(Order order)
    {
        if (!_costModel.Enabled)
        {
            return 0m;
        }

        var perTrade = _costModel.CommissionPerTrade;
        var perShare = _costModel.CommissionPerShare * order.Quantity;

        return perTrade + perShare;
    }

    /// <summary>
    /// Calculates slippage for an order.
    /// </summary>
    /// <param name="order">The order.</param>
    /// <param name="executionPrice">The execution price.</param>
    /// <returns>Slippage amount.</returns>
    public decimal CalculateSlippage(Order order, decimal executionPrice)
    {
        if (!_costModel.Enabled)
        {
            return 0m;
        }

        var slippageAmount = executionPrice * order.Quantity * (_costModel.SlippagePercent / 100m);

        // Slippage is always against the trader
        return order.Side == OrderSide.Buy ? slippageAmount : -slippageAmount;
    }

    /// <summary>
    /// Calculates the bid-ask spread cost.
    /// </summary>
    /// <param name="order">The order.</param>
    /// <param name="midPrice">The mid price.</param>
    /// <returns>Spread cost.</returns>
    public decimal CalculateSpread(Order order, decimal midPrice)
    {
        if (!_costModel.Enabled)
        {
            return 0m;
        }

        var spreadAmount = midPrice * order.Quantity * (_costModel.SpreadPercent / 100m);

        // Spread cost is always a cost (half spread on each side)
        return spreadAmount / 2m;
    }

    /// <summary>
    /// Calculates the total transaction cost for an order.
    /// </summary>
    /// <param name="order">The order.</param>
    /// <param name="fillPrice">The fill price.</param>
    /// <returns>Total transaction cost.</returns>
    public decimal CalculateTotalCost(Order order, decimal fillPrice)
    {
        if (!_costModel.Enabled)
        {
            return 0m;
        }

        var commission = CalculateCommission(order);
        var slippage = Math.Abs(CalculateSlippage(order, fillPrice));
        var spread = CalculateSpread(order, fillPrice);

        return commission + slippage + spread;
    }

    /// <summary>
    /// Applies slippage to an execution price.
    /// </summary>
    /// <param name="order">The order.</param>
    /// <param name="basePrice">The base execution price.</param>
    /// <returns>Price after slippage.</returns>
    public decimal ApplySlippage(Order order, decimal basePrice)
    {
        if (!_costModel.Enabled)
        {
            return basePrice;
        }

        var slippagePercent = _costModel.SlippagePercent / 100m;

        // Buy orders: price goes up (worse for buyer)
        // Sell orders: price goes down (worse for seller)
        return order.Side == OrderSide.Buy
            ? basePrice * (1 + slippagePercent)
            : basePrice * (1 - slippagePercent);
    }

    /// <summary>
    /// Gets a breakdown of transaction costs.
    /// </summary>
    /// <param name="order">The order.</param>
    /// <param name="fillPrice">The fill price.</param>
    /// <returns>Cost breakdown.</returns>
    public TransactionCostBreakdown GetCostBreakdown(Order order, decimal fillPrice)
    {
        return new TransactionCostBreakdown
        {
            Commission = CalculateCommission(order),
            Slippage = Math.Abs(CalculateSlippage(order, fillPrice)),
            Spread = CalculateSpread(order, fillPrice),
            TotalCost = CalculateTotalCost(order, fillPrice),
        };
    }
}
