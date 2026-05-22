using TradyStrat.Domain.Portfolio;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.Snapshot;

/// <summary>
/// Mutable accumulator for an <see cref="AiSnapshot"/>. Each section provider
/// contributes one slice. <see cref="Build"/> seals the accumulated state into
/// an immutable record and computes the hashes.
///
/// Phase 3 (current): all three hash fields (PromptHash, EnvelopeHash,
/// PromptVersionHash) carry the legacy single-payload hash so the sentinel
/// "895EED53A280A470" stays stable. Phase 6 replaces this with the three
/// independent hashes per spec §5.3.
/// </summary>
public sealed class SnapshotBuilder
{
    private static readonly string[] FocusShapeKeys = ["instrument_id", "recent_suggestions"];

    public GoalConfig? Goal { get; set; }
    public PortfolioSnapshot? Portfolio { get; set; }
    public List<TickerContext> Tickers { get; } = [];
    public List<TradeRecent> RecentTrades { get; } = [];
    public decimal? UsdPerEur { get; set; }
    public IReadOnlyList<PredictionMarket> Markets { get; set; } = [];
    public List<PastSuggestionRow> RecentSuggestions { get; } = [];

    public AiSnapshot Build(DateOnly today, int instrumentId)
    {
        if (Goal is null)      throw new InvalidOperationException("GoalSection did not run");
        if (Portfolio is null) throw new InvalidOperationException("PortfolioSection did not run");

        var tickers         = Tickers.ToArray();
        var recent          = RecentTrades.ToArray();
        var markets         = Markets;
        var pastSuggestions = RecentSuggestions.ToArray();

        // Envelope = stable across instruments on the same day. Spec §5.1.
        var envelope = new
        {
            today,
            goal = Goal,
            portfolio = Portfolio,
            tickers,
            recent_trades = recent,
            usd_per_eur = UsdPerEur,
            markets,
        };

        // Focus = per-instrument, includes the history block.
        var focus = new
        {
            instrument_id = instrumentId,
            recent_suggestions = pastSuggestions,
        };

        var envelopeHash = Hash(envelope);
        var promptHash   = Hash(new { envelope, focus });

        // PromptVersionHash covers the prompt-template surface, not the data values.
        // Phase 1 includes only the focus shape's keys; a follow-up should pull in
        // system_prompt + tool_def signature once SuggestionService exposes those.
        var promptVersionHash = Hash(new { focus_shape_keys = FocusShapeKeys });

        return new AiSnapshot(
            today, instrumentId, Goal, Portfolio, tickers, recent,
            UsdPerEur, markets, pastSuggestions,
            EnvelopeHash:      envelopeHash,
            PromptVersionHash: promptVersionHash,
            PromptHash:        promptHash);
    }

    private static string Hash(object payload)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOpts.Strict));
        return Convert.ToHexString(SHA256.HashData(bytes))[..16];
    }
}
