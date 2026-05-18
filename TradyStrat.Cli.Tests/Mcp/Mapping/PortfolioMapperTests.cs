using Shouldly;
using TradyStrat.Cli.Mcp.Mapping;
using TradyStrat.Domain;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Mapping;

public class PortfolioMapperTests
{
    private static readonly DateOnly AsOf = new(2026, 5, 18);
    private const decimal GoalEur = 10_000m;

    private static PortfolioSnapshot MakeSnapshot(IReadOnlyList<PositionRow>? positions = null)
        => new(
            Positions: positions ?? [],
            CurrentValueEur: 5_000m,
            CostBasisEur: 4_500m,
            UnrealizedPnLEur: 500m,
            RealizedPnLEur: 100m,
            ProgressPct: 50m,
            Shares: 50m,
            AvgCostEur: 90m);

    private static PositionRow MakePosition(string ticker = "CON3.L")
        => new(
            InstrumentId: 1,
            Ticker: ticker,
            Currency: "EUR",
            Quantity: 50m,
            CostBasisEur: 4_500m,
            MarketValueEur: 5_000m,
            UnrealizedPnLEur: 500m,
            RealizedPnLEur: 100m);

    private static Trade MakeTrade(int instrumentId, DateOnly executedOn, decimal price = 90m)
        => new()
        {
            Id = 1,
            InstrumentId = instrumentId,
            ExecutedOn = executedOn,
            Side = TradeSide.Buy,
            Quantity = 10m,
            PricePerShare = price,
            FeesEur = 1.50m,
            CreatedAt = DateTime.UtcNow,
        };

    private static readonly IReadOnlyDictionary<int, string> TickerMap =
        new Dictionary<int, string> { { 1, "CON3.L" }, { 2, "AAPL" } };

    [Fact]
    public void Maps_aggregate_and_positions()
    {
        var position = MakePosition();
        var snapshot = MakeSnapshot(positions: [position]);
        var dto = PortfolioMapper.ToSnapshot(snapshot, [], TickerMap, GoalEur, AsOf);

        dto.AsOfDate.ShouldBe(AsOf);

        dto.Aggregate.TotalValueEur.ShouldBe(5_000m);
        dto.Aggregate.CostBasisEur.ShouldBe(4_500m);
        dto.Aggregate.UnrealizedPnlEur.ShouldBe(500m);
        dto.Aggregate.RealizedPnlEur.ShouldBe(100m);
        dto.Aggregate.GoalEur.ShouldBe(GoalEur);
        dto.Aggregate.DistanceToGoalEur.ShouldBe(5_000m);   // 10_000 - 5_000
        dto.Aggregate.ProgressPct.ShouldBe(50m);

        dto.Positions.Count.ShouldBe(1);
        var pos = dto.Positions[0];
        pos.Ticker.ShouldBe("CON3.L");
        pos.Currency.ShouldBe("EUR");
        pos.Qty.ShouldBe(50m);
        pos.CostBasisEur.ShouldBe(4_500m);
        pos.MarketValueEur.ShouldBe(5_000m);
        pos.UnrealizedPnlEur.ShouldBe(500m);
        pos.RealizedPnlEur.ShouldBe(100m);

        dto.Trades.Count.ShouldBe(0);
        dto.TradesTruncated.ShouldBeFalse();
    }

    [Fact]
    public void Caps_trades_at_500_newest_first_and_sets_truncated_flag()
    {
        var snapshot = MakeSnapshot();
        // 600 trades with distinct dates from 2024-01-01 onwards
        var trades = Enumerable.Range(0, 600)
            .Select(i => MakeTrade(1, new DateOnly(2024, 1, 1).AddDays(i)))
            .ToList();

        var dto = PortfolioMapper.ToSnapshot(snapshot, trades, TickerMap, GoalEur, AsOf);

        dto.Trades.Count.ShouldBe(500);
        dto.TradesTruncated.ShouldBeTrue();
        // Newest-first: Trades[0] date should be later than Trades[^1] date
        dto.Trades[0].Date.ShouldBeGreaterThan(dto.Trades[^1].Date);
    }

    [Fact]
    public void Empty_ledger_truncated_flag_false()
    {
        var snapshot = MakeSnapshot();
        var dto = PortfolioMapper.ToSnapshot(snapshot, [], TickerMap, GoalEur, AsOf);

        dto.Trades.Count.ShouldBe(0);
        dto.TradesTruncated.ShouldBeFalse();
    }
}
