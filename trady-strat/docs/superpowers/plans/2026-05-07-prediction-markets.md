# Prediction-markets (Polymarket) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Polymarket-sourced prediction-market signal that feeds the daily AI snapshot and surfaces a "Markets the AI watched" rail under Today's Call.

**Architecture:** New `Features/PredictionMarkets/` module with a typed-HttpClient `PolymarketGammaProvider` injected into `SnapshotFactory`. Markets ride along inside `AiSnapshot`; the AI cites them via a new `market_citations` tool param; the chosen markets serialize to a new `MarketSnapshotJson` column on `Suggestion`. Dashboard renders a new `MarketsRail` component from that JSON. Feature is additive — failure path leaves the column NULL and the AI proceeds without markets.

**Tech Stack:** .NET 10, Blazor Server, EF Core (SQLite), xunit.v3 + Shouldly, Microsoft.Extensions.Http.Resilience, Microsoft.Extensions.AI, JsonDocument for camelCase wire parsing.

**Spec:** [`docs/superpowers/specs/2026-05-07-prediction-markets-design.md`](../specs/2026-05-07-prediction-markets-design.md)

---

## Pre-flight

Before starting:

- [ ] **Finish the in-progress merge.** `git status` should show clean working tree and no `Unmerged paths`. The currently-conflicted file is `TradyStrat/Features/Dashboard/UseCases/LoadDashboardUseCase.cs`. Resolve, run `dotnet build` + `dotnet test` to confirm green, then commit the merge. Do **not** start this plan until that's done — Task 17 modifies `SnapshotFactory.cs` which only has its post-merge constructor signature after the merge lands.

- [ ] **Commit the spec revision.** `git status` likely shows `docs/superpowers/specs/2026-05-07-prediction-markets-design.md` modified (the deep-review revision). Commit it on top of the finished merge with: `git add docs/superpowers/specs/2026-05-07-prediction-markets-design.md && git commit -m "docs(spec): revise prediction-markets spec after deep review"`.

- [ ] **Verify the dev environment.**
  - Run `dotnet --list-sdks` — must include a `10.0.x` line.
  - Run `dotnet tool restore` from repo root — silences any "dotnet-ef not found" hiccups.
  - Run `dotnet build` from repo root — must succeed before the first task.
  - Run `dotnet test` from repo root — must be green before the first task.

- [ ] **No new NuGet packages.** Everything used here (`Microsoft.Extensions.Http.Resilience`, `Microsoft.Extensions.AI`, `xunit.v3`, `Shouldly`) is already in `Directory.Packages.props`. Don't add anything.

---

## File map

**New production files:**

| Path | Responsibility |
|---|---|
| `TradyStrat/Common/Exceptions/PolymarketUnavailableException.cs` | Typed exception, mirrors `FxRateUnavailableException`. |
| `TradyStrat/Features/PredictionMarkets/PredictionMarket.cs` | Domain record (slug, question, probability, endDate, volume, tags). |
| `TradyStrat/Features/PredictionMarkets/MarketCitation.cs` | Domain record (slug, claim). |
| `TradyStrat/Features/PredictionMarkets/MarketSnapshot.cs` | Aggregate of Markets + Cited; carries `Empty` Null Object. |
| `TradyStrat/Features/PredictionMarkets/IPredictionMarketProvider.cs` | Provider abstraction. |
| `TradyStrat/Features/PredictionMarkets/PolymarketOptions.cs` | Options record + `PolymarketOptionsBinder.Read`. |
| `TradyStrat/Features/PredictionMarkets/Providers/PolymarketGammaProvider.cs` | Typed-HttpClient adapter to gamma-api.polymarket.com. |
| `TradyStrat/Modules/PredictionMarketsModule.cs` | DI wiring (typed HttpClient + resilience). |
| `TradyStrat/Features/Dashboard/Components/MarketsRail.razor` | Markup, no @code. |
| `TradyStrat/Features/Dashboard/Components/MarketsRail.razor.cs` | Code-behind partial class. |
| `TradyStrat/Features/Dashboard/Components/MarketsRail.razor.css` | Scoped styling. |
| `TradyStrat/Data/Migrations/<timestamp>_AddSuggestionMarketSnapshotJson.cs` | EF migration (generated). |

**Modified production files:**

| Path | What changes |
|---|---|
| `TradyStrat/appsettings.json` | New `Polymarket` section. |
| `TradyStrat/Common/Domain/Suggestion.cs` | Add `MarketSnapshotJson` (nullable, init-only). |
| `TradyStrat/Data/Configurations/SuggestionConfiguration.cs` | Add `HasMaxLength(8000)` for new column (matches `CitationsJson`). |
| `TradyStrat/Features/AiSuggestion/Snapshot/AiSnapshot.cs` | Add `Markets` field (positional, before `PromptHash`). |
| `TradyStrat/Features/AiSuggestion/Snapshot/SnapshotFactory.cs` | Inject provider, fetch markets, tolerate failure, include in hash. |
| `TradyStrat/Features/AiSuggestion/SuggestionService.cs` | System prompt addendum, new `market_citations` tool param, hygiene/dedup, write `MarketSnapshotJson`. |
| `TradyStrat/Features/Dashboard/DashboardViewModel.cs` | Add non-nullable `MarketSnapshot` defaulted to `Empty`. |
| `TradyStrat/Features/Dashboard/UseCases/LoadDashboardUseCase.cs` | Deserialize `MarketSnapshotJson`, log helper. |
| `TradyStrat/Features/Dashboard/DashboardPage.razor` | Render `<MarketsRail>` between `TodaysCallCard` and `CitationsBlock`. |

**New test files:**

| Path | Responsibility |
|---|---|
| `TradyStrat.Tests/PredictionMarkets/Fixtures/Polymarket/gamma-markets-bitcoin.json` | Captured Gamma response (≤3 markets). |
| `TradyStrat.Tests/PredictionMarkets/Fixtures/Polymarket/gamma-markets-multi-outcome.json` | Mix binary + multi-outcome. |
| `TradyStrat.Tests/PredictionMarkets/Fixtures/Polymarket/gamma-markets-empty.json` | `[]`. |
| `TradyStrat.Tests/PredictionMarkets/Fixtures/Polymarket/gamma-markets-malformed.json` | Garbage. |
| `TradyStrat.Tests/PredictionMarkets/PolymarketOptionsBinderTests.cs` | Defaults; validation throws. |
| `TradyStrat.Tests/PredictionMarkets/PolymarketNormalizationTests.cs` | Multi-outcome drop, parse failures. |
| `TradyStrat.Tests/PredictionMarkets/PolymarketFilterTests.cs` | Volume/horizon/sort/take/dedup. |
| `TradyStrat.Tests/PredictionMarkets/Providers/PolymarketGammaProviderTests.cs` | HttpMessageHandler stub. |
| `TradyStrat.Tests/AiSuggestion/Citations/SuggestionServiceCitationTests.cs` | Unknown-slug drop, dedup, NULL when empty. |

**Modified test files:**

| Path | What changes |
|---|---|
| `TradyStrat.Tests/AiSuggestion/Snapshot/SnapshotFactoryTests.cs` | New stub `IPredictionMarketProvider`; assert markets in snapshot, in hash, tolerated on failure. |
| `TradyStrat.Tests/AiSuggestion/SuggestionServiceTests.cs` | Update `SampleSnapshot()` to include `Markets: []`. |
| `TradyStrat.Tests/Dashboard/UseCases/LoadDashboardUseCaseTests.cs` | Round-trip `MarketSnapshotJson`; tolerate malformed. |

**Note:** Test root is `TradyStrat.Tests/` (no `Features/` segment) — confirmed by `ls TradyStrat.Tests/` (`AiSuggestion`, `Dashboard`, `Fx`, …). The spec's §12.1 inadvertently shows `TradyStrat.Tests/Features/...` — use the actual layout above.

---

## Task 1 — Foundation: typed exception

**Files:**
- Create: `TradyStrat/Common/Exceptions/PolymarketUnavailableException.cs`

- [ ] **Step 1: Create the exception**

```csharp
namespace TradyStrat.Common.Exceptions;

public sealed class PolymarketUnavailableException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
```

- [ ] **Step 2: Build to confirm**

Run: `dotnet build TradyStrat/TradyStrat.csproj`
Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat/Common/Exceptions/PolymarketUnavailableException.cs
git commit -m "feat(prediction-markets): add PolymarketUnavailableException"
```

---

## Task 2 — Foundation: domain records

**Files:**
- Create: `TradyStrat/Features/PredictionMarkets/PredictionMarket.cs`
- Create: `TradyStrat/Features/PredictionMarkets/MarketCitation.cs`
- Create: `TradyStrat/Features/PredictionMarkets/MarketSnapshot.cs`

- [ ] **Step 1: Create `PredictionMarket.cs`**

```csharp
namespace TradyStrat.Features.PredictionMarkets;

public sealed record PredictionMarket(
    string Slug,
    string Question,
    decimal Probability,                 // 0..1, the YES outcome price
    DateOnly EndDate,
    decimal VolumeUsd,
    IReadOnlyList<string> Tags);
```

- [ ] **Step 2: Create `MarketCitation.cs`**

```csharp
namespace TradyStrat.Features.PredictionMarkets;

public sealed record MarketCitation(
    string Slug,                         // FK back into MarketSnapshot.Markets
    string Claim);                       // one-line reason AI weighed this market
```

- [ ] **Step 3: Create `MarketSnapshot.cs` with Null Object**

```csharp
namespace TradyStrat.Features.PredictionMarkets;

public sealed record MarketSnapshot(
    IReadOnlyList<PredictionMarket> Markets,
    IReadOnlyList<MarketCitation> Cited)
{
    public static readonly MarketSnapshot Empty = new([], []);
}
```

- [ ] **Step 4: Build to confirm**

Run: `dotnet build TradyStrat/TradyStrat.csproj`
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/PredictionMarkets/PredictionMarket.cs \
        TradyStrat/Features/PredictionMarkets/MarketCitation.cs \
        TradyStrat/Features/PredictionMarkets/MarketSnapshot.cs
git commit -m "feat(prediction-markets): add domain records and MarketSnapshot.Empty"
```

---

## Task 3 — Provider interface

**Files:**
- Create: `TradyStrat/Features/PredictionMarkets/IPredictionMarketProvider.cs`

- [ ] **Step 1: Create the interface**

```csharp
namespace TradyStrat.Features.PredictionMarkets;

public interface IPredictionMarketProvider
{
    Task<IReadOnlyList<PredictionMarket>> GetMarketsAsync(CancellationToken ct);
}
```

- [ ] **Step 2: Build to confirm**

Run: `dotnet build TradyStrat/TradyStrat.csproj`
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat/Features/PredictionMarkets/IPredictionMarketProvider.cs
git commit -m "feat(prediction-markets): add IPredictionMarketProvider abstraction"
```

---

## Task 4 — `PolymarketOptions` + binder + validation tests

**Files:**
- Create: `TradyStrat/Features/PredictionMarkets/PolymarketOptions.cs`
- Test: `TradyStrat.Tests/PredictionMarkets/PolymarketOptionsBinderTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using Microsoft.Extensions.Configuration;
using Shouldly;
using TradyStrat.Features.PredictionMarkets;
using Xunit;

namespace TradyStrat.Tests.PredictionMarkets;

public class PolymarketOptionsBinderTests
{
    [Fact]
    public void Returns_defaults_when_section_missing()
    {
        var cfg = new ConfigurationBuilder().Build();   // no Polymarket section
        var opts = PolymarketOptionsBinder.Read(cfg);

        opts.BaseUrl.ShouldBe("https://gamma-api.polymarket.com");
        opts.Tags.ShouldBe(["bitcoin", "crypto", "coinbase", "ethereum"]);
        opts.MaxMarkets.ShouldBe(10);
        opts.MinVolumeUsd.ShouldBe(50_000m);
        opts.MaxHorizonDays.ShouldBe(365);
    }

    [Fact]
    public void Reads_overrides_from_configuration()
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Polymarket:BaseUrl"]        = "https://example.test",
                ["Polymarket:Tags:0"]         = "ethereum",
                ["Polymarket:MaxMarkets"]     = "5",
                ["Polymarket:MinVolumeUsd"]   = "1000",
                ["Polymarket:MaxHorizonDays"] = "90",
            })
            .Build();
        var opts = PolymarketOptionsBinder.Read(cfg);

        opts.BaseUrl.ShouldBe("https://example.test");
        opts.Tags.ShouldBe(["ethereum"]);
        opts.MaxMarkets.ShouldBe(5);
        opts.MinVolumeUsd.ShouldBe(1000m);
        opts.MaxHorizonDays.ShouldBe(90);
    }

    [Theory]
    [InlineData("MaxMarkets",     "0")]
    [InlineData("MaxMarkets",     "-1")]
    [InlineData("MinVolumeUsd",   "-1")]
    [InlineData("MaxHorizonDays", "0")]
    public void Throws_on_invalid_config(string key, string value)
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"Polymarket:{key}"] = value,
            })
            .Build();
        Should.Throw<ArgumentOutOfRangeException>(() => PolymarketOptionsBinder.Read(cfg));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~PolymarketOptionsBinderTests"`
Expected: build error — `PolymarketOptionsBinder` does not exist yet.

- [ ] **Step 3: Implement `PolymarketOptions.cs`**

```csharp
using Microsoft.Extensions.Configuration;

namespace TradyStrat.Features.PredictionMarkets;

public sealed record PolymarketOptions(
    string                BaseUrl,
    IReadOnlyList<string> Tags,
    int                   MaxMarkets,
    decimal               MinVolumeUsd,
    int                   MaxHorizonDays);

public static class PolymarketOptionsBinder
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

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~PolymarketOptionsBinderTests"`
Expected: 6 tests pass.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/PredictionMarkets/PolymarketOptions.cs \
        TradyStrat.Tests/PredictionMarkets/PolymarketOptionsBinderTests.cs
git commit -m "feat(prediction-markets): add PolymarketOptions binder with validation"
```

---

## Task 5 — Test fixtures (captured Gamma JSON)

**Files:**
- Create: `TradyStrat.Tests/PredictionMarkets/Fixtures/Polymarket/gamma-markets-bitcoin.json`
- Create: `TradyStrat.Tests/PredictionMarkets/Fixtures/Polymarket/gamma-markets-multi-outcome.json`
- Create: `TradyStrat.Tests/PredictionMarkets/Fixtures/Polymarket/gamma-markets-empty.json`
- Create: `TradyStrat.Tests/PredictionMarkets/Fixtures/Polymarket/gamma-markets-malformed.json`
- Modify: `TradyStrat.Tests/TradyStrat.Tests.csproj` (only if fixtures aren't already copied to output by an existing wildcard)

- [ ] **Step 1: Inspect csproj for an existing fixture-copy rule**

Run: `grep -n "CopyToOutputDirectory\|Fixtures" TradyStrat.Tests/TradyStrat.Tests.csproj`
Expected: existing wildcard like `<None Update="Fixtures/**/*"...>` covers our path. If absent, add this `ItemGroup` to the csproj:

```xml
<ItemGroup>
  <None Update="PredictionMarkets/Fixtures/**/*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

- [ ] **Step 2: Create `gamma-markets-bitcoin.json`**

Three binary BTC markets. Note: Polymarket's `outcomes` and `outcomePrices` ARE STRINGIFIED ARRAYS (string fields containing JSON-encoded arrays); `endDate` is camelCase.

```json
[
  {
    "slug": "btc-above-100k-eoy-2026",
    "question": "Will Bitcoin close above $100,000 on Dec 31, 2026?",
    "outcomes": "[\"Yes\", \"No\"]",
    "outcomePrices": "[\"0.32\", \"0.68\"]",
    "endDate": "2026-12-31T00:00:00Z",
    "volume": "1250000",
    "tags": [{"slug": "bitcoin"}, {"slug": "crypto"}]
  },
  {
    "slug": "btc-above-80k-eoy-2026",
    "question": "Will Bitcoin close above $80,000 on Dec 31, 2026?",
    "outcomes": "[\"Yes\", \"No\"]",
    "outcomePrices": "[\"0.71\", \"0.29\"]",
    "endDate": "2026-12-31T00:00:00Z",
    "volume": "780000",
    "tags": [{"slug": "bitcoin"}]
  },
  {
    "slug": "btc-below-50k-q3-2026",
    "question": "Will Bitcoin trade below $50,000 at any point in Q3 2026?",
    "outcomes": "[\"Yes\", \"No\"]",
    "outcomePrices": "[\"0.12\", \"0.88\"]",
    "endDate": "2026-09-30T00:00:00Z",
    "volume": "320000",
    "tags": [{"slug": "bitcoin"}, {"slug": "crypto"}]
  }
]
```

- [ ] **Step 3: Create `gamma-markets-multi-outcome.json`**

One binary, one multi-outcome (will be dropped during normalization), one with mismatched `outcomes` array (also dropped).

```json
[
  {
    "slug": "coin-beats-q3-2026",
    "question": "Will Coinbase beat Q3 2026 EPS estimates?",
    "outcomes": "[\"Yes\", \"No\"]",
    "outcomePrices": "[\"0.58\", \"0.42\"]",
    "endDate": "2026-11-15T00:00:00Z",
    "volume": "450000",
    "tags": [{"slug": "coinbase"}]
  },
  {
    "slug": "next-fomc-decision",
    "question": "Next FOMC rate decision",
    "outcomes": "[\"Cut 25bp\", \"Cut 50bp\", \"Hold\", \"Hike\"]",
    "outcomePrices": "[\"0.40\", \"0.10\", \"0.45\", \"0.05\"]",
    "endDate": "2026-09-17T00:00:00Z",
    "volume": "2000000",
    "tags": [{"slug": "fed"}]
  },
  {
    "slug": "weird-market",
    "question": "Will a meteor strike?",
    "outcomes": "[\"Maybe\"]",
    "outcomePrices": "[\"0.50\"]",
    "endDate": "2099-01-01T00:00:00Z",
    "volume": "100",
    "tags": []
  }
]
```

- [ ] **Step 4: Create `gamma-markets-empty.json`**

```json
[]
```

- [ ] **Step 5: Create `gamma-markets-malformed.json`**

```
{this is not valid JSON
```

- [ ] **Step 6: Build to confirm csproj wildcard picks fixtures up**

Run: `dotnet build TradyStrat.Tests/TradyStrat.Tests.csproj`
Expected: build succeeds, fixtures appear under `TradyStrat.Tests/bin/Debug/net10.0/PredictionMarkets/Fixtures/Polymarket/`. Verify with `ls TradyStrat.Tests/bin/Debug/net10.0/PredictionMarkets/Fixtures/Polymarket/`.

- [ ] **Step 7: Commit**

```bash
git add TradyStrat.Tests/PredictionMarkets/Fixtures/ TradyStrat.Tests/TradyStrat.Tests.csproj
git commit -m "test(prediction-markets): add captured Gamma JSON fixtures"
```

---

## Task 6 — Normalization tests + parsing logic (binary-only, drop bad rows)

**Files:**
- Create: `TradyStrat.Tests/PredictionMarkets/PolymarketNormalizationTests.cs`
- Create: `TradyStrat/Features/PredictionMarkets/Providers/PolymarketGammaProvider.cs` (parsing helpers only — HTTP comes in Task 8)

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Text.Json;
using Shouldly;
using TradyStrat.Features.PredictionMarkets;
using TradyStrat.Features.PredictionMarkets.Providers;
using Xunit;

namespace TradyStrat.Tests.PredictionMarkets;

public class PolymarketNormalizationTests
{
    private static JsonElement Load(string fixtureName)
    {
        var path = Path.Combine(AppContext.BaseDirectory,
            "PredictionMarkets", "Fixtures", "Polymarket", fixtureName);
        var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.Clone();
    }

    [Fact]
    public void Parses_three_binary_btc_markets()
    {
        var arr = Load("gamma-markets-bitcoin.json");
        var markets = PolymarketNormalizer.Normalize(arr).ToList();

        markets.Count.ShouldBe(3);
        markets[0].Slug.ShouldBe("btc-above-100k-eoy-2026");
        markets[0].Probability.ShouldBe(0.32m);
        markets[0].Tags.ShouldContain("bitcoin");
        markets[0].VolumeUsd.ShouldBe(1_250_000m);
        markets[0].EndDate.ShouldBe(new DateOnly(2026, 12, 31));
    }

    [Fact]
    public void Drops_multi_outcome_and_malformed_markets()
    {
        var arr = Load("gamma-markets-multi-outcome.json");
        var markets = PolymarketNormalizer.Normalize(arr).ToList();

        markets.Count.ShouldBe(1);                       // only "coin-beats-q3-2026" survives
        markets[0].Slug.ShouldBe("coin-beats-q3-2026");
    }

    [Fact]
    public void Returns_empty_for_empty_input()
    {
        var arr = Load("gamma-markets-empty.json");
        PolymarketNormalizer.Normalize(arr).ShouldBeEmpty();
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test --filter "FullyQualifiedName~PolymarketNormalizationTests"`
Expected: build error — `PolymarketNormalizer` does not exist.

- [ ] **Step 3: Implement `PolymarketNormalizer` (inside the Providers folder)**

Create `TradyStrat/Features/PredictionMarkets/Providers/PolymarketGammaProvider.cs` with the normalizer as an internal static class. We'll add the actual provider logic (HTTP) in Task 8.

```csharp
using System.Globalization;
using System.Text.Json;
using TradyStrat.Common.Exceptions;

namespace TradyStrat.Features.PredictionMarkets.Providers;

internal static class PolymarketNormalizer
{
    public static IEnumerable<PredictionMarket> Normalize(JsonElement array)
    {
        if (array.ValueKind != JsonValueKind.Array) yield break;

        foreach (var el in array.EnumerateArray())
        {
            if (TryNormalize(el, out var market)) yield return market!;
        }
    }

    private static bool TryNormalize(JsonElement el, out PredictionMarket? market)
    {
        market = null;

        if (!el.TryGetProperty("slug",          out var slugEl)
         || !el.TryGetProperty("question",      out var questionEl)
         || !el.TryGetProperty("outcomes",      out var outcomesEl)
         || !el.TryGetProperty("outcomePrices", out var pricesEl)
         || !el.TryGetProperty("endDate",       out var endDateEl)
         || !el.TryGetProperty("volume",        out var volumeEl))
            return false;

        // outcomes and outcomePrices are stringified JSON arrays in Gamma's payload.
        if (!TryParseStringifiedArray(outcomesEl,  out var outcomes)) return false;
        if (!TryParseStringifiedArray(pricesEl,    out var prices))   return false;

        // Phase 1: binary YES/NO only.
        if (outcomes.Count != 2 || outcomes[0] != "Yes" || outcomes[1] != "No") return false;
        if (prices.Count   != 2)                                                 return false;

        if (!decimal.TryParse(prices[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var yes) ||
            !decimal.TryParse(prices[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var no))
            return false;

        // Sanity: YES + NO must be ~1 (orderbook tick tolerance).
        if (Math.Abs(yes + no - 1m) > 0.01m) return false;

        if (!endDateEl.TryGetDateTime(out var endDateTime)) return false;
        var endDate = DateOnly.FromDateTime(endDateTime);

        var volumeStr = volumeEl.ValueKind == JsonValueKind.String ? volumeEl.GetString() : volumeEl.GetRawText();
        if (!decimal.TryParse(volumeStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var volume) || volume < 0)
            return false;

        var tags = new List<string>();
        if (el.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in tagsEl.EnumerateArray())
                if (t.TryGetProperty("slug", out var tagSlugEl) && tagSlugEl.GetString() is { } s)
                    tags.Add(s);
        }

        market = new PredictionMarket(
            Slug:        slugEl.GetString()     ?? "",
            Question:    questionEl.GetString() ?? "",
            Probability: yes,
            EndDate:     endDate,
            VolumeUsd:   volume,
            Tags:        tags);
        return true;
    }

    private static bool TryParseStringifiedArray(JsonElement el, out List<string> items)
    {
        items = [];
        if (el.ValueKind != JsonValueKind.String) return false;
        var raw = el.GetString();
        if (string.IsNullOrEmpty(raw)) return false;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return false;
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                items.Add(item.ValueKind == JsonValueKind.String ? item.GetString() ?? "" : item.GetRawText());
            }
            return true;
        }
        catch (JsonException) { return false; }
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test --filter "FullyQualifiedName~PolymarketNormalizationTests"`
Expected: 3 tests pass.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/PredictionMarkets/Providers/PolymarketGammaProvider.cs \
        TradyStrat.Tests/PredictionMarkets/PolymarketNormalizationTests.cs
git commit -m "feat(prediction-markets): normalize Gamma payload, drop multi-outcome"
```

---

## Task 7 — Filter pipeline tests + logic

**Files:**
- Create: `TradyStrat.Tests/PredictionMarkets/PolymarketFilterTests.cs`
- Modify: `TradyStrat/Features/PredictionMarkets/Providers/PolymarketGammaProvider.cs` (add `PolymarketFilter`)

- [ ] **Step 1: Write the failing tests**

```csharp
using Shouldly;
using TradyStrat.Features.PredictionMarkets;
using TradyStrat.Features.PredictionMarkets.Providers;
using Xunit;

namespace TradyStrat.Tests.PredictionMarkets;

public class PolymarketFilterTests
{
    private static PredictionMarket M(string slug, decimal volume, DateOnly endDate) =>
        new(Slug: slug, Question: slug, Probability: 0.5m,
            EndDate: endDate, VolumeUsd: volume, Tags: ["bitcoin"]);

    [Fact]
    public void Dedupes_by_slug_keeping_first()
    {
        var input = new[]
        {
            M("a", 100m, new DateOnly(2026, 12, 31)),
            M("a", 999m, new DateOnly(2026, 12, 31)),  // duplicate slug
            M("b", 200m, new DateOnly(2026, 12, 31)),
        };
        var result = PolymarketFilter.Apply(input,
            today: new DateOnly(2026, 5, 7),
            minVolumeUsd: 0m,
            maxHorizonDays: 365,
            maxMarkets: 10);

        result.Select(m => m.Slug).ShouldBe(["b", "a"]); // ordered by volume desc
        result.Single(m => m.Slug == "a").VolumeUsd.ShouldBe(100m);
    }

    [Fact]
    public void Filters_below_min_volume()
    {
        var input = new[]
        {
            M("a", 1000m,  new DateOnly(2026, 12, 31)),
            M("b", 50_000m, new DateOnly(2026, 12, 31)),
        };
        var result = PolymarketFilter.Apply(input,
            today: new DateOnly(2026, 5, 7),
            minVolumeUsd: 10_000m,
            maxHorizonDays: 365,
            maxMarkets: 10);

        result.Select(m => m.Slug).ShouldBe(["b"]);
    }

    [Fact]
    public void Filters_beyond_horizon()
    {
        var input = new[]
        {
            M("near", 100m, new DateOnly(2026, 6, 1)),
            M("far",  100m, new DateOnly(2030, 1, 1)),
        };
        var result = PolymarketFilter.Apply(input,
            today: new DateOnly(2026, 5, 7),
            minVolumeUsd: 0m,
            maxHorizonDays: 30,
            maxMarkets: 10);

        result.Select(m => m.Slug).ShouldBe(["near"]);
    }

    [Fact]
    public void Orders_by_volume_descending_and_takes_max()
    {
        var input = new[]
        {
            M("low",  100m, new DateOnly(2026, 12, 31)),
            M("high", 999m, new DateOnly(2026, 12, 31)),
            M("mid",  500m, new DateOnly(2026, 12, 31)),
        };
        var result = PolymarketFilter.Apply(input,
            today: new DateOnly(2026, 5, 7),
            minVolumeUsd: 0m,
            maxHorizonDays: 365,
            maxMarkets: 2);

        result.Select(m => m.Slug).ShouldBe(["high", "mid"]);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test --filter "FullyQualifiedName~PolymarketFilterTests"`
Expected: build error — `PolymarketFilter` does not exist.

- [ ] **Step 3: Add `PolymarketFilter` to `PolymarketGammaProvider.cs`**

Append to the file (alongside `PolymarketNormalizer`):

```csharp
internal static class PolymarketFilter
{
    public static IReadOnlyList<PredictionMarket> Apply(
        IEnumerable<PredictionMarket> markets,
        DateOnly today,
        decimal minVolumeUsd,
        int maxHorizonDays,
        int maxMarkets)
    {
        var horizon = today.AddDays(maxHorizonDays);
        var seen = new HashSet<string>();
        var deduped = new List<PredictionMarket>();
        foreach (var m in markets)
            if (seen.Add(m.Slug))
                deduped.Add(m);

        return deduped
            .Where(m => m.VolumeUsd >= minVolumeUsd)
            .Where(m => m.EndDate <= horizon)
            .OrderByDescending(m => m.VolumeUsd)
            .Take(maxMarkets)
            .ToList();
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test --filter "FullyQualifiedName~PolymarketFilterTests"`
Expected: 4 tests pass.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/PredictionMarkets/Providers/PolymarketGammaProvider.cs \
        TradyStrat.Tests/PredictionMarkets/PolymarketFilterTests.cs
git commit -m "feat(prediction-markets): add deterministic filter pipeline"
```

---

## Task 8 — Provider HTTP integration with stubbed `HttpMessageHandler`

**Files:**
- Modify: `TradyStrat/Features/PredictionMarkets/Providers/PolymarketGammaProvider.cs` (add `PolymarketGammaProvider` class)
- Create: `TradyStrat.Tests/PredictionMarkets/Providers/PolymarketGammaProviderTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Net;
using Shouldly;
using TradyStrat.Common.Exceptions;
using TradyStrat.Features.PredictionMarkets;
using TradyStrat.Features.PredictionMarkets.Providers;
using TradyStrat.Tests.Common.Time;
using Xunit;

namespace TradyStrat.Tests.PredictionMarkets.Providers;

public class PolymarketGammaProviderTests
{
    private const string FixtureRel = "PredictionMarkets/Fixtures/Polymarket";

    private static string ReadFixture(string name)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, FixtureRel, name));

    private static (HttpClient http, StubHandler handler) BuildHttp(
        string baseUrl = "https://gamma-api.polymarket.com",
        Func<HttpRequestMessage, Task<HttpResponseMessage>>? respond = null)
    {
        var handler = new StubHandler(respond ?? (_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ReadFixture("gamma-markets-bitcoin.json")),
            })));
        var http = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
        return (http, handler);
    }

    private static PolymarketOptions Options(int maxMarkets = 10) => new(
        BaseUrl: "https://gamma-api.polymarket.com",
        Tags: ["bitcoin"],
        MaxMarkets: maxMarkets,
        MinVolumeUsd: 0m,
        MaxHorizonDays: 3650);   // wide so fixtures don't get filtered out

    [Fact]
    public async Task Returns_normalized_filtered_list_on_success()
    {
        var (http, _) = BuildHttp();
        var sut = new PolymarketGammaProvider(http, Options(), new FakeClock(new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc)));

        var result = await sut.GetMarketsAsync(TestContext.Current.CancellationToken);

        result.Count.ShouldBe(3);
        result[0].Slug.ShouldBe("btc-above-100k-eoy-2026");
    }

    [Fact]
    public async Task Throws_on_500()
    {
        var (http, _) = BuildHttp(respond: _ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var sut = new PolymarketGammaProvider(http, Options(), new FakeClock(new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc)));

        await Should.ThrowAsync<PolymarketUnavailableException>(() =>
            sut.GetMarketsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Throws_on_unparseable_body()
    {
        var (http, _) = BuildHttp(respond: _ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ReadFixture("gamma-markets-malformed.json")),
            }));
        var sut = new PolymarketGammaProvider(http, Options(), new FakeClock(new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc)));

        await Should.ThrowAsync<PolymarketUnavailableException>(() =>
            sut.GetMarketsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Empty_response_returns_empty_list_no_exception()
    {
        var (http, _) = BuildHttp(respond: _ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ReadFixture("gamma-markets-empty.json")),
            }));
        var sut = new PolymarketGammaProvider(http, Options(), new FakeClock(new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc)));

        var result = await sut.GetMarketsAsync(TestContext.Current.CancellationToken);
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Issues_one_request_per_tag_and_dedupes()
    {
        var options = new PolymarketOptions(
            BaseUrl: "https://gamma-api.polymarket.com",
            Tags: ["bitcoin", "crypto"],
            MaxMarkets: 10, MinVolumeUsd: 0m, MaxHorizonDays: 3650);

        var requested = new List<string>();
        var (http, _) = BuildHttp(respond: req =>
        {
            lock (requested) requested.Add(req.RequestUri!.Query);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ReadFixture("gamma-markets-bitcoin.json")),
            });
        });
        var sut = new PolymarketGammaProvider(http, options, new FakeClock(new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc)));

        var result = await sut.GetMarketsAsync(TestContext.Current.CancellationToken);

        requested.Count.ShouldBe(2);
        requested.ShouldContain(q => q.Contains("tag_slug=bitcoin"));
        requested.ShouldContain(q => q.Contains("tag_slug=crypto"));
        // Same fixture returned twice → 3 unique markets after dedup.
        result.Count.ShouldBe(3);
    }

    [Fact]
    public async Task One_tag_failing_throws_and_discards_others()
    {
        var options = new PolymarketOptions(
            BaseUrl: "https://gamma-api.polymarket.com",
            Tags: ["bitcoin", "crypto"],
            MaxMarkets: 10, MinVolumeUsd: 0m, MaxHorizonDays: 3650);

        var (http, _) = BuildHttp(respond: req =>
        {
            if (req.RequestUri!.Query.Contains("tag_slug=crypto"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ReadFixture("gamma-markets-bitcoin.json")),
            });
        });
        var sut = new PolymarketGammaProvider(http, options, new FakeClock(new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc)));

        await Should.ThrowAsync<PolymarketUnavailableException>(() =>
            sut.GetMarketsAsync(TestContext.Current.CancellationToken));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => respond(request);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test --filter "FullyQualifiedName~PolymarketGammaProviderTests"`
Expected: build error — `PolymarketGammaProvider` does not exist.

- [ ] **Step 3: Implement the provider**

Append to `TradyStrat/Features/PredictionMarkets/Providers/PolymarketGammaProvider.cs`:

```csharp
using TradyStrat.Common.Time;

public sealed class PolymarketGammaProvider(
    HttpClient http,
    PolymarketOptions options,
    IClock clock) : IPredictionMarketProvider
{
    public async Task<IReadOnlyList<PredictionMarket>> GetMarketsAsync(CancellationToken ct)
    {
        // Fan out: one request per tag, all in parallel. Any failure aborts.
        var perTag = options.Tags
            .Select(tag => FetchTagAsync(tag, ct))
            .ToArray();

        IReadOnlyList<PredictionMarket>[] results;
        try
        {
            results = await Task.WhenAll(perTag);
        }
        catch (PolymarketUnavailableException) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new PolymarketUnavailableException("Polymarket fetch failed.", ex);
        }

        var merged = results.SelectMany(r => r);
        var today = DateOnly.FromDateTime(clock.UtcNow().Date);
        return PolymarketFilter.Apply(merged, today, options.MinVolumeUsd, options.MaxHorizonDays, options.MaxMarkets);
    }

    private async Task<IReadOnlyList<PredictionMarket>> FetchTagAsync(string tag, CancellationToken ct)
    {
        // Over-fetch buffer so post-filter has room to drop multi-outcome / out-of-horizon rows.
        var limit = options.MaxMarkets * 2;
        var url = $"/markets?active=true&closed=false&order=volume&ascending=false&limit={limit}&tag_slug={Uri.EscapeDataString(tag)}";

        try
        {
            using var resp = await http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
                throw new PolymarketUnavailableException($"Gamma {(int)resp.StatusCode} for tag {tag}");

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return PolymarketNormalizer.Normalize(doc.RootElement).ToList();
        }
        catch (PolymarketUnavailableException) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new PolymarketUnavailableException($"Polymarket fetch failed for tag {tag}", ex);
        }
    }
}
```

Note the file's top-of-file `using` directives now need to include `System.Text.Json`, `TradyStrat.Common.Exceptions`, `TradyStrat.Common.Time` — add them at the top of the file.

- [ ] **Step 4: Run tests**

Run: `dotnet test --filter "FullyQualifiedName~PolymarketGammaProviderTests"`
Expected: 6 tests pass.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/PredictionMarkets/Providers/PolymarketGammaProvider.cs \
        TradyStrat.Tests/PredictionMarkets/Providers/PolymarketGammaProviderTests.cs
git commit -m "feat(prediction-markets): add PolymarketGammaProvider with Task.WhenAll and graceful failure"
```

---

## Task 9 — Module wiring + appsettings

**Files:**
- Create: `TradyStrat/Modules/PredictionMarketsModule.cs`
- Modify: `TradyStrat/appsettings.json`

- [ ] **Step 1: Add the `Polymarket` section to `appsettings.json`**

Insert into `appsettings.json` (anywhere among the existing top-level objects):

```json
"Polymarket": {
  "BaseUrl":         "https://gamma-api.polymarket.com",
  "Tags":            ["bitcoin", "crypto", "coinbase", "ethereum"],
  "MaxMarkets":      10,
  "MinVolumeUsd":    50000,
  "MaxHorizonDays":  365
}
```

- [ ] **Step 2: Create `PredictionMarketsModule.cs`**

```csharp
using TheAppManager.Modules;
using TradyStrat.Features.PredictionMarkets;
using TradyStrat.Features.PredictionMarkets.Providers;

namespace TradyStrat.Modules;

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

- [ ] **Step 3: Verify the module is auto-discovered by TheAppManager**

Run: `grep -n "AddAppModule\|TheAppManager\|IAppModule" TradyStrat/Program.cs`
Expected: existing modules are auto-loaded by reflection (TheAppManager pattern). If `Program.cs` enumerates modules explicitly, add `PredictionMarketsModule` to the list. Otherwise nothing more is needed.

- [ ] **Step 4: Build and run smoke tests**

```bash
dotnet build TradyStrat/TradyStrat.csproj
dotnet test --filter "FullyQualifiedName~SmokeTests"
```

Expected: build succeeds; smoke tests still pass (the provider is registered but not yet consumed).

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Modules/PredictionMarketsModule.cs TradyStrat/appsettings.json
git commit -m "feat(prediction-markets): register PredictionMarketsModule with typed HttpClient + resilience"
```

---

## Task 10 — `Suggestion` entity column + EF migration

**Files:**
- Modify: `TradyStrat/Common/Domain/Suggestion.cs`
- Modify: `TradyStrat/Data/Configurations/SuggestionConfiguration.cs`
- Create: `TradyStrat/Data/Migrations/<timestamp>_AddSuggestionMarketSnapshotJson.cs` (generated)

- [ ] **Step 1: Add the property to `Suggestion.cs`**

Insert after the existing `CitationsJson` line:

```csharp
public string? MarketSnapshotJson { get; init; }
```

The class becomes (showing context only — keep all existing members):

```csharp
public sealed record Suggestion
{
    // … existing CitationOpts and other properties …
    public required string CitationsJson { get; init; }
    public string? MarketSnapshotJson { get; init; }   // ← new
    public required string PromptHash { get; init; }
    // … existing CreatedAt, OrderValueEur, Citations …
}
```

- [ ] **Step 2: Update `SuggestionConfiguration.cs`**

Insert the `Property` line after the `CitationsJson` config:

```csharp
builder.Property(s => s.MarketSnapshotJson).HasMaxLength(8000);
```

- [ ] **Step 3: Generate the migration**

Run from repo root:

```bash
dotnet ef migrations add AddSuggestionMarketSnapshotJson --project TradyStrat/TradyStrat.csproj
```

Expected: a new pair of files under `TradyStrat/Data/Migrations/` with a name like `20260507XXXXXX_AddSuggestionMarketSnapshotJson.cs` and `.Designer.cs`. The `Up` method should contain `migrationBuilder.AddColumn<string>(name: "MarketSnapshotJson", table: "Suggestions", type: "TEXT", nullable: true)`. If `maxLength: 8000` is missing, no need to edit — EF accepts both shapes for SQLite.

- [ ] **Step 4: Confirm migration applies cleanly**

Run: `dotnet build TradyStrat/TradyStrat.csproj` then `dotnet test --filter "FullyQualifiedName~SmokeTests"`
Expected: build green, smoke tests pass — migrations run automatically on startup; the in-memory test DB will run the new column too.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Common/Domain/Suggestion.cs \
        TradyStrat/Data/Configurations/SuggestionConfiguration.cs \
        TradyStrat/Data/Migrations/
git commit -m "feat(prediction-markets): add Suggestion.MarketSnapshotJson column + migration"
```

---

## Task 11 — Extend `AiSnapshot` with `Markets`

**Files:**
- Modify: `TradyStrat/Features/AiSuggestion/Snapshot/AiSnapshot.cs`

- [ ] **Step 1: Update `AiSnapshot` record**

Replace the existing record with:

```csharp
using TradyStrat.Common.Domain;
using TradyStrat.Features.PredictionMarkets;

namespace TradyStrat.Features.AiSuggestion.Snapshot;

public sealed record TickerContext(
    string Ticker, string Currency,
    decimal PriceNative, decimal? PriceEur,
    Zone Zone, IReadOnlyList<string> Reasons);

public sealed record TradeRecent(
    DateOnly ExecutedOn, TradeSide Side,
    decimal Quantity, decimal PricePerShare);

public sealed record AiSnapshot(
    DateOnly Today,
    GoalConfig Goal,
    PortfolioSnapshot Portfolio,
    IReadOnlyList<TickerContext> Tickers,
    IReadOnlyList<TradeRecent> RecentTrades,
    decimal? UsdPerEur,
    IReadOnlyList<PredictionMarket> Markets,    // NEW
    string PromptHash);
```

- [ ] **Step 2: Build will surface every caller that constructs `AiSnapshot` positionally — fix them**

Run: `dotnet build`
Expected: 2-4 build errors in test files where `new AiSnapshot(...)` is called positionally. For each callsite, insert `Markets: []` between `UsdPerEur` and `PromptHash`. Concretely:

In `TradyStrat.Tests/AiSuggestion/SuggestionServiceTests.cs:16-23`, change `SampleSnapshot()` to:

```csharp
private static AiSnapshot SampleSnapshot() => new(
    Today: new DateOnly(2026, 5, 6),
    Goal: GoalConfig.Default(DateTime.UtcNow),
    Portfolio: new([], 0, 0, 0, 0, 0, 0, 0),
    Tickers: [],
    RecentTrades: [],
    UsdPerEur: 1.08m,
    Markets: [],
    PromptHash: "hashvalue");
```

In any other test file flagged by the build error, apply the same `Markets: []` insertion.

- [ ] **Step 3: Run all tests**

Run: `dotnet test`
Expected: all pre-existing tests pass.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/AiSuggestion/Snapshot/AiSnapshot.cs TradyStrat.Tests/
git commit -m "feat(prediction-markets): extend AiSnapshot with Markets list"
```

---

## Task 12 — `SnapshotFactory` calls provider, includes markets in hash, tolerates failure

**Files:**
- Modify: `TradyStrat/Features/AiSuggestion/Snapshot/SnapshotFactory.cs`
- Modify: `TradyStrat.Tests/AiSuggestion/Snapshot/SnapshotFactoryTests.cs`

- [ ] **Step 1: Write new failing tests**

Append to `SnapshotFactoryTests.cs`:

```csharp
[Fact]
public async Task Includes_markets_from_provider_in_snapshot()
{
    await using var db = InMemoryDb.Create();
    var ct = TestContext.Current.CancellationToken;
    SeedInstruments(db);
    SeedDayOneFixtures(db);                                    // existing helper

    var providedMarkets = new[]
    {
        new PredictionMarket("btc-100k", "Will BTC > $100k EOY?",
            0.32m, new DateOnly(2026, 12, 31), 1_000_000m, ["bitcoin"]),
    };
    var sut = BuildSut(db, predictionMarkets: providedMarkets);
    var snap = await sut.CreateAsync(new DateOnly(2026, 5, 6), ct);

    snap.Markets.Count.ShouldBe(1);
    snap.Markets[0].Slug.ShouldBe("btc-100k");
}

[Fact]
public async Task PromptHash_changes_when_markets_change()
{
    await using var db = InMemoryDb.Create();
    var ct = TestContext.Current.CancellationToken;
    SeedInstruments(db);
    SeedDayOneFixtures(db);

    var snap1 = await BuildSut(db, predictionMarkets: []).CreateAsync(new DateOnly(2026, 5, 6), ct);
    var snap2 = await BuildSut(db, predictionMarkets: new[]
    {
        new PredictionMarket("btc-100k", "Will BTC > $100k EOY?",
            0.32m, new DateOnly(2026, 12, 31), 1_000_000m, ["bitcoin"]),
    }).CreateAsync(new DateOnly(2026, 5, 6), ct);

    snap1.PromptHash.ShouldNotBe(snap2.PromptHash);
}

[Fact]
public async Task Tolerates_provider_failure_returns_empty_markets()
{
    await using var db = InMemoryDb.Create();
    var ct = TestContext.Current.CancellationToken;
    SeedInstruments(db);
    SeedDayOneFixtures(db);

    var sut = BuildSut(db, predictionMarketsThrow: true);
    var snap = await sut.CreateAsync(new DateOnly(2026, 5, 6), ct);

    snap.Markets.ShouldBeEmpty();
}
```

Update the existing `BuildSut` helper to accept the new dependency:

```csharp
private static SnapshotFactory BuildSut(
    AppDbContext db,
    string focusTicker = "CON3.L",
    IReadOnlyList<PredictionMarket>? predictionMarkets = null,
    bool predictionMarketsThrow = false)
{
    // ... existing setup of classifier/engine/portfolio/fx/clock/listInstruments/config ...
    var provider = new StubPredictionMarketProvider(
        predictionMarkets ?? [],
        shouldThrow: predictionMarketsThrow);
    return new SnapshotFactory(engine, portfolio, fx,
        new TestRepo<GoalConfig>(db), new TestRepo<Trade>(db),
        listInstruments, config, provider, clock);
}

private sealed class StubPredictionMarketProvider(
    IReadOnlyList<PredictionMarket> markets, bool shouldThrow) : IPredictionMarketProvider
{
    public Task<IReadOnlyList<PredictionMarket>> GetMarketsAsync(CancellationToken ct) =>
        shouldThrow
            ? Task.FromException<IReadOnlyList<PredictionMarket>>(
                new PolymarketUnavailableException("stub failure"))
            : Task.FromResult(markets);
}
```

Add the `using` directives at the top of the file:
```csharp
using TradyStrat.Common.Exceptions;
using TradyStrat.Features.PredictionMarkets;
```

If `SeedDayOneFixtures` doesn't exist in your file under that name, identify the existing helper that seeds fixtures and reuse it (likely something like `SeedFixtureData` or invoked inline in the existing day-one test).

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test --filter "FullyQualifiedName~SnapshotFactoryTests"`
Expected: build error — `SnapshotFactory` constructor doesn't take `IPredictionMarketProvider`.

- [ ] **Step 3: Update `SnapshotFactory.cs`**

Add `IPredictionMarketProvider predictionMarkets` to the constructor (place it before `IClock clock`). Wire it inside `CreateAsync` and tolerate failures:

```csharp
public sealed class SnapshotFactory(
    IndicatorEngine indicators,
    PortfolioService portfolio,
    FxConverter fx,
    IReadRepositoryBase<GoalConfig> goalRepo,
    IReadRepositoryBase<Trade> tradeRepo,
    ListInstrumentsUseCase listInstruments,
    IConfiguration config,
    IPredictionMarketProvider predictionMarkets,   // NEW
    IClock clock) : ISnapshotFactory
{
    // ... existing private fields/method bodies kept ...

    public async Task<AiSnapshot> CreateAsync(DateOnly asOf, CancellationToken ct)
    {
        // ... existing logic up through computing recentDtos and usdPerEur ...

        // NEW: prediction markets — graceful degradation, never blocks the AI call.
        IReadOnlyList<PredictionMarket> markets;
        try
        {
            markets = await predictionMarkets.GetMarketsAsync(ct);
        }
        catch (PolymarketUnavailableException)
        {
            markets = [];
        }

        var promptHash = HashPrompt(asOf, snap, tickers, recentDtos, markets);

        return new AiSnapshot(asOf, goal, snap, tickers, recentDtos, usdPerEur, markets, promptHash);
    }

    private static string HashPrompt(DateOnly today, PortfolioSnapshot snap,
        IEnumerable<TickerContext> tickers, IEnumerable<TradeRecent> recent,
        IEnumerable<PredictionMarket> markets)
    {
        var payload = new { today, snap, tickers, recent, markets };
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOpts.Strict));
        return Convert.ToHexString(SHA256.HashData(bytes))[..16];
    }
}
```

Add `using TradyStrat.Features.PredictionMarkets;` and `using TradyStrat.Common.Exceptions;` at the top of the file.

- [ ] **Step 4: Run tests**

Run: `dotnet test --filter "FullyQualifiedName~SnapshotFactoryTests"`
Expected: existing tests still pass; the 3 new tests pass.

Run: `dotnet test`
Expected: full suite green.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/AiSuggestion/Snapshot/SnapshotFactory.cs \
        TradyStrat.Tests/AiSuggestion/Snapshot/SnapshotFactoryTests.cs
git commit -m "feat(prediction-markets): inject provider into SnapshotFactory with graceful degradation"
```

---

## Task 13 — `SuggestionService` system prompt + `market_citations` tool param + hygiene

**Files:**
- Modify: `TradyStrat/Features/AiSuggestion/SuggestionService.cs`

- [ ] **Step 1: Update the system prompt and tool delegate**

Replace the `SystemPrompt` constant body with the addended version (note: it's a `const` `string`, so an updated literal is the only change):

```csharp
private const string SystemPrompt = """
    You are a disciplined trading assistant for a personal accumulation strategy
    on CON3 (a 3x leveraged Coinbase ETP). You see snapshots once per day.
    Cite which indicators support each part of your suggestion.
    Be conservative: when signals conflict, say Hold.
    Always invoke the submit_suggestion tool exactly once.

    You may also cite Polymarket markets you weighed.
    Each market_citations[].slug MUST appear in the snapshot's markets[].
    Cite each market at most once.
    Cite a market only when you actually weighted it; not every market needs a citation.
    """;
```

- [ ] **Step 2: Update the tool delegate signature in `AskAsync`**

Modify the lambda passed to `AIFunctionFactory.Create` to add the new parameter and dedupe / drop unknowns:

```csharp
var submit = AIFunctionFactory.Create(
    (SuggestionAction action, decimal? quantity_hint, decimal? max_price_hint,
     int conviction, string rationale,
     IReadOnlyList<Citation>? citations,
     IReadOnlyList<MarketCitation>? market_citations) =>
    {
        var citationList = citations ?? [];

        var validSlugs = snapshot.Markets.Select(m => m.Slug).ToHashSet();
        var cleanedMarketCitations = (market_citations ?? [])
            .Where(c =>
            {
                if (validSlugs.Contains(c.Slug)) return true;
                LogUnknownMarketCitation(log, c.Slug);
                return false;
            })
            .GroupBy(c => c.Slug)
            .Select(g => g.First())
            .ToList();

        var marketJson = snapshot.Markets.Count == 0
            ? null
            : JsonSerializer.Serialize(
                new MarketSnapshot(snapshot.Markets, cleanedMarketCitations),
                JsonOpts.Strict);

        captured = new Suggestion
        {
            Id           = 0,
            ForDate      = snapshot.Today,
            Action       = action,
            QuantityHint = quantity_hint,
            MaxPriceHint = max_price_hint,
            Conviction   = conviction,
            Rationale    = rationale,
            CitationsJson = JsonSerializer.Serialize(citationList, JsonOpts.Strict),
            MarketSnapshotJson = marketJson,
            PromptHash   = snapshot.PromptHash,
            CreatedAt    = clock.UtcNow(),
        };
        return "ok";
    },
    name: ToolName,
    description: "Submit your structured trading suggestion with cited reasoning.");
```

- [ ] **Step 3: Add the `LoggerMessage` partial**

Append at the bottom of the file alongside the existing `LogCallFailed`:

```csharp
[LoggerMessage(Level = LogLevel.Warning, Message = "AI cited unknown market slug {Slug}; dropped")]
private static partial void LogUnknownMarketCitation(ILogger logger, string slug);
```

Add the `using` directives at the top:
```csharp
using TradyStrat.Features.PredictionMarkets;
```

- [ ] **Step 4: Run all existing AiSuggestion tests**

Run: `dotnet test --filter "FullyQualifiedName~AiSuggestion"`
Expected: existing tests still green (note: pre-existing `Captures_tool_invocation` test doesn't pass `market_citations` — that's still valid since the param is `IReadOnlyList<MarketCitation>?` nullable).

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/AiSuggestion/SuggestionService.cs
git commit -m "feat(prediction-markets): extend submit_suggestion tool with market_citations + hygiene"
```

---

## Task 14 — Citation hygiene tests (new file)

**Files:**
- Create: `TradyStrat.Tests/AiSuggestion/Citations/SuggestionServiceCitationTests.cs`

- [ ] **Step 1: Write the tests**

```csharp
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Common.Domain;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Features.AiSuggestion.Snapshot;
using TradyStrat.Features.PredictionMarkets;
using TradyStrat.Tests.AiSuggestion;          // FakeChatClient
using TradyStrat.Tests.Common.Time;
using Xunit;

namespace TradyStrat.Tests.AiSuggestion.Citations;

public class SuggestionServiceCitationTests
{
    private static AiSnapshot SnapshotWith(params PredictionMarket[] markets) => new(
        Today: new DateOnly(2026, 5, 6),
        Goal: GoalConfig.Default(DateTime.UtcNow),
        Portfolio: new([], 0, 0, 0, 0, 0, 0, 0),
        Tickers: [],
        RecentTrades: [],
        UsdPerEur: 1.08m,
        Markets: markets,
        PromptHash: "h");

    private static PredictionMarket M(string slug) =>
        new(slug, slug, 0.5m, new DateOnly(2026, 12, 31), 1m, []);

    [Fact]
    public async Task Drops_unknown_market_slugs_in_citations()
    {
        var clock = new FakeClock(new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc));
        var svc = new SuggestionService(
            new FakeChatClient(InvokeWithCitations([
                ("known", "claim-a"), ("unknown", "claim-b")
            ])),
            clock, NullLogger<SuggestionService>.Instance);

        var sug = await svc.AskAsync(SnapshotWith(M("known")), TestContext.Current.CancellationToken);

        sug.MarketSnapshotJson.ShouldNotBeNull();
        var snap = JsonSerializer.Deserialize<MarketSnapshot>(sug.MarketSnapshotJson!, JsonOpts.Strict)!;
        snap.Cited.Select(c => c.Slug).ShouldBe(["known"]);
    }

    [Fact]
    public async Task Dedupes_duplicate_market_citations_first_wins()
    {
        var clock = new FakeClock(new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc));
        var svc = new SuggestionService(
            new FakeChatClient(InvokeWithCitations([
                ("dup", "first-claim"), ("dup", "second-claim")
            ])),
            clock, NullLogger<SuggestionService>.Instance);

        var sug = await svc.AskAsync(SnapshotWith(M("dup")), TestContext.Current.CancellationToken);

        var snap = JsonSerializer.Deserialize<MarketSnapshot>(sug.MarketSnapshotJson!, JsonOpts.Strict)!;
        snap.Cited.Count.ShouldBe(1);
        snap.Cited[0].Claim.ShouldBe("first-claim");
    }

    [Fact]
    public async Task MarketSnapshotJson_is_NULL_when_snapshot_has_no_markets()
    {
        var clock = new FakeClock(new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc));
        var svc = new SuggestionService(
            new FakeChatClient(InvokeWithCitations([])),
            clock, NullLogger<SuggestionService>.Instance);

        var sug = await svc.AskAsync(SnapshotWith(/* no markets */), TestContext.Current.CancellationToken);

        sug.MarketSnapshotJson.ShouldBeNull();
    }

    private static Func<IList<AIFunction>, Task> InvokeWithCitations(
        IReadOnlyList<(string Slug, string Claim)> citations) =>
        async tools =>
        {
            var t = tools.Single();
            var fnArgs = new AIFunctionArguments();
            foreach (var prop in JsonSerializer.SerializeToElement(new
            {
                action = "Hold",
                conviction = 1,
                rationale = "test",
                citations = Array.Empty<object>(),
                market_citations = citations.Select(c => new { slug = c.Slug, claim = c.Claim }).ToArray(),
            }).EnumerateObject())
                fnArgs.Add(prop.Name, prop.Value);
            await t.InvokeAsync(fnArgs, TestContext.Current.CancellationToken);
        };
}
```

- [ ] **Step 2: Run the tests**

Run: `dotnet test --filter "FullyQualifiedName~SuggestionServiceCitationTests"`
Expected: 3 tests pass.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat.Tests/AiSuggestion/Citations/SuggestionServiceCitationTests.cs
git commit -m "test(prediction-markets): cover unknown-slug drop, dedup, and NULL-when-empty"
```

---

## Task 15 — `DashboardViewModel` extension

**Files:**
- Modify: `TradyStrat/Features/Dashboard/DashboardViewModel.cs`

- [ ] **Step 1: Add the property**

Open `DashboardViewModel.cs`. Add the new property to the record (positional or property-init, matching the file's existing style — for a positional record, append after the last positional field; for a class with init properties, add at the end):

```csharp
public MarketSnapshot MarketSnapshot { get; init; } = MarketSnapshot.Empty;
```

If `DashboardViewModel` is a positional record (typical for this project — see how `LoadDashboardUseCase.cs` invokes it), the cleaner approach is to keep it as init-only with a default rather than adding a positional. Use a `with`-init from the `LoadDashboardUseCase` invocation in Task 16. Add this property as a non-positional init-only.

Add `using TradyStrat.Features.PredictionMarkets;` at the top.

- [ ] **Step 2: Build**

Run: `dotnet build TradyStrat/TradyStrat.csproj`
Expected: 0 errors. (Existing callsites that build the VM via positional or named args remain valid; the new property defaults to `Empty`.)

- [ ] **Step 3: Commit**

```bash
git add TradyStrat/Features/Dashboard/DashboardViewModel.cs
git commit -m "feat(prediction-markets): add MarketSnapshot to DashboardViewModel"
```

---

## Task 16 — `LoadDashboardUseCase` deserializes `MarketSnapshotJson`

**Files:**
- Modify: `TradyStrat/Features/Dashboard/UseCases/LoadDashboardUseCase.cs`
- Modify: `TradyStrat.Tests/Dashboard/UseCases/LoadDashboardUseCaseTests.cs`

- [ ] **Step 1: Write the failing tests**

Append to `LoadDashboardUseCaseTests.cs`:

```csharp
[Fact]
public async Task Deserializes_market_snapshot_when_present()
{
    await using var db = InMemoryDb.Create();
    var ct = TestContext.Current.CancellationToken;
    SeedDayOneFixtures(db);   // existing helper

    var snap = new MarketSnapshot(
        Markets: [new PredictionMarket("btc-100k", "Will BTC > $100k?",
            0.32m, new DateOnly(2026, 12, 31), 1m, ["bitcoin"])],
        Cited:   [new MarketCitation("btc-100k", "weighed for context")]);
    var marketJson = JsonSerializer.Serialize(snap, JsonOpts.Strict);

    db.Suggestions.Add(new Suggestion
    {
        Id = 0, ForDate = new DateOnly(2026, 5, 6),
        Action = SuggestionAction.Hold, Conviction = 1,
        Rationale = "x", CitationsJson = "[]",
        MarketSnapshotJson = marketJson,
        PromptHash = "h", CreatedAt = DateTime.UtcNow,
    });
    await db.SaveChangesAsync(ct);

    var vm = await BuildSut(db).ExecuteAsync(
        new LoadDashboardInput(TargetDate: new DateOnly(2026, 5, 6), IsHistorical: true), ct);

    vm.MarketSnapshot.Markets.Count.ShouldBe(1);
    vm.MarketSnapshot.Cited.Count.ShouldBe(1);
}

[Fact]
public async Task Tolerates_malformed_market_snapshot_json()
{
    await using var db = InMemoryDb.Create();
    var ct = TestContext.Current.CancellationToken;
    SeedDayOneFixtures(db);

    db.Suggestions.Add(new Suggestion
    {
        Id = 0, ForDate = new DateOnly(2026, 5, 6),
        Action = SuggestionAction.Hold, Conviction = 1,
        Rationale = "x", CitationsJson = "[]",
        MarketSnapshotJson = "{ this is broken",
        PromptHash = "h", CreatedAt = DateTime.UtcNow,
    });
    await db.SaveChangesAsync(ct);

    var vm = await BuildSut(db).ExecuteAsync(
        new LoadDashboardInput(TargetDate: new DateOnly(2026, 5, 6), IsHistorical: true), ct);

    vm.MarketSnapshot.ShouldBe(MarketSnapshot.Empty);
}
```

Add the `using` directives at the top of the file:
```csharp
using System.Text.Json;
using TradyStrat.Features.AiSuggestion;        // JsonOpts
using TradyStrat.Features.PredictionMarkets;
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test --filter "FullyQualifiedName~LoadDashboardUseCaseTests"`
Expected: build error or test failure — `MarketSnapshot` not on VM, or always defaulting to `Empty`.

- [ ] **Step 3: Modify `LoadDashboardUseCase.cs`**

Add the deserialization block immediately after the `todays = ...` block (after the if/else that assigns `todays`, before the prior/CallDiff branch):

```csharp
var marketSnap = MarketSnapshot.Empty;
if (todays?.MarketSnapshotJson is { Length: > 0 } marketJson)
{
    try
    {
        marketSnap = JsonSerializer.Deserialize<MarketSnapshot>(marketJson, JsonOpts.Strict)
                     ?? MarketSnapshot.Empty;
    }
    catch (JsonException ex)
    {
        LoadDashboardLog.MarketSnapshotMalformed(log, ex);
    }
}
```

Pass it into the `DashboardViewModel` construction at the end of `ExecuteCore` — find the `return new DashboardViewModel(...)` and add `MarketSnapshot = marketSnap` (use the init-only syntax). If the constructor is positional, switch the call to mostly-init style and add `MarketSnapshot` there.

Add the new logger method to the existing `LoadDashboardLog` partial class at the bottom of the file:

```csharp
[LoggerMessage(Level = LogLevel.Warning, Message = "MarketSnapshotJson malformed; rail will not render")]
public static partial void MarketSnapshotMalformed(ILogger logger, Exception ex);
```

Add `using TradyStrat.Features.PredictionMarkets;` and `using TradyStrat.Features.AiSuggestion;` (for `JsonOpts`) and `using System.Text.Json;` to the top of the file.

- [ ] **Step 4: Run tests**

Run: `dotnet test --filter "FullyQualifiedName~LoadDashboardUseCaseTests"`
Expected: existing tests pass; the 2 new tests pass.

Run: `dotnet test`
Expected: whole suite green.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/Dashboard/UseCases/LoadDashboardUseCase.cs \
        TradyStrat.Tests/Dashboard/UseCases/LoadDashboardUseCaseTests.cs
git commit -m "feat(prediction-markets): deserialize MarketSnapshotJson in LoadDashboardUseCase"
```

---

## Task 17 — `MarketsRail` component (markup + code-behind)

**Files:**
- Create: `TradyStrat/Features/Dashboard/Components/MarketsRail.razor`
- Create: `TradyStrat/Features/Dashboard/Components/MarketsRail.razor.cs`

- [ ] **Step 1: Create `MarketsRail.razor`**

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

- [ ] **Step 2: Create `MarketsRail.razor.cs`**

```csharp
using System.Globalization;
using Microsoft.AspNetCore.Components;
using TradyStrat.Features.PredictionMarkets;

namespace TradyStrat.Features.Dashboard.Components;

public partial class MarketsRail : ComponentBase
{
    [Parameter, EditorRequired]
    public MarketSnapshot Snapshot { get; set; } = MarketSnapshot.Empty;

    private Dictionary<string, MarketCitation> _bySlug = new();

    protected override void OnParametersSet()
        => _bySlug = Snapshot.Cited.ToDictionary(c => c.Slug);   // hygiene already deduped (Task 13)

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");
}
```

- [ ] **Step 3: Build to confirm Razor compiles**

Run: `dotnet build TradyStrat/TradyStrat.csproj`
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/MarketsRail.razor \
        TradyStrat/Features/Dashboard/Components/MarketsRail.razor.cs
git commit -m "feat(prediction-markets): add MarketsRail component (markup + code-behind)"
```

---

## Task 18 — `MarketsRail` styling

**Files:**
- Create: `TradyStrat/Features/Dashboard/Components/MarketsRail.razor.css`

- [ ] **Step 1: Create the CSS**

```css
.markets-rail {
    margin: 1rem 0;
    padding: 0.75rem 1rem;
    background: rgba(255, 255, 255, 0.03);
    border: 1px solid rgba(255, 255, 255, 0.06);
    border-radius: 8px;
}

.rail-label {
    font-size: 0.7rem;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: rgba(255, 255, 255, 0.45);
    margin-bottom: 0.5rem;
}

.market-tiles {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 0.5rem;
}

.tile {
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 6px;
    padding: 0.6rem;
    font-size: 0.8rem;
    line-height: 1.35;
}

.tile.cited {
    border-color: rgba(241, 196, 15, 0.35);
    background: rgba(241, 196, 15, 0.04);
}

.tile .prob {
    font-weight: 700;
    font-size: 1.15rem;
    color: rgba(255, 255, 255, 0.7);
    font-variant-numeric: tabular-nums;
}

.tile.cited .prob {
    color: #f1c40f;
}

.tile .question {
    margin-top: 0.25rem;
    color: rgba(255, 255, 255, 0.85);
}

.tile .meta {
    margin-top: 0.4rem;
    font-size: 0.7rem;
    color: rgba(255, 255, 255, 0.45);
    font-variant-numeric: tabular-nums;
}

.tile .claim {
    margin-top: 0.4rem;
    font-size: 0.75rem;
    color: rgba(255, 255, 255, 0.7);
    border-top: 1px solid rgba(255, 255, 255, 0.08);
    padding-top: 0.4rem;
}

@media (max-width: 700px) {
    .market-tiles {
        grid-template-columns: 1fr;
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build TradyStrat/TradyStrat.csproj`
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/MarketsRail.razor.css
git commit -m "feat(prediction-markets): style MarketsRail with cited-tile highlight"
```

---

## Task 19 — Wire `<MarketsRail>` into `DashboardPage.razor`

**Files:**
- Modify: `TradyStrat/Features/Dashboard/DashboardPage.razor`

- [ ] **Step 1: Insert the component**

Find the line (in the `else` branch after the masthead + hero-row):

```razor
@if (vm.TodaysCall is not null)
{
    <CitationsBlock Sug="vm.TodaysCall" Today="vm.Today"
                    CallDiff="vm.CallDiff"
                    IndicatorHistories="vm.IndicatorHistories" />
}
```

Insert *before* this `@if` block:

```razor
<MarketsRail Snapshot="vm.MarketSnapshot" />
```

- [ ] **Step 2: Run the dev server and eyeball the rail**

```bash
dotnet run --project TradyStrat
```

Open `http://127.0.0.1:5180`. On the day the AI ran with markets, the rail appears under Today's Call; on a day with no markets / NULL `MarketSnapshotJson`, the rail is absent (no empty banner). To confirm the cited-vs-not styling, you'll need at least one Suggestion row in the DB with both populated `Markets` and at least one `Cited` entry — easiest check is to navigate to a date you've re-run the AI for after this branch lands.

- [ ] **Step 3: Run all tests**

Run: `dotnet test`
Expected: full suite green.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Dashboard/DashboardPage.razor
git commit -m "feat(prediction-markets): render MarketsRail under Today's Call"
```

---

## Task 20 — End-to-end manual smoke

**Files:** none (manual verification)

- [ ] **Step 1: Confirm the AI flow with markets**

```bash
dotnet run --project TradyStrat
```

In the browser, click **Re-run AI**. The Anthropic call now sees Polymarket markets in the snapshot. The AI may or may not cite specific markets — that's its choice.

- [ ] **Step 2: Inspect the persisted row**

```bash
sqlite3 "$HOME/Library/Application Support/TradyStrat/tradystrat.db" \
  "SELECT ForDate, length(MarketSnapshotJson), substr(MarketSnapshotJson, 1, 200) FROM Suggestions ORDER BY ForDate DESC LIMIT 1;"
```

Expected: today's row has a non-NULL `MarketSnapshotJson` of a few KB containing both `markets` and `cited` keys.

- [ ] **Step 3: Inspect the rail visually**

The rail appears between Today's Call and Citations, with all pre-filtered markets as tiles. Cited tiles (if any) have the gold outline + ★.

- [ ] **Step 4: Verify failure-mode UI**

Stop the app. Edit `appsettings.json` and set `Polymarket:BaseUrl` to `https://gamma-api.polymarket.invalid` (intentionally bad host). Restart and click **Re-run AI**. The AI call still completes (different rationale, same shape); the new Suggestion row has `MarketSnapshotJson = NULL`; the rail does not render. Check `tradystrat-yyyymmdd.log` for the `Polymarket unavailable` warning.

Restore the original `BaseUrl`.

- [ ] **Step 5: Verify time-travel**

Use the masthead's date arrows to step back to a pre-feature day. The rail must not render (the column is NULL on those rows, no empty state).

- [ ] **Step 6: Confirm no commits needed**

This is a verification task; nothing to commit.

---

## Self-Review

After writing the plan, I checked it against the spec:

**Spec coverage:**
- §3 Domain model → Tasks 1, 2, 10
- §4 Module shape → Tasks 3, 9
- §5 Provider contract → Tasks 3, 8
- §6 Polymarket Gamma API → Tasks 5, 6, 7, 8
- §7 AI integration → Tasks 11, 12, 13, 14
- §8 Configuration → Tasks 4, 9
- §9 Dashboard surface → Tasks 15, 16, 17, 18, 19
- §10 Failure handling → Task 1 (exception), Task 8 (provider), Task 12 (factory), Task 16 (dashboard)
- §11 DB migration → Task 10
- §12 Testing → Tasks 4, 6, 7, 8, 12, 14, 16

**Type consistency:**
- `IPredictionMarketProvider.GetMarketsAsync(CancellationToken)` — interface in Task 3, impl in Task 8, consumed in Task 12. Consistent.
- `MarketSnapshot.Empty` — defined in Task 2, consumed in Tasks 15, 16, 17.
- `PolymarketOptionsBinder.Read` — defined in Task 4, consumed in Task 9.
- `LoadDashboardLog.MarketSnapshotMalformed` — declared in Task 16 (not split into a separate task because it's tightly coupled to the use-case change).

**Placeholder scan:** no "TBD" / "implement later" / vague-validation prose in any step.

**Spec consistency check:**
- Spec §12.1 places tests under `TradyStrat.Tests/Features/PredictionMarkets/...`, but the actual test root is `TradyStrat.Tests/<feature>/...` (no `Features/` segment). Plan uses the correct paths.
- Spec §3.5 says `MarketSnapshotJson` has no `HasMaxLength` ("match `CitationsJson`"); actually `CitationsJson` has `HasMaxLength(8000)`. Plan reproduces the existing convention with `HasMaxLength(8000)`.

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-05-07-prediction-markets.md`. Two execution options:

**1. Subagent-Driven (recommended)** — I dispatch a fresh subagent per task, review between tasks, fast iteration.

**2. Inline Execution** — Execute tasks in this session using executing-plans, batch execution with checkpoints.

**Which approach?**
