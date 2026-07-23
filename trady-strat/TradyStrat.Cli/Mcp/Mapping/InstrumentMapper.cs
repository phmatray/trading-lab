using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Domain;

namespace TradyStrat.Cli.Mcp.Mapping;

internal static class InstrumentMapper
{
    public static InstrumentDto ToDto(Instrument inst, string focusTicker)
        => new(
            Ticker: inst.Ticker,
            DisplayName: inst.Name,
            Currency: inst.Currency.Code,
            Timezone: inst.Timezone.Value,
            Role: inst.Ticker == focusTicker ? InstrumentRole.Focus : InstrumentRole.Context);

    public static InstrumentListResponse ToResponse(IEnumerable<Instrument> instruments, string focusTicker)
        => new(instruments.Select(i => ToDto(i, focusTicker)).ToList());
}
