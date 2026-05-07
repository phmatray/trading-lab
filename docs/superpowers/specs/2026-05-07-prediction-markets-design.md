# TradyStrat — Prediction-market signal (Polymarket)

**Status:** Design approved (revised after deep review) · Ready for implementation plan
**Date:** 2026-05-07 (revised same day)
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
| `PromptHash` | **Includes Markets** in input. Audit field only — no consumer reads it. CallDiff is **not** extended for market drift in Phase 1; that's deliberate scope-trim. |
| HTTP client | **Typed** `AddHttpClient<IPredictionMarketProvider, PolymarketGammaProvider>(...)` with `.AddStandardResilienceHandler()`. Matches FxModule. |
| Tag fetch concurrency | **`Task.WhenAll` over the tag list.** Worst-case latency = single tag's timeout, not N × timeout. |
| Gamma JSON parsing | **Raw `JsonDocument` traversal in the provider** (camelCase, matches `YahooFxProvider`). `JsonOpts.Strict` is reserved for our internal snake_case `MarketSnapshotJson` round-trip. |
| Citation dedup | The AI may return duplicate slugs; hygiene dedupes (first wins). System prompt also says "cite each market at most once." |
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
    IReadOnlyList<MarketCitation> Cited)
{
    public static readonly MarketSnapshot Empty = new([], []);
}
```

Mirrors the `CallDiff.None` Null Object (`Features/AiSuggestion/CallDiff/CallDiff.cs:14`).
The dashboard render path can treat `Empty` and a populated snapshot uniformly
(`Snapshot.Markets.Count > 0` test).

Serialized into `Suggestions.MarketSnapshotJson` via `JsonOpts.Strict`
(snake_case — our internal round-trip format, distinct from Polymarket's
camelCase wire format; see §6).
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

`PromptHash` input includes the new `Markets` list (payload ordering:
`today, snap, tickers, recent, markets`). `PromptHash` is a **write-only
audit field** today — no production consumer reads it (`grep -rn PromptHash`
returns only writes plus equality assertions in tests). Including markets
keeps the hash a complete fingerprint of model input. **Phase 1 does not
extend `CallDiff` to detect market drift**; if that capability is wanted
later, it's a follow-up spec (new fields on the `CallDiff` record + new
`CallDiffBuilder` logic over `MarketSnapshot`).

### 3.5 Changed: `Suggestion` (entity)

C# property added to `Common/Domain/Suggestion.cs`:

```csharp
public string? MarketSnapshotJson { get; set; }
```

Mirrors the existing `CitationsJson` shape (also `string?`, JSON-encoded).

EF config (`Data/Configurations/SuggestionConfiguration.cs`): no
`HasMaxLength` — match `CitationsJson`'s open-ended config (precedent there;
distinct from `PromptHash` which is bounded at 128).

Migration adds:

```
MarketSnapshotJson  TEXT NULL
```

NULL semantics:
- Row created before this feature shipped (no backfill).
- Polymarket fetch failed at call time (graceful degradation, see §10).
- Polymarket fetch succeeded but the post-filter list was empty.

When non-NULL, holds `MarketSnapshot` JSON serialized via `JsonOpts.Strict`.

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
the existing `IAppModule` pattern. Mirrors `FxModule.cs:11` exactly:

```csharp
public sealed class PredictionMarketsModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        var options = PolymarketOptionsBinder.Read(builder.Configuration);
        builder.Services.AddSingleton(options);

        builder.Services
            .AddHttpClient<IPredictionMarketProvider, PolymarketGammaProvider>(c =>
            {
                c.BaseAddress = new Uri(options.BaseUrl);
                c.Timeout     = TimeSpan.FromSeconds(10);
                c.DefaultRequestHeaders.UserAgent.ParseAdd("TradyStrat/1.0");
            })
            .AddStandardResilienceHandler();
    }
}
```

`PolymarketOptionsBinder.Read(...)` reads each key with `??` defaults, so
a missing `Polymarket` section yields a fully populated `PolymarketOptions`
instance (no `required` modifiers — see §8). At-startup validation is
performed inside `Read(...)`: throws if `MaxMarkets ≤ 0`, `MinVolumeUsd < 0`,
or `MaxHorizonDays ≤ 0`.

`.AddStandardResilienceHandler()` (Microsoft.Extensions.Http.Resilience)
gives the provider the same retry / circuit-breaker / per-attempt-timeout
behavior FX and price feed already get. Without it, transient blips would
incorrectly trigger the §10 graceful-degradation path.

`Program.cs` remains a single line.

## 5. Provider contract

```csharp
public interface IPredictionMarketProvider
{
    Task<IReadOnlyList<PredictionMarket>> GetMarketsAsync(CancellationToken ct);
}

public sealed class PolymarketGammaProvider(
    HttpClient http,
    PolymarketOptions options)
    : IPredictionMarketProvider
{
    public Task<IReadOnlyList<PredictionMarket>> GetMarketsAsync(CancellationToken ct) => …
}
```

Constructor-injected configuration matches the project pattern
(`YahooFxProvider(HttpClient http)` reads its config from the registered
`HttpClient.BaseAddress`; `PolymarketGammaProvider` extends that pattern
to a typed options record because the filter has more knobs than a single
URL).

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

Tag fetches dispatched via **`Task.WhenAll`** — single tag's timeout
(10s, set on the typed HttpClient) caps the total. Sequential fetches
would put `Tags.Count × 10s` on the AI critical path worst-case.

### 6.2 JSON parsing

Polymarket Gamma returns **camelCase** field names (`endDate`,
`outcomePrices`, `outcomes`, `tags`). `JsonOpts.Strict` uses
`SnakeCaseLower` and is reserved for our internal `MarketSnapshotJson`
round-trip — **not** for the wire format. The provider parses the
response with raw `JsonDocument` traversal (matches the `YahooFxProvider`
shape verbatim — see `Features/Fx/Providers/YahooFxProvider.cs:29`):

```csharp
using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
foreach (var el in doc.RootElement.EnumerateArray())
{
    var slug      = el.GetProperty("slug").GetString();
    var question  = el.GetProperty("question").GetString();
    var endDate   = el.GetProperty("endDate").GetDateTime();
    // outcomes and outcomePrices are stringified JSON arrays in Gamma's payload —
    //   e.g. outcomes: "[\"Yes\", \"No\"]"
    //        outcomePrices: "[\"0.32\", \"0.68\"]"
    // Inner-parse via JsonDocument.Parse on the string value.
    …
}
```

This isolates wire-format brittleness inside the provider. Anything
downstream sees only `PredictionMarket` records.

### 6.3 Field mapping

| Gamma field | `PredictionMarket` field | Notes |
|---|---|---|
| `slug` | `Slug` | |
| `question` | `Question` | |
| `outcomePrices[indexOf("Yes")]` | `Probability` | parsed from string to decimal |
| `endDate` | `EndDate` | ISO-8601, take date part |
| `volume` | `VolumeUsd` | parsed from string to decimal |
| `tags[].slug` | `Tags` | flatten |

### 6.4 Normalization rules

A market is dropped (logged at Debug) if:
- `outcomes` is not exactly `["Yes", "No"]` (multi-outcome unsupported in Phase 1).
- `outcomePrices` cannot be parsed to two decimals summing to ~1.0 (±0.01 tolerance for orderbook ticks).
- `endDate` cannot be parsed.
- `volume` cannot be parsed or is < 0.

### 6.5 Final filter pipeline

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
> Cite each market at most once.
> Cite a market only when you actually weighted it; not every market needs a citation.

### 7.3 Citation hygiene

When the tool callback fires, before constructing the `Suggestion`:

```csharp
var validSlugs = snapshot.Markets.Select(m => m.Slug).ToHashSet();

// Drop unknown slugs, then dedupe (first claim per slug wins).
// AI may legally return duplicates despite the prompt rule; a render-time
// ToDictionary on the dashboard would otherwise throw.
var cleaned = (market_citations ?? [])
    .Where(c =>
    {
        if (validSlugs.Contains(c.Slug)) return true;
        LogUnknownMarketCitation(log, c.Slug);
        return false;
    })
    .GroupBy(c => c.Slug)
    .Select(g => g.First())
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

This changes the hash format. **Safe** because no production consumer
reads `PromptHash` (verified by `grep -rn PromptHash` — only writes plus
test equality assertions). The hash is a write-only audit field; flipping
its format breaks nothing.

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
public sealed record PolymarketOptions(
    string                BaseUrl,
    IReadOnlyList<string> Tags,
    int                   MaxMarkets,
    decimal               MinVolumeUsd,
    int                   MaxHorizonDays);

internal static class PolymarketOptionsBinder
{
    public static PolymarketOptions Read(IConfiguration cfg)
    {
        var s = cfg.GetSection("Polymarket");
        var opts = new PolymarketOptions(
            BaseUrl:        s["BaseUrl"] ?? "https://gamma-api.polymarket.com",
            Tags:           s.GetSection("Tags").Get<string[]>()
                              ?? ["bitcoin", "crypto", "coinbase", "ethereum"],
            MaxMarkets:     s.GetValue("MaxMarkets",     10),
            MinVolumeUsd:   s.GetValue("MinVolumeUsd",   50_000m),
            MaxHorizonDays: s.GetValue("MaxHorizonDays", 365));

        if (opts.MaxMarkets     <= 0) throw new ArgumentOutOfRangeException(nameof(opts.MaxMarkets));
        if (opts.MinVolumeUsd   <  0) throw new ArgumentOutOfRangeException(nameof(opts.MinVolumeUsd));
        if (opts.MaxHorizonDays <= 0) throw new ArgumentOutOfRangeException(nameof(opts.MaxHorizonDays));
        return opts;
    }
}
```

No `required` modifiers — defaults preserve startup when the
`Polymarket` section is missing entirely (matches the §13
graceful-default promise). Validation runs at module-load time;
mis-configured deployments fail fast.

The same `appsettings.json` snippet works in both `appsettings.json`
and `appsettings.Development.json` (`IConfiguration` merges both).
No secret. The Gamma API requires no key for read access.

## 9. Dashboard surface

### 9.1 New component (code-behind shape)

Three files in `TradyStrat/Features/Dashboard/Components/`, matching the
project-wide convention re-affirmed in commit `2026-05-06-blazor-code-behind`
(`TodaysCallCard.razor.cs:8` is the canonical template):

**`MarketsRail.razor`** (markup only — no `@code`):

```razor
@using TradyStrat.Features.PredictionMarkets

@if (Snapshot.Markets.Count > 0)
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
```

**`MarketsRail.razor.cs`** (code-behind):

```csharp
using System.Globalization;
using Microsoft.AspNetCore.Components;
using TradyStrat.Features.PredictionMarkets;

namespace TradyStrat.Features.Dashboard.Components;

public partial class MarketsRail : ComponentBase
{
    [Parameter, EditorRequired] public MarketSnapshot Snapshot { get; set; } = MarketSnapshot.Empty;

    private Dictionary<string, MarketCitation> _bySlug = new();

    protected override void OnParametersSet()
        => _bySlug = Snapshot.Cited.ToDictionary(c => c.Slug);   // hygiene already deduped (§7.3)

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");
}
```

`Snapshot` is **non-nullable**, defaulted to `MarketSnapshot.Empty` (§3.3
Null Object). The parent (`DashboardPage`) supplies `Empty` when the
`MarketSnapshotJson` column is NULL — see §9.3. Eliminates the
`is { } pattern` dance and the EditorRequired-on-nullable contradiction.

`OnParametersSet`'s `ToDictionary(c => c.Slug)` is safe because §7.3
hygiene deduped before persistence. A sanity test in
`AiSuggestionServiceCitationTests` confirms the invariant.

**`MarketsRail.razor.css`** holds the styling described in §9.4.

### 9.2 ViewModel extension

`DashboardViewModel` gains a non-nullable property defaulted to the Null Object:

```csharp
public MarketSnapshot MarketSnapshot { get; init; } = MarketSnapshot.Empty;
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

When `MarketSnapshotJson` is NULL or the deserialize failed,
`vm.MarketSnapshot = MarketSnapshot.Empty` and the component's
`@if (Snapshot.Markets.Count > 0)` short-circuits — nothing renders,
no empty state, no banner.

### 9.4 Styling notes

- Tile width: roughly matches `CitationsBlock`'s row width; flex-wrap rows of 3 on desktop, 1 column on narrow viewports.
- `.tile.cited` adds a subtle 1px outline + brighter text on the probability.
- Probability typography matches existing numeric callouts (`.num` family).

### 9.5 `LoadDashboardUseCase` change

Insert immediately after the `todays = ...` block (today,
`LoadDashboardUseCase.cs:89`), before the prior/CallDiff branch:

```csharp
var marketSnap = MarketSnapshot.Empty;
if (todays?.MarketSnapshotJson is { Length: > 0 } json)
{
    try
    {
        marketSnap = JsonSerializer.Deserialize<MarketSnapshot>(json, JsonOpts.Strict)
                     ?? MarketSnapshot.Empty;
    }
    catch (JsonException ex)
    {
        LoadDashboardLog.MarketSnapshotMalformed(log, ex);
        // marketSnap stays Empty — rail will be absent for this entry.
    }
}
```

`marketSnap` is then passed into the `DashboardViewModel` constructor
as `MarketSnapshot: marketSnap`. The dashboard renders correctly even
if a row has malformed JSON; the rail is just absent.

The `MarketSnapshotMalformed` `LoggerMessage` partial joins the
existing `LoadDashboardLog` static partial (`LoadDashboardUseCase.cs:203`),
not a new logger class — same shape as `BackfillCrashed`.

## 10. Failure handling

Mirrors the existing `FxRateUnavailableException` pattern in `SnapshotFactory`.

### 10.1 New typed exception

```csharp
// Common/Exceptions/PolymarketUnavailableException.cs
public sealed class PolymarketUnavailableException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
```

### 10.2 Failure matrix

Tag fetches run via **`Task.WhenAll`** (§6.1). **Any one tag failing
fails the whole fetch**: the provider throws
`PolymarketUnavailableException` and any successful sibling tags are
discarded. Rationale: silently weighting a partial market set can
mislead the AI; "no markets at all" is a clearer state to render
and reason about. Worst-case latency is one tag's timeout (10s),
not `Tags.Count × 10s`.

| Failure | Where caught | Behavior |
|---|---|---|
| HTTP 5xx / network / timeout (any tag) | `PolymarketGammaProvider` → throws `PolymarketUnavailableException` | `SnapshotFactory` catches → `Markets = []` → AI runs without markets → `MarketSnapshotJson` saved as NULL |
| HTTP 429 rate-limited | resilience handler retries; on persistent failure → `PolymarketUnavailableException` | same |
| 200 OK, unparseable JSON | provider → throws | same |
| 200 OK, empty post-filter result | provider returns `[]` (not exception) | `Markets = []`, `MarketSnapshotJson = NULL`. Warning log surfaces the empty filter result |
| AI cites slug not in snapshot | `SuggestionService.AskAsync` validation (§7.3) | offending citations dropped, warning logged, suggestion saved cleanly |
| AI cites the same slug twice | `SuggestionService.AskAsync` validation (§7.3) | duplicates collapsed, first `Claim` wins, suggestion saved cleanly |
| Dashboard reads malformed JSON | `LoadDashboardUseCase` catches `JsonException` | logs, `marketSnap = MarketSnapshot.Empty`, rail not rendered |

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

// Joins existing LoadDashboardLog static partial (LoadDashboardUseCase.cs:203), not a new logger class.
[LoggerMessage(Level = LogLevel.Warning, Message = "MarketSnapshotJson malformed; rail will not render")]
public static partial void MarketSnapshotMalformed(ILogger logger, Exception ex);
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
      PolymarketOptionsBinderTests.cs           ← defaults applied; validation throws on bad config
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
      Citations/                                ← new subfolder, matches test-refactor convention
        SuggestionServiceCitationTests.cs       ← unknown-slug citations dropped
                                                ← duplicate slugs deduped (first wins)
                                                ← MarketSnapshotJson NULL when Markets empty
    Dashboard/
      UseCases/
        LoadDashboardUseCaseTests.cs (extend)   ← MarketSnapshotJson deserialized correctly
                                                ← malformed JSON tolerated (Empty returned, logged)
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
  row and a new row simply has no market drift to report (CallDiff is
  not extended for markets in Phase 1, see §3.4).
- **Pre-feature `appsettings.json`:** still works. `PolymarketOptionsBinder.Read`
  (§8) returns a fully-populated `PolymarketOptions` even when the
  `Polymarket` section is missing entirely.
- **Phase 1 multi-ticker spec:** orthogonal. That spec adds
  `Trades.InstrumentId` and the `Instruments` table; this spec adds
  `Suggestions.MarketSnapshotJson`. Different tables / different columns.
  Migration order: whichever lands first.
- **Phase 2 multi-ticker AI spec (future):** will add
  `Suggestions.InstrumentId` FK. The column coexists, but the **filter
  is crypto-broad and not per-ticker** — meaning if Phase 2 has both
  CON3.L and (say) AAPL Suggestions, both rows will reference the same
  global crypto market list. **Per-ticker market filtering is explicit
  Phase-2 work** (probably a `tag` mapping per `Instrument`); Phase 1
  keeps markets global to the day. Acknowledged scope-trim, not silent.

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
- Extending `CallDiff` to surface market-probability drift between re-runs.

## 15. Patterns referenced

Following project convention (see the multi-ticker spec §15 for the
project's pattern naming style). Strict GoF naming differs in places
from project convention; flagged inline where it matters.

- **Adapter (project convention) / Gateway (strict GoF/PoEAA naming)** —
  `PolymarketGammaProvider` projects external HTTP responses to
  `PredictionMarket`. Strictly this is **Gateway** (Fowler) or
  **Anti-Corruption Layer** (Evans) since it's not adapting an existing
  client class to a target interface — it speaks raw HTTP and emits
  domain records. The project labels its sibling shape (`YahooFxProvider`)
  as Adapter for cohesion; this spec keeps the same project naming.
- **Strategy seam (not yet Strategy)** — `IPredictionMarketProvider` is
  a single-implementation interface today, so it's polymorphic dispatch,
  not Strategy. The seam exists for a future second source. **When Kalshi
  lands, expect Composite** (combining results from N providers) more than
  Strategy (selecting one).
- **Decorator (delegated to the HTTP layer)** — no provider-level
  decorator (cache or retry) is added here. Resilience comes from
  `.AddStandardResilienceHandler()` on the typed HttpClient (§4),
  which is itself a Decorator at the HTTP message-handler layer.
  The project's `DailyFxCache` shows what a domain-level decorator
  would look like; we don't need one for daily-cadence reads.
- **Memento** — `MarketSnapshot` is a Memento: a frozen capture of
  external provider state at AI-call time, replayed on dashboard render.
  Mirrors `Suggestion.Citations` (memento of indicator state).
- **Null Object** — `MarketSnapshot.Empty` (§3.3) mirrors `CallDiff.None`.
  Consumed by `DashboardViewModel.MarketSnapshot` default and by
  `LoadDashboardUseCase` on parse failure, so the dashboard render path
  has no nullable variant to handle.
- **Pipeline (implicit, future-pattern hint)** — §6.5's
  `Dedup → VolumeFilter → HorizonFilter → Order → Take` is a textbook
  Pipeline. Today as one method body it's right-sized; if extracted
  later as `IMarketFilter` links it becomes Chain of Responsibility.
- **Specification** — N/A: no DB queries against a markets table
  (snapshots live as JSON on `Suggestion`).
- **Graceful degradation** — same family as `FxRateUnavailableException`:
  feature is additive, never blocks the AI call.

## 16. Open questions

None. Decisions captured in §2; defer Kalshi multi-source design to
its own spec when the need arises.

---

*End of design.*
