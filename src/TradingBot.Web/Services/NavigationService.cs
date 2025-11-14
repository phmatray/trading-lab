// <copyright file="NavigationService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace TradingBot.Web.Services;

/// <summary>
/// Service for managing navigation state and active route detection.
/// </summary>
public class NavigationService : IDisposable
{
    private readonly NavigationManager _navigationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationService"/> class.
    /// </summary>
    /// <param name="navigationManager">The navigation manager.</param>
    public NavigationService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
        _navigationManager.LocationChanged += OnLocationChanged;
    }

    /// <summary>
    /// Occurs when navigation changes.
    /// </summary>
    public event Action<string>? OnNavigationChanged;

    /// <summary>
    /// Determines if the specified href is the active route.
    /// </summary>
    /// <param name="href">The href to check.</param>
    /// <param name="exactMatch">If true, requires exact match. If false, matches if current path starts with href.</param>
    /// <returns>True if the href is active, false otherwise.</returns>
    public bool IsActive(string href, bool exactMatch = false)
    {
        var currentPath = new Uri(_navigationManager.Uri).AbsolutePath;

        return exactMatch
            ? currentPath.Equals(href, StringComparison.OrdinalIgnoreCase)
            : currentPath.StartsWith(href, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Navigates to the specified href.
    /// </summary>
    /// <param name="href">The href to navigate to.</param>
    /// <param name="forceLoad">If true, bypasses client-side routing and forces a full page reload.</param>
    public void Navigate(string href, bool forceLoad = false)
    {
        _navigationManager.NavigateTo(href, forceLoad);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _navigationManager.LocationChanged -= OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        OnNavigationChanged?.Invoke(e.Location);
    }
}
