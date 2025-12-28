using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

/// <summary>
/// Search input component with debounce functionality.
/// </summary>
public partial class SearchInput : ComponentBase, IDisposable
{
    #region Parameters

    /// <summary>
    /// The current search value.
    /// </summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>
    /// Callback invoked when the value changes (after debounce).
    /// </summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>
    /// Placeholder text for the input.
    /// </summary>
    [Parameter]
    public string? Placeholder { get; set; } = "Search...";

    /// <summary>
    /// Debounce delay in milliseconds. Default is 300ms.
    /// </summary>
    [Parameter]
    public int DebounceMs { get; set; } = 300;

    /// <summary>
    /// ARIA label for accessibility.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    #endregion

    #region Private Fields

    private string? _currentValue;
    private System.Timers.Timer? _debounceTimer;

    #endregion

    #region Lifecycle Methods

    protected override void OnInitialized()
    {
        _currentValue = Value;
    }

    protected override void OnParametersSet()
    {
        if (_currentValue != Value)
        {
            _currentValue = Value;
        }
    }

    #endregion

    #region Event Handlers

    private void HandleInput(ChangeEventArgs e)
    {
        _currentValue = e.Value?.ToString();

        // Reset the debounce timer
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();

        _debounceTimer = new System.Timers.Timer(DebounceMs);
        _debounceTimer.Elapsed += async (sender, args) =>
        {
            _debounceTimer?.Stop();
            _debounceTimer?.Dispose();

            await InvokeAsync(async () =>
            {
                await ValueChanged.InvokeAsync(_currentValue);
            });
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    private async Task ClearSearch()
    {
        _currentValue = null;

        // Cancel any pending debounce
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();

        await ValueChanged.InvokeAsync(_currentValue);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
    }

    #endregion
}
