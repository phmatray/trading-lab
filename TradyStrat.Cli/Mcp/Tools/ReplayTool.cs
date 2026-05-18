using System.ComponentModel;
using ModelContextProtocol.Server;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;

namespace TradyStrat.Cli.Mcp.Tools;

[McpServerToolType]
internal sealed class ReplayTool(
    IUseCase<ReplaySuggestionsInput, ReplayReport> useCase,
    Guards guards,
    IClock clock)
{
    [McpServerTool(Name = "get_replay_report"),
     Description("Re-run the AI prompt against historical snapshots in dry-run mode and return hit-rate / forward-return stats.")]
    public async Task<ReplayReport> GetReplayReport(
        string instrument,
        string from,
        string to,
        CancellationToken ct = default)
    {
        var inst = await guards.ResolveInstrumentOrThrow(instrument, ct);
        var (f, t) = Guards.ResolveDateRange(from, to, defaultBack: 0, clockToday: clock.TodayLocal());
        return await useCase.ExecuteAsync(
            new ReplaySuggestionsInput(inst.Id, f, t, Persist: false, Force: false), ct);
    }
}
