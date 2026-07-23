// <copyright file="StrategyParameterDto.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Models;

/// <summary>
/// Represents metadata for a single strategy parameter with type information and validation bounds.
/// </summary>
public class StrategyParameterDto
{
    /// <summary>
    /// Gets or sets the internal parameter name (e.g., "FastPeriod").
    /// </summary>
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display-friendly parameter name (e.g., "Fast MA Period").
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameter description for user guidance.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameter type: "int", "decimal", "bool", or "string".
    /// </summary>
    public string ParameterType { get; set; } = "int";

    /// <summary>
    /// Gets or sets the current parameter value.
    /// </summary>
    public object CurrentValue { get; set; } = 0;

    /// <summary>
    /// Gets or sets the minimum allowed value (for numeric types).
    /// </summary>
    public object? MinValue { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed value (for numeric types).
    /// </summary>
    public object? MaxValue { get; set; }
}
