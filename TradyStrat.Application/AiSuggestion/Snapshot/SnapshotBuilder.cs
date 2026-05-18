using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TradyStrat.Application.PredictionMarkets;
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

        // Legacy hash payload — preserved exactly so the AiSnapshotServiceTests
        // sentinel "895EED53A280A470" stays stable through the Phase-3 refactor.
        // Phase 6 will replace this single hash with three independent hashes.
        var payload = new { today, snap = Portfolio, tickers, recent, markets };
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOpts.Strict));
        var hash  = Convert.ToHexString(SHA256.HashData(bytes))[..16];

        return new AiSnapshot(
            today, instrumentId, Goal, Portfolio, tickers, recent,
            UsdPerEur, markets, pastSuggestions,
            EnvelopeHash:      hash,
            PromptVersionHash: hash,
            PromptHash:        hash);
    }
}
