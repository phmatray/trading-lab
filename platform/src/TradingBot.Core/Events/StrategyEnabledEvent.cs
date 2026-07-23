// <copyright file="StrategyEnabledEvent.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.SharedKernel;

namespace TradingBot.Core.Events;

/// <summary>
/// Domain event raised when a weekly cash-managed strategy is enabled.
/// </summary>
public sealed class StrategyEnabledEvent : DomainEventBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyEnabledEvent"/> class.
    /// </summary>
    /// <param name="strategyId">The strategy identifier.</param>
    /// <param name="strategyName">The strategy name.</param>
    public StrategyEnabledEvent(Guid strategyId, string strategyName)
    {
        StrategyId = strategyId;
        StrategyName = strategyName;
    }

    /// <summary>
    /// Gets the strategy identifier.
    /// </summary>
    public Guid StrategyId { get; }

    /// <summary>
    /// Gets the strategy name.
    /// </summary>
    public string StrategyName { get; }
}
