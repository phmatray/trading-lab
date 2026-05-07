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
| `appsettings.Tickers.Context` | **Removed.** Watchlist instruments come from the DB. |
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

```
ProbeInstrumentUseCase.Execute(string ticker)
  → returns InstrumentProbe { Ticker, Name, Currency, Exchange, TimezoneId }
  → reads Yahoo quote-summary metadata.

AddInstrumentUseCase.Execute(InstrumentProbe probe, InstrumentKind kind)
  → idempotent on Ticker (DuplicateInstrumentException if already present).
  → inserts row.
  → triggers a one-shot warm of PriceBars via the existing PriceFeed
    providers, and — if Currency != "EUR" and no FxRates rows exist for
    (Base="EUR", Quote=Currency) — also warms that FX pair.
  → returns the persisted Instrument.

ListInstrumentsUseCase.Execute()
  → returns all instruments. Used by the dashboard, the trade form's
    instrument dropdown, and the price-feed warm-up loop.
```

### 5.3 Failure taxonomy

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
- `TodaysSuggestionCard.razor` — unchanged. Title hardcodes the focus ticker for now.
- `TradeLedgerSection.razor` — gains a Ticker column.

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

```
on startup:
  instruments = await ListInstrumentsUseCase.Execute()
  for each instrument in instruments:
      await DailyPriceCache.Warm(instrument.Ticker, lookback: 2y)
  fxPairs = instruments
              .Where(i => i.Currency != "EUR")
              .Select(i => (Base: "EUR", Quote: i.Currency))
              .Distinct()
  for each (base, quote) in fxPairs:
      await DailyFxCache.Warm(base, quote, lookback: 2y)
```

The hardcoded reads of `appsettings.Tickers.Focus` and
`appsettings.Tickers.Context` are removed from this service.
`Tickers.Focus` survives in appsettings as the AI suggestion target only.

### 7.2 Yahoo FX symbol convention

Yahoo expects `EURUSD=X`, `EURGBP=X`, etc. The existing `YahooClient`
already builds the EUR/USD symbol; we generalise the builder to
`$"{base}{quote}=X"` and pass through unchanged.

### 7.3 `IFxConverter` becomes generic

```csharp
// Today
decimal UsdToEur(decimal usd, DateOnly date);

// After Phase 1
decimal ToEur(decimal amount, string fromCurrency, DateOnly date);
```

Internally the converter looks up `(Base="EUR", Quote=fromCurrency, Date)`
in `DailyFxCache`. Same-currency (`fromCurrency == "EUR"`) is a
pass-through. Whatever same-date-fallback policy `DailyFxCache` already
applies for EUR/USD is preserved unchanged for the generic case.

Callers updated: `PortfolioService` (FIFO valuation loop), the dashboard
valuation step, the goal progress calculation.

## 8. Per-ticker FIFO

`PortfolioService` today maintains one FIFO queue (CON3.L only). It
becomes a per-ticker dictionary:

```
PortfolioService.GetPositions()
  trades = await _trades.ListAllAsync()      -- ordered by ExecutedOn
  ledgers = trades
              .GroupBy(t => t.InstrumentId)
              .ToDictionary(g => g.Key, g => FifoLedger.From(g))
  return ledgers.Select(kv => new Position(
              instrument: instrumentsById[kv.Key],
              openLots:   kv.Value.OpenLots,
              costBasis:  kv.Value.CostBasis,
              quantity:   kv.Value.Quantity))
```

The existing `FifoLedger` class doesn't change — it's already
per-ticker scoped, just instantiated multiple times. The change is at
the orchestration layer.

**Daily growth series** widens from "CON3.L EUR over time" to "sum of
per-ticker EUR market value over time" — replay trades day-by-day across
all instruments, value each open position at that day's close × that
day's FX. Same algorithm; outer loop changes from one ticker to all.

## 9. AI suggestion — what changes (almost nothing)

The AI loop stays single-ticker in Phase 1. The only edit is to
`SnapshotBuilder`, which today reads context tickers from
`appsettings.Tickers.Context`:

```
// Before
snapshot.ContextTickers = config.Tickers.Context;

// After
snapshot.ContextTickers = (await ListInstrumentsUseCase())
    .Where(i => i.Kind == InstrumentKind.Watchlist)
    .Select(i => i.Ticker)
    .ToList();

// snapshot.FocusTicker = config.Tickers.Focus;   // unchanged
```

The seeded migration sets `Watchlist = [COIN, BTC-USD]`, reproducing
today's context exactly. The Anthropic prompt input is byte-identical on
day one; same `PromptHash`; no AI regression risk.

Held instruments other than the focus ticker do **not** appear in the
prompt in Phase 1.

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
- **Multi-currency goal target.** Stays EUR-only.
- **Goal history.** `Goals` is still a single mutable row; replacing
  it with an append-only `GoalHistory` is a separate slice.
- **bUnit / component tests.** Test stack matches the existing project.

## 11. Test plan

### 11.1 New test files

| File | What it covers |
|---|---|
| `Features/Settings/Providers/InstrumentProberTests.cs` | Yahoo metadata JSON → `InstrumentProbe`; the four typed-exception failure modes. Driven by 4–6 captured JSON fixtures. |
| `Features/Settings/UseCases/AddInstrumentUseCaseTests.cs` | Happy path inserts a row + warms cache. Duplicate ticker throws. Unsupported currency throws. |
| `Features/Portfolio/PortfolioServiceMultiTickerTests.cs` | Trades on two tickers in different currencies → two correct FIFO ledgers, correct portfolio EUR (mock FX). Goal progress sums across tickers. |
| `Data/MigrationTests.cs` | Migration runs against a snapshot of the pre-Phase-1 schema with the 3 real trades + 519 FX rows; asserts post-state shapes (`Trades.InstrumentId` not null, `FxRates.Base`/`Quote` populated, `Goals.FocusTicker` absent, the three instruments seeded). |

### 11.2 Extended existing tests

- `Features/Fx/FxConverterTests.cs` — `ToEur(100, "GBP", date)` resolves
  via the generic pair lookup. Pass-through for `"EUR"`.
- `Features/PriceFeed/PriceFeedHostedServiceTests.cs` — warm-up iterates
  over instruments from the DB, not config.

### 11.3 Unchanged tests (regression sentinel)

`SnapshotBuilderTests` and `SuggestionServiceTests` should continue to
pass with no edits, because the seeded Watchlist (`[COIN, BTC-USD]`)
reproduces the prompt input exactly. If they break, that's a regression
worth investigating.

### 11.4 Pre-merge manual smoke test

This is a personal project; manual smoke is part of the contract.

1. `cp` the live DB.
2. Run the new code; verify warm-up succeeds and the dashboard renders
   the 3 existing trades' CON3.L position with the same EUR value as
   before the migration.
3. Add `ETHE.PA` (a real EUR-quoted ETP) via the Settings flow; verify
   it shows as Watchlist by default and can be flipped to Held; log a
   tiny test trade; verify portfolio totals and goal bar update.
4. Verify the AI suggestion still fires for CON3.L with the same
   `PromptHash` as the previous day.
