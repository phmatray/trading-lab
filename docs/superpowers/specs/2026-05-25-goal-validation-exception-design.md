# Goal Validation Exception — Design

## Context

After the domain folder hygiene refactor (2026-05-25, tag `domain-folder-hygiene-done`), `Goal.cs` lives in `TradyStrat.Domain.Goals` but throws `SettingValidationException` (lives in `TradyStrat.Domain.Settings`) for two invariant violations:

- `RetargetAmount(...)` — when `newTarget` is empty or non-positive
- `RescheduleDeadline(...)` — when `newDeadline` is in the past

This forces `Goal.cs` to declare `using TradyStrat.Domain.Settings;`, an awkward cross-aggregate dependency. The exception's name and home namespace both suggest it belongs to the Settings world, where its 9 other call sites live (Anthropic/Polymarket/Tickers VOs).

## Goal

Decouple `Goal` from `Settings` by introducing a Goals-owned validation exception. Match the post-Task-6 pattern where each aggregate owns its specific exceptions (TradeValidationException, DuplicateInstrumentException, IndicatorComputationException, …).

## Approach

Introduce `GoalValidationException` in `TradyStrat.Domain/Goals/`, mirroring `SettingValidationException`'s shape exactly:

```csharp
namespace TradyStrat.Domain.Goals;

public sealed class GoalValidationException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
```

(The `inner` parameter matches `SettingValidationException`'s signature for consistency, even though Goal's current call sites pass only `message`.)

Then:
1. `Goal.cs` — swap the two `throw new SettingValidationException(...)` calls to `throw new GoalValidationException(...)`. Drop `using TradyStrat.Domain.Settings;` (no other Settings symbol referenced).
2. `GoalTests.cs` — swap the two `Should.Throw<SettingValidationException>(...)` assertions to `Should.Throw<GoalValidationException>(...)`. Update the test file's `using` directive to import `TradyStrat.Domain.Goals` instead of `TradyStrat.Domain.Settings` (if it imports Settings — confirm during impl).

## Scope

**Production files (2):**
- New: `TradyStrat.Domain/Goals/GoalValidationException.cs`
- Modified: `TradyStrat.Domain/Goals/Goal.cs`

**Test files (1):**
- Modified: `TradyStrat.Domain.Tests/Goals/GoalTests.cs`

**Expected build/test outcome:**
- 0 errors, 0 warnings.
- 505 tests pass (no test count change — same 2 throw-assertion tests, just asserting a different type).

## Non-goals

- Renaming/restructuring `SettingValidationException` (it stays in `Settings/`).
- Adopting the `IBusinessRule` + `CheckRule` pattern (separate larger initiative).
- Touching any aggregate other than Goal.
- Fixing the pre-existing folder/namespace mismatch in `Goal.cs` (still declares `namespace TradyStrat.Domain;` despite living in `Goals/`) — separate follow-up.

## Catch-site safety

UI catches reference `TradyStratException` (the base) at:
- `TradyStrat/Features/Settings/SettingsPage.razor.cs:59`
- `TradyStrat/Features/Settings/Components/FocusTickerForm.razor.cs:58`
- `TradyStrat/Features/Settings/Components/AnthropicSettingsForm.razor.cs:76`
- `TradyStrat/Features/Settings/Components/PolymarketSettingsForm.razor.cs:86`

`GoalValidationException` inherits `TradyStratException`, so all four sites continue to catch correctly. No UI catch-site needs editing.

## Commit

Single commit on its own branch (`goal-validation-exception` worktree), tag-free, fast-forward to main when verified.

Suggested message:

```
refactor(domain): introduce GoalValidationException, decouple Goal from Settings

Goal previously threw SettingValidationException for RetargetAmount /
RescheduleDeadline invariant violations, which forced an awkward
Goals → Settings using-statement. Introduce a Goals-owned exception
matching the post-Task-6 "exceptions live with their aggregate"
pattern. SettingValidationException remains in Settings/ for the
9 Settings VO call sites.
```
