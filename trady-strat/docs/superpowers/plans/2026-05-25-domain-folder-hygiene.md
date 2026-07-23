# Domain Folder Hygiene Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Clean up the post-Phase-7 Domain project by (a) breaking the 18-file `Shared/` grab-bag into per-aggregate identifiers plus `Shared/Money/` and `Shared/Market/` sub-namespaces, and (b) co-locating the 15 aggregate-specific exceptions next to their owning aggregate so the `Exceptions/` junk drawer can be deleted.

**Architecture:** Pure refactor. No behavior change, no migration, no new types. Folder moves drive sub-namespace introductions (`TradyStrat.Domain.Shared.Money`, `TradyStrat.Domain.Shared.Market`, plus per-aggregate identifier types living in each AR folder). Aggregate-specific exceptions move into the AR folder that owns the invariant they signal. Foundational exceptions (`TradyStratException`, `BusinessRuleViolationException`) move to `SeedWork/` next to `IBusinessRule.cs`. `Exceptions/` folder is deleted at the end.

**Tech Stack:** .NET 10, xUnit v3, Shouldly. No new packages.

**Strategy for fixing consumers:** Each move-batch ends with `dotnet build TradyStrat.slnx`. The compiler enumerates every consumer file with a missing `using`. Walk the error list, add the new namespace import to each file, rebuild, repeat. ~75 consumer files import `Domain.Exceptions` and ~114 import `Domain.Shared`, but most need only one or two new imports. Tests should keep passing throughout — if any test fails, that's a real regression, not a refactor cost.

**Tag on landing:** `domain-folder-hygiene-done`.

---

## File Structure

### Identifier moves (Task 2)

```
TradyStrat.Domain/
├── Goals/
│   └── GoalId.cs               ← from Shared/
├── Instruments/
│   └── InstrumentId.cs         ← from Shared/
├── Portfolio/
│   ├── PortfolioId.cs          ← from Shared/
│   ├── PositionId.cs           ← from Shared/
│   ├── TradeId.cs              ← from Shared/
│   └── RomanNumeralId.cs       ← from Shared/ (used by CapitalEvent)
├── Suggestions/
│   └── SuggestionId.cs         ← from Shared/
```

### Shared split (Task 3)

```
TradyStrat.Domain/Shared/
├── Money/
│   ├── Money.cs                ← from Shared/
│   ├── Currency.cs             ← from Shared/
│   ├── CurrencyPair.cs         ← from Shared/
│   ├── Percentage.cs           ← from Shared/
│   ├── Price.cs                ← from Shared/
│   └── Quantity.cs             ← from Shared/
├── Market/
│   ├── Exchange.cs             ← from Shared/
│   ├── Ticker.cs               ← from Shared/
│   └── TimezoneId.cs           ← from Shared/
└── DateRange.cs                ← stays (truly cross-cutting; single file)
```

### Conviction relocation (Task 4)

```
TradyStrat.Domain/Suggestions/
└── Conviction.cs               ← from Shared/
```

### Foundational exception moves (Task 5)

```
TradyStrat.Domain/SeedWork/
├── TradyStratException.cs               ← from Exceptions/
└── BusinessRuleViolationException.cs    ← from Exceptions/ (already imports SeedWork)
```

### Aggregate-specific exception moves (Task 6)

```
TradyStrat.Domain/Portfolio/
├── TradeValidationException.cs          ← from Exceptions/
└── CsvImportException.cs                ← from Exceptions/

TradyStrat.Domain/Instruments/
├── DuplicateInstrumentException.cs      ← from Exceptions/
├── InstrumentNotFoundException.cs       ← from Exceptions/
└── InstrumentMetadataIncompleteException.cs   ← from Exceptions/

TradyStrat.Domain/Indicators/
└── IndicatorComputationException.cs     ← from Exceptions/

TradyStrat.Domain/MarketData/
├── FxRateUnavailableException.cs        ← from Exceptions/
└── NoTradingDaysException.cs            ← from Exceptions/

TradyStrat.Domain/PriceFeed/
└── PriceFeedUnavailableException.cs     ← from Exceptions/

TradyStrat.Domain/Suggestions/
└── PolymarketUnavailableException.cs    ← from Exceptions/

TradyStrat.Domain/Settings/
└── SettingValidationException.cs        ← from Exceptions/

TradyStrat.Domain/Shared/Money/
├── CurrencyMismatchException.cs         ← from Exceptions/
└── UnsupportedCurrencyException.cs      ← from Exceptions/
```

After Task 6, `TradyStrat.Domain/Exceptions/` is empty and the folder is removed.

### Test folder mirror (Task 7)

`TradyStrat.Domain.Tests/Shared/` and `TradyStrat.Domain.Tests/Exceptions/` (if any) get realigned so test files sit next to the types they test. Test files keep their existing namespaces (no behavior change).

---

## Task 1: Worktree + baseline

**Files:** none (setup only).

- [ ] **Step 1: Create an isolated worktree**

Per [`superpowers:using-git-worktrees`](../../../.claude/) — branch name `domain-folder-hygiene`.

```bash
cd /Users/philippe/repo/gh-phmatray/TradyStrat
git worktree add .claude/worktrees/domain-folder-hygiene -b domain-folder-hygiene main
cd .claude/worktrees/domain-folder-hygiene
```

- [ ] **Step 2: Capture baseline test count**

```bash
dotnet build TradyStrat.slnx -c Debug --nologo 2>&1 | tail -5
dotnet test TradyStrat.slnx --nologo -c Debug 2>&1 | tail -10
```

Expected: build succeeds; test summary reports 499 tests passed (Phase 7 baseline). Record the exact pass count to compare against at the end of each task.

- [ ] **Step 3: Confirm clean working tree**

```bash
git status
```

Expected: `nothing to commit, working tree clean`.

---

## Task 2: Move typed IDs into their aggregate folders

**Files:**
- Move: `TradyStrat.Domain/Shared/GoalId.cs` → `TradyStrat.Domain/Goals/GoalId.cs`
- Move: `TradyStrat.Domain/Shared/InstrumentId.cs` → `TradyStrat.Domain/Instruments/InstrumentId.cs`
- Move: `TradyStrat.Domain/Shared/PortfolioId.cs` → `TradyStrat.Domain/Portfolio/PortfolioId.cs`
- Move: `TradyStrat.Domain/Shared/PositionId.cs` → `TradyStrat.Domain/Portfolio/PositionId.cs`
- Move: `TradyStrat.Domain/Shared/TradeId.cs` → `TradyStrat.Domain/Portfolio/TradeId.cs`
- Move: `TradyStrat.Domain/Shared/RomanNumeralId.cs` → `TradyStrat.Domain/Portfolio/RomanNumeralId.cs`
- Move: `TradyStrat.Domain/Shared/SuggestionId.cs` → `TradyStrat.Domain/Suggestions/SuggestionId.cs`

- [ ] **Step 1: git-mv each ID file**

```bash
git mv TradyStrat.Domain/Shared/GoalId.cs        TradyStrat.Domain/Goals/GoalId.cs
git mv TradyStrat.Domain/Shared/InstrumentId.cs  TradyStrat.Domain/Instruments/InstrumentId.cs
git mv TradyStrat.Domain/Shared/PortfolioId.cs   TradyStrat.Domain/Portfolio/PortfolioId.cs
git mv TradyStrat.Domain/Shared/PositionId.cs    TradyStrat.Domain/Portfolio/PositionId.cs
git mv TradyStrat.Domain/Shared/TradeId.cs       TradyStrat.Domain/Portfolio/TradeId.cs
git mv TradyStrat.Domain/Shared/RomanNumeralId.cs TradyStrat.Domain/Portfolio/RomanNumeralId.cs
git mv TradyStrat.Domain/Shared/SuggestionId.cs  TradyStrat.Domain/Suggestions/SuggestionId.cs
```

- [ ] **Step 2: Update the namespace declaration in each moved file**

For each file above, change the `namespace` line. Concretely:

```csharp
// TradyStrat.Domain/Goals/GoalId.cs
namespace TradyStrat.Domain.Goals;

// TradyStrat.Domain/Instruments/InstrumentId.cs
namespace TradyStrat.Domain.Instruments;

// TradyStrat.Domain/Portfolio/PortfolioId.cs
namespace TradyStrat.Domain.Portfolio;

// TradyStrat.Domain/Portfolio/PositionId.cs
namespace TradyStrat.Domain.Portfolio;

// TradyStrat.Domain/Portfolio/TradeId.cs
namespace TradyStrat.Domain.Portfolio;

// TradyStrat.Domain/Portfolio/RomanNumeralId.cs
namespace TradyStrat.Domain.Portfolio;

// TradyStrat.Domain/Suggestions/SuggestionId.cs
namespace TradyStrat.Domain.Suggestions;
```

- [ ] **Step 3: Build and walk consumer errors**

```bash
dotnet build TradyStrat.slnx -c Debug --nologo 2>&1 | grep -E "error CS" | head -50
```

Expected error class: `CS0246: The type or namespace name 'GoalId' (or InstrumentId/PortfolioId/PositionId/TradeId/RomanNumeralId/SuggestionId) could not be found`.

For each error, open the file at the reported line and add the appropriate using. The mapping is:
- Files referencing `GoalId` → add `using TradyStrat.Domain.Goals;` (if not already there)
- Files referencing `InstrumentId` → add `using TradyStrat.Domain.Instruments;`
- Files referencing `PortfolioId`/`PositionId`/`TradeId`/`RomanNumeralId` → add `using TradyStrat.Domain.Portfolio;`
- Files referencing `SuggestionId` → add `using TradyStrat.Domain.Suggestions;`

Many consumers (esp. inside `TradyStrat.Domain`) already import these namespaces because the aggregate root sits there — those need no change.

- [ ] **Step 4: Re-build until clean**

```bash
dotnet build TradyStrat.slnx -c Debug --nologo 2>&1 | tail -5
```

Expected: `Build succeeded`. 0 errors.

- [ ] **Step 5: Run the full test suite**

```bash
dotnet test TradyStrat.slnx --nologo -c Debug 2>&1 | tail -10
```

Expected: 499 tests passed, same as baseline.

- [ ] **Step 6: Move identifier test files alongside the new homes**

```bash
git mv TradyStrat.Domain.Tests/Shared/RomanNumeralIdTests.cs TradyStrat.Domain.Tests/Portfolio/RomanNumeralIdTests.cs
```

If any other test file under `TradyStrat.Domain.Tests/Shared/` targets an identifier (search with `ls TradyStrat.Domain.Tests/Shared/`), move it to mirror the production move. Update the test file's namespace if it follows the folder. Rebuild + re-test.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
refactor(domain): relocate typed-id VOs into their aggregate folders

GoalId → Goals/, InstrumentId → Instruments/,
PortfolioId/PositionId/TradeId/RomanNumeralId → Portfolio/,
SuggestionId → Suggestions/.

Each ID now lives next to the aggregate that owns it. No behavior
change; consumer using-statements updated where the compiler flagged
them.
EOF
)"
```

---

## Task 3: Split `Shared/` into `Shared/Money/` + `Shared/Market/`

**Files:**
- Move (Money): `TradyStrat.Domain/Shared/{Money,Currency,CurrencyPair,Percentage,Price,Quantity}.cs` → `TradyStrat.Domain/Shared/Money/`
- Move (Market): `TradyStrat.Domain/Shared/{Exchange,Ticker,TimezoneId}.cs` → `TradyStrat.Domain/Shared/Market/`
- Stay: `TradyStrat.Domain/Shared/DateRange.cs` (single cross-cutting VO with no obvious bucket)

- [ ] **Step 1: git-mv each money VO**

```bash
mkdir -p TradyStrat.Domain/Shared/Money
git mv TradyStrat.Domain/Shared/Money.cs        TradyStrat.Domain/Shared/Money/Money.cs
git mv TradyStrat.Domain/Shared/Currency.cs     TradyStrat.Domain/Shared/Money/Currency.cs
git mv TradyStrat.Domain/Shared/CurrencyPair.cs TradyStrat.Domain/Shared/Money/CurrencyPair.cs
git mv TradyStrat.Domain/Shared/Percentage.cs   TradyStrat.Domain/Shared/Money/Percentage.cs
git mv TradyStrat.Domain/Shared/Price.cs        TradyStrat.Domain/Shared/Money/Price.cs
git mv TradyStrat.Domain/Shared/Quantity.cs     TradyStrat.Domain/Shared/Money/Quantity.cs
```

- [ ] **Step 2: git-mv each market VO**

```bash
mkdir -p TradyStrat.Domain/Shared/Market
git mv TradyStrat.Domain/Shared/Exchange.cs   TradyStrat.Domain/Shared/Market/Exchange.cs
git mv TradyStrat.Domain/Shared/Ticker.cs     TradyStrat.Domain/Shared/Market/Ticker.cs
git mv TradyStrat.Domain/Shared/TimezoneId.cs TradyStrat.Domain/Shared/Market/TimezoneId.cs
```

- [ ] **Step 3: Update namespace declarations**

```csharp
// All six files under Shared/Money/
namespace TradyStrat.Domain.Shared.Money;

// All three files under Shared/Market/
namespace TradyStrat.Domain.Shared.Market;
```

Note: `Money.cs` currently has `using TradyStrat.Domain.Exceptions;` and `using TradyStrat.Domain.SeedWork;`. The Exceptions import will go away in Task 6 (when CurrencyMismatchException moves into the same `Shared.Money` namespace, removing the need for the import). For now, leave the Exceptions using as-is — Task 6 cleans it up.

`Price.cs` and `Quantity.cs` import `TradyStrat.Domain.SeedWork`; that stays.

- [ ] **Step 4: Build and add new usings**

```bash
dotnet build TradyStrat.slnx -c Debug --nologo 2>&1 | grep -E "error CS" | head -100
```

For each unresolved-type error:
- Files referencing `Money`/`Currency`/`CurrencyPair`/`Percentage`/`Price`/`Quantity` → add `using TradyStrat.Domain.Shared.Money;`
- Files referencing `Exchange`/`Ticker`/`TimezoneId` → add `using TradyStrat.Domain.Shared.Market;`

Important: the type name `Money` collides with the namespace tail segment `Money`. Inside files that need both, the compiler resolves `Money` (the type) before `Money` (the namespace) only because the type is reachable via the `using`. If you hit a `CS0118` "Money is a namespace but used like a type" or similar, qualify the type as `Domain.Shared.Money.Money` once or — better — alias it: `using MoneyVo = TradyStrat.Domain.Shared.Money.Money;`. In practice this collision is rare because the namespace is unlikely to be referenced unqualified; flag any case where the compiler complains and resolve case-by-case.

Repeat build until clean.

- [ ] **Step 5: Run the full test suite**

```bash
dotnet test TradyStrat.slnx --nologo -c Debug 2>&1 | tail -10
```

Expected: 499 tests passed.

- [ ] **Step 6: Move corresponding test files**

```bash
mkdir -p TradyStrat.Domain.Tests/Shared/Money
mkdir -p TradyStrat.Domain.Tests/Shared/Market
```

For each existing test file under `TradyStrat.Domain.Tests/Shared/`, move it to the matching subfolder if it targets a Money/Market type. List first:

```bash
ls TradyStrat.Domain.Tests/Shared/
```

Then `git mv` each file (e.g. `MoneyTests.cs` → `Shared/Money/MoneyTests.cs`, `TickerTests.cs` → `Shared/Market/TickerTests.cs`, etc.). Update the namespace in each test file to match the new folder. Rebuild + re-test.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
refactor(domain): split Shared/ into Shared/Money + Shared/Market sub-namespaces

Money/Currency/CurrencyPair/Percentage/Price/Quantity → Shared/Money/
Exchange/Ticker/TimezoneId → Shared/Market/
DateRange remains at Shared/ root (truly cross-cutting).

Consumer using-statements updated where the compiler flagged them.
Test folder mirrors the production split.
EOF
)"
```

---

## Task 4: Move `Conviction` to Suggestions

**Files:**
- Move: `TradyStrat.Domain/Shared/Conviction.cs` → `TradyStrat.Domain/Suggestions/Conviction.cs`
- Move (test): `TradyStrat.Domain.Tests/Shared/ConvictionTests.cs` → `TradyStrat.Domain.Tests/Suggestions/ConvictionTests.cs`

Rationale: `Conviction` is only used inside the Suggestions flow (Suggestion, AiResponse, SuggestionMapper, etc.) — not a cross-cutting VO. Move it into its owning aggregate folder.

- [ ] **Step 1: git-mv production + test files**

```bash
git mv TradyStrat.Domain/Shared/Conviction.cs              TradyStrat.Domain/Suggestions/Conviction.cs
git mv TradyStrat.Domain.Tests/Shared/ConvictionTests.cs   TradyStrat.Domain.Tests/Suggestions/ConvictionTests.cs
```

- [ ] **Step 2: Update namespaces**

```csharp
// TradyStrat.Domain/Suggestions/Conviction.cs
namespace TradyStrat.Domain.Suggestions;

// TradyStrat.Domain.Tests/Suggestions/ConvictionTests.cs
namespace TradyStrat.Domain.Tests.Suggestions;
```

- [ ] **Step 3: Build and add usings**

```bash
dotnet build TradyStrat.slnx -c Debug --nologo 2>&1 | grep -E "error CS" | head -30
```

Files referencing `Conviction` that don't already import `TradyStrat.Domain.Suggestions` → add the using. Expect ~10–20 files (Cli/Mcp + Application/AiSuggestion + Infrastructure/AiSuggestion + a couple of test files).

- [ ] **Step 4: Build + test**

```bash
dotnet build TradyStrat.slnx -c Debug --nologo 2>&1 | tail -5
dotnet test TradyStrat.slnx --nologo -c Debug 2>&1 | tail -10
```

Expected: build succeeds; 499 tests pass.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
refactor(domain): move Conviction VO into Suggestions aggregate

Conviction is exclusively used by the Suggestion flow; it's not a
cross-cutting Shared concept. Co-locate it with the aggregate that
owns its semantics.
EOF
)"
```

---

## Task 5: Move foundational exceptions into `SeedWork/`

**Files:**
- Move: `TradyStrat.Domain/Exceptions/TradyStratException.cs` → `TradyStrat.Domain/SeedWork/TradyStratException.cs`
- Move: `TradyStrat.Domain/Exceptions/BusinessRuleViolationException.cs` → `TradyStrat.Domain/SeedWork/BusinessRuleViolationException.cs`

Rationale: `TradyStratException` is the abstract base for all domain exceptions — a seedwork-level primitive, conceptually a peer of `Entity`/`ValueObject`. `BusinessRuleViolationException` already imports `TradyStrat.Domain.SeedWork` and is paired with `IBusinessRule.cs`. After this task, `Exceptions/` contains only the 13 aggregate-specific exceptions, which Task 6 then disperses.

- [ ] **Step 1: git-mv both files**

```bash
git mv TradyStrat.Domain/Exceptions/TradyStratException.cs              TradyStrat.Domain/SeedWork/TradyStratException.cs
git mv TradyStrat.Domain/Exceptions/BusinessRuleViolationException.cs   TradyStrat.Domain/SeedWork/BusinessRuleViolationException.cs
```

- [ ] **Step 2: Update namespaces**

```csharp
// TradyStrat.Domain/SeedWork/TradyStratException.cs
namespace TradyStrat.Domain.SeedWork;

public abstract class TradyStratException(string message, Exception? inner = null) : Exception(message, inner);

// TradyStrat.Domain/SeedWork/BusinessRuleViolationException.cs
// (drop the `using TradyStrat.Domain.SeedWork;` line — it's now the file's own namespace)
namespace TradyStrat.Domain.SeedWork;

public sealed class BusinessRuleViolationException(IBusinessRule rule)
    : TradyStratException($"Business rule violated: {rule.Message}");
```

(Adjust the second class body to match the existing file — confirm shape with `Read` before editing.)

- [ ] **Step 3: Build and add usings**

```bash
dotnet build TradyStrat.slnx -c Debug --nologo 2>&1 | grep -E "error CS" | head -50
```

Files that import `TradyStrat.Domain.Exceptions` purely to reference `TradyStratException` or `BusinessRuleViolationException` → add `using TradyStrat.Domain.SeedWork;` and (in Task 6) the old Exceptions using will drop entirely.

Don't remove `using TradyStrat.Domain.Exceptions;` yet — many files still need it for the 13 aggregate exceptions left in that folder.

- [ ] **Step 4: Build + test**

```bash
dotnet build TradyStrat.slnx -c Debug --nologo 2>&1 | tail -5
dotnet test TradyStrat.slnx --nologo -c Debug 2>&1 | tail -10
```

Expected: build succeeds; 499 tests pass.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
refactor(domain): move TradyStratException + BusinessRuleViolation into SeedWork

Both are foundational types — TradyStratException is the abstract
base for every domain exception, BusinessRuleViolation pairs with
IBusinessRule. They belong in SeedWork next to Entity/ValueObject.
EOF
)"
```

---

## Task 6: Co-locate aggregate-specific exceptions

**Files:** 13 exception files move from `TradyStrat.Domain/Exceptions/` to the AR folder that owns the invariant they signal.

| Exception | Destination |
| --- | --- |
| `TradeValidationException` | `TradyStrat.Domain/Portfolio/` |
| `CsvImportException` | `TradyStrat.Domain/Portfolio/` (trade-import concern) |
| `DuplicateInstrumentException` | `TradyStrat.Domain/Instruments/` |
| `InstrumentNotFoundException` | `TradyStrat.Domain/Instruments/` |
| `InstrumentMetadataIncompleteException` | `TradyStrat.Domain/Instruments/` |
| `IndicatorComputationException` | `TradyStrat.Domain/Indicators/` |
| `FxRateUnavailableException` | `TradyStrat.Domain/MarketData/` (FxRate lives there) |
| `NoTradingDaysException` | `TradyStrat.Domain/MarketData/` (price-bar trading days) |
| `PriceFeedUnavailableException` | `TradyStrat.Domain/PriceFeed/` |
| `PolymarketUnavailableException` | `TradyStrat.Domain/Suggestions/` (Polymarket feeds suggestion citations) |
| `SettingValidationException` | `TradyStrat.Domain/Settings/` |
| `CurrencyMismatchException` | `TradyStrat.Domain/Shared/Money/` |
| `UnsupportedCurrencyException` | `TradyStrat.Domain/Shared/Money/` |

- [ ] **Step 1: git-mv every file**

```bash
git mv TradyStrat.Domain/Exceptions/TradeValidationException.cs              TradyStrat.Domain/Portfolio/TradeValidationException.cs
git mv TradyStrat.Domain/Exceptions/CsvImportException.cs                    TradyStrat.Domain/Portfolio/CsvImportException.cs
git mv TradyStrat.Domain/Exceptions/DuplicateInstrumentException.cs          TradyStrat.Domain/Instruments/DuplicateInstrumentException.cs
git mv TradyStrat.Domain/Exceptions/InstrumentNotFoundException.cs           TradyStrat.Domain/Instruments/InstrumentNotFoundException.cs
git mv TradyStrat.Domain/Exceptions/InstrumentMetadataIncompleteException.cs TradyStrat.Domain/Instruments/InstrumentMetadataIncompleteException.cs
git mv TradyStrat.Domain/Exceptions/IndicatorComputationException.cs         TradyStrat.Domain/Indicators/IndicatorComputationException.cs
git mv TradyStrat.Domain/Exceptions/FxRateUnavailableException.cs            TradyStrat.Domain/MarketData/FxRateUnavailableException.cs
git mv TradyStrat.Domain/Exceptions/NoTradingDaysException.cs                TradyStrat.Domain/MarketData/NoTradingDaysException.cs
git mv TradyStrat.Domain/Exceptions/PriceFeedUnavailableException.cs         TradyStrat.Domain/PriceFeed/PriceFeedUnavailableException.cs
git mv TradyStrat.Domain/Exceptions/PolymarketUnavailableException.cs        TradyStrat.Domain/Suggestions/PolymarketUnavailableException.cs
git mv TradyStrat.Domain/Exceptions/SettingValidationException.cs            TradyStrat.Domain/Settings/SettingValidationException.cs
git mv TradyStrat.Domain/Exceptions/CurrencyMismatchException.cs             TradyStrat.Domain/Shared/Money/CurrencyMismatchException.cs
git mv TradyStrat.Domain/Exceptions/UnsupportedCurrencyException.cs          TradyStrat.Domain/Shared/Money/UnsupportedCurrencyException.cs
```

- [ ] **Step 2: Update the namespace declaration in each moved file**

For each file, set the `namespace` line to match the destination folder. The expected values:

```csharp
// Portfolio/TradeValidationException.cs, Portfolio/CsvImportException.cs
namespace TradyStrat.Domain.Portfolio;

// Instruments/{Duplicate,InstrumentNotFound,InstrumentMetadataIncomplete}*.cs
namespace TradyStrat.Domain.Instruments;

// Indicators/IndicatorComputationException.cs
namespace TradyStrat.Domain.Indicators;

// MarketData/{FxRateUnavailable,NoTradingDays}*.cs
namespace TradyStrat.Domain.MarketData;

// PriceFeed/PriceFeedUnavailableException.cs
namespace TradyStrat.Domain.PriceFeed;

// Suggestions/PolymarketUnavailableException.cs
namespace TradyStrat.Domain.Suggestions;

// Settings/SettingValidationException.cs
namespace TradyStrat.Domain.Settings;

// Shared/Money/{CurrencyMismatch,UnsupportedCurrency}*.cs
namespace TradyStrat.Domain.Shared.Money;
```

Class bodies and constructors are untouched.

- [ ] **Step 3: Delete the now-empty `Exceptions/` folder**

```bash
ls TradyStrat.Domain/Exceptions/
```

Expected: empty (Task 5 cleared the two foundational files; Task 6 just cleared the remaining 13).

```bash
rmdir TradyStrat.Domain/Exceptions
```

- [ ] **Step 4: Build and add the new usings**

```bash
dotnet build TradyStrat.slnx -c Debug --nologo 2>&1 | grep -E "error CS" | head -100
```

For each unresolved-type error, look up the exception → destination row above and add the matching `using`. Common cases:
- Application/Cli/Infrastructure files that previously did `using TradyStrat.Domain.Exceptions;` and used multiple exception types will need to replace that single import with 2–4 specific ones, then drop the old `using` (which now refers to a non-existent namespace).
- The `Money.cs` file under `Shared/Money/` no longer needs `using TradyStrat.Domain.Exceptions;` because `CurrencyMismatchException` lives in its own namespace now — drop that line.
- Any file that *only* imported `TradyStrat.Domain.Exceptions` for `TradyStratException`/`BusinessRuleViolationException` should now import `TradyStrat.Domain.SeedWork` instead (handled in Task 5; verify here).

Iterate `dotnet build` until errors are 0.

- [ ] **Step 5: Run the full test suite**

```bash
dotnet test TradyStrat.slnx --nologo -c Debug 2>&1 | tail -10
```

Expected: 499 tests passed.

- [ ] **Step 6: Sanity-check no orphan `Domain.Exceptions` imports remain**

```bash
grep -rn "using TradyStrat.Domain.Exceptions" --include="*.cs" . 2>/dev/null | grep -v "/bin/" | grep -v "/obj/"
```

Expected: empty output. If anything matches, delete the orphan `using` line and rebuild.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
refactor(domain): co-locate aggregate-specific exceptions with their ARs

13 exception types move from Domain/Exceptions/ into the aggregate
folder whose invariant they signal (Portfolio, Instruments, Indicators,
MarketData, PriceFeed, Suggestions, Settings, Shared/Money). The
Domain/Exceptions/ folder is deleted — TradyStratException and
BusinessRuleViolationException already moved to SeedWork in the prior
commit, so the dump is gone entirely.

All consumer using-statements rewritten to the new namespaces.
EOF
)"
```

---

## Task 7: Final verification + tag + merge offer

**Files:** none — only verification commands and a tag.

- [ ] **Step 1: Full build + test run from a clean slate**

```bash
dotnet build TradyStrat.slnx -c Debug --nologo 2>&1 | tail -5
dotnet test TradyStrat.slnx --nologo -c Debug 2>&1 | tail -10
```

Expected: build succeeds with 0 warnings/errors; 499 tests passed (or whatever the Task 1 baseline was — must match exactly).

- [ ] **Step 2: Confirm Domain folder layout matches the plan**

```bash
find TradyStrat.Domain -maxdepth 3 -type d -not -path "*/bin*" -not -path "*/obj*" | sort
```

Expected output:

```
TradyStrat.Domain
TradyStrat.Domain/Abstractions
TradyStrat.Domain/Goals
TradyStrat.Domain/Goals/Events
TradyStrat.Domain/Indicators
TradyStrat.Domain/Indicators/Services
TradyStrat.Domain/Instruments
TradyStrat.Domain/Instruments/Events
TradyStrat.Domain/MarketData
TradyStrat.Domain/Portfolio
TradyStrat.Domain/Portfolio/Events
TradyStrat.Domain/PriceFeed
TradyStrat.Domain/SeedWork
TradyStrat.Domain/Settings
TradyStrat.Domain/Settings/Anthropic
TradyStrat.Domain/Settings/Polymarket
TradyStrat.Domain/Settings/Tickers
TradyStrat.Domain/Shared
TradyStrat.Domain/Shared/Market
TradyStrat.Domain/Shared/Money
TradyStrat.Domain/Suggestions
TradyStrat.Domain/Suggestions/Events
TradyStrat.Domain/Suggestions/Services
```

`Exceptions/` must NOT appear.

- [ ] **Step 3: Confirm Shared/ root is small**

```bash
ls TradyStrat.Domain/Shared/
```

Expected: just `DateRange.cs`, `Market/`, `Money/`.

- [ ] **Step 4: Confirm no orphan namespace references**

```bash
grep -rn "TradyStrat.Domain.Exceptions" --include="*.cs" . 2>/dev/null | grep -v "/bin/" | grep -v "/obj/"
```

Expected: empty.

- [ ] **Step 5: Tag the result**

```bash
git tag domain-folder-hygiene-done
```

- [ ] **Step 6: Offer merge**

Surface the worktree status to the user and ask whether to merge to `main` (via [`superpowers:finishing-a-development-branch`](../../../.claude/)).

---

## Self-review notes

- **Spec coverage:** Two-part goal — split Shared/ ✓ (Tasks 2–4), co-locate exceptions ✓ (Tasks 5–6). Verification + tag ✓ (Task 7).
- **No new types, no new tests:** This is a pure refactor. The TDD discipline collapses to "keep the 499-test baseline green after every task." If a test fails, that's a real regression, not a refactor artifact — investigate.
- **Worktree isolation:** Per stored preference (`feedback_collaboration_style.md`), multi-commit work uses an isolated worktree (Task 1).
- **Conviction-vs-Shared call:** Moved to Suggestions because every non-test consumer is in the AiSuggestion pipeline. Reversing this is a one-commit revert if the user disagrees.
- **`TradyStratException` placement:** Chose SeedWork over keeping a 2-file `Exceptions/` to fully kill the junk drawer. If the user prefers retaining a dedicated `Exceptions/` for these two roots, Task 5 becomes a no-op and the folder survives — easy to flip.
- **Test folder mirror:** Done as part of each production move (Tasks 2, 3, 4) rather than a separate cleanup pass, because doing it together keeps each commit's diff focused on one concept.
