// <copyright file="IWidget.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Spectre.Console;
using Spectre.Console.Rendering;

namespace TradingBot.Cli.Dashboard;

/// <summary>
/// Interface for dashboard widgets.
/// </summary>
public interface IWidget
{
    /// <summary>
    /// Gets the widget title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Renders the widget content asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Renderable content for the widget.</returns>
    Task<IRenderable> RenderAsync(CancellationToken cancellationToken = default);
}
