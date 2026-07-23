namespace TradyStrat.Application.Dashboard.Navigation;

public abstract record ValidationResult
{
    public sealed record Live : ValidationResult;
    public sealed record Historical(DateOnly Date) : ValidationResult;
    public sealed record RedirectTo(string Url) : ValidationResult;

    private ValidationResult() { }
}
