// <copyright file="StrategyConfiguration.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Configuration;

/// <summary>
/// Represents a user-customized configuration for a trading strategy.
/// </summary>
public class StrategyConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this configuration.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the strategy this configuration applies to.
    /// Must match an IStrategy.Name from the registered strategies.
    /// </summary>
    public string StrategyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON-serialized dictionary of parameter key-value pairs.
    /// Example: {"FastPeriod": 12, "SlowPeriod": 26, "SignalPeriod": 9}.
    /// </summary>
    public string ParametersJson { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the timestamp when this configuration was last modified.
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
