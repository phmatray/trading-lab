using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using TradingStrat.Web.Utilities;

namespace TradingStrat.Web.Components.Shared;

public partial class FormInputGroup : ComponentBase
{
    /// <summary>
    /// The label text to display above the input field.
    /// </summary>
    [Parameter]
    public string Label { get; set; } = "";

    /// <summary>
    /// The ID of the input element (for label "for" attribute).
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>
    /// Whether this field is optional (displays "(optional)" suffix).
    /// </summary>
    [Parameter]
    public bool IsOptional { get; set; }

    /// <summary>
    /// Optional help text displayed below the input.
    /// </summary>
    [Parameter]
    public string? HelpText { get; set; }

    /// <summary>
    /// The input field content (e.g., InputText, InputDate, etc.).
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Expression for validation (displays ValidationMessage for this field).
    /// </summary>
    [Parameter]
    public Expression<Func<object>>? ValidationFor { get; set; }

    /// <summary>
    /// Additional CSS classes to apply to the container div.
    /// </summary>
    [Parameter]
    public string ContainerClass { get; set; } = "";

    private string GetHelpTextClass()
    {
        return $"{TailwindStyles.TextMuted} text-xs mt-1";
    }
}
