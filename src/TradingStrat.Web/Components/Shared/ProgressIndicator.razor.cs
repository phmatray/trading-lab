using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

public partial class ProgressIndicator : ComponentBase
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public string CurrentMessage { get; set; } = "Processing...";
    [Parameter] public int? Progress { get; set; }
}
