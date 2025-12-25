using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

public partial class DataTable : ComponentBase
{
    /// <summary>
    /// Table header content
    /// </summary>
    [Parameter]
    public RenderFragment? TableHeader { get; set; }

    /// <summary>
    /// Table body content
    /// </summary>
    [Parameter]
    public RenderFragment? TableBody { get; set; }

    /// <summary>
    /// Optional table footer content
    /// </summary>
    [Parameter]
    public RenderFragment? TableFooter { get; set; }

    /// <summary>
    /// Whether the table is in a loading state
    /// </summary>
    [Parameter]
    public bool IsLoading { get; set; }

    /// <summary>
    /// Whether the table has no data
    /// </summary>
    [Parameter]
    public bool IsEmpty { get; set; }

    /// <summary>
    /// Message to display when empty
    /// </summary>
    [Parameter]
    public string EmptyMessage { get; set; } = "No data available";

    /// <summary>
    /// Optional actions to display in empty state
    /// </summary>
    [Parameter]
    public RenderFragment? EmptyActions { get; set; }
}
