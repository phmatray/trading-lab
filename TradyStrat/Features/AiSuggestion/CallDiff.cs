using TradyStrat.Common.Domain;

namespace TradyStrat.Features.AiSuggestion;

public sealed record CallDiff(
    bool ActionChanged,
    SuggestionAction? PriorAction,
    int? ConvictionDelta,
    IReadOnlyList<string> AddedCitationKeys,
    IReadOnlyList<string> RemovedCitationKeys,
    IReadOnlyList<CitationChange> ChangedCitations,
    string SummaryParagraph)
{
    public static readonly CallDiff None = new(
        ActionChanged: false,
        PriorAction: null,
        ConvictionDelta: null,
        AddedCitationKeys: [],
        RemovedCitationKeys: [],
        ChangedCitations: [],
        SummaryParagraph: "");
}
