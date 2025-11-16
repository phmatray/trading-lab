// <copyright file="StrategyDisabledEvent.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.SharedKernel;

namespace TradingBot.Core.Events;

/// <summary>
/// Domain event raised when a weekly cash-managed strategy is disabled.
/// </summary>
public sealed class StrategyDisabledEvent : DomainEventBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyDisabledEvent"/> class.
    /// </summary>
    /// <param name="strategyId">The strategy identifier.</param>
    /// <param name="strategyName">The strategy name.</param>
    public StrategyDisabledEvent(Guid strategyId, string strategyName)
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
