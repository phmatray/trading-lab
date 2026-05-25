using Shouldly;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.Settings.Tickers;
using TradyStrat.Domain.Shared.Money;
using TradyStrat.Domain.Shared.Market;
using TradyStrat.Infrastructure.Settings;
using TradyStrat.TestKit.Specifications;
using TradyStrat.TestKit.Time;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Settings;

public class EfFocusTickerRepositoryTests
{
    [Fact]
    public async Task RoundTrips_through_VO_and_updates_when_target_instrument_is_registered()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();

        // Seed the focus row + one matching instrument so GetAsync succeeds.
        Seed(db, SettingsKeys.TickersFocus, "CON3.L");
        db.Instruments.Add(Make("CON3.L"));
        // Second instrument so SaveAsync can switch focus to a different registered ticker.
        db.Instruments.Add(Make("COIN"));
        await db.SaveChangesAsync(ct);

        var repo = new EfFocusTickerRepository(db, new EfInstrumentRepository(db), new FakeClock(DateTime.UtcNow));
        var loaded = await repo.GetAsync(ct);
        loaded.Value.ShouldBe("CON3.L");

        await repo.SaveAsync(FocusTicker.Of("COIN"), ct);

        // Fresh repo against the same context proves the update went through SaveChanges (not just the change tracker).
        var reloaded = await new EfFocusTickerRepository(db, new EfInstrumentRepository(db), new FakeClock(DateTime.UtcNow)).GetAsync(ct);
        reloaded.Value.ShouldBe("COIN");
    }

    [Fact]
    public async Task SaveAsync_throws_SettingValidationException_when_ticker_is_not_registered()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();

        db.Instruments.Add(Make("CON3.L"));
        await db.SaveChangesAsync(ct);

        var repo = new EfFocusTickerRepository(db, new EfInstrumentRepository(db), new FakeClock(DateTime.UtcNow));

        var ex = await Should.ThrowAsync<SettingValidationException>(
            () => repo.SaveAsync(FocusTicker.Of("DOES_NOT_EXIST"), ct));

        ex.Message.ShouldContain("DOES_NOT_EXIST");
    }

    [Fact]
    public async Task SaveAsync_accepts_ticker_when_matching_instrument_is_registered()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();

        Seed(db, SettingsKeys.TickersFocus, "CON3.L");
        db.Instruments.Add(Make("CON3.L"));
        db.Instruments.Add(Make("COIN"));
        await db.SaveChangesAsync(ct);

        var repo = new EfFocusTickerRepository(db, new EfInstrumentRepository(db), new FakeClock(DateTime.UtcNow));

        await Should.NotThrowAsync(() => repo.SaveAsync(FocusTicker.Of("COIN"), ct));

        var reloaded = await new EfFocusTickerRepository(db, new EfInstrumentRepository(db), new FakeClock(DateTime.UtcNow)).GetAsync(ct);
        reloaded.Value.ShouldBe("COIN");
    }

    private static void Seed(Infrastructure.Data.AppDbContext db, string key, string value)
        => db.Add(new SettingEntry { Key = key, Value = value, UpdatedAt = DateTime.UtcNow });

    private static Instrument Make(string ticker)
        => Instrument.Existing(
            id:         new InstrumentId(0),
            ticker:     ticker,
            name:       ticker,
            currency:   Currency.Usd,
            exchange:   Exchange.Of("NMS"),
            timezoneId: TimezoneId.Of("America/New_York"),
            kind:       InstrumentKind.Held,
            addedAt:    DateTime.UtcNow);
}
