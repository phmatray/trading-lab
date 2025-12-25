using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

public partial class DateRangeInput : ComponentBase
{
    private readonly string _startDateId = $"start-date-{Guid.NewGuid():N}";
    private readonly string _endDateId = $"end-date-{Guid.NewGuid():N}";

    /// <summary>
    /// Start date
    /// </summary>
    [Parameter]
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    [Parameter]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Callback when start date changes
    /// </summary>
    [Parameter]
    public EventCallback<DateTime?> StartDateChanged { get; set; }

    /// <summary>
    /// Callback when end date changes
    /// </summary>
    [Parameter]
    public EventCallback<DateTime?> EndDateChanged { get; set; }

    /// <summary>
    /// Whether to show preset buttons
    /// </summary>
    [Parameter]
    public bool ShowPresets { get; set; } = true;

    /// <summary>
    /// Custom presets (overrides default presets if provided)
    /// </summary>
    [Parameter]
    public List<DateRangePreset>? CustomPresets { get; set; }

    private string? GetDateString(DateTime? date)
    {
        return date?.ToString("yyyy-MM-dd");
    }

    private async Task HandleStartDateChange(ChangeEventArgs e)
    {
        if (DateTime.TryParse(e.Value?.ToString(), out DateTime date))
        {
            await StartDateChanged.InvokeAsync(date);
        }
        else
        {
            await StartDateChanged.InvokeAsync(null);
        }
    }

    private async Task HandleEndDateChange(ChangeEventArgs e)
    {
        if (DateTime.TryParse(e.Value?.ToString(), out DateTime date))
        {
            await EndDateChanged.InvokeAsync(date);
        }
        else
        {
            await EndDateChanged.InvokeAsync(null);
        }
    }

    private List<DateRangePreset> GetPresets()
    {
        if (CustomPresets != null && CustomPresets.Any())
        {
            return CustomPresets;
        }

        DateTime today = DateTime.Today;
        return new List<DateRangePreset>
        {
            new() { Label = "Last 7 days", StartDate = today.AddDays(-7), EndDate = today },
            new() { Label = "Last 30 days", StartDate = today.AddDays(-30), EndDate = today },
            new() { Label = "Last 3 months", StartDate = today.AddMonths(-3), EndDate = today },
            new() { Label = "Last 6 months", StartDate = today.AddMonths(-6), EndDate = today },
            new() { Label = "Last year", StartDate = today.AddYears(-1), EndDate = today },
            new() { Label = "YTD", StartDate = new DateTime(today.Year, 1, 1), EndDate = today }
        };
    }

    private async Task ApplyPreset(DateRangePreset preset)
    {
        await StartDateChanged.InvokeAsync(preset.StartDate);
        await EndDateChanged.InvokeAsync(preset.EndDate);
    }

    /// <summary>
    /// Date range preset model
    /// </summary>
    public class DateRangePreset
    {
        public string Label { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
