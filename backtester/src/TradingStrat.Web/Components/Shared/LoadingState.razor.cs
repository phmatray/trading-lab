using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

public partial class LoadingState : ComponentBase
{
    /// <summary>
    /// Loading message to display
    /// </summary>
    [Parameter]
    public string Message { get; set; } = "Loading...";

    /// <summary>
    /// Loading style (Spinner or Skeleton)
    /// </summary>
    [Parameter]
    public LoadingStyle Style { get; set; } = LoadingStyle.Spinner;

    /// <summary>
    /// Type of skeleton to display (card, table, list, chart)
    /// Only used when Style is Skeleton
    /// </summary>
    [Parameter]
    public string SkeletonType { get; set; } = "card";

    /// <summary>
    /// Loading style options
    /// </summary>
    public enum LoadingStyle
    {
        Spinner,
        Skeleton
    }
}
