using Microsoft.AspNetCore.Components;
using TradingStrat.Web.Models;

namespace TradingStrat.Web.Components.Shared;

public partial class AlertMessage : ComponentBase
{
    [Parameter] public string Message { get; set; } = string.Empty;
    [Parameter] public AlertType Type { get; set; } = AlertType.Info;

    private string GetAlertClass() => Type switch
    {
        AlertType.Success => "bg-green-50 border border-green-200 text-green-800",
        AlertType.Error => "bg-red-50 border border-red-200 text-red-800",
        AlertType.Warning => "bg-yellow-50 border border-yellow-200 text-yellow-800",
        _ => "bg-blue-50 border border-blue-200 text-blue-800"
    };
}
