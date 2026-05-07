namespace TradyStrat.Common.Domain;

public sealed record PriceBar
{
    public required int Id { get; init; }
    public required string Ticker { get; init; }
    public required DateOnly Date { get; init; }
    public required decimal Open { get; init; }
    public required decimal High { get; init; }
    public required decimal Low { get; init; }
    public required decimal Close { get; init; }
    public required long Volume { get; init; }

    public decimal Range  => High - Low;
    public decimal Change => Close - Open;
    public bool    IsUp   => Close >= Open;
}
