// <copyright file="BadgeEnums.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Models;

/// <summary>
/// Badge variant/color options.
/// </summary>
public enum BadgeVariant
{
    /// <summary>
    /// Default gray badge.
    /// </summary>
    Default,

    /// <summary>
    /// Primary blue badge (solid).
    /// </summary>
    Primary,

    /// <summary>
    /// Success green badge.
    /// </summary>
    Success,

    /// <summary>
    /// Error red badge.
    /// </summary>
    Error,

    /// <summary>
    /// Warning yellow badge.
    /// </summary>
    Warning,

    /// <summary>
    /// Info blue badge.
    /// </summary>
    Info,
}

/// <summary>
/// Badge size options.
/// </summary>
public enum BadgeSize
{
    /// <summary>
    /// Small badge.
    /// </summary>
    Small,

    /// <summary>
    /// Medium badge.
    /// </summary>
    Medium,

    /// <summary>
    /// Large badge.
    /// </summary>
    Large,
}
