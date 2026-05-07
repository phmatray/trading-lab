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
| Step granularity | **Trading days only.** Closed days are skipped by prev / next; the date picker rounds non-trading days to the nearest *earlier* trading day. |
| URL contract | Query parameter `?on=YYYY-MM-DD`. Bare `/` means today (live). |
| Invalid / future / pre-floor dates | Server-side redirect to a canonical URL — never a rendered error. |
| Floor | Earliest stored CON3.L price bar. No separate config. |
| Ceiling | Today's trading day in CON3.L's exchange tz. |
| Schema changes | **None.** All required data already lives in `PriceBars` and `Suggestions`. |
| New service | `IEntryNavigationService` — earliest / latest / prev / next / resolve-or-fallback. |
| Module wiring | `IEntryNavigationService` is registered inside the existing `Modules/DashboardModule.cs`. No new module file. |
| Use case change | `LoadDashboardUseCase` accepts a `targetDate` (already had a "today"); resolves prev / next / earliest / latest via the new service. |
| Entry-no. computation | `TradesOnOrBeforeSpec(targetDate)` replaces `AllTradesSpec` for the masthead count. |
| "entry no." rename | **Not now.** The label is currently misnamed (it counts trades, not entries) but renaming is out of scope. Spec records the debt. |
| Test framework | xunit.v3 + Shouldly + EF Core InMemory. bUnit added if not already present, used for page-level redirect tests. |

## 3. URL contract

```
/                    → Dashboard, live mode (today). No query param.
/?on=2026-04-15      → Dashboard, historical mode pinned to that trading day.
/trades              → Unchanged.
/settings            → Unchanged.
```

**Validation order** — applied in `DashboardPage.OnParametersSetAsync`:

1. `on` missing or empty → live mode.
2. `on` not parsable as `yyyy-MM-dd` → redirect to `/`. Logged at warning level. Not surfaced to the user.
3. `on` parses but is **after** today → redirect to `/`.
4. `on` parses but is **before** the earliest trading day → redirect to `/?on={earliest}`.
5. `on` parses but is **not a trading day** (no price bar for CON3.L on that date) → redirect to `/?on={nearest earlier trading day}`.
6. Otherwise → render historical view.

**Why redirects, not error renders:** every URL the user can copy / bookmark / share resolves to a canonical, valid trading day. Browser back / forward stays predictable; nothing breaks on stale links.

## 4. Patterns inventory

| Component | Pattern | Notes |
|---|---|---|
| `IEntryNavigationService` | — (domain service) | Plain interface over five small queries. No GoF label warranted. |
| `EntryNavigationService` | **Specification** (Ardalis) | All five queries are Ardalis specs against `PriceBars`. |
| `ResolveOrFallbackAsync` | **Null Object** (sort of) | Returns the nearest valid trading day rather than throwing on closed-day input — callers never deal with `null`. |
| `DashboardPage` validation chain | **Chain of Responsibility** (light) | Six ordered checks against `?on=`. First match wins; default is "render historical." |
| `LoadDashboardUseCase` | **Command + Template Method** *(unchanged)* | Existing pattern preserved. |

## 5. Project layout — additions

### 5.1 New / modified production files

| File | Layer | Purpose |
|---|---|---|
| `Features/Dashboard/Navigation/IEntryNavigationService.cs` | Domain | Five-method interface (see §6). |
| `Features/Dashboard/Navigation/EntryNavigationService.cs` | Domain | Ardalis-spec-backed implementation. |
| `Common/Exceptions/NoTradingDaysException.cs` | Domain | Thrown when the price-bar table is empty. Inherits `TradyStratException`, lives alongside the existing typed exceptions. |
| `Modules/DashboardModule.cs` *(modify)* | Composition | Registers `IEntryNavigationService → EntryNavigationService` (scoped). No new module file. |
| `Features/Dashboard/DashboardPage.razor` *(modify)* | UI | Adds `[SupplyParameterFromQuery(Name="on")] string? OnParam`. Validation chain. Hides write actions when historical. |
| `Features/Dashboard/DashboardPage.razor.cs` *(modify)* | UI | New `OnParametersSetAsync` validating `OnParam`. New `IsHistorical` field. Keyboard handler. |
| `Features/Dashboard/DashboardViewModel.cs` *(modify)* | UI | Adds `IsHistorical`, `PrevTradingDay`, `NextTradingDay`, `EarliestTradingDay`, `LatestTradingDay`. |
| `Features/Dashboard/UseCases/LoadDashboardUseCase.cs` *(modify)* | Use case | Accepts `targetDate`; calls `IEntryNavigationService` for prev / next / earliest / latest; uses `TradesOnOrBeforeSpec(targetDate)` for entry-no. |
| `Features/Dashboard/Components/VaultMasthead.razor` *(modify)* | UI | Adds prev / next / date-pill / "return to today" controls. |
| `Features/Dashboard/Components/VaultMasthead.razor.cs` *(modify)* | UI | New parameters per §7.1. Self-hides controls when no nav data passed. |
| `Features/Dashboard/Components/RefreshFab.razor` *(modify)* | UI | New `Historical` parameter; early-returns when set. |
| `Features/Dashboard/Components/TodaysCallCard.razor` *(modify)* | UI | New `Historical` parameter; wraps "Re-run AI" / "Log trade" in `@if (!Historical)`; flips title from "Today's call" to "Call for {date}". |
| `Specifications/PriceBars/EarliestPriceBarSpec.cs` *(reuse or add)* | Persistence | Top-1 ascending by `Date`. |
| `Specifications/PriceBars/LatestPriceBarSpec.cs` *(reuse or add)* | Persistence | Top-1 descending by `Date`. |
| `Specifications/PriceBars/PriceBarOnOrBeforeSpec.cs` | Persistence | Top-1 desc where `Date <= input`. |
| `Specifications/PriceBars/PriceBarBeforeSpec.cs` | Persistence | Top-1 desc where `Date < input`. |
| `Specifications/PriceBars/PriceBarAfterSpec.cs` | Persistence | Top-1 asc where `Date > input`. |
| `Specifications/Trades/TradesOnOrBeforeSpec.cs` | Persistence | `Where(t => t.ExecutedOn <= input)` — used for masthead count. |
| `wwwroot/js/dashboard-keys.js` *(new)* | UI | Scoped ES module: subscribes to `←` / `→` on the dashboard root, calls back into Blazor via `DotNet.invokeMethodAsync`. Short-circuits when an editable element has focus. |

### 5.2 Reuse audit

Before adding the five `PriceBars` specs above, check `Specifications/PriceBars/` — `EarliestPriceBarSpec` / `LatestPriceBarSpec` may already exist (depth-design spec lists similar queries). Reuse rather than duplicate; the plan step performs this audit explicitly.

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

- `EarliestAsync` / `LatestAsync` query CON3.L price bars. Throw `NoTradingDaysException` if the table is empty for that ticker.
- `PreviousAsync(current)` returns the latest trading day strictly before `current`, or `null` when `current` is the floor.
- `NextAsync(current)` returns the earliest trading day strictly after `current`, or `null` when `current` is the ceiling.
- `ResolveOrFallbackAsync(requested)` returns `requested` if a price bar exists for that date, otherwise the nearest *earlier* trading day. Throws `NoTradingDaysException` only when nothing earlier exists either.

**Caching:** none initially. SQLite + indexed `Date` column makes these sub-millisecond. Add a memory cache only if profiling proves it necessary.

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

```
OnParametersSetAsync:
  resolved = ValidateOnParam(OnParam)   // either a DateOnly or a redirect
  if (resolved is RedirectTo url) {
    NavigationManager.NavigateTo(url, replace: true);
    return;
  }
  vm = await loadDashboard.ExecuteAsync(new LoadDashboardInput(resolved.Date), ct);
```

**Navigation history rules:**

- **Validation redirects** (steps 2–5 of §3): `replace: true`. Invalid URLs never appear in browser history; the user's back button doesn't trip on them.
- **User-initiated navigation** — clicking `‹` / `›`, picking a date in the picker, clicking "return to today", and the keyboard handler — all use the default `NavigateTo(url)` (push). Each step is a fresh history entry, so browser ◀ / ▶ retraces the user's actual viewing path.

### 7.4 Keyboard

A scoped `wwwroot/js/dashboard-keys.js` module subscribes to `keydown` on the dashboard's root element (not document-global). When `←` / `→` is pressed and `document.activeElement` is not an editable element (`input`, `textarea`, `select`, `[contenteditable]`), it invokes a Blazor `[JSInvokable]` method on `DashboardPage` that re-uses the same prev / next handlers as the buttons.

The module is imported by `DashboardPage` in `OnAfterRenderAsync(firstRender: true)` and torn down in `IAsyncDisposable.DisposeAsync`. Because the listener attaches to the dashboard's root element (not `window`), navigating away from `/` to `/trades` or `/settings` cleanly disposes the listener with the page; navigating back attaches a fresh one.

## 8. Use case change

`LoadDashboardUseCase` already takes an effective "today." The change:

- Rename its date input to `targetDate`.
- After loading the existing dashboard fields for `targetDate`, additionally resolve `EarliestTradingDay`, `LatestTradingDay`, `PrevTradingDay`, `NextTradingDay` via `IEntryNavigationService`.
- Compute `IsHistorical = targetDate < LatestTradingDay`.
- Replace `tradeRepo.CountAsync(new AllTradesSpec())` with `tradeRepo.CountAsync(new TradesOnOrBeforeSpec(targetDate))` so the masthead's "entry no." reflects trades as of the viewed date. Live mode's number is unchanged because today ≥ all stored trades.
- Return all of the above on `DashboardViewModel`.

No other use case changes. Existing live-mode callers pass `targetDate = clock.TodayInExchangeTzFor("CON3.L")` and behave identically.

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

### 10.1 Unit — `EntryNavigationService`

In-memory `AppDbContext` seeded with a known trading-day calendar.

- `EarliestAsync` / `LatestAsync` return `Min` / `Max` of `Date`.
- `PreviousAsync` skips weekend / holiday gaps; returns `null` at floor.
- `NextAsync` skips gaps; returns `null` at ceiling.
- `ResolveOrFallbackAsync` returns input when it's a trading day; nearest *earlier* when it isn't.
- Empty-DB case throws `NoTradingDaysException`.

### 10.2 Unit — `LoadDashboardUseCase` (extends existing tests)

- `targetDate` in the past → loads price bar / suggestion / portfolio for that date; sets `IsHistorical = true`; populates prev / next.
- `targetDate == today` → `IsHistorical = false`, `NextTradingDay = null`.
- Masthead trade count reflects `TradesOnOrBeforeSpec(targetDate)`, not the global count.
- Existing live-mode tests stay green (regression guard).

### 10.3 Page-level — `DashboardPage` via bUnit

- `?on=` missing → live render, all three actions present.
- `?on=foo` → `NavigateTo("/", replace: true)`.
- Future `?on=` → `NavigateTo("/", replace: true)`.
- Pre-floor `?on=` → `NavigateTo("/?on={earliest}", replace: true)`.
- Non-trading-day `?on=` → `NavigateTo("/?on={prev trading day}", replace: true)`.
- Historical render → `RefreshFab` not in DOM; "Re-run AI" / "Log trade" buttons not in DOM; "return to today" link present; card heading reads "Call for {date}".

### 10.4 Manual smoke — required before declaring done

1. `dotnet run`, open `/`. Confirm live mode, click `‹` once → URL becomes `/?on={prev}`, dashboard updates, FAB gone.
2. Click date pill, pick a Sunday → page redirects to nearest Friday.
3. Click "return to today" → URL collapses to `/`, FAB returns.
4. Browser back arrow → returns to historical view; forward → returns to today.
5. `←` / `→` keys step through dates; `‹` / `›` `disabled` at boundaries.
6. Type `?on=garbage` manually → redirects cleanly to `/`.
7. Visit `/trades` and `/settings` → masthead has no date control; behavior unchanged.

## 11. Out of scope (deliberate)

- Time travel for `/trades` and `/settings`.
- Editing past entries (re-running AI, logging a trade dated in the past via the dashboard, refreshing prices retroactively). Past entries are immutable; the existing `BackfillCoordinator` covers missing-AI-call backfill at first-load.
- A "summary diff" entry view (price move / trades logged / AI's call vs. outcome on that day) — proposed and rejected for this round.
- Additional URL filters (`?range=3m`, `?focus=COIN`). The query-param shape is forward-compatible with these but they're not added now.
- Renaming "entry no." to honestly reflect that it's a trade count. Tracked here as known semantic debt.
- Caching of trading-day lookups. Add only if profiling shows the need.

## 12. Acceptance

This spec is implementation-ready when:

- All decisions in §2 are concrete (no TBDs).
- Every file in §5.1 has a clear owner layer and purpose.
- Every edge case in §9 has a defined behavior.
- Tests in §10 cover both happy paths and each redirect branch.
