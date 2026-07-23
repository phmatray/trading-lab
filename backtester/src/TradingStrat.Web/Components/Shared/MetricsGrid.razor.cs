using Microsoft.AspNetCore.Components;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Web.Components.Shared;

public partial class MetricsGrid : ComponentBase
{
    [Parameter]
    public PerformanceMetrics? Metrics { get; set; }
}
