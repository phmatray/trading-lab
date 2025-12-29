using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

public partial class TickerInput : ComponentBase
{
    private readonly string _inputId = $"ticker-{Guid.NewGuid():N}";

    /// <summary>
    /// Current value of the ticker input
    /// </summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>
    /// Callback when value changes
    /// </summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>
    /// Expression for validation
    /// </summary>
    [Parameter]
    public Expression<Func<string>>? For { get; set; }

    /// <summary>
    /// Label text
    /// </summary>
    [Parameter]
    public string Label { get; set; } = "Ticker Symbol";

    /// <summary>
    /// Placeholder text
    /// </summary>
    [Parameter]
    public string Placeholder { get; set; } = "e.g., AAPL";

    /// <summary>
    /// Whether the field is required
    /// </summary>
    [Parameter]
    public bool Required { get; set; } = true;

    /// <summary>
    /// Whether to show validation messages
    /// </summary>
    [Parameter]
    public bool ShowValidation { get; set; } = true;

    /// <summary>
    /// Optional help text
    /// </summary>
    [Parameter]
    public string? HelpText { get; set; }

    /// <summary>
    /// Optional callback invoked when the input value changes
    /// </summary>
    [Parameter]
    public EventCallback OnInputChanged { get; set; }

    private string? CurrentValue { get; set; }

    protected override void OnParametersSet()
    {
        CurrentValue = Value;
    }

    private async Task HandleInput(ChangeEventArgs e)
    {
        string? newValue = e.Value?.ToString()?.ToUpperInvariant();
        CurrentValue = newValue;
        await ValueChanged.InvokeAsync(newValue);

        if (OnInputChanged.HasDelegate)
        {
            await OnInputChanged.InvokeAsync();
        }
    }
}
