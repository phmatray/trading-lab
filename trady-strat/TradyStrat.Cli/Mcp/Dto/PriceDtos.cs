namespace TradyStrat.Cli.Mcp.Dto;

public sealed record PriceSeries(
    string Instrument, DateOnly From, DateOnly To, int BarCount,
    IReadOnlyList<BarDto> Bars,
    IndicatorArraysDto? Indicators);

public sealed record BarDto(
    DateOnly Date, decimal Open, decimal High, decimal Low, decimal Close, long Volume);

public sealed record IndicatorArraysDto(
    IReadOnlyList<decimal?> Rsi,
    IReadOnlyList<decimal?> BollingerMid,
    IReadOnlyList<decimal?> Sma200,
    IReadOnlyList<decimal?> Ichimoku);
