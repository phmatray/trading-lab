namespace TradyStrat.Domain.Suggestions;

public sealed record Citation
{
    public string Claim     { get; private set; } = "";
    public string Indicator { get; private set; } = "";
    public string Ticker    { get; private set; } = "";
    public string Value     { get; private set; } = "";

    private Citation() { }   // EF

    public Citation(string claim, string indicator, string ticker, string value)
    {
        Claim     = claim     ?? "";
        Indicator = indicator ?? "";
        Ticker    = ticker    ?? "";
        Value     = value     ?? "";
    }
}
