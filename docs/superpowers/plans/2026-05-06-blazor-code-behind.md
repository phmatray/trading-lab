# Blazor Code-Behind Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split each Blazor component with C# logic into a `.razor` (markup + routing directives) and a `.razor.cs` (partial class with fields, parameters, lifecycle, `[Inject]` properties).

**Architecture:** For each of the 10 components in scope, create a sibling `.razor.cs` partial class that owns all C# concerns. The `.razor` keeps `@page`, `@layout`, `@rendermode`, markup, inline `<style>`, and the subset of `@using` directives required for markup compilation (component tag names, type names referenced from inline expressions). `@inject` directives become `[Inject]` properties on the partial class.

**Tech Stack:** .NET 10, Blazor Server (`Microsoft.NET.Sdk.Web`), C# 14, file-scoped namespaces, nullable reference types enabled, `TreatWarningsAsErrors=true`.

**Reference spec:** `docs/superpowers/specs/2026-05-06-blazor-code-behind-design.md`

---

## Conventions used in every task

- Each new `.razor.cs` file uses **file-scoped namespace**, matches the folder layout (consistent with existing `.cs` files in the project).
- Class declared as `public partial class <Name> : ComponentBase` so the file compiles standalone in the IDE.
- `[Inject]` properties: `[Inject] private <Type> <Name> { get; set; } = default!;` — `private` access, `default!` to satisfy nullable reference types under `TreatWarningsAsErrors`.
- The `.razor` file keeps an `@using` directive **only if the markup itself references** a component tag (e.g. `<VaultMasthead>`) or a type name (e.g. `CultureInfo.InvariantCulture`, `TradeSide.Buy`). All other `@using` directives are dropped from the `.razor` and added as regular `using` statements in the `.razor.cs`.
- Fields like `_vm`, `FrFr`, `Initial` referenced from markup do **not** require the type's `@using` to remain in the `.razor` — Razor resolves member access through the partial class.
- Build verification after each component (`dotnet build`) catches any missed `@using` immediately.

---

## Task 0: Baseline — confirm clean build before refactor

**Files:** none

- [ ] **Step 1: Confirm working tree is clean**

Run: `git status`
Expected: `nothing to commit, working tree clean`

- [ ] **Step 2: Build the solution to establish a green baseline**

Run: `dotnet build TradyStrat.slnx`
Expected: `Build succeeded.` with 0 errors. Note the warning count for comparison.

- [ ] **Step 3: Run the test suite as a baseline**

Run: `dotnet test TradyStrat.slnx`
Expected: All tests pass.

---

## Task 1: `RefreshFab`

**Files:**
- Create: `TradyStrat/Features/Dashboard/Components/RefreshFab.razor.cs`
- Modify: `TradyStrat/Features/Dashboard/Components/RefreshFab.razor`

- [ ] **Step 1: Create `RefreshFab.razor.cs`**

```csharp
using Microsoft.AspNetCore.Components;

namespace TradyStrat.Features.Dashboard.Components;

public partial class RefreshFab : ComponentBase
{
    [Parameter] public bool Busy { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
}
```

- [ ] **Step 2: Replace `RefreshFab.razor` content**

Final file contents:

```razor
<button class="fab" @onclick="OnClick" disabled="@Busy" title="Refresh prices">
    @if (Busy) { <span class="spin">↻</span> } else { <span>↻</span> }
</button>
```

(Removes the `@code { … }` block. No `@using` was present.)

- [ ] **Step 3: Build**

Run: `dotnet build TradyStrat.slnx`
Expected: 0 errors, no new warnings.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/RefreshFab.razor TradyStrat/Features/Dashboard/Components/RefreshFab.razor.cs
git commit -m "refactor(dashboard): extract RefreshFab code-behind"
```

---

## Task 2: `VaultMasthead`

**Files:**
- Create: `TradyStrat/Features/Dashboard/Components/VaultMasthead.razor.cs`
- Modify: `TradyStrat/Features/Dashboard/Components/VaultMasthead.razor`

- [ ] **Step 1: Create `VaultMasthead.razor.cs`**

```csharp
using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace TradyStrat.Features.Dashboard.Components;

public partial class VaultMasthead : ComponentBase
{
    [Parameter] public DateOnly Today { get; set; }
    [Parameter] public int EntryNumber { get; set; }

    private static string FormatDate(DateOnly d)
        => d.ToString("dd · MM · yyyy", CultureInfo.InvariantCulture);
}
```

- [ ] **Step 2: Replace `VaultMasthead.razor` content**

Final file contents:

```razor
@using System.Globalization
<div class="masthead">
    <div class="brand">
        <a href="/">Tradystrat</a>
        <span class="arc">— a private chronicle of accumulation</span>
    </div>
    <nav class="nav">
        <NavLink class="nav-link" href="/" Match="NavLinkMatch.All">Dashboard</NavLink>
        <NavLink class="nav-link" href="/trades">Trades</NavLink>
        <NavLink class="nav-link" href="/settings">Settings</NavLink>
    </nav>
    <div class="meta">@FormatDate(Today) · entry no. @EntryNumber.ToString("D4", CultureInfo.InvariantCulture)</div>
</div>
```

(Keeps `@using System.Globalization` because `CultureInfo.InvariantCulture` appears in the markup expression. Removes `@code` block.)

- [ ] **Step 3: Build**

Run: `dotnet build TradyStrat.slnx`
Expected: 0 errors, no new warnings.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/VaultMasthead.razor TradyStrat/Features/Dashboard/Components/VaultMasthead.razor.cs
git commit -m "refactor(dashboard): extract VaultMasthead code-behind"
```

---

## Task 3: `TodaysCallCard`

**Files:**
- Create: `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.cs`
- Modify: `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor`

- [ ] **Step 1: Create `TodaysCallCard.razor.cs`**

```csharp
using System.Globalization;
using Microsoft.AspNetCore.Components;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Dashboard.Components;

public partial class TodaysCallCard : ComponentBase
{
    [Parameter, EditorRequired] public Suggestion Sug { get; set; } = null!;
    [Parameter] public DateOnly Today { get; set; }
    [Parameter] public EventCallback OnLogTrade { get; set; }
    [Parameter] public EventCallback OnRerun { get; set; }

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

    private string Verb => Sug.Action switch
    {
        SuggestionAction.Acquire => "Acquire.",
        SuggestionAction.Hold    => "Hold.",
        SuggestionAction.Trim    => "Trim.",
        SuggestionAction.Wait    => "Wait.",
        _ => "—"
    };
}
```

- [ ] **Step 2: Replace `TodaysCallCard.razor` content**

Final file contents:

```razor
@using TradyStrat.Shared.Domain
<div class="call">
    <div class="label">Today's call · @Today.ToString("d MMMM", FrFr)</div>
    <div class="verb">@Verb</div>

    @if (Sug is { Action: SuggestionAction.Acquire, QuantityHint: { } q and > 0m, MaxPriceHint: { } mp and > 0m })
    {
        <div class="order num">
            @q.ToString("F0", FrFr) sh CON3 · ≤ €@mp.ToString("F2", FrFr)
            · ≈ €@((q * mp).ToString("F2", FrFr))
        </div>
    }

    <p class="reasons">@Sug.Rationale</p>

    @if (Sug.Citations.Count > 0)
    {
        <ol class="citations">
            @foreach (var c in Sug.Citations)
            {
                <li><b>@c.Indicator (@c.Ticker):</b> @c.Claim · <em>@c.Value</em></li>
            }
        </ol>
    }

    <div class="actions">
        <button class="cta" disabled="@(Sug.Action != SuggestionAction.Acquire)"
                @onclick="OnLogTrade">Log trade</button>
        <button class="cta ghost" @onclick="OnRerun">Re-run AI</button>
    </div>
</div>
```

(Drops `@using System.Globalization` — `CultureInfo` not referenced by markup; `FrFr` is a field of the partial class. Keeps `@using TradyStrat.Shared.Domain` because `SuggestionAction.Acquire` appears in markup pattern. Removes `@code` block.)

- [ ] **Step 3: Build**

Run: `dotnet build TradyStrat.slnx`
Expected: 0 errors, no new warnings.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.cs
git commit -m "refactor(dashboard): extract TodaysCallCard code-behind"
```

---

## Task 4: `PortfolioRail`

**Files:**
- Create: `TradyStrat/Features/Dashboard/Components/PortfolioRail.razor.cs`
- Modify: `TradyStrat/Features/Dashboard/Components/PortfolioRail.razor`

- [ ] **Step 1: Create `PortfolioRail.razor.cs`**

```csharp
using System.Globalization;
using Microsoft.AspNetCore.Components;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Dashboard.Components;

public partial class PortfolioRail : ComponentBase
{
    [Parameter, EditorRequired] public PortfolioSnapshot Snap { get; set; } = null!;
    [Parameter, EditorRequired] public IReadOnlyList<TickerView> Tickers { get; set; } = null!;

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

    private static string PnL(decimal pnl, decimal value)
    {
        if (value == 0m) return "—";
        var pct = pnl / (value - pnl) * 100m;
        return $"{(pct >= 0 ? "+" : "")}{pct.ToString("F1", FrFr)} %";
    }

    private static string FormatPrimary(TickerView t) => t.Currency switch
    {
        "EUR" => $"€{t.Price.ToString("N2", FrFr)}",
        "USD" => $"${t.Price.ToString("N2", FrFr)}",
        _     => t.Price.ToString("N2", FrFr)
    };

    private static string FormatDelta(decimal pct)
        => $"{(pct >= 0 ? "+" : "")}{pct.ToString("F1", FrFr)}%";
}
```

Note: `TickerView` lives in `TradyStrat.Features.Dashboard` (parent feature folder), which is why both that namespace and `TradyStrat.Shared.Domain` (for `PortfolioSnapshot`) are imported above.

- [ ] **Step 2: Replace `PortfolioRail.razor` content**

Final file contents:

```razor
<div class="rail">
    <div class="cell">
        <div class="lbl">Position</div>
        <div class="val"><span class="num">@Snap.Shares.ToString("N0", FrFr)</span> sh</div>
        <div class="sub">avg €@Snap.AvgCostEur.ToString("F2", FrFr)
                         · P&amp;L @PnL(Snap.UnrealizedPnLEur, Snap.CurrentValueEur)</div>
    </div>
    @foreach (var t in Tickers)
    {
        <div class="cell">
            <div class="tk">@t.Ticker</div>
            <div class="val">
                <span class="num">@FormatPrimary(t)</span>
                @if (t.DeltaPct is { } dp)
                {
                    <span class="delta @(dp >= 0 ? "" : "dn") num">@FormatDelta(dp)</span>
                }
            </div>
            @if (t.PriceEur is { } eur && t.Currency != "EUR")
            {
                <div class="sub">≈ €@eur.ToString("N2", FrFr)</div>
            }
        </div>
    }
</div>
```

(Markup uses no type names — only field/parameter member access. All `@using` lines drop.)

- [ ] **Step 3: Build**

Run: `dotnet build TradyStrat.slnx`
Expected: 0 errors. If unresolved-type errors appear, add the missing `using` to the `.cs` file (see note in Step 1).

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/PortfolioRail.razor TradyStrat/Features/Dashboard/Components/PortfolioRail.razor.cs
git commit -m "refactor(dashboard): extract PortfolioRail code-behind"
```

---

## Task 5: `GrowthChart`

**Files:**
- Create: `TradyStrat/Features/Dashboard/Components/GrowthChart.razor.cs`
- Modify: `TradyStrat/Features/Dashboard/Components/GrowthChart.razor`

- [ ] **Step 1: Create `GrowthChart.razor.cs`**

```csharp
using System.Globalization;
using Microsoft.AspNetCore.Components;
using TradyStrat.Features.Dashboard;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Dashboard.Components;

public partial class GrowthChart : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<GrowthPoint> Points { get; set; } = null!;
    [Parameter, EditorRequired] public GoalConfig Goal { get; set; } = null!;

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

    private DateOnly? EndDate =>
        Goal.TargetDate is { } d && Points.Count > 0 && d > Points[0].Date ? d : null;

    private string LinePath => PathBuilder.Line(Points, 1200, 220, Goal.TargetEur, EndDate);
    private string AreaPath => PathBuilder.Area(Points, 1200, 220, Goal.TargetEur, EndDate);

    private string GoalLabel => Goal.TargetDate is { } td
        ? $"€{Goal.TargetEur.ToString("N0", FrFr)} by {td.ToString("MMM yyyy", CultureInfo.InvariantCulture)} — goal"
        : $"€{Goal.TargetEur.ToString("N0", FrFr)} — goal";
}
```

- [ ] **Step 2: Replace `GrowthChart.razor` content**

Final file contents:

```razor
@using System.Globalization
@using TradyStrat.Features.Dashboard
<div class="chart-wrap">
    <div class="lbl">Capital growth · trajectory toward goal</div>
    <svg viewBox="0 0 1200 240" preserveAspectRatio="none" class="chart">
        <defs>
            <linearGradient id="vault-grad" x1="0" x2="0" y1="0" y2="1">
                <stop offset="0%" stop-color="#C49A56" stop-opacity="0.35" />
                <stop offset="100%" stop-color="#C49A56" stop-opacity="0" />
            </linearGradient>
        </defs>
        <g class="grid">
            <line x1="0" y1="40" x2="1200" y2="40"/>
            <line x1="0" y1="100" x2="1200" y2="100"/>
            <line x1="0" y1="160" x2="1200" y2="160"/>
            <line x1="0" y1="220" x2="1200" y2="220"/>
        </g>
        <path class="goal" d="M0,225 L1200,8" />
        <text class="axis-label" x="1186" y="20" text-anchor="end">@GoalLabel</text>
        <path style="fill:url(#vault-grad)" d="@AreaPath" />
        <path class="line" d="@LinePath" />
        @if (Points.Count > 0)
        {
            var last = Points[^1];
            var lastX = (double)PathBuilder.XForIndex(Points, Points.Count - 1, 1200, EndDate);
            var lastY = (double)(220m - last.ValueEur / Math.Max(1m, Goal.TargetEur) * 220m);
            var lastXStr = lastX.ToString("F1", CultureInfo.InvariantCulture);
            var lastYStr = lastY.ToString("F1", CultureInfo.InvariantCulture);
            var lastYLblStr = (lastY - 8).ToString("F1", CultureInfo.InvariantCulture);
            var labelAnchor = lastX < 200 ? "start" : "end";
            var labelX = (lastX < 200 ? lastX + 8 : lastX - 14).ToString("F1", CultureInfo.InvariantCulture);
            var lastValueStr = last.ValueEur.ToString("N0", FrFr);
            // Vertical "now" marker — only render when there's empty future runway to the right.
            if (lastX < 1192)
            {
                <line class="now" x1="@lastXStr" y1="0" x2="@lastXStr" y2="220" />
            }
            <circle r="3.5" cx="@lastXStr" cy="@lastYStr" fill="#C49A56" />
            <svg:text class="axis-label" x="@labelX" y="@lastYLblStr" text-anchor="@labelAnchor">
                €@lastValueStr — today
            </svg:text>
        }
        <g class="axis">
            @if (Points.Count > 0)
            {
                var firstDate = Points[0].Date.ToString("MMM yyyy", CultureInfo.InvariantCulture);
                var endDate   = (EndDate ?? Points[^1].Date).ToString("MMM yyyy", CultureInfo.InvariantCulture);
                var midOffset = ((Points[0].Date.DayNumber + (EndDate ?? Points[^1].Date).DayNumber) / 2);
                var midDate   = DateOnly.FromDayNumber(midOffset).ToString("MMM yyyy", CultureInfo.InvariantCulture);
                <svg:text x="0" y="236">@firstDate</svg:text>
                <svg:text x="600" y="236" text-anchor="middle">@midDate</svg:text>
                <svg:text x="1200" y="236" text-anchor="end">@endDate</svg:text>
            }
        </g>
    </svg>
</div>
```

(Keeps `@using System.Globalization` for `CultureInfo.InvariantCulture` in markup. Keeps `@using TradyStrat.Features.Dashboard` for `PathBuilder` referenced in markup. Drops `@using TradyStrat.Shared.Domain` — used only in C# code. Removes `@code` block.)

- [ ] **Step 3: Build**

Run: `dotnet build TradyStrat.slnx`
Expected: 0 errors, no new warnings.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/GrowthChart.razor TradyStrat/Features/Dashboard/Components/GrowthChart.razor.cs
git commit -m "refactor(dashboard): extract GrowthChart code-behind"
```

---

## Task 6: `HeroCapital`

**Files:**
- Create: `TradyStrat/Features/Dashboard/Components/HeroCapital.razor.cs`
- Modify: `TradyStrat/Features/Dashboard/Components/HeroCapital.razor`

- [ ] **Step 1: Create `HeroCapital.razor.cs`**

```csharp
using System.Globalization;
using Microsoft.AspNetCore.Components;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Dashboard.Components;

public partial class HeroCapital : ComponentBase
{
    [Parameter, EditorRequired] public PortfolioSnapshot Snap { get; set; } = null!;
    [Parameter, EditorRequired] public GoalConfig Goal { get; set; } = null!;
    [Parameter, EditorRequired] public DateOnly Today { get; set; }

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

    private decimal Pct => Snap.ProgressPct;

    // Cost basis of currently-held lots = principal still in market.
    private decimal CostBasisEur => Snap.Shares * Snap.AvgCostEur;
    private decimal Goal100 => Goal.TargetEur <= 0m ? 1m : Goal.TargetEur;

    // All percentages are vs. goal so the bar adds up to ProgressPct.
    private decimal CostBasisPct  => Clamp01(CostBasisEur / Goal100 * 100m);
    private decimal RealizedPct   => Clamp01(Math.Max(0m, Snap.RealizedPnLEur) / Goal100 * 100m);
    private decimal UnrealizedAbsPct => Clamp01(Math.Abs(Snap.UnrealizedPnLEur) / Goal100 * 100m);
    private decimal CurrentPct    => Clamp01(Snap.CurrentValueEur / Goal100 * 100m);

    // When unrealized < 0, principal segment must NOT extend past current —
    // the "loss" hashed segment fills the gap from current back to cost.
    private decimal PrincipalShownPct =>
        Snap.UnrealizedPnLEur < 0m
            ? Math.Max(0m, CostBasisPct - UnrealizedAbsPct)
            : CostBasisPct;

    private static decimal Clamp01(decimal pct) => Math.Max(0m, Math.Min(100m, pct));
    private static string Fmt(decimal pct) => pct.ToString("F2", CultureInfo.InvariantCulture);

    private string FormatSigned(decimal v)
        => $"{(v >= 0 ? "+€" : "−€")}{Math.Abs(v).ToString("N0", FrFr)}";

    private string AriaSummary =>
        $"Progress {Pct.ToString("F1", FrFr)}% — own capital €{CostBasisEur.ToString("N0", FrFr)}, " +
        $"unrealized {FormatSigned(Snap.UnrealizedPnLEur)}, realized {FormatSigned(Snap.RealizedPnLEur)}";

    private string? DaysLeft(DateOnly target)
    {
        var days = target.DayNumber - Today.DayNumber;
        if (days < 0) return "past due";
        if (days == 0) return "today";
        return $"{days.ToString("N0", FrFr)} days left";
    }
}
```

- [ ] **Step 2: Replace `HeroCapital.razor` content**

Final file contents:

```razor
@using System.Globalization
<div class="hero">
    <div class="label">Capital under accumulation</div>
    <div class="amount">
        <span class="euro">€</span><span class="num">@Snap.CurrentValueEur.ToString("N0", FrFr)</span>
        <span class="of">— of €@Goal.TargetEur.ToString("N0", FrFr) —</span>
        @if (Goal.TargetDate is { } target)
        {
            <span class="by">by @target.ToString("dd · MM · yyyy", CultureInfo.InvariantCulture)@(DaysLeft(target) is { } d ? $" · {d}" : "")</span>
        }
    </div>

    <div class="progress">
        <div class="bar" role="img" aria-label="@AriaSummary">
            @* Principal segment — 0 → min(cost, current). The portion of progress
               that is your own capital, irrespective of mark-to-market. *@
            <span class="seg principal" style="left:0; width:@Fmt(PrincipalShownPct)%"></span>

            @* Realized PnL segment — adds on top of principal. Booked, no longer at risk. *@
            @if (RealizedPct > 0m)
            {
                <span class="seg realized" style="left:@Fmt(PrincipalShownPct)%; width:@Fmt(RealizedPct)%"></span>
            }

            @* Unrealized PnL segment — depends on sign:
               · gain: green, extends current beyond cost
               · loss: red hashed, drawn from current back up to cost (the gap) *@
            @if (Snap.UnrealizedPnLEur > 0m)
            {
                <span class="seg gain" style="left:@Fmt(PrincipalShownPct + RealizedPct)%; width:@Fmt(UnrealizedAbsPct)%"></span>
            }
            else if (Snap.UnrealizedPnLEur < 0m)
            {
                <span class="seg loss" style="left:@Fmt(PrincipalShownPct + RealizedPct)%; width:@Fmt(UnrealizedAbsPct)%"></span>
            }

            @* Current-position tick at exact "today" mark *@
            <span class="tick now" style="left:@Fmt(CurrentPct)%" title="today"></span>
        </div>
        <div class="pct num">@Pct.ToString("F1", FrFr) %</div>
    </div>

    <dl class="legend">
        <div>
            <dt><span class="swatch principal"></span> Own capital</dt>
            <dd class="num">€@CostBasisEur.ToString("N0", FrFr)</dd>
        </div>
        @if (Snap.RealizedPnLEur != 0m)
        {
            <div>
                <dt><span class="swatch realized"></span> Realized</dt>
                <dd class="num @(Snap.RealizedPnLEur >= 0 ? "pos" : "neg")">@FormatSigned(Snap.RealizedPnLEur)</dd>
            </div>
        }
        <div>
            <dt><span class="swatch @(Snap.UnrealizedPnLEur >= 0 ? "gain" : "loss")"></span> Unrealized</dt>
            <dd class="num @(Snap.UnrealizedPnLEur >= 0 ? "pos" : "neg")">@FormatSigned(Snap.UnrealizedPnLEur)</dd>
        </div>
    </dl>
</div>
```

(Keeps `@using System.Globalization` for `CultureInfo.InvariantCulture` in markup. Drops `@using TradyStrat.Shared.Domain` — used only by C# parameters. Removes `@code` block.)

- [ ] **Step 3: Build**

Run: `dotnet build TradyStrat.slnx`
Expected: 0 errors, no new warnings.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/HeroCapital.razor TradyStrat/Features/Dashboard/Components/HeroCapital.razor.cs
git commit -m "refactor(dashboard): extract HeroCapital code-behind"
```

---

## Task 7: `AddTradeDialog`

**Files:**
- Create: `TradyStrat/Features/Trades/Components/AddTradeDialog.razor.cs`
- Modify: `TradyStrat/Features/Trades/Components/AddTradeDialog.razor`

- [ ] **Step 1: Create `AddTradeDialog.razor.cs`**

```csharp
using Microsoft.AspNetCore.Components;
using TradyStrat.Application.UseCases.Trades;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Trades.Components;

public partial class AddTradeDialog : ComponentBase
{
    [Parameter] public Trade? Initial { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback<LogTradeInput> OnSubmit { get; set; }

    private DateTime _date = DateTime.Today;
    private string _side = "Buy";
    private decimal _qty;
    private decimal _price;
    private decimal _fees;
    private string? _note;
    private string? _err;

    protected override void OnInitialized()
    {
        if (Initial is { } t)
        {
            _date  = t.ExecutedOn.ToDateTime(TimeOnly.MinValue);
            _side  = t.Side.ToString();
            _qty   = t.Quantity;
            _price = t.PricePerShare;
            _fees  = t.FeesEur;
            _note  = t.Note;
        }
    }

    private async Task DoSubmit()
    {
        if (_qty <= 0 || _price <= 0)
        {
            _err = "Quantity and price must be positive.";
            return;
        }
        var input = new LogTradeInput(
            DateOnly.FromDateTime(_date),
            Enum.Parse<TradeSide>(_side),
            _qty, _price, _fees, _note);

        await OnSubmit.InvokeAsync(input);
    }
}
```

- [ ] **Step 2: Replace `AddTradeDialog.razor` content**

Final file contents:

```razor
<div class="modal" @onclick="() => OnCancel.InvokeAsync()">
    <div class="modal-body" @onclick:stopPropagation="true">
        <h3>@(Initial is null ? "Add trade" : "Edit trade")</h3>
        <div class="grid">
            <label>Date<input type="date" @bind="_date" /></label>
            <label>Side<select @bind="_side">
                <option value="Buy">Buy</option><option value="Sell">Sell</option>
            </select></label>
            <label>Quantity<input type="number" step="0.0001" @bind="_qty" /></label>
            <label>Price (€)<input type="number" step="0.01" @bind="_price" /></label>
            <label>Fees (€)<input type="number" step="0.01" @bind="_fees" /></label>
            <label class="full">Note<input @bind="_note" /></label>
        </div>
        @if (!string.IsNullOrEmpty(_err))
        {
            <p class="err">@_err</p>
        }
        <div class="modal-actions">
            <button class="btn" @onclick="DoSubmit">Save</button>
            <button class="btn ghost" @onclick="() => OnCancel.InvokeAsync()">Cancel</button>
        </div>
    </div>
</div>
```

(Markup uses no type names. Drops both `@using` lines. Removes `@code` block.)

- [ ] **Step 3: Build**

Run: `dotnet build TradyStrat.slnx`
Expected: 0 errors, no new warnings.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Trades/Components/AddTradeDialog.razor TradyStrat/Features/Trades/Components/AddTradeDialog.razor.cs
git commit -m "refactor(trades): extract AddTradeDialog code-behind"
```

---

## Task 8: `SettingsPage`

**Files:**
- Create: `TradyStrat/Features/Settings/SettingsPage.razor.cs`
- Modify: `TradyStrat/Features/Settings/SettingsPage.razor`

- [ ] **Step 1: Create `SettingsPage.razor.cs`**

```csharp
using Ardalis.Specification;
using Microsoft.AspNetCore.Components;
using TradyStrat.Application.UseCases.Settings;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Shared.Time;
using TradyStrat.Specifications.Trades;

namespace TradyStrat.Features.Settings;

public partial class SettingsPage : ComponentBase
{
    [Inject] private IReadRepositoryBase<GoalConfig> GoalRepo { get; set; } = default!;
    [Inject] private IReadRepositoryBase<Trade> TradeRepo { get; set; } = default!;
    [Inject] private IClock Clock { get; set; } = default!;
    [Inject] private UpdateGoalUseCase UpdateGoal { get; set; } = default!;

    private decimal _target = 1_000_000m;
    private DateTime? _date;
    private string? _msg;
    private bool _isError;
    private bool _busy;
    private int _count;
    private DateTime? _lastUpdated;

    protected override async Task OnInitializedAsync()
    {
        var existing = await GoalRepo.GetByIdAsync(1, CancellationToken.None);
        if (existing is not null)
        {
            _target = existing.TargetEur;
            _date   = existing.TargetDate?.ToDateTime(TimeOnly.MinValue);
            _lastUpdated = existing.UpdatedAt;
        }
        _count = await TradeRepo.CountAsync(new AllTradesSpec(), CancellationToken.None);
    }

    private void OnInputChanged()
    {
        if (_msg is not null) { _msg = null; _isError = false; }
    }

    private async Task SaveAsync()
    {
        if (_busy) return;
        _busy = true;
        _msg = null;
        _isError = false;
        try
        {
            var date = _date is { } d ? DateOnly.FromDateTime(d) : (DateOnly?)null;
            var saved = await UpdateGoal.ExecuteAsync(new UpdateGoalInput(_target, date), CancellationToken.None);
            _lastUpdated = saved.UpdatedAt;
            _msg = "Saved.";
        }
        catch (TradyStratException ex)
        {
            _msg = ex.Message;
            _isError = true;
        }
        catch (Exception)
        {
            _msg = "Save failed — see logs.";
            _isError = true;
        }
        finally
        {
            _busy = false;
        }
    }
}
```

- [ ] **Step 2: Replace `SettingsPage.razor` content**

Final file contents:

```razor
@page "/settings"
@rendermode InteractiveServer
@using System.Globalization
@using TradyStrat.Features.Dashboard.Components

<VaultMasthead Today="@(Clock.TodayInExchangeTzFor("CON3.L"))" EntryNumber="@_count" />

<div class="settings">
    <div class="label">Settings</div>
    <h2>Goal</h2>
    <div class="grid">
        <label class="field">
            <span class="lbl">Target&nbsp;(€)</span>
            <input type="number" step="1000" min="1" @bind="_target" @bind:after="OnInputChanged" />
        </label>
        <label class="field">
            <span class="lbl">Target date <em class="hint">optional</em></span>
            <input type="date" @bind="_date" @bind:after="OnInputChanged" />
        </label>
        <label class="field">
            <span class="lbl">Focus ticker</span>
            <input value="CON3.L" disabled />
        </label>
    </div>
    <div class="actions">
        <button class="btn" @onclick="SaveAsync" disabled="@_busy">@(_busy ? "Saving…" : "Save")</button>
        @if (!string.IsNullOrEmpty(_msg))
        {
            <span class="msg @(_isError ? "err" : "ok")">@_msg</span>
        }
        @if (_lastUpdated is { } t)
        {
            <span class="meta">last updated @t.ToLocalTime().ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture)</span>
        }
    </div>
</div>
```

(Keeps `@page`, `@rendermode`, `@using System.Globalization` for `CultureInfo` in markup, `@using TradyStrat.Features.Dashboard.Components` for the `<VaultMasthead>` tag. Drops 6 other `@using` lines and all 4 `@inject` directives. Removes `@code` block.)

- [ ] **Step 3: Build**

Run: `dotnet build TradyStrat.slnx`
Expected: 0 errors, no new warnings.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Settings/SettingsPage.razor TradyStrat/Features/Settings/SettingsPage.razor.cs
git commit -m "refactor(settings): extract SettingsPage code-behind"
```

---

## Task 9: `TradesPage`

**Files:**
- Create: `TradyStrat/Features/Trades/TradesPage.razor.cs`
- Modify: `TradyStrat/Features/Trades/TradesPage.razor`

- [ ] **Step 1: Create `TradesPage.razor.cs`**

```csharp
using System.Globalization;
using Ardalis.Specification;
using Microsoft.AspNetCore.Components;
using TradyStrat.Application.UseCases.Trades;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Shared.Time;
using TradyStrat.Specifications.Trades;

namespace TradyStrat.Features.Trades;

public partial class TradesPage : ComponentBase
{
    [Inject] private IReadRepositoryBase<Trade> Repo { get; set; } = default!;
    [Inject] private IClock Clock { get; set; } = default!;
    [Inject] private LogTradeUseCase LogTrade { get; set; } = default!;
    [Inject] private EditTradeUseCase EditTrade { get; set; } = default!;
    [Inject] private DeleteTradeUseCase DeleteTrade { get; set; } = default!;
    [Inject] private ImportTradesCsvUseCase ImportCsv { get; set; } = default!;

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

    // Show share counts compactly: integers as N0, fractional shares with up
    // to 4 decimals but trailing zeros trimmed. So 42100,0000 → 42 100, but
    // 12,5000 → 12,5 — no decimal noise on integer trades.
    private static string FormatQty(decimal q)
    {
        if (q == decimal.Truncate(q)) return q.ToString("N0", FrFr);
        var s = q.ToString("N4", FrFr);
        // Trim trailing zeros after the decimal separator, then a hanging separator.
        var sep = FrFr.NumberFormat.NumberDecimalSeparator;
        var idx = s.IndexOf(sep, StringComparison.Ordinal);
        if (idx < 0) return s;
        s = s.TrimEnd('0');
        if (s.EndsWith(sep, StringComparison.Ordinal)) s = s[..^sep.Length];
        return s;
    }

    private List<Trade> _trades = new();
    private int _count;
    private bool _showAdd;
    private bool _showImport;
    private Trade? _editing;
    private string _csvText = "";
    private string? _importError;

    protected override async Task OnInitializedAsync() => await Reload();

    private async Task Reload()
    {
        var list = await Repo.ListAsync(new AllTradesSpec(), CancellationToken.None);
        _trades = list.ToList();
        _count = _trades.Count;
    }

    private void OnAddClicked()
    {
        _editing = null;
        _showAdd = true;
    }

    private void OnImportClicked()
    {
        _csvText = "";
        _importError = null;
        _showImport = true;
    }

    private void StartEdit(Trade t)
    {
        _editing = t;
        _showAdd = true;
    }

    private async Task HandleSubmit(LogTradeInput input)
    {
        if (_editing is null)
        {
            await LogTrade.ExecuteAsync(input, CancellationToken.None);
        }
        else
        {
            await EditTrade.ExecuteAsync(new EditTradeInput(
                _editing.Id, input.ExecutedOn, input.Side,
                input.Quantity, input.PricePerShare, input.FeesEur, input.Note), CancellationToken.None);
        }
        CloseDialogs();
        await Reload();
    }

    private async Task DeleteAsync(Trade t)
    {
        await DeleteTrade.ExecuteAsync(new DeleteTradeInput(t.Id), CancellationToken.None);
        await Reload();
    }

    private async Task DoImport()
    {
        try
        {
            await ImportCsv.ExecuteAsync(new ImportTradesCsvInput(_csvText), CancellationToken.None);
            CloseDialogs();
            await Reload();
        }
        catch (CsvImportException ex)
        {
            _importError = ex.Message;
        }
    }

    private void CloseDialogs()
    {
        _showAdd = false;
        _showImport = false;
        _editing = null;
        _importError = null;
    }
}
```

Note: the original `.razor` used `Shared.Exceptions.CsvImportException` (partial qualification). The `.cs` version above uses `using TradyStrat.Shared.Exceptions;` and the unqualified `CsvImportException` — cleaner and equivalent.

- [ ] **Step 2: Replace `TradesPage.razor` content**

Final file contents:

```razor
@page "/trades"
@rendermode InteractiveServer
@using System.Globalization
@using TradyStrat.Features.Dashboard.Components
@using TradyStrat.Features.Trades.Components
@using TradyStrat.Shared.Domain

<VaultMasthead Today="@Clock.TodayInExchangeTzFor("CON3.L")" EntryNumber="@_count" />

<div class="trades">
    <div class="hdr">
        <div class="label">Trade ledger</div>
        <div>
            <button class="btn" @onclick="OnAddClicked">+ Add trade</button>
            <button class="btn ghost" @onclick="OnImportClicked">Import CSV</button>
        </div>
    </div>

    <table class="t">
        <thead><tr><th>Date</th><th>Side</th><th>Qty</th><th>Price</th><th>Fees</th><th>Note</th><th></th></tr></thead>
        <tbody>
        @foreach (var t in _trades)
        {
            <tr>
                <td class="num">@t.ExecutedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)</td>
                <td><span class="side @(t.Side == TradeSide.Buy ? "buy" : "sell")">@t.Side</span></td>
                <td class="num">@FormatQty(t.Quantity)</td>
                <td class="num">€@t.PricePerShare.ToString("F2", FrFr)</td>
                <td class="num">€@t.FeesEur.ToString("F2", FrFr)</td>
                <td class="note">@t.Note</td>
                <td class="row-actions">
                    <button class="link" @onclick="() => StartEdit(t)">edit</button>
                    <button class="link danger" @onclick="() => DeleteAsync(t)" title="Delete trade">×</button>
                </td>
            </tr>
        }
        </tbody>
    </table>
</div>

@if (_showAdd)
{
    <AddTradeDialog Initial="_editing"
                    OnCancel="CloseDialogs"
                    OnSubmit="HandleSubmit" />
}

@if (_showImport)
{
    <div class="modal" @onclick="CloseDialogs">
        <div class="modal-body" @onclick:stopPropagation="true">
            <h3>Import CSV</h3>
            <p>Paste a CSV with columns <code>date,side,qty,price,fees</code>.</p>
            <textarea @bind="_csvText" rows="10"></textarea>
            <div class="modal-actions">
                <button class="btn" @onclick="DoImport">Import</button>
                <button class="btn ghost" @onclick="CloseDialogs">Cancel</button>
            </div>
            @if (!string.IsNullOrEmpty(_importError))
            {
                <p class="err">@_importError</p>
            }
        </div>
    </div>
}
```

(Keeps `@page`, `@rendermode`, and 4 `@using`: `System.Globalization` for `CultureInfo`, `Features.Dashboard.Components` for `<VaultMasthead>`, `Features.Trades.Components` for `<AddTradeDialog>`, `Shared.Domain` for `TradeSide.Buy`. Drops `Ardalis.Specification`, `Application.UseCases.Trades`, `Shared.Time`, `Specifications.Trades`. Drops 6 `@inject` directives. Removes `@code` block.)

- [ ] **Step 3: Build**

Run: `dotnet build TradyStrat.slnx`
Expected: 0 errors, no new warnings.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Trades/TradesPage.razor TradyStrat/Features/Trades/TradesPage.razor.cs
git commit -m "refactor(trades): extract TradesPage code-behind"
```

---

## Task 10: `DashboardPage`

**Files:**
- Create: `TradyStrat/Features/Dashboard/DashboardPage.razor.cs`
- Modify: `TradyStrat/Features/Dashboard/DashboardPage.razor`

- [ ] **Step 1: Create `DashboardPage.razor.cs`**

```csharp
using Microsoft.AspNetCore.Components;
using TradyStrat.Application.Abstractions;
using TradyStrat.Application.UseCases.AiSuggestion;
using TradyStrat.Application.UseCases.Dashboard;
using TradyStrat.Application.UseCases.Prices;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Features.Dashboard;

public partial class DashboardPage : ComponentBase
{
    [Inject] private LoadDashboardUseCase LoadDashboard { get; set; } = default!;
    [Inject] private ForceRefetchSuggestionUseCase ForceRefetch { get; set; } = default!;
    [Inject] private RefreshAllPricesUseCase RefreshPrices { get; set; } = default!;

    private DashboardViewModel? _vm;
    private string? _error;
    private bool _busy;
    private bool _showRerunConfirm;

    protected override async Task OnInitializedAsync() => await Reload();

    private async Task Reload()
    {
        try
        {
            _vm = await LoadDashboard.ExecuteAsync(Unit.Value, CancellationToken.None);
            _error = null;
        }
        catch (TradyStratException ex)
        {
            _vm = null;
            _error = ex.Message;
        }
    }

    private async Task OnRefreshClicked()
    {
        _busy = true;
        try   { await RefreshPrices.ExecuteAsync(Unit.Value, CancellationToken.None); await Reload(); }
        finally { _busy = false; }
    }

    private void OnRerunRequested() => _showRerunConfirm = true;

    private async Task ConfirmRerun()
    {
        _showRerunConfirm = false;
        _busy = true;
        try   { await ForceRefetch.ExecuteAsync(Unit.Value, CancellationToken.None); await Reload(); }
        finally { _busy = false; }
    }

    private void OnLogTradeRequested()
    {
        // Phase 7 wires this to /trades navigation or an inline dialog.
    }
}
```

Note: original code referenced `Shared.Exceptions.TradyStratException` with partial qualification. The `.cs` version adds `using TradyStrat.Shared.Exceptions;` and uses the unqualified type — cleaner and equivalent.

- [ ] **Step 2: Replace `DashboardPage.razor` content**

Final file contents:

```razor
@page "/"
@rendermode InteractiveServer
@using TradyStrat.Features.Dashboard.Components

@if (_vm is null && _error is null)
{
    <p style="padding:48px 56px;color:var(--vault-gold);font-family:var(--font-mono);
              letter-spacing:0.32em;text-transform:uppercase">Loading…</p>
}
else if (_error is not null)
{
    <div style="padding:48px 56px;font-family:var(--font-mono);max-width:720px">
        <p style="color:var(--vault-red);letter-spacing:0.32em;text-transform:uppercase;font-size:11px;margin-bottom:14px">
            Could not load dashboard
        </p>
        <p style="color:rgba(236,230,214,0.78);font-family:var(--font-display);font-size:16px;line-height:1.55;margin-bottom:22px">
            @_error
        </p>
        <p style="color:rgba(236,230,214,0.5);font-size:12px">
            Common cause: no price bars for CON3.L (Leverage Shares 3x Long Coinbase ETP, LSE) in the database. Run with internet so the price feed can warm the cache, then refresh.
        </p>
        <button class="btn" style="margin-top:18px;padding:10px 16px;background:var(--vault-gold);color:#1B1710;font-family:var(--font-mono);font-size:10px;letter-spacing:0.26em;text-transform:uppercase;font-weight:600"
                @onclick="OnRefreshClicked" disabled="@_busy">
            @(_busy ? "Refreshing…" : "Retry")
        </button>
    </div>
}
else
{
    var vm = _vm!;
    <VaultMasthead Today="vm.Today" EntryNumber="vm.EntryNumber" />
    <div class="hero-row">
        <HeroCapital Snap="vm.Portfolio" Goal="vm.Goal" Today="vm.Today" />
        <TodaysCallCard Sug="vm.TodaysCall" Today="vm.Today"
                        OnRerun="OnRerunRequested" OnLogTrade="OnLogTradeRequested" />
    </div>
    <PortfolioRail Snap="vm.Portfolio" Tickers="vm.Tickers" />
    <GrowthChart Points="vm.Growth" Goal="vm.Goal" />
    <RefreshFab Busy="_busy" OnClick="OnRefreshClicked" />

    @if (_showRerunConfirm)
    {
        <div class="modal" @onclick="() => _showRerunConfirm = false">
            <div class="modal-body" @onclick:stopPropagation="true">
                <h3>Re-run AI?</h3>
                <p>This will use one Anthropic API call. Continue?</p>
                <div class="modal-actions">
                    <button class="btn" @onclick="ConfirmRerun">Confirm</button>
                    <button class="btn ghost" @onclick="() => _showRerunConfirm = false">Cancel</button>
                </div>
            </div>
        </div>
    }
}

<style>
    .modal {
        position: fixed; inset: 0;
        background: rgba(0,0,0,0.7);
        display: flex; align-items: center; justify-content: center;
        z-index: 1000;
    }
    .modal-body {
        background: var(--vault-bg-2);
        border: 1px solid var(--vault-rule);
        padding: 32px; max-width: 420px;
    }
    .modal-body h3 {
        font-family: var(--font-display); font-style: italic;
        font-size: 28px; margin-bottom: 12px;
    }
    .modal-body p { color: rgba(236,230,214,0.78); margin-bottom: 22px; }
    .modal-actions { display: flex; gap: 10px; justify-content: flex-end; }
    .btn {
        padding: 10px 16px; background: var(--vault-gold);
        color: #1B1710; font-family: var(--font-mono); font-size: 10px;
        letter-spacing: 0.26em; text-transform: uppercase; font-weight: 600;
    }
    .btn.ghost { background: transparent; color: var(--vault-ivory);
                 border: 1px solid var(--vault-rule); font-weight: 400; }
</style>
```

(Keeps `@page`, `@rendermode`, the `@using TradyStrat.Features.Dashboard.Components` for the 6 child component tags, the inline `<style>` block. Drops 4 `@using` lines used only by C# code. Drops 3 `@inject` directives. Removes `@code` block.)

- [ ] **Step 3: Build**

Run: `dotnet build TradyStrat.slnx`
Expected: 0 errors, no new warnings.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Dashboard/DashboardPage.razor TradyStrat/Features/Dashboard/DashboardPage.razor.cs
git commit -m "refactor(dashboard): extract DashboardPage code-behind"
```

---

## Task 11: Final verification

**Files:** none

- [ ] **Step 1: Confirm no `@code` blocks remain in scope-listed components**

Run:
```bash
grep -l '^@code' TradyStrat/Features/Dashboard/DashboardPage.razor \
    TradyStrat/Features/Dashboard/Components/HeroCapital.razor \
    TradyStrat/Features/Dashboard/Components/GrowthChart.razor \
    TradyStrat/Features/Dashboard/Components/PortfolioRail.razor \
    TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor \
    TradyStrat/Features/Dashboard/Components/RefreshFab.razor \
    TradyStrat/Features/Dashboard/Components/VaultMasthead.razor \
    TradyStrat/Features/Trades/TradesPage.razor \
    TradyStrat/Features/Trades/Components/AddTradeDialog.razor \
    TradyStrat/Features/Settings/SettingsPage.razor
```
Expected: no output (grep prints nothing because none of the files contain `@code` lines).

- [ ] **Step 2: Confirm all 10 code-behind files exist**

Run:
```bash
ls TradyStrat/Features/Dashboard/DashboardPage.razor.cs \
   TradyStrat/Features/Dashboard/Components/HeroCapital.razor.cs \
   TradyStrat/Features/Dashboard/Components/GrowthChart.razor.cs \
   TradyStrat/Features/Dashboard/Components/PortfolioRail.razor.cs \
   TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.cs \
   TradyStrat/Features/Dashboard/Components/RefreshFab.razor.cs \
   TradyStrat/Features/Dashboard/Components/VaultMasthead.razor.cs \
   TradyStrat/Features/Trades/TradesPage.razor.cs \
   TradyStrat/Features/Trades/Components/AddTradeDialog.razor.cs \
   TradyStrat/Features/Settings/SettingsPage.razor.cs
```
Expected: all 10 paths listed (no "No such file or directory").

- [ ] **Step 3: Full clean build**

Run: `dotnet build TradyStrat.slnx --no-incremental`
Expected: 0 errors. Compare warning count to the Task 0 baseline — should be the same.

- [ ] **Step 4: Test suite**

Run: `dotnet test TradyStrat.slnx`
Expected: All tests pass (count matches Task 0 baseline).

- [ ] **Step 5: Manual smoke test**

Run: `dotnet run --project TradyStrat`

Open the served URL and verify:
- `/` (Dashboard) renders the masthead, hero, today's call, rail, chart, refresh FAB.
- Click the refresh FAB → spinner shows, prices reload.
- Click "Re-run AI" on the call card → confirmation modal opens; Cancel dismisses; Confirm triggers a refetch.
- Navigate to `/trades` → ledger renders; "+ Add trade" opens the dialog; submitting adds a row; Edit reopens the same dialog with prefilled values; Delete removes the row.
- "Import CSV" opens the import modal and accepts a CSV string.
- Navigate to `/settings` → goal target/date/last-updated render; Save updates the value.

Stop the dev server (Ctrl-C).

- [ ] **Step 6: Final review of git log**

Run: `git log --oneline -n 11`
Expected: 10 component commits + this plan's design commit visible (or whatever pre-existed at HEAD).

No commit needed for this task.
