using Microsoft.AspNetCore.Components;
using TradingStrat.Application.Ports.Outbound;

namespace TradingStrat.Web.Components.Shared;

public partial class DataSummaryCard : ComponentBase
{
    /// <summary>
    /// The data summary result to display.
    /// </summary>
    [Parameter]
    public DataSummaryResult? Summary { get; set; }
}
