namespace TradyStrat.Domain;

public sealed record BollingerReading(decimal Upper, decimal Middle, decimal Lower, decimal Sigma)
{
    public static readonly BollingerReading Empty = new(0m, 0m, 0m, 0m);
    public bool IsEmpty => this == Empty;
}
