using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Application.AiSuggestion.Snapshot.Sections;
using TradyStrat.Application.Fx;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Data;
using TradyStrat.TestKit;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Application.Tests.AiSuggestion.Snapshot.Sections;

public class RecentSuggestionsSectionTests
{
    private static readonly DateOnly AsOf = new(2026, 5, 18);
    private const int Instr = 1;
    private const string Ticker = "TST";

    [Fact]
    public async Task Emits_30_most_recent_rows_chronological_drops_older()
    {
        await using var db = InMemoryDb.Create();
        SeedInstrument(db, Instr, Ticker);
        for (int i = 0; i < 35; i++)
            db.Suggestions.Add(MkSuggestion(Instr, AsOf.AddDays(-(35 - i)), SuggestionAction.Hold, i));
        SeedBars(db, Ticker, AsOf.AddDays(-40), 60, baseClose: 100m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var section = NewSection(db);
        var b = new SnapshotBuilder();
        await section.ContributeAsync(b, Instr, AsOf, TestContext.Current.CancellationToken);

        b.RecentSuggestions.Count.ShouldBe(30);
        b.RecentSuggestions.Select(r => r.Date).ShouldBeInOrder();
    }

    [Fact]
    public async Task Marks_recent_rows_with_incomplete_forward_window()
    {
        await using var db = InMemoryDb.Create();
        SeedInstrument(db, Instr, Ticker);
        db.Suggestions.Add(MkSuggestion(Instr, AsOf.AddDays(-2), SuggestionAction.Acquire, 7));
        SeedBars(db, Ticker, AsOf.AddDays(-2), 2, baseClose: 100m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var section = NewSection(db);
        var b = new SnapshotBuilder();
        await section.ContributeAsync(b, Instr, AsOf, TestContext.Current.CancellationToken);

        var row = b.RecentSuggestions.ShouldHaveSingleItem();
        row.IsForwardWindowComplete.ShouldBeFalse();
        row.FwdReturnPct.ShouldBe(0m);
        row.WasCorrect.ShouldBeFalse();
    }

    [Fact]
    public async Task Computes_fwd_return_and_was_correct_for_complete_window()
    {
        await using var db = InMemoryDb.Create();
        SeedInstrument(db, Instr, Ticker);
        db.Suggestions.Add(MkSuggestion(Instr, AsOf.AddDays(-10), SuggestionAction.Acquire, 8));
        SeedExactBars(db, Ticker, AsOf.AddDays(-10), [100m, 101m, 101m, 102m, 102m, 103m]);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var section = NewSection(db);
        var b = new SnapshotBuilder();
        await section.ContributeAsync(b, Instr, AsOf, TestContext.Current.CancellationToken);

        var row = b.RecentSuggestions.ShouldHaveSingleItem();
        row.IsForwardWindowComplete.ShouldBeTrue();
        row.FwdReturnPct.ShouldBe(3.0m);
        row.WasCorrect.ShouldBeTrue();
    }

    [Fact]
    public async Task NetTradeFlow_null_when_no_trades_in_window()
    {
        await using var db = InMemoryDb.Create();
        SeedInstrument(db, Instr, Ticker);
        db.Suggestions.Add(MkSuggestion(Instr, AsOf.AddDays(-10), SuggestionAction.Hold, 6));
        SeedExactBars(db, Ticker, AsOf.AddDays(-10), [100m, 100m, 100m, 100m, 100m, 100m]);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var section = NewSection(db);
        var b = new SnapshotBuilder();
        await section.ContributeAsync(b, Instr, AsOf, TestContext.Current.CancellationToken);

        b.RecentSuggestions.ShouldHaveSingleItem().NetTradeFlowEur.ShouldBeNull();
    }

    [Fact]
    public async Task NetTradeFlow_negative_for_buy_in_window()
    {
        await using var db = InMemoryDb.Create();
        SeedInstrument(db, Instr, Ticker);
        var sDate = AsOf.AddDays(-10);
        db.Suggestions.Add(MkSuggestion(Instr, sDate, SuggestionAction.Acquire, 7));
        SeedExactBars(db, Ticker, sDate, [100m, 100m, 100m, 100m, 100m, 100m]);
        db.Trades.Add(new Trade
        {
            Id = 0, InstrumentId = Instr, ExecutedOn = sDate.AddDays(3),
            Side = TradeSide.Buy, Quantity = 10m, PricePerShare = 50m,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var section = NewSection(db);
        var b = new SnapshotBuilder();
        await section.ContributeAsync(b, Instr, AsOf, TestContext.Current.CancellationToken);

        b.RecentSuggestions.ShouldHaveSingleItem().NetTradeFlowEur.ShouldBe(-500m);
    }

    [Fact]
    public async Task Headline_trims_at_whitespace_no_ellipsis()
    {
        await using var db = InMemoryDb.Create();
        SeedInstrument(db, Instr, Ticker);
        var rationale = "The EMA20 just crossed EMA50 from below on rising volume; conviction holds despite Polymarket softness.";
        db.Suggestions.Add(MkSuggestion(Instr, AsOf.AddDays(-10), SuggestionAction.Acquire, 7, rationale));
        SeedExactBars(db, Ticker, AsOf.AddDays(-10), [100m, 100m, 100m, 100m, 100m, 100m]);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var section = NewSection(db);
        var b = new SnapshotBuilder();
        await section.ContributeAsync(b, Instr, AsOf, TestContext.Current.CancellationToken);

        var headline = b.RecentSuggestions.ShouldHaveSingleItem().RationaleHeadline;
        headline.Length.ShouldBeLessThanOrEqualTo(80);
        headline.ShouldNotEndWith(" ");
        rationale.ShouldStartWith(headline);
        headline.ShouldNotEndWith("…");
    }

    // ----- helpers -----

    private static RecentSuggestionsSection NewSection(AppDbContext db)
    {
        var listInstr = new ListInstrumentsUseCase(new TestRepo<Instrument>(db), NullLogger<ListInstrumentsUseCase>.Instance);
        var fx        = new FxConverter(new TestRepo<FxRate>(db));
        var rule      = new FixedThresholdCorrectness(2.0m);
        return new RecentSuggestionsSection(
            new TestRepo<Suggestion>(db),
            new TestRepo<PriceBar>(db),
            new TestRepo<Trade>(db),
            listInstr, fx, rule);
    }

    private static Suggestion MkSuggestion(int instrId, DateOnly date, SuggestionAction action, int conviction, string rationale = "rationale")
        => new()
        {
            Id           = 0,
            InstrumentId = instrId,
            ForDate      = date,
            Action       = action,
            Conviction   = conviction,
            Rationale    = rationale,
            CitationsJson = "[]",
            PromptHash   = "TEST",
            CreatedAt    = DateTime.UtcNow,
        };

    private static void SeedInstrument(AppDbContext db, int id, string ticker)
        => db.Instruments.Add(new Instrument
        {
            Id = id, Ticker = ticker, Name = ticker, Currency = "EUR",
            Exchange = "X", TimezoneId = "Etc/UTC", Kind = InstrumentKind.Held,
            AddedAt = DateTime.UtcNow,
        });

    private static void SeedBars(AppDbContext db, string ticker, DateOnly from, int count, decimal baseClose)
    {
        for (int i = 0; i < count; i++)
            db.PriceBars.Add(new PriceBar
            {
                Id = 0, Ticker = ticker, Date = from.AddDays(i),
                Open = baseClose, High = baseClose, Low = baseClose, Close = baseClose, Volume = 1,
            });
    }

    private static void SeedExactBars(AppDbContext db, string ticker, DateOnly from, decimal[] closes)
    {
        for (int i = 0; i < closes.Length; i++)
            db.PriceBars.Add(new PriceBar
            {
                Id = 0, Ticker = ticker, Date = from.AddDays(i),
                Open = closes[i], High = closes[i], Low = closes[i], Close = closes[i], Volume = 1,
            });
    }
}
