# Goal Validation Exception Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce `GoalValidationException` in `Domain/Goals/`, swap the two `SettingValidationException` throws in `Goal.cs` to use it, and remove the cross-aggregate `using TradyStrat.Domain.Settings;` import from both `Goal.cs` and `GoalTests.cs`.

**Architecture:** Mirror the pattern of `SettingValidationException` (sealed class with `(string, Exception?)` constructor inheriting `TradyStratException`). Goal-specific exception lives next to `Goal.cs`, matching the post-Task-6 convention where aggregate-specific exceptions co-locate with their aggregate (TradeValidationException, DuplicateInstrumentException, IndicatorComputationException, etc.). UI catches `TradyStratException` base — no catch-site edits needed.

**Tech Stack:** .NET 10, xUnit v3, Shouldly. No new packages.

**Spec:** [`docs/superpowers/specs/2026-05-25-goal-validation-exception-design.md`](../specs/2026-05-25-goal-validation-exception-design.md).

**Baseline:** main `8ecee0e`, 505 tests pass, 0 warnings, 0 errors. Must match exactly after this change.

---

## File Structure

```
TradyStrat.Domain/Goals/
├── GoalValidationException.cs       ← NEW (sealed, inherits TradyStratException)
└── Goal.cs                          ← MODIFIED (swap 2 throws, drop `using Settings;`)

TradyStrat.Domain.Tests/Goals/
└── GoalTests.cs                     ← MODIFIED (swap 2 Should.Throw<> generic args, drop `using Settings;`)
```

Zero new tests, zero deleted tests — same 2 throw assertions, just asserting a different type.

---

## Task 1: Worktree + landing

**Files:**
- Create: `TradyStrat.Domain/Goals/GoalValidationException.cs`
- Modify: `TradyStrat.Domain/Goals/Goal.cs` (lines 4, 43, 58)
- Modify: `TradyStrat.Domain.Tests/Goals/GoalTests.cs` (lines 5, 49, 84)

- [ ] **Step 1: Create isolated worktree**

Per `superpowers:using-git-worktrees`. Branch name `goal-validation-exception`.

```bash
cd /Users/philippe/repo/gh-phmatray/TradyStrat
git worktree add .claude/worktrees/goal-validation-exception -b goal-validation-exception main
cd .claude/worktrees/goal-validation-exception
```

- [ ] **Step 2: Confirm baseline**

```bash
dotnet build TradyStrat.slnx -c Debug --nologo 2>&1 | tail -5
dotnet test TradyStrat.slnx --nologo -c Debug 2>&1 | tail -10
```

Expected: build succeeds 0/0. Tests: 200 Domain + 155 Application + 96 Infrastructure + 50 Cli + 4 E2E = **505 passed**, 0 failed, 0 skipped.

- [ ] **Step 3: Flip the two assertion types in `GoalTests.cs` to drive the test failure (TDD red)**

Edit `TradyStrat.Domain.Tests/Goals/GoalTests.cs`:

Around line 49, change:
```csharp
        Should.Throw<SettingValidationException>(
            () => goal.RetargetAmount(Money.Of(0m, Currency.Eur), new FixedClock(_now)));
```
to:
```csharp
        Should.Throw<GoalValidationException>(
            () => goal.RetargetAmount(Money.Of(0m, Currency.Eur), new FixedClock(_now)));
```

Around line 84, change:
```csharp
        Should.Throw<SettingValidationException>(
            () => goal.RescheduleDeadline(yesterday, clock));
```
to:
```csharp
        Should.Throw<GoalValidationException>(
            () => goal.RescheduleDeadline(yesterday, clock));
```

Leave all `using` lines alone for now — the next two steps will trigger the build errors that drive the implementation.

- [ ] **Step 4: Run the build to confirm RED**

```bash
dotnet build TradyStrat.slnx -c Debug --nologo 2>&1 | grep -E "error CS" | head -10
```

Expected: two `CS0246: The type or namespace name 'GoalValidationException' could not be found` errors in `GoalTests.cs` at the two edited lines.

If you see different errors, stop and report — the baseline diverged from what the plan expected.

- [ ] **Step 5: Create `GoalValidationException.cs` (minimal — drives test toward GREEN)**

Create `TradyStrat.Domain/Goals/GoalValidationException.cs`:

```csharp
namespace TradyStrat.Domain.Goals;

public sealed class GoalValidationException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
```

(The `TradyStratException` reference resolves via `using TradyStrat.Domain.SeedWork;` which the new file doesn't yet have. The next step adds it.)

- [ ] **Step 6: Add the `using TradyStrat.Domain.SeedWork;` directive to the new file**

Update `TradyStrat.Domain/Goals/GoalValidationException.cs` so it reads in full:

```csharp
using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Goals;

public sealed class GoalValidationException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
```

- [ ] **Step 7: Build — expect tests to compile, but two tests to FAIL at runtime**

```bash
dotnet build TradyStrat.slnx -c Debug --nologo 2>&1 | tail -5
```

Expected: 0 errors, 0 warnings. Build succeeds because `GoalValidationException` now exists (and `GoalTests.cs` already imports `TradyStrat.Domain.Goals`, so the type resolves via lexical lookup).

```bash
dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~GoalTests" --nologo -c Debug 2>&1 | tail -15
```

Expected: 2 test failures —
- `GoalTests.RetargetAmount_rejects_non_positive_target`: assertion expected `GoalValidationException` but got `SettingValidationException`.
- `GoalTests.RescheduleDeadline_rejects_past_dates`: same pattern.

If a different set of tests fails, stop and report.

- [ ] **Step 8: Swap the two throws in `Goal.cs` to `GoalValidationException`**

Edit `TradyStrat.Domain/Goals/Goal.cs`:

Around line 43, change:
```csharp
            throw new SettingValidationException(
                $"Target must be positive (was {newTarget}).");
```
to:
```csharp
            throw new GoalValidationException(
                $"Target must be positive (was {newTarget}).");
```

Around line 58, change:
```csharp
                throw new SettingValidationException(
                    $"Deadline must be today or later (was {newDeadline:O}, today is {today:O}).");
```
to:
```csharp
                throw new GoalValidationException(
                    $"Deadline must be today or later (was {newDeadline:O}, today is {today:O}).");
```

Class body, method signatures, message strings, and the rest of the file remain unchanged.

- [ ] **Step 9: Drop `using TradyStrat.Domain.Settings;` from `Goal.cs`**

After Step 8, `Goal.cs` no longer references any symbol from `TradyStrat.Domain.Settings`. Delete line 4 (`using TradyStrat.Domain.Settings;`).

Confirm by reading the file: the remaining usings should be `TradyStrat.Domain.Goals`, `TradyStrat.Domain.Goals.Events`, `TradyStrat.Domain.SeedWork`, and `TradyStrat.Domain.Shared.Money`.

- [ ] **Step 10: Run the full Goal tests to confirm GREEN**

```bash
dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~GoalTests" --nologo -c Debug 2>&1 | tail -10
```

Expected: all GoalTests pass (no specific count — the entire fixture should be green).

- [ ] **Step 11: Drop the now-orphan `using TradyStrat.Domain.Settings;` from `GoalTests.cs`**

Open `TradyStrat.Domain.Tests/Goals/GoalTests.cs`. Confirm the file no longer references any Settings symbol (the only previous reference was `SettingValidationException`, swapped in Step 3). Delete line 5 (`using TradyStrat.Domain.Settings;`).

- [ ] **Step 12: Full build + test sweep**

```bash
dotnet build TradyStrat.slnx -c Debug --nologo 2>&1 | tail -5
dotnet test TradyStrat.slnx --nologo -c Debug 2>&1 | tail -10
```

Expected: 0 errors, 0 warnings. **505 tests pass** (200 Domain + 155 Application + 96 Infrastructure + 50 Cli + 4 E2E). No failures, no skips, no count drift.

- [ ] **Step 13: Sanity check — confirm no residual cross-aggregate import**

```bash
grep -n "TradyStrat\.Domain\.Settings" TradyStrat.Domain/Goals/Goal.cs TradyStrat.Domain.Tests/Goals/GoalTests.cs
```

Expected: empty output (no matches in either file).

```bash
grep -rn "SettingValidationException" TradyStrat.Domain/Goals/ TradyStrat.Domain.Tests/Goals/ 2>/dev/null
```

Expected: empty output (no references to `SettingValidationException` anywhere under the Goals folders).

```bash
grep -rn "SettingValidationException" TradyStrat.Domain/Settings/ 2>/dev/null | wc -l
```

Expected: 9 (the 9 Settings VO throw sites remain untouched).

- [ ] **Step 14: Commit**

```bash
git add -A
git status
git commit -m "$(cat <<'EOF'
refactor(domain): introduce GoalValidationException, decouple Goal from Settings

Goal previously threw SettingValidationException for RetargetAmount /
RescheduleDeadline invariant violations, which forced an awkward
Goals → Settings using-statement. Introduce a Goals-owned exception
matching the post-Task-6 "exceptions live with their aggregate"
pattern. SettingValidationException remains in Settings/ for the
9 Settings VO call sites.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 15: Verify clean working tree**

```bash
git status
git log --oneline -3
```

Expected: clean working tree; the new commit on top of `8ecee0e` (main HEAD).

---

## Self-Review

**Spec coverage:**
- New `GoalValidationException` in `Domain/Goals/` → Step 5–6 ✅
- Inherits `TradyStratException` with `(string, Exception?)` constructor → Step 6 ✅
- Goal.cs swaps 2 throws → Step 8 ✅
- Drop `using TradyStrat.Domain.Settings;` from Goal.cs → Step 9 ✅
- GoalTests.cs swaps 2 Should.Throw<> generic args → Step 3 ✅
- Drop `using TradyStrat.Domain.Settings;` from GoalTests.cs → Step 11 ✅
- 505 tests still pass → Step 12 ✅
- SettingValidationException unchanged in Settings/ → Step 13 (final grep verifies the 9 Settings call sites survive) ✅
- UI catch sites untouched (catch `TradyStratException` base, unaffected by new subclass) → covered by the design, no task needed ✅

**Placeholder scan:** No "TBD" / "implement later" / "similar to" — all steps include actual code and exact commands.

**Type consistency:**
- `GoalValidationException` constructor signature: `(string message, Exception? inner = null)` — used identically in Step 6 and assumed in Step 3/8. ✅
- All step references use the same file paths (`TradyStrat.Domain/Goals/Goal.cs`, etc.). ✅
- Line numbers match the actual current file state (verified pre-write).
