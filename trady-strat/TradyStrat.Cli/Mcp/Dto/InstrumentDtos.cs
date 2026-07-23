namespace TradyStrat.Cli.Mcp.Dto;

public sealed record InstrumentListResponse(IReadOnlyList<InstrumentDto> Instruments);

public sealed record InstrumentDto(
    string Ticker,
    string DisplayName,
    string Currency,
    string Timezone,
    InstrumentRole Role);

public enum InstrumentRole { Focus, Context }
