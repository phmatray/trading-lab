// <copyright file="UIStateService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Services;

/// <summary>
/// Manages transient UI state for the application.
/// State does NOT persist across sessions.
/// </summary>
public class UIStateService
{
    private bool _sidebarCollapsed = false;

    /// <summary>
    /// Occurs when UI state changes.
    /// </summary>
    public event Action? OnStateChanged;

    /// <summary>
    /// Gets or sets a value indicating whether the sidebar is collapsed.
    /// Note: This state does NOT persist across sessions per FR-003a.
    /// </summary>
    public bool SidebarCollapsed
    {
        get => _sidebarCollapsed;
        set
        {
            if (_sidebarCollapsed == value)
            {
                return;
            }

            _sidebarCollapsed = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Toggles the sidebar collapsed state.
    /// </summary>
    public void ToggleSidebar()
    {
        SidebarCollapsed = !SidebarCollapsed;
    }

    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }
}
