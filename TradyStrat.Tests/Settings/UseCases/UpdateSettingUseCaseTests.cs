using TradyStrat.Infrastructure.Settings.Config;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Infrastructure.Data;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Tests.Common.Time;     // FakeClock
using TradyStrat.Tests.Fx;              // TestRepo<T>
using TradyStrat.Tests.Specifications;   // InMemoryDb
using Xunit;

namespace TradyStrat.Tests.Settings.UseCases;

public class UpdateSettingUseCaseTests
{
    private static Instrument MakeInstrument(string ticker) => new()
    {
        Id = 0, Ticker = ticker, Name = ticker, Currency = "USD",
        Exchange = "LSE", TimezoneId = "Europe/London",
        Kind = InstrumentKind.Held, AddedAt = DateTime.UtcNow,
    };

    private static async Task<(UpdateSettingUseCase uc, ISettingsService svc)> Build(AppDbContext db, params string[] tickers)
    {
        foreach (var t in tickers) db.Instruments.Add(MakeInstrument(t));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var svc = new SettingsService(new TestRepo<SettingEntry>(db), new SettingsRegistry(), db,
            new FakeClock(new DateTime(2026, 5, 11, 12, 0, 0, DateTimeKind.Utc)));
        var uc = new UpdateSettingUseCase(new SettingsRegistry(), new TestRepo<Instrument>(db), svc,
            NullLogger<UpdateSettingUseCase>.Instance);
        return (uc, svc);
    }

    [Fact]
    public async Task Happy_path_persists_and_returns_timestamp()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var (uc, svc) = await Build(db);

        var ts = await uc.ExecuteAsync(new UpdateSettingInput(SettingsKeys.AnthropicMaxTokens, "2048"), ct);

        ts.ShouldBe(new DateTime(2026, 5, 11, 12, 0, 0, DateTimeKind.Utc));
        (await svc.GetAsync<int>(SettingsKeys.AnthropicMaxTokens, ct)).ShouldBe(2048);
    }

    [Fact]
    public async Task SearchQueries_array_is_normalised_on_write()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var (uc, svc) = await Build(db);

        await uc.ExecuteAsync(new UpdateSettingInput(SettingsKeys.PolymarketSearchQueries, """[ "btc" , "eth" ]"""), ct);

        // Stored value is canonical JSON produced by the descriptor's Format, not the raw input.
        (await svc.GetRawAsync(SettingsKeys.PolymarketSearchQueries, ct)).ShouldBe("""["btc","eth"]""");
    }

    [Theory]
    [InlineData(SettingsKeys.AnthropicMaxTokens, "0")]
    [InlineData(SettingsKeys.AnthropicMaxTokens, "100001")]
    [InlineData(SettingsKeys.AnthropicMaxTokens, "not-a-number")]
    [InlineData(SettingsKeys.PolymarketMaxMarkets, "0")]
    [InlineData(SettingsKeys.PolymarketMinVolumeUsd, "-1")]
    [InlineData(SettingsKeys.PolymarketMaxHorizonDays, "0")]
    [InlineData(SettingsKeys.PolymarketSearchQueries, "[]")]
    [InlineData(SettingsKeys.PolymarketSearchQueries, """["btc"," "]""")]
    [InlineData(SettingsKeys.PolymarketSearchQueries, "not-json")]
    [InlineData(SettingsKeys.AnthropicModel, "")]
    [InlineData(SettingsKeys.TickersFocus, "")]
    public async Task Invalid_values_throw_SettingValidationException(string key, string raw)
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var (uc, _) = await Build(db, "CON3.L");

        await Should.ThrowAsync<SettingValidationException>(() => uc.ExecuteAsync(new UpdateSettingInput(key, raw), ct));
    }

    [Fact]
    public async Task Focus_ticker_must_be_a_known_instrument()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var (uc, _) = await Build(db, "CON3.L");

        await Should.ThrowAsync<SettingValidationException>(() =>
            uc.ExecuteAsync(new UpdateSettingInput(SettingsKeys.TickersFocus, "NOPE"), ct));
    }

    [Fact]
    public async Task Focus_ticker_accepts_a_known_instrument()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var (uc, svc) = await Build(db, "CON3.L", "COIN");

        await uc.ExecuteAsync(new UpdateSettingInput(SettingsKeys.TickersFocus, "COIN"), ct);

        (await svc.GetRawAsync(SettingsKeys.TickersFocus, ct)).ShouldBe("COIN");
    }

    [Fact]
    public async Task Unknown_key_throws_InvalidOperationException()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var (uc, _) = await Build(db);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            uc.ExecuteAsync(new UpdateSettingInput("does.not.exist", "x"), ct));
    }
}
