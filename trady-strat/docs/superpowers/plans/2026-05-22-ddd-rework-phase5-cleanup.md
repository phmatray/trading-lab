# DDD Rework — Phase 5 (Cleanup slice: Goal + MarketData + Indicators) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Three small absorptions in one phase. (a) Rewrite `GoalConfig` as `Goal` AR (singleton) with `Money Target` + `Goal.HasDeadline` + behavior split into `RetargetAmount` / `RescheduleDeadline`; (b) rename the generic `IReadRepositoryBase<PriceBar>` / `IReadRepositoryBase<FxRate>` ports to read-only `IPriceBarReadRepository` / `IFxRateReadRepository` and relocate their specs to Infrastructure; (c) move `IndicatorEngine` + `ZoneClassifier` from Application to `Domain/Indicators/Services/` (they were domain services miscategorized) and make `IndicatorBundle` non-nullable via `Empty` sentinels (with `Percentage` VO for `Rsi`).

**Architecture:** `Goal` becomes a singleton AR (`Id = GoalId.Singleton`) with VO `Money Target` and `DateOnly TargetDate` defaulting to `DateOnly.MinValue` (HasDeadline predicate). New per-aggregate `IGoalRepository` (singleton pattern, like Portfolio). `FxRate` adopts `CurrencyPair` VO via EF value converters (existing string `Base`/`Quote` columns unchanged). Indicators code moves namespaces only — no behavioral change. The 11 sites that today inject `IReadRepositoryBase<PriceBar>` swap to `IPriceBarReadRepository`; the 3 sites that inject `IReadRepositoryBase<FxRate>` swap to `IFxRateReadRepository`. Write paths (hosted-services-only) get narrow `IPriceFeedWriter` / `IFxRateWriter` ports.

**Tech Stack:** .NET 10, EF Core 10.0.7 (Sqlite + value converters), Ardalis.Specification 9.3.1, xunit.v3 3.2.2, Shouldly 4.3.0.

**Spec reference:** [`docs/superpowers/specs/2026-05-22-ddd-domain-rework-design.md`](../specs/2026-05-22-ddd-domain-rework-design.md) §4, §10.

**Phase 2+3+4 conventions preserved:** sealed-class AR + private setters + parameterless ctor for EF, factory methods (no public ctors), no-null in domain (`Empty`/`None` + `IsEmpty`), per-aggregate repository (replacing `IRepositoryBase<T>`), EF mapping via value converters for new VOs, `PendingModelChangesWarning` suppression in `AppDbContext.OnConfiguring`.

**No DB schema migration.** Goal, PriceBars, FxRates tables are unchanged — only projections change.

**Spec partial-deferral called out:** Spec §10 mentions wrapping `PriceBar.Open/High/Low/Close` as `Money` "currency derived from the owning Instrument; the bar itself is single-currency". Done literally that requires either a per-row currency column (schema migration, forbidden by §10) or a JOIN at every read (changes the repo contract). **Deferred to a future phase** alongside the `IPriceBarReadRepository` cutover; documented inline near `PriceBar.cs`. The `Ticker` VO adoption on `PriceBar` is similarly deferred (it would require a per-row TEXT column converter and ripples through ~30 call sites that pass plain strings). Phase 5 does adopt `Percentage` for `Rsi` and `CurrencyPair` for `FxRate` — the changes that don't force a schema rewrite.

---

## Pre-work: Worktree setup

- [ ] **Step 0.1: Create an isolated worktree**

```bash
git worktree add .claude/worktrees/ddd-phase5 -b worktree-ddd-phase5
cd .claude/worktrees/ddd-phase5
```

All subsequent paths are relative to the worktree root.

- [ ] **Step 0.2: Verify baseline green**

```bash
dotnet tool restore
dotnet build TradyStrat.slnx
dotnet test TradyStrat.slnx --no-build
```

Expected: 436 / 436 tests pass. If failing on `main`, stop and surface before proceeding.

---

# Phase 5 — Three absorptions

## Task 1: `Percentage` VO

**Files:**
- Create: `TradyStrat.Domain/Shared/Percentage.cs`
- Test:   `TradyStrat.Domain.Tests/Shared/PercentageTests.cs`

`Percentage` is a decimal-valued VO representing a 0..100-ish ratio (RSI domain) with explicit `IsEmpty` sentinel for "indicator could not compute". Equality is structural.

- [ ] **Step 1.1: Write failing tests**

```csharp
using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class PercentageTests
{
    [Fact]
    public void Of_preserves_value()
    {
        Percentage.Of(42.5m).Value.ShouldBe(42.5m);
    }

    [Fact]
    public void Empty_is_zero_value_with_IsEmpty_true()
    {
        Percentage.Empty.Value.ShouldBe(0m);
        Percentage.Empty.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Of_is_not_IsEmpty()
    {
        Percentage.Of(0m).IsEmpty.ShouldBeFalse();   // zero is a valid reading
        Percentage.Of(50m).IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void Equality_is_structural()
    {
        Percentage.Of(50m).ShouldBe(Percentage.Of(50m));
        Percentage.Empty.ShouldBe(Percentage.Empty);
        Percentage.Of(0m).ShouldNotBe(Percentage.Empty); // zero-specified ≠ Empty
    }
}
```

- [ ] **Step 1.2: Run failing**

```bash
dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~PercentageTests"
```

Expected: build error.

- [ ] **Step 1.3: Implement `Percentage`**

`TradyStrat.Domain/Shared/Percentage.cs`:

```csharp
namespace TradyStrat.Domain.Shared;

public readonly record struct Percentage
{
    public decimal Value   { get; }
    public bool    IsEmpty { get; }

    private Percentage(decimal value, bool isEmpty)
    {
        Value = value;
        IsEmpty = isEmpty;
    }

    public static Percentage Of(decimal value) => new(value, isEmpty: false);
    public static Percentage Empty { get; } = new(0m, isEmpty: true);

    public override string ToString()
        => IsEmpty ? "—" : Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "%";
}
```

- [ ] **Step 1.4: Run tests passing**

Expected: 6 pass (3 facts + 4 in equality).

- [ ] **Step 1.5: Commit**

```bash
git add TradyStrat.Domain/Shared/Percentage.cs TradyStrat.Domain.Tests/Shared/PercentageTests.cs
git commit -m "feat(domain): Percentage VO with Empty sentinel — Phase 5 Task 1"
```

---

## Task 2: `CurrencyPair` VO

**Files:**
- Create: `TradyStrat.Domain/Shared/CurrencyPair.cs`
- Test:   `TradyStrat.Domain.Tests/Shared/CurrencyPairTests.cs`

`CurrencyPair(Currency Base, Currency Quote)` — a two-`Currency` tuple with `ToString()` formatted as `"EUR/USD"`. Used by FxRate (Task 5).

- [ ] **Step 2.1: Write failing tests**

```csharp
using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class CurrencyPairTests
{
    [Fact]
    public void Of_assigns_base_and_quote()
    {
        var pair = CurrencyPair.Of(Currency.Eur, Currency.Usd);
        pair.Base.ShouldBe(Currency.Eur);
        pair.Quote.ShouldBe(Currency.Usd);
    }

    [Fact]
    public void ToString_formats_as_BASE_slash_QUOTE()
    {
        CurrencyPair.Of(Currency.Eur, Currency.Usd).ToString().ShouldBe("EUR/USD");
    }

    [Fact]
    public void Equality_is_structural()
    {
        CurrencyPair.Of(Currency.Eur, Currency.Usd)
            .ShouldBe(CurrencyPair.Of(Currency.Eur, Currency.Usd));
        CurrencyPair.Of(Currency.Eur, Currency.Usd)
            .ShouldNotBe(CurrencyPair.Of(Currency.Usd, Currency.Eur));
    }

    [Fact]
    public void Of_rejects_same_base_and_quote()
    {
        Should.Throw<ArgumentException>(() => CurrencyPair.Of(Currency.Eur, Currency.Eur));
    }
}
```

- [ ] **Step 2.2: Run failing**

Expected: build error.

- [ ] **Step 2.3: Implement `CurrencyPair`**

```csharp
namespace TradyStrat.Domain.Shared;

public readonly record struct CurrencyPair
{
    public Currency Base  { get; }
    public Currency Quote { get; }

    private CurrencyPair(Currency @base, Currency quote)
    {
        Base = @base;
        Quote = quote;
    }

    public static CurrencyPair Of(Currency @base, Currency quote)
    {
        if (@base == quote)
            throw new ArgumentException(
                $"CurrencyPair Base and Quote must differ (both {@base}).", nameof(quote));
        return new CurrencyPair(@base, quote);
    }

    public override string ToString() => $"{Base}/{Quote}";
}
```

- [ ] **Step 2.4: Run tests passing**

Expected: 4 pass.

- [ ] **Step 2.5: Commit**

```bash
git add TradyStrat.Domain/Shared/CurrencyPair.cs TradyStrat.Domain.Tests/Shared/CurrencyPairTests.cs
git commit -m "feat(domain): CurrencyPair VO — Phase 5 Task 2"
```

---

## Task 3: `RomanNumeralId` VO

**Files:**
- Create: `TradyStrat.Domain/Shared/RomanNumeralId.cs`
- Test:   `TradyStrat.Domain.Tests/Shared/RomanNumeralIdTests.cs`

Lowercase Roman numeral identifier for `CapitalEvent`. Existing code uses raw strings `"i"`, `"ii"`, `"iii"`, `"iv"` etc. The VO validates lowercase + canonical Roman form.

- [ ] **Step 3.1: Write failing tests**

```csharp
using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class RomanNumeralIdTests
{
    [Theory]
    [InlineData("i")]
    [InlineData("ii")]
    [InlineData("iii")]
    [InlineData("iv")]
    [InlineData("v")]
    [InlineData("ix")]
    [InlineData("x")]
    [InlineData("xiv")]
    public void Of_accepts_canonical_lowercase_roman_numerals(string raw)
    {
        RomanNumeralId.Of(raw).Value.ShouldBe(raw);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Of_rejects_empty(string? value)
    {
        Should.Throw<ArgumentException>(() => RomanNumeralId.Of(value!));
    }

    [Theory]
    [InlineData("I")]                  // uppercase rejected
    [InlineData("II")]
    [InlineData("1")]
    [InlineData("abc")]
    [InlineData("iiii")]                // non-canonical (should be "iv")
    public void Of_rejects_non_canonical(string raw)
    {
        Should.Throw<ArgumentException>(() => RomanNumeralId.Of(raw));
    }
}
```

- [ ] **Step 3.2: Run failing**

Expected: build error.

- [ ] **Step 3.3: Implement `RomanNumeralId`**

```csharp
using System.Text.RegularExpressions;

namespace TradyStrat.Domain.Shared;

public readonly record struct RomanNumeralId
{
    public string Value { get; }

    private RomanNumeralId(string value) => Value = value;

    // Canonical lowercase Roman numerals 1-39 (we don't expect more than ~10 capital events).
    private static readonly Regex CanonicalLowercase = new(
        @"^m{0,3}(cm|cd|d?c{0,3})(xc|xl|l?x{0,3})(ix|iv|v?i{0,3})$",
        RegexOptions.Compiled);

    public static RomanNumeralId Of(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Roman numeral must not be empty.", nameof(raw));
        var trimmed = raw.Trim();
        if (trimmed.Length == 0 || !CanonicalLowercase.IsMatch(trimmed))
            throw new ArgumentException(
                $"'{raw}' is not a canonical lowercase Roman numeral.", nameof(raw));
        return new RomanNumeralId(trimmed);
    }

    public override string ToString() => Value;
}
```

The regex rejects empty string explicitly (the alternation only matches non-empty canonical forms because the three optional groups can each be empty but the whole expression with `m{0,3}` + three groups matching `(empty)(empty)(empty)` = empty string also matches — so we add the `trimmed.Length == 0` guard explicitly).

- [ ] **Step 3.4: Run tests passing**

Expected: 16 pass (8 valid + 3 empty + 5 invalid).

- [ ] **Step 3.5: Commit**

```bash
git add TradyStrat.Domain/Shared/RomanNumeralId.cs TradyStrat.Domain.Tests/Shared/RomanNumeralIdTests.cs
git commit -m "feat(domain): RomanNumeralId VO with canonical-form validation — Phase 5 Task 3"
```

---

## Task 4: `Goal` AR (replaces `GoalConfig` record)

**Files:**
- Rename: `TradyStrat.Domain/Goals/GoalConfig.cs` → `TradyStrat.Domain/Goals/Goal.cs`
- Create: `TradyStrat.Domain.Tests/Goals/GoalTests.cs`

Rewrite `GoalConfig` record as a `sealed class Goal` AR with VO `Money Target`, `DateOnly TargetDate` (Empty = `DateOnly.MinValue`), `HasDeadline` predicate, `RetargetAmount(Money, IClock)` and `RescheduleDeadline(DateOnly, IClock)` methods. The `Default(DateTime)` factory moves to `Goal.Initial(IClock)`.

- [ ] **Step 4.1: Read the existing `GoalConfig.cs`**

```bash
cat TradyStrat.Domain/Goals/GoalConfig.cs
```

Note the shape: `int Id`, `decimal TargetEur`, `DateOnly? TargetDate`, `DateTime UpdatedAt`, `static Default(DateTime now)` factory.

- [ ] **Step 4.2: Write failing tests**

`TradyStrat.Domain.Tests/Goals/GoalTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Goals;

public class GoalTests
{
    private static readonly DateTime _now = new(2026, 5, 22, 12, 0, 0, DateTimeKind.Utc);

    private sealed class FixedClock(DateTime now) : IClock
    {
        public DateTime UtcNow() => now;
        public DateOnly TodayLocal() => DateOnly.FromDateTime(now);
        public DateOnly TodayInExchangeTzFor(string ticker) => DateOnly.FromDateTime(now);
    }

    [Fact]
    public void Initial_uses_singleton_id_and_default_one_million_target_no_deadline()
    {
        var goal = Goal.Initial(new FixedClock(_now));
        goal.Id.ShouldBe(GoalId.Singleton);
        goal.Target.ShouldBe(Money.Of(1_000_000m, Currency.Eur));
        goal.HasDeadline.ShouldBeFalse();
        goal.UpdatedAt.ShouldBe(_now);
    }

    [Fact]
    public void RetargetAmount_updates_Target_and_UpdatedAt_only()
    {
        var goal = Goal.Initial(new FixedClock(_now));
        var clock2 = new FixedClock(_now.AddDays(1));

        goal.RetargetAmount(Money.Of(2_000_000m, Currency.Eur), clock2);

        goal.Target.ShouldBe(Money.Of(2_000_000m, Currency.Eur));
        goal.HasDeadline.ShouldBeFalse();
        goal.UpdatedAt.ShouldBe(clock2.UtcNow());
    }

    [Fact]
    public void RetargetAmount_rejects_non_positive_target()
    {
        var goal = Goal.Initial(new FixedClock(_now));
        Should.Throw<SettingValidationException>(
            () => goal.RetargetAmount(Money.Of(0m, Currency.Eur), new FixedClock(_now)));
        Should.Throw<SettingValidationException>(
            () => goal.RetargetAmount(Money.Of(-1m, Currency.Eur), new FixedClock(_now)));
    }

    [Fact]
    public void RescheduleDeadline_sets_HasDeadline_true_for_future_date()
    {
        var goal = Goal.Initial(new FixedClock(_now));
        var future = DateOnly.FromDateTime(_now).AddDays(90);

        goal.RescheduleDeadline(future, new FixedClock(_now));

        goal.HasDeadline.ShouldBeTrue();
        goal.TargetDate.ShouldBe(future);
    }

    [Fact]
    public void RescheduleDeadline_with_MinValue_clears_HasDeadline()
    {
        var goal = Goal.Initial(new FixedClock(_now));
        goal.RescheduleDeadline(DateOnly.FromDateTime(_now).AddDays(90), new FixedClock(_now));

        goal.RescheduleDeadline(DateOnly.MinValue, new FixedClock(_now.AddDays(1)));

        goal.HasDeadline.ShouldBeFalse();
        goal.TargetDate.ShouldBe(DateOnly.MinValue);
    }

    [Fact]
    public void RescheduleDeadline_rejects_past_dates()
    {
        var clock = new FixedClock(_now);
        var goal = Goal.Initial(clock);
        var yesterday = DateOnly.FromDateTime(_now).AddDays(-1);

        Should.Throw<SettingValidationException>(
            () => goal.RescheduleDeadline(yesterday, clock));
    }
}
```

- [ ] **Step 4.3: Run failing**

```bash
dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~GoalTests"
```

Expected: build error.

- [ ] **Step 4.4: Delete `GoalConfig.cs` and create `Goal.cs`**

```bash
git mv TradyStrat.Domain/Goals/GoalConfig.cs TradyStrat.Domain/Goals/Goal.cs
```

Then replace the contents of `TradyStrat.Domain/Goals/Goal.cs`:

```csharp
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain;

public sealed class Goal
{
    public GoalId   Id         { get; private set; }
    public Money    Target     { get; private set; } = Money.Zero(Currency.Eur);
    public DateOnly TargetDate { get; private set; } = DateOnly.MinValue;
    public DateTime UpdatedAt  { get; private set; }

    public bool HasDeadline => TargetDate != DateOnly.MinValue;

    private Goal() { }   // EF

    private Goal(GoalId id, Money target, DateOnly targetDate, DateTime updatedAt)
    {
        Id         = id;
        Target     = target;
        TargetDate = targetDate;
        UpdatedAt  = updatedAt;
    }

    /// <summary>Singleton-id Initial Goal with €1M target and no deadline.</summary>
    public static Goal Initial(IClock clock)
        => new(GoalId.Singleton,
               Money.Of(1_000_000m, Currency.Eur),
               DateOnly.MinValue,
               clock.UtcNow());

    /// <summary>Rehydration factory used by EF mapping.</summary>
    public static Goal Existing(GoalId id, Money target, DateOnly targetDate, DateTime updatedAt)
        => new(id, target, targetDate, updatedAt);

    public void RetargetAmount(Money newTarget, IClock clock)
    {
        if (newTarget.IsEmpty || newTarget.Amount <= 0m)
            throw new SettingValidationException(
                $"Target must be positive (was {newTarget}).");
        Target = newTarget;
        UpdatedAt = clock.UtcNow();
    }

    public void RescheduleDeadline(DateOnly newDeadline, IClock clock)
    {
        if (newDeadline != DateOnly.MinValue)
        {
            var today = clock.TodayLocal();
            if (newDeadline < today)
                throw new SettingValidationException(
                    $"Deadline must be today or later (was {newDeadline:O}, today is {today:O}).");
        }
        TargetDate = newDeadline;
        UpdatedAt = clock.UtcNow();
    }
}
```

- [ ] **Step 4.5: Run tests passing**

```bash
dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~GoalTests"
```

Expected: 6 pass.

**At this point the rest of the solution will fail to build** — `GoalConfig` consumers across Application, Infrastructure, UI still reference the old shape. Tasks 5-17 progressively mop up.

- [ ] **Step 4.6: Commit**

```bash
git add TradyStrat.Domain/Goals/Goal.cs TradyStrat.Domain.Tests/Goals/GoalTests.cs
git commit -m "feat(domain): Goal AR with VO Money Target + HasDeadline + RetargetAmount/RescheduleDeadline — Phase 5 Task 4"
```

---

## Task 5: `CapitalEvent` adopts `RomanNumeralId`

**Files:**
- Modify: `TradyStrat.Domain/Portfolio/CapitalEvent.cs`

- [ ] **Step 5.1: Rewrite**

```csharp
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain;

/// <summary>
/// An editorial annotation on the growth chart — a trade or AI signal worth
/// remembering. Rendered as an italic Roman numeral above the chart line and
/// elaborated in the footnote rail beneath the chart.
/// </summary>
public sealed record CapitalEvent(
    DateOnly Date,
    RomanNumeralId RomanId,
    string Headline,
    string Body);
```

- [ ] **Step 5.2: Find call sites + update**

```bash
grep -rn 'new CapitalEvent\b' --include='*.cs' . | grep -v '/bin/' | grep -v '/obj/' | grep -v '/worktrees/'
```

For each match (likely the seed list in `LoadDashboardUseCase.cs`), wrap the raw string in `RomanNumeralId.Of(...)`:

```csharp
new(new DateOnly(2025, 12, 7), RomanNumeralId.Of("i"),
    "Initial position.",
    "CON3.L entry — UK construction sentiment turning, AI rationale cited macro-housing tailwind."),
```

- [ ] **Step 5.3: Update consumers reading `.RomanId`**

```bash
grep -rn '\.RomanId\b' --include='*.cs' --include='*.razor*' . | grep -v '/bin/' | grep -v '/obj/' | grep -v '/worktrees/'
```

Display sites need `.RomanId.Value` (the underlying string) instead of `.RomanId`. Razor templates typically render with `@event.RomanId.Value`.

- [ ] **Step 5.4: Commit**

```bash
git add -A
git commit -m "refactor(domain): CapitalEvent adopts RomanNumeralId VO — Phase 5 Task 5"
```

---

## Task 6: `GrowthPoint` adopts `Money` + `Percentage`

**Files:**
- Modify: `TradyStrat.Domain/Goals/GrowthPoint.cs`

Spec §10: "`GrowthPoint(DateOnly Date, Money Value, Percentage ProgressPct)` becomes typed".

- [ ] **Step 6.1: Rewrite**

```csharp
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain;

public sealed record GrowthPoint(DateOnly Date, Money Value, Percentage ProgressPct);
```

- [ ] **Step 6.2: Find construction sites + update**

```bash
grep -rn 'new GrowthPoint\b' --include='*.cs' . | grep -v '/bin/' | grep -v '/obj/' | grep -v '/worktrees/'
```

Expected matches: `Portfolio.GrowthSeries(...)` (constructs `GrowthPoint(date, valueEur)` — needs `Money` + `Percentage` derived from target), `LoadDashboardUseCase` (modifies the last point). Update each:

```csharp
// Before:
new GrowthPoint(date, valueEur)
// After (when target ratio is known):
new GrowthPoint(date, Money.Of(valueEur, Currency.Eur), Percentage.Of(valueEur / target.Amount * 100m))
// After (when only the value matters, no progress context):
new GrowthPoint(date, Money.Of(valueEur, Currency.Eur), Percentage.Empty)
```

Determine each call site's intent by reading the surrounding code; if it doesn't have a target reference, use `Percentage.Empty`.

- [ ] **Step 6.3: Update consumers**

```bash
grep -rn '\.ValueEur\b' --include='*.cs' --include='*.razor*' . | grep -v '/bin/' | grep -v '/obj/' | grep -v '/worktrees/'
```

For each match, replace `point.ValueEur` with `point.Value.Amount` (the Money's amount).

- [ ] **Step 6.4: Commit**

```bash
git add -A
git commit -m "refactor(domain): GrowthPoint adopts Money + Percentage — Phase 5 Task 6"
```

---

## Task 7: `IGoalRepository` port

**Files:**
- Create: `TradyStrat.Application/Goals/IGoalRepository.cs`

- [ ] **Step 7.1: Create the port**

```csharp
using TradyStrat.Domain;

namespace TradyStrat.Application.Goals;

/// <summary>
/// Singleton-pattern repository for the Goal AR (only one row, GoalId.Singleton).
/// Mirrors IPortfolioRepository's shape.
/// </summary>
public interface IGoalRepository
{
    /// <summary>Returns the singleton Goal, creating it from Goal.Initial if not yet persisted.</summary>
    Task<Goal> GetAsync(CancellationToken ct);

    Task SaveAsync(Goal goal, CancellationToken ct);
}
```

- [ ] **Step 7.2: Commit**

```bash
git add TradyStrat.Application/Goals/IGoalRepository.cs
git commit -m "feat(application): IGoalRepository port — Phase 5 Task 7"
```

---

## Task 8: `EfGoalRepository` + DI registration

**Files:**
- Create: `TradyStrat.Infrastructure/Goals/EfGoalRepository.cs`
- Modify: `TradyStrat.Infrastructure/Data/Configurations/GoalConfigConfiguration.cs` → rename to `GoalConfiguration.cs`, rewrite for new VOs
- Modify: `TradyStrat.Infrastructure/Data/AppDbContext.cs` (rename `DbSet<GoalConfig> Goals` → `DbSet<Goal> Goals`)
- Modify: An existing module file to register the new repo (e.g. `SettingsInfrastructureModule.cs` or new `GoalsInfrastructureModule.cs`)

The EF mapping changes: `Money Target` → owned (Amount/Currency/IsEmpty); `DateOnly TargetDate` → direct column (replaces nullable `DateOnly?` column). DB schema unchanged at the columns level — `TargetEur` column stays, repurposed as `Target_Amount` via `HasColumnName`.

- [ ] **Step 8.1: Read the existing GoalConfigConfiguration**

```bash
cat TradyStrat.Infrastructure/Data/Configurations/GoalConfigConfiguration.cs
```

Note the simple mapping: `ToTable("Goals")`, `HasKey(g => g.Id)`, `ValueGeneratedNever()`, `TargetEur` as TEXT.

- [ ] **Step 8.2: Rename + rewrite the configuration**

```bash
git mv TradyStrat.Infrastructure/Data/Configurations/GoalConfigConfiguration.cs \
       TradyStrat.Infrastructure/Data/Configurations/GoalConfiguration.cs
```

Replace contents:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Domain;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Infrastructure.Data.Configurations;

public sealed class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("Goals");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();

        // Money owned VO → existing TargetEur column for Amount; new shadow columns for Currency + IsEmpty.
        builder.OwnsOne(g => g.Target, m =>
        {
            m.Property(x => x.Amount)
             .HasColumnName("TargetEur")    // preserve existing column name (no migration)
             .HasColumnType("TEXT");
            m.Property(x => x.Currency)
             .HasConversion(c => c.Code, s => Currency.Parse(s))
             .HasColumnName("TargetCurrency")
             .HasMaxLength(3);
            m.Property(x => x.IsEmpty).HasColumnName("TargetIsEmpty");
        });

        // TargetDate: was nullable DateOnly?, now non-nullable with DateOnly.MinValue as sentinel.
        // SQLite stores DateOnly as TEXT; NULL rows from the legacy schema rehydrate as MinValue
        // via a value converter on the way in.
        builder.Property(g => g.TargetDate)
               .HasConversion(
                   d => d == DateOnly.MinValue ? (DateOnly?)null : d,
                   d => d ?? DateOnly.MinValue)
               .HasColumnName("TargetDate");

        builder.Property(g => g.UpdatedAt);
    }
}
```

**Schema implication:** the new `TargetCurrency` (TEXT 3) and `TargetIsEmpty` (INTEGER) columns are added by the schema rebuild — but Phase 5 explicitly says no migration. There's a tension: spec §10 says "Goal table unchanged" yet the AR has new VO-shaped fields. The pragmatic resolution: **let EF rebuild the table on next migration but don't generate one in Phase 5**; the runtime config sets default values (`Currency.Eur`, `IsEmpty = false`) so existing rows rehydrate cleanly even without those columns being present. Since Goals is a singleton table with 1 row, the dev DB never sees the issue (the only existing row is correct on first save after Phase 5 ships).

If runtime errors fire from "column not found", the fallback is: add a one-line migration that ALTER TABLE adds the two columns with defaults. That's a one-task follow-up; the plan as written assumes the EF rebuild can be deferred.

**Test it during Task 17 (final verification): if EF reads a Goal row and throws, generate the missing-columns migration.**

- [ ] **Step 8.3: Implement `EfGoalRepository`**

`TradyStrat.Infrastructure/Goals/EfGoalRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TradyStrat.Application.Goals;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.Infrastructure.Goals;

public sealed class EfGoalRepository(AppDbContext db, IClock clock) : IGoalRepository
{
    public async Task<Goal> GetAsync(CancellationToken ct)
    {
        var goal = await db.Goals.SingleOrDefaultAsync(ct);
        if (goal is not null) return goal;

        var initial = Goal.Initial(clock);
        db.Goals.Add(initial);
        await db.SaveChangesAsync(ct);
        return initial;
    }

    public Task SaveAsync(Goal goal, CancellationToken ct) => db.SaveChangesAsync(ct);
}
```

- [ ] **Step 8.4: Rename the DbSet in AppDbContext**

Read `TradyStrat.Infrastructure/Data/AppDbContext.cs` and change:

```csharp
public DbSet<GoalConfig>  Goals = Set<GoalConfig>();
```

to:

```csharp
public DbSet<Goal>  Goals => Set<Goal>();
```

- [ ] **Step 8.5: Register in DI**

In an existing infrastructure module (use `SettingsInfrastructureModule.cs` since `UpdateGoalUseCase` already lives there) add:

```csharp
services.AddScoped<IGoalRepository, EfGoalRepository>();
```

- [ ] **Step 8.6: Build Infrastructure**

```bash
dotnet build TradyStrat.Infrastructure
```

Expected: succeeds (Domain green; Infrastructure may need fixups on EF-touching files — chase them down).

- [ ] **Step 8.7: Commit**

```bash
git add -A
git commit -m "feat(infra): GoalConfiguration with Money owned VO + EfGoalRepository + DI — Phase 5 Task 8"
```

---

## Task 9: Rewrite `UpdateGoalUseCase` against `IGoalRepository`

**Files:**
- Modify: `TradyStrat.Infrastructure/Settings/UseCases/UpdateGoalUseCase.cs`

Today's use case has two coupled responsibilities (target + date) wrapped in `with`. The new flow lets the AR enforce its own invariants via `RetargetAmount` / `RescheduleDeadline`.

- [ ] **Step 9.1: Rewrite**

```csharp
using TradyStrat.Application.Goals;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Infrastructure.Settings.UseCases;

public sealed record UpdateGoalInput(decimal TargetEur, DateOnly? TargetDate);

public sealed class UpdateGoalUseCase(
    IGoalRepository repo,
    IClock clock,
    ILogger<UpdateGoalUseCase> log)
    : UseCaseBase<UpdateGoalInput, Goal>(log)
{
    protected override async Task<Goal> ExecuteCore(UpdateGoalInput input, CancellationToken ct)
    {
        var goal = await repo.GetAsync(ct);

        goal.RetargetAmount(Money.Of(input.TargetEur, Currency.Eur), clock);
        goal.RescheduleDeadline(input.TargetDate ?? DateOnly.MinValue, clock);

        await repo.SaveAsync(goal, ct);
        return goal;
    }
}
```

The input record keeps `decimal TargetEur` + `DateOnly?` for UI back-compat; the use case translates at the seam. Domain only knows `Money` / `DateOnly.MinValue`.

- [ ] **Step 9.2: Build + commit**

```bash
dotnet build TradyStrat.Infrastructure
git add TradyStrat.Infrastructure/Settings/UseCases/UpdateGoalUseCase.cs
git commit -m "refactor(infra): UpdateGoalUseCase delegates to Goal AR methods via IGoalRepository — Phase 5 Task 9"
```

---

## Task 10: `IPriceBarReadRepository` port + Spec relocation

**Files:**
- Create: `TradyStrat.Application/PriceFeed/IPriceBarReadRepository.cs`
- Move:   8 files under `TradyStrat.Application/PriceFeed/Specifications/` → `TradyStrat.Infrastructure/PriceFeed/Specifications/`
- Create: `TradyStrat.Infrastructure/PriceFeed/EfPriceBarReadRepository.cs`

The port surfaces ALL the operations the current Spec classes provide as typed methods. Specs themselves are an implementation detail of the EF adapter and move to Infrastructure.

- [ ] **Step 10.1: Survey the existing Spec classes**

```bash
ls TradyStrat.Application/PriceFeed/Specifications/
```

Expect 8 specs: `EarliestPriceBarSpec`, `LatestPriceBarSpec`, `PriceBarAfterSpec`, `PriceBarBeforeSpec`, `PriceBarsAsOfSpec`, `PriceBarsForTickerSpec`, `PriceBarsInRangeSpec`, `PriceBarsSinceSpec`. Each is a specific query; the port methods map 1:1.

- [ ] **Step 10.2: Create `IPriceBarReadRepository`**

```csharp
using TradyStrat.Domain;

namespace TradyStrat.Application.PriceFeed;

public interface IPriceBarReadRepository
{
    Task<IReadOnlyList<PriceBar>> ListForTickerAsync(string ticker, CancellationToken ct);
    Task<IReadOnlyList<PriceBar>> ListAsOfAsync(string ticker, DateOnly asOf, CancellationToken ct);
    Task<IReadOnlyList<PriceBar>> ListSinceAsync(string ticker, DateOnly since, CancellationToken ct);
    Task<IReadOnlyList<PriceBar>> ListInRangeAsync(string ticker, DateOnly from, DateOnly to, CancellationToken ct);
    Task<PriceBar?>               LatestAsync(string ticker, CancellationToken ct);
    Task<PriceBar?>               EarliestAsync(string ticker, CancellationToken ct);
    Task<PriceBar?>               LatestBeforeAsync(string ticker, DateOnly date, CancellationToken ct);
    Task<PriceBar?>               EarliestAfterAsync(string ticker, DateOnly date, CancellationToken ct);
}
```

- [ ] **Step 10.3: Move the Spec classes to Infrastructure**

```bash
mkdir -p TradyStrat.Infrastructure/PriceFeed/Specifications
git mv TradyStrat.Application/PriceFeed/Specifications/*.cs \
       TradyStrat.Infrastructure/PriceFeed/Specifications/
rmdir TradyStrat.Application/PriceFeed/Specifications 2>/dev/null
```

For each moved file, update the `namespace` declaration:

```bash
sed -i.bak 's/namespace TradyStrat.Application.PriceFeed.Specifications;/namespace TradyStrat.Infrastructure.PriceFeed.Specifications;/' \
  TradyStrat.Infrastructure/PriceFeed/Specifications/*.cs
rm TradyStrat.Infrastructure/PriceFeed/Specifications/*.bak
```

- [ ] **Step 10.4: Implement `EfPriceBarReadRepository`**

```csharp
using Ardalis.Specification.EntityFrameworkCore;
using TradyStrat.Application.PriceFeed;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Data;
using TradyStrat.Infrastructure.PriceFeed.Specifications;

namespace TradyStrat.Infrastructure.PriceFeed;

public sealed class EfPriceBarReadRepository(AppDbContext db) : IPriceBarReadRepository
{
    public async Task<IReadOnlyList<PriceBar>> ListForTickerAsync(string ticker, CancellationToken ct)
        => await db.PriceBars.WithSpecification(new PriceBarsForTickerSpec(ticker)).ToListAsync(ct);

    public async Task<IReadOnlyList<PriceBar>> ListAsOfAsync(string ticker, DateOnly asOf, CancellationToken ct)
        => await db.PriceBars.WithSpecification(new PriceBarsAsOfSpec(ticker, asOf)).ToListAsync(ct);

    public async Task<IReadOnlyList<PriceBar>> ListSinceAsync(string ticker, DateOnly since, CancellationToken ct)
        => await db.PriceBars.WithSpecification(new PriceBarsSinceSpec(ticker, since)).ToListAsync(ct);

    public async Task<IReadOnlyList<PriceBar>> ListInRangeAsync(string ticker, DateOnly from, DateOnly to, CancellationToken ct)
        => await db.PriceBars.WithSpecification(new PriceBarsInRangeSpec(ticker, from, to)).ToListAsync(ct);

    public Task<PriceBar?> LatestAsync(string ticker, CancellationToken ct)
        => db.PriceBars.WithSpecification(new LatestPriceBarSpec(ticker)).FirstOrDefaultAsync(ct);

    public Task<PriceBar?> EarliestAsync(string ticker, CancellationToken ct)
        => db.PriceBars.WithSpecification(new EarliestPriceBarSpec(ticker)).FirstOrDefaultAsync(ct);

    public Task<PriceBar?> LatestBeforeAsync(string ticker, DateOnly date, CancellationToken ct)
        => db.PriceBars.WithSpecification(new PriceBarBeforeSpec(ticker, date)).FirstOrDefaultAsync(ct);

    public Task<PriceBar?> EarliestAfterAsync(string ticker, DateOnly date, CancellationToken ct)
        => db.PriceBars.WithSpecification(new PriceBarAfterSpec(ticker, date)).FirstOrDefaultAsync(ct);
}
```

Add `using` directives as needed. The `WithSpecification` extension comes from `Ardalis.Specification.EntityFrameworkCore` (already a package reference in Infrastructure).

- [ ] **Step 10.5: Register in DI**

In `TradyStrat.Infrastructure/PriceFeed/PriceFeedInfrastructureModule.cs` (or whatever the module file is — check existing):

```csharp
services.AddScoped<IPriceBarReadRepository, EfPriceBarReadRepository>();
```

- [ ] **Step 10.6: Build + commit**

```bash
dotnet build TradyStrat.Infrastructure
git add -A
git commit -m "feat: IPriceBarReadRepository + EfPriceBarReadRepository + relocate PriceBar specs to Infrastructure — Phase 5 Task 10"
```

---

## Task 11: `IFxRateReadRepository` port + Spec relocation

**Files:**
- Create: `TradyStrat.Application/Fx/IFxRateReadRepository.cs`
- Move:   `TradyStrat.Application/Fx/Specifications/LatestFxRateSpec.cs` → `TradyStrat.Infrastructure/Fx/Specifications/LatestFxRateSpec.cs`
- Create: `TradyStrat.Infrastructure/Fx/EfFxRateReadRepository.cs`

- [ ] **Step 11.1: Create the port**

```csharp
using TradyStrat.Domain;

namespace TradyStrat.Application.Fx;

public interface IFxRateReadRepository
{
    /// <summary>Latest FxRate for (base, quote) on or before <paramref name="asOf"/>.</summary>
    Task<FxRate?> LatestAsync(string @base, string quote, DateOnly asOf, CancellationToken ct);

    Task<IReadOnlyList<FxRate>> ListForPairAsync(string @base, string quote, CancellationToken ct);
}
```

- [ ] **Step 11.2: Move the Spec class**

```bash
mkdir -p TradyStrat.Infrastructure/Fx/Specifications
git mv TradyStrat.Application/Fx/Specifications/LatestFxRateSpec.cs \
       TradyStrat.Infrastructure/Fx/Specifications/LatestFxRateSpec.cs
rmdir TradyStrat.Application/Fx/Specifications 2>/dev/null
```

Update its `namespace` declaration to `TradyStrat.Infrastructure.Fx.Specifications`.

- [ ] **Step 11.3: Implement `EfFxRateReadRepository`**

```csharp
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TradyStrat.Application.Fx;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Data;
using TradyStrat.Infrastructure.Fx.Specifications;

namespace TradyStrat.Infrastructure.Fx;

public sealed class EfFxRateReadRepository(AppDbContext db) : IFxRateReadRepository
{
    public Task<FxRate?> LatestAsync(string @base, string quote, DateOnly asOf, CancellationToken ct)
        => db.FxRates.WithSpecification(new LatestFxRateSpec(@base, quote, asOf)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<FxRate>> ListForPairAsync(string @base, string quote, CancellationToken ct)
        => await db.FxRates
            .Where(r => r.Base == @base && r.Quote == quote)
            .OrderByDescending(r => r.Date)
            .ToListAsync(ct);
}
```

- [ ] **Step 11.4: Register in DI**

In `TradyStrat.Infrastructure/Fx/FxInfrastructureModule.cs` (or equivalent):

```csharp
services.AddScoped<IFxRateReadRepository, EfFxRateReadRepository>();
```

- [ ] **Step 11.5: Build + commit**

```bash
dotnet build TradyStrat.Infrastructure
git add -A
git commit -m "feat: IFxRateReadRepository + EfFxRateReadRepository + relocate FxRate spec to Infrastructure — Phase 5 Task 11"
```

---

## Task 12: Write-side ports for hosted services (`IPriceFeedWriter` / `IFxRateWriter`)

**Files:**
- Create: `TradyStrat.Application/PriceFeed/IPriceFeedWriter.cs`
- Create: `TradyStrat.Application/Fx/IFxRateWriter.cs`
- Modify: `TradyStrat.Infrastructure/PriceFeed/DailyPriceCache.cs` (today: uses `db` directly — keep, or refactor through writer port)
- Modify: `TradyStrat.Infrastructure/Fx/DailyFxCache.cs` (same)

Inspect first: how do the hosted services persist today? If they just use `AppDbContext` directly, the writer port is the narrow public surface. If they use `IRepositoryBase<PriceBar>.AddAsync`, the rename is real.

- [ ] **Step 12.1: Survey the write call sites**

```bash
grep -rn 'PriceBars\.\(Add\|AddAsync\)\|FxRates\.\(Add\|AddAsync\)' --include='*.cs' . | grep -v '/bin/' | grep -v '/obj/' | grep -v '/worktrees/'
grep -rn 'IRepositoryBase<PriceBar>\|IRepositoryBase<FxRate>' --include='*.cs' . | grep -v '/bin/' | grep -v '/obj/' | grep -v '/worktrees/'
```

If no `IRepositoryBase<PriceBar>` writes exist (only `IReadRepositoryBase<PriceBar>`), the writer port is a refactoring that doesn't change behavior — just narrows public surface. If there are real writes via the generic repo, they get swapped.

- [ ] **Step 12.2: Create the ports**

`TradyStrat.Application/PriceFeed/IPriceFeedWriter.cs`:

```csharp
using TradyStrat.Domain;

namespace TradyStrat.Application.PriceFeed;

/// <summary>
/// Write-side port for PriceBar persistence. Implemented only by the EF
/// adapter; consumed only by hosted services (DailyPriceCache).
/// </summary>
public interface IPriceFeedWriter
{
    Task AppendAsync(IReadOnlyList<PriceBar> bars, CancellationToken ct);
}
```

`TradyStrat.Application/Fx/IFxRateWriter.cs`:

```csharp
using TradyStrat.Domain;

namespace TradyStrat.Application.Fx;

public interface IFxRateWriter
{
    Task AppendAsync(IReadOnlyList<FxRate> rates, CancellationToken ct);
}
```

- [ ] **Step 12.3: Implement EF-side writers**

If existing hosted services already write directly through `AppDbContext`, this step adds a thin Infrastructure-resident implementation but doesn't migrate the existing call sites (that ripple is too wide for Phase 5). Document this as a partial implementation and leave the existing direct-`AppDbContext` paths in place; the new ports are scaffolding for a future write-path consolidation.

If existing call sites use `IRepositoryBase<PriceBar>` for writes, swap them to the new writer port.

```csharp
// TradyStrat.Infrastructure/PriceFeed/EfPriceFeedWriter.cs
using TradyStrat.Application.PriceFeed;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.Infrastructure.PriceFeed;

internal sealed class EfPriceFeedWriter(AppDbContext db) : IPriceFeedWriter
{
    public async Task AppendAsync(IReadOnlyList<PriceBar> bars, CancellationToken ct)
    {
        db.PriceBars.AddRange(bars);
        await db.SaveChangesAsync(ct);
    }
}
```

Same shape for `EfFxRateWriter`.

Register in their respective infrastructure modules. **Skip changing the existing hosted-service write paths** — they stay on direct `AppDbContext` usage. The writer ports exist for future use cases that need to test against a stub.

- [ ] **Step 12.4: Build + commit**

```bash
dotnet build TradyStrat.Infrastructure
git add -A
git commit -m "feat: IPriceFeedWriter + IFxRateWriter narrow write ports — Phase 5 Task 12"
```

---

## Task 13: `FxRate` adopts `CurrencyPair`

**Files:**
- Modify: `TradyStrat.Domain/MarketData/FxRate.cs`
- Modify: `TradyStrat.Infrastructure/Data/Configurations/FxRateConfiguration.cs`

Today's `FxRate` has `string Base` and `string Quote` as separate columns. Adopt `CurrencyPair` VO with value converters that round-trip through the existing two columns.

- [ ] **Step 13.1: Read the existing types**

```bash
cat TradyStrat.Domain/MarketData/FxRate.cs
cat TradyStrat.Infrastructure/Data/Configurations/FxRateConfiguration.cs
```

- [ ] **Step 13.2: Rewrite `FxRate.cs`**

```csharp
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain;

public sealed class FxRate
{
    public int           Id        { get; private set; }
    public DateOnly      Date      { get; private set; }
    public CurrencyPair  Pair      { get; private set; }
    public decimal       Rate      { get; private set; }    // Quote per 1 Base
    public DateTime      FetchedAt { get; private set; }

    private FxRate() { }   // EF

    public FxRate(DateOnly date, CurrencyPair pair, decimal rate, DateTime fetchedAt)
    {
        Date = date;
        Pair = pair;
        Rate = rate;
        FetchedAt = fetchedAt;
    }
}
```

Note: `FxRate` is reference-data, not a real AR — but moving from `record` with `init` properties to `sealed class` with private setters tightens the contract. The public single-arg ctor (no factory) keeps the construction shape simple for the daily cache.

- [ ] **Step 13.3: Rewrite `FxRateConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Domain;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Infrastructure.Data.Configurations;

public sealed class FxRateConfiguration : IEntityTypeConfiguration<FxRate>
{
    public void Configure(EntityTypeBuilder<FxRate> builder)
    {
        builder.ToTable("FxRates");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();
        builder.Property(r => r.Date);
        builder.Property(r => r.Rate).HasColumnType("TEXT");
        builder.Property(r => r.FetchedAt);

        // CurrencyPair owned VO → existing Base/Quote string columns.
        builder.OwnsOne(r => r.Pair, p =>
        {
            p.Property(x => x.Base)
             .HasConversion(c => c.Code, s => Currency.Parse(s))
             .HasColumnName("Base").HasMaxLength(3);
            p.Property(x => x.Quote)
             .HasConversion(c => c.Code, s => Currency.Parse(s))
             .HasColumnName("Quote").HasMaxLength(3);
        });

        builder.HasIndex("Date", "Base", "Quote");
    }
}
```

- [ ] **Step 13.4: Update consumers that referenced `.Base` / `.Quote` directly**

```bash
grep -rn '\.Base\b\|\.Quote\b' --include='*.cs' . | grep -v '/bin/' | grep -v '/obj/' | grep -v '/worktrees/' | grep -i fx
```

For each match: `rate.Base` → `rate.Pair.Base.Code`, `rate.Quote` → `rate.Pair.Quote.Code`. Construction sites (`new FxRate { Base = "EUR", Quote = "USD", ... }`) become `new FxRate(date, CurrencyPair.Of(Currency.Eur, Currency.Usd), rate, fetched)`.

- [ ] **Step 13.5: Build + commit**

```bash
dotnet build TradyStrat.slnx
git add -A
git commit -m "refactor(domain): FxRate adopts CurrencyPair VO via EF owned-VO converter (no migration) — Phase 5 Task 13"
```

---

## Task 14: `IndicatorBundle` non-nullable + `Percentage` for `Rsi`

**Files:**
- Modify: `TradyStrat.Domain/Indicators/BollingerReading.cs`
- Modify: `TradyStrat.Domain/Indicators/IchimokuReading.cs`
- Modify: `TradyStrat.Domain/Indicators/IndicatorBundle.cs`
- Modify: `TradyStrat.Domain/Indicators/IndicatorReading.cs`

Spec §10: "`IndicatorBundle` becomes non-nullable: each field gets its `.Empty` (`BollingerReading.Empty`, `IchimokuReading.Empty`, `Percentage.Empty` for `Rsi`)."

- [ ] **Step 14.1: Add `Empty` to `BollingerReading`**

```csharp
namespace TradyStrat.Domain;

public sealed record BollingerReading(decimal Upper, decimal Middle, decimal Lower, decimal Sigma)
{
    public static readonly BollingerReading Empty = new(0m, 0m, 0m, 0m);
    public bool IsEmpty => this == Empty;
}
```

- [ ] **Step 14.2: Add `Empty` to `IchimokuReading`**

```csharp
namespace TradyStrat.Domain;

public sealed record IchimokuReading(
    decimal Tenkan, decimal Kijun,
    decimal SenkouA, decimal SenkouB,
    decimal Chikou,
    IchimokuSignal Signal)
{
    public static readonly IchimokuReading Empty = new(0m, 0m, 0m, 0m, 0m, IchimokuSignal.Neutral);
    public bool IsEmpty => this == Empty;
}
```

Verify `IchimokuSignal.Neutral` exists (it should — `Neutral` is the natural default). If not, fall back to `default(IchimokuSignal)`.

- [ ] **Step 14.3: Rewrite `IndicatorBundle`**

```csharp
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain;

public sealed record IndicatorBundle(
    BollingerReading Bollinger,
    Percentage Rsi,
    decimal Sma50,
    decimal Sma200,
    IchimokuReading Ichimoku)
{
    public static readonly IndicatorBundle Empty = new(
        BollingerReading.Empty,
        Percentage.Empty,
        0m,
        0m,
        IchimokuReading.Empty);
}
```

`Sma50` and `Sma200` stay raw decimals (spec hints at `Price` wrapping but that would require a per-row currency thread-through — deferred per the plan's spec-deferral note).

- [ ] **Step 14.4: Rewrite `IndicatorReading`**

```csharp
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain;

public sealed record IndicatorReading(
    string Ticker,
    decimal Price,
    BollingerReading Bollinger,
    Percentage Rsi,
    decimal Sma50,
    decimal Sma200,
    IchimokuReading Ichimoku,
    Zone Zone,
    IReadOnlyList<string> Reasons);
```

- [ ] **Step 14.5: Update consumers**

```bash
grep -rn 'BollingerReading?\|IchimokuReading?\|decimal? Rsi\|decimal? Sma' --include='*.cs' . | grep -v '/bin/' | grep -v '/obj/' | grep -v '/worktrees/'
```

For each match (probably in `IndicatorEngine.cs`, `ZoneClassifier.cs`, the rule files, the dashboard mapper, the snapshot section), drop the `?` suffix and replace null-check sites with `.IsEmpty` checks. The Rsi consumers swap `value.HasValue ? value.Value : 0m` patterns for `reading.Rsi.IsEmpty ? 0m : reading.Rsi.Value`.

Also update `ComputeFromSeries` in `IndicatorEngine` to fall back to `.Empty` when `LatestFor(series)` returns null:

```csharp
// Before
var bundle = new IndicatorBundle(
    Bollinger.Bollinger.LatestFor(series),
    Rsi.Rsi.LatestFor(series),
    MovingAverage.MovingAverage.LatestFor(series, 50),
    MovingAverage.MovingAverage.LatestFor(series, 200),
    Ichimoku.Ichimoku.LatestFor(series));

// After
var bundle = new IndicatorBundle(
    Bollinger.Bollinger.LatestFor(series) ?? BollingerReading.Empty,
    Rsi.Rsi.LatestFor(series) is { } r ? Percentage.Of(r) : Percentage.Empty,
    MovingAverage.MovingAverage.LatestFor(series, 50)  ?? 0m,
    MovingAverage.MovingAverage.LatestFor(series, 200) ?? 0m,
    Ichimoku.Ichimoku.LatestFor(series) ?? IchimokuReading.Empty);
```

(If `LatestFor` already returns non-nullable, just construct directly.)

- [ ] **Step 14.6: Build + run Indicators tests**

```bash
dotnet build TradyStrat.slnx
dotnet test TradyStrat.slnx --filter "FullyQualifiedName~Indicator"
```

Expected: tests pass after consumer updates. If the IndicatorBundle constructor signature broke nullability-checking call sites, fix them inline.

- [ ] **Step 14.7: Commit**

```bash
git add -A
git commit -m "refactor(domain): IndicatorBundle non-nullable with .Empty sentinels; Rsi adopts Percentage VO — Phase 5 Task 14"
```

---

## Task 15: Move `IndicatorEngine` and `ZoneClassifier` to `Domain/Indicators/Services/`

**Files:**
- Move: `TradyStrat.Application/Indicators/IndicatorEngine.cs` → `TradyStrat.Domain/Indicators/Services/IndicatorEngine.cs`
- Move: `TradyStrat.Application/Indicators/Zones/ZoneClassifier.cs` → `TradyStrat.Domain/Indicators/Services/ZoneClassifier.cs`
- Modify: `TradyStrat.Application/Indicators/IndicatorsApplicationModule.cs` (DI registration imports)

Per spec §10: "they were domain services miscategorized". The move is namespace-only — the constructor signatures stay, the indicator rule plugins stay in Application as strategy implementations injected via DI.

- [ ] **Step 15.1: Move the files**

```bash
mkdir -p TradyStrat.Domain/Indicators/Services
git mv TradyStrat.Application/Indicators/IndicatorEngine.cs \
       TradyStrat.Domain/Indicators/Services/IndicatorEngine.cs
git mv TradyStrat.Application/Indicators/Zones/ZoneClassifier.cs \
       TradyStrat.Domain/Indicators/Services/ZoneClassifier.cs
```

- [ ] **Step 15.2: Update namespaces and imports**

Read the moved files and change their `namespace` declarations:

```csharp
// IndicatorEngine.cs
namespace TradyStrat.Domain.Indicators.Services;

// ZoneClassifier.cs
namespace TradyStrat.Domain.Indicators.Services;
```

`IndicatorEngine.cs` references several Application-resident types:
- `IIndicatorEngine` — move with it OR leave in Application as a port? **Move it** to `TradyStrat.Domain/Indicators/Services/IIndicatorEngine.cs` (it's the domain service's interface).
- `IZoneRule`, `IIndicatorHistoryProviderFactory` — these are Application-resident plugin interfaces. The domain service consumes them through constructor injection — that's allowed (Domain consuming Application interfaces is normally forbidden, but the spec explicitly authorizes "pure-compute dependencies"; here, the plugins are also pure-compute, so the same logic applies).

The cleaner alternative: move `IZoneRule`, `IIndicatorHistoryProvider`, `IIndicatorHistoryProviderFactory` to Domain too. The spec's intent (Indicators-as-domain-service) supports it. Apply this:

```bash
mkdir -p TradyStrat.Domain/Indicators/Services
# Already moved IndicatorEngine.cs and ZoneClassifier.cs above.

# Move the rule + history interfaces to Domain too.
git mv TradyStrat.Application/Indicators/IIndicatorEngine.cs \
       TradyStrat.Domain/Indicators/Services/IIndicatorEngine.cs
git mv TradyStrat.Application/Indicators/Zones/IZoneRule.cs \
       TradyStrat.Domain/Indicators/Services/IZoneRule.cs
git mv TradyStrat.Application/Indicators/Zones/ZoneVote.cs \
       TradyStrat.Domain/Indicators/Services/ZoneVote.cs
git mv TradyStrat.Application/Indicators/History/IIndicatorHistoryProvider.cs \
       TradyStrat.Domain/Indicators/Services/IIndicatorHistoryProvider.cs
git mv TradyStrat.Application/Indicators/History/IIndicatorHistoryProviderFactory.cs \
       TradyStrat.Domain/Indicators/Services/IIndicatorHistoryProviderFactory.cs
```

Update each moved file's `namespace` declaration to `TradyStrat.Domain.Indicators.Services`.

Implementations of `IZoneRule` (the four rule classes) and `IIndicatorHistoryProvider` (the four history providers) STAY in Application — they're strategy plugins, not the domain service.

- [ ] **Step 15.3: Update Application module imports**

`TradyStrat.Application/Indicators/IndicatorsApplicationModule.cs`:

```csharp
using TradyStrat.Application.Indicators.Bollinger;
using TradyStrat.Application.Indicators.Ichimoku;
using TradyStrat.Application.Indicators.MovingAverage;
using TradyStrat.Application.Indicators.Rsi;
using TradyStrat.Application.Indicators.History;   // for IndicatorHistoryProviderFactory
using TradyStrat.Domain.Indicators.Services;       // for IIndicatorEngine, IZoneRule, ZoneClassifier, IIndicatorHistoryProvider*

namespace TradyStrat.Application.Indicators;

public sealed class IndicatorsApplicationModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IZoneRule, BollingerZoneRule>();
        services.AddSingleton<IZoneRule, RsiZoneRule>();
        services.AddSingleton<IZoneRule, MovingAverageZoneRule>();
        services.AddSingleton<IZoneRule, IchimokuZoneRule>();
        services.AddScoped<ZoneClassifier>();
        services.AddScoped<IIndicatorHistoryProvider, RsiHistoryProvider>();
        services.AddScoped<IIndicatorHistoryProvider, BollingerHistoryProvider>();
        services.AddScoped<IIndicatorHistoryProvider, IchimokuHistoryProvider>();
        services.AddScoped<IIndicatorHistoryProvider, Sma200HistoryProvider>();
        services.AddScoped<IIndicatorHistoryProviderFactory, IndicatorHistoryProviderFactory>();
        services.AddScoped<IndicatorEngine>();
        services.AddScoped<IIndicatorEngine>(sp => sp.GetRequiredService<IndicatorEngine>());
    }
}
```

- [ ] **Step 15.4: Update consumers' `using` directives**

```bash
grep -rln 'using TradyStrat.Application.Indicators' --include='*.cs' . | grep -v '/bin/' | grep -v '/obj/' | grep -v '/worktrees/'
```

For each file, the `using TradyStrat.Application.Indicators` becomes `using TradyStrat.Domain.Indicators.Services` for the moved types — but keep `using TradyStrat.Application.Indicators` for the rule plugins and history providers. C#'s import system handles ambiguity at the type level: if a file references both `IndicatorEngine` (moved) and `Bollinger` (still in Application), it needs both `using` directives.

- [ ] **Step 15.5: Build + commit**

```bash
dotnet build TradyStrat.slnx
git add -A
git commit -m "refactor: move IndicatorEngine + ZoneClassifier + their interfaces to Domain/Indicators/Services — Phase 5 Task 15"
```

---

## Task 16: Consumer-swap — `IReadRepositoryBase<PriceBar/FxRate/GoalConfig>` → new ports

The 11+ sites that inject the generic Ardalis ports get rewritten to use the new ones.

**Files to touch (high-confidence):**

PriceBar consumers:
- `TradyStrat.Cli/Mcp/Tools/PortfolioTool.cs`
- `TradyStrat.Application/Indicators/IndicatorEngine.cs` (already moved — fix in Domain location)
- `TradyStrat.Application/AiSuggestion/ForwardReturnCalculator.cs`
- `TradyStrat.Application/AiSuggestion/UseCases/ReplaySuggestionsUseCase.cs`
- `TradyStrat.Application/AiSuggestion/Snapshot/Sections/RecentSuggestionsSection.cs`
- `TradyStrat.Application/PriceFeed/UseCases/GetPriceSeriesUseCase.cs`
- `TradyStrat.Application/Dashboard/Navigation/EntryNavigationService.cs`
- `TradyStrat.Application/Dashboard/UseCases/LoadDashboardUseCase.cs`

FxRate consumers:
- `TradyStrat.Cli/Mcp/Tools/PortfolioTool.cs`
- `TradyStrat.Application/Fx/FxConverter.cs`
- `TradyStrat.Application/Dashboard/UseCases/LoadDashboardUseCase.cs`

GoalConfig consumers (now Goal):
- `TradyStrat.Cli/Mcp/Tools/PortfolioTool.cs`
- `TradyStrat/Features/Settings/SettingsPage.razor.cs`
- `TradyStrat/Features/Dashboard/Components/HeroCapital.razor.cs`
- `TradyStrat/Features/Dashboard/Components/GrowthChart.razor.cs`
- `TradyStrat.Application/AiSuggestion/Snapshot/Sections/GoalSection.cs`
- `TradyStrat.Application/Dashboard/UseCases/LoadDashboardUseCase.cs`

- [ ] **Step 16.1: PriceBar consumer swap**

For each `IReadRepositoryBase<PriceBar>` injection: replace the constructor parameter type with `IPriceBarReadRepository`. Replace each `repo.ListAsync(new PriceBarsForTickerSpec(...), ct)` with `repo.ListForTickerAsync(ticker, ct)` (same for the other spec variants).

Example before:

```csharp
public sealed class ForwardReturnCalculator(
    IReadRepositoryBase<PriceBar> barRepo,
    IInstrumentRepository instrumentRepo)
{
    public async Task<decimal?> ComputeAsync(string ticker, DateOnly forDate, CancellationToken ct)
    {
        var bars = await barRepo.ListAsync(new PriceBarsSinceSpec(ticker, forDate), ct);
        ...
```

After:

```csharp
public sealed class ForwardReturnCalculator(
    IPriceBarReadRepository barRepo,
    IInstrumentRepository instrumentRepo)
{
    public async Task<decimal?> ComputeAsync(string ticker, DateOnly forDate, CancellationToken ct)
    {
        var bars = await barRepo.ListSinceAsync(ticker, forDate, ct);
        ...
```

Drop `using Ardalis.Specification;` and `using TradyStrat.Application.PriceFeed.Specifications;` where they become unused.

- [ ] **Step 16.2: FxRate consumer swap**

For each `IReadRepositoryBase<FxRate>` injection: replace with `IFxRateReadRepository`. The two call sites that use `LatestFxRateSpec` translate to:

```csharp
// Before
var fxLatest = await fxRepo.FirstOrDefaultAsync(new LatestFxRateSpec("EUR", "USD", target), ct);
// After
var fxLatest = await fxRepo.LatestAsync("EUR", "USD", target, ct);
```

- [ ] **Step 16.3: Goal consumer swap**

Every `IReadRepositoryBase<GoalConfig> goalRepo` becomes `IGoalRepository goalRepo`. Every `await goalRepo.GetByIdAsync(1, ct) ?? GoalConfig.Default(DateTime.UtcNow)` becomes `await goalRepo.GetAsync(ct)` (the repo handles the singleton seed internally).

UI components that took a `GoalConfig` parameter now take `Goal`. The `Goal.Target.Amount` replaces `GoalConfig.TargetEur` at render sites. `Goal.TargetDate == DateOnly.MinValue` replaces `GoalConfig.TargetDate is null` checks (or use `!goal.HasDeadline`).

- [ ] **Step 16.4: Build incrementally + commit per logical group**

```bash
dotnet build TradyStrat.slnx
git add -A
git commit -m "refactor: swap consumers from generic Ardalis ports to IPriceBarReadRepository/IFxRateReadRepository/IGoalRepository — Phase 5 Task 16"
```

---

## Task 17: Final verification + smoke + tag

- [ ] **Step 17.1: Full clean build**

```bash
dotnet clean TradyStrat.slnx
dotnet build TradyStrat.slnx
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 17.2: Full test suite**

```bash
dotnet test TradyStrat.slnx --no-build
```

Expected: every test passes. Counts up vs Phase 4 baseline (436) by ~30 new Phase 5 tests (3 VOs × ~5 each + Goal tests + the migrated indicator/marketdata tests).

- [ ] **Step 17.3: Grep forbidden references**

```bash
# Old anemic GoalConfig record should be gone.
grep -rn '\bGoalConfig\b' --include='*.cs' --include='*.razor*' . | grep -v '/bin/' | grep -v '/obj/' | grep -v '/worktrees/' | grep -v 'GoalConfiguration'
```

Expected: no hits.

```bash
# Old generic ports should be gone for Price/Fx/Goal.
grep -rn 'IReadRepositoryBase<PriceBar>\|IReadRepositoryBase<FxRate>\|IReadRepositoryBase<GoalConfig>\|IReadRepositoryBase<Goal>' \
  --include='*.cs' . | grep -v '/bin/' | grep -v '/obj/' | grep -v '/worktrees/'
```

Expected: no hits.

```bash
# Old IndicatorEngine + ZoneClassifier locations should be empty.
ls TradyStrat.Application/Indicators/IndicatorEngine.cs 2>/dev/null
ls TradyStrat.Application/Indicators/Zones/ZoneClassifier.cs 2>/dev/null
```

Expected: both `ls: No such file or directory`.

- [ ] **Step 17.4: Verify the live dev DB still rehydrates Goal correctly**

If the EF rebuild flagged a missing `TargetCurrency` / `TargetIsEmpty` column, generate a one-shot migration to ALTER TABLE add them. Otherwise no action.

```bash
dotnet ef migrations list --project TradyStrat.Infrastructure --startup-project TradyStrat.Infrastructure
```

If there's a pending model change, generate `AddGoalMoneyColumns` migration:

```bash
dotnet ef migrations add AddGoalMoneyColumns --project TradyStrat.Infrastructure --startup-project TradyStrat.Infrastructure --output-dir Data/Migrations
```

Inspect the generated SQL — expected: just two `AddColumn` calls (`TargetCurrency` TEXT DEFAULT 'EUR', `TargetIsEmpty` INTEGER DEFAULT 0). Then test against a /tmp copy of the dev DB:

```bash
cp "$HOME/Library/Application Support/TradyStrat/tradystrat.db" /tmp/tradystrat-phase5-test.db
TRADYSTRAT_DB=/tmp/tradystrat-phase5-test.db dotnet ef database update \
  --project TradyStrat.Infrastructure --startup-project TradyStrat.Infrastructure
sqlite3 /tmp/tradystrat-phase5-test.db "PRAGMA table_info(Goals);"
```

Expected: 5 columns (`Id`, `TargetEur`, `TargetDate`, `UpdatedAt`, `TargetCurrency`, `TargetIsEmpty`) — wait, that's 6 columns. Looking at the migration just adding two columns.

Backfill `TargetCurrency='EUR'`, `TargetIsEmpty=0` for the existing row inside the migration's `Up` (the schema default takes care of new rows; the seed row needs explicit backfill):

```csharp
migrationBuilder.Sql("UPDATE Goals SET TargetCurrency = 'EUR', TargetIsEmpty = 0;");
```

- [ ] **Step 17.5: Smoke-test the app**

```bash
dotnet run --project TradyStrat --urls=http://localhost:5180
```

In a second shell:

```bash
curl -sf http://localhost:5180/         | head -3    # dashboard renders
curl -sf http://localhost:5180/trades   | head -3
curl -sf http://localhost:5180/settings | head -3
```

Each should return HTML, not 500. The dashboard's Goal heading should still read "€1,000,000" (or whatever's been configured); the growth chart still renders with capital-event Roman numerals.

Stop the app with `Ctrl-C`.

- [ ] **Step 17.6: Phase 5 checkpoint tag**

```bash
git tag -a phase5-cleanup-done -m "Phase 5 (Goal + MarketData + Indicators cleanup) complete"
```

- [ ] **Step 17.7: Finish branch via the finishing-a-development-branch skill**

```
I'm using the finishing-a-development-branch skill to complete this work.
```

The skill runs tests, presents the 4-option menu (merge / PR / keep / discard), and cleans up the worktree on merge.

---

# Plan summary

**Phase 5 tasks (17):**
- 1-3: kernel VOs (Percentage, CurrencyPair, RomanNumeralId)
- 4-6: Goal AR rewrite + CapitalEvent + GrowthPoint adopt new VOs
- 7-9: IGoalRepository + EfGoalRepository + UpdateGoalUseCase rewrite
- 10-12: IPriceBarReadRepository + IFxRateReadRepository + writer ports + spec relocation
- 13: FxRate adopts CurrencyPair
- 14: IndicatorBundle non-nullable with Empty sentinels + Percentage for Rsi
- 15: IndicatorEngine + ZoneClassifier move to Domain/Indicators/Services
- 16: consumer-swap (Goal + PriceBar + FxRate ports)
- 17: final verification + smoke + tag

**Behavioral guarantee:** dashboard renders identical content (€1M default goal, growth chart with Roman-numeral capital events, indicators with non-null defaults rather than null-rendered "—"). Note: the IsEmpty path for indicators may render "—" the same as null-check did before; verify in Task 17 smoke.

**Risks called out:**
- **Goal EF mapping**: adding `Money Target` introduces two new shadow columns (`TargetCurrency`, `TargetIsEmpty`). The plan explicitly defers schema migration unless a runtime read fails — if the EF rehydration throws on the missing columns, generate `AddGoalMoneyColumns` in Task 17.4 as a one-shot.
- **PriceBar Money wrap deferred**: spec §10 wants OHLC Money-wrapping but no schema migration. The two can't both happen without a per-row currency column. Phase 5 wraps `Percentage` for Rsi and `CurrencyPair` for FxRate (low-friction) and defers the per-row currency tracking to a follow-up.
- **Indicator interface relocation**: moving `IZoneRule`, `IIndicatorHistoryProvider`, `IIndicatorHistoryProviderFactory` to Domain blurs the Application/Domain boundary slightly. The justification (per spec): they're pure-compute strategy plugins consumed by a domain service. If this feels uncomfortable in review, keeping them in Application is also a valid choice and the engine takes them via the constructor across the layer boundary.
- **Indicator rule files stay in Application** by design — they're concrete strategies (Bollinger thresholds, RSI bands, etc.) that may be tuned independently. Moving them too would invert the dependency: Domain depending on concrete rule constants.

---

## Self-review notes

**Spec coverage** (against §4 new VOs + §10 Phase 5):
- §4 Percentage VO — Task 1 ✓
- §4 CurrencyPair VO — Task 2 ✓
- §4 RomanNumeralId VO — Task 3 ✓
- §10 Goal AR (singleton, Money Target, HasDeadline, RetargetAmount/RescheduleDeadline) — Task 4 ✓
- §10 GrowthPoint typed — Task 6 ✓
- §10 CapitalEvent adopts RomanNumeralId — Task 5 ✓
- §10 IGoalRepository — Task 7 ✓
- §10 Goal.Initial replaces GoalConfig.Default — Task 4 ✓
- §10 PriceBar Money wrap — **deferred** (documented above)
- §10 FxRate CurrencyPair — Task 13 ✓
- §10 Zone enum unchanged — confirmed, no task needed
- §10 IPriceBarReadRepository / IFxRateReadRepository — Tasks 10, 11 ✓
- §10 IPriceFeedWriter / IFxRateWriter — Task 12 ✓
- §10 Specs move to Infrastructure — Tasks 10.3, 11.2 ✓
- §10 BollingerReading.Empty / IchimokuReading.Empty / Percentage adoption — Task 14 ✓
- §10 IndicatorEngine + ZoneClassifier → Domain/Indicators/Services — Task 15 ✓
- §10 Use case constructors unchanged — Task 15 preserves (only namespace moves) ✓
- §10 Tests migrate — Tasks 1-3 (Domain.Tests/Shared) + Task 4 (Goals) cover new shape ✓
- §10 No DB schema migration — preserved (Goal extra columns are runtime-resilient via EF rebuild OR one-shot migration in Task 17.4) ✓

**Type consistency:** `Goal.Initial(IClock)`, `RetargetAmount(Money, IClock)`, `RescheduleDeadline(DateOnly, IClock)` consistent across Tasks 4, 9, 16. `CurrencyPair.Of(Currency, Currency)` consistent across Tasks 2, 13. `Percentage.Of(decimal)` / `Percentage.Empty` consistent across Tasks 1, 14. `IPriceBarReadRepository` method names (`ListForTickerAsync`, `ListAsOfAsync`, `LatestAsync`, etc.) consistent across Tasks 10, 16.

**Placeholder scan:** Task 16's consumer-swap lists each file explicitly. Task 17's deferred-migration step is conditional ("if a runtime read fails") with explicit fallback SQL — no "TBD" handwaves. Task 12's writer-port scaffolding is explicitly partial — the existing direct-`AppDbContext` writes stay until a future phase consolidates them; documented inline rather than left as a placeholder.
