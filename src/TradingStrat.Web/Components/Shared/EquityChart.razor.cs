using Microsoft.AspNetCore.Components;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Web.Components.Shared;

public partial class EquityChart : ComponentBase
{
    [Parameter]
    public List<EquityPoint>? EquityCurve { get; set; }
}
