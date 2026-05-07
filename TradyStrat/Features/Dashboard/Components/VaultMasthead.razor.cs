using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace TradyStrat.Features.Dashboard.Components;

public partial class VaultMasthead : ComponentBase
{
    [Parameter] public DateOnly Today { get; set; }
    [Parameter] public int EntryNumber { get; set; }
    [Parameter] public string PriceAsOfRelative { get; set; } = "";

    // Time-travel — only set on the dashboard. Self-hide otherwise.
    [Parameter] public DateOnly? PrevTradingDay { get; set; }
    [Parameter] public DateOnly? NextTradingDay { get; set; }
    [Parameter] public DateOnly? EarliestTradingDay { get; set; }
    [Parameter] public DateOnly? LatestTradingDay { get; set; }
    [Parameter] public bool IsHistorical { get; set; }
    [Parameter] public EventCallback<DateOnly> OnDateSelected { get; set; }

    private bool ShowNav =>
        EarliestTradingDay is not null && LatestTradingDay is not null;

    private static string FormatDate(DateOnly d)
        => d.ToString("dd · MM · yyyy", CultureInfo.InvariantCulture);

    private static string FormatIso(DateOnly d)
        => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private string PrevHref =>
        PrevTradingDay is { } p ? $"/?on={FormatIso(p)}" : "#";

    private string NextHref =>
        NextTradingDay is { } n ? $"/?on={FormatIso(n)}" : "#";

    private async Task OnPickerChanged(ChangeEventArgs e)
    {
        if (e.Value is string s &&
            DateOnly.TryParseExact(
                s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var picked))
        {
            await OnDateSelected.InvokeAsync(picked);
        }
    }
}
