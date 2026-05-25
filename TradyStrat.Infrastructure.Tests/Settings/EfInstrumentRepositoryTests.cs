using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Shared.Money;
using TradyStrat.Domain.Shared.Market;
using TradyStrat.Infrastructure.Data;
using TradyStrat.Infrastructure.Settings;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Settings;

/// <summary>
/// SQLite in-memory fixture so the Currency/Exchange/TimezoneId value converters
/// run their full round-trip. EF InMemory provider doesn't honor HasConversion
/// for VO fields the way SQLite does, so we use the real provider.
/// </summary>
public class EfInstrumentRepositoryTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly DbContextOptions<AppDbContext> _opts;

    public EfInstrumentRepositoryTests()
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

    private static readonly DateTime _probedNow = new(2026, 5, 22, 0, 0, 0, DateTimeKind.Utc);

    private static Instrument Probed(string ticker = "TST")
    {
        var inst = Instrument.Probed(
            ticker:     ticker,
            name:       $"Stub {ticker}",
            currency:   Currency.Eur,
            exchange:   Exchange.Of("LSE"),
            timezoneId: TimezoneId.Of("Europe/London"),
            kind:       InstrumentKind.Held,
            now:        _probedNow);
        inst.Confirm(new StubClock(_probedNow));
        return inst;
    }

    [Fact]
    public async Task Add_round_trips_VO_fields()
    {
        var ct = TestContext.Current.CancellationToken;

        await using (var db = NewContext())
        {
            await new EfInstrumentRepository(db).AddAsync(Probed("ABC"), ct);
        }

        await using var read = NewContext();
        var loaded = await new EfInstrumentRepository(read).FindByTickerAsync("ABC", ct);

        loaded.ShouldNotBeNull();
        loaded.Ticker.ShouldBe("ABC");
        loaded.Currency.Code.ShouldBe("EUR");
        loaded.Exchange.Code.ShouldBe("LSE");
        loaded.Timezone.Value.ShouldBe("Europe/London");
        loaded.AddedAt.ShouldBe(new DateTime(2026, 5, 22, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Add_throws_DuplicateInstrumentException_on_repeat_ticker()
    {
        var ct = TestContext.Current.CancellationToken;

        await using (var db = NewContext())
        {
            await new EfInstrumentRepository(db).AddAsync(Probed("DUP"), ct);
        }

        await using var db2 = NewContext();
        await Should.ThrowAsync<DuplicateInstrumentException>(
            () => new EfInstrumentRepository(db2).AddAsync(Probed("DUP"), ct));
    }

    [Fact]
    public async Task FindByTickerAsync_normalizes_input_casing()
    {
        var ct = TestContext.Current.CancellationToken;

        await using (var db = NewContext())
        {
            await new EfInstrumentRepository(db).AddAsync(Probed("XYZ"), ct);
        }

        await using var read = NewContext();
        var loaded = await new EfInstrumentRepository(read).FindByTickerAsync("  xyz  ", ct);
        loaded.ShouldNotBeNull();
        loaded.Ticker.ShouldBe("XYZ");
    }

    [Fact]
    public async Task ListAsync_orders_by_ticker_alphabetically()
    {
        var ct = TestContext.Current.CancellationToken;

        foreach (var t in new[] { "ZZZ", "AAA", "MMM" })
        {
            await using var db = NewContext();
            await new EfInstrumentRepository(db).AddAsync(Probed(t), ct);
        }

        await using var read = NewContext();
        var list = await new EfInstrumentRepository(read).ListAsync(ct);
        list.Select(i => i.Ticker).ShouldBe(["AAA", "MMM", "ZZZ"]);
    }

    private sealed class StubClock(DateTime now) : IClock
    {
        public DateTime UtcNow() => now;
        public DateOnly TodayLocal() => DateOnly.FromDateTime(now);
        public DateOnly TodayInExchangeTzFor(string ticker) => DateOnly.FromDateTime(now);
    }
}
