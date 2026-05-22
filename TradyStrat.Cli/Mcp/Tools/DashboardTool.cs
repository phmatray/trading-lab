using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using TradyStrat.Application.Dashboard;
using TradyStrat.Application.Dashboard.UseCases;
using TradyStrat.Application.Indicators;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain.Indicators.Services;
using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Cli.Mcp.Mapping;
using TradyStrat.Domain;

namespace TradyStrat.Cli.Mcp.Tools;

[McpServerToolType]
internal sealed class DashboardTool(
    IUseCase<LoadDashboardInput, DashboardViewModel> useCase,
    Guards guards,
    IIndicatorEngine indicators,
    IClock clock,
    IConfiguration config)
{
    [McpServerTool(Name = "get_dashboard"),
     Description("Snapshot of an instrument: price, indicators, zone, today's AI suggestion, position.")]
    public async Task<DashboardSnapshot> GetDashboard(
        string? instrument = null,
        string? asOf = null,
        CancellationToken ct = default)
    {
        var ticker = instrument ?? config["Tickers:Focus"] ?? "CON3.L";
        await guards.ResolveInstrumentOrThrow(ticker, ct);
        var today = clock.TodayLocal();
        var (_, target) = Guards.ResolveDateRange(from: asOf, to: asOf, defaultBack: 0, clockToday: today);
        var isHistorical = target != today;

        var vm = await useCase.ExecuteAsync(new LoadDashboardInput(target, isHistorical), ct);
        var reading = await indicators.ComputeFor(ticker, target, ct);
        return DashboardMapper.ToSnapshot(vm, reading);
    }
}
