namespace TradyStrat.Domain.Portfolio;

public readonly record struct PortfolioId(int Value)
{
    public static PortfolioId New()       => new(0);
    public static PortfolioId Singleton => new(1);
    public override string ToString() => $"PortfolioId({Value})";
}
