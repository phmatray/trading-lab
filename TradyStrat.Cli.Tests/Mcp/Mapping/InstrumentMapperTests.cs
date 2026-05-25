using Shouldly;
using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Cli.Mcp.Mapping;
using TradyStrat.Domain;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Mapping;

public class InstrumentMapperTests
{
    private static Instrument MakeInstrument(string ticker) => Instrument.Existing(
        id:         new InstrumentId(1),
        ticker:     ticker,
        name:       $"{ticker} Corp",
        currency:   Currency.Usd,
        exchange:   Exchange.Of("LSE"),
        timezoneId: TimezoneId.Of("Europe/London"),
        kind:       InstrumentKind.Held,
        addedAt:    new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Maps_focus_ticker_with_role_Focus()
    {
        var inst = MakeInstrument("CON3.L");
        var dto = InstrumentMapper.ToDto(inst, focusTicker: "CON3.L");

        dto.Ticker.ShouldBe("CON3.L");
        dto.DisplayName.ShouldBe("CON3.L Corp");
        dto.Currency.ShouldBe("USD");
        dto.Timezone.ShouldBe("Europe/London");
        dto.Role.ShouldBe(InstrumentRole.Focus);
    }

    [Fact]
    public void Maps_non_focus_ticker_with_role_Context()
    {
        var inst = MakeInstrument("AAPL");
        var dto = InstrumentMapper.ToDto(inst, focusTicker: "CON3.L");

        dto.Ticker.ShouldBe("AAPL");
        dto.Role.ShouldBe(InstrumentRole.Context);
    }

    [Fact]
    public void ToResponse_wraps_list_with_one_focus()
    {
        var instruments = new[]
        {
            MakeInstrument("CON3.L"),
            MakeInstrument("AAPL"),
            MakeInstrument("MSFT"),
        };

        var response = InstrumentMapper.ToResponse(instruments, focusTicker: "CON3.L");

        response.Instruments.Count.ShouldBe(3);
        response.Instruments.Count(i => i.Role == InstrumentRole.Focus).ShouldBe(1);
        response.Instruments.Single(i => i.Role == InstrumentRole.Focus).Ticker.ShouldBe("CON3.L");
        response.Instruments.Where(i => i.Role == InstrumentRole.Context)
            .Select(i => i.Ticker)
            .ShouldBe(["AAPL", "MSFT"]);
    }
}
