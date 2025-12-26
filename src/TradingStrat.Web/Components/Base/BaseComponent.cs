using Microsoft.AspNetCore.Components;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;

namespace TradingStrat.Web.Components.Base;

/// <summary>
/// Base component with standardized error handling pattern.
/// All Blazor components should inherit from this to ensure consistent error handling.
/// </summary>
public abstract class BaseComponent : ComponentBase
{
    [Inject] protected NotificationService NotificationService { get; set; } = null!;

    /// <summary>
    /// Handles exceptions with user-friendly messages and logging.
    /// </summary>
    /// <param name="ex">The exception to handle</param>
    /// <param name="context">Context describing what operation failed (e.g., "Failed to fetch data")</param>
    protected async Task HandleErrorAsync(Exception ex, string context)
    {
        string userMessage = ex switch
        {
            ArgumentNullException argEx => $"Missing required value: {argEx.ParamName}",
            ArgumentException => $"Invalid input: {ex.Message}",
            InvalidOperationException => $"Operation failed: {ex.Message}",
            HttpRequestException => "Network error. Please check your connection and try again.",
            TimeoutException => "The operation timed out. Please try again.",
            UnauthorizedAccessException => "You don't have permission to perform this action.",
            _ => "An unexpected error occurred. Please try again."
        };

        // Show user-friendly error notification
        await NotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Error,
            context,
            userMessage
        );

        // Log detailed error for debugging (visible in browser console)
        Console.WriteLine($"[ERROR] {context}: {ex.GetType().Name} - {ex.Message}");
        Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");

        // Log inner exception if present
        if (ex.InnerException != null)
        {
            Console.WriteLine($"[ERROR] Inner exception: {ex.InnerException.Message}");
        }
    }

    /// <summary>
    /// Handles JavaScript interop exceptions specifically (common in Blazor).
    /// </summary>
    /// <param name="ex">The JS interop exception</param>
    /// <param name="jsMethod">The JavaScript method that failed</param>
    protected async Task HandleJsInteropErrorAsync(Exception ex, string jsMethod)
    {
        // JS interop errors are often transient (e.g., element not yet rendered)
        Console.WriteLine($"[JS INTEROP ERROR] Method '{jsMethod}' failed: {ex.Message}");

        // Only notify user for non-transient errors
        if (!IsTransientJsError(ex))
        {
            await NotificationService.AddNotificationAsync(
                NotificationType.System,
                NotificationSeverity.Warning,
                "UI Interaction Failed",
                $"Could not execute {jsMethod}. Try refreshing the page."
            );
        }
    }

    /// <summary>
    /// Determines if a JS error is likely transient (e.g., element not yet rendered).
    /// </summary>
    private static bool IsTransientJsError(Exception ex)
    {
        string message = ex.Message.ToLowerInvariant();
        return message.Contains("null") ||
               message.Contains("undefined") ||
               message.Contains("not found") ||
               message.Contains("disposed");
    }

    /// <summary>
    /// Logs a warning message without showing a notification to the user.
    /// Use for non-critical issues that don't require user action.
    /// </summary>
    protected void LogWarning(string context, string message)
    {
        Console.WriteLine($"[WARNING] {context}: {message}");
    }

    /// <summary>
    /// Logs an info message for debugging purposes.
    /// </summary>
    protected void LogInfo(string context, string message)
    {
        Console.WriteLine($"[INFO] {context}: {message}");
    }
}
