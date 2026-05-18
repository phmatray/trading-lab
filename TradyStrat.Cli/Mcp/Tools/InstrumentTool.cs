using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Application.UseCases;
using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Cli.Mcp.Mapping;

namespace TradyStrat.Cli.Mcp.Tools;

[McpServerToolType]
public sealed class InstrumentTool(
    ListInstrumentsUseCase listInstruments,
    IConfiguration config)
{
    [McpServerTool(Name = "list_instruments"),
     Description("List all instruments TradyStrat tracks.")]
    public async Task<InstrumentListResponse> ListInstruments(CancellationToken ct = default)
    {
        var instruments = await listInstruments.ExecuteAsync(Unit.Value, ct);
        var focus = config["Tickers:Focus"] ?? "CON3.L";
        return InstrumentMapper.ToResponse(instruments, focus);
    }
}
