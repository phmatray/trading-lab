// <copyright file="IToastService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Services;

/// <summary>
/// Toast message types.
/// </summary>
public enum ToastType
{
    /// <summary>
    /// Success toast (green).
    /// </summary>
    Success,

    /// <summary>
    /// Error toast (red).
    /// </summary>
    Error,

    /// <summary>
    /// Warning toast (yellow).
    /// </summary>
    Warning,

    /// <summary>
    /// Info toast (blue).
    /// </summary>
    Info,
}

/// <summary>
/// Service for displaying toast notifications to the user.
/// </summary>
public interface IToastService
{
    /// <summary>
    /// Event raised when a toast notification is added.
    /// </summary>
    event EventHandler<ToastMessage>? OnToastAdded;

    /// <summary>
    /// Shows a success toast message.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">Optional title for the toast.</param>
    /// <param name="durationMs">Duration in milliseconds before auto-dismissal (default: 3000ms).</param>
    void ShowSuccess(string message, string? title = null, int durationMs = 3000);

    /// <summary>
    /// Shows an error toast message.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">Optional title for the toast.</param>
    /// <param name="durationMs">Duration in milliseconds before auto-dismissal (default: 5000ms).</param>
    void ShowError(string message, string? title = null, int durationMs = 5000);

    /// <summary>
    /// Shows a warning toast message.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">Optional title for the toast.</param>
    /// <param name="durationMs">Duration in milliseconds before auto-dismissal (default: 4000ms).</param>
    void ShowWarning(string message, string? title = null, int durationMs = 4000);

    /// <summary>
    /// Shows an info toast message.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">Optional title for the toast.</param>
    /// <param name="durationMs">Duration in milliseconds before auto-dismissal (default: 3000ms).</param>
    void ShowInfo(string message, string? title = null, int durationMs = 3000);
}

/// <summary>
/// Represents a toast notification message.
/// </summary>
public sealed class ToastMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for this toast.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the type of toast.
    /// </summary>
    public ToastType Type { get; set; }

    /// <summary>
    /// Gets or sets the optional title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Gets or sets the duration in milliseconds before auto-dismissal.
    /// </summary>
    public int DurationMs { get; set; } = 3000;

    /// <summary>
    /// Gets or sets the timestamp when the toast was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
