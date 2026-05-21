using TradyStrat.Domain;

namespace TradyStrat.Application.Dashboard;

/// <summary>
/// Live state of an AI suggestion on the dashboard. <c>null</c> (at the
/// reference site) means "no suggestion expected" (watchlist instruments,
/// or historical-mode loads where the row simply doesn't exist).
/// </summary>
public abstract record SuggestionState
{
    public sealed record Pending : SuggestionState;
    public sealed record Ready(Suggestion Suggestion) : SuggestionState;
    public sealed record Failed(string Reason) : SuggestionState;
}
