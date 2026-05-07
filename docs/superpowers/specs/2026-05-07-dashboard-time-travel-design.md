# TradyStrat — Dashboard time-travel navigation

**Status:** Design approved · Ready for implementation plan
**Date:** 2026-05-07
**Author:** Philippe Matray (with Claude)
**Depends on:** [`2026-05-06-tradystrat-dashboard-design.md`](./2026-05-06-tradystrat-dashboard-design.md), [`2026-05-07-tradystrat-dashboard-depth-design.md`](./2026-05-07-tradystrat-dashboard-depth-design.md)

---

## 1. Purpose & goal

The dashboard reflects "today" only. Every prior day's view — the prices, indicators, AI call, portfolio snapshot — is silently overwritten on the next refresh. This spec turns the dashboard into a navigable journal: each past trading day becomes a permanent, addressable, read-only entry.

Concrete user-facing additions:

- A `?on=YYYY-MM-DD` query parameter on `/` that pins the dashboard to a past trading day.
- Prev / next day buttons in the masthead that step trading-day to trading-day.
- A clickable date pill that opens a calendar picker for direct jumps.
- A "return to today" link that appears only when the user has drifted off live mode.
- Keyboard shortcuts (`←` / `→`) mirroring the prev / next buttons.
- Browser back / forward stepping naturally through the user's viewing path (free side-effect of URL-driven state).

Out of scope (deliberately) — see §11.

## 2. Decisions at a glance

| Concern | Decision |
|---|---|
| Scope of time travel | **Dashboard only.** `/trades` and `/settings` stay live. |
| Read-only when historical | **Strict.** `RefreshFab`, "Re-run AI", "Log trade" are not rendered. |
| Mode rule | `IsHistorical = (?on= was supplied and parsed successfully)`. URL presence is the state. Bare `/` is live; any valid `/?on=…` is historical. |
| Step granularity | **Trading days only.** Closed days are skipped by prev / next; the date picker rounds non-trading days to the nearest *earlier* trading day. |
| URL contract | Query parameter `?on=YYYY-MM-DD`. Bare `/` means today (live). |
| Invalid / future / pre-floor dates | Server-side redirect to a canonical URL — never a rendered error. |
| Floor | Earliest stored CON3.L price bar. No separate config. |
| Ceiling | Latest stored CON3.L price bar (the most recent trading day). Live mode is "calendar today" but resolves data via existing `*AsOf` queries that handle the gap on weekends/holidays. |
| Schema changes | **None.** All required data already lives in `PriceBars` and `Suggestions`. |
| New service | `IEntryNavigationService` — earliest / latest / prev / next / resolve-or-fallback. |
| Module wiring | `IEntryNavigationService` is registered inside the existing `Modules/DashboardModule.cs`. No new module file. |
| Use case input | `LoadDashboardUseCase` becomes `IUseCase<LoadDashboardInput, DashboardViewModel>`. The `LoadDashboardInput` record carries the resolved `TargetDate`. |
| Use case internal change | Switch existing call sites to the `asOf` overloads that already exist (`IndicatorEngine.ComputeFor(...,DateOnly,...)`, `IndicatorEngine.HistoryFor(...,DateOnly,...)`, `PortfolioService.SnapshotAsync(asOf,...)`). |
| Today's-call lookup | In **historical** mode, query `SuggestionForDateSpec(targetDate)` directly via the suggestion repo — **bypass** `GetTodaysSuggestionUseCase` so a missing past suggestion does not trigger an AI call. Live mode keeps using `GetTodaysSuggestionUseCase` unchanged. |
| Entry-no. computation | `TradesAsOfSpec(targetDate).CountAsync` replaces `AllTradesSpec.CountAsync` on the dashboard. The existing `TradesAsOfSpec` is reused — no new spec class. |
| Trade-count divergence | `TradesPage` and `SettingsPage` continue to use `AllTradesSpec().CountAsync` for their masthead. The dashboard's count diverges from those only if a trade is logged with a future `ExecutedOn`. Documented in §11; harmonization is out of scope. |
| "entry no." rename | **Not now.** The label is currently misnamed (it counts trades, not entries) but renaming is out of scope. Spec records the debt. |
| Test framework | xunit.v3 + Shouldly + EF Core InMemory. **No bUnit.** Validation logic lives in a pure helper (`OnParamValidator`) that is unit-testable without the renderer. |

## 3. URL contract

```
/                    → Dashboard, live mode (today). No query param.
/?on=2026-04-15      → Dashboard, historical mode pinned to that trading day.
/trades              → Unchanged.
/settings            → Unchanged.
```

**Validation order** — `OnParamValidator.Validate(string?, IEntryNavigationService) → ValidationResult` (a sum-type of `Live | Historical(date) | RedirectTo(url)`), invoked from `DashboardPage.OnParametersSetAsync`:

1. `on` missing or empty → `Live`.
2. `on` not parsable as `yyyy-MM-dd` → `RedirectTo("/")`. Logged at warning level. Not surfaced to the user.
3. `on` parses but is **after the latest trading day** → `RedirectTo("/")`.
4. `on` parses but is **before the earliest trading day** → `RedirectTo($"/?on={earliest}")`.
5. `on` parses but is **not a trading day** (no price bar for CON3.L on that date) → `RedirectTo($"/?on={nearest earlier trading day}")`.
6. Otherwise → `Historical(date)`.

The page calls `NavigationManager.NavigateTo(url, replace: true)` for any `RedirectTo`, then short-circuits the data load. `Live` and `Historical(date)` both proceed to load the dashboard via `LoadDashboardUseCase` — they differ only in the `TargetDate` passed and in `IsHistorical` on the resulting view model.

**Why redirects, not error renders:** every URL the user can copy / bookmark / share resolves to a canonical, valid trading day. Browser back / forward stays predictable; nothing breaks on stale links.

**Unknown query params** are ignored. Adding `?range=3m` or `?focus=COIN` later (out of scope here) doesn't conflict — Blazor's `[SupplyParameterFromQuery]` only binds named params, leaving the rest untouched.

## 4. Patterns inventory

GoF labels are applied only where they hold strictly. Components without a clean GoF mapping are listed without a forced label.

| Component | Pattern | Notes |
|---|---|---|
| `IEntryNavigationService` | **Facade** | Single interface fronting five spec-backed queries against `PriceBars`. Could equally be called a domain service with no label; Facade is the closest honest GoF fit. |
| New `*Spec.cs` classes (`EarliestPriceBarSpec`, `PriceBarBeforeSpec`, `PriceBarAfterSpec`) | **Specification** (Ardalis) | The Specification pattern lives here, in the spec classes themselves — not in the service that uses them. |
| `ResolveOrFallbackAsync` | — | Returns a fallback `DateOnly` so callers never deal with `null` for closed-day input. This is a total function, not the GoF Null Object pattern (which provides a do-nothing **object** for a non-null seam). No label. |
| `OnParamValidator.Validate` | — | Pure function returning a sum-typed `ValidationResult`. Sequential `if/else if` over `?on=`. Not Chain of Responsibility — that pattern requires handler objects that decide whether to handle or pass. No label. |
| `LoadDashboardUseCase` | **Command + Template Method** *(unchanged)* | Existing pattern preserved. Input record now `LoadDashboardInput(TargetDate)` instead of `Unit`. |
| `UseCaseBase<,>` *(unchanged)* | **Template Method** | Existing. |
| Existing `TradesAsOfSpec`, `SuggestionForDateSpec`, etc. *(unchanged)* | **Specification** | Existing. Reused for the as-of queries — no duplicates introduced. |

## 5. Project layout — additions

### 5.1 New production files

| File | Layer | Purpose |
|---|---|---|
| `Features/Dashboard/Navigation/IEntryNavigationService.cs` | Domain | Five-method interface (see §6). |
| `Features/Dashboard/Navigation/EntryNavigationService.cs` | Domain | Spec-backed implementation. |
| `Features/Dashboard/Navigation/OnParamValidator.cs` | Domain | Pure function `Validate(string? onParam, IEntryNavigationService nav, CancellationToken ct) → ValidationResult`. Returns one of `Live`, `Historical(DateOnly)`, `RedirectTo(string url)`. Tested in isolation (no bUnit). |
| `Features/Dashboard/Navigation/ValidationResult.cs` | Domain | Sum-type as a sealed abstract record + three subtypes (or an `OneOf`-style discriminated union — implementer's choice). |
| `Features/Dashboard/UseCases/LoadDashboardInput.cs` | Use case | `record LoadDashboardInput(DateOnly TargetDate, bool IsHistorical);` |
| `Common/Exceptions/NoTradingDaysException.cs` | Domain | Thrown by `EntryNavigationService.EarliestAsync` / `LatestAsync` when the price-bar table is empty. Inherits `TradyStratException`. |
| `Features/PriceFeed/Specifications/EarliestPriceBarSpec.cs` | Persistence | Top-1 ascending by `Date` for a given ticker. |
| `Features/PriceFeed/Specifications/PriceBarBeforeSpec.cs` | Persistence | Top-1 desc where `Date < input`. |
| `Features/PriceFeed/Specifications/PriceBarAfterSpec.cs` | Persistence | Top-1 asc where `Date > input`. |
| `wwwroot/js/dashboard-keys.js` | UI | ES module: subscribes to `keydown` on `document`, invokes Blazor `[JSInvokable]` `OnPrev` / `OnNext` on the dashboard's `DotNetObjectReference`. Short-circuits when `document.activeElement` is editable. Imported in `OnAfterRenderAsync(firstRender: true)` and explicitly torn down in `DisposeAsync`. |

### 5.2 Modified production files

| File | Change |
|---|---|
| `Modules/DashboardModule.cs` | Register `IEntryNavigationService → EntryNavigationService` (scoped). |
| `Features/Dashboard/DashboardPage.razor` | Add `[SupplyParameterFromQuery(Name="on")] string? OnParam` parameter on the page. Render `RefreshFab` / action buttons only when `!IsHistorical`. |
| `Features/Dashboard/DashboardPage.razor.cs` | Replace `OnInitializedAsync` data-load with `OnParametersSetAsync` that runs `OnParamValidator.Validate(...)`, redirects via `NavigationManager.NavigateTo(url, replace: true)` on `RedirectTo`, otherwise calls `LoadDashboard.ExecuteAsync(new LoadDashboardInput(targetDate), ct)`. Implement `IAsyncDisposable`, `[JSInvokable] OnPrev` / `OnNext`, and the JS-module hookup. Set `_isHistorical` from the validator result. |
| `Features/Dashboard/DashboardViewModel.cs` | Add `IsHistorical`, `PrevTradingDay`, `NextTradingDay`, `EarliestTradingDay`, `LatestTradingDay`. |
| `Features/Dashboard/UseCases/LoadDashboardUseCase.cs` | Generic becomes `UseCaseBase<LoadDashboardInput, DashboardViewModel>`. Replace internal `var today = clock.TodayInExchangeTzFor(...)` with `var target = input.TargetDate`. Switch to `asOf` overloads on `IndicatorEngine.ComputeFor`, `IndicatorEngine.HistoryFor`, `PortfolioService.SnapshotAsync`. Replace `tradeRepo.CountAsync(new AllTradesSpec(), ct)` with `tradeRepo.CountAsync(new TradesAsOfSpec(target), ct)`. Inject `IEntryNavigationService` and populate the four nav fields. Today's-call lookup branches: live mode keeps `await todaysSuggestion.ExecuteAsync(Unit.Value, ct)`; historical mode does `await suggestionRepo.FirstOrDefaultAsync(new SuggestionForDateSpec(target), ct)` — no AI invocation, may be `null`. `IsHistorical` flag flows through to the view model. |
| `Features/Dashboard/Components/VaultMasthead.razor` | Adds prev / next / date-pill / "return to today" controls. Inline date `<input type="date">` (hidden, focused by pill click). |
| `Features/Dashboard/Components/VaultMasthead.razor.cs` | Add parameters per §7.1. Self-hide control row when `EarliestTradingDay`/`LatestTradingDay` are null (the `/trades` and `/settings` cases). |
| `Features/Dashboard/Components/RefreshFab.razor` | Add `[Parameter] public bool Historical { get; set; }`. Early-return `null` render when set (or wrap entire markup in `@if (!Historical)`). |
| `Features/Dashboard/Components/TodaysCallCard.razor` | Add `[Parameter] public bool Historical { get; set; }`. Wrap action buttons in `@if (!Historical)`. Render an empty-state row ("No AI call recorded for {date}.") when `Sug is null` (historical case where the suggestion wasn't ever generated). Title flips to "Call for {date}" when `Historical`. |
| `Features/Dashboard/Components/TodaysCallCard.razor.cs` *(if it exists, otherwise add code-behind)* | Make `Sug` parameter nullable to support the historical-empty case. |
| `wwwroot/css/vault.css` | Styles for `.masthead .nav-step`, `.masthead .date-pill`, `.masthead .return-today`, the disabled state, and the hidden `<input type="date">` shim. |

### 5.3 Reuse — already in the codebase, no new file

| Existing artifact | Used for |
|---|---|
| `LatestPriceBarSpec(ticker)` | `EntryNavigationService.LatestAsync`. |
| `PriceBarsAsOfSpec(ticker, asOf)` (lists all on-or-before, ascending) | `EntryNavigationService.ResolveOrFallbackAsync` — `LastOrDefaultAsync` returns the nearest earlier (or exact) trading day in one query. Avoids adding a top-1 `OnOrBefore` spec. |
| `TradesAsOfSpec(asOfInclusive)` | Dashboard masthead "entry no." count. |
| `SuggestionForDateSpec(date)` | Historical-mode suggestion lookup. |
| `IndicatorEngine.ComputeFor(ticker, DateOnly asOf, ct)` overload | Replaces the no-asOf overload in `LoadDashboardUseCase`. |
| `IndicatorEngine.HistoryFor(ticker, kind, lastN, DateOnly asOf, ct)` overload | Replaces the no-asOf overload. |
| `PortfolioService.SnapshotAsync(asOf, focusPriceEur, targetEur, ct)` overload | Replaces the no-asOf overload. |
| `SnapshotFactory.CreateAsync(asOf, ct)` *(unchanged)* | Already date-parameterized; not a path the dashboard hits in historical mode (we don't regenerate AI calls). |

## 6. `IEntryNavigationService` contract

```csharp
public interface IEntryNavigationService
{
    Task<DateOnly>  EarliestAsync(CancellationToken ct);
    Task<DateOnly>  LatestAsync(CancellationToken ct);
    Task<DateOnly?> PreviousAsync(DateOnly current, CancellationToken ct);
    Task<DateOnly?> NextAsync(DateOnly current, CancellationToken ct);
    Task<DateOnly>  ResolveOrFallbackAsync(DateOnly requested, CancellationToken ct);
}
```

**Semantics:**

- `EarliestAsync` queries the new `EarliestPriceBarSpec("CON3.L")`. Throws `NoTradingDaysException` if the table is empty for that ticker.
- `LatestAsync` queries the existing `LatestPriceBarSpec("CON3.L")`. Throws `NoTradingDaysException` if empty.
- `PreviousAsync(current)` queries the new `PriceBarBeforeSpec("CON3.L", current)`. Returns `null` when `current` is the floor.
- `NextAsync(current)` queries the new `PriceBarAfterSpec("CON3.L", current)`. Returns `null` when `current` is the ceiling.
- `ResolveOrFallbackAsync(requested)` queries the existing `PriceBarsAsOfSpec("CON3.L", requested)` and takes the last element (i.e., the most recent on-or-before). Returns the requested date when a bar exists for it; otherwise the nearest earlier trading day. Throws `NoTradingDaysException` only when nothing earlier exists either.

**Caching:** none initially. SQLite + indexed `Date` column makes these sub-millisecond. Add a memory cache only if profiling proves it necessary.

**Why a service rather than inline LINQ:** three callers — page-load validation (`OnParamValidator`), the masthead's prev/next button enablement (via `LoadDashboardUseCase`), and the date picker's "round to nearest trading day" landing (also via the validator). Centralizing the trading-day query keeps spec reuse coherent.

## 7. Component contracts

### 7.1 `VaultMasthead` — new parameters

```csharp
[Parameter] public DateOnly? PrevTradingDay { get; set; }
[Parameter] public DateOnly? NextTradingDay { get; set; }
[Parameter] public DateOnly? EarliestTradingDay { get; set; }
[Parameter] public DateOnly? LatestTradingDay { get; set; }
[Parameter] public bool      IsHistorical { get; set; }
[Parameter] public EventCallback<DateOnly> OnDateSelected { get; set; }
```

**Self-hiding rule:** the date control renders only when `EarliestTradingDay` and `LatestTradingDay` are both non-null. `/trades` and `/settings` pass none of these and the control disappears — no separate masthead variant.

**Layout:** `‹` button · clickable date pill · `›` button · optional "return to today" link, all on the same horizontal row as the existing nav links and meta text. `‹` is `disabled` when `PrevTradingDay` is null; `›` is `disabled` when `NextTradingDay` is null.

**Date pill:** clicking it focuses a hidden `<input type="date" min="{earliest}" max="{latest}">`. The picker's `change` event fires `OnDateSelected`.

**"Return to today":** rendered only when `IsHistorical` is true. Clicking it navigates to `/`.

### 7.2 `RefreshFab`, `TodaysCallCard` — new parameter

Both gain `[Parameter] public bool Historical { get; set; }`.

- `RefreshFab` early-returns from its render fragment when `Historical`.
- `TodaysCallCard` wraps the action buttons in `@if (!Historical) { … }` and switches its heading copy to "Call for {date}".

### 7.3 `DashboardPage` — validation flow

The page replaces its current `OnInitializedAsync` data-load with `OnParametersSetAsync`, so query-param changes re-trigger the load:

```csharp
protected override async Task OnParametersSetAsync()
{
    var result = await OnParamValidator.Validate(OnParam, Nav, _ct);

    bool isHistorical;
    DateOnly targetDate;
    switch (result)
    {
        case RedirectTo r:
            NavigationManager.NavigateTo(r.Url, replace: true);
            return;
        case Live:
            isHistorical = false;
            targetDate   = Clock.TodayInExchangeTzFor("CON3.L"); // existing convention; asOf queries downstream handle the closed-day gap
            break;
        case Historical h:
            isHistorical = true;
            targetDate   = h.Date;
            break;
    }

    _vm = await LoadDashboard.ExecuteAsync(
        new LoadDashboardInput(targetDate, isHistorical), _ct);
}
```

> *Pseudo-code; concrete implementation matches existing busy/error handling around the existing `Reload()` helper.*

**Navigation history rules:**

- **Validation redirects** (steps 2–5 of §3): `NavigateTo(url, replace: true)`. Invalid URLs never appear in browser history; the user's back button doesn't trip on them.
- **User-initiated navigation** — clicking `‹` / `›`, picking a date in the picker, clicking "return to today", and the keyboard handler — all use the default `NavigateTo(url)` (push). Each step is a fresh history entry, so browser ◀ / ▶ retraces the user's actual viewing path.

### 7.4 Keyboard

`wwwroot/js/dashboard-keys.js` attaches a `keydown` listener to **`document`** (not the dashboard root — element-level listeners only fire when that element or a descendant has focus, which is unreliable for a page-level shortcut). The listener:

1. Returns early if `document.activeElement` matches `input, textarea, select, [contenteditable=""], [contenteditable="true"]`.
2. On `ArrowLeft`, calls `dotNetRef.invokeMethodAsync('OnPrev')`.
3. On `ArrowRight`, calls `dotNetRef.invokeMethodAsync('OnNext')`.

`DashboardPage` implements `IAsyncDisposable`. In `OnAfterRenderAsync(firstRender: true)` it imports the module, creates a `DotNetObjectReference<DashboardPage>`, and calls the module's `attach(dotNetRef)` which records both the ref and the `removeEventListener` cleanup function. In `DisposeAsync` it calls the module's `detach()` (which calls `removeEventListener` and disposes the `DotNetObjectReference`). Because the listener is on `document`, explicit teardown is mandatory — the listener does not auto-clean when the component unmounts.

`OnPrev` and `OnNext` are `[JSInvokable]` methods on `DashboardPage` that share the same handlers as the masthead's `‹` / `›` button clicks (a `NavigateTo` to the prev / next URL).

## 8. Use case change

The current `LoadDashboardUseCase` is `IUseCase<Unit, DashboardViewModel>` and hardcodes `var today = clock.TodayInExchangeTzFor("CON3.L")` internally. The required changes:

**Signature change:**

- Add `record LoadDashboardInput(DateOnly TargetDate, bool IsHistorical)` (new file).
- Generic becomes `UseCaseBase<LoadDashboardInput, DashboardViewModel>`.
- Rename internal `today` → `target` and read it from `input.TargetDate`.

**Switch to existing as-of overloads** (no new infrastructure):

| Current call | Replacement |
|---|---|
| `indicators.ComputeFor(ticker, ct)` | `indicators.ComputeFor(ticker, target, ct)` |
| `indicators.HistoryFor(c.Ticker, kind.Value, SparklineWindow, ct)` | `indicators.HistoryFor(c.Ticker, kind.Value, SparklineWindow, target, ct)` |
| `portfolio.SnapshotAsync(focusPriceEur ?? 0m, goal.TargetEur, ct)` | `portfolio.SnapshotAsync(target, focusPriceEur ?? 0m, goal.TargetEur, ct)` |
| `tradeRepo.CountAsync(new AllTradesSpec(), ct)` | `tradeRepo.CountAsync(new TradesAsOfSpec(target), ct)` |
| `priceRepo.FirstOrDefaultAsync(new LatestPriceBarSpec(FocusTicker), ct)` | unchanged — `LatestPriceDate` on the view model continues to mean "the data freshness of the displayed snapshot." |
| `growth.BuildAsync(FocusTicker, ct)` | unchanged for now. Documented limitation: in historical mode the growth chart's right edge will still be the latest stored bar, not `target`. Acceptable for v1; flagged in §11. |

**Today's-call lookup branch** (replacing the unconditional `todaysSuggestion.ExecuteAsync(Unit.Value, ct)`):

```csharp
Suggestion? call;
if (input.IsHistorical)  // see "IsHistorical plumbing" below
{
    call = await suggestionRepo.FirstOrDefaultAsync(new SuggestionForDateSpec(target), ct);
}
else
{
    call = await todaysSuggestion.ExecuteAsync(Unit.Value, ct);
}
```

This keeps the live path identical and prevents the AI from being invoked for missing past suggestions. `Suggestion?` is now nullable on the consumer side; `TodaysCallCard` renders an empty state when null.

**`IsHistorical` plumbing.** The page already knows `IsHistorical` (validator output). It's part of `LoadDashboardInput`:

```csharp
public sealed record LoadDashboardInput(DateOnly TargetDate, bool IsHistorical);
```

The use case forwards `IsHistorical` to the view model. It's also used to guard the AI branch above and to skip the existing fire-and-forget backfill enqueue (lines 122–132 of the current `LoadDashboardUseCase.cs`), which should not run when viewing a past entry.

**New navigation fields.** Inject `IEntryNavigationService`. Compute `Earliest`, `Latest`, `Prev`, `Next` for `target`. Populate the new view-model fields (§5.2 row for `DashboardViewModel`).

**Skipped in historical mode:**

- The fire-and-forget `EnsureBackfilledAsync` chain.
- The AI invocation in `GetTodaysSuggestionUseCase` (already covered).
- The `PriceFeedHostedService` warm path is unaffected (it's a startup-time hosted service, not a per-load call).

**`growth.BuildAsync(FocusTicker, ct)` historical limitation.** The current `GrowthSeriesBuilder` always builds from earliest to latest stored bar. In historical mode the growth chart will therefore still show "today's" right-edge value — visually consistent but technically extends past the viewed date. Adding an `asOf` overload to `GrowthSeriesBuilder` is **out of scope** here; flagged in §11.

No other use case changes. Existing live-mode call sites must be updated to pass `LoadDashboardInput(clock.TodayInExchangeTzFor("CON3.L"), IsHistorical: false)`.

## 9. Edge cases

| Input / state | Behavior |
|---|---|
| `?on=` missing or empty | Live mode. |
| `?on=foo` (unparsable) | Redirect `/` (replace: true). Logged warning. |
| `?on=2026-12-31` (future) | Redirect `/`. |
| `?on=2020-01-01` (before earliest) | Redirect `/?on={earliest}`. |
| `?on=2026-04-12` (Sunday — closed) | Redirect `/?on={prev trading day}`. |
| `?on=2026-04-15` (valid trading day) | Render historical. |
| Price-bar table empty (cold-start before warm-up) | Existing "Could not load dashboard" error block renders. No nav surface yet. |
| User at floor clicks `‹` | No-op; button is `disabled`. |
| User at ceiling clicks `›` | No-op; button is `disabled`. "Return to today" hidden. |
| User on input field presses `←` / `→` | No-op (handler short-circuits on editable focus). |

## 10. Testing

xunit.v3 + Shouldly + EF Core InMemory. **No bUnit**. Page-level redirect logic is testable as a pure function via `OnParamValidator`; the page itself stays a thin shell.

### 10.1 Unit — `EntryNavigationService`

In-memory `AppDbContext` seeded with a known trading-day calendar (e.g., Mon Apr 13, Tue 14, Wed 15, Thu 16, Fri 17 — skipping the Apr 18–19 weekend, then Mon 20).

- `EarliestAsync` / `LatestAsync` return `Min` / `Max` of `Date` for the focus ticker.
- `PreviousAsync(Mon Apr 20)` returns Fri Apr 17 (gap-skip).
- `PreviousAsync(Mon Apr 13)` returns `null` (floor).
- `NextAsync(Fri Apr 17)` returns Mon Apr 20.
- `NextAsync(Mon Apr 20)` returns `null` (ceiling).
- `ResolveOrFallbackAsync(Sun Apr 19)` returns Fri Apr 17.
- `ResolveOrFallbackAsync(Wed Apr 15)` returns Wed Apr 15 (input is itself a trading day).
- `ResolveOrFallbackAsync(any date before earliest)` throws `NoTradingDaysException`.
- `EarliestAsync` / `LatestAsync` on empty DB throws `NoTradingDaysException`.

### 10.2 Unit — `OnParamValidator`

`IEntryNavigationService` is mocked (or fake-implemented) with a fixed earliest = Apr 13 and latest = Apr 20.

| Input | Expected `ValidationResult` |
|---|---|
| `null` | `Live` |
| `""` | `Live` |
| `"foo"` | `RedirectTo("/")` |
| `"2026-04-25"` (after latest) | `RedirectTo("/")` |
| `"2026-04-10"` (before earliest) | `RedirectTo("/?on=2026-04-13")` |
| `"2026-04-19"` (Sunday — closed) | `RedirectTo("/?on=2026-04-17")` |
| `"2026-04-15"` (valid) | `Historical(2026-04-15)` |

### 10.3 Unit — `LoadDashboardUseCase` (extends existing tests)

- `LoadDashboardInput(target, IsHistorical: true)` → loads price bar / suggestion / portfolio **for that date** via the as-of overloads; sets `IsHistorical = true` on view model; populates `PrevTradingDay` / `NextTradingDay`.
- `LoadDashboardInput(target, IsHistorical: false)` with `target == latest trading day` → uses live path (`GetTodaysSuggestionUseCase`), `IsHistorical = false`, `NextTradingDay = null`.
- Historical mode does **not** call `GetTodaysSuggestionUseCase` (verified via a fake / counting decorator).
- Historical mode does **not** enqueue the backfill chain.
- Masthead `EntryNumber` reflects `TradesAsOfSpec(target).CountAsync`, not the global count.
- Historical mode where `SuggestionForDateSpec(target)` returns null → view model carries `TodaysCall = null` (no exception, no AI invocation).
- Existing live-mode tests stay green (regression guard).

### 10.4 Manual smoke — required before declaring done

1. `dotnet run`, open `/`. Confirm live mode, click `‹` once → URL becomes `/?on={prev}`, dashboard updates, FAB gone, "Re-run AI" / "Log trade" gone, "return to today" link visible.
2. Click date pill, pick a Sunday → page redirects to nearest Friday.
3. Click "return to today" → URL collapses to `/`, FAB returns.
4. Browser back arrow → returns to historical view; forward → returns to today.
5. `←` / `→` keys step through dates; `‹` / `›` `disabled` at boundaries; pressing `←` / `→` while focused inside the date `<input>` does **not** navigate (focus short-circuit works).
6. Type `?on=garbage` manually → redirects cleanly to `/`.
7. Visit `/trades` and `/settings` → masthead has no date control; behavior unchanged.
8. Navigate to a historical day that has **no stored AI suggestion** → `TodaysCallCard` shows the empty state, no AI call fires (verify via logs).

## 11. Out of scope (deliberate)

- Time travel for `/trades` and `/settings`.
- Editing past entries (re-running AI, logging a trade dated in the past via the dashboard, refreshing prices retroactively). Past entries are immutable; the existing `BackfillCoordinator` covers missing-AI-call backfill at first-load.
- A "summary diff" entry view (price move / trades logged / AI's call vs. outcome on that day) — proposed and rejected for this round.
- Additional URL filters (`?range=3m`, `?focus=COIN`). The query-param shape is forward-compatible with these but they're not added now.
- Renaming "entry no." to honestly reflect that it's a trade count. Tracked here as known semantic debt.
- Caching of trading-day lookups. Add only if profiling shows the need.
- **Trade-count harmonization across pages.** `TradesPage` and `SettingsPage` continue to call `AllTradesSpec().CountAsync` (showing all trades including any future-dated). The dashboard, post-change, calls `TradesAsOfSpec(target).CountAsync`. The two diverge only if a trade has a future `ExecutedOn` — unlikely in practice. Harmonization (adopting `TradesAsOfSpec(today)` everywhere or another rule) is deferred.
- **Native `<input type="date">` styling.** The browser's default picker doesn't blend with the Vault aesthetic. We accept the visual inconsistency for v1 rather than ship a custom calendar component. Listed as known UX debt.
- **`GrowthSeriesBuilder` as-of view.** In historical mode, the growth chart's right edge will still be the latest stored bar, not `target`. The visible curve will look identical to live mode. Adding an `asOf` overload to `GrowthSeriesBuilder` is deliberately deferred — the dashboard's hero number, portfolio rail, and call card will all be correctly as-of `target`, which is what the user came to see.
- **`OnLogTradeRequested` is a no-op stub today** (`DashboardPage.razor.cs:54-57`). Hiding it when historical is correct, but it's not yet wired to anything; this spec doesn't change that.

## 12. Acceptance

This spec is implementation-ready when:

- All decisions in §2 are concrete (no TBDs).
- Every file in §5.1 has a clear owner layer and purpose.
- Every edge case in §9 has a defined behavior.
- Tests in §10 cover both happy paths and each redirect branch.
