// <copyright file="SpinnerEnums.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Models;

/// <summary>
/// Spinner size options.
/// </summary>
public enum SpinnerSize
{
    /// <summary>
    /// Small spinner (16x16px).
    /// </summary>
    Small,

    /// <summary>
    /// Medium spinner (24x24px).
    /// </summary>
    Medium,

    /// <summary>
    /// Large spinner (32x32px).
    /// </summary>
    Large,

    /// <summary>
    /// Extra large spinner (48x48px).
    /// </summary>
    ExtraLarge,
}

/// <summary>
/// Spinner color variants.
/// </summary>
public enum SpinnerColor
{
    /// <summary>
    /// Primary blue color.
    /// </summary>
    Primary,

    /// <summary>
    /// Success green color.
    /// </summary>
    Success,

    /// <summary>
    /// Warning yellow color.
    /// </summary>
    Warning,

    /// <summary>
    /// Danger red color.
    /// </summary>
    Danger,

    /// <summary>
    /// White color (for dark backgrounds).
    /// </summary>
    White,
}
