# TradyStrat — Dashboard polish (visual bugs + readability) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix five concrete visual bugs on the dashboard and lift readability of the today's-call card and growth chart, without changing schema, FX-pair coverage, or any non-dashboard feature.

**Architecture:** All changes are confined to `Features/Dashboard/` (Razor + scoped CSS) plus one targeted `LoadDashboardUseCase` adjustment to reconcile the hero and chart "today" numbers. No new types except a small `GoalLine` constants class to keep the SVG aligned. Unit tests for the use-case and component-class changes; manual viewport verification (Chrome DevTools MCP at 1440 / 1024 / 500) for pure CSS/markup tweaks.

**Tech Stack:** .NET 10 · Blazor Server · Razor scoped CSS · xunit.v3 + Shouldly + EF InMemory · Chrome DevTools MCP for visual verification.

**Out-of-scope (deferred):**
- The CON3.L `$` symbol bug. The data-layer cause (`LoadDashboardUseCase.cs:40` declaring CON3.L as `USD` while Yahoo serves it in GBp; FX support limited to EURUSD) crosses the price-feed and FX subsystems. A separate plan should add GBPEUR coverage, fix the catalog, and route CON3.L through a `GbpPenceToEurAsync` path. Track as **Followup-A** at the bottom of this document.

---

## File map

### Modified files
```
TradyStrat/Features/Dashboard/Components/GrowthChart.razor          # goal-line corners, Y labels
TradyStrat/Features/Dashboard/Components/GrowthChart.razor.cs       # Y-axis label model
TradyStrat/Features/Dashboard/Components/GrowthChart.razor.css      # Y-axis label styling
TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor       # dropcap + structured diff
TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.cs    # VerbStem -> Verb (no period)
TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.css   # diff list styling
TradyStrat/Features/Dashboard/Components/PortfolioRail.razor.css    # mid-width breakpoint
TradyStrat/Application/UseCases/Dashboard/LoadDashboardUseCase.cs   # pin last GrowthPoint to snap
TradyStrat.Tests/UseCases/Dashboard/LoadDashboardUseCaseTests.cs    # 1 new [Fact]
TradyStrat.Tests/Dashboard/CallDiffViewModelTests.cs                # NEW (Task 6 helper coverage)
```

### New files
```
TradyStrat/Features/Dashboard/Components/CallDiffList.razor         # extracted summary list
TradyStrat/Features/Dashboard/Components/CallDiffList.razor.cs
TradyStrat/Features/Dashboard/Components/CallDiffList.razor.css
TradyStrat/Features/Dashboard/CallDiffRow.cs                        # row VM consumed by CallDiffList
```

---

## Task 1 — Goal line aligned to chart corners (Bug 3)

**Files:**
- Modify: `TradyStrat/Features/Dashboard/Components/GrowthChart.razor:18`

The goal line currently goes `M0,225 L1200,8`, missing the chart corners. The viewBox is `0 0 1200 240`; the data-area `value→y` mapping is `220 - value/target * 220`, so €0 → `y=220` and `target` → `y=0`. The line must run those exact corners.

- [ ] **Step 1: Inspect current path**

Run: `grep -n 'class="goal"' TradyStrat/Features/Dashboard/Components/GrowthChart.razor`
Expected output:
```
18:        <path class="goal" d="M0,225 L1200,8" />
```

- [ ] **Step 2: Edit the path**

In `TradyStrat/Features/Dashboard/Components/GrowthChart.razor`, change line 18 from:

```razor
        <path class="goal" d="M0,225 L1200,8" />
```

to:

```razor
        <path class="goal" d="M0,220 L1200,0" />
```

- [ ] **Step 3: Verify visually with Chrome DevTools MCP**

Steps:
1. With dashboard running at `http://127.0.0.1:5180/`, navigate to it via `mcp__plugin_chrome-devtools-mcp_chrome-devtools__navigate_page` (`type: reload`).
2. Resize page to 1440×900.
3. `evaluate_script`:
```javascript
() => document.querySelector('svg.chart .goal').getAttribute('d')
```
Expected: `"M0,220 L1200,0"`.
4. `take_screenshot` of the chart region; visually confirm the dashed gold line passes through the bottom-left and top-right corners of the grid frame (top grid line at `y=40`, bottom grid line at `y=220`).

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/GrowthChart.razor
git commit -m "fix(chart): align goal line to chart corners"
```

---

## Task 2 — Verb dropcap loses the period; rationale gets explicit whitespace (Bug 4)

**Files:**
- Modify: `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.cs:32-39`
- Modify: `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor:22`

The current markup `<span class="verb-drop">@Verb</span>@Sug.Rationale` produces a DOM textContent of `"Hold.Signals are mixed…"` — no whitespace between the floated dropcap and the rationale. Screen readers and copy-paste both render that glued phrase. Additionally the giant trailing period on the dropcap is awkward typography.

- [ ] **Step 1: Drop the period from `Verb`**

In `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.cs:32-39`, replace:

```csharp
    private string Verb => Sug.Action switch
    {
        SuggestionAction.Acquire => "Acquire.",
        SuggestionAction.Hold    => "Hold.",
        SuggestionAction.Trim    => "Trim.",
        SuggestionAction.Wait    => "Wait.",
        _ => "—"
    };
```

with:

```csharp
    private string Verb => Sug.Action switch
    {
        SuggestionAction.Acquire => "Acquire",
        SuggestionAction.Hold    => "Hold",
        SuggestionAction.Trim    => "Trim",
        SuggestionAction.Wait    => "Wait",
        _ => "—"
    };
```

- [ ] **Step 2: Move the period into the prose flow with explicit whitespace**

In `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor:22`, replace:

```razor
        <span class="verb-drop" data-verb="@VerbStem">@Verb</span>@Sug.Rationale
```

with:

```razor
        <span class="verb-drop" data-verb="@VerbStem">@Verb</span><span class="verb-tail">.</span> @Sug.Rationale
```

The trailing period sits in a normal-weight span next to the dropcap so it reads as part of the running prose; the explicit space character before `@Sug.Rationale` fixes the textContent gluing.

- [ ] **Step 3: Add a tiny CSS hook for the tail period**

In `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.css`, after the `.verb-drop` block (around line 70), add:

```css
.verb-tail {
    /* Floats with the dropcap so the period sits at the same baseline
       as the bottom of the giant glyph, not as part of the body prose. */
    float: left;
    font-family: var(--font-display);
    font-style: italic;
    font-weight: 400;
    color: var(--vault-ivory);
    font-size: 86px;
    line-height: 0.86;
    margin: 8px 18px 2px -10px;
    letter-spacing: -0.01em;
}
@media (max-width: 720px) {
    .verb-tail { font-size: 64px; margin-right: 14px; }
}
```

- [ ] **Step 4: Verify in the rendered DOM**

Run via Chrome MCP `evaluate_script`:
```javascript
() => ({
  reasonsText: document.querySelector('.reasons').textContent.trim().slice(0, 30),
  verbText: document.querySelector('.verb-drop').textContent
})
```
Expected:
- `reasonsText` starts with `"Hold. Signals are mixed"` (note the period and space between).
- `verbText === "Hold"` (no period in the dropcap span itself).

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.cs TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.css
git commit -m "fix(call-card): drop period from verb dropcap, add whitespace before rationale"
```

---

## Task 3 — Portfolio rail intermediate breakpoint (Bug 5)

**Files:**
- Modify: `TradyStrat/Features/Dashboard/Components/PortfolioRail.razor.css`

`PortfolioRail.razor.css` only relies on the page-level 980-px breakpoint. Between ~980 and 1180 px the four columns (`1.4fr 1fr 1fr 1fr`) crush the BTC cell so its 28-px display number wraps under its delta and the `≈ €` sub line nudges into the cell border.

- [ ] **Step 1: Read the current `.rail` rule**

Run: `sed -n '1,15p' TradyStrat/Features/Dashboard/Components/PortfolioRail.razor.css`
Expected: shows `.rail { display: grid; grid-template-columns: 1.4fr 1fr 1fr 1fr; … }`.

- [ ] **Step 2: Append the intermediate breakpoint**

Append the following at the end of `TradyStrat/Features/Dashboard/Components/PortfolioRail.razor.css`:

```css
/* Mid-width: position cell goes full-width above three equally-sized
   ticker cells. Avoids 28px display numerals being crushed in narrow
   columns between the page-level 980px breakpoint and ~1180px. */
@media (max-width: 1180px) and (min-width: 981px) {
    .rail {
        grid-template-columns: 1fr 1fr 1fr;
    }
    .rail .cell:first-child {
        grid-column: 1 / -1;
        border-right: none;
        border-bottom: 1px solid var(--vault-rule);
    }
    .val { font-size: 22px; }
}
```

- [ ] **Step 3: Verify at 1024 px viewport**

Via Chrome MCP:
1. `resize_page` to 1024×900.
2. `take_screenshot` (full page).
3. `evaluate_script`:
```javascript
() => {
  const cells = [...document.querySelectorAll('.rail .cell')];
  return cells.map(c => ({
    cls: c.className,
    text: c.textContent.replace(/\s+/g,' ').trim().slice(0,40),
    box: { x: Math.round(c.getBoundingClientRect().x), w: Math.round(c.getBoundingClientRect().width) }
  }));
}
```
Expected: position cell spans the full width (its `w` ≈ rail width); the next three cells share the second row equally (each `w` ≈ rail-width / 3).

4. `resize_page` to 1440×900 → confirm the original four-column layout is intact (position cell width is roughly 1.4 / 4.4 of the rail width).

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/PortfolioRail.razor.css
git commit -m "fix(portfolio-rail): intermediate breakpoint to keep ticker cells legible"
```

---

## Task 4 — Y-axis labels on growth chart (readability)

**Files:**
- Modify: `TradyStrat/Features/Dashboard/Components/GrowthChart.razor`
- Modify: `TradyStrat/Features/Dashboard/Components/GrowthChart.razor.cs`
- Modify: `TradyStrat/Features/Dashboard/Components/GrowthChart.razor.css`

The chart's four horizontal grid lines are unlabelled. Without €0 / quartiles / target anchors the slope of the gold area is hard to interpret. Add four small italic mono labels at the left edge, baseline-aligned with each grid line.

- [ ] **Step 1: Add a `YAxisLabels` model on the code-behind**

Open `TradyStrat/Features/Dashboard/Components/GrowthChart.razor.cs`. After the existing properties (or in the existing computed-properties region) add a method/property that yields the four label rows. Read the current file first to find the right insertion point — search for `private string GoalLabel` (or equivalent) and insert next to it.

Add:

```csharp
    /// <summary>Y-axis labels keyed by their viewBox y coordinate.</summary>
    /// <remarks>
    /// Anchored at the four grid-line y values (40, 100, 160, 220). The values
    /// shown are 100%, 75%, 50%, 25% of <see cref="GoalConfig.TargetEur"/>;
    /// the bottom (€0) is implied by the axis baseline.
    /// </remarks>
    private IReadOnlyList<(double Y, string Text)> YAxisLabels
    {
        get
        {
            var t = Goal.TargetEur;
            return
            [
                (40,  $"€{(t * 0.75m).ToString("N0", FrFr)}"),
                (100, $"€{(t * 0.50m).ToString("N0", FrFr)}"),
                (160, $"€{(t * 0.25m).ToString("N0", FrFr)}"),
            ];
        }
    }
```

(The top grid line at `y=40` is left unlabelled because the existing "€200 000 by Dec 2026 — goal" callout already anchors the top; labelling 75% / 50% / 25% gives three clean visual stops without crowding the area path.)

If `FrFr` and `Goal` aren't already accessible in this code-behind, look up how the existing `GoalLabel`/`AreaPath` properties access them and reuse the same pattern. Do **not** introduce a new culture instance.

- [ ] **Step 2: Render the labels in the SVG**

In `TradyStrat/Features/Dashboard/Components/GrowthChart.razor`, locate the `<g class="grid">` block (lines 12–17). Immediately after that closing `</g>` and before `<path class="goal" …>` insert:

```razor
        <g class="y-axis">
            @foreach (var (y, text) in YAxisLabels)
            {
                <svg:text x="6" y="@((y - 4).ToString("F0", CultureInfo.InvariantCulture))">@text</svg:text>
            }
        </g>
```

The y-coordinate is offset 4 px above the grid line so the text sits *just above* its line rather than overlapping it.

- [ ] **Step 3: Style the y-axis labels**

In `TradyStrat/Features/Dashboard/Components/GrowthChart.razor.css`, after the `.chart .axis text` rule (around line 11) add:

```css
.chart .y-axis text {
    font-family: var(--font-mono);
    font-size: 10px;
    fill: rgba(236,230,214,0.32);
    letter-spacing: 0.08em;
    font-feature-settings: "tnum";
}
```

- [ ] **Step 4: Verify rendering and accessibility**

Via Chrome MCP at 1440×900:
1. `evaluate_script`:
```javascript
() => [...document.querySelectorAll('svg.chart .y-axis text')].map(t => ({
  x: t.getAttribute('x'),
  y: t.getAttribute('y'),
  txt: t.textContent.trim()
}))
```
Expected: three entries with `x="6"`, y-values matching `36`, `96`, `156`, and texts like `"€150 000"`, `"€100 000"`, `"€50 000"` (assuming target = €200 000).

2. `take_screenshot` of the chart and confirm the labels do **not** overlap the gold area path's left edge or the today line.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/GrowthChart.razor TradyStrat/Features/Dashboard/Components/GrowthChart.razor.cs TradyStrat/Features/Dashboard/Components/GrowthChart.razor.css
git commit -m "feat(chart): y-axis labels at 25/50/75% of goal target"
```

---

## Task 5 — Reconcile hero and chart "today" numbers (Bug 1)

**Files:**
- Modify: `TradyStrat/Application/UseCases/Dashboard/LoadDashboardUseCase.cs`
- Test: `TradyStrat.Tests/UseCases/Dashboard/LoadDashboardUseCaseTests.cs`

`PortfolioService.SnapshotAsync` returns `Snap.CurrentValueEur` (in EUR — `shares * focusPriceEur`). `GrowthSeriesBuilder.BuildAsync` returns `GrowthPoint.ValueEur` but actually computes `shares * bar.Close` in raw price units. The chart label hardcodes `€` so the chart "today" silently disagrees with the hero "today" by the FX factor. Truthfully fixing the entire growth series in EUR requires injecting `FxConverter` into `GrowthSeriesBuilder` and walking the EURUSD history per bar — that's a separate refactor (Followup-B). For this plan we **pin the last point** to the snapshot so the two big numbers reconcile at "today".

- [ ] **Step 1: Write the failing test**

Add this test to `TradyStrat.Tests/UseCases/Dashboard/LoadDashboardUseCaseTests.cs` next to the existing `Composes_view_model_with_three_tickers_and_growth_series`:

```csharp
    [Fact]
    public async Task Last_growth_point_matches_hero_current_value_eur()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;

        // CON3.L — 250-bar series so all indicators compute
        foreach (var b in SeriesLoader.LoadCloses("CON3.L")) db.PriceBars.Add(b);
        foreach (var t in new[] { "COIN", "BTC-USD" })
            db.PriceBars.Add(new PriceBar { Id=0, Ticker=t, Date=new(2026,5,6),
                Open=200, High=200, Low=200, Close=200, Volume=1 });
        // EURUSD = 1.08 means EUR ≠ USD numerically, surfacing the bug if not pinned.
        db.FxRates.Add(new FxRate { Id=0, Pair="EURUSD", Date=new(2026,5,6),
            UsdPerEur = 1.08m, FetchedAt = DateTime.UtcNow });
        db.Goals.Add(GoalConfig.Default(DateTime.UtcNow));
        db.Trades.Add(new Trade {
            Id = 0, ExecutedOn = new(2025,12,7), Side = TradeSide.Buy,
            Quantity = 100m, PricePerShare = 4m, FeesEur = 0m, Note = null,
            CreatedAt = DateTime.UtcNow });
        db.Suggestions.Add(new Suggestion {
            Id = 0, ForDate = new(2026,5,6), Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "stable", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);

        var classifier = new ZoneClassifier(new IZoneRule[] {
            new BollingerZoneRule(), new RsiZoneRule(),
            new MovingAverageZoneRule(), new IchimokuZoneRule() });
        var indicators = new IndicatorEngine(new TestRepo<PriceBar>(db), classifier, new IndicatorHistoryProviderFactory([]));
        var portfolio  = new PortfolioService(new TestRepo<Trade>(db));
        var growth     = new GrowthSeriesBuilder(new TestRepo<Trade>(db), new TestRepo<PriceBar>(db));
        var fx         = new FxConverter(new TestRepo<FxRate>(db));
        var clock      = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));

        var snapStub = new StubSnapshotFactory(new AiSnapshot(
            new(2026,5,6), GoalConfig.Default(DateTime.UtcNow),
            new(0,0,0,0,0,0), [], [], 1.08m, "h"));
        var aiStub = new StubAiClient(new Suggestion {
            Id = 0, ForDate = new(2026,5,6), Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "x", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });
        var todays = new GetTodaysSuggestionUseCase(
            new TestRepo<Suggestion>(db), snapStub, aiStub, clock,
            NullLogger<GetTodaysSuggestionUseCase>.Instance);

        var uc = new LoadDashboardUseCase(
            indicators, portfolio, growth, fx,
            new TestRepo<GoalConfig>(db),
            new TestRepo<Trade>(db),
            new TestRepo<PriceBar>(db),
            new TestRepo<Suggestion>(db),
            new TestRepo<FxRate>(db),
            todays,
            new NullCoordinator(),
            clock,
            NullLogger<LoadDashboardUseCase>.Instance);

        var vm = await uc.ExecuteAsync(Unit.Value, ct);

        vm.Growth.Count.ShouldBeGreaterThan(0);
        var lastPoint = vm.Growth[^1];
        lastPoint.ValueEur.ShouldBe(vm.Portfolio.CurrentValueEur);
        lastPoint.Date.ShouldBe(vm.Today);
    }
```

- [ ] **Step 2: Run the test and confirm it fails**

Run:
```bash
dotnet test TradyStrat.Tests/TradyStrat.Tests.csproj --filter "FullyQualifiedName~LoadDashboardUseCaseTests.Last_growth_point_matches_hero_current_value_eur"
```
Expected: 1 failed test. Failure message: `lastPoint.ValueEur.ShouldBe(vm.Portfolio.CurrentValueEur)` — last point is in raw price units, hero is in EUR.

- [ ] **Step 3: Pin the last growth point in `LoadDashboardUseCase`**

Open `TradyStrat/Application/UseCases/Dashboard/LoadDashboardUseCase.cs`. Locate line 67:

```csharp
        var growthSeries = await growth.BuildAsync(FocusTicker, ct);
```

Replace the immediately-following block (between line 67 and line 68's `var todays = …`) with:

```csharp
        var growthSeries = await growth.BuildAsync(FocusTicker, ct);

        // The growth series is computed from raw bar.Close values (no FX).
        // Pin its trailing point to the hero's EUR-valued snapshot so the chart's
        // "today" label and the big hero number agree on the same page.
        // Followup-B: thread FxConverter through GrowthSeriesBuilder for a fully
        // EUR-correct curve across all dates.
        if (growthSeries.Count > 0)
        {
            var pinned = growthSeries.ToList();
            pinned[^1] = pinned[^1] with
            {
                Date = today,
                ValueEur = snap.CurrentValueEur
            };
            growthSeries = pinned;
        }
```

- [ ] **Step 4: Run the new test — it should now pass**

Run:
```bash
dotnet test TradyStrat.Tests/TradyStrat.Tests.csproj --filter "FullyQualifiedName~LoadDashboardUseCaseTests.Last_growth_point_matches_hero_current_value_eur"
```
Expected: 1 passing test.

- [ ] **Step 5: Run the full Dashboard test class to confirm no regressions**

Run:
```bash
dotnet test TradyStrat.Tests/TradyStrat.Tests.csproj --filter "FullyQualifiedName~LoadDashboardUseCaseTests"
```
Expected: all tests in `LoadDashboardUseCaseTests` pass.

- [ ] **Step 6: Verify in the browser**

Via Chrome MCP at 1440×900, after a hot-reload:
```javascript
() => {
  const heroNum = document.querySelector('.hero .num').textContent.replace(/\s+/g,' ').trim();
  const chartLabels = [...document.querySelectorAll('svg.chart text')].map(t => t.textContent.trim());
  const todayLabel = chartLabels.find(t => t.includes('today')) || null;
  return { heroNum, todayLabel };
}
```
Expected: `todayLabel` includes the same numeric value as `heroNum` (e.g. both `"30 182"`).

- [ ] **Step 7: Commit**

```bash
git add TradyStrat/Application/UseCases/Dashboard/LoadDashboardUseCase.cs TradyStrat.Tests/UseCases/Dashboard/LoadDashboardUseCaseTests.cs
git commit -m "fix(dashboard): pin last growth point to hero's EUR snapshot so today's number agrees"
```

---

## Task 6 — Structured `CallDiff` summary, replacing the wall of italic text (readability)

**Files:**
- Create: `TradyStrat/Features/Dashboard/CallDiffRow.cs`
- Create: `TradyStrat/Features/Dashboard/Components/CallDiffList.razor`
- Create: `TradyStrat/Features/Dashboard/Components/CallDiffList.razor.cs`
- Create: `TradyStrat/Features/Dashboard/Components/CallDiffList.razor.css`
- Modify: `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor`
- Modify: `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.css`
- Test: `TradyStrat.Tests/Dashboard/CallDiffViewModelTests.cs`

The current "summary-bar" mashes every diff item into one paragraph at 12.5 px italic — at 1440 px desktop it's a wall of text with `·` separators. `CallDiff` already exposes the structured fields we need (`ChangedCitations`, `AddedCitationKeys`, `RemovedCitationKeys`, `ActionChanged`, `ConvictionDelta`); render them as a tight list instead.

- [ ] **Step 1: Define the row VM and projection helper**

Create `TradyStrat/Features/Dashboard/CallDiffRow.cs`:

```csharp
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Dashboard;

/// <summary>One row of the today's-call diff list.</summary>
/// <param name="Kind">"changed", "added", "removed" — drives the side glyph.</param>
/// <param name="Indicator">e.g. "200-SMA" / "RSI(14)" / "Ichimoku" / "Zone".</param>
/// <param name="Ticker">e.g. "CON3.L".</param>
/// <param name="Detail">Free-form delta text, e.g. "Below 200-SMA → Above" or empty.</param>
public sealed record CallDiffRow(string Kind, string Indicator, string Ticker, string Detail);

public static class CallDiffRowProjector
{
    public static IReadOnlyList<CallDiffRow> Project(CallDiff diff)
    {
        var rows = new List<CallDiffRow>(
            diff.ChangedCitations.Count + diff.AddedCitationKeys.Count + diff.RemovedCitationKeys.Count);

        foreach (var c in diff.ChangedCitations)
            rows.Add(new CallDiffRow("changed", c.Indicator, c.Ticker,
                $"{c.PriorClaim} → {c.CurrentClaim}"));

        foreach (var key in diff.AddedCitationKeys)
        {
            var (ind, tk) = SplitKey(key);
            rows.Add(new CallDiffRow("added", ind, tk, ""));
        }

        foreach (var key in diff.RemovedCitationKeys)
        {
            var (ind, tk) = SplitKey(key);
            rows.Add(new CallDiffRow("removed", ind, tk, ""));
        }

        return rows;
    }

    private static (string Indicator, string Ticker) SplitKey(string key)
    {
        var idx = key.IndexOf(':');
        return idx < 0 ? (key, "") : (key[..idx], key[(idx + 1)..]);
    }
}
```

> **Note on `CitationChange` field names.** The projection above uses `c.Indicator`, `c.Ticker`, `c.PriorClaim`, `c.CurrentClaim`. Before continuing, **read** `TradyStrat/Features/AiSuggestion/CitationChange.cs` and confirm those property names match. If the actual fields are different (for example `Old`/`New`, or `Before`/`After`), update the projection to use the real names — keep the same display semantics ("from → to"). **Do not** assume; verify.

- [ ] **Step 2: Add a unit test for the projector**

Create `TradyStrat.Tests/Dashboard/CallDiffViewModelTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Features.Dashboard;
using TradyStrat.Shared.Domain;
using Xunit;

namespace TradyStrat.Tests.Dashboard;

public class CallDiffViewModelTests
{
    [Fact]
    public void Project_emits_changed_added_and_removed_rows()
    {
        // Arrange — one of each row kind, then verify ordering and field projection.
        // The CitationChange field names below must match the real record; if they
        // differ in the codebase, update the constructor call accordingly.
        var diff = new CallDiff(
            ActionChanged: false,
            PriorAction: null,
            ConvictionDelta: null,
            AddedCitationKeys: ["RSI(14):BTC-USD"],
            RemovedCitationKeys: ["Bollinger:BTC-USD"],
            ChangedCitations: [new CitationChange(
                Indicator: "200-SMA", Ticker: "COIN",
                PriorClaim: "Below 200-SMA (260.87)",
                CurrentClaim: "Below 200-SMA (255.10)")],
            SummaryParagraph: "ignored");

        var rows = CallDiffRowProjector.Project(diff);

        rows.Count.ShouldBe(3);
        rows[0].Kind.ShouldBe("changed");
        rows[0].Indicator.ShouldBe("200-SMA");
        rows[0].Ticker.ShouldBe("COIN");
        rows[0].Detail.ShouldContain("→");
        rows[1].Kind.ShouldBe("added");
        rows[1].Indicator.ShouldBe("RSI(14)");
        rows[1].Ticker.ShouldBe("BTC-USD");
        rows[2].Kind.ShouldBe("removed");
        rows[2].Indicator.ShouldBe("Bollinger");
        rows[2].Ticker.ShouldBe("BTC-USD");
    }

    [Fact]
    public void Project_returns_empty_for_None()
    {
        var rows = CallDiffRowProjector.Project(CallDiff.None);
        rows.ShouldBeEmpty();
    }
}
```

- [ ] **Step 3: Run the test — should fail (compile error: missing types)**

Run:
```bash
dotnet test TradyStrat.Tests/TradyStrat.Tests.csproj --filter "FullyQualifiedName~CallDiffViewModelTests"
```
Expected: build error referencing `CallDiffRow` / `CallDiffRowProjector` if Step 1 hasn't compiled, otherwise an assertion failure.

- [ ] **Step 4: Run the test — should pass after Step 1's file is in place**

Run:
```bash
dotnet test TradyStrat.Tests/TradyStrat.Tests.csproj --filter "FullyQualifiedName~CallDiffViewModelTests"
```
Expected: 2 passing tests. If `CitationChange` has different field names, fix the test fixture and projection together until both compile and pass.

- [ ] **Step 5: Create the `CallDiffList` component**

Create `TradyStrat/Features/Dashboard/Components/CallDiffList.razor`:

```razor
@using TradyStrat.Features.Dashboard
@using TradyStrat.Features.AiSuggestion

@if (Rows.Count > 0)
{
    <ul class="cdl">
        @foreach (var row in Rows)
        {
            <li class="cdl-row" data-kind="@row.Kind">
                <span class="cdl-glyph" aria-hidden="true">@GlyphFor(row.Kind)</span>
                <span class="cdl-name"><b>@row.Indicator</b><span class="cdl-tk">@row.Ticker</span></span>
                @if (!string.IsNullOrEmpty(row.Detail))
                {
                    <span class="cdl-detail">@row.Detail</span>
                }
                else
                {
                    <span class="cdl-detail cdl-keyword">@row.Kind</span>
                }
            </li>
        }
    </ul>
}
```

Create `TradyStrat/Features/Dashboard/Components/CallDiffList.razor.cs`:

```csharp
using Microsoft.AspNetCore.Components;
using TradyStrat.Features.Dashboard;

namespace TradyStrat.Features.Dashboard.Components;

public partial class CallDiffList : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<CallDiffRow> Rows { get; set; } = [];

    private static string GlyphFor(string kind) => kind switch
    {
        "added"   => "+",
        "removed" => "−",
        _         => "Δ"
    };
}
```

Create `TradyStrat/Features/Dashboard/Components/CallDiffList.razor.css`:

```css
.cdl {
    list-style: none;
    margin: 0 0 18px 0;
    padding: 10px 14px 4px;
    background: linear-gradient(
        90deg,
        rgba(196,154,86,0.10) 0%,
        rgba(196,154,86,0.03) 80%,
        transparent 100%);
    border-left: 2px solid rgba(196,154,86,0.55);
    max-width: 60ch;
}
.cdl-row {
    display: grid;
    grid-template-columns: 14px auto 1fr;
    align-items: baseline;
    gap: 10px;
    padding: 4px 0;
    font-family: var(--font-mono);
    font-size: 11.5px;
    letter-spacing: 0.04em;
    color: rgba(236,230,214,0.82);
    border-bottom: 1px dotted rgba(196,154,86,0.10);
}
.cdl-row:last-child { border-bottom: none; }

.cdl-glyph {
    font-family: var(--font-display);
    font-style: italic;
    font-size: 13px;
    text-align: center;
    color: var(--vault-gold);
}
.cdl-row[data-kind="removed"] .cdl-glyph { color: rgba(214,120,120,0.7); }
.cdl-row[data-kind="added"]   .cdl-glyph { color: var(--vault-green); }

.cdl-name b {
    color: var(--vault-ivory);
    font-weight: 500;
    font-variant: small-caps;
    letter-spacing: 0.07em;
    font-size: 12.5px;
    margin-right: 6px;
}
.cdl-tk {
    font-size: 9px;
    letter-spacing: 0.18em;
    color: rgba(236,230,214,0.45);
    text-transform: uppercase;
}
.cdl-detail {
    color: rgba(236,230,214,0.7);
    font-size: 11.5px;
    line-height: 1.45;
}
.cdl-keyword {
    text-transform: uppercase;
    font-size: 9px;
    letter-spacing: 0.22em;
    color: rgba(196,154,86,0.65);
}
```

- [ ] **Step 6: Wire `CallDiffList` into `TodaysCallCard.razor` and remove the old `summary-bar`**

In `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor`, replace the existing block:

```razor
    @if (HasDiff)
    {
        <div class="summary-bar">
            @CallDiff.SummaryParagraph
        </div>
    }
```

with:

```razor
    @if (HasDiff)
    {
        <CallDiffList Rows="CallDiffRowProjector.Project(CallDiff)" />
    }
```

Add the using at the top of the file (next to existing `@using` directives):

```razor
@using TradyStrat.Features.Dashboard
```

- [ ] **Step 7: Drop the now-unused `.summary-bar` CSS**

In `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.css`, delete the `.summary-bar` rule (lines 15–28 in the current file). Leave `.backfill-pill` alone.

- [ ] **Step 8: Verify in browser**

Via Chrome MCP at 1440×900:
```javascript
() => {
  const list = document.querySelector('.cdl');
  return {
    rendered: !!list,
    rowCount: list ? list.querySelectorAll('.cdl-row').length : 0,
    kinds: list ? [...list.querySelectorAll('.cdl-row')].map(r => r.dataset.kind) : []
  };
}
```
Expected: `rendered: true`, `rowCount` matches `CallDiff.ChangedCitations.Count + AddedCitationKeys.Count + RemovedCitationKeys.Count`, and `kinds` contains the expected mix of `"changed"`, `"added"`, `"removed"`.

Also confirm visually that the long italic paragraph above the dropcap has been replaced by a tight bordered list, and that the dropcap rationale below is unaffected.

- [ ] **Step 9: Commit**

```bash
git add TradyStrat/Features/Dashboard/CallDiffRow.cs TradyStrat/Features/Dashboard/Components/CallDiffList.razor TradyStrat/Features/Dashboard/Components/CallDiffList.razor.cs TradyStrat/Features/Dashboard/Components/CallDiffList.razor.css TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.css TradyStrat.Tests/Dashboard/CallDiffViewModelTests.cs
git commit -m "feat(call-card): structured diff list replaces concatenated summary paragraph"
```

---

## Task 7 — Final cross-task verification

Once all five fixes are in, do a single end-to-end sweep — these checks belong here rather than per-task because they catch interactions.

**Files:** none modified.

- [ ] **Step 1: Run the full test suite**

Run:
```bash
dotnet test TradyStrat.Tests/TradyStrat.Tests.csproj
```
Expected: all tests pass (existing + new ones from Task 5 and Task 6).

- [ ] **Step 2: Three-viewport visual sweep with Chrome MCP**

With dashboard reloaded:

1. `resize_page` 1440×900 → `take_screenshot` (full page) → confirm:
   - Hero number and chart "today" label show the same value.
   - Goal line passes through bottom-left and top-right corners of the grid.
   - Y-axis labels (€50 000 / €100 000 / €150 000) sit above their grid lines, no overlap.
   - Today's-call shows the new structured `.cdl` list (no `.summary-bar`).
   - Dropcap reads `Hold` (no period); rationale starts `". Signals…"` visually flowing from the dropcap.

2. `resize_page` 1024×900 → `take_screenshot` → confirm:
   - Position cell is full-width above three equal ticker cells.
   - Ticker prices fit on one line with the delta beside them.

3. `resize_page` 500×900 → `take_screenshot` → confirm:
   - Hero collapses to single column; rail collapses to one column; chart still visible.
   - Dropcap shrinks per existing 720-px breakpoint.

- [ ] **Step 3: DOM accessibility spot-check**

```javascript
() => ({
  reasonsTextStart: document.querySelector('.reasons').textContent.trim().slice(0, 30),
  cdlExists: !!document.querySelector('.cdl'),
  goalPath: document.querySelector('svg.chart .goal').getAttribute('d'),
  yLabels: [...document.querySelectorAll('svg.chart .y-axis text')].map(t => t.textContent.trim())
})
```
Expected:
- `reasonsTextStart` begins `"Hold. Signals are mixed"` (period + space, properly tokenised).
- `cdlExists: true`.
- `goalPath: "M0,220 L1200,0"`.
- `yLabels` length is 3.

- [ ] **Step 4: Final commit (only if anything changed)**

If the verification surfaced a tweak, commit it. Otherwise no-op.

```bash
git status
# only commit if changes exist
```

---

## Followup-A — CON3.L currency labelling (Bug 2 deferred)

Outside this plan; create when prioritised.

**Why deferred:** truthful display requires schema-touching work — `LoadDashboardUseCase.cs:40` declares CON3.L as `"USD"` but Yahoo serves the LSE listing in GBp; `FxConverter` and `YahooFxProvider` only know `EURUSD`; correctly converting the rail's `≈ €` line and the hero's `CurrentValueEur` for CON3.L needs GBPEUR (or EURGBP) coverage end-to-end.

**Sketch of the future plan:**
1. Extend `YahooFxProvider.FetchAsync` symbol mapping with `"GBPEUR" => "GBPEUR=X"`. Add a parallel seed/refresh path in `RefreshAllPricesUseCase` and `PriceFeedHostedService`.
2. Add `FxConverter.GbpPenceToEurAsync(decimal pence, DateOnly asOf, …)` that fetches the `GBPEUR` rate and divides pence by 100 before applying.
3. Change the catalog entry in `LoadDashboardUseCase.cs:40` from `(FocusTicker, "USD")` to `(FocusTicker, "GBp")`; route GBp tickers through `GbpPenceToEurAsync` instead of `UsdToEurAsync`.
4. Extend `PortfolioRail.razor.cs:FormatPrimary` with a `"GBp" => "Xp"` branch and a defensive fallback `_ => $"{ccy} {price.ToString("N2", FrFr)}"`.

## Followup-B — Fully EUR-correct growth curve

The pin in Task 5 reconciles only the trailing point. For a curve that's EUR across all dates, inject `FxConverter` into `GrowthSeriesBuilder.BuildAsync`, look up the per-bar `EURUSD` (or `GBPEUR` if Followup-A is in) and multiply each point. Update the existing `GrowthSeriesBuilderTests.Builds_one_point_per_day_with_running_value` fixture to assert EUR-converted values.

---

## Self-review

- **Spec coverage:** Bug 1 → Task 5; Bug 3 → Task 1; Bug 4 → Task 2; Bug 5 → Task 3; chart-Y-axis readability → Task 4; `CallDiff` wall-of-text readability → Task 6; final verification → Task 7. Bug 2 explicitly deferred to Followup-A with reason and sketch.
- **Placeholder scan:** No `TBD` / `…implement…` / "similar to Task N" — every step shows the actual code, command, or expected output. The one `Note` in Task 6 (about `CitationChange` field names) is a verification instruction, not a placeholder; the test step will catch a mismatch immediately.
- **Type consistency:** `Verb` (string, no period) vs `VerbStem` (lowercase, unchanged); `CallDiffRow` fields used identically in `CallDiffRowProjector`, the test, the component, and the CSS data attributes; `Rows` parameter on `CallDiffList` matches projector return type. `growthSeries` rebound to `IReadOnlyList<GrowthPoint>` via `ToList()` (works since `List<T>` implements `IReadOnlyList<T>`).
