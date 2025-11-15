// <copyright file="PendingOrdersSpec.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Ardalis.Specification;
using TradingBot.Core.Enums;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Core.Specifications;

/// <summary>
/// Specification for querying pending orders.
/// Example of using Ardalis.Specification for common queries.
/// </summary>
public sealed class PendingOrdersSpec : Specification<Order>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PendingOrdersSpec"/> class.
    /// </summary>
    public PendingOrdersSpec()
    {
        Query.Where(o => o.Status == OrderStatus.Pending)
             .OrderBy(o => o.CreatedAt);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PendingOrdersSpec"/> class for a specific symbol.
    /// </summary>
    /// <param name="symbol">The trading symbol to filter by.</param>
    public PendingOrdersSpec(string symbol)
    {
        Query.Where(o => o.Status == OrderStatus.Pending && o.Symbol == symbol)
             .OrderBy(o => o.CreatedAt);
    }
}
