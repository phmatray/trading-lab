using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

/// <summary>
/// Breadcrumb navigation component for hierarchical navigation.
/// </summary>
public partial class BreadcrumbNav : ComponentBase
{
    #region Parameters

    /// <summary>
    /// List of breadcrumb items to display.
    /// </summary>
    [Parameter]
    public List<Breadcrumb> Breadcrumbs { get; set; } = new();

    #endregion

    #region Nested Types

    /// <summary>
    /// Represents a single breadcrumb item.
    /// </summary>
    public class Breadcrumb
    {
        /// <summary>
        /// Display label for the breadcrumb.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Navigation link for the breadcrumb.
        /// </summary>
        public string Href { get; set; } = string.Empty;
    }

    #endregion
}
