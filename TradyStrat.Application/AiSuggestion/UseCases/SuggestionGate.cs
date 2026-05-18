namespace TradyStrat.Application.AiSuggestion.UseCases;

/// <summary>
/// Process-wide mutex serializing today's-suggestion writes.
///
/// Two concurrent dashboard loads (or a dashboard load racing a Re-run AI
/// click) both pass the "row doesn't exist" check, both call the AI, then
/// both try to INSERT with the same ForDate — the second hits the
/// UQ(ForDate) constraint. The gate forces the second request to wait
/// until the first finishes, then re-check; if the first already
/// inserted, the second returns that row instead of calling the AI again.
///
/// Static because the use cases are scoped per request, but we need a
/// shared lock across all scopes.
/// </summary>
internal static class SuggestionGate
{
    public static readonly SemaphoreSlim Instance = new(1, 1);
}
