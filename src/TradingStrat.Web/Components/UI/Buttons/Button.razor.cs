using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TradingStrat.Web.Utilities;

namespace TradingStrat.Web.Components.UI.Buttons;

public partial class Button
{
    /// <summary>
    /// Button color variant. Mutually exclusive with Outline and Plain.
    /// </summary>
    [Parameter]
    public ButtonColor? Color { get; set; }

    /// <summary>
    /// Outline variant (transparent background with border). Mutually exclusive with Color and Plain.
    /// </summary>
    [Parameter]
    public bool Outline { get; set; }

    /// <summary>
    /// Plain variant (no background, minimal styling). Mutually exclusive with Color and Outline.
    /// </summary>
    [Parameter]
    public bool Plain { get; set; }

    /// <summary>
    /// Button type (button, submit, reset). Only used when Href is null.
    /// </summary>
    [Parameter]
    public ButtonType Type { get; set; } = ButtonType.Button;

    /// <summary>
    /// When set, renders as an anchor element instead of a button.
    /// </summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>
    /// Disables the button.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Additional CSS classes to apply.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Button content (text, icons, etc.).
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Click event handler. Only triggered when Href is null.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Additional HTML attributes to apply.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    // State tracking for data-* attributes
    private bool _isFocused;
    private bool _isHovered;
    private bool _isActive;

    private string GetButtonClasses()
    {
        var classes = new List<string>
        {
            "btn-base",
            "cursor-default"
        };

        // Determine variant
        if (Outline)
        {
            classes.Add("btn-outline");
        }
        else if (Plain)
        {
            classes.Add("btn-plain");
        }
        else
        {
            // Solid variant (default)
            classes.Add("btn-solid");

            // Add color-specific classes
            string colorClass = (Color ?? ButtonColor.DarkZinc) switch
            {
                ButtonColor.Blue => CatalystStyles.ButtonBlue,
                ButtonColor.Red => CatalystStyles.ButtonRed,
                ButtonColor.Green => CatalystStyles.ButtonGreen,
                ButtonColor.Orange => CatalystStyles.ButtonOrange,
                ButtonColor.Indigo => CatalystStyles.ButtonIndigo,
                ButtonColor.Cyan => CatalystStyles.ButtonCyan,
                ButtonColor.Amber => CatalystStyles.ButtonAmber,
                ButtonColor.Yellow => CatalystStyles.ButtonYellow,
                ButtonColor.Lime => CatalystStyles.ButtonLime,
                ButtonColor.Emerald => CatalystStyles.ButtonEmerald,
                ButtonColor.Teal => CatalystStyles.ButtonTeal,
                ButtonColor.Sky => CatalystStyles.ButtonSky,
                ButtonColor.Violet => CatalystStyles.ButtonViolet,
                ButtonColor.Purple => CatalystStyles.ButtonPurple,
                ButtonColor.Fuchsia => CatalystStyles.ButtonFuchsia,
                ButtonColor.Pink => CatalystStyles.ButtonPink,
                ButtonColor.Rose => CatalystStyles.ButtonRose,
                ButtonColor.Zinc => CatalystStyles.ButtonZinc,
                ButtonColor.DarkZinc => CatalystStyles.ButtonDarkZinc,
                ButtonColor.White => CatalystStyles.ButtonWhite,
                ButtonColor.Dark => CatalystStyles.ButtonDark,
                ButtonColor.Light => CatalystStyles.ButtonLight,
                _ => CatalystStyles.ButtonDarkZinc
            };

            classes.Add(colorClass);
        }

        // Add custom class if provided
        if (!string.IsNullOrWhiteSpace(Class))
        {
            classes.Add(Class);
        }

        return string.Join(" ", classes);
    }

    private async Task HandleClick(MouseEventArgs e)
    {
        if (!Disabled && OnClick.HasDelegate)
        {
            await OnClick.InvokeAsync(e);
        }
    }

    private void HandleFocus() => _isFocused = true;
    private void HandleBlur() => _isFocused = false;
    private void HandleMouseEnter() => _isHovered = true;
    private void HandleMouseLeave()
    {
        _isHovered = false;
        _isActive = false;
    }
    private void HandleMouseDown() => _isActive = true;
    private void HandleMouseUp() => _isActive = false;
}

/// <summary>
/// Button color variants matching Catalyst UI Kit palette.
/// </summary>
public enum ButtonColor
{
    Blue,
    Red,
    Green,
    Orange,
    Indigo,
    Cyan,
    Amber,
    Yellow,
    Lime,
    Emerald,
    Teal,
    Sky,
    Violet,
    Purple,
    Fuchsia,
    Pink,
    Rose,
    Zinc,
    DarkZinc,
    White,
    Dark,
    Light
}

/// <summary>
/// Button HTML type attribute.
/// </summary>
public enum ButtonType
{
    Button,
    Submit,
    Reset
}
