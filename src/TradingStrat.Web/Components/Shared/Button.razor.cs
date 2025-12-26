namespace TradingStrat.Web.Components.Shared;

public enum ButtonVariant
{
    Primary,
    Secondary,
    Danger,
    Success,
    Info,
    Ghost
}

public enum ButtonSize
{
    Small,
    Medium,
    Large
}

public enum ButtonType
{
    Button,
    Submit,
    Reset
}

public static class ButtonTypeExtensions
{
    public static string ToHtmlType(this ButtonType type) => type switch
    {
        ButtonType.Submit => "submit",
        ButtonType.Reset => "reset",
        _ => "button"
    };
}
