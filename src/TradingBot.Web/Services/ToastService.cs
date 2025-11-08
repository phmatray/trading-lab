// <copyright file="ToastService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Services;

/// <summary>
/// Implementation of toast notification service.
/// </summary>
public sealed class ToastService : IToastService
{
    /// <inheritdoc/>
    public event EventHandler<ToastMessage>? OnToastAdded;

    /// <inheritdoc/>
    public void ShowSuccess(string message, string? title = null, int durationMs = 3000)
    {
        var toast = new ToastMessage
        {
            Type = ToastType.Success,
            Title = title ?? "Success",
            Message = message,
            DurationMs = durationMs,
        };

        OnToastAdded?.Invoke(this, toast);
    }

    /// <inheritdoc/>
    public void ShowError(string message, string? title = null, int durationMs = 5000)
    {
        var toast = new ToastMessage
        {
            Type = ToastType.Error,
            Title = title ?? "Error",
            Message = message,
            DurationMs = durationMs,
        };

        OnToastAdded?.Invoke(this, toast);
    }

    /// <inheritdoc/>
    public void ShowWarning(string message, string? title = null, int durationMs = 4000)
    {
        var toast = new ToastMessage
        {
            Type = ToastType.Warning,
            Title = title ?? "Warning",
            Message = message,
            DurationMs = durationMs,
        };

        OnToastAdded?.Invoke(this, toast);
    }

    /// <inheritdoc/>
    public void ShowInfo(string message, string? title = null, int durationMs = 3000)
    {
        var toast = new ToastMessage
        {
            Type = ToastType.Info,
            Title = title ?? "Info",
            Message = message,
            DurationMs = durationMs,
        };

        OnToastAdded?.Invoke(this, toast);
    }
}
