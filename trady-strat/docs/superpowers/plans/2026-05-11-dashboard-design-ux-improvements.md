# TradyStrat Dashboard Design & UX Improvements — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tighten the Vault dashboard's design and UX — fix the broken mobile layout, keep the headline capital figure on screen while scrolling, re-weight the hero around the "impossible goal" story, de-noise the Polymarket section, reconcile the overlapping Positions/Holdings sections, surface a TL;DR on the AI call, clarify masthead jargon, polish the Trades and Settings pages, standardise number formatting and the verb palette, and fix heading semantics.

**Architecture:** Blazor Server (`.NET 10`). UI lives in `TradyStrat/Features/**` as `.razor` markup with co-located `.razor.css` (scoped) and a `.razor.cs` code-behind for logic (Passive View — keep logic out of `@code`/markup; the only components without a code-behind are the trivially-simple `PositionsTable` and `EventFootnoteRail`, and that's fine). Global styles: `TradyStrat/wwwroot/css/vault.css` (design tokens) + `settings-forms.css`. JS interop modules live in `TradyStrat/wwwroot/js/` and are loaded as **ES modules via `JS.InvokeAsync<IJSObjectReference>("import", "./js/…")`** from a component's `OnAfterRenderAsync`, disposed in `DisposeAsync` (see `DashboardPage.razor.cs` → `dashboard-keys.js`, `GrowthChart.razor.cs` → `growth-chart.js`). The work is split into **11 task groups (0–10)**, each producing a working, mergeable change on its own; execute in order — Group 0 (verb palette) and Group 1 (number formatting) introduce shared helpers that later groups use.

**Tech Stack:** Blazor Server (InteractiveServer, prerender on), C# 13 / .NET 10, scoped CSS (`::deep`), xUnit v3 + Shouldly, Ardalis.Specification (already used for EF queries), Chrome DevTools MCP for visual verification, ES modules for JS interop.

**Verification note:** Most of this is CSS/markup; the "test" for visual work is: `dotnet run --project TradyStrat`, open `http://127.0.0.1:5180/` in Chrome DevTools MCP (`new_page` / `resize_page` / `take_screenshot` / `take_snapshot`), and check against the acceptance criteria in each task. Logic-bearing changes (Group 0 verb helper, Group 1 `NumberFormat`, Group 2 heading roles) get real unit tests. Before claiming any group complete: `dotnet build` (zero new warnings) and `dotnet test` (suite must stay green — 213+ tests).

> ⚠ **Codebase facts the executor must rely on (already verified):**
> - `PortfolioSnapshot` record: `Positions (IReadOnlyList<PositionRow>)`, `CurrentValueEur`, `CostBasisEur`, `UnrealizedPnLEur`, `RealizedPnLEur`, `ProgressPct`, `Shares`, `AvgCostEur` (the last two are legacy single-position scalars — prefer `CostBasisEur`/`Positions`).
> - `PositionRow`: `Ticker`, `Quantity`, `CostBasisEur`, `MarketValueEur`, `UnrealizedPnLEur` (the exact members the existing `PositionsTable` already binds).
> - `TickerView` (the element type of `PortfolioRail.Tickers`): `Ticker`, `Currency` ("EUR"/"USD"/…), `Price`, `DeltaPct?`, `PriceEur?`, `Spark`, `TodaysCall` — **no `IsHeld`**.
> - Prediction-market types (`TradyStrat.Features.PredictionMarkets`): `MarketSnapshot(IReadOnlyList<PredictionMarket> Markets, IReadOnlyList<MarketCitation> Cited)` with `Empty`; `PredictionMarket(string Slug, string Question, decimal Probability /*0..1*/, DateOnly EndDate, decimal VolumeUsd, IReadOnlyList<string> Tags)`; `MarketCitation(string Slug, string Claim)`.
> - `MarketsRail.razor.cs` **already exists** (`partial class MarketsRail : ComponentBase`, `[Parameter] MarketSnapshot Snapshot`, `_bySlug = Snapshot.Cited.ToDictionary(c => c.Slug)` in `OnParametersSet`, `FrFr` field).
> - `SuggestionAction` enum: `Acquire`, `Hold`, `Trim`, `Wait`. `Suggestion.Citations` is `IReadOnlyList<Citation>`; `Citation` has `Indicator`, `Ticker`, `Claim`, `Value`. `TodaysCallCard.razor.cs` has `Verb`/`VerbStem` switches over `Sug?.Action`.
> - `Trade` record: `Id`, `InstrumentId`, `ExecutedOn`, `Side (TradeSide)`, `Quantity`, `PricePerShare`, `FeesEur`, `Note?`, `CreatedAt`; helpers `GrossEur`, `NetEur` (`Buy ? Gross+Fees : Gross−Fees`), `IsBuy`. `TradesPage.razor.cs` loads `_trades` via `AllTradesSpec` which orders **`ExecutedOn` then `Id` (oldest-first)**; it has a `.razor.cs` code-behind (`StartEdit`, `DeleteAsync`, `Reload`, `FrFr`, `FormatQty`, …).
> - `GoalPaceVm`: `Mode (GoalPaceMode: Active|TargetReached|GoalDatePassed)`, `VsPlanEur`, `MonthlyCompoundPct`, `ImpliedCagrPct`. `HeroCapital.razor.cs`: `Pct => Snap.ProgressPct`, `CurrentPct => Clamp01(Snap.CurrentValueEur/Goal100*100)`, `Goal100`, `Clamp01`, `Fmt`, `CostBasisEur => Snap.Shares*Snap.AvgCostEur`, `FormatSigned`, `FormatVsPlan`, `AriaSummary` (calls `FormatSigned` ×2), `DaysLeft`.
> - `Routes.razor` has `<FocusOnNavigate RouteData="routeData" Selector="h1" />` — **no page currently has an `<h1>`** (Group 2 fixes this).
> - `App.razor` loads CSS via `<link>` and `blazor.web.js` via `<script>`; component JS is **not** `<script>`-tagged, it's interop-imported.

---

## File Structure

**New files**
- `TradyStrat/Common/Domain/SuggestionActionDisplay.cs` — single source of truth for `SuggestionAction → verb string / css stem` (Group 0).
- `TradyStrat.Tests/Common/Domain/SuggestionActionDisplayTests.cs` (Group 0).
- `TradyStrat/Common/Formatting/NumberFormat.cs` + `TradyStrat.Tests/Common/Formatting/NumberFormatTests.cs` (Group 1).
- `TradyStrat/Features/Dashboard/Components/StickyCapitalBar.razor` + `.razor.css` + `.razor.cs` (Group 4).
- `TradyStrat/wwwroot/js/sticky-bar.js` — ES module exporting `observeHero(barEl, heroEl)` / `disconnect()` (Group 4).

**Modified files** — per group; exact targets in each task. Cross-cutting: `wwwroot/css/vault.css` gets the `--verb-color-*` tokens (G0) and `h2.section-heading` (G2).

---

# Task Group 0 — Verb palette: single source of truth

**Why:** `SuggestionAction → verb word` lives in `TodaysCallCard.razor.cs` (`Verb`/`VerbStem`); `SuggestionAction → colour` lives in `TodaysCallCard.razor.css` (`.verb-drop[data-verb=…]`) and `PortfolioRail.razor.css` (`.rail-call .action.hold|trim|…`). Group 4 (`StickyCapitalBar`) and Group 7 would add a third colour copy. Consolidate now: one C# helper for the strings, CSS custom properties for the colours.

### Task 0.1: `SuggestionActionDisplay` + tests

**Files:** Create `TradyStrat/Common/Domain/SuggestionActionDisplay.cs`; Test `TradyStrat.Tests/Common/Domain/SuggestionActionDisplayTests.cs`.

- [ ] **Step 1: Write the failing tests**

```csharp
using Shouldly;
using TradyStrat.Common.Domain;
using Xunit;

namespace TradyStrat.Tests.Common.Domain;

public class SuggestionActionDisplayTests
{
    [Theory]
    [InlineData(SuggestionAction.Acquire, "Acquire", "acquire")]
    [InlineData(SuggestionAction.Hold,    "Hold",    "hold")]
    [InlineData(SuggestionAction.Trim,    "Trim",    "trim")]
    [InlineData(SuggestionAction.Wait,    "Wait",    "wait")]
    public void KnownActions(SuggestionAction a, string verb, string stem)
    {
        SuggestionActionDisplay.Verb(a).ShouldBe(verb);
        SuggestionActionDisplay.Stem(a).ShouldBe(stem);
    }

    [Fact]
    public void NullAction_FallsBackToDash()
    {
        SuggestionActionDisplay.Verb(null).ShouldBe("—");
        SuggestionActionDisplay.Stem(null).ShouldBe("none");
    }
}
```

- [ ] **Step 2: Run, confirm fail** — `dotnet test --filter "FullyQualifiedName~SuggestionActionDisplayTests"` → FAIL (type missing).

- [ ] **Step 3: Implement**

```csharp
namespace TradyStrat.Common.Domain;

/// <summary>Single source of truth for how a SuggestionAction is presented:
/// the display verb ("Hold") and the lowercase CSS stem ("hold") used by
/// [data-verb] selectors and --verb-color-* tokens.</summary>
public static class SuggestionActionDisplay
{
    public static string Verb(SuggestionAction? action) => action switch
    {
        SuggestionAction.Acquire => "Acquire",
        SuggestionAction.Hold    => "Hold",
        SuggestionAction.Trim    => "Trim",
        SuggestionAction.Wait    => "Wait",
        _ => "—",
    };

    public static string Stem(SuggestionAction? action) => action switch
    {
        SuggestionAction.Acquire => "acquire",
        SuggestionAction.Hold    => "hold",
        SuggestionAction.Trim    => "trim",
        SuggestionAction.Wait    => "wait",
        _ => "none",
    };
}
```

- [ ] **Step 4: Run, confirm pass.**

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Common/Domain/SuggestionActionDisplay.cs TradyStrat.Tests/Common/Domain/SuggestionActionDisplayTests.cs
git commit -m "feat(domain): add SuggestionActionDisplay (verb word + css stem)"
```

### Task 0.2: Point `TodaysCallCard` at the helper; add `--verb-color-*` tokens; repoint the CSS

**Files:** `TodaysCallCard.razor.cs`, `wwwroot/css/vault.css`, `TodaysCallCard.razor.css`, `PortfolioRail.razor.css`.

- [ ] **Step 1: `TodaysCallCard.razor.cs`** — replace the bodies of the `Verb` / `VerbStem` properties with `SuggestionActionDisplay.Verb(Sug?.Action)` / `SuggestionActionDisplay.Stem(Sug?.Action)` (add `using TradyStrat.Common.Domain;` — likely already present). Delete the now-duplicated `switch` expressions.

- [ ] **Step 2: `wwwroot/css/vault.css`** — add inside `:root` (after `--vault-red`):

```css
  /* Verb palette — referenced by TodaysCallCard, PortfolioRail, StickyCapitalBar.
     One place to change "trim is amber, not red". */
  --verb-color-hold:    var(--vault-ivory);
  --verb-color-acquire: var(--vault-green);
  --verb-color-trim:    var(--vault-red);
  --verb-color-wait:    rgba(236, 230, 214, 0.6);
```

- [ ] **Step 3: `TodaysCallCard.razor.css`** — replace the per-verb colour declarations on `.verb-drop[data-verb=…]` and `.verb-tail[data-verb=…]` so each references the token, e.g. `.verb-drop[data-verb="hold"]{ color: var(--verb-color-hold); }` … `[data-verb="trim"]{ color: var(--verb-color-trim); }` … `[data-verb="wait"]{ color: var(--verb-color-wait); font-style: italic; }` … `[data-verb="acquire"]{ color: var(--verb-color-acquire); }`. Keep the `acquire` font-size overrides as-is. (Net visual change: zero — the tokens hold the same colours.)

- [ ] **Step 4: `PortfolioRail.razor.css`** — repoint `.rail-call .action.acquire{ color: var(--verb-color-acquire); border-color: var(--verb-color-acquire); }`, `.action.hold{ color: var(--ink-3); }` (unchanged — "hold" in the rail chip is intentionally `ink-3`, not the headline ivory; leave it), `.action.trim{ color: var(--verb-color-trim); border-color: var(--verb-color-trim); }` (was `#c8a857` amber — **deliberate change to red** to match the rest of the app's "trim" colour; if you'd rather keep amber, add `--verb-color-trim-rail` — but unifying is the point), `.action.wait{ color: var(--ink-3); }` (unchanged).

- [ ] **Step 5: Build + visual check + tests** — `dotnet build` (no warnings), `dotnet test` (green). Open `/`: section III drop-cap `Hold.` unchanged colour; section VII `.rail-call` chip — `HOLD` still `ink-3`; if the focus ticker's call were `Trim` it'd be red (can't easily force; accept). 

- [ ] **Step 6: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.cs TradyStrat/wwwroot/css/vault.css TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.css TradyStrat/Features/Dashboard/Components/PortfolioRail.razor.css
git commit -m "refactor(verbs): centralise verb word + colour palette"
```

---

# Task Group 1 — Consolidate number formatting

**Why:** `€31 911`, `€-46 048`, `−€46 048`, `63650,00` (no thousands sep), `63 650 sh` coexist. One helper, one convention.

**Decision (locked):** `NumberFormat` builds its **own deterministic `NumberFormatInfo`** (don't trust ICU's `fr-FR`, whose group separator is ` ` and varies by ICU version): `NumberGroupSeparator = " "` (NO-BREAK SPACE — renders as a space, never breaks), `NumberDecimalSeparator = ","`, `NumberGroupSizes = [3]`. Money: euro amounts get **0 decimals when the value is whole *or* ≥ €1 000**, else 2 (`€31 911`, `€1 000`, `€999,50`, `€0,59`, `€0`). Signed money: real minus `−` (U+2212) or `+`, then `€`, no space: `−€46 048` / `+€19 685` / `+€0`. Quantity: thousands-separated, trailing-zero-trimmed (`63 650`, `63 650,5`). Percent: 1 decimal, narrow no-break space ` ` before `%` (`16,0 %`). Price: always 2 decimals, currency symbol prefix (`$203,04`, `€1,22`).

### Task 1.1: `NumberFormat` + tests

**Files:** Create `TradyStrat/Common/Formatting/NumberFormat.cs`; Test `TradyStrat.Tests/Common/Formatting/NumberFormatTests.cs`.

- [ ] **Step 1: Write the failing tests** (literals use explicit ` ` / ` ` escapes so copy-paste is unambiguous):

```csharp
using Shouldly;
using TradyStrat.Common.Formatting;
using Xunit;

namespace TradyStrat.Tests.Common.Formatting;

public class NumberFormatTests
{
    [Fact] public void Eur_Large_NoDecimals()        => NumberFormat.Eur(31911.42m).ShouldBe("€31 911");
    [Fact] public void Eur_WholeUnderThousand()      => NumberFormat.Eur(42m).ShouldBe("€42");
    [Fact] public void Eur_Zero()                    => NumberFormat.Eur(0m).ShouldBe("€0");
    [Fact] public void Eur_FractionUnderThousand()   => NumberFormat.Eur(0.59m).ShouldBe("€0,59");
    [Fact] public void Eur_FractionJustUnder()       => NumberFormat.Eur(999.5m).ShouldBe("€999,50");
    [Fact] public void Eur_WholeThousand()           => NumberFormat.Eur(1000m).ShouldBe("€1 000");

    [Fact] public void EurBody_StripsSymbol()        => NumberFormat.EurBody(31911m).ShouldBe("31 911");

    [Fact] public void SignedEur_Negative_RealMinus()=> NumberFormat.SignedEur(-46048m).ShouldBe("−€46 048");
    [Fact] public void SignedEur_Positive()          => NumberFormat.SignedEur(19685m).ShouldBe("+€19 685");
    [Fact] public void SignedEur_Zero()              => NumberFormat.SignedEur(0m).ShouldBe("+€0");

    [Fact] public void Qty_Whole()                   => NumberFormat.Qty(63650m).ShouldBe("63 650");
    [Fact] public void Qty_Fractional_TrimsZeros()   => NumberFormat.Qty(63650.5m).ShouldBe("63 650,5");

    [Fact] public void Pct_OneDecimal_NarrowSpace()  => NumberFormat.Pct(16.04m).ShouldBe("16,0 %");

    [Fact] public void Price_TwoDecimals_Prefix()    => NumberFormat.Price(203.04m, "$").ShouldBe("$203,04");
    [Fact] public void Price_GroupedAbove1000()      => NumberFormat.Price(1203.04m, "€").ShouldBe("€1 203,04");
}
```

- [ ] **Step 2: Run, confirm fail** — `dotnet test --filter "FullyQualifiedName~NumberFormatTests"` → FAIL (type missing).

- [ ] **Step 3: Implement**

```csharp
using System.Globalization;

namespace TradyStrat.Common.Formatting;

/// <summary>Single source of truth for displaying money, quantities and percentages.
/// Uses an explicit NumberFormatInfo (NO-BREAK SPACE group separator, comma decimal)
/// so output is deterministic regardless of ICU version. Euro amounts show 0 decimals
/// when whole or ≥ 1 000, else 2. Signed amounts: real minus (U+2212), then €, no space.
/// Percent: 1 decimal + narrow no-break space before %.</summary>
public static class NumberFormat
{
    private const char Minus = '−';        // MINUS SIGN
    private const char NarrowNbsp = ' ';   // NARROW NO-BREAK SPACE
    private const decimal NoDecimalsThreshold = 1000m;

    private static readonly NumberFormatInfo Nfi = new()
    {
        NumberGroupSeparator   = " ",      // NO-BREAK SPACE
        NumberDecimalSeparator = ",",
        NumberGroupSizes       = [3],
    };

    private static int DecimalsFor(decimal amount)
        => (amount == decimal.Truncate(amount) || Math.Abs(amount) >= NoDecimalsThreshold) ? 0 : 2;

    public static string EurBody(decimal amount)
        => amount.ToString("N" + DecimalsFor(amount), Nfi);

    public static string Eur(decimal amount)
        => "€" + EurBody(amount);

    public static string SignedEur(decimal amount)
    {
        var sign = amount < 0 ? Minus.ToString() : "+";
        var abs = Math.Abs(amount);
        return sign + "€" + abs.ToString("N" + DecimalsFor(abs), Nfi);
    }

    public static string Qty(decimal quantity)
        => quantity == decimal.Truncate(quantity)
            ? quantity.ToString("N0", Nfi)
            : quantity.ToString("#,##0.################", Nfi);

    public static string Pct(decimal value)
        => value.ToString("0.0", Nfi) + NarrowNbsp + "%";

    public static string Price(decimal value, string currencySymbol)
        => currencySymbol + value.ToString("N2", Nfi);
}
```

- [ ] **Step 4: Run, confirm pass** (15 tests).

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Common/Formatting/NumberFormat.cs TradyStrat.Tests/Common/Formatting/NumberFormatTests.cs
git commit -m "feat(format): add NumberFormat helper for consistent money/qty/pct display"
```

### Task 1.2: Route `HeroCapital` through `NumberFormat`

**Files:** `HeroCapital.razor`, `HeroCapital.razor.cs`.

- [ ] **Step 1: `HeroCapital.razor`** — add `@using TradyStrat.Common.Formatting`. Replacements:
  - `.amount` block: keep the gold `€` glyph markup; change `<span class="num">@Snap.CurrentValueEur.ToString("N0", FrFr)</span>` → `<span class="num">@NumberFormat.EurBody(Snap.CurrentValueEur)</span>`; change `— of €@Goal.TargetEur.ToString("N0", FrFr) —` → `— of €@NumberFormat.EurBody(Goal.TargetEur) —`.
  - `Own capital` `<dd>`: `€@CostBasisEur.ToString("N0", FrFr)` → `€@NumberFormat.EurBody(CostBasisEur)`.
  - Realized / Unrealized `<dd>`: `@FormatSigned(Snap.RealizedPnLEur)` → `@NumberFormat.SignedEur(Snap.RealizedPnLEur)`; same for `UnrealizedPnLEur`.
  - `.pct`: `@Pct.ToString("F1", FrFr) %` → `@NumberFormat.Pct(Pct)`.
  - `vs. plan` value: `@FormatVsPlan(GoalPace.VsPlanEur)` → `@NumberFormat.SignedEur(GoalPace.VsPlanEur)`.
  - `monthly`: `@GoalPace.MonthlyCompoundPct.ToString("F1", FrFr) % / mo` → `@NumberFormat.Pct(GoalPace.MonthlyCompoundPct) / mo`.
  - `CAGR`: `@GoalPace.ImpliedCagrPct.ToString("F0", FrFr) %` → `@(GoalPace.ImpliedCagrPct.ToString("N0", FrFr) + " %")` (CAGR: 0 decimals, keep inline — `FrFr` still in scope).
  - `GoalDatePassed` branch: `€@GoalPace.VsPlanEur.ToString("N0", FrFr)` → `@NumberFormat.Eur(GoalPace.VsPlanEur)`.

- [ ] **Step 2: `HeroCapital.razor.cs`** —
  - `AriaSummary` calls `FormatSigned` ×2 and uses `FrFr`. Rewrite it to use `NumberFormat`: `unrealized {NumberFormat.SignedEur(Snap.UnrealizedPnLEur)}, realized {NumberFormat.SignedEur(Snap.RealizedPnLEur)}` and `Progress {NumberFormat.Pct(Pct)} — own capital {NumberFormat.Eur(CostBasisEur)}`.
  - Now delete `FormatSigned` and `FormatVsPlan` (unused after the above).
  - Keep `FrFr` — still used by `DaysLeft` and the CAGR inline format above.
  - Add `using TradyStrat.Common.Formatting;`.
  - (Optional, free correctness win since you're here: change `CostBasisEur => Snap.Shares * Snap.AvgCostEur` to `=> Snap.CostBasisEur` — the latter is the real multi-position field; the former is only valid for single-position snapshots. If you do, leave a one-line note in the commit.)

- [ ] **Step 3: Build + visual check** — `dotnet build` (no warnings). Open `/`, screenshot section II. Acceptance: `€31 911`, `— of €200 000 —`, `OWN CAPITAL €77 959`, `UNREALIZED −€46 048` (real minus), `16,0 %`, `VS. PLAN +€19 685`, `MONTHLY 9,6 % / mo`, `CAGR 206 %`; alarm-tint still works.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/HeroCapital.razor TradyStrat/Features/Dashboard/Components/HeroCapital.razor.cs
git commit -m "refactor(hero): route money/pct display through NumberFormat"
```

### Task 1.3: Route `PositionsTable`, `PortfolioRail`, `TradesPage` through `NumberFormat`

**Files:** `PositionsTable.razor`, `PortfolioRail.razor`, `PortfolioRail.razor.cs`, `TradesPage.razor`, `TradesPage.razor.cs`.

- [ ] **Step 1: `PositionsTable.razor`** — add `@using TradyStrat.Common.Formatting`. `@r.Quantity.ToString("F2", FrFr)` → `@NumberFormat.Qty(r.Quantity)`; `€@r.CostBasisEur.ToString("N0", FrFr)` → `@NumberFormat.Eur(r.CostBasisEur)`; same for `MarketValueEur`; `@FormatSigned(r.UnrealizedPnLEur)` → `@NumberFormat.SignedEur(r.UnrealizedPnLEur)`. Total row: `€@Rows.Sum(...).ToString("N0", FrFr)` → `@NumberFormat.Eur(Rows.Sum(...))`; `@FormatSigned(TotalUnreal)` → `@NumberFormat.SignedEur(TotalUnreal)`. In the `@code` block delete `FormatSigned` and `FrFr` (now unused).

- [ ] **Step 2: `PortfolioRail`** —
  - `PortfolioRail.razor`: add `@using TradyStrat.Common.Formatting`. `@Snap.Shares.ToString("N0", FrFr)` → `@NumberFormat.Qty(Snap.Shares)`. `avg €@Snap.AvgCostEur.ToString("F2", FrFr)` → `avg @NumberFormat.Price(Snap.AvgCostEur, "€")`. `≈ €@eur.ToString("N2", FrFr)` → `≈ @NumberFormat.Price(eur, "€")`.
  - `PortfolioRail.razor.cs`: `FormatPrimary(TickerView t)` — replace the body with `NumberFormat.Price(t.Price, t.Currency == "EUR" ? "€" : t.Currency == "USD" ? "$" : "")` (preserve the "unknown currency ⇒ no symbol" behaviour). `PnL(...)` — replace `pct.ToString("F1", FrFr) %` with `NumberFormat.Pct(pct)` and drop the manual `+` (NumberFormat doesn't sign percentages — but `PnL` *wants* a sign; so instead keep `PnL` returning `(pct >= 0 ? "+" : NumberFormat… )` — simplest: `var s = NumberFormat.Pct(Math.Abs(pct)); return (pct < 0 ? "−" : "+") + s;` using real minus). `FormatDelta` — see Task 7.2 (it changes there); for now leave it.
  - Keep `FrFr` in `PortfolioRail.razor.cs` if `TruncateRationale`/anything else still needs it (it doesn't after the above — but `OnParametersSet` etc. don't; check `grep FrFr PortfolioRail.razor.cs` and remove if zero hits).

- [ ] **Step 3: `TradesPage`** — `TradesPage.razor`: add `@using TradyStrat.Common.Formatting`. `@FormatQty(t.Quantity)` → `@NumberFormat.Qty(t.Quantity)`; `€@t.PricePerShare.ToString("F2", FrFr)` → `@NumberFormat.Price(t.PricePerShare, "€")`; `€@t.FeesEur.ToString("F2", FrFr)` → `@NumberFormat.Price(t.FeesEur, "€")`. `TradesPage.razor.cs`: delete the local `FormatQty` (now unused — `NumberFormat.Qty` has equivalent trailing-zero-trim behaviour). Keep `FrFr` (still used elsewhere in the code-behind — Group 9 adds a use; check & keep).

- [ ] **Step 4: Build + full test suite + visual check** — `dotnet build` (no warnings), `dotnet test` (green). Open `/` + `/trades`; screenshot Positions, Holdings, ledger. Acceptance: Positions `QTY 63 650`, `COST BASIS €77 959`, `UNREALISED PNL −€46 048`; Holdings `63 650 sh · avg €1,22`, `$0,59`, `≈ €0,50`; Trades `€1,65`, `€0,00`. No `63650,00` anywhere.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/PositionsTable.razor TradyStrat/Features/Dashboard/Components/PortfolioRail.razor TradyStrat/Features/Dashboard/Components/PortfolioRail.razor.cs TradyStrat/Features/Trades/TradesPage.razor TradyStrat/Features/Trades/TradesPage.razor.cs
git commit -m "refactor: route Positions/Holdings/Trades money display through NumberFormat"
```

---

# Task Group 2 — Heading semantics: real `<h2>` per section + an `<h1>`

**Why:** Six sections use a styled `<div>` (one is `<h3>`) — SR users can't navigate by section, hierarchy is incoherent, and `Routes.razor`'s `<FocusOnNavigate Selector="h1">` matches nothing because no page has an `<h1>`. Fix: a shared `h2.section-heading` style (also the future home for the 7 near-duplicate label rules), and an `<h1>` on the masthead wordmark.

### Task 2.1: Add `h2.section-heading` to `vault.css` + make the wordmark an `<h1>`

**Files:** `wwwroot/css/vault.css`, `VaultMasthead.razor`, `VaultMasthead.razor.css`.

- [ ] **Step 1: `vault.css`** — append after `.section-roman`:

```css
/* Section heading — semantic <h2> styled like the legacy .label/.rail-label divs.
   Components keep their own positioning wrapper class; this owns the typography. */
h2.section-heading {
    font-family: var(--font-mono);
    font-size: 11px;
    font-weight: 500;
    letter-spacing: var(--tracking-xs);
    text-transform: uppercase;
    color: var(--vault-gold);
    margin: 0;
    display: flex;
    align-items: baseline;
}
```

- [ ] **Step 2: `VaultMasthead.razor`** — change `<div class="brand"><a href="/">Tradystrat</a></div>` to `<h1 class="brand"><a href="/">Tradystrat</a></h1>`. (The wordmark is the page-level `<h1>` on every page that renders the masthead — Dashboard, Trades, Settings. `FocusOnNavigate` will give it `tabindex="-1"` and focus it after navigation.)

- [ ] **Step 3: `VaultMasthead.razor.css`** — the `.brand` rule already sets font/colour; add `margin: 0;` to it (neutralise `<h1>` UA margins). Keep `justify-self: start` etc.

- [ ] **Step 4: Build + verify** — `dotnet build`. Open `/`: wordmark looks identical; `take_snapshot` shows `heading "TRADYSTRAT" level=1`. Tab through after a route change — focus lands on the wordmark, no visible focus ring jump that looks wrong (it's `tabindex=-1`, so only programmatic focus; fine).

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/wwwroot/css/vault.css TradyStrat/Features/Dashboard/Components/VaultMasthead.razor TradyStrat/Features/Dashboard/Components/VaultMasthead.razor.css
git commit -m "a11y: add page <h1> (wordmark) + shared .section-heading rule"
```

### Task 2.2: Convert each section label to `<h2 class="section-heading">`

**Files:** `GrowthChart.razor`, `HeroCapital.razor`, `TodaysCallCard.razor`, `MarketsRail.razor`, `CitationsBlock.razor` (+`.css`), `PositionsTable.razor` (+`.css`), `PortfolioRail.razor`. For each: replace the label element with `<h2 class="section-heading">…<span class="section-roman">N.</span>… </h2>` keeping the inner `.section-roman` span; keep the component's existing positioning wrapper class **only if** it supplies needed margin/padding (e.g. `margin-bottom`), in which case keep the class but ensure its rule no longer fights `.section-heading` (delete font/colour/letter-spacing from it; keep only margins).

- [ ] **Step 1: `GrowthChart.razor`** — `<div class="lbl"><span class="section-roman">I.</span>Capital growth · trajectory toward goal</div>` → `<h2 class="lbl section-heading"><span class="section-roman">I.</span>Capital growth · trajectory toward goal</h2>`. In `GrowthChart.razor.css` strip the font/colour/letter-spacing/text-transform/display from `.lbl`, leaving `.lbl { margin: 0 0 18px; }`.

- [ ] **Step 2: `HeroCapital.razor`** — `<div class="label">…II.…</div>` → `<h2 class="label section-heading">…</h2>`. `HeroCapital.razor.css`: reduce `.label` to `.label { margin: 0 0 14px; display: flex; align-items: baseline; }` (the `display:flex` here is harmless; or drop it since `.section-heading` already sets it).

- [ ] **Step 3: `TodaysCallCard.razor`** — `<div class="label">…III.…<span class="freshness">…</span></div>` → `<h2 class="label section-heading">…</h2>`. `TodaysCallCard.razor.css`: `.label { margin: 0 0 12px; display: flex; align-items: baseline; }` (the `.freshness` span inside is fine in a flex heading).

- [ ] **Step 4: `MarketsRail.razor`** — `<div class="rail-label"><span class="section-roman">IV.</span>Polymarket · @Snapshot.Markets.Count markets</div>` → `<h2 class="rail-label section-heading"><span class="section-roman">IV.</span>Polymarket · @Snapshot.Markets.Count markets</h2>`. `MarketsRail.razor.css`: `.rail-label { margin: 0 0 18px; }` (drop the 10px/0.32em — accept unification to 11px/0.28em; if a quick look says it reads worse, restore `font-size:10px; letter-spacing:0.32em;` on `.rail-label` only — but prefer unifying).

- [ ] **Step 5: `CitationsBlock.razor`** — `.cit-head` is `display:flex; justify-content:space-between`. Change `<span class="cit-label"><span class="section-roman">V.</span>Cited evidence · …</span>` → `<h2 class="cit-label section-heading"><span class="section-roman">V.</span>Cited evidence · …</h2>` (still inside `<header class="cit-head">`, still flex-item). `CitationsBlock.razor.css`: `.cit-label { margin: 0; }` (drop everything else — `.section-heading` covers it; accept the 10px→11px unification).

- [ ] **Step 6: `PositionsTable.razor`** — `<h3><span class="section-roman">VI.</span>Positions</h3>` → `<h2 class="section-heading"><span class="section-roman">VI.</span>Positions</h2>`. `PositionsTable.razor.css`: change `.positions h3 { … }` → `.positions h2 { margin: 0 0 14px; }` (drop the duplicated typography).

- [ ] **Step 7: `PortfolioRail.razor`** — inside `<header class="rail-head">`, `<span class="rail-label"><span class="section-roman">VII.</span>Holdings</span>` → `<h2 class="rail-label section-heading"><span class="section-roman">VII.</span>Holdings</h2>`. `PortfolioRail.razor.css`: `.rail-label { margin: 0; }` (drop the rest). The `.rail-head { padding: 22px 56px 0; margin-bottom: -2px; }` wrapper stays.

- [ ] **Step 8: `EventFootnoteRail.razor`** — no heading; it's a continuation of section I (`<section aria-label="Capital event footnotes">` is correct). No change.

- [ ] **Step 9: Build + verify (visual + a11y)** — `dotnet build` (no warnings). Open `/`: `take_screenshot` full page — every section label looks the same as before (or, for the 4 sections that were 10px/0.32em, marginally larger/tighter — acceptable; if any looks wrong restore a 1-line override). `take_snapshot` — sections I–VII are all `heading … level=2`; the wordmark is `level=1`.

- [ ] **Step 10: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/GrowthChart.razor TradyStrat/Features/Dashboard/Components/GrowthChart.razor.css TradyStrat/Features/Dashboard/Components/HeroCapital.razor TradyStrat/Features/Dashboard/Components/HeroCapital.razor.css TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.css TradyStrat/Features/Dashboard/Components/MarketsRail.razor TradyStrat/Features/Dashboard/Components/MarketsRail.razor.css TradyStrat/Features/Dashboard/Components/CitationsBlock.razor TradyStrat/Features/Dashboard/Components/CitationsBlock.razor.css TradyStrat/Features/Dashboard/Components/PositionsTable.razor TradyStrat/Features/Dashboard/Components/PositionsTable.razor.css TradyStrat/Features/Dashboard/Components/PortfolioRail.razor TradyStrat/Features/Dashboard/Components/PortfolioRail.razor.css
git commit -m "a11y(dashboard): make section labels semantic <h2> headings"
```

---

# Task Group 3 — Responsive / mobile pass

**Why:** ≤430px: nav clips ("SETTINGS" off-screen), the travel ribbon overflows, the growth chart is an unreadable sliver, hero numerals collide with page padding. Add ≤860px (tablet) and ≤560px (phone) treatments.

### Task 3.1: Page chrome scales down

**Files:** `wwwroot/css/vault.css`.

- [ ] **Step 1** — append:

```css
@media (max-width: 860px) {
  main { border-left: none; border-right: none; }
}
@media (max-width: 560px) {
  main::before { background:
    radial-gradient(circle at 100% -10%, rgba(196,154,86,0.08), transparent 55%); }
}
```

- [ ] **Step 2: Build + verify** at 390×844 and 1440×900 (no desktop regression).
- [ ] **Step 3: Commit** — `git commit -m "style(vault): drop side borders, soften glow on narrow viewports"`.

### Task 3.2: Masthead reflow

**Files:** `VaultMasthead.razor.css`.

- [ ] **Step 1** — append:

```css
@media (max-width: 860px) {
  .masthead { grid-template-columns: 1fr; gap: 14px; padding: 18px 24px 14px; }
  .brand { justify-self: start; }
  .nav { justify-self: start; flex-wrap: wrap; gap: 18px 22px; }
  .head-right { justify-self: start; flex-wrap: wrap; gap: 10px; }
}
@media (max-width: 560px) {
  /* Ribbon cells share borders so they must stay 28px; let the trailing meta
     ("journal entry 0004 · 14h ago") wrap onto its own row inside the box. */
  .masthead .travel-ribbon { flex-wrap: wrap; height: auto; }
  .masthead .ribbon-step, .masthead .ribbon-date { height: 28px; }
  .masthead .ribbon-date { border-right: 1px solid var(--vault-rule); }
  .masthead .ribbon-meta {
    flex-basis: 100%;
    border-top: 1px solid var(--vault-rule);
    padding: 6px 12px;
    justify-content: flex-start;
  }
}
```

- [ ] **Step 2: Build + verify** at 390px: wordmark on its own line; all three nav links visible (wrapping if needed); `‹ 11·05·2026 ›` intact with `journal entry 0004 · 14h ago` on a second row inside the ribbon; nothing clipped right. Re-check `/trades` + `/settings` (masthead renders `ShowNav=false` → just `.meta`, already wraps).
- [ ] **Step 3: Commit** — `git commit -m "style(masthead): reflow nav + travel ribbon on narrow viewports"`.

### Task 3.3: Chart + section paddings on phones

**Files:** `GrowthChart.razor.css`, `HeroCapital.razor.css`, `TodaysCallCard.razor.css`. (`MarketsRail.razor.css` / `PositionsTable.razor.css` already have ≤700/720px rules — leave them.)

- [ ] **Step 1: `GrowthChart.razor.css`** — append:

```css
@media (max-width: 720px) {
  .chart-wrap { padding: 18px 20px 26px; }
  .chart { height: 280px; }
  .chart-text .goal-label { font-size: 12px; }
  .chart-text .y-label, .chart-text .x-axis { font-size: 10px; }
  .navi-presets { font-size: 9px; }
  .navi-presets button { padding: 4px 8px; }
  .navi-track { height: 30px; }
  .navi-bg { height: 30px; }
}
@media (max-width: 480px) {
  .chart-text .y-label { display: none; }  /* overlaps the line at this width; hero restates the today figure */
}
```

- [ ] **Step 2: `HeroCapital.razor.css`** — append:

```css
@media (max-width: 720px) {
  .hero { padding: 24px 20px 22px; }
  .amount { font-size: clamp(44px, 13vw, 64px); }
  .progress { gap: 10px; }
  .pace-line-row { gap: 6px 12px; }
}
```

- [ ] **Step 3: `TodaysCallCard.razor.css`** — add `@media (max-width: 720px){ .call{ padding: 22px 20px 24px; } }` (the verb-drop already has a 720px rule).

- [ ] **Step 4: Build + full sweep at 390px** — scroll top→bottom; every section fits with ~20px gutters; chart legible; no `<body>` horizontal scrollbar.
- [ ] **Step 5: Commit** — `git commit -m "style(dashboard): tighten section paddings + chart sizing on phones"`.

---

# Task Group 4 — Sticky capital strip

**Why:** Once past the hero you lose `€31 911 / 16% / Hold`. Add a slim bar that fades in after the hero scrolls fully above the fold; clicking it scrolls to top.

**Decision (locked):** Separate component rendered once in `DashboardPage.razor` (inside the loaded branch, before `.dash-stage`). `position: fixed`, hidden by default (`opacity:0; pointer-events:none; transform: translateY(-100%)`), revealed via a `.visible` class. The class is toggled by an **ES module** (`sticky-bar.js`) using `IntersectionObserver` on `.hero-row`; the module is imported and invoked from `DashboardPage.razor.cs`'s `OnAfterRenderAsync` **whenever `_vm` is loaded** (so it survives the loading→loaded transition and the retry-after-error path), and disconnected in `DisposeAsync`. This mirrors how `dashboard-keys.js` is wired. The component takes the data slice as typed params plus the `SuggestionAction?` (not a pre-stringified verb) and resolves the verb via `SuggestionActionDisplay`.

### Task 4.1: `StickyCapitalBar` component (markup + code-behind + scoped css)

**Files:** Create `StickyCapitalBar.razor`, `StickyCapitalBar.razor.cs`, `StickyCapitalBar.razor.css`.

- [ ] **Step 1: `StickyCapitalBar.razor`**

```razor
@using TradyStrat.Common.Formatting

<a class="sticky-cap" href="#" data-sticky-cap title="Back to top">
    <span class="sc-brand">Tradystrat</span>
    <span class="sc-cap"><span class="sc-euro">€</span>@NumberFormat.EurBody(CurrentValueEur)</span>
    <span class="sc-pct">@NumberFormat.Pct(ProgressPct) of @NumberFormat.Eur(TargetEur)</span>
    @if (Action is { } a)
    {
        <span class="sc-verb sc-verb-@Stem">@Verb</span>
    }
</a>
```

- [ ] **Step 2: `StickyCapitalBar.razor.cs`**

```csharp
using Microsoft.AspNetCore.Components;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Dashboard.Components;

public partial class StickyCapitalBar : ComponentBase
{
    [Parameter, EditorRequired] public decimal CurrentValueEur { get; set; }
    [Parameter, EditorRequired] public decimal TargetEur { get; set; }
    [Parameter, EditorRequired] public decimal ProgressPct { get; set; }
    [Parameter] public SuggestionAction? Action { get; set; }

    private string Verb => SuggestionActionDisplay.Verb(Action);
    private string Stem => SuggestionActionDisplay.Stem(Action);
}
```

- [ ] **Step 3: `StickyCapitalBar.razor.css`**

```css
.sticky-cap {
    position: fixed; top: 0; left: 0; right: 0; z-index: 50;
    display: flex; align-items: baseline; gap: 22px;
    padding: 10px max(24px, calc((100vw - 1280px) / 2 + 56px));
    background: rgba(14,13,10,0.92);
    border-bottom: 1px solid var(--vault-rule);
    backdrop-filter: blur(8px);
    color: var(--vault-ivory); text-decoration: none;
    transform: translateY(-100%); opacity: 0; pointer-events: none;
    transition: transform 240ms ease, opacity 240ms ease;
}
.sticky-cap.visible { transform: translateY(0); opacity: 1; pointer-events: auto; }
.sc-brand { font-family: var(--font-display); text-transform: uppercase;
    letter-spacing: var(--tracking-xs); font-size: 12px; color: var(--vault-gold); }
.sc-cap { font-family: var(--font-display); font-weight: 300; font-size: 22px; letter-spacing: -0.02em; }
.sc-cap .sc-euro { color: var(--vault-gold); font-size: 0.7em; margin-right: 3px; }
.sc-pct { font-family: var(--font-mono); font-size: 10px; letter-spacing: 0.16em;
    text-transform: uppercase; color: var(--ink-3); }
.sc-verb { margin-left: auto; font-family: var(--font-display); font-style: italic; font-size: 16px; }
.sc-verb-hold    { color: var(--verb-color-hold); }
.sc-verb-trim    { color: var(--verb-color-trim); }
.sc-verb-wait    { color: var(--verb-color-wait); }
.sc-verb-acquire { color: var(--verb-color-acquire); }
@media (max-width: 560px) {
    .sticky-cap { gap: 12px; padding: 8px 16px; }
    .sc-pct { display: none; }
    .sc-cap { font-size: 18px; }
}
@media (prefers-reduced-motion: reduce) { .sticky-cap { transition: opacity 240ms ease; transform: none; } }
```

- [ ] **Step 4: Build (component compiles, not wired)** — `dotnet build`.
- [ ] **Step 5: Commit** — `git commit -m "feat(dashboard): add StickyCapitalBar component (not yet wired)"`.

### Task 4.2: `sticky-bar.js` ES module + interop wiring

**Files:** Create `wwwroot/js/sticky-bar.js`; modify `DashboardPage.razor` and `DashboardPage.razor.cs`.

- [ ] **Step 1: `wwwroot/js/sticky-bar.js`**

```js
// Toggles `.visible` on the [data-sticky-cap] bar once the dashboard hero
// (.hero-row) has scrolled fully above the fold; clicking the bar scrolls to top.
let observer = null;
let clickHandler = null;

export function observeHero() {
  disconnect();
  const bar = document.querySelector('[data-sticky-cap]');
  const hero = document.querySelector('.hero-row');
  if (!bar || !hero) return;

  observer = new IntersectionObserver(
    (entries) => {
      for (const e of entries) {
        bar.classList.toggle('visible', !e.isIntersecting && e.boundingClientRect.top < 0);
      }
    },
    { rootMargin: '0px 0px -100% 0px', threshold: 0 }
  );
  observer.observe(hero);

  clickHandler = (ev) => { ev.preventDefault(); window.scrollTo({ top: 0, behavior: 'smooth' }); };
  bar.addEventListener('click', clickHandler);
}

export function disconnect() {
  if (observer) { observer.disconnect(); observer = null; }
  const bar = document.querySelector('[data-sticky-cap]');
  if (bar && clickHandler) bar.removeEventListener('click', clickHandler);
  clickHandler = null;
}
```

- [ ] **Step 2: `DashboardPage.razor`** — inside the loaded branch (the `else` that renders `<div class="dash-stage">`), add **before** `<div class="dash-stage">`:

```razor
        <StickyCapitalBar CurrentValueEur="vm.Portfolio.CurrentValueEur"
                          TargetEur="vm.Goal.TargetEur"
                          ProgressPct="vm.Portfolio.ProgressPct"
                          Action="vm.TodaysCall?.Action" />
```

  (`vm.Portfolio` is the `PortfolioSnapshot` already passed to `HeroCapital`; `vm.Goal.TargetEur` is what `HeroCapital` reads as `Goal.TargetEur`; `vm.TodaysCall` is the `Suggestion?` passed to `TodaysCallCard` — `.Action` is the `SuggestionAction`, and `?.Action` yields `SuggestionAction?` for the nullable case. Confirm `DashboardViewModel` exposes `Portfolio`, `Goal`, `TodaysCall` with these names — `DashboardPage.razor` already uses `vm.Portfolio`, `vm.Goal`, `vm.TodaysCall`.)

- [ ] **Step 3: `DashboardPage.razor.cs`** — it already imports `dashboard-keys.js` in `OnAfterRenderAsync(firstRender)`. Add a second module field `private IJSObjectReference? _stickyModule;` and, in `OnAfterRenderAsync` (every render, not just first — because the hero appears after the async load), after rendering when `_vm is not null`:

```csharp
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _keysModule = await JS.InvokeAsync<IJSObjectReference>("import", "./js/dashboard-keys.js");
            // … existing first-render setup …
        }

        if (_vm is not null)
        {
            _stickyModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./js/sticky-bar.js");
            await _stickyModule.InvokeVoidAsync("observeHero");
        }
    }
```

  In the existing `DisposeAsync`, before disposing `_keysModule`, add:

```csharp
        if (_stickyModule is not null)
        {
            try { await _stickyModule.InvokeVoidAsync("disconnect"); } catch (JSDisconnectedException) { }
            await _stickyModule.DisposeAsync();
        }
```

  (`observeHero` is idempotent — it `disconnect()`s first — so calling it again on a later render after a re-load is safe and re-attaches to the fresh `.hero-row`.)

- [ ] **Step 4: Build + verify** — `dotnet build` (no warnings). Open `/` at 1440px: scroll past the hero → bar fades in: `Tradystrat €31 911 16,0 % of €200 000 Hold`. Scroll up → fades out. Click mid-scroll → smooth-scrolls to top. At 390px: `.sc-pct` hidden, layout tidy. Navigate `/` → `/trades` → `/` → bar still works (re-imported/re-observed on the remount's `OnAfterRenderAsync`). Force the error→Retry path (rename the DB momentarily, or just trust the code path) — after Retry the bar still attaches. Toggle `prefers-reduced-motion` → no transform jump. Confirm no console errors (`list_console_messages`).
- [ ] **Step 5: Commit** — `git commit -m "feat(dashboard): reveal sticky capital strip after hero scrolls out"`.

---

# Task Group 5 — Hero re-weighting + plan tick on the progress bar

**Why:** The "impossible goal" story (`9,6 % / mo · CAGR 206 %`) is the easily-missed last line; the progress bar shows 16% filled with no marker for where the *plan* says you should be. (a) Promote the pace line to a hairlined stratum right under the amount; (b) draw a gold "plan" tick on the bar.

### Task 5.1: Promote the pace line above the progress bar

**Files:** `HeroCapital.razor`, `HeroCapital.razor.css`.

- [ ] **Step 1: `HeroCapital.razor`** — move the whole `@if (GoalPace.Mode == GoalPaceMode.Active){ <div class="pace-line-row">…</div> } else if(TargetReached){…} else if(GoalDatePassed){…}` block so it sits **immediately after `</div>` of `.amount` and before `<div class="progress">`**. Change the `Active` branch's outer div to `<div class="pace-line-row pace-line-top">`.

- [ ] **Step 2: `HeroCapital.razor.css`** — add:

```css
.pace-line-row.pace-line-top {
    margin-top: 18px;
    padding: 12px 0;
    border-top: 1px solid rgba(196,154,86,0.18);
    border-bottom: 1px solid rgba(196,154,86,0.18);
}
```

- [ ] **Step 3: Build + verify** — section II order is now: heading → `€31 911 — of €200 000 — by 31·12·2027 · 599 days left` → hairlined `VS. PLAN +€19 685 · MONTHLY 9,6 % / mo · CAGR 206 %` → progress bar → legend. Alarm-tint still works.
- [ ] **Step 4: Commit** — `git commit -m "design(hero): promote the pace line above the progress bar"`.

### Task 5.2: Plan tick on the progress bar

**Files:** `HeroCapital.razor`, `HeroCapital.razor.cs`, `HeroCapital.razor.css`.

- [ ] **Step 1: `HeroCapital.razor.cs`** — add (uses the same `Goal100`/`Clamp01` as `CurrentPct`, so the two ticks share a basis):

```csharp
// Where the required-CAGR plan says you should be today, as % of goal.
// Returns -1 when not applicable (no live pace) ⇒ caller skips rendering.
private decimal PlanPct =>
    GoalPace.Mode == GoalPaceMode.Active
        ? Clamp01((Snap.CurrentValueEur - GoalPace.VsPlanEur) / Goal100 * 100m)
        : -1m;
```

- [ ] **Step 2: `HeroCapital.razor`** — inside `<div class="bar" …>`, immediately after `<span class="tick now" …>`:

```razor
            @if (PlanPct >= 0m)
            {
                <span class="tick plan" style="--l:@Fmt(PlanPct)%" title="plan target today"></span>
            }
```

- [ ] **Step 3: `HeroCapital.razor.css`** — after `.tick.now { … }`:

```css
.tick.plan {
    position: absolute; left: var(--l); top: -4px; bottom: -4px;
    width: 1px; background: var(--vault-gold);
    box-shadow: 0 0 5px rgba(196,154,86,0.6); opacity: 0.85;
    transition: left 600ms ease-out;
}
.tick.plan::after {  /* tiny caret above the bar so the two ticks read distinctly */
    content: ""; position: absolute; left: -2px; top: -5px;
    border-left: 3px solid transparent; border-right: 3px solid transparent;
    border-top: 4px solid var(--vault-gold);
}
```

- [ ] **Step 4: Build + verify** — the ivory `now` hairline sits right of a gold `plan` hairline-with-caret (you're +€19 685 ahead). At 720px both ticks render. If `PlanPct` and `CurrentPct` are within ~2% they visually merge — acceptable.
- [ ] **Step 5: Commit** — `git commit -m "design(hero): mark the plan-target position on the progress bar"`.

---

# Task Group 6 — Polymarket: lead with cited markets, collapse the rate-cut ladder

**Why:** 8 near-identical tiles, 7 of them "Will N Fed rate cuts happen in 2026?" mostly 0–1%. Lead with the AI-cited markets; collapse the ladder into one distribution row; demote any leftover.

**Decision (locked):** Band 1 = markets in `Snapshot.Cited` (full tiles). Band 2 = ladder buckets (`"Will N Fed rate cuts happen in 2026?"` / `"… N or more …"`) as one inline row showing buckets with prob ≥ 0.5% plus always the highest bucket. Band 3 = any non-cited, non-ladder markets as smaller tiles. Render only the non-empty bands. Detection lives in the existing `MarketsRail.razor.cs`.

### Task 6.1: Grouping + ladder detection in `MarketsRail.razor.cs` (**modify** — file exists)

**Files:** Modify `TradyStrat/Features/Dashboard/Components/MarketsRail.razor.cs`.

- [ ] **Step 1: add to the existing `partial class MarketsRail`** (don't redeclare `Snapshot`/`_bySlug`/`FrFr`/`OnParametersSet`):

```csharp
using System.Text.RegularExpressions;
// … existing usings …

public partial class MarketsRail : ComponentBase
{
    // … existing members …

    private static readonly Regex LadderRe = new(
        @"^Will\s+(\d+)(\s+or\s+more)?\s+Fed\s+rate\s+cuts?\s+happen\s+in\s+2026\??$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    protected readonly record struct LadderBucket(int Cuts, bool OrMore, decimal Probability);

    protected sealed record GroupedMarkets(
        IReadOnlyList<PredictionMarket> Cited,
        IReadOnlyList<LadderBucket> Ladder,
        IReadOnlyList<PredictionMarket> Other);

    protected GroupedMarkets Group()
    {
        var cited = new List<PredictionMarket>();
        var ladder = new List<LadderBucket>();
        var other = new List<PredictionMarket>();
        foreach (var m in Snapshot.Markets)
        {
            if (_bySlug.ContainsKey(m.Slug)) { cited.Add(m); continue; }
            var match = LadderRe.Match(m.Question);
            if (match.Success)
            {
                ladder.Add(new LadderBucket(
                    int.Parse(match.Groups[1].Value),
                    match.Groups[2].Success,
                    m.Probability));
                continue;
            }
            other.Add(m);
        }
        ladder.Sort((a, b) => a.Cuts.CompareTo(b.Cuts));
        return new GroupedMarkets(cited, ladder, other);
    }

    // Show buckets ≥ 0.5% plus always the last (highest) bucket — by index, not reference.
    protected static IEnumerable<int> VisibleBucketIndices(IReadOnlyList<LadderBucket> all)
    {
        for (var i = 0; i < all.Count; i++)
            if (all[i].Probability >= 0.005m || i == all.Count - 1) yield return i;
    }
}
```

- [ ] **Step 2: Build** — `dotnet build` (no warnings; note the new `System.Text.RegularExpressions` using).
- [ ] **Step 3: Commit** — `git commit -m "feat(markets): add rate-cut ladder detection + market grouping"`.

### Task 6.2: Three-band layout

**Files:** `MarketsRail.razor`, `MarketsRail.razor.css`.

- [ ] **Step 1: rewrite the body of `MarketsRail.razor`** (keep the `@using TradyStrat.Features.PredictionMarkets`, the `@if (Snapshot.Markets.Count > 0)` guard, and the `<h2 class="rail-label section-heading">` from Group 2):

```razor
@if (Snapshot.Markets.Count > 0)
{
    var g = Group();
    <div class="markets-rail">
        <h2 class="rail-label section-heading"><span class="section-roman">IV.</span>Polymarket · @Snapshot.Markets.Count markets</h2>

        @if (g.Cited.Count > 0)
        {
            <div class="market-tiles">
                @foreach (var m in g.Cited)
                {
                    <div class="tile cited">
                        <div class="prob">@m.Probability.ToString("P0", FrFr)</div>
                        <div class="question">@m.Question</div>
                        <div class="meta">@m.EndDate.ToString("d MMM yyyy", FrFr) · vol $@m.VolumeUsd.ToString("N0", FrFr)</div>
                        <div class="claim">★ @_bySlug[m.Slug].Claim</div>
                    </div>
                }
            </div>
        }

        @if (g.Ladder.Count > 0)
        {
            <div class="ladder">
                <span class="ladder-label">Fed rate cuts, 2026</span>
                <div class="ladder-row">
                    @foreach (var i in VisibleBucketIndices(g.Ladder))
                    {
                        var b = g.Ladder[i];
                        <span class="ladder-bucket @(b.Probability >= 0.30m ? "lead" : "")">
                            <span class="lb-n">@(b.OrMore ? $"{b.Cuts}+" : b.Cuts.ToString())</span>
                            <span class="lb-p">@b.Probability.ToString("P0", FrFr)</span>
                        </span>
                    }
                </div>
            </div>
        }

        @if (g.Other.Count > 0)
        {
            <div class="market-tiles other">
                @foreach (var m in g.Other)
                {
                    <div class="tile">
                        <div class="prob">@m.Probability.ToString("P0", FrFr)</div>
                        <div class="question">@m.Question</div>
                        <div class="meta">@m.EndDate.ToString("d MMM yyyy", FrFr) · vol $@m.VolumeUsd.ToString("N0", FrFr)</div>
                    </div>
                }
            </div>
        }
    </div>
}
```

- [ ] **Step 2: `MarketsRail.razor.css`** — append (the existing `.tile`/`.tile.cited`/`.prob`/`.question`/`.meta`/`.claim` rules and the ≤1024/≤700px breakpoints are reused):

```css
.market-tiles + .ladder, .ladder + .market-tiles.other, .market-tiles + .market-tiles.other { margin-top: 16px; }
.ladder { padding: 12px 0; border-top: 1px dotted rgba(196,154,86,0.18); }
.ladder-label { font-family: var(--font-mono); font-size: 9px; letter-spacing: 0.28em;
    text-transform: uppercase; color: var(--ink-3); }
.ladder-row { margin-top: 10px; display: flex; flex-wrap: wrap; gap: 8px 14px; align-items: baseline; }
.ladder-bucket { display: inline-flex; align-items: baseline; gap: 6px;
    font-family: var(--font-mono); font-size: 12px; color: var(--ink-3); font-variant-numeric: tabular-nums; }
.ladder-bucket .lb-n { font-size: 10px; letter-spacing: 0.12em; color: var(--ink-4); }
.ladder-bucket.lead, .ladder-bucket.lead .lb-n { color: var(--vault-gold); }
.market-tiles.other .tile { padding: 10px 12px 12px; }
.market-tiles.other .tile .prob { font-size: 18px; }
```

- [ ] **Step 3: Build + verify** — `dotnet build` (no warnings). Open `/`, screenshot section IV. Acceptance: cited tiles first (BTC-$150k tile + the "no cuts" tile show `★ …`); then `FED RATE CUTS, 2026` reading roughly `0 → 58% · 1 → 1% · 6 → 1% · 12+ → 0%` with the `0` bucket gold-tinted; then leftover tiles (if any). At ≤700px ladder wraps, tiles go 1-up. `cited + visible-buckets + other ≤ Snapshot.Markets.Count` and nothing important hidden.
- [ ] **Step 4: Commit** — `git commit -m "design(markets): lead with cited markets, collapse rate-cut ladder into a distribution"`.

---

# Task Group 7 — Reconcile Positions ↔ Holdings

**Why:** `VI. Positions` is a one-row table with a `Total` that equals the row; `VII. Holdings` mixes the held position card with watchlist tickers (BTC, COIN) without saying which is which; the day-move `+14,8%` chip reads as just another green number next to position P&L. (a) Hide the Total row for a single position; (b) tag held vs watchlist in the rail; (c) make the day-move chip unmistakably "today".

### Task 7.1: Suppress the redundant Total row

**Files:** `PositionsTable.razor`.

- [ ] **Step 1** — wrap `<tr class="total">…</tr>` in `@if (Rows.Count > 1) { … }`.
- [ ] **Step 2: Build + verify** — `/` shows the Positions table with just the `CON3.L` row, no `Total`. (Add a second instrument in Settings, see the Total return, revert.)
- [ ] **Step 3: Commit** — `git commit -m "design(positions): hide the redundant Total row when only one position"`.

### Task 7.2: Tag held vs watchlist; mark the day-move chip

**Files:** `PortfolioRail.razor`, `PortfolioRail.razor.cs`, `PortfolioRail.razor.css`.

- [ ] **Step 1: `PortfolioRail.razor.cs`** — `PortfolioRail` already receives `Snap` (`PortfolioSnapshot`, which has `Positions`), so derive the held set without any new parameter:

```csharp
private HashSet<string>? _heldTickers;
protected override void OnParametersSet()
    => _heldTickers = Snap.Positions.Select(p => p.Ticker).ToHashSet();
private bool IsHeld(TickerView t) => _heldTickers!.Contains(t.Ticker);

// day-move chip: absolute value, real minus carried by the arrow glyph in markup
private static string FormatDelta(decimal pct) => NumberFormat.Pct(Math.Abs(pct));
```

  (Add `using TradyStrat.Common.Formatting;` if not present from Task 1.3. If a `OnParametersSet` already exists, merge.)

- [ ] **Step 2: `PortfolioRail.razor`** — in each ticker `.cell`, change `<div class="tk">@t.Ticker</div>` to `<div class="tk">@t.Ticker <span class="tk-kind @(IsHeld(t) ? "" : "watch")">@(IsHeld(t) ? "held" : "watch")</span></div>`. Change the delta chip from `<span class="delta @(dp >= 0 ? "" : "dn") num">@FormatDelta(dp)</span>` to `<span class="delta @(dp >= 0 ? "" : "dn") num" title="change today">@(dp >= 0 ? "↑" : "↓") @FormatDelta(dp)</span>`. (The first `.cell` — the held-position summary — is unchanged; its `P&L −59,1 %` already reads as P&L via the `.sub` mono treatment, now with a real minus from Task 1.3's `PnL` fix.)

- [ ] **Step 3: `PortfolioRail.razor.css`** — add:

```css
.tk-kind { margin-left: 8px; font-family: var(--font-mono); font-size: 8px;
    letter-spacing: 0.22em; text-transform: uppercase; color: var(--ink-4);
    border: 1px solid var(--vault-rule); padding: 2px 5px; border-radius: 2px; }
.tk-kind:not(.watch) { color: var(--vault-gold); border-color: rgba(196,154,86,0.4); }
```

  And ensure `.tk` lays the chip out inline — it's `font-family: var(--font-display); font-style: italic;` already; add `display: inline-flex; align-items: baseline; gap: 0;` if the chip wraps oddly.

- [ ] **Step 4: Build + verify** — section VII: `Position` cell `63 650 sh · avg €1,22 · P&L −59,1 %`; each ticker cell shows the ticker with a `HELD`/`WATCH` chip (`CON3.L` → HELD gold, `BTC-USD`/`COIN` → WATCH muted); day chip `↑ 14,8 %` / `↓ 1,3 %` with "change today" tooltip. No breakage at 1180px / 720px.
- [ ] **Step 5: Commit** — `git commit -m "design(holdings): tag held vs watchlist tickers, mark day-change explicitly"`.

---

# Task Group 8 — Today's Call: TL;DR, disabled-button reason, masthead clarity

### Task 8.1: 3-bullet TL;DR above the rationale

**Files:** `TodaysCallCard.razor`, `TodaysCallCard.razor.css`.

- [ ] **Step 1: `TodaysCallCard.razor`** — between the backfill pill and `<p class="reasons">`:

```razor
        @if (Sug.Citations is { Count: > 0 })
        {
            <ul class="tldr">
                @foreach (var c in Sug.Citations.Take(3))
                {
                    <li><span class="tldr-k">@c.Indicator <em>@c.Ticker</em></span> @c.Claim</li>
                }
            </ul>
        }
```

- [ ] **Step 2: `TodaysCallCard.razor.css`** — add:

```css
.tldr { list-style: none; margin: 0 0 16px; padding: 0; display: grid; gap: 6px; }
.tldr li { font-family: var(--font-mono); font-size: 11px; line-height: 1.5; color: var(--ink-3);
    padding-left: 14px; position: relative; }
.tldr li::before { content: "·"; position: absolute; left: 2px; color: var(--vault-gold); }
.tldr-k { color: var(--ink-2); letter-spacing: 0.06em; }
.tldr-k em { color: var(--vault-gold); font-style: normal; }
```

  (The `.verb-drop` float lives inside `.reasons`, which comes *after* the `<ul>`; a float can't push back into a preceding block, so the TL;DR sits full-width above. Verify the rationale prose still wraps the giant `Hold.` glyph as before.)

- [ ] **Step 3: Build + verify** — under the section heading: three mono bullets (`RSI(14) CON3.L  Neutral momentum…` / `200-SMA CON3.L  Price 0.59 well below…` / `Ichimoku CON3.L  Bearish trend filter still active`), then the `Hold.` drop-cap + full rationale, unchanged. At 720px the bullets wrap cleanly. Note: this intentionally previews the first 3 of section V (Cited Evidence) — that's the point (skim up top, full list below).
- [ ] **Step 4: Commit** — `git commit -m "design(call): add a 3-bullet TL;DR above the AI rationale"`.

### Task 8.2: Explain the disabled "Log trade" button

**Files:** `TodaysCallCard.razor`, `TodaysCallCard.razor.css`.

- [ ] **Step 1: `TodaysCallCard.razor`** — replace the `<div class="actions">…</div>` block (and add a hint below it):

```razor
            <div class="actions">
                <button class="cta"
                        disabled="@(Sug.Action != SuggestionAction.Acquire)"
                        title="@(Sug.Action == SuggestionAction.Acquire ? "Log the suggested acquisition" : $"Disabled — today's call is \"{Verb}\", not an acquisition")"
                        @onclick="OnLogTrade">Log trade</button>
                <button class="cta ghost" @onclick="OnRerun">Re-run AI</button>
            </div>
            @if (Sug.Action != SuggestionAction.Acquire)
            {
                <p class="actions-hint">Logging is enabled only when the call is an acquisition.</p>
            }
```

  (`Verb` is the existing `TodaysCallCard.razor.cs` property — now backed by `SuggestionActionDisplay`.)

- [ ] **Step 2: `TodaysCallCard.razor.css`** — add:

```css
.actions-hint { margin: 10px 0 0; clear: both; font-family: var(--font-mono);
    font-size: 9px; letter-spacing: 0.18em; text-transform: uppercase; color: var(--ink-4); }
```

- [ ] **Step 3: Build + verify** — hover disabled `Log trade` → tooltip `Disabled — today's call is "Hold", not an acquisition`; micro-hint line below the buttons. When the call is `Acquire`, button enabled, hint gone.
- [ ] **Step 4: Commit** — `git commit -m "ux(call): explain why Log trade is disabled"`.

### Task 8.3: Clarify the masthead journal-entry label + date-picker affordance

**Files:** `VaultMasthead.razor`, `VaultMasthead.razor.css`.

- [ ] **Step 1: `VaultMasthead.razor`** — change `entry @EntryNumber.ToString("D4", …)` → `journal entry @EntryNumber.ToString("D4", …)` in **both** the `.ribbon-meta` span (nav branch) and the `.meta` div (`ShowNav=false` branch — currently `· entry @EntryNumber…`). Add `title="Pick a date — time-travel the dashboard to that trading day"` to the `<label class="ribbon-date">`.
- [ ] **Step 2: `VaultMasthead.razor.css`** — only if the longer "journal entry …" string crowds the ribbon at 860–1100px (check in step 3): add `@media (max-width: 1100px){ .masthead .ribbon-meta{ letter-spacing: 0.12em; } }`. The ≤560px rule from Task 3.2 already wraps it onto its own row.
- [ ] **Step 3: Build + verify** — masthead at 1440/900/390px: reads `journal entry 0004 · 14h ago`; date hover shows the time-travel tooltip; nothing clips.
- [ ] **Step 4: Commit** — `git commit -m "ux(masthead): say 'journal entry', explain the date-picker time-travel"`.

---

# Task Group 9 — Trades page actions + Settings input styling

### Task 9.1: Trades — icon buttons, delete confirm, cumulative-invested column

**Why:** `edit`/`×` are tiny text links (small tap targets; red `×` reads like "unsaved"); delete is a one mis-click; a running "invested" column ties the ledger to the dashboard's cost basis. **Definition:** the new column shows *cumulative net cash invested* (`Σ Trade.NetEur` with sells subtracted) — for the current all-buys data its last row equals the dashboard's `OWN CAPITAL`; once sells exist it diverges from the FIFO cost basis, which is fine — this column is "what you've put in", not "cost of held lots". Don't reimplement FIFO here.

**Files:** `TradesPage.razor`, `TradesPage.razor.cs`, `TradesPage.razor.css`.

- [ ] **Step 1: `TradesPage.razor.cs`** — `_trades` is loaded oldest-first by `AllTradesSpec`. In `Reload()`, after `_trades = list.ToList();`, build a parallel running total:

```csharp
    private List<(Trade Trade, decimal CumulativeEur)> _rows = new();
    // … in Reload(), after _trades is set:
    {
        var running = 0m;
        _rows = _trades.Select(t =>
        {
            running += t.IsBuy ? t.NetEur : -t.NetEur;
            return (t, running);
        }).ToList();
    }
    private Trade? _pendingDelete;
```

  (Keep `_trades`/`_count` as they are; `_rows` is just the display projection.)

- [ ] **Step 2: `TradesPage.razor`** — change the table header to add a `Invested` column after `Fees`: `<th class="num">Invested</th>` (before the actions `<th></th>`). Change the body loop to iterate `_rows` instead of `_trades`:

```razor
        @foreach (var (t, cum) in _rows)
        {
            <tr>
                <td class="ticker">@TickerFor(t.InstrumentId)</td>
                <td class="num">@t.ExecutedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)</td>
                <td><span class="side @(t.Side == TradeSide.Buy ? "buy" : "sell")">@t.Side</span></td>
                <td class="num">@NumberFormat.Qty(t.Quantity)</td>
                <td class="num">@NumberFormat.Price(t.PricePerShare, "€")</td>
                <td class="num">@NumberFormat.Price(t.FeesEur, "€")</td>
                <td class="num invested">@NumberFormat.Eur(cum)</td>
                <td class="note">@t.Note</td>
                <td class="row-actions">
                    <button class="icon-btn" @onclick="() => StartEdit(t)" title="Edit trade" aria-label="Edit trade">✎</button>
                    <button class="icon-btn danger" @onclick="() => _pendingDelete = t" title="Delete trade" aria-label="Delete trade">🗑</button>
                </td>
            </tr>
        }
```

  Add a confirm modal after the existing Import-CSV modal (reusing the `.modal`/`.modal-body`/`.btn` classes already in `TradesPage.razor.css`):

```razor
@if (_pendingDelete is { } pd)
{
    <div class="modal" @onclick="() => _pendingDelete = null">
        <div class="modal-body" @onclick:stopPropagation="true">
            <h3>Delete trade?</h3>
            <p>@pd.Side @NumberFormat.Qty(pd.Quantity) @TickerFor(pd.InstrumentId) on @pd.ExecutedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) — this can't be undone.</p>
            <div class="modal-actions">
                <button class="btn" @onclick="async () => { await DeleteAsync(pd); _pendingDelete = null; }">Delete</button>
                <button class="btn ghost" @onclick="() => _pendingDelete = null">Cancel</button>
            </div>
        </div>
    </div>
}
```

  (`@using TradyStrat.Common.Formatting` from Task 1.3 is already on this file.)

- [ ] **Step 3: `TradesPage.razor.css`** — replace the `.link` / `.link.danger` rules with:

```css
.row-actions { white-space: nowrap; text-align: right; }
.icon-btn { width: 28px; height: 28px; display: inline-flex; align-items: center; justify-content: center;
    color: var(--ink-3); border: 1px solid transparent; border-radius: 3px; font-size: 13px;
    transition: color 140ms, border-color 140ms, background-color 140ms; }
.icon-btn:hover { color: var(--vault-gold); border-color: var(--vault-rule); background: rgba(236,230,214,0.03); }
.icon-btn.danger:hover { color: var(--vault-red); border-color: rgba(214,120,120,0.4); }
.t td.invested { color: var(--ink-3); }
```

  If the extra column makes the table overflow at ≤720px, wrap `<table class="t">` in `<div class="t-scroll">` with `.t-scroll{ overflow-x: auto; }`.

- [ ] **Step 4: Build + verify** — `/trades`: `Invested` column whose bottom value equals the dashboard `OWN CAPITAL` (`€77 959` with current data); `✎`/`🗑` 28×28 icon buttons with hover states; `🗑` opens "Delete trade?"; Cancel dismisses; Delete removes the row and re-reads. At ≤720px the table is usable.
- [ ] **Step 5: Commit** — `git commit -m "ux(trades): icon action buttons, delete confirm, cumulative-invested column"`.

### Task 9.2: Theme native `<select>` + date inputs on Settings

**Files:** `wwwroot/css/settings-forms.css` (and `SettingsPage.razor.css` if it has its own input rules — fold them into `settings-forms.css`, which already mirrors the Goal section).

- [ ] **Step 1** — find the existing `.settings-section .field input` rule; extend it to also match `select`, add `color-scheme: dark`, `appearance: none`, a custom dropdown caret, and a tinted Chromium date-picker indicator:

```css
.settings-section .field input,
.settings-section .field select {
    color-scheme: dark; appearance: none;
    background-color: var(--vault-bg-2);
    border: 1px solid var(--vault-rule);
    color: var(--vault-ivory);
    font-family: var(--font-mono); font-size: 13px;
    padding: 9px 11px; border-radius: 2px;
    transition: border-color 140ms;
}
.settings-section .field input:focus,
.settings-section .field select:focus { border-color: var(--vault-gold); outline: none; }
.settings-section .field select {
    padding-right: 28px;
    background-image:
        linear-gradient(45deg, transparent 50%, var(--vault-gold) 50%),
        linear-gradient(135deg, var(--vault-gold) 50%, transparent 50%);
    background-position: calc(100% - 14px) center, calc(100% - 9px) center;
    background-size: 5px 5px, 5px 5px; background-repeat: no-repeat;
}
.settings-section .field input[type="date"]::-webkit-calendar-picker-indicator {
    filter: invert(70%) sepia(40%) saturate(400%) hue-rotate(2deg); cursor: pointer;
}
```

  (Merge — don't double-declare — with whatever the file already sets for `.field input`. If `SettingsPage.razor.css` styles the Goal-section inputs separately, delete that and let `settings-forms.css` own all of it.)

- [ ] **Step 2: Build + verify** — `/settings`: `Focus ticker` select shows a gold ▾ on dark with a gold focus ring; date inputs show a tinted calendar icon + a dark picker popup; no layout shift in the two-column `.grid`.
- [ ] **Step 3: Commit** — `git commit -m "style(settings): theme native select + date inputs to match the vault"`.

---

# Task Group 10 — Growth chart polish (verify interactivity, emphasise active range)

**Why:** The chart *already* has a JS module (`growth-chart.js`), a hover tooltip (`.gc-tooltip`), a navigator strip, and range presets with an `.active` state. So this is mostly verification plus a small emphasis bump.

**Files:** `GrowthChart.razor.css` (emphasis); `GrowthChart.razor` / `growth-chart.js` only if a gap is found.

- [ ] **Step 1: Verify the tooltip** — hover the chart line at a few x-positions; expect `.gc-tooltip` with `DATE / big value / delta / rows`. If it doesn't appear, `list_console_messages`, find the `growth-chart.js` error, fix it (likely a stale element id), document it. If it works → no change.
- [ ] **Step 2: Verify touch** — at 390px tap-and-drag the chart; if the tooltip doesn't follow, add `pointer`-event handlers alongside the existing `mouse` ones in `growth-chart.js` (extend, don't rewrite). If it works → no change.
- [ ] **Step 3: Emphasise the active preset** — `GrowthChart.razor.css`, replace `.navi-presets button.active { … }` (and the `+ button` border rule):

```css
.navi-presets button.active {
    color: #1B1710; background: var(--vault-gold); border-color: var(--vault-gold);
    box-shadow: 0 0 10px rgba(196,154,86,0.4);
}
.navi-presets button.active + button { border-left-color: var(--vault-gold); }
```

- [ ] **Step 4: Build + verify** — `/`: click each of `3M/6M/1Y/2Y/ALL` — active one is solid gold-on-dark, instantly readable; chart window updates; tooltip + navigator drag still work.
- [ ] **Step 5: Commit** — `git commit -m "design(chart): make the active range preset solid-gold for visibility"`.

---

# Wrap-up (after all groups, or after each group you ship)

- [ ] **Full build + test** — `dotnet build` (zero new warnings), `dotnet test` (all green; ≥213 tests, none lost).
- [ ] **Cross-page visual sweep** — `/`, `/trades`, `/settings` at 1440/900/390px; scroll each fully; confirm: no `<body>` horizontal scrollbar at any width; sticky bar appears/disappears (and survives `/`→`/trades`→`/` and the error→Retry path); all numbers use the unified format (no `63650,00`, real `−`); sections are `<h2>` and the wordmark is `<h1>` (a11y snapshot); no console errors.
- [ ] **`prefers-reduced-motion`** — toggle on; page-load cascade, sticky-bar transform, chart draw-in all degrade gracefully (existing `@media` blocks + the new sticky-bar rule cover this).
- [ ] **Clean up** — remove any throwaway screenshot dirs; `git status` clean.

---

## Self-Review

**Spec coverage** — every review-list item maps to a task: verb-palette DRY → G0; number formatting → G1; heading semantics + missing `<h1>` → G2; mobile → G3; sticky header → G4; hero re-weighting + plan tick → G5; Polymarket dedup → G6; Positions/Holdings + day-change → G7; today's-call TL;DR + disabled button + journal-entry label → G8; trades-page actions + cumulative column + settings inputs → G9; chart interactivity (verify, since it already exists) + active-range → G10.

**Placeholder scan** — no "TBD"/"handle edge cases"/"similar to Task N". The "confirm `DashboardViewModel` exposes `Portfolio`/`Goal`/`TodaysCall`" note in Task 4.2 is a one-line existence check (those names are already used in `DashboardPage.razor`); the "if it crowds, add a media query" notes in 8.3/9.1 are conditional polish, not deferred work.

**Type consistency** — `NumberFormat` surface: `Eur`, `EurBody`, `SignedEur`, `Qty`, `Pct`, `Price(decimal, string)` — used the same way at every call site, all 15 tests assert exact strings with explicit ` `/` `/`−` escapes (no ICU dependency). `SuggestionActionDisplay.Verb/Stem` — used by `TodaysCallCard.razor.cs` and `StickyCapitalBar.razor.cs`; the CSS verb colours are `--verb-color-{stem}` tokens in one place. `StickyCapitalBar` params (`CurrentValueEur`, `TargetEur`, `ProgressPct`, `Action: SuggestionAction?`) match between `.razor.cs` and the call site in `DashboardPage.razor`. `MarketsRail`: types are `MarketSnapshot`/`PredictionMarket`/`MarketCitation` (verified), the file is **modified** (it exists), `LadderBucket` is a `readonly record struct` and "is this the last bucket" is an index check. Sections all become `<h2 class="section-heading">` (one styling rule); the wordmark becomes `<h1 class="brand">`.

**No unresolved forks** — Group 2 commits to the shared `.section-heading` class (the DRY choice; it's also the future home for the 7 duplicate label rules). Group 9's cumulative column has a stated definition (cumulative net invested via `Trade.NetEur`, not FIFO). `HeroCapital.CostBasisEur` switch to `Snap.CostBasisEur` is offered as an optional free fix, clearly marked.

**Known follow-ups left out of scope (noted, not done here):** the three Phase-1 carryover bugs (CSV import routing, `EntryNavigationService` CON3.L anchoring, hardcoded `"CON3.L"` string literals) and the planned `DashboardViewModel` rewrite ("Task 14" referenced in `PortfolioSnapshot`) are untouched.
