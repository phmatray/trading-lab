// <copyright file="ConfirmDialogVariant.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Components.Molecules;

/// <summary>
/// Defines the variant styles for the confirm dialog.
/// </summary>
public enum ConfirmDialogVariant
{
    /// <summary>
    /// Informational dialog (blue).
    /// </summary>
    Info,

    /// <summary>
    /// Warning dialog (yellow).
    /// </summary>
    Warning,

    /// <summary>
    /// Dangerous action dialog (red).
    /// </summary>
    Danger,
}
