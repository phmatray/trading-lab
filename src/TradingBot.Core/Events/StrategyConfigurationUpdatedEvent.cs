// <copyright file="StrategyConfigurationUpdatedEvent.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.SharedKernel;

namespace TradingBot.Core.Events;

/// <summary>
/// Domain event raised when strategy configuration parameters are updated.
/// </summary>
public sealed class StrategyConfigurationUpdatedEvent : DomainEventBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyConfigurationUpdatedEvent"/> class.
    /// </summary>
    /// <param name="strategyId">The strategy identifier.</param>
    /// <param name="strategyName">The strategy name.</param>
    public StrategyConfigurationUpdatedEvent(Guid strategyId, string strategyName)
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
