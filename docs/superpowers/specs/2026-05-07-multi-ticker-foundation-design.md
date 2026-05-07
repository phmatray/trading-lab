# TradyStrat — Multi-ticker foundation (Phase 1)

**Status:** Design approved · Ready for implementation plan
**Date:** 2026-05-07
**Author:** Philippe Matray (with Claude)
**Depends on:** [`2026-05-06-tradystrat-dashboard-design.md`](./2026-05-06-tradystrat-dashboard-design.md)
**Successor:** Phase 2 — Multi-ticker AI (separate spec, not yet written)

---

## 1. Purpose & goal

TradyStrat today is hard-wired to a single held instrument (CON3.L) with two
companion tickers (COIN, BTC-USD) used only as indicator/AI context. The
schema and dashboard cannot represent any other holdings: `Trades` has no
ticker column, `Goals.FocusTicker` is free text, `FxRates` only models
EUR/USD, and the set of tracked tickers is hardcoded in `appsettings.json`.

This spec widens the data model and the dashboard so the user can hold
**N first-class tickers**, each with its own currency, position, and
zone analysis, valued together against a single portfolio-wide €1M goal.

The AI suggestion deliberately stays single-ticker in this phase — see §10.

## 2. Decisions at a glance

| Concern | Decision |
|---|---|
| Multi-ticker meaning | **True diversification.** N held tickers, each first-class on the dashboard. |
| Goal model | **Portfolio-wide EUR target.** `Goals.FocusTicker` removed. |
| Ticker identity on Trades | **Int FK to `Instruments.Id`**, not a string column. |
| FX scope | **Generic `(Base, Quote, Date)`.** Any pair Yahoo can serve. |
| Onboarding | **Self-service Settings UI** with Yahoo probe-and-confirm. |
| Held vs Watchlist | **Explicit `Instrument.Kind` enum.** Watchlist instruments appear in zone analysis and AI context but not in portfolio totals. |
| `appsettings.Tickers.Focus` | **Kept.** Selects the AI suggestion target. Phase 2 dissolves it. |
| `appsettings.Tickers.Context` | **Removed.** Was a dead config key — nothing in running code reads it; cleanup. |
| `appsettings.Fx.Pair` | **Removed.** FX pairs derive from `Instrument.Currency`. |
| Trade-form ticker scope | **Held instruments only.** Logging a trade on a Watchlist instrument is not allowed in v1. |
| AddInstrument transaction policy | **Commit-then-warm-best-effort.** Insert succeeds first; cache warm failures log and self-heal on next startup. |
| FIFO refactor approach | **In place inside `PortfolioService.BuildSnapshot()`** — no extracted `FifoLedger` class (YAGNI for one consumer). |
| `IFxRateProvider` interface | **Changes.** `(string pair, …)` → `(string base, string quote, …)`. Ripples to `DailyFxCache.EnsureFreshAsync` and `LatestFxRateSpec`. |
| Yahoo metadata endpoint | **Net-new code path.** `YahooPriceFeed.GetInstrumentMetadataAsync(ticker)` calling `v7/finance/quote`. |
| Pattern drift | **Two named patterns weaken:** `SnapshotFactory` becomes a service (was Factory Method); `YahooFxProvider` symbol resolution wants a future Strategy. Recorded in §13, not fixed here. |
| `PriceBars.Ticker` | **No FK to Instruments.** PriceBars is a write-through cache, owned by the PriceFeed module. |
| Suggestions table | **Untouched in Phase 1.** Phase 2 adds `InstrumentId` FK and changes the UQ to `(ForDate, InstrumentId)`. |
| Migration | **Single EF migration**, applied automatically on startup. |
| Rollback | **None.** User `cp`s the DB before first run on the new code. |
| AI behavior | **Unchanged.** Same single daily call for the focus ticker. Same `PromptHash` on day one (seeded `[CON3.L, COIN, BTC-USD]` reproduces today's prompt input byte-identically). |
| Test stack | xunit.v3 + Shouldly + EF Core InMemory + captured Yahoo JSON fixtures. **No bUnit.** |

## 3. Domain model

### 3.1 New: `Instrument`

```
Instrument
  Id            int PK
  Ticker        string         -- "CON3.L", "COIN", "BTC-USD", "ETHE.PA"
  Name          string         -- Yahoo longName
  Currency      string(3)      -- ISO 4217
  Exchange      string         -- Yahoo fullExchangeName
  TimezoneId    string         -- IANA, e.g. "Europe/London"
  Kind          int (enum)     -- Held | Watchlist
  AddedAt       DateTime
```

Indexes: `UQ(Ticker)`.

`InstrumentKind`: `Held = 0`, `Watchlist = 1`.

### 3.2 Changed: `Trade`

```
Trade
  + InstrumentId  int FK Instruments.Id   -- replaces implicit CON3.L
  -- existing columns unchanged
```

Indexes: existing `IX(ExecutedOn)` is kept (serves the all-tickers
trade-ledger view); new `IX(InstrumentId, ExecutedOn)` is added (serves
the per-ticker FIFO replay).

### 3.3 Changed: `FxRate`

```
FxRate
  Id          int PK
  Base        string(3)         -- replaces "Pair"
  Quote       string(3)
  Date        DateOnly (TEXT)
  Rate        decimal (TEXT)    -- renamed from "UsdPerEur"; "Quote per 1 Base"
  FetchedAt   DateTime
```

Indexes: `UQ(Base, Quote, Date)` (replaces `IX_FxRates_Pair_Date`).

The "Quote per 1 Base" semantic matches the legacy `UsdPerEur` field
(USD per 1 EUR), so the migration's `Base='EUR', Quote='USD'` mapping
preserves the existing 519 rows' meaning.

### 3.4 Changed: `Goal`

```
Goal
  - FocusTicker REMOVED
  -- TargetEur, TargetDate, UpdatedAt unchanged
```

### 3.5 Unchanged: `PriceBar`

`PriceBars.Ticker` stays a free string with no FK. Reason: PriceBars is a
write-through cache populated by Yahoo — an implementation detail of the
PriceFeed module. Coupling it to the Instruments aggregate would force
cascade rules we don't need (we never remove instruments in Phase 1).

### 3.6 Unchanged: `Suggestion`

Untouched in Phase 1. Phase 2 will add an `InstrumentId` FK (consistent
with `Trade.InstrumentId`) and change the UQ from `(ForDate)` to
`(ForDate, InstrumentId)`.

## 4. Schema migration

A single EF Core migration. SQLite doesn't support `ALTER COLUMN`, so EF
emits table rebuilds where needed; row counts (3 trades, 519 FX rates,
1 goal) make this trivial.

```
1. CREATE TABLE Instruments (...)

2. INSERT seed rows (raw SQL inside the migration):
     ('CON3.L',  'Leverage Shares 3x Long Coinbase',  'USD', 'LSE',
      'Europe/London',     Held,      now)
     ('COIN',    'Coinbase Global, Inc.',             'USD', 'NMS',
      'America/New_York',  Watchlist, now)
     ('BTC-USD', 'Bitcoin USD',                       'USD', 'CCC',
      'UTC',               Watchlist, now)

3. ALTER TABLE Trades ADD COLUMN InstrumentId INT NULL
4. UPDATE Trades SET InstrumentId =
       (SELECT Id FROM Instruments WHERE Ticker='CON3.L')
5. Make InstrumentId NOT NULL + FK. Add IX(InstrumentId, ExecutedOn).
   Keep the existing IX(ExecutedOn).

6. Rebuild FxRates in one EF table-rebuild: drop Pair, add Base + Quote
   (NOT NULL, with constants 'EUR' and 'USD' supplied during the data
   copy), rename UsdPerEur → Rate. Replace the UQ index with
   UQ(Base, Quote, Date).

7. Rebuild Goals dropping FocusTicker.

8. Suggestions: untouched.
```

**Code-side cleanup that must accompany this migration** (otherwise the
EF model and DB diverge or compilation fails):

- Remove `FocusTicker` from `GoalConfig` entity.
- Remove the `FocusTicker` mapping from `GoalConfiguration.cs`.
- Remove the `FocusTicker = "CON3.L"` literal from
  `UpdateGoalUseCase` (insert path only — the update path uses
  `with { ... }` and silently drops the property).
- Remove the disabled "Focus ticker" `<input>` from
  `SettingsPage.razor`.
- Delete the `Tickers.Context` and `Fx.Pair` keys from
  `appsettings.json` and `appsettings.Development.json` (both are
  dead config after this phase — see §7.1).

**EF migration generation order.**

1. Update entity classes and `IEntityTypeConfiguration<T>` mappings.
2. `dotnet ef migrations add MultiTickerFoundation`.
3. **Manually edit** the generated migration body to add
   `migrationBuilder.InsertData(...)` for the three seeded instruments
   and the `UPDATE Trades SET InstrumentId = …` backfill — EF's
   diff-based generation does not produce these.
4. Auto-applied at startup via the existing migration runner.

**Rollback policy.** None. The README already states "safe to delete the
DB to start fresh"; the user's only irreplaceable data is the 3 CON3.L
trades, which the user will preserve by copying the DB file before first
run on the new code. The migration's commit message records this.

## 5. "Add instrument" Settings flow

### 5.1 UX

```
┌─ Add instrument ──────────────────────────────────┐
│  Ticker:  [ ETHE.PA      ]   [ Probe ]            │
│                                                   │
│  ── after probe ──                                │
│  Found: WisdomTree Physical Ethereum              │
│         Currency: EUR · Exchange: Euronext Paris  │
│         Timezone: Europe/Paris                    │
│  Kind:   ( ) Held    (•) Watchlist                │
│           [ Add ]   [ Cancel ]                    │
└───────────────────────────────────────────────────┘
```

The flow is two-step by design: probe is read-only and surfaces what
Yahoo says; the user confirms before persisting. Avoids "I typed a typo
and it's now in my DB" and gives the user a chance to read the resolved
metadata before committing.

### 5.2 Use cases

Inputs are wrapped in records to match the existing `IUseCase<TInput, TOutput>`
convention.

```
record ProbeInstrumentInput(string Ticker);
record AddInstrumentInput(InstrumentProbe Probe, InstrumentKind Kind);

ProbeInstrumentUseCase : IUseCase<ProbeInstrumentInput, InstrumentProbe>
  → returns InstrumentProbe { Ticker, Name, Currency, Exchange, TimezoneId }
  → calls a NEW method on the Yahoo provider (see §5.3 below) — this is
    a net-new code path; `YahooPriceFeed` today only fetches OHLCV.

AddInstrumentUseCase : IUseCase<AddInstrumentInput, Instrument>
  → idempotent on Ticker (DuplicateInstrumentException if already present).
  → **Transaction policy: commit-then-warm-best-effort.** Inserts the
    Instrument row first and commits. Then attempts to warm PriceBars
    and (if Currency != "EUR" and no FxRates rows exist for
    Base="EUR", Quote=Currency) the FX pair. Warm failure is logged at
    warning level and surfaced to the UI as a non-fatal banner; the
    instrument stays in the DB and re-warms on next startup. Rationale:
    transactional rollback across HTTP calls is more code than this
    personal app needs, and a missing-cache state is self-healing.
  → returns the persisted Instrument.

ListInstrumentsUseCase : IUseCase<Unit, IReadOnlyList<Instrument>>
  → returns all instruments. Used by the dashboard, the trade form's
    instrument dropdown, and the price-feed warm-up loop.
```

### 5.3 New Yahoo metadata method

`YahooPriceFeed` today only parses chart/OHLCV responses. Phase 1
extends it with:

```csharp
Task<InstrumentMetadata> GetInstrumentMetadataAsync(
    string ticker,
    CancellationToken ct);

record InstrumentMetadata(
    string Ticker, string LongName, string Currency,
    string FullExchangeName, string ExchangeTimezoneName);
```

Implementation calls Yahoo's `v7/finance/quote?symbols={ticker}` (the
endpoint that returns `longName`, `currency`, `fullExchangeName`,
`exchangeTimezoneName`). Parsed via the same JSON-fixture-driven
pattern used for OHLCV. Failure modes map to the typed exceptions in
§5.4.

### 5.4 Failure taxonomy

Typed exceptions in the existing `TradyStratException` family.

| Failure | Exception | UI surface |
|---|---|---|
| Yahoo returns no quote / 404 | `InstrumentNotFoundException` | "No instrument found for `XYZ`. Check the symbol." |
| Yahoo response missing currency or exchange | `InstrumentMetadataIncompleteException` | "Yahoo returned partial data; can't add automatically. Try a different symbol." |
| Network/transport error | existing `YahooApiException` | "Couldn't reach Yahoo. Try again." |
| Duplicate ticker | `DuplicateInstrumentException` | "`XYZ` is already tracked." |
| FX pair not resolvable from EUR | `UnsupportedCurrencyException` | "EUR↔ABC FX rate isn't available. Can't add this instrument." |

The `UnsupportedCurrencyException` check happens at probe time, by
attempting to fetch one bar of `EUR{Currency}=X` from Yahoo. Failing
early is cheaper than persisting an instrument that can't be valued.

## 6. Dashboard rework

### 6.1 Layout

```
┌─ Goal progress ───────────────────────────────────┐
│  €X / €1,000,000   [progress bar]    by 2026-12-31│
│  (TargetEur, summed market value of all Held      │
│   positions converted to EUR)                     │
└───────────────────────────────────────────────────┘

┌─ Positions (Held) ────────────────────────────────┐
│  Ticker   Qty    Cost basis   Mkt value   PnL     │
│  CON3.L   …      €X           €Y          +Z%     │
│  ETHE.PA  …      €X           €Y          +Z%     │
│  …                                                │
│  Total                        €Σ                  │
└───────────────────────────────────────────────────┘

┌─ Today's suggestion (CON3.L) ─────────────────────┐
│  [unchanged AI card; still calls for the          │
│   appsettings focus ticker]                       │
└───────────────────────────────────────────────────┘

┌─ Zone analysis ───────────────────────────────────┐
│  One row per Held + Watchlist instrument:         │
│  ticker · zone · RSI · BB · 200-SMA · Ichimoku    │
└───────────────────────────────────────────────────┘

┌─ Trade ledger ────────────────────────────────────┐
│  [existing component, gains a Ticker column;      │
│   new-trade form gains an Instrument dropdown]    │
└───────────────────────────────────────────────────┘
```

### 6.2 `LoadDashboardUseCase` shape

```
LoadDashboardResult {
  GoalProgress:     { TargetEur, CurrentEur, PctToGoal, TargetDate }
  Positions:        IReadOnlyList<PositionRow>      -- Held only
  ZoneCards:        IReadOnlyList<ZoneCard>         -- Held + Watchlist
  TodaysSuggestion: Suggestion?                     -- focus ticker only
  RecentTrades:     IReadOnlyList<TradeRow>         -- top N, all tickers
}

PositionRow {
  Instrument, Quantity, CostBasisEur, MarketValueEur,
  UnrealisedPnLEur, UnrealisedPnLPct
}
```

### 6.3 Component changes in `Features/Dashboard/Components/`

- `GoalProgressCard.razor` — input changes from "CON3.L EUR value" to "portfolio EUR".
- `PositionsTable.razor` — **new**.
- `ZoneCard.razor` — already a component; loop becomes `@foreach (var zc in Result.ZoneCards)`.
- `TodaysSuggestionCard.razor` — unchanged AI logic; title now reads
  the focus ticker from `appsettings.Tickers.Focus` instead of being
  hardcoded to `"CON3.L"`. (Tiny edit, but a hardcoded "CON3.L" string
  in the view would block testing the spec on any other focus.)
- `TradeLedgerSection.razor` — gains a Ticker column.
- `AddTradeDialog.razor` — gains an Instrument dropdown populated
  from **Held instruments only**. Logging a trade against a Watchlist
  instrument is not allowed in v1; the user must add a Held
  instrument from Settings first. (Auto-promotion of Watchlist →
  Held on first trade is a UX nicety we deliberately defer.)

### 6.4 Empty state

If no Held instruments exist (fresh install before the user adds any),
the Positions table shows "No positions yet — add an instrument in
Settings to start tracking." Goal bar shows €0.

### 6.5 Performance

Dashboard load goes from one ticker's indicators + two context tickers'
indicators (3 indicator passes) to N+M passes (N held, M watchlist). At
5–10 instruments this is fine — TaLib is in-process and bars are cached.
If it ever bites, `PortfolioSnapshots` (a separate deferred slice)
becomes the answer. Not pre-optimised here.

## 7. PriceFeed warm-up & FX changes

### 7.1 `PriceFeedHostedService` warms what's in the DB

The current implementation has its own hardcoded array (not config-driven):

```csharp
private static readonly string[] Tickers = ["CON3.L", "COIN", "BTC-USD"];
private const string FxPair = "EURUSD";
```

After Phase 1:

```
on startup:
  instruments = await ListInstrumentsUseCase.Execute()
  for each instrument in instruments:
      await DailyPriceCache.EnsureFreshAsync(instrument.Ticker, ct)
  fxPairs = instruments
              .Where(i => i.Currency != "EUR")
              .Select(i => (Base: "EUR", Quote: i.Currency))
              .Distinct()
  for each (base, quote) in fxPairs:
      await DailyFxCache.EnsureFreshAsync(base, quote, ct)
```

The hardcoded `Tickers` array and `FxPair` constant are removed.
`appsettings.Tickers.Focus` survives in appsettings as the AI
suggestion target only. `appsettings.Tickers.Context` is **deleted as
cleanup** (it was a dead config key — nothing in the running code reads
it). `appsettings.Fx.Pair` is **deleted as cleanup** (FX pairs derive
from `Instrument.Currency` now).

### 7.2 Yahoo FX provider — interface change

`YahooFxProvider` today is a hardcoded switch:

```csharp
var symbol = pair switch {
    "EURUSD" => "EURUSD=X",
    _        => throw new FxRateUnavailableException(...)
};
```

Generalising means changing the `IFxRateProvider` interface signature
**and the cache and spec that ride on it**:

| Layer | Today | After Phase 1 |
|---|---|---|
| `IFxRateProvider.GetAsync` | `(string pair, DateOnly date, …)` | `(string base, string quote, DateOnly date, …)` |
| `DailyFxCache.EnsureFreshAsync` | `(string pair, …)` | `(string base, string quote, …)` |
| `LatestFxRateSpec` | filters on `Pair` | filters on `(Base, Quote)` |
| `YahooFxProvider` symbol | switch on `"EURUSD"` | format `$"{base}{quote}=X"` |

The string-format generalisation is acceptable for currency pairs
(uniform Yahoo convention) but not for other asset classes
(see §13 — pattern drift). A symbol-resolution Strategy is deferred.

### 7.3 `FxConverter` becomes generic

```csharp
// Today (real signature — async, takes CancellationToken)
Task<decimal> UsdToEurAsync(decimal usd, DateOnly asOf, CancellationToken ct);

// After Phase 1
Task<decimal> ToEurAsync(
    decimal amount, string fromCurrency, DateOnly asOf, CancellationToken ct);
```

Internally the converter looks up `(Base="EUR", Quote=fromCurrency, Date)`
in `DailyFxCache`. Same-currency (`fromCurrency == "EUR"`) is a
pass-through. Whatever same-date-fallback policy `DailyFxCache` already
applies for EUR/USD is preserved unchanged for the generic case.

Existing callers of `UsdToEurAsync`: `SnapshotFactory` (2 sites),
`LoadDashboardUseCase` (1 site). Phase 1 adds a new caller in
`PortfolioService` (per-position EUR valuation, see §8). All four
sites are updated to `ToEurAsync(amount, instrument.Currency, asOf, ct)`.

## 8. Per-ticker FIFO

Today's FIFO logic lives **inline** inside `PortfolioService.BuildSnapshot()`
— there is no separate `FifoLedger` class to reuse. Phase 1 widens
the existing method by grouping trades by `InstrumentId` and running
the existing per-ticker accounting once per group.

**Refactoring choice:** restructure `BuildSnapshot` in place; do **not**
extract a new `FifoLedger` class. There's only one consumer (the
service itself), so the extraction would be YAGNI. If a second consumer
appears later (e.g. a tax-report use case), that's the moment to
extract.

Pseudo-code for the in-place change:

```
PortfolioService.BuildSnapshot(asOf)
  trades = await _trades.ListAsync(new TradesAsOfSpec(asOf))
  groups = trades.GroupBy(t => t.InstrumentId)

  positions = []
  foreach (var g in groups):
      // existing FIFO walk over g, producing OpenLots, CostBasis, Quantity
      var perTicker = FifoWalk(g.OrderBy(t => t.ExecutedOn))
      var instrument = instrumentsById[g.Key]
      var marketValueEur = await _fx.ToEurAsync(
          perTicker.Quantity * latestClose(instrument.Ticker, asOf),
          instrument.Currency, asOf, ct)
      positions.Add(new Position(instrument, perTicker, marketValueEur))

  return new PortfolioSnapshot(positions, totalEur: positions.Sum(...))
```

The "FIFO walk" body is the existing inline code, untouched. Only the
outer loop and the per-position market-value/FX calls are new.

**Daily growth series** widens from "CON3.L EUR over time" to "sum of
per-ticker EUR market value over time" — replay trades day-by-day across
all instruments, value each open position at that day's close × that
day's FX. Same algorithm; outer loop changes from one ticker to all.

## 9. AI suggestion — what changes (almost nothing)

The AI loop stays single-ticker in Phase 1. The only edit is to
`SnapshotFactory` (currently has a static `Catalog` of
`(string Ticker, string Currency)[]` with focus + context hardcoded).
After Phase 1 it reads the same shape from the Instruments table:

```
// Before (static catalog inside SnapshotFactory)
private static readonly (string Ticker, string Currency)[] Catalog = [
    ("CON3.L",  "USD"),
    ("COIN",    "USD"),
    ("BTC-USD", "USD"),
];

// After (DB-driven; focus from config, context from Instruments)
var focus    = _config.Tickers.Focus;             // unchanged
var watchlist = (await ListInstrumentsUseCase())
                 .Where(i => i.Kind == InstrumentKind.Watchlist)
                 .Select(i => (i.Ticker, i.Currency))
                 .ToList();
var focusInstr = (await ListInstrumentsUseCase())
                 .Single(i => i.Ticker == focus);
var catalog = new[] { (focusInstr.Ticker, focusInstr.Currency) }
                 .Concat(watchlist).ToArray();
```

`Currency` is preserved because the snapshot uses it downstream for
indicator/FX context; dropping it on the floor would silently change
the prompt input. The seeded migration produces
`[(CON3.L, USD), (COIN, USD), (BTC-USD, USD)]` — byte-identical to
today's hardcoded catalog. Same `PromptHash` on day one; no AI
regression risk.

Held instruments other than the focus ticker do **not** appear in the
prompt in Phase 1.

> **Pattern note.** README §17 calls `SnapshotFactory.BuildAsync` a
> Factory Method. Once it depends on a DB-backed use case, the
> stateless-factory framing weakens — it becomes a service that
> assembles snapshots. We don't fix this here; §13 records the drift.

## 10. What this spec deliberately defers

These items are valuable but out of scope for Phase 1.

- **Phase 2 — multi-ticker AI.** `Suggestions.InstrumentId` FK, UQ
  becomes `(ForDate, InstrumentId)`, prompt redesign, optional
  structured citations, suggestion ↔ trade linkage. Separate spec,
  builds on Phase 1's schema.
- **Portfolio snapshots.** Materialised daily PnL/cost-basis rows.
  Performance optimisation that becomes interesting only at higher
  position counts.
- **Corporate actions.** Splits, mergers, ETP rebalances. Will silently
  corrupt cost basis when they happen; needs its own slice.
- **Adjusted close** on `PriceBars`. Required for honest total-return
  analysis; defer until needed.
- **Removing or archiving instruments.** Once added, an instrument
  stays. UI for retiring a ticker is a future concern.
- **Toggling `Instrument.Kind` post-add.** A misclick at add-time
  (Held vs Watchlist) is currently unrecoverable from the UI; the user
  edits the DB or re-adds. We accept this trap in v1 because the
  alternative (Kind toggle UI + portfolio-state implications when
  flipping Held → Watchlist with open positions) is its own slice.
- **Multi-currency goal target.** Stays EUR-only.
- **Goal history.** `Goals` is still a single mutable row; replacing
  it with an append-only `GoalHistory` is a separate slice.
- **bUnit / component tests.** Test stack matches the existing project.

## 11. Test plan

### 11.1 New test files

| File | What it covers |
|---|---|
| `Features/PriceFeed/Providers/YahooPriceFeedMetadataTests.cs` | The new `GetInstrumentMetadataAsync` parser — happy path + the four typed-exception failure modes. Driven by 4–6 captured Yahoo `v7/finance/quote` JSON fixtures. |
| `Features/Settings/UseCases/ProbeInstrumentUseCaseTests.cs` | Use-case orchestration with a stubbed `IYahooPriceFeed`: ticker → `InstrumentProbe` happy path; FX-pair-not-resolvable surfaces `UnsupportedCurrencyException`. |
| `Features/Settings/UseCases/AddInstrumentUseCaseTests.cs` | Happy path inserts a row + warms cache. Duplicate ticker throws. **Transaction policy:** simulated warm failure leaves the Instrument row committed (does not roll back). |
| `Features/Portfolio/PortfolioServiceMultiTickerTests.cs` | Trades on two tickers in different currencies → two correct per-ticker FIFO results in `BuildSnapshot`, correct portfolio EUR (mock FX). Goal progress sums across tickers. |
| `Data/MigrationTests.cs` | Migration runs against a snapshot of the pre-Phase-1 schema with the 3 real trades + 519 FX rows; asserts post-state shapes (`Trades.InstrumentId` not null, `FxRates.Base`/`Quote` populated, `Goals.FocusTicker` absent, the three instruments seeded). |

### 11.2 Extended existing tests

- `Features/Fx/FxConverterTests.cs` — `ToEurAsync(100, "GBP", date, ct)`
  resolves via the generic pair lookup. Pass-through for `"EUR"`.
- `Features/PriceFeed/PriceFeedHostedServiceTests.cs` — warm-up iterates
  over instruments from the DB, not config.

### 11.3 Unchanged tests (regression sentinel)

`SnapshotFactoryTests` and `SuggestionServiceTests` should continue
to pass with no edits, because the seeded Watchlist (`[COIN, BTC-USD]`)
reproduces the prompt input exactly. **Concrete sentinel:** the
`PromptHash` of a snapshot built post-migration with the seeded set
equals the captured `PromptHash` from the existing fixture — assert this
explicitly in `SnapshotFactoryTests`.

### 11.4 Pre-merge manual smoke test

This is a personal project; manual smoke is part of the contract.

1. `cp` the live DB.
2. Run the new code; verify warm-up succeeds and the dashboard renders
   the 3 existing trades' CON3.L position with the same EUR value as
   before the migration.
3. Add `ETHE.PA` (a real EUR-quoted ETP) via the Settings flow as a
   **Held** instrument (since flipping kind post-add is not supported
   in v1 — see §10); log a tiny test trade against it; verify
   portfolio totals and goal bar update.
4. Add a Watchlist-only instrument (e.g. `ETH-USD`); verify it appears
   in zone analysis but not in the positions table.
5. Verify the AI suggestion still fires for the focus ticker with the
   same `PromptHash` as the previous day.

## 12. Specification classes — what's new and what changes

The Specification pattern (Ardalis) is used for all DB queries. Phase 1
adds the following spec classes; the existing ones in
`Features/Trades/`, `Features/PriceFeed/`, `Features/Fx/`, and
`Features/AiSuggestion/` are unaffected unless listed as "modified."

**New (`Features/Settings/Specifications/`):**

- `InstrumentByTickerSpec(string ticker)` — duplicate-check on add;
  also used by `SnapshotFactory` to resolve `focus → Instrument`.
- `AllInstrumentsSpec` — used by `PriceFeedHostedService` warm loop
  and `ListInstrumentsUseCase`.
- `InstrumentsByKindSpec(InstrumentKind kind)` — used to project
  Held → positions, Watchlist → context tickers and zone cards.

**New (`Features/Trades/Specifications/`):**

- `TradesByInstrumentSpec(int instrumentId)` — exists for completeness
  but the per-ticker FIFO refactor in §8 actually loads all trades and
  groups in-memory (3 trades today; not worth the per-ticker round-trip).
  Add it when a real per-ticker query path appears (e.g. tax report).
  **Status: deferred — do not add in Phase 1.**

**Modified (`Features/Fx/Specifications/`):**

- `LatestFxRateSpec` — filter changes from `Pair == "EURUSD"` to
  `Base == base && Quote == quote`. Constructor signature becomes
  `LatestFxRateSpec(string @base, string quote, DateOnly asOf)`.

**Unaffected:** all `PriceBars` specs (cache table, free string ticker
unchanged), all `Suggestions` specs (untouched in Phase 1), all
`Trades` specs other than the deferred new one.

## 13. GoF pattern drift recorded for this phase

Two named patterns from README §17 weaken in Phase 1. We record the
drift instead of fixing it; both fixes belong in later phases.

**13.1 SnapshotFactory: Factory Method → Service.** README §17 lists
`SnapshotFactory.BuildAsync` under "Factory Method." With the change in
§9 it now depends on `ListInstrumentsUseCase` (a DB-backed runtime
read). The factory framing — a static creator that doesn't need state —
no longer fits cleanly; it's a service that assembles snapshots from
collaborators. README §17 should be updated when this spec lands. We
do not rename the class in Phase 1 (rename churn ≠ semantic gain).

**13.2 Symbol resolution: switch → string-format → (deferred) Strategy.**
The `YahooFxProvider` symbol switch is being replaced with
`$"{base}{quote}=X"` — fine for FX, where Yahoo's convention is
uniform. But Yahoo's symbol convention varies by asset class:

| Asset class | Yahoo symbol example |
|---|---|
| FX        | `EURUSD=X` |
| Crypto    | `BTC-USD` (no suffix) |
| Futures   | `GC=F` |
| Indices   | `^GSPC` |

A proper `IYahooSymbolStrategy` keyed by an asset-class enum would be
the GoF-correct shape. We defer it: Phase 1 only widens FX, and the
string format is correct for every FX pair Yahoo supports. The Strategy
arrives the day a non-FX, non-stock asset class is onboarded.
