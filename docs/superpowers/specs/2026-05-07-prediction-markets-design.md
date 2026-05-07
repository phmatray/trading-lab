# TradyStrat — Prediction-market signal (Polymarket)

**Status:** Design approved · Ready for implementation plan
**Date:** 2026-05-07
**Author:** Philippe Matray (with Claude)
**Depends on:** [`2026-05-06-tradystrat-dashboard-design.md`](./2026-05-06-tradystrat-dashboard-design.md)
**Coexists with:** [`2026-05-07-multi-ticker-foundation-design.md`](./2026-05-07-multi-ticker-foundation-design.md) (orthogonal column on `Suggestions`; no schema conflict)

---

## 1. Purpose & goal

The current AI suggestion sees backward-looking signals only: price, TA
indicators (Bollinger / RSI / SMA / Ichimoku), portfolio state, recent
trades, and FX. It has no view on forward-pricing of crypto-relevant
events — BTC price targets, Coinbase quarterly outcomes, ETF approvals,
macro pivots — even though those events drive CON3.L meaningfully.

This spec adds a **prediction-market signal** sourced from Polymarket:
a curated, deterministic list of active crypto markets is fetched
once per AI call, fed into the snapshot the model sees, and surfaced
on the dashboard as a rail under the Today's Call card. The AI may
cite specific markets it weighed, the same way it cites indicators today.

Goal: make the AI's daily call sensitive to crowd-priced forward
probabilities, event awareness, and tail-risk pricing — and give the
human reading the dashboard transparent insight into what the AI saw
and which odds it weighted.

## 2. Decisions at a glance

| Concern | Decision |
|---|---|
| Source API | **Polymarket only** (Gamma REST API). Public endpoint, no auth. |
| Selection mechanism | **Pre-filter, then AI cites.** System fetches; AI does not call out itself. |
| Filter shape | **Hardcoded tags, appsettings tunables** for `MaxMarkets`, `MinVolumeUsd`, `MaxHorizonDays`. |
| Tag set (initial) | `bitcoin`, `crypto`, `coinbase`, `ethereum`. Code-level. Tweakable in `appsettings.json`. |
| Refresh policy | **Snapshot-only.** Odds captured at AI-call time; persisted with the Suggestion; never refreshed for that row. |
| Multi-outcome markets | **Dropped during normalization.** Phase 1 supports binary YES/NO only. |
| Caching | **None.** ≤1 fetch per AI call (≈1/day plus rare re-runs). Gamma's free public tier handles it. |
| Background service | **None.** No new HostedService. |
| Storage shape | **One new TEXT column** `Suggestions.MarketSnapshotJson`, holding `{markets, cited}`. NULL for pre-feature rows or fetch failures. |
| Migration | **Single EF migration**, applied automatically on startup. |
| Backfill | **None.** Existing Suggestions stay markets-less. New Suggestions only. |
| AI tool change | One added param `market_citations: IReadOnlyList<MarketCitation>?` on `submit_suggestion`. |
| `PromptHash` | **Includes Markets** in input. Day-to-day odds drift produces a real `CallDiff` row. |
| Dashboard surface | **Dedicated rail** between `TodaysCallCard` and `CitationsBlock`. Cited tiles get a `★` badge. |
| Failure mode | **Graceful.** Polymarket failure → snapshot with `Markets = []` → AI proceeds → `MarketSnapshotJson = NULL`. AI never fails because of Polymarket. |
| Citation hygiene | AI-cited slugs not present in snapshot are **dropped server-side** with a warning log; suggestion still saved. |
| Test stack | xunit.v3 + Shouldly + EF Core InMemory + captured Gamma JSON fixtures. **No bUnit.** |
| Live API in tests | Not exercised. Same posture as live Yahoo / live Anthropic today. |

## 3. Domain model

### 3.1 New: `PredictionMarket` (value record)

```csharp
public sealed record PredictionMarket(
    string Slug,
    string Question,
    decimal Probability,                 // 0..1, the YES outcome price
    DateOnly EndDate,
    decimal VolumeUsd,
    IReadOnlyList<string> Tags);
```

Lives in `Features/PredictionMarkets/PredictionMarket.cs`.

### 3.2 New: `MarketCitation` (value record)

```csharp
public sealed record MarketCitation(
    string Slug,                         // FK back into the Markets list of the same MarketSnapshot
    string Claim);                       // one-line reason AI weighed this market
```

Lives in `Features/PredictionMarkets/MarketCitation.cs`.

### 3.3 New: `MarketSnapshot` (value record)

```csharp
public sealed record MarketSnapshot(
    IReadOnlyList<PredictionMarket> Markets,
    IReadOnlyList<MarketCitation> Cited);
```

Serialized into `Suggestions.MarketSnapshotJson` via `JsonOpts.Strict`.
Lives in `Features/PredictionMarkets/MarketSnapshot.cs`.

### 3.4 Changed: `AiSnapshot`

```csharp
public sealed record AiSnapshot(
    DateOnly Today,
    GoalConfig Goal,
    PortfolioSnapshot Portfolio,
    IReadOnlyList<TickerContext> Tickers,
    IReadOnlyList<TradeRecent> RecentTrades,
    decimal? UsdPerEur,
    IReadOnlyList<PredictionMarket> Markets,    // NEW (may be empty)
    string PromptHash);
```

`PromptHash` input includes the new `Markets` list. The hash payload
ordering is: `today, snap, tickers, recent, markets`. Adding markets
to the hash means a same-day re-run with shifted Polymarket prices
correctly surfaces a `CallDiff` change row.

### 3.5 Changed: `Suggestion` (entity)

Adds one nullable column:

```
MarketSnapshotJson  TEXT NULL
```

NULL semantics:
- Row created before this feature shipped (no backfill).
- Polymarket fetch failed at call time (graceful degradation, see §10).
- Polymarket fetch succeeded but the post-filter list was empty.

When non-NULL, holds `MarketSnapshot` JSON.

## 4. Module shape

```
TradyStrat/Features/PredictionMarkets/
  PredictionMarket.cs
  MarketCitation.cs
  MarketSnapshot.cs
  IPredictionMarketProvider.cs
  PolymarketOptions.cs
  Providers/
    PolymarketGammaProvider.cs
```

Wired in a new `PredictionMarketsModule.cs` under `Modules/`, following
the existing `IAppModule` pattern. Registers:

- `PolymarketOptions` bound from `Polymarket` config section.
- `IPredictionMarketProvider` → `PolymarketGammaProvider`.
- `HttpClient` for the provider via `IHttpClientFactory` named `"polymarket"` with
  base address from `PolymarketOptions.BaseUrl` and a 10-second timeout.

`Program.cs` remains a single line.

## 5. Provider contract

```csharp
public interface IPredictionMarketProvider
{
    Task<IReadOnlyList<PredictionMarket>> GetMarketsAsync(
        PolymarketOptions options,
        CancellationToken ct);
}
```

Returns the post-filter, post-normalization list (≤ `MaxMarkets`).
On failure throws `PolymarketUnavailableException`.

## 6. Polymarket Gamma API integration

### 6.1 Endpoint

`GET https://gamma-api.polymarket.com/markets`

Query parameters used:
- `active=true`
- `closed=false`
- `tag_slug={tag}` — one HTTP call per tag in `PolymarketOptions.Tags`
- `order=volume`
- `ascending=false`
- `limit={MaxMarkets * 2}` per tag (over-fetch buffer for post-filtering)

### 6.2 Field mapping

| Gamma field | `PredictionMarket` field | Notes |
|---|---|---|
| `slug` | `Slug` | |
| `question` | `Question` | |
| `outcomePrices[indexOf("Yes")]` | `Probability` | parsed from string to decimal |
| `endDate` | `EndDate` | ISO-8601, take date part |
| `volume` | `VolumeUsd` | parsed from string to decimal |
| `tags[].slug` | `Tags` | flatten |

### 6.3 Normalization rules

A market is dropped (logged at Debug) if:
- `outcomes` is not exactly `["Yes", "No"]` (multi-outcome unsupported in Phase 1).
- `outcomePrices` cannot be parsed to two decimals summing to ~1.0 (±0.01 tolerance for orderbook ticks).
- `endDate` cannot be parsed.
- `volume` cannot be parsed or is < 0.

### 6.4 Final filter pipeline

After all tag responses are received and merged:
1. **Dedup** by `Slug` (a market may belong to multiple tags).
2. **Filter** `VolumeUsd ≥ MinVolumeUsd`.
3. **Filter** `EndDate ≤ Today + MaxHorizonDays`.
4. **Order** `VolumeUsd desc`.
5. **Take** `MaxMarkets`.

The result is the list returned by `GetMarketsAsync`.

## 7. AI integration

### 7.1 Tool param addition

`SuggestionService.AskAsync` extends the `submit_suggestion`
delegate signature with one new nullable parameter:

```csharp
(SuggestionAction action, decimal? quantity_hint, decimal? max_price_hint,
 int conviction, string rationale,
 IReadOnlyList<Citation>? citations,
 IReadOnlyList<MarketCitation>? market_citations) => …
```

### 7.2 System-prompt addendum

The existing prompt grows by one paragraph (appended verbatim to the
existing `SystemPrompt` constant):

> You may also cite Polymarket markets you weighed.
> Each `market_citations[].slug` MUST appear in the snapshot's `markets[]`.
> Cite a market only when you actually weighted it; not every market needs a citation.

### 7.3 Citation hygiene

When the tool callback fires, before constructing the `Suggestion`:

```csharp
var validSlugs = snapshot.Markets.Select(m => m.Slug).ToHashSet();
var cleaned = (market_citations ?? [])
    .Where(c => {
        var ok = validSlugs.Contains(c.Slug);
        if (!ok) LogUnknownMarketCitation(log, c.Slug);
        return ok;
    })
    .ToList();
```

`MarketSnapshotJson` is built only when the snapshot included at least
one market. Otherwise the column stays NULL for that row.

```csharp
string? marketJson = snapshot.Markets.Count == 0
    ? null
    : JsonSerializer.Serialize(new MarketSnapshot(snapshot.Markets, cleaned), JsonOpts.Strict);
```

### 7.4 `PromptHash` change

`SnapshotFactory.HashPrompt` is updated:

```csharp
private static string HashPrompt(DateOnly today, PortfolioSnapshot snap,
    IEnumerable<TickerContext> tickers, IEnumerable<TradeRecent> recent,
    IEnumerable<PredictionMarket> markets)
{
    var payload = new { today, snap, tickers, recent, markets };
    var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOpts.Strict));
    return Convert.ToHexString(SHA256.HashData(bytes))[..16];
}
```

This deliberately changes existing hash values for any future re-run
of an old day. Not a problem because old `Suggestion` rows already
store their hash; CallDiff compares against that stored hash.

## 8. Configuration

`appsettings.json` gains:

```json
"Polymarket": {
  "BaseUrl":         "https://gamma-api.polymarket.com",
  "Tags":            ["bitcoin", "crypto", "coinbase", "ethereum"],
  "MaxMarkets":      10,
  "MinVolumeUsd":    50000,
  "MaxHorizonDays":  365
}
```

Bound to:

```csharp
public sealed class PolymarketOptions
{
    public required string BaseUrl { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required int MaxMarkets { get; init; }
    public required decimal MinVolumeUsd { get; init; }
    public required int MaxHorizonDays { get; init; }
}
```

No secret. The Gamma API requires no key for read access.

## 9. Dashboard surface

### 9.1 New component

`TradyStrat/Features/Dashboard/Components/MarketsRail.razor` (+ `.razor.cs` + `.razor.css`).

```razor
@using TradyStrat.Features.PredictionMarkets

@if (Snapshot is { Markets.Count: > 0 })
{
    <div class="markets-rail">
        <div class="rail-label">Polymarket · @Snapshot.Markets.Count markets</div>
        <div class="market-tiles">
            @foreach (var m in Snapshot.Markets)
            {
                var citation = _bySlug.GetValueOrDefault(m.Slug);
                <div class="tile @(citation is not null ? "cited" : "")">
                    <div class="prob">@m.Probability.ToString("P0", FrFr)</div>
                    <div class="question">@m.Question</div>
                    <div class="meta">
                        @m.EndDate.ToString("d MMM yyyy", FrFr) ·
                        vol $@m.VolumeUsd.ToString("N0", FrFr)
                    </div>
                    @if (citation is not null)
                    {
                        <div class="claim">★ @citation.Claim</div>
                    }
                </div>
            }
        </div>
    </div>
}

@code {
    [Parameter, EditorRequired] public MarketSnapshot? Snapshot { get; set; }
    private Dictionary<string, MarketCitation> _bySlug = new();
    protected override void OnParametersSet()
        => _bySlug = (Snapshot?.Cited ?? []).ToDictionary(c => c.Slug);
    private static readonly System.Globalization.CultureInfo FrFr = new("fr-FR");
}
```

### 9.2 ViewModel extension

`DashboardViewModel` gains:

```csharp
public MarketSnapshot? MarketSnapshot { get; init; }
```

### 9.3 Page wiring

`DashboardPage.razor` adds the rail between `TodaysCallCard` and `CitationsBlock`:

```razor
<div class="hero-row">
    <HeroCapital ... />
    <TodaysCallCard ... />
</div>
<MarketsRail Snapshot="vm.MarketSnapshot" />
@if (vm.TodaysCall is not null) { <CitationsBlock ... /> }
```

When `MarketSnapshot` is null (NULL column or feature off), the
component renders nothing — no empty state, no banner.

### 9.4 Styling notes

- Tile width: roughly matches `CitationsBlock`'s row width; flex-wrap rows of 3 on desktop, 1 column on narrow viewports.
- `.tile.cited` adds a subtle 1px outline + brighter text on the probability.
- Probability typography matches existing numeric callouts (`.num` family).

### 9.5 `LoadDashboardUseCase` change

After loading `Suggestion`, deserialize:

```csharp
MarketSnapshot? marketSnap = null;
if (sug?.MarketSnapshotJson is { Length: > 0 } json)
{
    try
    {
        marketSnap = JsonSerializer.Deserialize<MarketSnapshot>(json, JsonOpts.Strict);
    }
    catch (JsonException ex)
    {
        LogMarketSnapshotMalformed(log, ex);
    }
}
```

The dashboard renders correctly even if a row has malformed JSON;
the rail is just absent.

## 10. Failure handling

Mirrors the existing `FxRateUnavailableException` pattern in `SnapshotFactory`.

### 10.1 New typed exception

```csharp
// Common/Exceptions/PolymarketUnavailableException.cs
public sealed class PolymarketUnavailableException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
```

### 10.2 Failure matrix

Tag fetches are sequential (or `Task.WhenAll` — implementer's choice;
no semantic difference). **Any one tag failing fails the whole fetch**:
the provider throws `PolymarketUnavailableException` and the partial
set already collected is discarded. Rationale: silently weighting a
partial market set can mislead the AI; "no markets at all" is a
clearer state to render and reason about.

| Failure | Where caught | Behavior |
|---|---|---|
| HTTP 5xx / network / timeout (any tag) | `PolymarketGammaProvider` → throws `PolymarketUnavailableException` | `SnapshotFactory` catches → `Markets = []` → AI runs without markets → `MarketSnapshotJson` saved as NULL |
| HTTP 429 rate-limited | same | same |
| 200 OK, unparseable JSON | provider → throws | same |
| 200 OK, empty post-filter result | provider returns `[]` (not exception) | `Markets = []`, `MarketSnapshotJson = NULL`. Warning log surfaces the empty filter result |
| AI cites slug not in snapshot | `SuggestionService.AskAsync` validation | offending citations dropped, warning logged, suggestion saved cleanly |
| Dashboard reads malformed JSON | `LoadDashboardUseCase` catches `JsonException` | logs, treats as null, rail not rendered |

### 10.3 Logging

`LoggerMessage` source-generators, matching the existing
`SuggestionService.LogCallFailed` pattern:

```csharp
[LoggerMessage(Level = LogLevel.Warning, Message = "Polymarket unavailable, snapshot will omit markets")]
private static partial void LogPolymarketUnavailable(ILogger logger, Exception ex);

[LoggerMessage(Level = LogLevel.Warning,
    Message = "Polymarket filter returned 0 markets — adjust Tags / MinVolumeUsd / MaxHorizonDays")]
private static partial void LogPolymarketEmpty(ILogger logger);

[LoggerMessage(Level = LogLevel.Warning, Message = "AI cited unknown market slug {Slug}; dropped")]
private static partial void LogUnknownMarketCitation(ILogger logger, string slug);

[LoggerMessage(Level = LogLevel.Warning, Message = "MarketSnapshotJson malformed; rail will not render")]
private static partial void LogMarketSnapshotMalformed(ILogger logger, Exception ex);
```

## 11. Database migration

Single EF migration: `AddSuggestionMarketSnapshotJson`.

```csharp
migrationBuilder.AddColumn<string>(
    name:        "MarketSnapshotJson",
    table:       "Suggestions",
    type:        "TEXT",
    nullable:    true);
```

No index. No data backfill. Applied automatically on startup
(existing migration runner pattern).

## 12. Testing

Test stack: xunit.v3 + Shouldly + EF Core InMemory + captured Gamma
JSON fixtures. **No bUnit.** No live external services.

### 12.1 Test inventory

```
TradyStrat.Tests/
  Features/
    PredictionMarkets/
      Providers/
        PolymarketGammaProviderTests.cs        ← HttpMessageHandler stub + fixtures
      PolymarketNormalizationTests.cs           ← multi-outcome drop, parse failures, tag flatten
      PolymarketFilterTests.cs                  ← volume/horizon/sort/take/dedup pure logic
      Fixtures/
        Polymarket/
          gamma-markets-bitcoin.json
          gamma-markets-multi-outcome.json
          gamma-markets-empty.json
          gamma-markets-malformed.json
    AiSuggestion/
      Snapshot/
        SnapshotFactoryTests.cs (extend)        ← markets in snapshot
                                                ← PromptHash includes markets
                                                ← tolerates PolymarketUnavailableException
      AiSuggestionServiceCitationTests.cs       ← unknown-slug citations dropped
                                                ← MarketSnapshotJson NULL when Markets empty
      CallDiff/
        CallDiffTests.cs (extend)               ← market drift produces a diff row
    Dashboard/
      LoadDashboardUseCaseTests.cs (extend)     ← MarketSnapshotJson deserialized correctly
                                                ← malformed JSON tolerated (logged, null returned)
```

### 12.2 Test fixtures

Captured from real Gamma responses, then sanitized down to ≤3 markets
each to keep diffs reviewable. `gamma-markets-multi-outcome.json`
holds at least one binary and one multi-outcome market to exercise
the drop path.

### 12.3 Negative-case coverage

Every row in the §10.2 failure matrix has a corresponding test.

## 13. Backwards compatibility

- **Existing Suggestions:** untouched. `MarketSnapshotJson` is NULL;
  the rail does not render for those days. CallDiff between two old
  rows is unaffected (neither had markets); CallDiff between an old
  row and a new row simply has no market drift to report (the old
  side has no market state to compare against).
- **Pre-feature `appsettings.json`:** still works. The
  `PredictionMarketsModule` registers `PolymarketOptions` with default
  values applied if the section is missing. (Concretely: a sealed
  default instance returned when `Configuration.GetSection("Polymarket").Exists()` is false.)
- **Phase 1 multi-ticker spec:** orthogonal. That spec adds
  `Trades.InstrumentId` and the `Instruments` table; this spec adds
  `Suggestions.MarketSnapshotJson`. Different tables / different columns.
  Migration order: whichever lands first.
- **Phase 2 multi-ticker AI spec (future):** will add
  `Suggestions.InstrumentId` FK. Coexists with `MarketSnapshotJson` —
  one Suggestion row per (ForDate, InstrumentId), each with its own
  market snapshot.

## 14. Out of scope (Phase 1)

- Kalshi as an additional source. The `IPredictionMarketProvider`
  interface is shaped to allow it; not implemented here.
- Multi-outcome markets.
- Live (non-snapshot) odds on the dashboard.
- Background polling / hosted service.
- Per-ticker market filtering. (All filtering is crypto-broad; Phase 2
  multi-ticker AI may revisit.)
- Charting market-probability time series.
- AI initiating its own market searches.
- A Settings UI for editing tags / threshold.
- Backfilling historical Suggestions with retroactive market data.
- Polymarket auth or write-side (trading) integration.

## 15. Patterns referenced

- **Adapter** — `PolymarketGammaProvider` adapts the Gamma REST shape to
  the `PredictionMarket` domain record. Mirrors `YahooFxProvider`.
- **Strategy seam** — `IPredictionMarketProvider` is the implicit
  Strategy slot if Kalshi is added. Single implementation today (YAGNI).
- **Specification** — N/A: no DB queries against a markets table
  (snapshots live as JSON on `Suggestion`).
- **Graceful degradation** — same family as `FxRateUnavailableException`:
  feature is additive, never blocks the AI call.

## 16. Open questions

None. Decisions captured in §2; defer Kalshi multi-source design to
its own spec when the need arises.

---

*End of design.*
