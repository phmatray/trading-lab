using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Application.UseCases;
using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Cli.Mcp.Mapping;
using TradyStrat.Domain;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Cli.Mcp.Tools;

[McpServerToolType]
internal sealed class SuggestionTool(
    IUseCase<QuerySuggestionsInput, QuerySuggestionsOutput> useCase,
    Guards guards,
    IClock clock,
    IConfiguration config)
{
    [McpServerTool(Name = "query_suggestions"),
     Description("Past AI suggestions for an instrument with action, conviction, and outcome.")]
    public async Task<SuggestionPage> QuerySuggestions(
        string? instrument = null,
        string? from = null,
        string? to = null,
        string? action = null,
        int limit = 30,
        CancellationToken ct = default)
    {
        if (limit < 1 || limit > 100)
            throw new ArgumentException($"limit must be between 1 and 100 (got {limit}).");

        SuggestionAction? parsedAction = null;
        if (action is not null)
        {
            if (!Enum.TryParse<SuggestionAction>(action, ignoreCase: false, out var a))
                throw new ArgumentException(
                    $"action must be one of {string.Join(", ", Enum.GetNames<SuggestionAction>())} (got '{action}').");
            parsedAction = a;
        }

        var ticker = instrument ?? config["Tickers:Focus"] ?? "CON3.L";
        var inst = await guards.ResolveInstrumentOrThrow(ticker, ct);
        var (f, t) = Guards.ResolveDateRange(from, to, defaultBack: 90, clockToday: clock.TodayLocal());

        var output = await useCase.ExecuteAsync(
            new QuerySuggestionsInput(inst.Id, f, t, parsedAction, limit), ct);
        return SuggestionMapper.ToPage(output, ticker, f, t);
    }
}
