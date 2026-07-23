# TradyStrat — Dashboard depth pass — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add five interaction-depth features to the existing dashboard (chart crosshair, goal-pace banner, today's-call diff with eager AI backfill, inline freshness pills, indicator sparklines) without schema or new exception types.

**Architecture:** All work fits the existing feature-folder + use-case + Ardalis.Specification pattern. New Strategy/Builder/Factory/Singleton/Observer/Decorator components wire into existing modules; one Razor JSInterop module for crosshair. Indicator history is computed on demand (no cache table). AI backfill of missing days runs sequentially in chronological order via a `Singleton + Observer` coordinator the UI subscribes to.

**Tech Stack:** .NET 10 · Blazor Server · EF Core 10 + SQLite · Ardalis.Specification · TaLibStandard · `Microsoft.Extensions.AI` `IChatClient` (via `IAiClient` adapter) · xunit.v3 + Shouldly + EF InMemory · scoped CSS · ES module JSInterop.

**Spec:** [`docs/superpowers/specs/2026-05-07-tradystrat-dashboard-depth-design.md`](../specs/2026-05-07-tradystrat-dashboard-depth-design.md)

---

## File map

### New files
```
TradyStrat/
├─ Shared/Domain/
│  ├─ IndicatorKind.cs                                  # enum
│  └─ IndicatorKindParser.cs                            # citation-string → enum
├─ Shared/Time/
│  └─ RelativeTimeFormatter.cs                          # "12 min ago"
├─ Specifications/
│  ├─ Suggestions/SuggestionsInRangeSpec.cs
│  ├─ Suggestions/PriorSuggestionSpec.cs
│  ├─ Trades/TradesAsOfSpec.cs
│  ├─ PriceBars/PriceBarsAsOfSpec.cs
│  └─ FxRates/FxRateAsOfSpec.cs
├─ Features/Indicators/
│  ├─ IndicatorSeries.cs                                # value record
│  ├─ IIndicatorHistoryProvider.cs                      # Strategy interface
│  ├─ HistoryProviders/
│  │  ├─ RsiHistoryProvider.cs
│  │  ├─ BollingerHistoryProvider.cs
│  │  ├─ IchimokuHistoryProvider.cs
│  │  └─ Sma200HistoryProvider.cs
│  └─ IndicatorHistoryProviderFactory.cs                # Factory
├─ Features/AiSuggestion/
│  ├─ ISnapshotFactory.cs                               # replaces ISnapshotBuilder
│  ├─ SnapshotFactory.cs                                # replaces SnapshotBuilder
│  ├─ CallDiff.cs                                       # record
│  ├─ CitationChange.cs                                 # record
│  ├─ CallDiffBuilder.cs                                # Builder
│  ├─ BackfillStatus.cs                                 # discriminated record
│  └─ SuggestionBackfillCoordinator.cs (+ interface)    # Singleton+Observer
├─ Features/Dashboard/
│  ├─ GoalPaceCalculator.cs (+ GoalPaceVm + GoalPaceMode)
│  └─ Components/                                       # see "Modified" + JS
├─ Application/UseCases/AiSuggestion/
│  └─ BackfillSuggestionsUseCase.cs
└─ wwwroot/js/
   └─ growth-chart.js                                   # ES module for crosshair
```

### Deleted files
```
TradyStrat/Features/AiSuggestion/ISnapshotBuilder.cs   (renamed → ISnapshotFactory)
TradyStrat/Features/AiSuggestion/SnapshotBuilder.cs    (renamed → SnapshotFactory)
```

### Modified files
```
Features/Indicators/IndicatorEngine.cs                  # add HistoryFor + as-of ComputeFor
Features/Portfolio/PortfolioService.cs                  # add as-of SnapshotAsync overload
Features/Dashboard/DashboardViewModel.cs                # 7 new fields
Application/UseCases/Dashboard/LoadDashboardUseCase.cs  # populate new fields, queue backfill
Application/UseCases/AiSuggestion/GetTodaysSuggestionUseCase.cs    # ISnapshotFactory
Application/UseCases/AiSuggestion/ForceRefetchSuggestionUseCase.cs # ISnapshotFactory
Modules/AiSuggestionModule.cs                           # DI updates
Features/Dashboard/Components/HeroCapital.razor + .cs   # goal-pace banner
Features/Dashboard/Components/VaultMasthead.razor       # inline freshness on date strip
Features/Dashboard/Components/TodaysCallCard.razor + .cs + .css  # diff + sparklines + freshness + backfill
Features/Dashboard/Components/GrowthChart.razor + .cs   # JSInterop wiring
TradyStrat.Tests/Specifications/SpecsRoundtripTests.cs  # 5 new [Fact]s
TradyStrat.Tests/AiSuggestion/SnapshotBuilderTests.cs   # rename to SnapshotFactoryTests
```

---

# Phase 1 — Foundation domain types

## Task 1 — `IndicatorKind` enum + `IndicatorKindParser`

**Files:**
- Create: `TradyStrat/Shared/Domain/IndicatorKind.cs`
- Create: `TradyStrat/Shared/Domain/IndicatorKindParser.cs`
- Test: `TradyStrat.Tests/Domain/IndicatorKindParserTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// TradyStrat.Tests/Domain/IndicatorKindParserTests.cs
using Shouldly;
using TradyStrat.Shared.Domain;
using Xunit;

namespace TradyStrat.Tests.Domain;

public class IndicatorKindParserTests
{
    [Theory]
    [InlineData("RSI(14)",   IndicatorKind.Rsi)]
    [InlineData("RSI",       IndicatorKind.Rsi)]
    [InlineData("Bollinger", IndicatorKind.Bollinger)]
    [InlineData("Ichimoku",  IndicatorKind.Ichimoku)]
    [InlineData("200-SMA",   IndicatorKind.Sma200)]
    [InlineData("SMA-200",   IndicatorKind.Sma200)]
    [InlineData("50-SMA",    IndicatorKind.Sma50)]
    public void Maps_known_labels(string label, IndicatorKind expected)
        => IndicatorKindParser.From(label).ShouldBe(expected);

    [Theory]
    [InlineData("MACD")]
    [InlineData("")]
    [InlineData("unknown")]
    public void Returns_null_for_unknown(string label)
        => IndicatorKindParser.From(label).ShouldBeNull();

    [Fact]
    public void Null_input_returns_null()
        => IndicatorKindParser.From(null!).ShouldBeNull();
}
```

- [ ] **Step 2: Run to verify failure**

```
dotnet test --filter "FullyQualifiedName~IndicatorKindParserTests"
```
Expected: build error (`IndicatorKind` not defined).

- [ ] **Step 3: Add `IndicatorKind` enum**

```csharp
// TradyStrat/Shared/Domain/IndicatorKind.cs
namespace TradyStrat.Shared.Domain;

public enum IndicatorKind
{
    Rsi = 1,
    Bollinger = 2,
    Ichimoku = 3,
    Sma50 = 4,
    Sma200 = 5,
}
```

- [ ] **Step 4: Add `IndicatorKindParser`**

```csharp
// TradyStrat/Shared/Domain/IndicatorKindParser.cs
namespace TradyStrat.Shared.Domain;

public static class IndicatorKindParser
{
    public static IndicatorKind? From(string? citationLabel)
    {
        if (string.IsNullOrWhiteSpace(citationLabel)) return null;
        var label = citationLabel.Trim();

        if (label.StartsWith("RSI", StringComparison.OrdinalIgnoreCase))
            return IndicatorKind.Rsi;
        if (label.Equals("Bollinger", StringComparison.OrdinalIgnoreCase))
            return IndicatorKind.Bollinger;
        if (label.Equals("Ichimoku", StringComparison.OrdinalIgnoreCase))
            return IndicatorKind.Ichimoku;
        if (label.Contains("200", StringComparison.Ordinal) &&
            label.Contains("SMA", StringComparison.OrdinalIgnoreCase))
            return IndicatorKind.Sma200;
        if (label.Contains("50", StringComparison.Ordinal) &&
            label.Contains("SMA", StringComparison.OrdinalIgnoreCase))
            return IndicatorKind.Sma50;
        return null;
    }
}
```

- [ ] **Step 5: Run to verify pass**

```
dotnet test --filter "FullyQualifiedName~IndicatorKindParserTests"
```
Expected: PASS (10 tests).

- [ ] **Step 6: Commit**

```bash
git add TradyStrat/Shared/Domain/IndicatorKind.cs \
        TradyStrat/Shared/Domain/IndicatorKindParser.cs \
        TradyStrat.Tests/Domain/IndicatorKindParserTests.cs
git commit -m "feat(domain): add IndicatorKind enum + parser for citation strings"
```

---

## Task 2 — Five new Ardalis specifications

**Files:**
- Create: `TradyStrat/Specifications/Suggestions/SuggestionsInRangeSpec.cs`
- Create: `TradyStrat/Specifications/Suggestions/PriorSuggestionSpec.cs`
- Create: `TradyStrat/Specifications/Trades/TradesAsOfSpec.cs`
- Create: `TradyStrat/Specifications/PriceBars/PriceBarsAsOfSpec.cs`
- Create: `TradyStrat/Specifications/FxRates/FxRateAsOfSpec.cs`
- Test: `TradyStrat.Tests/Specifications/SpecsRoundtripTests.cs` (extend)

- [ ] **Step 1: Write failing tests**

Append to `TradyStrat.Tests/Specifications/SpecsRoundtripTests.cs` (inside the existing class):

```csharp
[Fact]
public async Task SuggestionsInRangeSpec_filters_inclusive_and_orders_ascending()
{
    using var ctx = InMemoryDb.New();
    ctx.Suggestions.AddRange(
        Make(new DateOnly(2026, 5, 1)),
        Make(new DateOnly(2026, 5, 3)),
        Make(new DateOnly(2026, 5, 5)),
        Make(new DateOnly(2026, 5, 7)));
    await ctx.SaveChangesAsync();
    var repo = new RepositoryBase<Suggestion>(ctx);

    var result = await repo.ListAsync(new SuggestionsInRangeSpec(
        new DateOnly(2026, 5, 3), new DateOnly(2026, 5, 6)));

    result.Select(s => s.ForDate).ShouldBe([new DateOnly(2026, 5, 3), new DateOnly(2026, 5, 5)]);
}

[Fact]
public async Task PriorSuggestionSpec_returns_most_recent_strictly_before()
{
    using var ctx = InMemoryDb.New();
    ctx.Suggestions.AddRange(
        Make(new DateOnly(2026, 5, 1)),
        Make(new DateOnly(2026, 5, 5)),
        Make(new DateOnly(2026, 5, 7)));
    await ctx.SaveChangesAsync();
    var repo = new RepositoryBase<Suggestion>(ctx);

    var result = await repo.FirstOrDefaultAsync(new PriorSuggestionSpec(new DateOnly(2026, 5, 7)));

    result.ShouldNotBeNull();
    result.ForDate.ShouldBe(new DateOnly(2026, 5, 5));
}

[Fact]
public async Task TradesAsOfSpec_filters_inclusive_and_orders_ascending()
{
    using var ctx = InMemoryDb.New();
    ctx.Trades.AddRange(
        TradeOn(new DateOnly(2026, 4, 1)),
        TradeOn(new DateOnly(2026, 4, 15)),
        TradeOn(new DateOnly(2026, 5, 2)));
    await ctx.SaveChangesAsync();
    var repo = new RepositoryBase<Trade>(ctx);

    var result = await repo.ListAsync(new TradesAsOfSpec(new DateOnly(2026, 4, 30)));

    result.Select(t => t.ExecutedOn)
        .ShouldBe([new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 15)]);
}

[Fact]
public async Task PriceBarsAsOfSpec_filters_by_ticker_and_date_inclusive()
{
    using var ctx = InMemoryDb.New();
    ctx.PriceBars.AddRange(
        BarOn("CON3.L", new DateOnly(2026, 4, 1)),
        BarOn("CON3.L", new DateOnly(2026, 5, 2)),
        BarOn("COIN",   new DateOnly(2026, 4, 1)));
    await ctx.SaveChangesAsync();
    var repo = new RepositoryBase<PriceBar>(ctx);

    var result = await repo.ListAsync(new PriceBarsAsOfSpec("CON3.L", new DateOnly(2026, 4, 30)));

    result.Count.ShouldBe(1);
    result[0].Ticker.ShouldBe("CON3.L");
    result[0].Date.ShouldBe(new DateOnly(2026, 4, 1));
}

[Fact]
public async Task FxRateAsOfSpec_returns_most_recent_for_pair_le_date()
{
    using var ctx = InMemoryDb.New();
    ctx.FxRates.AddRange(
        FxOn("EURUSD=X", new DateOnly(2026, 4, 28), 1.10m),
        FxOn("EURUSD=X", new DateOnly(2026, 4, 30), 1.11m),
        FxOn("EURUSD=X", new DateOnly(2026, 5, 5),  1.12m));
    await ctx.SaveChangesAsync();
    var repo = new RepositoryBase<FxRate>(ctx);

    var result = await repo.FirstOrDefaultAsync(
        new FxRateAsOfSpec("EURUSD=X", new DateOnly(2026, 5, 1)));

    result.ShouldNotBeNull();
    result.Date.ShouldBe(new DateOnly(2026, 4, 30));
    result.UsdPerEur.ShouldBe(1.11m);
}
```

If the existing test class lacks helper factories `Make`, `TradeOn`, `BarOn`, `FxOn`, look at the existing tests in the same file and use the same fixture style. Otherwise add the small helpers near the top of the class — copy field shapes from the corresponding `Configurations/*Configuration.cs` files in `TradyStrat/Data/`.

- [ ] **Step 2: Run to verify failure**

```
dotnet test --filter "FullyQualifiedName~SpecsRoundtripTests"
```
Expected: 5 build errors (specs not defined yet).

- [ ] **Step 3: Add `SuggestionsInRangeSpec`**

```csharp
// TradyStrat/Specifications/Suggestions/SuggestionsInRangeSpec.cs
using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.Suggestions;

public sealed class SuggestionsInRangeSpec : Specification<Suggestion>
{
    public SuggestionsInRangeSpec(DateOnly fromInclusive, DateOnly toInclusive)
        => Query.Where(s => s.ForDate >= fromInclusive && s.ForDate <= toInclusive)
                .OrderBy(s => s.ForDate);
}
```

- [ ] **Step 4: Add `PriorSuggestionSpec`**

```csharp
// TradyStrat/Specifications/Suggestions/PriorSuggestionSpec.cs
using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.Suggestions;

public sealed class PriorSuggestionSpec : Specification<Suggestion>
{
    public PriorSuggestionSpec(DateOnly beforeExclusive)
        => Query.Where(s => s.ForDate < beforeExclusive)
                .OrderByDescending(s => s.ForDate)
                .Take(1);
}
```

- [ ] **Step 5: Add `TradesAsOfSpec`**

```csharp
// TradyStrat/Specifications/Trades/TradesAsOfSpec.cs
using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.Trades;

public sealed class TradesAsOfSpec : Specification<Trade>
{
    public TradesAsOfSpec(DateOnly asOfInclusive)
        => Query.Where(t => t.ExecutedOn <= asOfInclusive)
                .OrderBy(t => t.ExecutedOn);
}
```

- [ ] **Step 6: Add `PriceBarsAsOfSpec`**

```csharp
// TradyStrat/Specifications/PriceBars/PriceBarsAsOfSpec.cs
using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.PriceBars;

public sealed class PriceBarsAsOfSpec : Specification<PriceBar>
{
    public PriceBarsAsOfSpec(string ticker, DateOnly asOfInclusive)
        => Query.Where(p => p.Ticker == ticker && p.Date <= asOfInclusive)
                .OrderBy(p => p.Date);
}
```

- [ ] **Step 7: Add `FxRateAsOfSpec`**

```csharp
// TradyStrat/Specifications/FxRates/FxRateAsOfSpec.cs
using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.FxRates;

public sealed class FxRateAsOfSpec : Specification<FxRate>
{
    public FxRateAsOfSpec(string pair, DateOnly asOfInclusive)
        => Query.Where(f => f.Pair == pair && f.Date <= asOfInclusive)
                .OrderByDescending(f => f.Date)
                .Take(1);
}
```

- [ ] **Step 8: Run tests**

```
dotnet test --filter "FullyQualifiedName~SpecsRoundtripTests"
```
Expected: PASS (5 new + existing).

- [ ] **Step 9: Commit**

```bash
git add TradyStrat/Specifications/Suggestions/SuggestionsInRangeSpec.cs \
        TradyStrat/Specifications/Suggestions/PriorSuggestionSpec.cs \
        TradyStrat/Specifications/Trades/TradesAsOfSpec.cs \
        TradyStrat/Specifications/PriceBars/PriceBarsAsOfSpec.cs \
        TradyStrat/Specifications/FxRates/FxRateAsOfSpec.cs \
        TradyStrat.Tests/Specifications/SpecsRoundtripTests.cs
git commit -m "feat(specs): add as-of and range specs for backfill + diff queries"
```

---

## Task 3 — `RelativeTimeFormatter`

**Files:**
- Create: `TradyStrat/Shared/Time/RelativeTimeFormatter.cs`
- Test: `TradyStrat.Tests/Time/RelativeTimeFormatterTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// TradyStrat.Tests/Time/RelativeTimeFormatterTests.cs
using Shouldly;
using TradyStrat.Shared.Time;
using Xunit;

namespace TradyStrat.Tests.Time;

public class RelativeTimeFormatterTests
{
    private static readonly DateTime Now = new(2026, 5, 7, 18, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0,    "just now")]
    [InlineData(45,   "just now")]   // < 60 s
    public void Just_now_bucket(int secondsAgo, string expected)
        => RelativeTimeFormatter.Format(Now.AddSeconds(-secondsAgo), Now).ShouldBe(expected);

    [Theory]
    [InlineData(60,    "1 min ago")]
    [InlineData(60*12, "12 min ago")]
    [InlineData(60*59, "59 min ago")]
    public void Minutes_bucket(int secondsAgo, string expected)
        => RelativeTimeFormatter.Format(Now.AddSeconds(-secondsAgo), Now).ShouldBe(expected);

    [Theory]
    [InlineData(60*60,    "1h ago")]
    [InlineData(60*60*14, "14h ago")]
    [InlineData(60*60*23, "23h ago")]
    public void Hours_bucket(int secondsAgo, string expected)
        => RelativeTimeFormatter.Format(Now.AddSeconds(-secondsAgo), Now).ShouldBe(expected);

    [Fact]
    public void Yesterday_when_exactly_one_calendar_day_back()
    {
        var asOf = new DateTime(2026, 5, 6, 18, 0, 0, DateTimeKind.Utc);  // exactly 24h
        RelativeTimeFormatter.Format(asOf, Now).ShouldBe("yesterday");
    }

    [Fact]
    public void Days_bucket_two_to_six()
    {
        var asOf = Now.AddDays(-3);
        RelativeTimeFormatter.Format(asOf, Now).ShouldBe("3 days ago");
    }

    [Fact]
    public void Absolute_when_seven_or_more_days()
    {
        var asOf = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);  // ~25 days back
        RelativeTimeFormatter.Format(asOf, Now).ShouldBe("12 apr");
    }
}
```

- [ ] **Step 2: Run to verify failure**

```
dotnet test --filter "FullyQualifiedName~RelativeTimeFormatterTests"
```
Expected: build error (`RelativeTimeFormatter` not defined).

- [ ] **Step 3: Implement formatter**

```csharp
// TradyStrat/Shared/Time/RelativeTimeFormatter.cs
using System.Globalization;

namespace TradyStrat.Shared.Time;

public static class RelativeTimeFormatter
{
    public static string Format(DateTime asOfUtc, DateTime nowUtc)
    {
        var delta = nowUtc - asOfUtc;
        if (delta.TotalSeconds < 60)  return "just now";
        if (delta.TotalMinutes < 60)  return $"{(int)delta.TotalMinutes} min ago";
        if (delta.TotalHours < 24)    return $"{(int)delta.TotalHours}h ago";
        var calendarDelta = nowUtc.Date.DayNumber() - asOfUtc.Date.DayNumber();
        if (calendarDelta == 1)       return "yesterday";
        if (calendarDelta < 7)        return $"{calendarDelta} days ago";
        return asOfUtc.ToString("dd MMM", CultureInfo.InvariantCulture).ToLowerInvariant();
    }

    private static int DayNumber(this DateTime dt) => DateOnly.FromDateTime(dt).DayNumber;
}
```

- [ ] **Step 4: Run tests**

```
dotnet test --filter "FullyQualifiedName~RelativeTimeFormatterTests"
```
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Shared/Time/RelativeTimeFormatter.cs \
        TradyStrat.Tests/Time/RelativeTimeFormatterTests.cs
git commit -m "feat(time): add RelativeTimeFormatter for inline freshness pills"
```

---

# Phase 2 — Indicator history strategies

## Task 4 — `IIndicatorHistoryProvider` + `IndicatorSeries`

**Files:**
- Create: `TradyStrat/Features/Indicators/IndicatorSeries.cs`
- Create: `TradyStrat/Features/Indicators/IIndicatorHistoryProvider.cs`

No test in this task — just type definitions consumed by the next four tasks.

- [ ] **Step 1: Add `IndicatorSeries` value record**

```csharp
// TradyStrat/Features/Indicators/IndicatorSeries.cs
namespace TradyStrat.Features.Indicators;

public sealed record IndicatorSeries(
    IReadOnlyList<decimal> Values,
    decimal? ThresholdHi,
    decimal? ThresholdLo)
{
    public static readonly IndicatorSeries Empty = new([], null, null);
}
```

- [ ] **Step 2: Add `IIndicatorHistoryProvider` interface**

```csharp
// TradyStrat/Features/Indicators/IIndicatorHistoryProvider.cs
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Indicators;

public interface IIndicatorHistoryProvider
{
    IndicatorKind Kind { get; }
    IndicatorSeries Compute(IReadOnlyList<PriceBar> bars, int lastN);
}
```

- [ ] **Step 3: Confirm build**

```
dotnet build
```
Expected: success.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Indicators/IndicatorSeries.cs \
        TradyStrat/Features/Indicators/IIndicatorHistoryProvider.cs
git commit -m "feat(indicators): add IIndicatorHistoryProvider Strategy interface"
```

---

## Task 5 — `RsiHistoryProvider`

**Files:**
- Create: `TradyStrat/Features/Indicators/HistoryProviders/RsiHistoryProvider.cs`
- Test: `TradyStrat.Tests/Indicators/HistoryProviders/RsiHistoryProviderTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
// TradyStrat.Tests/Indicators/HistoryProviders/RsiHistoryProviderTests.cs
using Shouldly;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Indicators.HistoryProviders;
using TradyStrat.Shared.Domain;
using Xunit;

namespace TradyStrat.Tests.Indicators.HistoryProviders;

public class RsiHistoryProviderTests
{
    private static List<PriceBar> Bars(int n)
    {
        var list = new List<PriceBar>(n);
        var d = new DateOnly(2026, 1, 1);
        for (int i = 0; i < n; i++)
        {
            // Small alternating walk to give RSI something to compute.
            var c = 100m + (i % 2 == 0 ? i * 0.5m : -i * 0.3m);
            list.Add(new PriceBar
            {
                Id = i + 1,
                Ticker = "T",
                Date = d.AddDays(i),
                Open = c, High = c + 1m, Low = c - 1m, Close = c, Volume = 1000,
            });
        }
        return list;
    }

    [Fact]
    public void Kind_is_Rsi()
        => new RsiHistoryProvider().Kind.ShouldBe(IndicatorKind.Rsi);

    [Fact]
    public void Returns_lastN_values_with_70_30_thresholds()
    {
        var s = new RsiHistoryProvider().Compute(Bars(50), 20);

        s.Values.Count.ShouldBe(20);
        s.ThresholdHi.ShouldBe(70m);
        s.ThresholdLo.ShouldBe(30m);
        s.Values.ShouldAllBe(v => v >= 0m && v <= 100m);
    }

    [Fact]
    public void Returns_truncated_when_insufficient_history()
    {
        // Only 10 bars: RSI(14) cannot produce 20 values.
        var s = new RsiHistoryProvider().Compute(Bars(10), 20);
        s.Values.Count.ShouldBeLessThan(20);
    }

    [Fact]
    public void Returns_empty_when_no_bars()
        => new RsiHistoryProvider().Compute([], 20).Values.ShouldBeEmpty();
}
```

- [ ] **Step 2: Run to verify failure**

```
dotnet test --filter "FullyQualifiedName~RsiHistoryProviderTests"
```
Expected: build error.

- [ ] **Step 3: Implement provider**

```csharp
// TradyStrat/Features/Indicators/HistoryProviders/RsiHistoryProvider.cs
using TechnicalAnalysis.Common;
using TechnicalAnalysis.Functions;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Indicators.HistoryProviders;

public sealed class RsiHistoryProvider : IIndicatorHistoryProvider
{
    private const int Period = 14;

    public IndicatorKind Kind => IndicatorKind.Rsi;

    public IndicatorSeries Compute(IReadOnlyList<PriceBar> bars, int lastN)
    {
        if (bars.Count < Period + 1) return IndicatorSeries.Empty;

        var closes = bars.Select(b => (double)b.Close).ToArray();
        var output = new double[closes.Length];
        int begIdx = 0, nb = 0;
        var period = Period;

        var rc = TAFunc.Rsi(
            0, closes.Length - 1, in closes,
            in period, ref begIdx, ref nb, ref output);

        if (rc != RetCode.Success || nb == 0) return IndicatorSeries.Empty;

        var take = Math.Min(lastN, nb);
        var slice = new decimal[take];
        for (int i = 0; i < take; i++) slice[i] = (decimal)output[nb - take + i];

        return new IndicatorSeries(slice, ThresholdHi: 70m, ThresholdLo: 30m);
    }
}
```

- [ ] **Step 4: Run tests**

```
dotnet test --filter "FullyQualifiedName~RsiHistoryProviderTests"
```
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/Indicators/HistoryProviders/RsiHistoryProvider.cs \
        TradyStrat.Tests/Indicators/HistoryProviders/RsiHistoryProviderTests.cs
git commit -m "feat(indicators): add RsiHistoryProvider"
```

---

## Task 6 — `BollingerHistoryProvider`

**Files:**
- Create: `TradyStrat/Features/Indicators/HistoryProviders/BollingerHistoryProvider.cs`
- Test: `TradyStrat.Tests/Indicators/HistoryProviders/BollingerHistoryProviderTests.cs`

Threshold semantics: Values = middle band series (price reference); ThresholdHi = upper band at last bar; ThresholdLo = lower band at last bar.

- [ ] **Step 1: Write failing test**

```csharp
// TradyStrat.Tests/Indicators/HistoryProviders/BollingerHistoryProviderTests.cs
using Shouldly;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Indicators.HistoryProviders;
using TradyStrat.Shared.Domain;
using Xunit;

namespace TradyStrat.Tests.Indicators.HistoryProviders;

public class BollingerHistoryProviderTests
{
    private static List<PriceBar> Bars(int n) =>
        Enumerable.Range(0, n).Select(i => new PriceBar
        {
            Id = i + 1, Ticker = "T",
            Date = new DateOnly(2026, 1, 1).AddDays(i),
            Open = 100m, High = 101m, Low = 99m,
            Close = 100m + (decimal)Math.Sin(i * 0.4) * 2m,
            Volume = 1000,
        }).ToList();

    [Fact]
    public void Kind_is_Bollinger()
        => new BollingerHistoryProvider().Kind.ShouldBe(IndicatorKind.Bollinger);

    [Fact]
    public void Returns_middle_band_with_hi_lo_thresholds_at_last_bar()
    {
        var s = new BollingerHistoryProvider().Compute(Bars(60), 30);

        s.Values.Count.ShouldBe(30);
        s.ThresholdHi.ShouldNotBeNull();
        s.ThresholdLo.ShouldNotBeNull();
        s.ThresholdHi.Value.ShouldBeGreaterThan(s.ThresholdLo!.Value);
    }

    [Fact]
    public void Returns_empty_when_insufficient_bars()
        => new BollingerHistoryProvider().Compute(Bars(10), 30).Values.ShouldBeEmpty();
}
```

- [ ] **Step 2: Run to verify failure**

```
dotnet test --filter "FullyQualifiedName~BollingerHistoryProviderTests"
```
Expected: build error.

- [ ] **Step 3: Implement provider**

```csharp
// TradyStrat/Features/Indicators/HistoryProviders/BollingerHistoryProvider.cs
using TechnicalAnalysis.Common;
using TechnicalAnalysis.Functions;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Indicators.HistoryProviders;

public sealed class BollingerHistoryProvider : IIndicatorHistoryProvider
{
    private const int Period = 20;
    private const double DevUp = 2.0;
    private const double DevDown = 2.0;

    public IndicatorKind Kind => IndicatorKind.Bollinger;

    public IndicatorSeries Compute(IReadOnlyList<PriceBar> bars, int lastN)
    {
        if (bars.Count < Period) return IndicatorSeries.Empty;

        var closes = bars.Select(b => (double)b.Close).ToArray();
        var upper  = new double[closes.Length];
        var middle = new double[closes.Length];
        var lower  = new double[closes.Length];
        int begIdx = 0, nb = 0;
        var period = Period; var devUp = DevUp; var devDown = DevDown;
        var maType = MAType.Sma;

        var rc = TAFunc.BollingerBands(
            0, closes.Length - 1, in closes,
            in period, in devUp, in devDown, in maType,
            ref begIdx, ref nb,
            ref upper, ref middle, ref lower);

        if (rc != RetCode.Success || nb == 0) return IndicatorSeries.Empty;

        var take = Math.Min(lastN, nb);
        var slice = new decimal[take];
        for (int i = 0; i < take; i++) slice[i] = (decimal)middle[nb - take + i];

        return new IndicatorSeries(
            Values: slice,
            ThresholdHi: (decimal)upper[nb - 1],
            ThresholdLo: (decimal)lower[nb - 1]);
    }
}
```

- [ ] **Step 4: Run tests**

```
dotnet test --filter "FullyQualifiedName~BollingerHistoryProviderTests"
```
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/Indicators/HistoryProviders/BollingerHistoryProvider.cs \
        TradyStrat.Tests/Indicators/HistoryProviders/BollingerHistoryProviderTests.cs
git commit -m "feat(indicators): add BollingerHistoryProvider"
```

---

## Task 7 — `IchimokuHistoryProvider`

**Files:**
- Create: `TradyStrat/Features/Indicators/Ichimoku/IchimokuHistoryProvider.cs`
- Test: `TradyStrat.Tests/Indicators/Ichimoku/IchimokuHistoryProviderTests.cs`

Threshold semantics: thresholds null; sparkline shows the close-price line (the underlying price the cloud is overlaid against). The existing `Ichimoku.cs` computes Ichimoku locally — read it before implementing this task to match the conventions.

- [ ] **Step 1: Read the existing Ichimoku implementation**

```
cat TradyStrat/Features/Indicators/Ichimoku.cs
```

Note its computation style and outputs (Tenkan, Kijun, Senkou A/B, Chikou). Use the same internal helpers if they exist; otherwise compute the close-price slice as a fallback (the spec says thresholds null and price line as the value).

- [ ] **Step 2: Write failing test**

```csharp
// TradyStrat.Tests/Indicators/Ichimoku/IchimokuHistoryProviderTests.cs
using Shouldly;
using TradyStrat.Features.Indicators;
using TradyStrat.Shared.Domain;
using Xunit;

namespace TradyStrat.Tests.Indicators;

public class IchimokuHistoryProviderTests
{
    private static List<PriceBar> Bars(int n) =>
        Enumerable.Range(0, n).Select(i => new PriceBar
        {
            Id = i + 1, Ticker = "T",
            Date = new DateOnly(2026, 1, 1).AddDays(i),
            Open = 100m, High = 101m, Low = 99m,
            Close = 100m + (decimal)i * 0.1m,
            Volume = 1000,
        }).ToList();

    [Fact]
    public void Kind_is_Ichimoku()
        => new IchimokuHistoryProvider().Kind.ShouldBe(IndicatorKind.Ichimoku);

    [Fact]
    public void Returns_lastN_close_prices_with_no_thresholds()
    {
        var s = new IchimokuHistoryProvider().Compute(Bars(80), 30);

        s.Values.Count.ShouldBe(30);
        s.ThresholdHi.ShouldBeNull();
        s.ThresholdLo.ShouldBeNull();
        s.Values[^1].ShouldBe(100m + 79 * 0.1m);  // last close price
    }

    [Fact]
    public void Returns_empty_when_no_bars()
        => new IchimokuHistoryProvider().Compute([], 30).Values.ShouldBeEmpty();
}
```

- [ ] **Step 3: Run to verify failure**

```
dotnet test --filter "FullyQualifiedName~IchimokuHistoryProviderTests"
```
Expected: build error.

- [ ] **Step 4: Implement provider**

```csharp
// TradyStrat/Features/Indicators/Ichimoku/IchimokuHistoryProvider.cs
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Indicators;

public sealed class IchimokuHistoryProvider : IIndicatorHistoryProvider
{
    public IndicatorKind Kind => IndicatorKind.Ichimoku;

    public IndicatorSeries Compute(IReadOnlyList<PriceBar> bars, int lastN)
    {
        if (bars.Count == 0) return IndicatorSeries.Empty;

        // Spec: sparkline shows the close-price line (the underlying the cloud overlays).
        // Thresholds null because Ichimoku has no single hi/lo threshold pair to draw.
        var take = Math.Min(lastN, bars.Count);
        var slice = new decimal[take];
        for (int i = 0; i < take; i++) slice[i] = bars[bars.Count - take + i].Close;
        return new IndicatorSeries(slice, ThresholdHi: null, ThresholdLo: null);
    }
}
```

- [ ] **Step 5: Run tests**

```
dotnet test --filter "FullyQualifiedName~IchimokuHistoryProviderTests"
```
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add TradyStrat/Features/Indicators/Ichimoku/IchimokuHistoryProvider.cs \
        TradyStrat.Tests/Indicators/Ichimoku/IchimokuHistoryProviderTests.cs
git commit -m "feat(indicators): add IchimokuHistoryProvider"
```

---

## Task 8 — `Sma200HistoryProvider`

**Files:**
- Create: `TradyStrat/Features/Indicators/MovingAverage/Sma200HistoryProvider.cs`
- Test: `TradyStrat.Tests/Indicators/MovingAverage/Sma200HistoryProviderTests.cs`

Threshold semantics: Values = SMA(200) series; ThresholdHi = SMA value at last bar drawn as a horizontal target line. ThresholdLo null.

- [ ] **Step 1: Write failing test**

```csharp
// TradyStrat.Tests/Indicators/MovingAverage/Sma200HistoryProviderTests.cs
using Shouldly;
using TradyStrat.Features.Indicators;
using TradyStrat.Shared.Domain;
using Xunit;

namespace TradyStrat.Tests.Indicators;

public class Sma200HistoryProviderTests
{
    private static List<PriceBar> Bars(int n) =>
        Enumerable.Range(0, n).Select(i => new PriceBar
        {
            Id = i + 1, Ticker = "T",
            Date = new DateOnly(2025, 1, 1).AddDays(i),
            Open = 100m, High = 101m, Low = 99m, Close = 100m + i * 0.1m, Volume = 1000,
        }).ToList();

    [Fact]
    public void Kind_is_Sma200()
        => new Sma200HistoryProvider().Kind.ShouldBe(IndicatorKind.Sma200);

    [Fact]
    public void Returns_lastN_with_threshold_at_last_value()
    {
        var s = new Sma200HistoryProvider().Compute(Bars(250), 30);

        s.Values.Count.ShouldBe(30);
        s.ThresholdHi.ShouldBe(s.Values[^1]);
        s.ThresholdLo.ShouldBeNull();
    }

    [Fact]
    public void Returns_empty_when_fewer_than_period_bars()
        => new Sma200HistoryProvider().Compute(Bars(50), 30).Values.ShouldBeEmpty();
}
```

- [ ] **Step 2: Run to verify failure**

```
dotnet test --filter "FullyQualifiedName~Sma200HistoryProviderTests"
```
Expected: build error.

- [ ] **Step 3: Implement provider**

Use the same `TAFunc` MovingAverage / SMA call style as the existing `Features/Indicators/MovingAverage.cs` — read it once, then mirror its parameter shape.

```csharp
// TradyStrat/Features/Indicators/MovingAverage/Sma200HistoryProvider.cs
using TechnicalAnalysis.Common;
using TechnicalAnalysis.Functions;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Indicators;

public sealed class Sma200HistoryProvider : IIndicatorHistoryProvider
{
    private const int Period = 200;

    public IndicatorKind Kind => IndicatorKind.Sma200;

    public IndicatorSeries Compute(IReadOnlyList<PriceBar> bars, int lastN)
    {
        if (bars.Count < Period) return IndicatorSeries.Empty;

        var closes = bars.Select(b => (double)b.Close).ToArray();
        var output = new double[closes.Length];
        int begIdx = 0, nb = 0;
        var period = Period;
        var maType = MAType.Sma;

        var rc = TAFunc.MovingAverage(
            0, closes.Length - 1, in closes,
            in period, in maType,
            ref begIdx, ref nb, ref output);

        if (rc != RetCode.Success || nb == 0) return IndicatorSeries.Empty;

        var take = Math.Min(lastN, nb);
        var slice = new decimal[take];
        for (int i = 0; i < take; i++) slice[i] = (decimal)output[nb - take + i];

        return new IndicatorSeries(slice, ThresholdHi: slice[^1], ThresholdLo: null);
    }
}
```

If `TAFunc.MovingAverage`'s parameter signature in the existing `MovingAverage.cs` differs (e.g. uses `Sma` directly rather than `MovingAverage` + `MAType`), match that exactly — the existing implementation is the source of truth for TaLib API conventions in this repo.

- [ ] **Step 4: Run tests**

```
dotnet test --filter "FullyQualifiedName~Sma200HistoryProviderTests"
```
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/Indicators/MovingAverage/Sma200HistoryProvider.cs \
        TradyStrat.Tests/Indicators/MovingAverage/Sma200HistoryProviderTests.cs
git commit -m "feat(indicators): add Sma200HistoryProvider"
```

---

## Task 9 — `IndicatorHistoryProviderFactory`

**Files:**
- Create: `TradyStrat/Features/Indicators/IIndicatorHistoryProviderFactory.cs`
- Create: `TradyStrat/Features/Indicators/IndicatorHistoryProviderFactory.cs`
- Test: `TradyStrat.Tests/Indicators/IndicatorHistoryProviderFactoryTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
// TradyStrat.Tests/Indicators/IndicatorHistoryProviderFactoryTests.cs
using Shouldly;
using TradyStrat.Features.Indicators;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using Xunit;

namespace TradyStrat.Tests.Indicators;

public class IndicatorHistoryProviderFactoryTests
{
    private static IndicatorHistoryProviderFactory Factory() =>
        new([
            new RsiHistoryProvider(),
            new BollingerHistoryProvider(),
            new IchimokuHistoryProvider(),
            new Sma200HistoryProvider(),
        ]);

    [Theory]
    [InlineData(IndicatorKind.Rsi,       typeof(RsiHistoryProvider))]
    [InlineData(IndicatorKind.Bollinger, typeof(BollingerHistoryProvider))]
    [InlineData(IndicatorKind.Ichimoku,  typeof(IchimokuHistoryProvider))]
    [InlineData(IndicatorKind.Sma200,    typeof(Sma200HistoryProvider))]
    public void Resolves_concrete_strategy(IndicatorKind kind, Type expected)
        => Factory().For(kind).GetType().ShouldBe(expected);

    [Fact]
    public void Throws_for_unregistered_kind()
        => Should.Throw<IndicatorComputationException>(
            () => Factory().For(IndicatorKind.Sma50));
}
```

- [ ] **Step 2: Run to verify failure**

```
dotnet test --filter "FullyQualifiedName~IndicatorHistoryProviderFactoryTests"
```
Expected: build error.

- [ ] **Step 3: Add factory interface**

```csharp
// TradyStrat/Features/Indicators/IIndicatorHistoryProviderFactory.cs
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Indicators;

public interface IIndicatorHistoryProviderFactory
{
    IIndicatorHistoryProvider For(IndicatorKind kind);
}
```

- [ ] **Step 4: Add factory implementation**

```csharp
// TradyStrat/Features/Indicators/IndicatorHistoryProviderFactory.cs
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Features.Indicators;

public sealed class IndicatorHistoryProviderFactory(
    IEnumerable<IIndicatorHistoryProvider> providers) : IIndicatorHistoryProviderFactory
{
    private readonly Dictionary<IndicatorKind, IIndicatorHistoryProvider> _byKind =
        providers.ToDictionary(p => p.Kind);

    public IIndicatorHistoryProvider For(IndicatorKind kind)
        => _byKind.TryGetValue(kind, out var p)
            ? p
            : throw new IndicatorComputationException(
                $"No history provider registered for {kind}.");
}
```

- [ ] **Step 5: Run tests**

```
dotnet test --filter "FullyQualifiedName~IndicatorHistoryProviderFactoryTests"
```
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add TradyStrat/Features/Indicators/IIndicatorHistoryProviderFactory.cs \
        TradyStrat/Features/Indicators/IndicatorHistoryProviderFactory.cs \
        TradyStrat.Tests/Indicators/IndicatorHistoryProviderFactoryTests.cs
git commit -m "feat(indicators): add IndicatorHistoryProviderFactory + interface"
```

---

## Task 10 — `IndicatorEngine.HistoryFor` + as-of `ComputeFor`

**Files:**
- Modify: `TradyStrat/Features/Indicators/IndicatorEngine.cs`
- Test: `TradyStrat.Tests/Indicators/IndicatorEngineHistoryTests.cs`

Goal: extend `IndicatorEngine` with two new methods — `HistoryFor` (sparklines) and an as-of overload of `ComputeFor` (used later by `SnapshotFactory`).

- [ ] **Step 1: Read existing engine**

```
cat TradyStrat/Features/Indicators/IndicatorEngine.cs
```

The engine takes `IReadRepositoryBase<PriceBar>` and a `ZoneClassifier`. Loads bars via `PriceBarsForTickerSpec(ticker)`. Computes the bundle from the full series.

- [ ] **Step 2: Write failing test for `HistoryFor`**

```csharp
// TradyStrat.Tests/Indicators/IndicatorEngineHistoryTests.cs
using Ardalis.Specification.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Features.Indicators;
using TradyStrat.Shared.Domain;
using TradyStrat.Tests.Specifications;
using Xunit;

namespace TradyStrat.Tests.Indicators;

public class IndicatorEngineHistoryTests
{
    [Fact]
    public async Task HistoryFor_returns_series_for_kind()
    {
        using var ctx = InMemoryDb.New();
        for (int i = 0; i < 50; i++)
            ctx.PriceBars.Add(new PriceBar
            {
                Id = i + 1, Ticker = "T",
                Date = new DateOnly(2026, 1, 1).AddDays(i),
                Open = 100m, High = 101m, Low = 99m,
                Close = 100m + i * 0.5m, Volume = 1000,
            });
        await ctx.SaveChangesAsync();

        var factory = new IndicatorHistoryProviderFactory([new RsiHistoryProvider()]);
        var engine = new IndicatorEngine(
            new RepositoryBase<PriceBar>(ctx),
            new ZoneClassifier([]),     // empty rule set fine for this test
            factory);

        var s = await engine.HistoryFor("T", IndicatorKind.Rsi, 20, default);

        s.Values.Count.ShouldBeGreaterThan(0);
        s.ThresholdHi.ShouldBe(70m);
    }
}
```

- [ ] **Step 3: Run to verify failure**

```
dotnet test --filter "FullyQualifiedName~IndicatorEngineHistoryTests"
```
Expected: build error (ctor signature changed).

- [ ] **Step 4: Modify `IndicatorEngine`**

Replace the entire file with:

```csharp
// TradyStrat/Features/Indicators/IndicatorEngine.cs
using Ardalis.Specification;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Specifications.PriceBars;

namespace TradyStrat.Features.Indicators;

public sealed class IndicatorEngine(
    IReadRepositoryBase<PriceBar> bars,
    ZoneClassifier classifier,
    IIndicatorHistoryProviderFactory historyFactory)
{
    public async Task<IndicatorReading> ComputeFor(string ticker, CancellationToken ct)
    {
        var series = await bars.ListAsync(new PriceBarsForTickerSpec(ticker), ct);
        return ComputeFromSeries(ticker, series);
    }

    public async Task<IndicatorReading> ComputeFor(string ticker, DateOnly asOf, CancellationToken ct)
    {
        var series = await bars.ListAsync(new PriceBarsAsOfSpec(ticker, asOf), ct);
        return ComputeFromSeries(ticker, series);
    }

    public async Task<IndicatorSeries> HistoryFor(
        string ticker, IndicatorKind kind, int lastN, CancellationToken ct)
    {
        var series = await bars.ListAsync(new PriceBarsForTickerSpec(ticker), ct);
        return historyFactory.For(kind).Compute(series, lastN);
    }

    public async Task<IndicatorSeries> HistoryFor(
        string ticker, IndicatorKind kind, int lastN, DateOnly asOf, CancellationToken ct)
    {
        var series = await bars.ListAsync(new PriceBarsAsOfSpec(ticker, asOf), ct);
        return historyFactory.For(kind).Compute(series, lastN);
    }

    private IndicatorReading ComputeFromSeries(string ticker, IReadOnlyList<PriceBar> series)
    {
        if (series.Count == 0)
            throw new IndicatorComputationException($"No price bars for {ticker}");

        var price  = series[^1].Close;
        var bundle = new IndicatorBundle(
            Bollinger.LatestFor(series),
            Rsi.LatestFor(series),
            MovingAverage.LatestFor(series, 50),
            MovingAverage.LatestFor(series, 200),
            Ichimoku.LatestFor(series));

        var (zone, reasons) = classifier.Classify(price, bundle);
        return new IndicatorReading(ticker, price,
            bundle.Bollinger, bundle.Rsi, bundle.Sma50, bundle.Sma200, bundle.Ichimoku,
            zone, reasons);
    }
}
```

- [ ] **Step 5: Run tests**

```
dotnet test --filter "FullyQualifiedName~IndicatorEngineHistoryTests"
```

If existing tests fail because they instantiate `IndicatorEngine` with two arguments only, update them to pass a factory (use `new IndicatorHistoryProviderFactory([])` for tests that don't exercise history).

- [ ] **Step 6: Build entire solution to surface other call-site breaks**

```
dotnet build
```

Fix any compile errors in tests / code that constructed `IndicatorEngine` with the old two-arg signature.

- [ ] **Step 7: Run full test suite**

```
dotnet test
```
Expected: all green.

- [ ] **Step 8: Commit**

```bash
git add TradyStrat/Features/Indicators/IndicatorEngine.cs \
        TradyStrat.Tests/Indicators/IndicatorEngineHistoryTests.cs \
        $(git diff --name-only --cached)
git commit -m "feat(indicators): add HistoryFor and as-of ComputeFor on IndicatorEngine"
```

---

## Task 11 — Register history strategies + factory in DI

**Files:**
- Modify: `TradyStrat/Modules/IndicatorsModule.cs` (or wherever `IndicatorEngine` is registered)

- [ ] **Step 1: Locate the module**

```
grep -rE "AddScoped<IndicatorEngine|AddSingleton<IndicatorEngine" TradyStrat/Modules/
```

Open the file shown.

- [ ] **Step 2: Add registrations**

Inside `ConfigureServices(WebApplicationBuilder builder)`, after the existing `IndicatorEngine` registration, add:

```csharp
builder.Services.AddScoped<IIndicatorHistoryProvider, RsiHistoryProvider>();
builder.Services.AddScoped<IIndicatorHistoryProvider, BollingerHistoryProvider>();
builder.Services.AddScoped<IIndicatorHistoryProvider, IchimokuHistoryProvider>();
builder.Services.AddScoped<IIndicatorHistoryProvider, Sma200HistoryProvider>();
builder.Services.AddScoped<IIndicatorHistoryProviderFactory, IndicatorHistoryProviderFactory>();
```

- [ ] **Step 3: Build**

```
dotnet build
```
Expected: success.

- [ ] **Step 4: Run app smoke**

```
dotnet run --project TradyStrat
```

Open http://127.0.0.1:5180 — dashboard should still render. No new behavior yet; this just confirms DI is wired.

- [ ] **Step 5: Stop the app, commit**

```bash
git add TradyStrat/Modules/IndicatorsModule.cs
git commit -m "chore(di): register indicator history providers and factory"
```

---

# Phase 3 — Snapshot factory migration

## Task 12 — `PortfolioService.SnapshotAsync` as-of overload

**Files:**
- Modify: `TradyStrat/Features/Portfolio/PortfolioService.cs`
- Test: `TradyStrat.Tests/Portfolio/PortfolioServiceAsOfTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
// TradyStrat.Tests/Portfolio/PortfolioServiceAsOfTests.cs
using Ardalis.Specification.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Features.Portfolio;
using TradyStrat.Shared.Domain;
using TradyStrat.Tests.Specifications;
using Xunit;

namespace TradyStrat.Tests.Portfolio;

public class PortfolioServiceAsOfTests
{
    [Fact]
    public async Task Excludes_trades_after_asOf()
    {
        using var ctx = InMemoryDb.New();
        ctx.Trades.AddRange(
            new Trade { Id = 1, ExecutedOn = new DateOnly(2026, 4, 1), Side = TradeSide.Buy,
                Quantity = 100m, PricePerShare = 1m, FeesEur = 0m, Note = "", CreatedAt = DateTime.UtcNow },
            new Trade { Id = 2, ExecutedOn = new DateOnly(2026, 5, 5), Side = TradeSide.Buy,
                Quantity = 200m, PricePerShare = 1m, FeesEur = 0m, Note = "", CreatedAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        var svc = new PortfolioService(new RepositoryBase<Trade>(ctx));

        var snap = await svc.SnapshotAsync(
            asOf: new DateOnly(2026, 4, 30),
            currentPriceEur: 2m, goalEur: 1000m, ct: default);

        snap.Shares.ShouldBe(100m);   // only the April trade counted
    }
}
```

- [ ] **Step 2: Run to verify failure**

```
dotnet test --filter "FullyQualifiedName~PortfolioServiceAsOfTests"
```
Expected: build error (no overload).

- [ ] **Step 3: Add overload**

Open `TradyStrat/Features/Portfolio/PortfolioService.cs`. Add a new method *next to* the existing `SnapshotAsync`. Refactor: extract the inner FIFO loop into a private helper that takes `IReadOnlyList<Trade>`, then have both public methods call it.

```csharp
// In TradyStrat/Features/Portfolio/PortfolioService.cs — add usings:
using TradyStrat.Specifications.Trades;

// Add to class:
public async Task<PortfolioSnapshot> SnapshotAsync(
    DateOnly asOf, decimal currentPriceEur, decimal goalEur, CancellationToken ct)
{
    var trades = await this.trades.ListAsync(new TradesAsOfSpec(asOf), ct);
    return BuildSnapshot(trades, currentPriceEur, goalEur);
}
```

Refactor the existing `SnapshotAsync(decimal, decimal, CancellationToken)` to also delegate to a private `BuildSnapshot(IReadOnlyList<Trade>, decimal, decimal)` helper containing the existing FIFO + cost-basis logic.

```csharp
public async Task<PortfolioSnapshot> SnapshotAsync(
    decimal currentPriceEur, decimal goalEur, CancellationToken ct)
{
    var all = await trades.ListAsync(new AllTradesSpec(), ct);
    return BuildSnapshot(all, currentPriceEur, goalEur);
}

private static PortfolioSnapshot BuildSnapshot(
    IReadOnlyList<Trade> trades, decimal currentPriceEur, decimal goalEur)
{
    // <existing inner body of the original SnapshotAsync goes here verbatim,
    //  unchanged — it already iterates `all` and computes the snapshot>
}
```

- [ ] **Step 4: Run tests**

```
dotnet test --filter "FullyQualifiedName~PortfolioServiceAsOfTests"
```
Expected: PASS. Re-run full suite to confirm no regression in existing portfolio tests.

```
dotnet test
```

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/Portfolio/PortfolioService.cs \
        TradyStrat.Tests/Portfolio/PortfolioServiceAsOfTests.cs
git commit -m "feat(portfolio): add as-of SnapshotAsync overload"
```

---

## Task 13 — Replace `ISnapshotBuilder` with `ISnapshotFactory`

**Files:**
- Delete: `TradyStrat/Features/AiSuggestion/ISnapshotBuilder.cs`
- Delete: `TradyStrat/Features/AiSuggestion/SnapshotBuilder.cs`
- Create: `TradyStrat/Features/AiSuggestion/ISnapshotFactory.cs`
- Create: `TradyStrat/Features/AiSuggestion/SnapshotFactory.cs`
- Modify: `TradyStrat/Application/UseCases/AiSuggestion/GetTodaysSuggestionUseCase.cs`
- Modify: `TradyStrat/Application/UseCases/AiSuggestion/ForceRefetchSuggestionUseCase.cs`
- Modify: `TradyStrat/Modules/AiSuggestionModule.cs`
- Move: `TradyStrat.Tests/AiSuggestion/SnapshotBuilderTests.cs` → `SnapshotFactoryTests.cs`

This task touches multiple files. The TDD cycle here is "make tests pass after the refactor" — we update existing tests rather than write new ones first.

- [ ] **Step 1: Add `ISnapshotFactory` interface**

```csharp
// TradyStrat/Features/AiSuggestion/ISnapshotFactory.cs
namespace TradyStrat.Features.AiSuggestion;

public interface ISnapshotFactory
{
    Task<AiSnapshot> CreateAsync(DateOnly asOf, CancellationToken ct);
}
```

- [ ] **Step 2: Create `SnapshotFactory` implementation**

Read the existing `TradyStrat/Features/AiSuggestion/SnapshotBuilder.cs` to copy its body. The new class differs in three ways:

1. Implements `ISnapshotFactory.CreateAsync(asOf, ct)` (not `BuildAsync(ct)`).
2. Uses `today = asOf` (parameter), not `clock.TodayInExchangeTzFor(...)`.
3. Reads through as-of specs: `IndicatorEngine.ComputeFor(ticker, asOf, ct)` and `PortfolioService.SnapshotAsync(asOf, ...)`. FX continues to use the `today` parameter (`fx.UsdToEurAsync(amount, today, ct)`) — the FxConverter uses `<= today` already, so passing the as-of date works.
4. Recent trades use `LatestTradesSpec(20)` filtered to `<= asOf` — for now, post-filter the results in memory (`.Where(t => t.ExecutedOn <= asOf).Take(20)`). If `LatestTradesSpec` already orders descending by date, simpler is to use `TradesAsOfSpec(asOf)` and `.TakeLast(20).Reverse()`.

```csharp
// TradyStrat/Features/AiSuggestion/SnapshotFactory.cs
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ardalis.Specification;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Portfolio;
using TradyStrat.Shared.Domain;
using TradyStrat.Specifications.Trades;

namespace TradyStrat.Features.AiSuggestion;

public sealed class SnapshotFactory(
    IndicatorEngine indicators,
    PortfolioService portfolio,
    FxConverter fx,
    IReadRepositoryBase<GoalConfig> goalRepo,
    IReadRepositoryBase<Trade> tradeRepo) : ISnapshotFactory
{
    private const string FocusTicker = "CON3.L";

    private static readonly (string Ticker, string Currency)[] Catalog =
    [
        (FocusTicker, "USD"),
        ("COIN",      "USD"),
        ("BTC-USD",   "USD"),
    ];

    public async Task<AiSnapshot> CreateAsync(DateOnly asOf, CancellationToken ct)
    {
        var goal = await goalRepo.GetByIdAsync(1, ct) ?? GoalConfig.Default(DateTime.UtcNow);

        var tickers = new List<TickerContext>();
        decimal? focusPriceEur = null;

        foreach (var (ticker, currency) in Catalog)
        {
            var reading = await indicators.ComputeFor(ticker, asOf, ct);
            decimal? eur = null;
            if (currency == "USD")
                eur = await fx.UsdToEurAsync(reading.Price, asOf, ct);
            if (ticker == FocusTicker) focusPriceEur = eur ?? reading.Price;

            tickers.Add(new TickerContext(
                ticker, currency, reading.Price, eur, reading.Zone, reading.Reasons));
        }

        var snap = await portfolio.SnapshotAsync(asOf, focusPriceEur ?? 0m, goal.TargetEur, ct);

        var asOfTrades = await tradeRepo.ListAsync(new TradesAsOfSpec(asOf), ct);
        var recentDtos = asOfTrades
            .OrderByDescending(t => t.ExecutedOn).Take(20)
            .OrderBy(t => t.ExecutedOn)
            .Select(t => new TradeRecent(t.ExecutedOn, t.Side, t.Quantity, t.PricePerShare))
            .ToList();

        decimal? usdPerEur = null;
        try
        {
            var oneEurInEur = await fx.UsdToEurAsync(1m, asOf, ct);
            if (oneEurInEur != 0m) usdPerEur = 1m / oneEurInEur;
        }
        catch (Shared.Exceptions.FxRateUnavailableException) { }

        var promptHash = HashPrompt(asOf, snap, tickers, recentDtos);
        return new AiSnapshot(asOf, goal, snap, tickers, recentDtos, usdPerEur, promptHash);
    }

    private static string HashPrompt(DateOnly today, PortfolioSnapshot snap,
        IEnumerable<TickerContext> tickers, IEnumerable<TradeRecent> recent)
    {
        var payload = new { today, snap, tickers, recent };
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOpts.Strict));
        return Convert.ToHexString(SHA256.HashData(bytes))[..16];
    }
}
```

Note: `GoalConfig.Default(DateTime.UtcNow)` matches the existing fallback signature — verify by reading the existing `SnapshotBuilder.cs` line that calls `GoalConfig.Default(...)`.

- [ ] **Step 3: Update `GetTodaysSuggestionUseCase`**

In `TradyStrat/Application/UseCases/AiSuggestion/GetTodaysSuggestionUseCase.cs`:

- Change ctor parameter `ISnapshotBuilder snapshotBuilder` → `ISnapshotFactory snapshotFactory`.
- Change body line `var snap = await snapshotBuilder.BuildAsync(ct);` →
  `var snap = await snapshotFactory.CreateAsync(today, ct);`

`today` is already in scope (line 20).

- [ ] **Step 4: Update `ForceRefetchSuggestionUseCase`**

Open `TradyStrat/Application/UseCases/AiSuggestion/ForceRefetchSuggestionUseCase.cs`. Apply the same change: rename ctor param, replace `BuildAsync(ct)` with `CreateAsync(<today>, ct)`. Compute `today` the same way as `GetTodaysSuggestionUseCase` does — copy that line.

- [ ] **Step 5: Update `AiSuggestionModule`**

In `TradyStrat/Modules/AiSuggestionModule.cs`, replace:

```csharp
builder.Services.AddScoped<ISnapshotBuilder, SnapshotBuilder>();
```

with:

```csharp
builder.Services.AddScoped<ISnapshotFactory, SnapshotFactory>();
```

- [ ] **Step 6: Delete the old files**

```bash
rm TradyStrat/Features/AiSuggestion/ISnapshotBuilder.cs \
   TradyStrat/Features/AiSuggestion/SnapshotBuilder.cs
```

- [ ] **Step 7: Migrate test file**

```bash
git mv TradyStrat.Tests/AiSuggestion/SnapshotBuilderTests.cs \
       TradyStrat.Tests/AiSuggestion/SnapshotFactoryTests.cs
```

Open `SnapshotFactoryTests.cs` and update:
- Class name `SnapshotBuilderTests` → `SnapshotFactoryTests`.
- Replace `new SnapshotBuilder(...)` with `new SnapshotFactory(...)` (drop the `IClock` ctor arg — `SnapshotFactory` no longer needs it).
- Replace `.BuildAsync(ct)` with `.CreateAsync(<existing-today-variable>, ct)`. Where the existing test was implicit about today, pass `new DateOnly(2026, 5, 7)` (or whatever fixture date the test uses).

- [ ] **Step 8: Add a new test for as-of behavior**

Add to the same file:

```csharp
[Fact]
public async Task CreateAsync_uses_asOf_for_snapshot_Today()
{
    // <reuse existing fixture setup helpers — seed bars/fx/goal as the existing test does>
    var asOf = new DateOnly(2026, 4, 30);

    var snapshot = await sut.CreateAsync(asOf, default);

    snapshot.Today.ShouldBe(asOf);
}
```

Add another for missing data → typed exception:

```csharp
[Fact]
public async Task CreateAsync_throws_PriceFeedUnavailableException_when_no_bars_for_asOf()
{
    using var ctx = InMemoryDb.New();
    // Seed FX + goal + trades but NO PriceBars for asOf
    // <fixture setup>

    var sut = new SnapshotFactory(/* deps from ctx */);
    await Should.ThrowAsync<PriceFeedUnavailableException>(
        () => sut.CreateAsync(new DateOnly(2026, 4, 30), default));
}
```

- [ ] **Step 9: Build + run tests**

```
dotnet build
dotnet test
```
Expected: all green.

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "refactor(ai): rename SnapshotBuilder → SnapshotFactory with as-of param"
```

---

# Phase 4 — Backfill machinery

## Task 14 — `BackfillStatus` discriminated record

**Files:**
- Create: `TradyStrat/Features/AiSuggestion/BackfillStatus.cs`

No test in this task — just type definition.

- [ ] **Step 1: Add the type**

```csharp
// TradyStrat/Features/AiSuggestion/BackfillStatus.cs
namespace TradyStrat.Features.AiSuggestion;

public abstract record BackfillStatus
{
    public sealed record Idle : BackfillStatus
    {
        public static readonly Idle Instance = new();
        private Idle() { }
    }

    public sealed record Running(int Remaining, int Total, DateOnly CurrentDate) : BackfillStatus;

    public sealed record Failed(DateOnly LastSuccessful, DateOnly FailedAt, string Reason)
        : BackfillStatus;
}
```

- [ ] **Step 2: Build**

```
dotnet build
```
Expected: success.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat/Features/AiSuggestion/BackfillStatus.cs
git commit -m "feat(ai): add BackfillStatus discriminated record"
```

---

## Task 15 — `BackfillSuggestionsUseCase`

**Files:**
- Create: `TradyStrat/Application/UseCases/AiSuggestion/BackfillSuggestionsUseCase.cs`
- Test: `TradyStrat.Tests/UseCases/AiSuggestion/BackfillSuggestionsUseCaseTests.cs`

This use case backfills *one* missing date. The coordinator (next task) calls it in a loop.

- [ ] **Step 1: Write failing test**

```csharp
// TradyStrat.Tests/UseCases/AiSuggestion/BackfillSuggestionsUseCaseTests.cs
using Ardalis.Specification.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Application.UseCases.AiSuggestion;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Tests.AiSuggestion;        // FakeChatClient lives here
using TradyStrat.Tests.Specifications;
using Xunit;

namespace TradyStrat.Tests.UseCases.AiSuggestion;

public class BackfillSuggestionsUseCaseTests
{
    private sealed class StubFactory(DateOnly expectedAsOf) : ISnapshotFactory
    {
        public DateOnly? Captured { get; private set; }
        public Task<AiSnapshot> CreateAsync(DateOnly asOf, CancellationToken ct)
        {
            Captured = asOf;
            return Task.FromResult(new AiSnapshot(
                Today: asOf,
                Goal: GoalConfig.Default(DateTime.UtcNow),
                Portfolio: new PortfolioSnapshot(0, 0, 0, 0, 0, 0),
                Tickers: [],
                RecentTrades: [],
                UsdPerEur: 1m,
                PromptHash: "test"));
        }
    }

    private sealed class StubAiClient : IAiClient
    {
        public Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct) =>
            Task.FromResult(new Suggestion
            {
                Id = 0,
                ForDate = snapshot.Today,
                Action = SuggestionAction.Hold,
                Conviction = 5,
                Rationale = "stub",
                CitationsJson = "[]",
                PromptHash = snapshot.PromptHash,
                CreatedAt = DateTime.UtcNow,
            });
    }

    [Fact]
    public async Task Persists_suggestion_with_ForDate_from_asOf()
    {
        using var ctx = InMemoryDb.New();
        var factory = new StubFactory(new DateOnly(2026, 5, 4));
        var ai = new StubAiClient();
        var sut = new BackfillSuggestionsUseCase(
            new RepositoryBase<Suggestion>(ctx), factory, ai,
            NullLogger<BackfillSuggestionsUseCase>.Instance);

        var asOf = new DateOnly(2026, 5, 4);
        var s = await sut.ExecuteAsync(asOf, default);

        s.ForDate.ShouldBe(asOf);
        factory.Captured.ShouldBe(asOf);
        ctx.Suggestions.Single().ForDate.ShouldBe(asOf);
    }
}
```

`NullLogger<>` lives in `Microsoft.Extensions.Logging.Abstractions` — add the using.

- [ ] **Step 2: Run to verify failure**

```
dotnet test --filter "FullyQualifiedName~BackfillSuggestionsUseCaseTests"
```
Expected: build error.

- [ ] **Step 3: Implement use case**

```csharp
// TradyStrat/Application/UseCases/AiSuggestion/BackfillSuggestionsUseCase.cs
using Ardalis.Specification;
using TradyStrat.Application.Abstractions;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Application.UseCases.AiSuggestion;

public sealed class BackfillSuggestionsUseCase(
    IRepositoryBase<Suggestion> repo,
    ISnapshotFactory snapshotFactory,
    IAiClient ai,
    ILogger<BackfillSuggestionsUseCase> log)
    : UseCaseBase<DateOnly, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(DateOnly asOf, CancellationToken ct)
    {
        var snapshot   = await snapshotFactory.CreateAsync(asOf, ct);   // snapshot.Today == asOf
        var suggestion = await ai.AskAsync(snapshot, ct);                // sets ForDate from snapshot.Today
        await repo.AddAsync(suggestion, ct);
        return suggestion;
    }
}
```

- [ ] **Step 4: Run tests**

```
dotnet test --filter "FullyQualifiedName~BackfillSuggestionsUseCaseTests"
```
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Application/UseCases/AiSuggestion/BackfillSuggestionsUseCase.cs \
        TradyStrat.Tests/UseCases/AiSuggestion/BackfillSuggestionsUseCaseTests.cs
git commit -m "feat(ai): add BackfillSuggestionsUseCase for one missing date"
```

---

## Task 16 — `SuggestionBackfillCoordinator` (Singleton + Observer)

**Files:**
- Create: `TradyStrat/Features/AiSuggestion/ISuggestionBackfillCoordinator.cs`
- Create: `TradyStrat/Features/AiSuggestion/SuggestionBackfillCoordinator.cs`
- Test: `TradyStrat.Tests/AiSuggestion/SuggestionBackfillCoordinatorTests.cs`

The coordinator is the load-bearing piece. Tests cover empty range, single date, multi-day chronological, reentrancy, mid-chain failure, multi-subscriber, cancellation, typed-exception halt.

- [ ] **Step 1: Write failing tests**

```csharp
// TradyStrat.Tests/AiSuggestion/SuggestionBackfillCoordinatorTests.cs
using Ardalis.Specification.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Application.UseCases.AiSuggestion;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Tests.Specifications;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;

namespace TradyStrat.Tests.AiSuggestion;

public class SuggestionBackfillCoordinatorTests
{
    private static (SuggestionBackfillCoordinator coord, AppDbContext ctx, RecordingAi ai)
        BuildSut(Func<DateOnly, Task<Suggestion>>? aiOverride = null)
    {
        var ctx = InMemoryDb.New();
        var ai = new RecordingAi(aiOverride);
        var factory = new PassthroughFactory();
        var useCase = new BackfillSuggestionsUseCase(
            new RepositoryBase<Suggestion>(ctx), factory, ai,
            NullLogger<BackfillSuggestionsUseCase>.Instance);
        var coord = new SuggestionBackfillCoordinator(
            new RepositoryBase<Suggestion>(ctx), useCase,
            NullLogger<SuggestionBackfillCoordinator>.Instance);
        return (coord, ctx, ai);
    }

    [Fact]
    public async Task Empty_range_stays_idle_and_emits_no_events()
    {
        var (coord, _, _) = BuildSut();
        var events = new List<BackfillStatus>();
        coord.StatusChanged += s => events.Add(s);

        await coord.EnsureBackfilledAsync(
            new DateOnly(2026, 5, 7), new DateOnly(2026, 5, 7), default);

        coord.Status.ShouldBeOfType<BackfillStatus.Idle>();
        events.ShouldBeEmpty();
    }

    [Fact]
    public async Task Single_missing_date_emits_running_then_idle()
    {
        var (coord, ctx, _) = BuildSut();
        var events = new List<BackfillStatus>();
        coord.StatusChanged += s => events.Add(s);

        await coord.EnsureBackfilledAsync(
            fromExclusive: new DateOnly(2026, 5, 5),
            toInclusive:   new DateOnly(2026, 5, 6), default);

        events.OfType<BackfillStatus.Running>().Count().ShouldBe(1);
        events[^1].ShouldBeOfType<BackfillStatus.Idle>();
        ctx.Suggestions.Count().ShouldBe(1);
        ctx.Suggestions.Single().ForDate.ShouldBe(new DateOnly(2026, 5, 6));
    }

    [Fact]
    public async Task Multi_day_runs_chronologically_ascending()
    {
        var (coord, ctx, ai) = BuildSut();

        await coord.EnsureBackfilledAsync(
            fromExclusive: new DateOnly(2026, 5, 1),
            toInclusive:   new DateOnly(2026, 5, 4), default);

        ai.Calls.Select(c => c).ShouldBe([
            new DateOnly(2026, 5, 2),
            new DateOnly(2026, 5, 3),
            new DateOnly(2026, 5, 4),
        ]);
    }

    [Fact]
    public async Task Mid_chain_failure_halts_with_typed_status()
    {
        var (coord, ctx, _) = BuildSut(d =>
            d.Day == 3
                ? throw new AnthropicCallFailedException("boom")
                : Task.FromResult(StubSuggestion(d)));

        await coord.EnsureBackfilledAsync(
            fromExclusive: new DateOnly(2026, 5, 1),
            toInclusive:   new DateOnly(2026, 5, 5), default);

        coord.Status.ShouldBeOfType<BackfillStatus.Failed>();
        var failed = (BackfillStatus.Failed)coord.Status;
        failed.LastSuccessful.ShouldBe(new DateOnly(2026, 5, 2));
        failed.FailedAt.ShouldBe(new DateOnly(2026, 5, 3));
        ctx.Suggestions.Count().ShouldBe(2);   // 5/2 and ... wait - dates 5/1 not in scope
                                                // (fromExclusive=5/1 → start at 5/2)
                                                // missing = 5/2, 5/3, 5/4, 5/5
                                                // 5/3 fails → 5/2 persisted only
        ctx.Suggestions.Single().ForDate.ShouldBe(new DateOnly(2026, 5, 2));
    }

    [Fact]
    public async Task Reentrancy_returns_same_inflight_task()
    {
        var (coord, _, _) = BuildSut();

        var t1 = coord.EnsureBackfilledAsync(
            new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 3), default);
        var t2 = coord.EnsureBackfilledAsync(
            new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 3), default);

        // Even if not literally the same Task instance, both must complete the same job.
        await Task.WhenAll(t1, t2);
        coord.Status.ShouldBeOfType<BackfillStatus.Idle>();
    }

    [Fact]
    public async Task Cancellation_throws_does_not_set_failed()
    {
        var cts = new CancellationTokenSource();
        var (coord, _, _) = BuildSut(async d =>
        {
            // Observe the token so cancellation actually propagates.
            await Task.Delay(1000, cts.Token);
            return StubSuggestion(d);
        });

        var task = coord.EnsureBackfilledAsync(
            new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 5), cts.Token);
        cts.CancelAfter(20);
        await Should.ThrowAsync<OperationCanceledException>(task);
        coord.Status.ShouldNotBeOfType<BackfillStatus.Failed>();
    }

    [Fact]
    public async Task Multi_subscriber_fan_out()
    {
        var (coord, _, _) = BuildSut();
        var a = new List<BackfillStatus>(); var b = new List<BackfillStatus>();
        coord.StatusChanged += s => a.Add(s);
        coord.StatusChanged += s => b.Add(s);

        await coord.EnsureBackfilledAsync(
            new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 2), default);

        a.Count.ShouldBe(b.Count);
        a.Count.ShouldBeGreaterThan(0);
    }

    private static Suggestion StubSuggestion(DateOnly d) => new()
    {
        Id = 0, ForDate = d, Action = SuggestionAction.Hold, Conviction = 5,
        Rationale = "stub", CitationsJson = "[]", PromptHash = "test",
        CreatedAt = DateTime.UtcNow,
    };

    // Stub factory + AI helpers
    private sealed class PassthroughFactory : ISnapshotFactory
    {
        public Task<AiSnapshot> CreateAsync(DateOnly asOf, CancellationToken ct) =>
            Task.FromResult(new AiSnapshot(
                asOf, GoalConfig.Default(DateTime.UtcNow),
                new PortfolioSnapshot(0, 0, 0, 0, 0, 0), [], [], 1m, "test"));
    }

    private sealed class RecordingAi(Func<DateOnly, Task<Suggestion>>? handler) : IAiClient
    {
        public List<DateOnly> Calls { get; } = new();
        public async Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct)
        {
            Calls.Add(snapshot.Today);
            if (handler is null) return StubSuggestion(snapshot.Today);
            return await handler(snapshot.Today);
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

```
dotnet test --filter "FullyQualifiedName~SuggestionBackfillCoordinatorTests"
```
Expected: build error.

- [ ] **Step 3: Add interface**

```csharp
// TradyStrat/Features/AiSuggestion/ISuggestionBackfillCoordinator.cs
namespace TradyStrat.Features.AiSuggestion;

public interface ISuggestionBackfillCoordinator
{
    BackfillStatus Status { get; }
    event Action<BackfillStatus>? StatusChanged;
    Task EnsureBackfilledAsync(DateOnly fromExclusive, DateOnly toInclusive, CancellationToken ct);
}
```

- [ ] **Step 4: Implement coordinator**

```csharp
// TradyStrat/Features/AiSuggestion/SuggestionBackfillCoordinator.cs
using Ardalis.Specification;
using TradyStrat.Application.UseCases.AiSuggestion;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Specifications.Suggestions;

namespace TradyStrat.Features.AiSuggestion;

public sealed partial class SuggestionBackfillCoordinator(
    IReadRepositoryBase<Suggestion> suggestions,
    BackfillSuggestionsUseCase backfillOne,
    ILogger<SuggestionBackfillCoordinator> log) : ISuggestionBackfillCoordinator
{
    private readonly object _gate = new();
    private Task? _inflight;
    private BackfillStatus _status = BackfillStatus.Idle.Instance;

    public BackfillStatus Status => _status;
    public event Action<BackfillStatus>? StatusChanged;

    public Task EnsureBackfilledAsync(DateOnly fromExclusive, DateOnly toInclusive, CancellationToken ct)
    {
        lock (_gate)
        {
            if (_inflight is { IsCompleted: false }) return _inflight;
            _inflight = RunChainAsync(fromExclusive, toInclusive, ct);
            return _inflight;
        }
    }

    private async Task RunChainAsync(DateOnly fromExclusive, DateOnly toInclusive, CancellationToken ct)
    {
        var existing = await suggestions.ListAsync(
            new SuggestionsInRangeSpec(fromExclusive, toInclusive), ct);
        var existingDates = existing.Select(s => s.ForDate).ToHashSet();

        var missing = new List<DateOnly>();
        for (var d = fromExclusive.AddDays(1); d <= toInclusive; d = d.AddDays(1))
            if (!existingDates.Contains(d)) missing.Add(d);

        if (missing.Count == 0)
        {
            SetStatus(BackfillStatus.Idle.Instance);
            return;
        }

        DateOnly? lastOk = null;
        for (int i = 0; i < missing.Count; i++)
        {
            var date = missing[i];
            SetStatus(new BackfillStatus.Running(missing.Count - i, missing.Count, date));

            try
            {
                await backfillOne.ExecuteAsync(date, ct);
                lastOk = date;
            }
            catch (OperationCanceledException) { throw; }
            catch (TradyStratException ex)
            {
                LogChainHalted(log, date, lastOk, ex);
                SetStatus(new BackfillStatus.Failed(
                    LastSuccessful: lastOk ?? fromExclusive,
                    FailedAt: date,
                    Reason: ex.Message));
                return;
            }
        }

        SetStatus(BackfillStatus.Idle.Instance);
    }

    private void SetStatus(BackfillStatus next)
    {
        _status = next;
        StatusChanged?.Invoke(next);
    }

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Backfill chain halted at {BackfillDate} (last successful: {LastSuccessfulDate})")]
    private static partial void LogChainHalted(
        ILogger logger, DateOnly backfillDate, DateOnly? lastSuccessfulDate, Exception ex);
}
```

- [ ] **Step 5: Run tests**

```
dotnet test --filter "FullyQualifiedName~SuggestionBackfillCoordinatorTests"
```
Expected: PASS (7 tests).

If the cancellation test is flaky, adjust the delay in `RecordingAi` and `cts.CancelAfter` to give enough time without being slow.

- [ ] **Step 6: Commit**

```bash
git add TradyStrat/Features/AiSuggestion/ISuggestionBackfillCoordinator.cs \
        TradyStrat/Features/AiSuggestion/SuggestionBackfillCoordinator.cs \
        TradyStrat.Tests/AiSuggestion/SuggestionBackfillCoordinatorTests.cs
git commit -m "feat(ai): add SuggestionBackfillCoordinator (singleton + observer)"
```

---

## Task 17 — Register coordinator in DI

**Files:**
- Modify: `TradyStrat/Modules/AiSuggestionModule.cs`

- [ ] **Step 1: Add registrations**

In `AiSuggestionModule.ConfigureServices`, after the existing `AddScoped<IAiClient, ...>` registration:

```csharp
builder.Services.AddScoped<BackfillSuggestionsUseCase>();
builder.Services.AddSingleton<ISuggestionBackfillCoordinator, SuggestionBackfillCoordinator>();
```

Note: the coordinator is **Singleton** (per spec); it depends on `IReadRepositoryBase<Suggestion>` and `BackfillSuggestionsUseCase` which are scoped. To resolve scoped services from a singleton, the coordinator's `RunChainAsync` should create a scope per chain — but for spec 1's threading model (one coordinator, one chain at a time, called from request scope), the simpler shape is to have the coordinator take an `IServiceScopeFactory` and create a scope for each `RunChainAsync` invocation.

Update `SuggestionBackfillCoordinator` ctor to:

```csharp
public sealed partial class SuggestionBackfillCoordinator(
    IServiceScopeFactory scopeFactory,
    ILogger<SuggestionBackfillCoordinator> log) : ISuggestionBackfillCoordinator
```

Inside `RunChainAsync`, create a scope:

```csharp
using var scope = scopeFactory.CreateScope();
var suggestions = scope.ServiceProvider
    .GetRequiredService<IReadRepositoryBase<Suggestion>>();
var backfillOne = scope.ServiceProvider
    .GetRequiredService<BackfillSuggestionsUseCase>();
// ...rest of method uses these instead of injected fields
```

Update the test ctor in `SuggestionBackfillCoordinatorTests.BuildSut` to pass an `IServiceScopeFactory` — easiest is to register the necessary services in a `ServiceCollection` and call `BuildServiceProvider().GetRequiredService<IServiceScopeFactory>()`. Or, keep the test using the direct-deps overload and have **two** ctors on the coordinator (one for tests with direct deps, one for prod with the scope factory). The simpler route: write a tiny test-only `IServiceScopeFactory` stub that returns scoped instances of stubs.

Pragmatic alternative: make the coordinator's primary ctor take direct deps, and add a **second ctor** that takes `IServiceScopeFactory` and builds the direct-deps internally per chain. Use that second ctor for DI registration. This keeps tests simple.

- [ ] **Step 2: Build**

```
dotnet build
```
Expected: success after the ctor refactor.

- [ ] **Step 3: Run all tests**

```
dotnet test
```
Expected: green.

- [ ] **Step 4: Run app smoke**

```
dotnet run --project TradyStrat
```

Open http://127.0.0.1:5180. Dashboard should still render normally — coordinator exists but is not yet invoked.

- [ ] **Step 5: Stop app, commit**

```bash
git add TradyStrat/Modules/AiSuggestionModule.cs \
        TradyStrat/Features/AiSuggestion/SuggestionBackfillCoordinator.cs \
        TradyStrat.Tests/AiSuggestion/SuggestionBackfillCoordinatorTests.cs
git commit -m "chore(di): register SuggestionBackfillCoordinator + scope-factory wiring"
```

---

# Phase 5 — Pure logic for UI

## Task 18 — `GoalPaceCalculator`

**Files:**
- Create: `TradyStrat/Features/Dashboard/GoalPaceCalculator.cs`
- Test: `TradyStrat.Tests/Dashboard/GoalPaceCalculatorTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// TradyStrat.Tests/Dashboard/GoalPaceCalculatorTests.cs
using Shouldly;
using TradyStrat.Features.Dashboard;
using TradyStrat.Shared.Domain;
using Xunit;

namespace TradyStrat.Tests.Dashboard;

public class GoalPaceCalculatorTests
{
    private static GoalConfig Goal(decimal target = 500_000m, DateOnly? targetDate = null) => new()
    {
        Id = 1,
        TargetEur = target,
        TargetDate = targetDate ?? new DateOnly(2027, 6, 30),
        FocusTicker = "CON3.L",
        UpdatedAt = DateTime.UtcNow,
    };

    [Fact]
    public void NotStarted_when_firstTradeDate_null()
    {
        var vm = GoalPaceCalculator.Compute(
            currentValueEur: 30_000m, goal: Goal(),
            today: new DateOnly(2026, 5, 7), firstTradeDate: null);

        vm.Mode.ShouldBe(GoalPaceMode.NotStarted);
        vm.VsPlanEur.ShouldBe(0m);
        vm.MonthlyCompoundPct.ShouldBe(0m);
        vm.ImpliedCagrPct.ShouldBe(0m);
    }

    [Fact]
    public void NotStarted_when_no_target_date()
    {
        var goalNoDate = new GoalConfig
        {
            Id = 1, TargetEur = 500_000m, TargetDate = null,
            FocusTicker = "CON3.L", UpdatedAt = DateTime.UtcNow,
        };
        var vm = GoalPaceCalculator.Compute(30_000m, goalNoDate,
            new DateOnly(2026, 5, 7), new DateOnly(2026, 1, 1));
        vm.Mode.ShouldBe(GoalPaceMode.NotStarted);
    }

    [Fact]
    public void GoalDatePassed_when_today_after_target()
    {
        var vm = GoalPaceCalculator.Compute(30_000m, Goal(),
            today: new DateOnly(2027, 7, 1), firstTradeDate: new DateOnly(2026, 1, 1));

        vm.Mode.ShouldBe(GoalPaceMode.GoalDatePassed);
    }

    [Fact]
    public void TargetReached_when_currentValue_at_or_above_target()
    {
        var vm = GoalPaceCalculator.Compute(500_001m, Goal(),
            new DateOnly(2026, 5, 7), new DateOnly(2026, 1, 1));

        vm.Mode.ShouldBe(GoalPaceMode.TargetReached);
    }

    [Fact]
    public void Active_computes_vsPlan_negative_when_behind()
    {
        // first trade 2026-01-01, target 2027-06-30 → ~547 day plan.
        // today 2026-05-07 → ~127 days elapsed.
        // linear baseline = 500000 * (127 / 547) ≈ 116,000.
        // current 30,000 → vsPlan ≈ -86,000.
        var vm = GoalPaceCalculator.Compute(
            currentValueEur: 30_000m,
            goal: Goal(target: 500_000m, targetDate: new DateOnly(2027, 6, 30)),
            today: new DateOnly(2026, 5, 7),
            firstTradeDate: new DateOnly(2026, 1, 1));

        vm.Mode.ShouldBe(GoalPaceMode.Active);
        vm.VsPlanEur.ShouldBeLessThan(0m);
        vm.MonthlyCompoundPct.ShouldBeGreaterThan(0m);
        vm.ImpliedCagrPct.ShouldBeGreaterThan(0m);
    }
}
```

- [ ] **Step 2: Run to verify failure**

```
dotnet test --filter "FullyQualifiedName~GoalPaceCalculatorTests"
```
Expected: build error.

- [ ] **Step 3: Implement calculator**

```csharp
// TradyStrat/Features/Dashboard/GoalPaceCalculator.cs
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Dashboard;

public enum GoalPaceMode { Active, NotStarted, GoalDatePassed, TargetReached }

public sealed record GoalPaceVm(
    decimal VsPlanEur,
    decimal MonthlyCompoundPct,
    decimal ImpliedCagrPct,
    GoalPaceMode Mode)
{
    public static readonly GoalPaceVm Zero = new(0m, 0m, 0m, GoalPaceMode.NotStarted);
}

public static class GoalPaceCalculator
{
    public static GoalPaceVm Compute(
        decimal currentValueEur,
        GoalConfig goal,
        DateOnly today,
        DateOnly? firstTradeDate)
    {
        if (firstTradeDate is null || goal.TargetDate is null)
            return GoalPaceVm.Zero;

        var targetDate = goal.TargetDate.Value;
        if (today > targetDate)
            return new(0m, 0m, 0m, GoalPaceMode.GoalDatePassed);

        if (currentValueEur >= goal.TargetEur)
            return new(currentValueEur - goal.TargetEur, 0m, 0m, GoalPaceMode.TargetReached);

        var totalPlanDays = targetDate.DayNumber - firstTradeDate.Value.DayNumber;
        var elapsedDays   = today.DayNumber - firstTradeDate.Value.DayNumber;
        if (totalPlanDays <= 0 || elapsedDays < 0)
            return GoalPaceVm.Zero;

        var baseline   = goal.TargetEur * (decimal)elapsedDays / totalPlanDays;
        var vsPlan     = currentValueEur - baseline;

        var daysLeft   = targetDate.DayNumber - today.DayNumber;
        var monthsLeft = (decimal)daysLeft / 30m;
        var yearsLeft  = (decimal)daysLeft / 365m;

        decimal monthlyPct = 0m, cagrPct = 0m;
        if (monthsLeft > 0m && currentValueEur > 0m)
        {
            var ratio = (double)(goal.TargetEur / currentValueEur);
            monthlyPct = (decimal)(Math.Pow(ratio, 1.0 / (double)monthsLeft) - 1.0) * 100m;
            cagrPct    = (decimal)(Math.Pow(ratio, 1.0 / (double)yearsLeft)  - 1.0) * 100m;
        }

        return new(vsPlan, monthlyPct, cagrPct, GoalPaceMode.Active);
    }
}
```

- [ ] **Step 4: Run tests**

```
dotnet test --filter "FullyQualifiedName~GoalPaceCalculatorTests"
```
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/Dashboard/GoalPaceCalculator.cs \
        TradyStrat.Tests/Dashboard/GoalPaceCalculatorTests.cs
git commit -m "feat(dashboard): add GoalPaceCalculator (vs-plan, monthly-compound, CAGR)"
```

---

## Task 19 — `CallDiffBuilder` + `CallDiff` record

**Files:**
- Create: `TradyStrat/Features/AiSuggestion/CitationChange.cs`
- Create: `TradyStrat/Features/AiSuggestion/CallDiff.cs`
- Create: `TradyStrat/Features/AiSuggestion/CallDiffBuilder.cs`
- Test: `TradyStrat.Tests/AiSuggestion/CallDiffBuilderTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// TradyStrat.Tests/AiSuggestion/CallDiffBuilderTests.cs
using Shouldly;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Shared.Domain;
using Xunit;

namespace TradyStrat.Tests.AiSuggestion;

public class CallDiffBuilderTests
{
    private static Suggestion Make(
        SuggestionAction action, int conviction, params (string Indicator, string Ticker, string Value)[] cits)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(
            cits.Select(c => new Citation("", c.Indicator, c.Ticker, c.Value)).ToList());
        return new Suggestion
        {
            Id = 0, ForDate = new DateOnly(2026, 5, 7),
            Action = action, Conviction = conviction,
            Rationale = "", CitationsJson = json, PromptHash = "h",
            CreatedAt = DateTime.UtcNow,
        };
    }

    [Fact]
    public void Returns_None_when_prior_is_null()
        => new CallDiffBuilder()
            .WithToday(Make(SuggestionAction.Hold, 5))
            .WithPrior(null)
            .Build().ShouldBe(CallDiff.None);

    [Fact]
    public void Detects_action_change()
    {
        var diff = new CallDiffBuilder()
            .WithToday(Make(SuggestionAction.Hold, 5))
            .WithPrior(Make(SuggestionAction.Trim, 4))
            .Build();

        diff.ActionChanged.ShouldBeTrue();
        diff.PriorAction.ShouldBe(SuggestionAction.Trim);
        diff.ConvictionDelta.ShouldBe(1);
    }

    [Fact]
    public void Detects_no_action_change_no_conviction_delta()
    {
        var diff = new CallDiffBuilder()
            .WithToday(Make(SuggestionAction.Hold, 5))
            .WithPrior(Make(SuggestionAction.Hold, 5))
            .Build();

        diff.ActionChanged.ShouldBeFalse();
        diff.ConvictionDelta.ShouldBe(0);
        diff.AddedCitationKeys.ShouldBeEmpty();
        diff.RemovedCitationKeys.ShouldBeEmpty();
        diff.ChangedCitations.ShouldBeEmpty();
    }

    [Fact]
    public void Detects_added_removed_changed_citations()
    {
        var today = Make(SuggestionAction.Hold, 5,
            ("RSI(14)", "CON3.L", "51"),
            ("Ichimoku", "CON3.L", "Inside"),
            ("RSI(14)", "BTC-USD", "70"));   // added
        var prior = Make(SuggestionAction.Hold, 5,
            ("RSI(14)", "CON3.L", "49"),     // value changed
            ("Ichimoku", "CON3.L", "Below"), // value changed
            ("Bollinger", "CON3.L", "Inside"));  // removed

        var diff = new CallDiffBuilder().WithToday(today).WithPrior(prior).Build();

        diff.AddedCitationKeys.ShouldContain("RSI(14):BTC-USD");
        diff.RemovedCitationKeys.ShouldContain("Bollinger:CON3.L");
        diff.ChangedCitations.Select(c => c.Key).ShouldContain("RSI(14):CON3.L");
        diff.ChangedCitations.Select(c => c.Key).ShouldContain("Ichimoku:CON3.L");
    }

    [Fact]
    public void Summary_paragraph_mentions_changes()
    {
        var diff = new CallDiffBuilder()
            .WithToday(Make(SuggestionAction.Hold, 5, ("RSI(14)", "BTC-USD", "70")))
            .WithPrior(Make(SuggestionAction.Trim, 4))
            .Build();

        diff.SummaryParagraph.ShouldContain("Trim");
        diff.SummaryParagraph.ShouldContain("Hold");
    }
}
```

- [ ] **Step 2: Run to verify failure**

```
dotnet test --filter "FullyQualifiedName~CallDiffBuilderTests"
```
Expected: build error.

- [ ] **Step 3: Add `CitationChange`**

```csharp
// TradyStrat/Features/AiSuggestion/CitationChange.cs
namespace TradyStrat.Features.AiSuggestion;

public sealed record CitationChange(string Key, string PriorValue, string NewValue);
```

- [ ] **Step 4: Add `CallDiff`**

```csharp
// TradyStrat/Features/AiSuggestion/CallDiff.cs
using TradyStrat.Shared.Domain;

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
```

- [ ] **Step 5: Add `CallDiffBuilder`**

```csharp
// TradyStrat/Features/AiSuggestion/CallDiffBuilder.cs
using System.Text;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.AiSuggestion;

public sealed class CallDiffBuilder
{
    private Suggestion? _today;
    private Suggestion? _prior;

    public CallDiffBuilder WithToday(Suggestion today) { _today = today; return this; }
    public CallDiffBuilder WithPrior(Suggestion? prior) { _prior = prior; return this; }

    public CallDiff Build()
    {
        if (_today is null) throw new InvalidOperationException("WithToday(...) is required.");
        if (_prior is null) return CallDiff.None;

        var todayCits = _today.Citations.ToDictionary(Key, c => c.Value);
        var priorCits = _prior.Citations.ToDictionary(Key, c => c.Value);

        var added   = todayCits.Keys.Except(priorCits.Keys).OrderBy(k => k).ToList();
        var removed = priorCits.Keys.Except(todayCits.Keys).OrderBy(k => k).ToList();
        var changed = todayCits
            .Where(kv => priorCits.TryGetValue(kv.Key, out var prior) && prior != kv.Value)
            .Select(kv => new CitationChange(kv.Key, priorCits[kv.Key], kv.Value))
            .OrderBy(c => c.Key)
            .ToList();

        var actionChanged   = _today.Action != _prior.Action;
        var convictionDelta = _today.Conviction - _prior.Conviction;

        return new CallDiff(
            ActionChanged: actionChanged,
            PriorAction: _prior.Action,
            ConvictionDelta: convictionDelta,
            AddedCitationKeys: added,
            RemovedCitationKeys: removed,
            ChangedCitations: changed,
            SummaryParagraph: BuildSummary(_today, _prior, added, removed, changed));
    }

    private static string Key(Citation c) => $"{c.Indicator}:{c.Ticker}";

    private static string BuildSummary(
        Suggestion today, Suggestion prior,
        IReadOnlyList<string> added, IReadOnlyList<string> removed,
        IReadOnlyList<CitationChange> changed)
    {
        var sb = new StringBuilder();
        sb.Append(today.Action == prior.Action
            ? $"{today.Action} unchanged."
            : $"{prior.Action} → {today.Action}.");

        var dc = today.Conviction - prior.Conviction;
        if (dc != 0) sb.Append($" Conviction {today.Conviction} ({(dc > 0 ? "+" : "")}{dc}).");

        var noteworthy = new List<string>();
        foreach (var ch in changed) noteworthy.Add($"{ch.Key} {ch.PriorValue} → {ch.NewValue}");
        foreach (var k in added)    noteworthy.Add($"{k} added");
        foreach (var k in removed)  noteworthy.Add($"{k} dropped");
        if (noteworthy.Count > 0)
            sb.Append(" ").Append(string.Join(" · ", noteworthy)).Append('.');

        return sb.ToString();
    }
}
```

- [ ] **Step 6: Run tests**

```
dotnet test --filter "FullyQualifiedName~CallDiffBuilderTests"
```
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add TradyStrat/Features/AiSuggestion/CitationChange.cs \
        TradyStrat/Features/AiSuggestion/CallDiff.cs \
        TradyStrat/Features/AiSuggestion/CallDiffBuilder.cs \
        TradyStrat.Tests/AiSuggestion/CallDiffBuilderTests.cs
git commit -m "feat(ai): add CallDiff record + CallDiffBuilder (no AI in summary)"
```

---

# Phase 6 — ViewModel + LoadDashboardUseCase integration

## Task 20 — Extend `DashboardViewModel`

**Files:**
- Modify: `TradyStrat/Features/Dashboard/DashboardViewModel.cs`

This task is purely additive — no test. Subsequent tasks rely on these fields existing.

- [ ] **Step 1: Update record**

Replace the file with:

```csharp
// TradyStrat/Features/Dashboard/DashboardViewModel.cs
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Features.Indicators;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Dashboard;

public sealed record DashboardViewModel(
    DateOnly Today,
    int EntryNumber,
    PortfolioSnapshot Portfolio,
    GoalConfig Goal,
    Suggestion TodaysCall,
    TickerView[] Tickers,
    GrowthPoint[] Growth,
    DateOnly? LatestPriceDate,
    // ↓ new for spec 1
    GoalPaceVm GoalPace,
    CallDiff CallDiff,
    BackfillStatus BackfillStatus,
    string PriceAsOfRelative,
    string CallAsOfRelative,
    string FxAsOfRelative,
    IReadOnlyDictionary<(string Ticker, IndicatorKind Kind), IndicatorSeries> IndicatorHistories);
```

- [ ] **Step 2: Build (will fail at LoadDashboardUseCase call site)**

```
dotnet build
```
Expected: error in `LoadDashboardUseCase.cs` — its `return new DashboardViewModel(...)` is missing the new arguments. **This is the bridge to the next task.**

- [ ] **Step 3: Stop here. Do NOT commit yet — Task 21 closes the loop.**

---

## Task 21 — Extend `LoadDashboardUseCase`

**Files:**
- Modify: `TradyStrat/Application/UseCases/Dashboard/LoadDashboardUseCase.cs`
- Test: `TradyStrat.Tests/UseCases/Dashboard/LoadDashboardUseCaseBackfillTests.cs`

- [ ] **Step 1: No new automated test in this task**

Integration coverage at this layer is provided by Task 27's manual MCP smoke. The unit tests for `CallDiffBuilder` (T19), `GoalPaceCalculator` (T18), `BackfillSuggestionsUseCase` (T15), `SuggestionBackfillCoordinator` (T16), and the indicator history strategies (T5–T9) collectively cover the new logic this use case orchestrates. If you want an end-to-end check after wiring, write a `LoadDashboardUseCaseBackfillTests.cs` modeled on the existing dashboard-use-case test file in the same folder — seed today's `Suggestion`, a prior `Suggestion` two days back, plus minimal price/FX/trade fixtures, then call `ExecuteAsync(Unit.Value, ct)` and assert that `vm.CallDiff != CallDiff.None`, `vm.GoalPace.Mode == GoalPaceMode.Active`, and `vm.IndicatorHistories.Count > 0`.

- [ ] **Step 2: Modify `LoadDashboardUseCase`**

Replace the existing body. The new ctor adds `IReadRepositoryBase<Suggestion> suggestionRepo`, `IIndicatorHistoryProviderFactory historyFactory` (or use `IndicatorEngine.HistoryFor` directly), `ISuggestionBackfillCoordinator backfillCoord`, and a clock-time UTC accessor.

```csharp
// TradyStrat/Application/UseCases/Dashboard/LoadDashboardUseCase.cs
using Ardalis.Specification;
using TradyStrat.Application.Abstractions;
using TradyStrat.Application.UseCases.AiSuggestion;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Features.Dashboard;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Portfolio;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Time;
using TradyStrat.Specifications.FxRates;
using TradyStrat.Specifications.PriceBars;
using TradyStrat.Specifications.Suggestions;
using TradyStrat.Specifications.Trades;

namespace TradyStrat.Application.UseCases.Dashboard;

public sealed class LoadDashboardUseCase(
    IndicatorEngine indicators,
    PortfolioService portfolio,
    GrowthSeriesBuilder growth,
    FxConverter fx,
    IReadRepositoryBase<GoalConfig> goalRepo,
    IReadRepositoryBase<Trade> tradeRepo,
    IReadRepositoryBase<PriceBar> priceRepo,
    IReadRepositoryBase<Suggestion> suggestionRepo,
    IReadRepositoryBase<FxRate> fxRepo,
    GetTodaysSuggestionUseCase todaysSuggestion,
    ISuggestionBackfillCoordinator backfillCoord,
    IClock clock,
    ILogger<LoadDashboardUseCase> log)
    : UseCaseBase<Unit, DashboardViewModel>(log)
{
    private const string FocusTicker = "CON3.L";
    private const string FxPair      = "EURUSD=X";
    private const int    SparklineWindow = 30;

    private static readonly (string Ticker, string Currency)[] Catalog =
    [
        (FocusTicker, "USD"),
        ("COIN",      "USD"),
        ("BTC-USD",   "USD"),
    ];

    protected override async Task<DashboardViewModel> ExecuteCore(Unit _, CancellationToken ct)
    {
        var today = clock.TodayInExchangeTzFor(FocusTicker);
        var goal  = await goalRepo.GetByIdAsync(1, ct) ?? GoalConfig.Default(clock.UtcNow());

        var tickers = new List<TickerView>();
        decimal? focusPriceEur = null;

        foreach (var (ticker, currency) in Catalog)
        {
            var reading = await indicators.ComputeFor(ticker, ct);
            decimal? eur = null;
            if (currency == "USD")
                eur = await fx.UsdToEurAsync(reading.Price, today, ct);
            if (ticker == FocusTicker) focusPriceEur = eur ?? reading.Price;

            var deltaPct = await ComputeDeltaPctAsync(ticker, ct);
            tickers.Add(new TickerView(
                ticker, currency, reading.Price, eur, deltaPct, reading.Zone));
        }

        var snap = await portfolio.SnapshotAsync(focusPriceEur ?? 0m, goal.TargetEur, ct);
        var growthSeries = await growth.BuildAsync(FocusTicker, ct);
        var todays    = await todaysSuggestion.ExecuteAsync(Unit.Value, ct);
        var entryNum  = await tradeRepo.CountAsync(new AllTradesSpec(), ct);
        var latestBar = await priceRepo.FirstOrDefaultAsync(new LatestPriceBarSpec(FocusTicker), ct);

        // ---- new: prior suggestion + call diff
        var prior = await suggestionRepo.FirstOrDefaultAsync(new PriorSuggestionSpec(today), ct);
        var callDiff = new CallDiffBuilder()
            .WithToday(todays)
            .WithPrior(prior)
            .Build();

        // ---- new: indicator histories per citation
        var histories = new Dictionary<(string Ticker, IndicatorKind Kind), IndicatorSeries>();
        foreach (var c in todays.Citations)
        {
            var kind = IndicatorKindParser.From(c.Indicator);
            if (kind is null) continue;
            var key = (c.Ticker, kind.Value);
            if (histories.ContainsKey(key)) continue;
            histories[key] = await indicators.HistoryFor(c.Ticker, kind.Value, SparklineWindow, ct);
        }

        // ---- new: goal pace
        var firstTrade = await tradeRepo.FirstOrDefaultAsync(new EarliestTradeSpec(), ct);
        var goalPace = GoalPaceCalculator.Compute(
            currentValueEur: snap.CurrentValueEur,
            goal: goal,
            today: today,
            firstTradeDate: firstTrade?.ExecutedOn);

        // ---- new: freshness pills
        var nowUtc = clock.UtcNow();
        var fxLatest = await fxRepo.FirstOrDefaultAsync(new LatestFxRateSpec(FxPair), ct);
        var priceAsOf = latestBar is { } lb
            ? RelativeTimeFormatter.Format(lb.Date.ToDateTime(TimeOnly.MinValue), nowUtc)
            : "";
        var callAsOf = RelativeTimeFormatter.Format(todays.CreatedAt, nowUtc);
        var fxAsOf   = fxLatest is { } fxr
            ? RelativeTimeFormatter.Format(fxr.FetchedAt, nowUtc)
            : "";

        // ---- new: enqueue backfill (fire-and-forget) & snapshot status
        var lastEntryBeforeToday = prior?.ForDate;
        if (lastEntryBeforeToday is { } lastDate && today.AddDays(-1) > lastDate)
        {
            _ = backfillCoord.EnsureBackfilledAsync(lastDate, today.AddDays(-1), ct);
        }
        var backfillStatus = backfillCoord.Status;

        return new DashboardViewModel(
            Today: today,
            EntryNumber: entryNum,
            Portfolio: snap,
            Goal: goal,
            TodaysCall: todays,
            Tickers: tickers.ToArray(),
            Growth: growthSeries,
            LatestPriceDate: latestBar?.Date,
            GoalPace: goalPace,
            CallDiff: callDiff,
            BackfillStatus: backfillStatus,
            PriceAsOfRelative: priceAsOf,
            CallAsOfRelative: callAsOf,
            FxAsOfRelative: fxAsOf,
            IndicatorHistories: histories);
    }

    private async Task<decimal?> ComputeDeltaPctAsync(string ticker, CancellationToken ct)
    {
        var bars = await priceRepo.ListAsync(new PriceBarsForTickerSpec(ticker), ct);
        if (bars.Count < 2) return null;
        var prev = bars[^2].Close;
        var curr = bars[^1].Close;
        if (prev == 0m) return null;
        return (curr - prev) / prev * 100m;
    }
}
```

- [ ] **Step 3: Add the missing `EarliestTradeSpec`**

```csharp
// TradyStrat/Specifications/Trades/EarliestTradeSpec.cs
using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.Trades;

public sealed class EarliestTradeSpec : Specification<Trade>
{
    public EarliestTradeSpec() => Query.OrderBy(t => t.ExecutedOn).Take(1);
}
```

Add a round-trip test for it next to the others in `SpecsRoundtripTests.cs`.

- [ ] **Step 4: Build**

```
dotnet build
```
Expected: success.

- [ ] **Step 5: Run all tests**

```
dotnet test
```
Expected: green. Any existing test that constructed `LoadDashboardUseCase` directly will need the new ctor params — fix in place.

- [ ] **Step 6: Run app smoke**

```
dotnet run --project TradyStrat
```

Open http://127.0.0.1:5180. **Dashboard should still render** — backend now produces the new VM fields but the UI doesn't use them yet. Backfill may run in the background if there's a gap; nothing visible.

- [ ] **Step 7: Commit (single commit for this whole bridge)**

```bash
git add TradyStrat/Features/Dashboard/DashboardViewModel.cs \
        TradyStrat/Application/UseCases/Dashboard/LoadDashboardUseCase.cs \
        TradyStrat/Specifications/Trades/EarliestTradeSpec.cs \
        TradyStrat.Tests/UseCases/Dashboard/LoadDashboardUseCaseBackfillTests.cs \
        TradyStrat.Tests/Specifications/SpecsRoundtripTests.cs
git commit -m "feat(dashboard): wire goal-pace, call diff, sparklines, backfill into VM"
```

---

# Phase 7 — UI

## Task 22 — Goal-pace banner in `HeroCapital`

**Files:**
- Modify: `TradyStrat/Features/Dashboard/Components/HeroCapital.razor` and `.cs` and `.css`

The three-stat row goes below the existing "BY ... DAYS LEFT" line.

- [ ] **Step 1: Add `GoalPace` parameter to code-behind**

In `HeroCapital.razor.cs`, append:

```csharp
[Parameter, EditorRequired] public GoalPaceVm GoalPace { get; set; } = null!;

private string FormatVsPlan(decimal eur)
    => (eur >= 0 ? "+€" : "−€") + Math.Abs(eur).ToString("N0", FrFr);
```

Add `using TradyStrat.Features.Dashboard;` at the top.

- [ ] **Step 2: Add `<GoalPaceVm>` to the page invocation**

In `Features/Dashboard/DashboardPage.razor`, find the `<HeroCapital ... />` element and add:

```razor
<HeroCapital Snap="@Vm.Portfolio" Goal="@Vm.Goal" Today="@Vm.Today"
             GoalPace="@Vm.GoalPace" />
```

- [ ] **Step 3: Update `HeroCapital.razor`**

Below the existing `<div class="progress">...</div>...</dl>` block, before the closing `</div>` of the hero, add:

```razor
@if (GoalPace.Mode == GoalPaceMode.Active)
{
    <div class="pace-stats">
        <div class="stat">
            <div class="k">vs. plan</div>
            <div class="v @(GoalPace.VsPlanEur < 0 ? "neg" : "pos")">
                @FormatVsPlan(GoalPace.VsPlanEur)
            </div>
        </div>
        <div class="stat">
            <div class="k">monthly growth needed</div>
            <div class="v">@GoalPace.MonthlyCompoundPct.ToString("F1", FrFr) % / mo</div>
        </div>
        <div class="stat">
            <div class="k">implied CAGR</div>
            <div class="v">@GoalPace.ImpliedCagrPct.ToString("F0", FrFr) %</div>
        </div>
    </div>
}
else if (GoalPace.Mode == GoalPaceMode.TargetReached)
{
    <p class="pace-line italic">Target reached — keep going.</p>
}
else if (GoalPace.Mode == GoalPaceMode.GoalDatePassed)
{
    <p class="pace-line italic">Goal date passed — €@GoalPace.VsPlanEur.ToString("N0", FrFr) over.</p>
}
```

- [ ] **Step 4: Update `HeroCapital.razor.css`**

Append:

```css
.pace-stats {
    display: grid;
    grid-template-columns: 1fr 1fr 1fr;
    gap: 22px;
    border-top: 1px solid rgba(196,154,86,0.15);
    padding-top: 14px;
    margin-top: 16px;
}
.pace-stats .stat .k {
    font-size: 9px;
    letter-spacing: 0.22em;
    color: rgba(236,230,214,0.45);
    text-transform: uppercase;
    margin-bottom: 4px;
}
.pace-stats .stat .v {
    font-size: 18px;
    color: #ece6d6;
    letter-spacing: -0.3px;
    font-feature-settings: "tnum";
}
.pace-stats .stat .v.neg { color: #c87a5a; }
.pace-stats .stat .v.pos { color: #6a9a6a; }
.pace-line {
    font-style: italic;
    font-size: 13px;
    color: rgba(236,230,214,0.62);
    margin-top: 14px;
}
```

- [ ] **Step 5: Run app**

```
dotnet run --project TradyStrat
```

Open http://127.0.0.1:5180. Verify: the three-stat row appears below "BY 30 · 06 · 2027 · 419 DAYS LEFT". The values are real (derived from current portfolio + goal).

- [ ] **Step 6: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/HeroCapital.razor \
        TradyStrat/Features/Dashboard/Components/HeroCapital.razor.cs \
        TradyStrat/Features/Dashboard/Components/HeroCapital.razor.css \
        TradyStrat/Features/Dashboard/DashboardPage.razor
git commit -m "feat(dashboard): add goal-pace three-stat row to hero"
```

---

## Task 23 — Inline freshness pills (date strip + "TODAY'S CALL · …")

**Files:**
- Modify: `TradyStrat/Features/Dashboard/Components/VaultMasthead.razor`
- Modify: `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor`

This task only adds the inline " · 12 min ago" / " · 14h ago" suffix to existing labels. The freshness data is already on the ViewModel.

- [ ] **Step 1: VaultMasthead — extend the date strip**

Open `VaultMasthead.razor`. Find the element rendering "07 · 05 · 2026 · ENTRY NO. 0003". Append:

```razor
@if (!string.IsNullOrEmpty(PriceAsOfRelative))
{
    <span class="freshness"> · prices @PriceAsOfRelative</span>
}
```

In `VaultMasthead.razor.cs` (or the inline `@code` block), add:

```csharp
[Parameter] public string PriceAsOfRelative { get; set; } = "";
```

In the dashboard page invocation:

```razor
<VaultMasthead Today="@Vm.Today" EntryNumber="@Vm.EntryNumber"
               PriceAsOfRelative="@Vm.PriceAsOfRelative" />
```

CSS in `VaultMasthead.razor.css`:

```css
.freshness {
    color: rgba(236,230,214,0.42);
    font-size: 9px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
}
```

- [ ] **Step 2: TodaysCallCard — extend the label**

In `TodaysCallCard.razor`, find the label `"TODAY'S CALL · 7 MAI"` element. Append:

```razor
@if (!string.IsNullOrEmpty(CallAsOfRelative))
{
    <span class="freshness"> · @CallAsOfRelative</span>
}
```

In `.razor.cs`:

```csharp
[Parameter] public string CallAsOfRelative { get; set; } = "";
```

In the dashboard page invocation, pass `CallAsOfRelative="@Vm.CallAsOfRelative"`.

CSS:

```css
.freshness {
    color: rgba(236,230,214,0.42);
    font-size: 9px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
}
```

- [ ] **Step 3: Run app**

```
dotnet run --project TradyStrat
```

Verify: the date strip shows " · prices 12 min ago" (or whatever the cache says). The Today's Call label shows " · 14h ago". No layout regression on narrow viewports.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/VaultMasthead.razor* \
        TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor \
        TradyStrat/Features/Dashboard/DashboardPage.razor
git commit -m "feat(dashboard): inline freshness pills on date strip + call label"
```

---

## Task 24 — `TodaysCallCard` diff bar + sparklines + backfill subscription

**Files:**
- Modify: `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor`
- Modify: `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.cs`
- Modify: `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.css`

The largest UI task. Adds: italic summary header bar (when prior exists), faint background tint on changed citation rows, right-aligned sparkline column, backfill progress pill that subscribes to the coordinator's `StatusChanged`.

- [ ] **Step 1: Add parameters + injected coordinator + lifecycle**

Open `TodaysCallCard.razor.cs`. Add at top:

```csharp
using Microsoft.AspNetCore.Components;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Features.Indicators;
using TradyStrat.Shared.Domain;
```

Add to the class:

```csharp
[Parameter, EditorRequired] public CallDiff CallDiff { get; set; } = CallDiff.None;
[Parameter, EditorRequired] public IReadOnlyDictionary<(string Ticker, IndicatorKind Kind), IndicatorSeries> IndicatorHistories { get; set; }
    = new Dictionary<(string, IndicatorKind), IndicatorSeries>();
[Parameter, EditorRequired] public BackfillStatus BackfillStatus { get; set; } = BackfillStatus.Idle.Instance;

[Inject] private ISuggestionBackfillCoordinator Coordinator { get; set; } = null!;

private string? _backfillLabel;

protected override void OnInitialized()
{
    Coordinator.StatusChanged += OnBackfillStatus;
    UpdateBackfillLabel(BackfillStatus);
}

private void OnBackfillStatus(BackfillStatus status)
{
    InvokeAsync(() =>
    {
        UpdateBackfillLabel(status);
        StateHasChanged();
    });
}

private void UpdateBackfillLabel(BackfillStatus status)
{
    _backfillLabel = status switch
    {
        BackfillStatus.Running r => $"backfilling {r.Total - r.Remaining + 1} of {r.Total} — {r.CurrentDate:dd MMM}",
        BackfillStatus.Failed f  => $"stopped at {f.FailedAt:dd MMM} — {f.Reason}",
        _ => null,
    };
}

public void Dispose() => Coordinator.StatusChanged -= OnBackfillStatus;
```

Make the class implement `IDisposable`:

```csharp
public partial class TodaysCallCard : ComponentBase, IDisposable
```

- [ ] **Step 2: Render summary header + backfill pill + diff-aware citations**

In `TodaysCallCard.razor`, near the top of the card body (after the existing label-row):

```razor
@if (CallDiff != CallDiff.None && !string.IsNullOrEmpty(CallDiff.SummaryParagraph))
{
    <div class="summary-bar">
        @CallDiff.SummaryParagraph
    </div>
}
@if (_backfillLabel is not null)
{
    <div class="backfill-pill">@_backfillLabel</div>
}
```

For the citation list, wrap each `<li>` with the changed-row class when applicable. Replace the existing iteration with:

```razor
@{
    var changedKeys = CallDiff.ChangedCitations.Select(c => c.Key).ToHashSet();
    var addedKeys   = CallDiff.AddedCitationKeys.ToHashSet();
}
<ul class="citations">
    @{ var i = 0; }
    @foreach (var c in TodaysCall.Citations)
    {
        i++;
        var key = $"{c.Indicator}:{c.Ticker}";
        var isChanged = changedKeys.Contains(key);
        var isNew     = addedKeys.Contains(key);
        var kind = IndicatorKindParser.From(c.Indicator);
        IndicatorSeries? series = null;
        if (kind is { } k && IndicatorHistories.TryGetValue((c.Ticker, k), out var s))
            series = s;
        <li class="@(isChanged ? "changed" : "")">
            <span class="roman">@RomanLowercase(i).</span>
            <span class="claim"><b>@c.Indicator</b> (@c.Ticker): @c.Claim
                @if (isNew) { <span class="new-tag">new</span> }
            </span>
            <span class="v">@c.Value</span>
            @if (series is not null && series.Values.Count > 0)
            {
                <span class="spark">
                    @((MarkupString)RenderSparklineSvg(series))
                </span>
            }
            else
            {
                <span class="spark"></span>
            }
        </li>
    }
</ul>
```

Add helpers in `TodaysCallCard.razor.cs`:

```csharp
private static string RomanLowercase(int i) =>
    i switch
    {
        1 => "i", 2 => "ii", 3 => "iii", 4 => "iv", 5 => "v",
        6 => "vi", 7 => "vii", 8 => "viii", 9 => "ix", 10 => "x",
        _ => i.ToString(),
    };

private static string RenderSparklineSvg(IndicatorSeries s)
{
    if (s.Values.Count < 2) return "";
    decimal min = s.Values.Min(), max = s.Values.Max();
    if (s.ThresholdHi is { } hi) { if (hi < min) min = hi; if (hi > max) max = hi; }
    if (s.ThresholdLo is { } lo) { if (lo < min) min = lo; if (lo > max) max = lo; }
    var range = max - min;
    if (range == 0m) range = 1m;

    var w = 60; var h = 14;
    var pts = new System.Text.StringBuilder();
    for (int i = 0; i < s.Values.Count; i++)
    {
        var x = (double)i * w / (s.Values.Count - 1);
        var y = h - (double)((s.Values[i] - min) / range) * h;
        if (i > 0) pts.Append(' ');
        pts.Append($"{x.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)},{y.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}");
    }

    var thresholdLines = new System.Text.StringBuilder();
    void DrawThreshold(decimal? t)
    {
        if (t is null) return;
        var ty = h - (double)((t.Value - min) / range) * h;
        thresholdLines.Append(
            $"<line x1=\"0\" y1=\"{ty.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}\" " +
            $"x2=\"{w}\" y2=\"{ty.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}\" " +
            $"stroke=\"rgba(196,154,86,0.25)\" stroke-dasharray=\"2 2\" />");
    }
    DrawThreshold(s.ThresholdHi);
    DrawThreshold(s.ThresholdLo);

    return $"<svg viewBox=\"0 0 {w} {h}\" width=\"{w}\" height=\"{h}\">" +
           thresholdLines +
           $"<polyline points=\"{pts}\" fill=\"none\" stroke=\"#c49a56\" stroke-width=\"1.2\" />" +
           "</svg>";
}
```

- [ ] **Step 3: CSS**

Append to `TodaysCallCard.razor.css`:

```css
.summary-bar {
    background: rgba(196,154,86,0.06);
    border-left: 2px solid rgba(196,154,86,0.45);
    padding: 8px 12px;
    font-size: 12px;
    color: rgba(236,230,214,0.72);
    margin-bottom: 14px;
    font-style: italic;
}
.backfill-pill {
    margin-bottom: 10px;
    font-size: 10px;
    letter-spacing: 0.16em;
    color: rgba(196,154,86,0.85);
    text-transform: uppercase;
}
.citations li {
    display: grid;
    grid-template-columns: auto 1fr auto auto;
    align-items: center;
    gap: 8px;
}
.citations li.changed {
    background: rgba(196,154,86,0.06);
    margin: 0 -6px;
    padding: 6px;
    border-radius: 2px;
}
.citations .spark {
    width: 60px;
    display: inline-flex;
    justify-content: flex-end;
}
.new-tag {
    display: inline-block;
    font-size: 8px;
    letter-spacing: 0.18em;
    color: #c49a56;
    border: 1px solid rgba(196,154,86,0.45);
    padding: 1px 6px;
    margin-left: 8px;
    text-transform: uppercase;
    vertical-align: middle;
}
```

- [ ] **Step 4: Pass parameters from DashboardPage**

In `DashboardPage.razor`, the `<TodaysCallCard />` invocation must include:

```razor
<TodaysCallCard TodaysCall="@Vm.TodaysCall"
                CallAsOfRelative="@Vm.CallAsOfRelative"
                CallDiff="@Vm.CallDiff"
                IndicatorHistories="@Vm.IndicatorHistories"
                BackfillStatus="@Vm.BackfillStatus" />
```

- [ ] **Step 5: Run app**

```
dotnet run --project TradyStrat
```

Verify (open http://127.0.0.1:5180):
- The italic summary header bar appears (if there's a prior suggestion in the DB).
- Sparklines render in a right-aligned column, vertically aligned across rows.
- If you delete a recent prior suggestion row from the DB and reload, the backfill pill briefly shows progress, then disappears once the chain completes.

- [ ] **Step 6: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor* \
        TradyStrat/Features/Dashboard/DashboardPage.razor
git commit -m "feat(dashboard): call-diff bar + sparklines + backfill pill on TodaysCallCard"
```

---

## Task 25 — `growth-chart.js` ES module

**Files:**
- Create: `TradyStrat/wwwroot/js/growth-chart.js`

- [ ] **Step 1: Add the module**

```javascript
// TradyStrat/wwwroot/js/growth-chart.js
const formatters = {
    eur: new Intl.NumberFormat('fr-FR', { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 }),
    pct: new Intl.NumberFormat('fr-FR', { style: 'percent', maximumFractionDigits: 1 }),
};

function fmtSigned(n) {
    if (n == null || isNaN(n)) return '—';
    const v = Math.round(n);
    return (v >= 0 ? '+€' : '−€') + Math.abs(v).toLocaleString('fr-FR');
}

function findIndex(dates, mouseRatio) {
    const idx = Math.round(mouseRatio * (dates.length - 1));
    return Math.max(0, Math.min(dates.length - 1, idx));
}

const tooltipsByElement = new WeakMap();

export function init(svg, data /*, locale */) {
    if (!svg || !data || !Array.isArray(data.dates) || data.dates.length === 0) return;

    const wrap = svg.parentElement;
    if (getComputedStyle(wrap).position === 'static') wrap.style.position = 'relative';

    const tooltip = document.createElement('div');
    tooltip.className = 'gc-tooltip';
    tooltip.style.position = 'absolute';
    tooltip.style.pointerEvents = 'none';
    tooltip.style.display = 'none';
    wrap.appendChild(tooltip);

    const guide = document.createElementNS('http://www.w3.org/2000/svg', 'line');
    guide.setAttribute('class', 'gc-guide');
    guide.setAttribute('y1', '0');
    guide.setAttribute('y2', '100%');
    guide.style.stroke = 'rgba(196,154,86,0.5)';
    guide.style.strokeWidth = '1';
    guide.style.display = 'none';
    svg.appendChild(guide);

    function show(evt) {
        const rect = svg.getBoundingClientRect();
        const ratio = (evt.clientX - rect.left) / rect.width;
        const i = findIndex(data.dates, ratio);
        const d = new Date(data.dates[i]);
        const dateLabel = d.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });

        const capital     = data.capital[i] ?? 0;
        const prior       = i > 0 ? (data.capital[i - 1] ?? capital) : capital;
        const dPrior      = capital - prior;
        const position    = data.position?.[i];
        const focusPxEur  = data.focusTickerEur?.[i];
        const planAtIdx   = data.targetEur && data.targetDate
            ? data.targetEur * (i / (data.dates.length - 1))
            : null;
        const vsPlan      = planAtIdx != null ? capital - planAtIdx : null;

        tooltip.innerHTML =
            `<div class="date">${dateLabel}</div>` +
            `<div class="big">${formatters.eur.format(capital)}</div>` +
            `<div class="delta">${fmtSigned(dPrior)} vs. prior day</div>` +
            (position != null ? `<div class="row"><span>POSITION</span><span>${Math.round(position).toLocaleString('fr-FR')} sh</span></div>` : '') +
            (focusPxEur != null ? `<div class="row"><span>CON3.L</span><span>€${focusPxEur.toFixed(2)}</span></div>` : '') +
            (vsPlan != null ? `<div class="row"><span>VS. PLAN</span><span>${fmtSigned(vsPlan)}</span></div>` : '');

        tooltip.style.display = 'block';
        const x = ratio * rect.width;
        tooltip.style.left = Math.min(rect.width - tooltip.offsetWidth - 10, Math.max(0, x + 12)) + 'px';
        tooltip.style.top  = '8px';

        guide.setAttribute('x1', `${ratio * 100}%`);
        guide.setAttribute('x2', `${ratio * 100}%`);
        guide.style.display = '';
    }

    function hide() {
        tooltip.style.display = 'none';
        guide.style.display = 'none';
    }

    svg.addEventListener('pointermove', show);
    svg.addEventListener('pointerleave', hide);

    tooltipsByElement.set(svg, { tooltip, guide, show, hide });
}

export function dispose(svg) {
    const refs = tooltipsByElement.get(svg);
    if (!refs) return;
    svg.removeEventListener('pointermove', refs.show);
    svg.removeEventListener('pointerleave', refs.hide);
    refs.tooltip.remove();
    refs.guide.remove();
    tooltipsByElement.delete(svg);
}
```

- [ ] **Step 2: Confirm static-file serving**

The file lives in `wwwroot/js/`. Blazor Server serves `wwwroot/` automatically. No additional configuration.

- [ ] **Step 3: Smoke**

```
dotnet run --project TradyStrat
```

Visit http://127.0.0.1:5180/js/growth-chart.js — the file should be served as JavaScript. (No interop wired yet — that's the next task.)

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/wwwroot/js/growth-chart.js
git commit -m "feat(chart): add growth-chart.js ES module for crosshair tooltip"
```

---

## Task 26 — Wire JSInterop in `GrowthChart.razor.cs`

**Files:**
- Modify: `TradyStrat/Features/Dashboard/Components/GrowthChart.razor`
- Modify: `TradyStrat/Features/Dashboard/Components/GrowthChart.razor.cs`
- Modify: `TradyStrat/Features/Dashboard/Components/GrowthChart.razor.css`

- [ ] **Step 1: Read existing code-behind**

```
cat TradyStrat/Features/Dashboard/Components/GrowthChart.razor.cs
```

It currently has no JS interop. Note the existing parameters (`Growth: GrowthPoint[]`, plus likely `Goal` for the target line).

- [ ] **Step 2: Modify the Razor markup**

In `GrowthChart.razor`, add `@ref="_svgRef"` to the `<svg>` element and add a wrapper div. Example shape:

```razor
<div class="growth-chart-wrap">
    <svg @ref="_svgRef" viewBox="0 0 1200 240" preserveAspectRatio="none" class="chart">
        @* existing path/grid/etc. unchanged *@
    </svg>
</div>
```

- [ ] **Step 3: Add JS interop in the code-behind**

Open `GrowthChart.razor.cs`. Add usings:

```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TradyStrat.Features.Dashboard;
using TradyStrat.Shared.Domain;
```

Make the partial class implement `IAsyncDisposable`:

```csharp
public partial class GrowthChart : ComponentBase, IAsyncDisposable
```

Add fields and parameters (preserve existing ones):

```csharp
[Inject] private IJSRuntime JS { get; set; } = null!;

[Parameter, EditorRequired] public GrowthPoint[] Growth { get; set; } = [];
[Parameter, EditorRequired] public GoalConfig Goal { get; set; } = null!;
[Parameter, EditorRequired] public PortfolioSnapshot Portfolio { get; set; } = null!;
[Parameter] public decimal? FocusTickerEur { get; set; }   // optional — server may not always have it

private ElementReference _svgRef;
private IJSObjectReference? _module;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (!firstRender) return;
    try
    {
        _module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/growth-chart.js");
        await _module.InvokeVoidAsync("init", _svgRef, BuildPayload(), "fr-FR");
    }
    catch (JSException) { /* graceful degradation */ }
}

private object BuildPayload() => new
{
    dates       = Growth.Select(g => g.Date.ToString("o")).ToArray(),
    capital     = Growth.Select(g => (double)g.ValueEur).ToArray(),
    position    = Growth.Select(_ => (double)Portfolio.Shares).ToArray(),       // shares constant per render; refine later
    focusTickerEur = Growth.Select(_ => (double)(FocusTickerEur ?? 0m)).ToArray(),
    targetEur   = (double)Goal.TargetEur,
    targetDate  = Goal.TargetDate?.ToString("o"),
};

public async ValueTask DisposeAsync()
{
    if (_module is null) return;
    try
    {
        await _module.InvokeVoidAsync("dispose", _svgRef);
        await _module.DisposeAsync();
    }
    catch (JSDisconnectedException) { }
    catch (ObjectDisposedException) { }
}
```

`position` and `focusTickerEur` are **per-render constants** in this iteration (we don't yet have per-day position/price-EUR series on the ViewModel). The crosshair will still show those fields, just with constant values; a follow-up can add proper per-day series. This is an acceptable simplification for spec 1.

- [ ] **Step 4: Pass parameters from `DashboardPage`**

```razor
<GrowthChart Growth="@Vm.Growth" Goal="@Vm.Goal" Portfolio="@Vm.Portfolio" />
```

- [ ] **Step 5: CSS for tooltip**

Append to `GrowthChart.razor.css`:

```css
.growth-chart-wrap {
    position: relative;
}
::deep .gc-tooltip {
    background: rgba(20,18,14,0.96);
    border: 1px solid rgba(196,154,86,0.35);
    padding: 10px 14px;
    color: #ece6d6;
    backdrop-filter: blur(4px);
    font-family: 'Newsreader', Georgia, serif;
    font-size: 12px;
    pointer-events: none;
    min-width: 200px;
}
::deep .gc-tooltip .date {
    font-size: 10px; letter-spacing: 0.2em; color: #c49a56;
    text-transform: uppercase; margin-bottom: 4px;
}
::deep .gc-tooltip .big {
    font-size: 22px; line-height: 1; letter-spacing: -0.5px;
    font-feature-settings: "tnum";
}
::deep .gc-tooltip .delta {
    font-size: 11px; color: rgba(236,230,214,0.65); margin-top: 4px; font-style: italic;
}
::deep .gc-tooltip .row {
    display: flex; justify-content: space-between; gap: 18px;
    font-size: 11px; color: rgba(236,230,214,0.7); margin-top: 2px;
}
::deep .gc-tooltip .row span:first-child {
    letter-spacing: 0.1em; text-transform: uppercase; font-size: 9px;
    color: rgba(236,230,214,0.45);
}
```

`::deep` is Blazor scoped-CSS syntax for descending into child markup that the JS module injects.

- [ ] **Step 6: Run app + manual hover verification**

```
dotnet run --project TradyStrat
```

Open http://127.0.0.1:5180. Hover the growth chart with the mouse — verify the tooltip appears with date + capital + delta + position + ticker + vs.-plan, and a vertical guide line. Move along the chart; the values should update.

Open dev-tools console — should be no errors. If the JS module 404s, confirm the file lives at `TradyStrat/wwwroot/js/growth-chart.js` (not under `TradyStrat/TradyStrat/wwwroot/...`).

- [ ] **Step 7: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/GrowthChart.razor* \
        TradyStrat/Features/Dashboard/DashboardPage.razor
git commit -m "feat(chart): wire crosshair tooltip via JSInterop ES module"
```

---

# Phase 8 — Manual smoke verification

## Task 27 — Chrome DevTools MCP checklist

**Goal:** verify all five features end-to-end against the running app.

- [ ] **Step 1: Run app**

```
dotnet run --project TradyStrat
```

- [ ] **Step 2: Crosshair (#1)**

- Hover the growth chart at three positions; verify tooltip shows: date, capital, Δ vs. prior day, position, focus-ticker close, vs. plan.
- Resize to mobile (390 × 844). Tap the chart; verify tooltip pins to top.

- [ ] **Step 3: Goal-pace banner (#3)**

- Verify three-stat row shows under "BY ... DAYS LEFT".
- Toggle Settings → set TargetDate to a past date. Reload dashboard. Verify "Goal date passed" italic line.
- Reset settings.

- [ ] **Step 4: Today's Call diff + freshness (#4 + #7)**

- Verify "TODAY'S CALL · 7 MAI · Xh ago" inline freshness pill.
- If a prior suggestion exists in the DB: verify the italic summary bar mentions changes; verify changed rows have the faint background tint.

- [ ] **Step 5: Sparklines (#9)**

- Verify the right-aligned column of small sparklines next to citation values.
- Confirm threshold lines (RSI 70/30, Bollinger upper/lower, SMA-200 horizontal).

- [ ] **Step 6: Backfill (#4 backfill machinery)**

- Manually delete a recent `Suggestion` row from the SQLite DB (path in README).
- Reload dashboard.
- Verify the inline "backfilling N of M — DD MMM" pill appears, then disappears once the chain completes; the diff sharpens forward.

- [ ] **Step 7: Verify no console errors**

Open browser dev-tools. Network 200s for `/js/growth-chart.js`. No console errors.

- [ ] **Step 8: Stop app, run full test suite one more time**

```
dotnet test
```
Expected: green.

- [ ] **Step 9: Commit anything outstanding (likely nothing)**

```bash
git status
# if clean, no commit needed
```

---

# Self-review checklist (run by the engineer before declaring done)

- [ ] All 5 features in the spec have at least one task implementing them.
- [ ] No TBD / TODO / "implement later" / placeholder text in the plan.
- [ ] Type names match across tasks (`IndicatorKind`, `IndicatorSeries`, `CallDiff`, `BackfillStatus`, etc.).
- [ ] Method signatures are consistent — `EnsureBackfilledAsync(fromExclusive, toInclusive, ct)`, `CreateAsync(asOf, ct)`, `HistoryFor(ticker, kind, lastN, ct)`.
- [ ] The five new specs are referenced by the tasks that consume them (Task 13: SnapshotFactory uses `PriceBarsAsOfSpec`/`FxRateAsOfSpec`/`TradesAsOfSpec`; Task 16: coordinator uses `SuggestionsInRangeSpec`; Task 21: LoadDashboardUseCase uses `PriorSuggestionSpec` and `EarliestTradeSpec`).
- [ ] Existing exception types (`AnthropicCallFailedException`, `PriceFeedUnavailableException`, etc.) are propagated unchanged — no new exception classes.
- [ ] DI registrations updated: history strategies + factory (T11), `ISnapshotFactory` (T13), backfill use case + coordinator (T17).
- [ ] Razor parameters wired through `DashboardPage.razor` for HeroCapital, VaultMasthead, TodaysCallCard, GrowthChart.
- [ ] `dotnet test` passes after every task.
- [ ] Manual MCP smoke (Task 27) covers all six visual checkpoints.
