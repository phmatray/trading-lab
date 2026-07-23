using System.ComponentModel;
using ModelContextProtocol.Server;
using TradyStrat.Application.PriceFeed.UseCases;
using TradyStrat.Application.UseCases;
using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Cli.Mcp.Mapping;
using TradyStrat.Domain;

namespace TradyStrat.Cli.Mcp.Tools;

[McpServerToolType]
internal sealed class PriceTool(
    IUseCase<GetPriceSeriesInput, GetPriceSeriesOutput> useCase,
    Guards guards,
    IClock clock)
{
    private const int MaxBars = 365;

    [McpServerTool(Name = "query_prices"),
     Description("Daily OHLCV bars for an instrument, optionally with indicator series.")]
    public async Task<PriceSeries> QueryPrices(
        string instrument,
        string? from = null,
        string? to = null,
        bool withIndicators = false,
        CancellationToken ct = default)
    {
        var inst = await guards.ResolveInstrumentOrThrow(instrument, ct);
        var (f, t) = Guards.ResolveDateRange(from, to, defaultBack: 90, clockToday: clock.TodayLocal());
        if ((t.DayNumber - f.DayNumber + 1) > MaxBars)
            throw new ArgumentException(
                $"Date range exceeds {MaxBars}-day maximum. Narrow the window or make multiple calls.");

        var output = await useCase.ExecuteAsync(
            new GetPriceSeriesInput(inst.Ticker, f, t, withIndicators), ct);
        return PriceMapper.ToSeries(output, inst.Ticker);
    }
}
