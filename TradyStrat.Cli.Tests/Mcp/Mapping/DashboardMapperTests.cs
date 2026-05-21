using Shouldly;
using TradyStrat.Application.AiSuggestion.Backfill;
using TradyStrat.Application.AiSuggestion.CallDiff;
using TradyStrat.Application.Dashboard;
using TradyStrat.Application.Indicators;
using TradyStrat.Application.PredictionMarkets;
using TradyStrat.Cli.Mcp.Mapping;
using TradyStrat.Domain;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Mapping;

public class DashboardMapperTests
{
    private static readonly DateOnly Today = new(2026, 5, 18);
    private const string FocusTicker = "CON3.L";

    private static IndicatorReading MakeReading(Zone zone = Zone.Accumulate)
        => new(
            Ticker: FocusTicker,
            Price: 100m,
            Bollinger: new BollingerReading(Upper: 110m, Middle: 100m, Lower: 90m, Sigma: 5m),
            Rsi: 45m,
            Sma50: 98m,
            Sma200: 92m,
            Ichimoku: new IchimokuReading(
                Tenkan: 99m, Kijun: 97m,
                SenkouA: 96m, SenkouB: 94m,
                Chikou: 102m,
                Signal: IchimokuSignal.AboveCloud),
            Zone: zone,
            Reasons: ["RSI in zone"]);

    private static DashboardViewModel MakeViewModel(Suggestion? todaysCall = null)
    {
        var position = new PositionRow(
            InstrumentId: 1,
            Ticker: FocusTicker,
            Currency: "EUR",
            Quantity: 50m,
            CostBasisEur: 4500m,
            MarketValueEur: 5000m,
            UnrealizedPnLEur: 500m,
            RealizedPnLEur: 100m);

        var snap = new PortfolioSnapshot(
            Positions: [position],
            CurrentValueEur: 5000m,
            CostBasisEur: 4500m,
            UnrealizedPnLEur: 500m,
            RealizedPnLEur: 100m,
            ProgressPct: 50m,
            Shares: 50m,
            AvgCostEur: 90m);

        var callState = todaysCall is null ? null : new SuggestionState.Ready(todaysCall);

        var tickerView = new TickerView(
            InstrumentId: 1,
            Ticker: FocusTicker,
            Currency: "USD",
            Price: 120m,
            PriceEur: 100m,
            DeltaPct: 1.5m,
            Zone: Zone.Accumulate,
            Spark: [98m, 99m, 100m],
            CallState: callState);

        var goal = new GoalConfig
        {
            Id = 1,
            TargetEur = 10_000m,
            TargetDate = new DateOnly(2027, 12, 31),
            UpdatedAt = DateTime.UtcNow,
        };

        return new DashboardViewModel(
            Today: Today,
            EntryNumber: 4,
            Portfolio: snap,
            Goal: goal,
            FocusCallState: callState,
            Tickers: [tickerView],
            Positions: [position],
            FocusTicker: FocusTicker,
            Growth: [],
            LatestPriceDate: Today,
            GoalPace: GoalPaceVm.Zero,
            CallDiff: CallDiff.None,
            BackfillStatus: BackfillStatus.Idle.Instance,
            PriceAsOfRelative: "today",
            CallAsOfRelative: "1h ago",
            FxAsOfRelative: "2h ago",
            IndicatorHistories: new Dictionary<(string, IndicatorKind), IndicatorSeries>(),
            CapitalEvents: [],
            IsHistorical: false,
            EarliestTradingDay: new DateOnly(2025, 12, 7),
            LatestTradingDay: Today,
            PrevTradingDay: Today.AddDays(-1),
            NextTradingDay: null);
    }

    private static Suggestion MakeSuggestion(string? envelopeHash, string? promptVersionHash)
        => new()
        {
            Id = 1,
            InstrumentId = 1,
            ForDate = Today,
            Action = SuggestionAction.Acquire,
            Conviction = 8,
            Rationale = "Strong buy signal",
            CitationsJson = "[]",
            PromptHash = "abc123",
            EnvelopeHash = envelopeHash,
            PromptVersionHash = promptVersionHash,
            CreatedAt = DateTime.UtcNow,
        };

    [Fact]
    public void Maps_full_dashboard_with_suggestion()
    {
        var suggestion = MakeSuggestion("abcdef1234567890", "feedbeef12345678");
        var vm = MakeViewModel(suggestion);
        var reading = MakeReading();

        var snap = DashboardMapper.ToSnapshot(vm, reading);

        snap.Ticker.ShouldBe(FocusTicker);
        snap.AsOfDate.ShouldBe(Today);

        // Last close: USD = reading.Price, EUR = focus TickerView.PriceEur
        snap.LastClose.Usd.ShouldBe(120m);
        snap.LastClose.Eur.ShouldBe(100m);
        snap.LastClose.FxRate.ShouldBe(1.2m);  // 120 / 100

        // Zone
        snap.Zone.Overall.ShouldBe("Accumulate");
        snap.Zone.ByIndicator.Bollinger.ShouldBe("Accumulate");
        snap.Zone.ByIndicator.Rsi.ShouldBe("Accumulate");
        snap.Zone.ByIndicator.Sma.ShouldBe("Accumulate");
        snap.Zone.ByIndicator.Ichimoku.ShouldBe("Accumulate");

        // Indicators
        snap.Indicators.Bollinger.Upper.ShouldBe(110m);
        snap.Indicators.Bollinger.Mid.ShouldBe(100m);
        snap.Indicators.Bollinger.Lower.ShouldBe(90m);
        snap.Indicators.Rsi.Value.ShouldBe(45m);
        snap.Indicators.Sma.Sma50.ShouldBe(98m);
        snap.Indicators.Sma.Sma200.ShouldBe(92m);
        snap.Indicators.Ichimoku.Tenkan.ShouldBe(99m);
        snap.Indicators.Ichimoku.Kijun.ShouldBe(97m);
        snap.Indicators.Ichimoku.SenkouA.ShouldBe(96m);
        snap.Indicators.Ichimoku.SenkouB.ShouldBe(94m);
        snap.Indicators.Ichimoku.Chikou.ShouldBe(102m);

        // Suggestion present
        snap.Suggestion.ShouldNotBeNull();
        snap.Suggestion!.Action.ShouldBe("Acquire");
        snap.Suggestion.Conviction.ShouldBe(8);
        snap.Suggestion.EnvelopeHash.ShouldBe("abcdef12");
        snap.Suggestion.PromptVersionHash.ShouldBe("feedbeef");

        // Position
        snap.Position.Qty.ShouldBe(50m);
        snap.Position.CostBasisEur.ShouldBe(4500m);
        snap.Position.MarketValueEur.ShouldBe(5000m);
        snap.Position.UnrealizedPnlEur.ShouldBe(500m);
        snap.Position.RealizedPnlEur.ShouldBe(100m);
    }

    [Fact]
    public void Suggestion_is_null_when_dashboard_has_no_suggestion()
    {
        var vm = MakeViewModel(todaysCall: null);
        var reading = MakeReading();

        var snap = DashboardMapper.ToSnapshot(vm, reading);

        snap.Suggestion.ShouldBeNull();
    }

    [Fact]
    public void Hashes_truncated_to_8_chars()
    {
        var suggestion = MakeSuggestion("abcdefghijklmnop", "12345678abcdefgh");
        var vm = MakeViewModel(suggestion);
        var reading = MakeReading();

        var snap = DashboardMapper.ToSnapshot(vm, reading);

        snap.Suggestion!.EnvelopeHash.ShouldBe("abcdefgh");
        snap.Suggestion!.PromptVersionHash.ShouldBe("12345678");
    }
}
