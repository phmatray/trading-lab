using Microsoft.AspNetCore.Components;

namespace TradyStrat.Features.Dashboard.Components;

public partial class RefreshFab : ComponentBase
{
    [Parameter] public bool Busy { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
}
