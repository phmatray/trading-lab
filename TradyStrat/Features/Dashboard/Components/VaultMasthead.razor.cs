using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace TradyStrat.Features.Dashboard.Components;

public partial class VaultMasthead : ComponentBase
{
    [Parameter] public DateOnly Today { get; set; }
    [Parameter] public int EntryNumber { get; set; }

    private static string FormatDate(DateOnly d)
        => d.ToString("dd · MM · yyyy", CultureInfo.InvariantCulture);
}
