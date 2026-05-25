using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Infrastructure.AiSuggestion;
using TradyStrat.Infrastructure.Data;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.AiSuggestion;

/// <summary>
/// SQLite in-memory fixture so the OwnsOne/OwnsMany mappings exercise the real
/// provider's INSERT semantics. EF InMemory chokes on `ValueGeneratedOnAdd`
/// parent + OwnsOne dependent because it cannot mutate the dependent's shadow FK.
///
/// Each `AddAsync` call uses a *fresh* DbContext on the shared SQLite connection
/// to mirror production (DbContext scoped per request). A single shared context
/// reuses Quantity.None / Price.None singletons via the change tracker and
/// surfaces a phantom "key cannot be modified" error that production never hits.
/// </summary>
public class EfSuggestionRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 5, 22, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _conn;
    private readonly DbContextOptions<AppDbContext> _opts;

    public EfSuggestionRepositoryTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        _opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        using var bootstrap = new AppDbContext(_opts);
        bootstrap.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _conn.Dispose();
        GC.SuppressFinalize(this);
    }

    private AppDbContext NewContext() => new(_opts);

    private static Suggestion Build(int instrumentId, DateOnly forDate,
        Quantity? qty = null, Price? price = null,
        IReadOnlyList<Citation>? citations = null) => Suggestion.From(
        instrumentId: new InstrumentId(instrumentId),
        forDate:      forDate,
        action:       SuggestionAction.Hold,
        quantityHint: qty ?? Quantity.None,
        maxPriceHint: price ?? Price.None(Currency.Eur),
        conviction:   Conviction.Of(7),
        rationale:    "Test rationale.",
        citations:    citations ?? [new Citation("c1", "rsi", "X", "30")],
        snapshot:     MarketSnapshot.Empty,
        fingerprint:  PromptFingerprint.Of("h1", "e1", "v1"),
        thinkingText: "",
        createdAt:    Now);

    private async Task AddOneAsync(Suggestion s, CancellationToken ct)
    {
        await using var db = NewContext();
        await new EfSuggestionRepository(db).AddAsync(s, ct);
    }

    [Fact]
    public async Task Add_and_get_round_trips_typed_fields()
    {
        var ct = TestContext.Current.CancellationToken;
        await AddOneAsync(Build(1, new DateOnly(2026, 5, 22),
            qty: Quantity.Of(10m),
            price: Price.Of(Money.Of(4m, Currency.Eur))), ct);

        await using var db = NewContext();
        var fresh = await new EfSuggestionRepository(db)
            .GetForAsync(new InstrumentId(1), new DateOnly(2026, 5, 22), ct);

        fresh.ShouldNotBeNull();
        fresh.QuantityHint.IsSpecified.ShouldBeTrue();
        fresh.QuantityHint.Value.ShouldBe(10m);
        fresh.MaxPriceHint.IsEmpty.ShouldBeFalse();
        fresh.MaxPriceHint.PerUnit.Amount.ShouldBe(4m);
        fresh.Conviction.Value.ShouldBe(7);
        fresh.Citations.Count.ShouldBe(1);
        fresh.Citations[0].Claim.ShouldBe("c1");
        fresh.Fingerprint.PromptHash.ShouldBe("h1");
    }

    [Fact]
    public async Task ListFor_filters_by_instrument_and_date_range()
    {
        var ct = TestContext.Current.CancellationToken;
        await AddOneAsync(Build(1, new DateOnly(2026, 5, 20)), ct);
        await AddOneAsync(Build(1, new DateOnly(2026, 5, 22)), ct);
        await AddOneAsync(Build(1, new DateOnly(2026, 5, 25)), ct);
        await AddOneAsync(Build(2, new DateOnly(2026, 5, 22)), ct);

        await using var db = NewContext();
        var list = await new EfSuggestionRepository(db).ListForAsync(
            new InstrumentId(1),
            new DateRange(new DateOnly(2026, 5, 21), new DateOnly(2026, 5, 24)),
            ct);

        list.Count.ShouldBe(1);
        list[0].ForDate.ShouldBe(new DateOnly(2026, 5, 22));
    }

    [Fact]
    public async Task PriorToAsync_returns_most_recent_before_date()
    {
        var ct = TestContext.Current.CancellationToken;
        await AddOneAsync(Build(1, new DateOnly(2026, 5, 18)), ct);
        await AddOneAsync(Build(1, new DateOnly(2026, 5, 20)), ct);
        await AddOneAsync(Build(1, new DateOnly(2026, 5, 22)), ct);

        await using var db = NewContext();
        var prior = await new EfSuggestionRepository(db)
            .PriorToAsync(new InstrumentId(1), new DateOnly(2026, 5, 22), ct);
        prior.ShouldNotBeNull();
        prior.ForDate.ShouldBe(new DateOnly(2026, 5, 20));
    }
}
