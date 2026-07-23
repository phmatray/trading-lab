using Microsoft.AspNetCore.Components;
using TradingStrat.Web.Models;

namespace TradingStrat.Web.Components.Shared;

public partial class AlertMessage : ComponentBase
{
    [Parameter] public string Message { get; set; } = string.Empty;
    [Parameter] public AlertType Type { get; set; } = AlertType.Info;

    private string GetAlertClass() => Type switch
    {
        AlertType.Success => "bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800 text-green-800 dark:text-green-400",
        AlertType.Error => "bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800 text-red-800 dark:text-red-400",
        AlertType.Warning => "bg-yellow-50 dark:bg-yellow-900/20 border-yellow-200 dark:border-yellow-800 text-yellow-800 dark:text-yellow-400",
        _ => "bg-blue-50 dark:bg-blue-900/20 border-blue-200 dark:border-blue-800 text-blue-800 dark:text-blue-400"
    };

    private string GetIconName() => Type switch
    {
        AlertType.Success => "status",
        AlertType.Error => "alert",
        AlertType.Warning => "alert",
        _ => "alert"
    };
}
