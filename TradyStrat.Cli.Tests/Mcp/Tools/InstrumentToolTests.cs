using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Cli.Mcp.Tools;
using TradyStrat.Domain;
using TradyStrat.TestKit;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Tools;

public sealed class InstrumentToolTests
{
    private static Instrument MakeInstrument(int id, string ticker) => new()
    {
        Id = id,
        Ticker = ticker,
        Name = ticker,
        Currency = "USD",
        Exchange = "TEST",
        TimezoneId = "Europe/London",
        Kind = InstrumentKind.Held,
        AddedAt = DateTime.UtcNow,
    };

    private static async Task<InstrumentTool> BuildToolAsync(
        string focus,
        CancellationToken ct,
        params Instrument[] instruments)
    {
        var db = InMemoryDb.Create();
        db.Instruments.AddRange(instruments);
        await db.SaveChangesAsync(ct);

        var repo = new TestRepo<Instrument>(db);
        var useCase = new ListInstrumentsUseCase(repo, NullLogger<ListInstrumentsUseCase>.Instance);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tickers:Focus"] = focus,
            })
            .Build();

        return new InstrumentTool(useCase, config);
    }

    [Fact]
    public async Task Returns_all_three_with_focus_role_on_focus_ticker()
    {
        var ct = TestContext.Current.CancellationToken;
        var instruments = new[]
        {
            MakeInstrument(1, "CON3.L"),
            MakeInstrument(2, "COIN"),
            MakeInstrument(3, "BTC-USD"),
        };

        var tool = await BuildToolAsync("CON3.L", ct, instruments);
        var result = await tool.ListInstruments(ct);

        result.Instruments.Count.ShouldBe(3);

        var focus = result.Instruments.Single(i => i.Role == InstrumentRole.Focus);
        focus.Ticker.ShouldBe("CON3.L");

        result.Instruments
            .Where(i => i.Role == InstrumentRole.Context)
            .Count()
            .ShouldBe(2);
    }
}
