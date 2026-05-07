# TradyStrat — Dashboard time-travel — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the dashboard into a navigable journal of past trading days. Add `?on=YYYY-MM-DD` query-param navigation, prev/next masthead controls, a date picker, a "return to today" link, and `←`/`→` keyboard shortcuts. Strict read-only when historical; trades/settings unaffected; no schema changes.

**Architecture:** A new `IEntryNavigationService` (Facade) fronts the existing `PriceBars` table for trading-day queries via three new Ardalis specs and two reused ones. A pure `OnParamValidator` returns a sum-typed `ValidationResult` (`Live | Historical(date) | RedirectTo(url)`) the page consumes from `OnParametersSetAsync`. `LoadDashboardUseCase` switches input from `Unit` to `LoadDashboardInput(TargetDate, IsHistorical)`, swaps in the existing as-of overloads on `IndicatorEngine`/`PortfolioService`, and branches the today's-call lookup so historical mode never invokes the AI. The masthead grows prev/next/date-pill/"return to today" controls that self-hide when no nav data is passed (so `/trades` and `/settings` are unaffected). A small ES module wires `←`/`→` to `[JSInvokable]` methods on the page.

**Tech Stack:** .NET 10 · Blazor Server · EF Core 10 + SQLite · Ardalis.Specification · xunit.v3 + Shouldly + EF InMemory · scoped CSS · ES module JSInterop.

**Spec:** [`docs/superpowers/specs/2026-05-07-dashboard-time-travel-design.md`](../specs/2026-05-07-dashboard-time-travel-design.md)

---

## File map

### New files

```
TradyStrat/
├─ Common/Exceptions/
│  └─ NoTradingDaysException.cs
├─ Features/PriceFeed/Specifications/
│  ├─ EarliestPriceBarSpec.cs
│  ├─ PriceBarBeforeSpec.cs
│  └─ PriceBarAfterSpec.cs
├─ Features/Dashboard/Navigation/
│  ├─ IEntryNavigationService.cs
│  ├─ EntryNavigationService.cs
│  ├─ ValidationResult.cs
│  └─ OnParamValidator.cs
├─ Features/Dashboard/UseCases/
│  └─ LoadDashboardInput.cs
└─ wwwroot/js/
   └─ dashboard-keys.js

TradyStrat.Tests/
├─ Dashboard/Navigation/
│  ├─ EntryNavigationServiceTests.cs
│  └─ OnParamValidatorTests.cs
└─ (extends) Specifications/SpecsRoundtripTests.cs
```

### Modified files

```
TradyStrat/
├─ Modules/DashboardModule.cs                                 # register IEntryNavigationService
├─ Features/Dashboard/DashboardViewModel.cs                   # +5 nav fields, IsHistorical, nullable TodaysCall
├─ Features/Dashboard/DashboardPage.razor                     # +OnParam, +Historical guards
├─ Features/Dashboard/DashboardPage.razor.cs                  # OnParametersSetAsync, JSInvokable, IAsyncDisposable
├─ Features/Dashboard/UseCases/LoadDashboardUseCase.cs        # generic change, asOf overloads, branch
├─ Features/Dashboard/Components/VaultMasthead.razor          # nav controls + return-today
├─ Features/Dashboard/Components/VaultMasthead.razor.cs       # 6 new params
├─ Features/Dashboard/Components/RefreshFab.razor             # @if (!Historical)
├─ Features/Dashboard/Components/RefreshFab.razor.cs          # +Historical param
├─ Features/Dashboard/Components/TodaysCallCard.razor         # @if (!Historical) on actions, empty state when Sug null, "Call for {date}"
├─ Features/Dashboard/Components/TodaysCallCard.razor.cs      # +Historical param, Sug → Suggestion?
└─ wwwroot/css/vault.css                                      # nav-step / date-pill / return-today styles
```

---

## Conventions

- **Working directory:** `/Users/philippe/repo/gh-phmatray/TradyStrat`. All paths in this plan are relative to this directory.
- **Test discovery:** xunit.v3 auto-discovers `[Fact]` and `[Theory]`. Run targeted tests with `dotnet test --filter "FullyQualifiedName~ClassOrMethod"`.
- **Build during dev:** `dotnet build TradyStrat.slnx` from the repo root.
- **Commit style:** matches recent log — `<type>(<scope>): <subject>`. `feat`, `refactor`, `test`, `chore`, `docs`. No trailing period.
- **One commit per task.** Tasks are scoped so each leaves the build green.

---

## Task 1: Add `NoTradingDaysException`

**Files:**
- Create: `TradyStrat/Common/Exceptions/NoTradingDaysException.cs`

- [ ] **Step 1: Create the exception**

```csharp
// TradyStrat/Common/Exceptions/NoTradingDaysException.cs
namespace TradyStrat.Common.Exceptions;

public sealed class NoTradingDaysException(string message = "No trading days available for the focus ticker.")
    : TradyStratException(message);
```

- [ ] **Step 2: Build to confirm compiles**

Run: `dotnet build TradyStrat.slnx`
Expected: build succeeds. (No tests to run yet — the type is unused.)

- [ ] **Step 3: Commit**

```bash
git add TradyStrat/Common/Exceptions/NoTradingDaysException.cs
git commit -m "feat(exceptions): add NoTradingDaysException"
```

---

## Task 2: Add three new `PriceBar` specifications

The `EntryNavigationService` will use these. We TDD them via `SpecsRoundtripTests`, the existing pattern.

**Files:**
- Create: `TradyStrat/Features/PriceFeed/Specifications/EarliestPriceBarSpec.cs`
- Create: `TradyStrat/Features/PriceFeed/Specifications/PriceBarBeforeSpec.cs`
- Create: `TradyStrat/Features/PriceFeed/Specifications/PriceBarAfterSpec.cs`
- Modify: `TradyStrat.Tests/Specifications/SpecsRoundtripTests.cs`

- [ ] **Step 1: Add three failing roundtrip tests**

Append the following at the end of the `SpecsRoundtripTests` class (above the closing `}`):

```csharp
private static PriceBar Bar(string ticker, int day) => new()
{
    Id = 0, Ticker = ticker, Date = new DateOnly(2026, 1, day),
    Open = 1, High = 1, Low = 1, Close = 1, Volume = 1,
};

[Fact]
public async Task EarliestPriceBarSpec_returns_min_date_for_ticker()
{
    var ct = TestContext.Current.CancellationToken;
    await using var db = InMemoryDb.Create();
    db.PriceBars.AddRange(
        Bar("CON3.L", 5),
        Bar("CON3.L", 2),
        Bar("CON3.L", 9),
        Bar("COIN",   1)); // different ticker — must be ignored
    await db.SaveChangesAsync(ct);

    var bar = await db.PriceBars
        .WithSpecification(new EarliestPriceBarSpec("CON3.L"))
        .FirstOrDefaultAsync(ct);

    bar.ShouldNotBeNull();
    bar.Date.ShouldBe(new DateOnly(2026, 1, 2));
}

[Fact]
public async Task PriceBarBeforeSpec_returns_latest_bar_strictly_before_date()
{
    var ct = TestContext.Current.CancellationToken;
    await using var db = InMemoryDb.Create();
    db.PriceBars.AddRange(
        Bar("CON3.L", 1), Bar("CON3.L", 5), Bar("CON3.L", 9));
    await db.SaveChangesAsync(ct);

    var bar = await db.PriceBars
        .WithSpecification(new PriceBarBeforeSpec("CON3.L", new DateOnly(2026, 1, 9)))
        .FirstOrDefaultAsync(ct);

    bar.ShouldNotBeNull();
    bar.Date.ShouldBe(new DateOnly(2026, 1, 5));
}

[Fact]
public async Task PriceBarBeforeSpec_returns_null_at_floor()
{
    var ct = TestContext.Current.CancellationToken;
    await using var db = InMemoryDb.Create();
    db.PriceBars.Add(Bar("CON3.L", 5));
    await db.SaveChangesAsync(ct);

    var bar = await db.PriceBars
        .WithSpecification(new PriceBarBeforeSpec("CON3.L", new DateOnly(2026, 1, 5)))
        .FirstOrDefaultAsync(ct);

    bar.ShouldBeNull();
}

[Fact]
public async Task PriceBarAfterSpec_returns_earliest_bar_strictly_after_date()
{
    var ct = TestContext.Current.CancellationToken;
    await using var db = InMemoryDb.Create();
    db.PriceBars.AddRange(
        Bar("CON3.L", 1), Bar("CON3.L", 5), Bar("CON3.L", 9));
    await db.SaveChangesAsync(ct);

    var bar = await db.PriceBars
        .WithSpecification(new PriceBarAfterSpec("CON3.L", new DateOnly(2026, 1, 1)))
        .FirstOrDefaultAsync(ct);

    bar.ShouldNotBeNull();
    bar.Date.ShouldBe(new DateOnly(2026, 1, 5));
}

[Fact]
public async Task PriceBarAfterSpec_returns_null_at_ceiling()
{
    var ct = TestContext.Current.CancellationToken;
    await using var db = InMemoryDb.Create();
    db.PriceBars.Add(Bar("CON3.L", 9));
    await db.SaveChangesAsync(ct);

    var bar = await db.PriceBars
        .WithSpecification(new PriceBarAfterSpec("CON3.L", new DateOnly(2026, 1, 9)))
        .FirstOrDefaultAsync(ct);

    bar.ShouldBeNull();
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~SpecsRoundtripTests"`
Expected: 5 NEW tests fail to compile (`EarliestPriceBarSpec`, `PriceBarBeforeSpec`, `PriceBarAfterSpec` don't exist).

- [ ] **Step 3: Create the three specs**

```csharp
// TradyStrat/Features/PriceFeed/Specifications/EarliestPriceBarSpec.cs
using Ardalis.Specification;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.PriceFeed.Specifications;

public sealed class EarliestPriceBarSpec : Specification<PriceBar>
{
    public EarliestPriceBarSpec(string ticker)
    {
        Query.Where(b => b.Ticker == ticker)
             .OrderBy(b => b.Date)
             .Take(1);
    }
}
```

```csharp
// TradyStrat/Features/PriceFeed/Specifications/PriceBarBeforeSpec.cs
using Ardalis.Specification;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.PriceFeed.Specifications;

public sealed class PriceBarBeforeSpec : Specification<PriceBar>
{
    public PriceBarBeforeSpec(string ticker, DateOnly exclusive)
    {
        Query.Where(b => b.Ticker == ticker && b.Date < exclusive)
             .OrderByDescending(b => b.Date)
             .Take(1);
    }
}
```

```csharp
// TradyStrat/Features/PriceFeed/Specifications/PriceBarAfterSpec.cs
using Ardalis.Specification;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.PriceFeed.Specifications;

public sealed class PriceBarAfterSpec : Specification<PriceBar>
{
    public PriceBarAfterSpec(string ticker, DateOnly exclusive)
    {
        Query.Where(b => b.Ticker == ticker && b.Date > exclusive)
             .OrderBy(b => b.Date)
             .Take(1);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~SpecsRoundtripTests"`
Expected: all 5 new tests pass; existing tests still green.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/PriceFeed/Specifications/EarliestPriceBarSpec.cs \
        TradyStrat/Features/PriceFeed/Specifications/PriceBarBeforeSpec.cs \
        TradyStrat/Features/PriceFeed/Specifications/PriceBarAfterSpec.cs \
        TradyStrat.Tests/Specifications/SpecsRoundtripTests.cs
git commit -m "feat(specs): add Earliest/Before/After PriceBar specs for trading-day navigation"
```

---

## Task 3: `IEntryNavigationService` interface + implementation (TDD)

**Files:**
- Create: `TradyStrat/Features/Dashboard/Navigation/IEntryNavigationService.cs`
- Create: `TradyStrat/Features/Dashboard/Navigation/EntryNavigationService.cs`
- Create: `TradyStrat.Tests/Dashboard/Navigation/EntryNavigationServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// TradyStrat.Tests/Dashboard/Navigation/EntryNavigationServiceTests.cs
using Shouldly;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Features.Dashboard.Navigation;
using TradyStrat.Tests.Fx;            // shared TestRepo<T>
using TradyStrat.Tests.Specifications; // InMemoryDb
using Xunit;

namespace TradyStrat.Tests.Dashboard.Navigation;

public class EntryNavigationServiceTests
{
    // Trading-day calendar:
    //   Mon 13, Tue 14, Wed 15, Thu 16, Fri 17, (Sat 18, Sun 19 — closed), Mon 20.
    private static readonly DateOnly Mon13 = new(2026, 4, 13);
    private static readonly DateOnly Wed15 = new(2026, 4, 15);
    private static readonly DateOnly Fri17 = new(2026, 4, 17);
    private static readonly DateOnly Sun19 = new(2026, 4, 19);
    private static readonly DateOnly Mon20 = new(2026, 4, 20);

    private static PriceBar Bar(DateOnly d) => new()
    {
        Id = 0, Ticker = "CON3.L", Date = d,
        Open = 1, High = 1, Low = 1, Close = 1, Volume = 1,
    };

    private static async Task<EntryNavigationService> SeedAsync(
        TradyStrat.Data.AppDbContext db, params DateOnly[] dates)
    {
        foreach (var d in dates) db.PriceBars.Add(Bar(d));
        await db.SaveChangesAsync();
        return new EntryNavigationService(new TestRepo<PriceBar>(db));
    }

    [Fact]
    public async Task EarliestAsync_returns_min_date()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db, Mon13, Wed15, Fri17, Mon20);

        var result = await sut.EarliestAsync(ct);
        result.ShouldBe(Mon13);
    }

    [Fact]
    public async Task LatestAsync_returns_max_date()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db, Mon13, Wed15, Fri17, Mon20);

        var result = await sut.LatestAsync(ct);
        result.ShouldBe(Mon20);
    }

    [Fact]
    public async Task PreviousAsync_skips_weekend_gap()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db, Mon13, Wed15, Fri17, Mon20);

        var result = await sut.PreviousAsync(Mon20, ct);
        result.ShouldBe(Fri17);
    }

    [Fact]
    public async Task PreviousAsync_returns_null_at_floor()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db, Mon13, Wed15, Fri17, Mon20);

        var result = await sut.PreviousAsync(Mon13, ct);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task NextAsync_skips_weekend_gap()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db, Mon13, Wed15, Fri17, Mon20);

        var result = await sut.NextAsync(Fri17, ct);
        result.ShouldBe(Mon20);
    }

    [Fact]
    public async Task NextAsync_returns_null_at_ceiling()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db, Mon13, Wed15, Fri17, Mon20);

        var result = await sut.NextAsync(Mon20, ct);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveOrFallbackAsync_returns_input_when_trading_day()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db, Mon13, Wed15, Fri17, Mon20);

        var result = await sut.ResolveOrFallbackAsync(Wed15, ct);
        result.ShouldBe(Wed15);
    }

    [Fact]
    public async Task ResolveOrFallbackAsync_returns_nearest_earlier_on_closed_day()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db, Mon13, Wed15, Fri17, Mon20);

        var result = await sut.ResolveOrFallbackAsync(Sun19, ct);
        result.ShouldBe(Fri17);
    }

    [Fact]
    public async Task ResolveOrFallbackAsync_throws_when_nothing_earlier_exists()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db, Mon20); // only Mon20 seeded

        await Should.ThrowAsync<NoTradingDaysException>(
            async () => await sut.ResolveOrFallbackAsync(Mon13, ct));
    }

    [Fact]
    public async Task EarliestAsync_throws_when_db_empty()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db); // no dates

        await Should.ThrowAsync<NoTradingDaysException>(
            async () => await sut.EarliestAsync(ct));
    }

    [Fact]
    public async Task LatestAsync_throws_when_db_empty()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db);

        await Should.ThrowAsync<NoTradingDaysException>(
            async () => await sut.LatestAsync(ct));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~EntryNavigationServiceTests"`
Expected: tests fail to compile (`IEntryNavigationService`, `EntryNavigationService` don't exist).

- [ ] **Step 3: Create the interface**

```csharp
// TradyStrat/Features/Dashboard/Navigation/IEntryNavigationService.cs
namespace TradyStrat.Features.Dashboard.Navigation;

public interface IEntryNavigationService
{
    Task<DateOnly>  EarliestAsync(CancellationToken ct);
    Task<DateOnly>  LatestAsync(CancellationToken ct);
    Task<DateOnly?> PreviousAsync(DateOnly current, CancellationToken ct);
    Task<DateOnly?> NextAsync(DateOnly current, CancellationToken ct);
    Task<DateOnly>  ResolveOrFallbackAsync(DateOnly requested, CancellationToken ct);
}
```

- [ ] **Step 4: Implement the service**

```csharp
// TradyStrat/Features/Dashboard/Navigation/EntryNavigationService.cs
using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Features.PriceFeed.Specifications;

namespace TradyStrat.Features.Dashboard.Navigation;

public sealed class EntryNavigationService(IReadRepositoryBase<PriceBar> bars)
    : IEntryNavigationService
{
    private const string FocusTicker = "CON3.L";

    public async Task<DateOnly> EarliestAsync(CancellationToken ct)
    {
        var bar = await bars.FirstOrDefaultAsync(new EarliestPriceBarSpec(FocusTicker), ct);
        return bar?.Date ?? throw new NoTradingDaysException();
    }

    public async Task<DateOnly> LatestAsync(CancellationToken ct)
    {
        var bar = await bars.FirstOrDefaultAsync(new LatestPriceBarSpec(FocusTicker), ct);
        return bar?.Date ?? throw new NoTradingDaysException();
    }

    public async Task<DateOnly?> PreviousAsync(DateOnly current, CancellationToken ct)
    {
        var bar = await bars.FirstOrDefaultAsync(new PriceBarBeforeSpec(FocusTicker, current), ct);
        return bar?.Date;
    }

    public async Task<DateOnly?> NextAsync(DateOnly current, CancellationToken ct)
    {
        var bar = await bars.FirstOrDefaultAsync(new PriceBarAfterSpec(FocusTicker, current), ct);
        return bar?.Date;
    }

    public async Task<DateOnly> ResolveOrFallbackAsync(DateOnly requested, CancellationToken ct)
    {
        var onOrBefore = await bars.ListAsync(
            new PriceBarsAsOfSpec(FocusTicker, requested), ct);
        if (onOrBefore.Count == 0) throw new NoTradingDaysException();
        return onOrBefore[^1].Date;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~EntryNavigationServiceTests"`
Expected: 11 tests pass.

- [ ] **Step 6: Commit**

```bash
git add TradyStrat/Features/Dashboard/Navigation/IEntryNavigationService.cs \
        TradyStrat/Features/Dashboard/Navigation/EntryNavigationService.cs \
        TradyStrat.Tests/Dashboard/Navigation/EntryNavigationServiceTests.cs
git commit -m "feat(navigation): add IEntryNavigationService for trading-day prev/next/resolve"
```

---

## Task 4: `ValidationResult` sum-type

**Files:**
- Create: `TradyStrat/Features/Dashboard/Navigation/ValidationResult.cs`

- [ ] **Step 1: Create the sum-type**

```csharp
// TradyStrat/Features/Dashboard/Navigation/ValidationResult.cs
namespace TradyStrat.Features.Dashboard.Navigation;

public abstract record ValidationResult
{
    public sealed record Live : ValidationResult;
    public sealed record Historical(DateOnly Date) : ValidationResult;
    public sealed record RedirectTo(string Url) : ValidationResult;

    private ValidationResult() { }
}
```

> Why a private constructor: keeps the hierarchy closed (only the three nested records can extend it), so consumers' `switch` expressions are exhaustive.

- [ ] **Step 2: Build to confirm compiles**

Run: `dotnet build TradyStrat.slnx`
Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat/Features/Dashboard/Navigation/ValidationResult.cs
git commit -m "feat(navigation): add ValidationResult sum-type"
```

---

## Task 5: `OnParamValidator` (TDD)

**Files:**
- Create: `TradyStrat/Features/Dashboard/Navigation/OnParamValidator.cs`
- Create: `TradyStrat.Tests/Dashboard/Navigation/OnParamValidatorTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// TradyStrat.Tests/Dashboard/Navigation/OnParamValidatorTests.cs
using Shouldly;
using TradyStrat.Features.Dashboard.Navigation;
using Xunit;

namespace TradyStrat.Tests.Dashboard.Navigation;

public class OnParamValidatorTests
{
    private static readonly DateOnly Earliest = new(2026, 4, 13); // Mon
    private static readonly DateOnly Latest   = new(2026, 4, 20); // Mon
    private static readonly DateOnly Fri17    = new(2026, 4, 17);
    private static readonly DateOnly Sun19    = new(2026, 4, 19); // closed

    private sealed class FakeNav : IEntryNavigationService
    {
        public Task<DateOnly>  EarliestAsync(CancellationToken ct) => Task.FromResult(Earliest);
        public Task<DateOnly>  LatestAsync(CancellationToken ct)   => Task.FromResult(Latest);
        public Task<DateOnly?> PreviousAsync(DateOnly current, CancellationToken ct) => throw new NotImplementedException();
        public Task<DateOnly?> NextAsync(DateOnly current, CancellationToken ct)     => throw new NotImplementedException();

        public Task<DateOnly> ResolveOrFallbackAsync(DateOnly requested, CancellationToken ct)
        {
            // Only Apr 13–17 and Apr 20 are trading days; Sun 19 falls back to Fri 17.
            DateOnly[] tradingDays = [Earliest, new(2026,4,14), new(2026,4,15), new(2026,4,16), Fri17, Latest];
            for (int i = tradingDays.Length - 1; i >= 0; i--)
                if (tradingDays[i] <= requested) return Task.FromResult(tradingDays[i]);
            throw new InvalidOperationException("test setup: nothing earlier");
        }
    }

    private static Task<ValidationResult> ValidateAsync(string? onParam) =>
        OnParamValidator.Validate(onParam, new FakeNav(), TestContext.Current.CancellationToken);

    [Fact] public async Task Null_input_is_Live()  => (await ValidateAsync(null)).ShouldBeOfType<ValidationResult.Live>();
    [Fact] public async Task Empty_input_is_Live() => (await ValidateAsync("")).ShouldBeOfType<ValidationResult.Live>();

    [Fact]
    public async Task Unparsable_input_redirects_to_root()
    {
        var r = await ValidateAsync("foo");
        var redirect = r.ShouldBeOfType<ValidationResult.RedirectTo>();
        redirect.Url.ShouldBe("/");
    }

    [Fact]
    public async Task After_latest_redirects_to_root()
    {
        var r = await ValidateAsync("2026-04-25");
        var redirect = r.ShouldBeOfType<ValidationResult.RedirectTo>();
        redirect.Url.ShouldBe("/");
    }

    [Fact]
    public async Task Before_earliest_redirects_to_earliest()
    {
        var r = await ValidateAsync("2026-04-10");
        var redirect = r.ShouldBeOfType<ValidationResult.RedirectTo>();
        redirect.Url.ShouldBe("/?on=2026-04-13");
    }

    [Fact]
    public async Task Closed_day_redirects_to_nearest_earlier()
    {
        var r = await ValidateAsync("2026-04-19"); // Sunday
        var redirect = r.ShouldBeOfType<ValidationResult.RedirectTo>();
        redirect.Url.ShouldBe("/?on=2026-04-17");
    }

    [Fact]
    public async Task Valid_trading_day_returns_Historical()
    {
        var r = await ValidateAsync("2026-04-15");
        var hist = r.ShouldBeOfType<ValidationResult.Historical>();
        hist.Date.ShouldBe(new DateOnly(2026, 4, 15));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~OnParamValidatorTests"`
Expected: tests fail to compile (`OnParamValidator` doesn't exist).

- [ ] **Step 3: Implement the validator**

```csharp
// TradyStrat/Features/Dashboard/Navigation/OnParamValidator.cs
using System.Globalization;

namespace TradyStrat.Features.Dashboard.Navigation;

public static class OnParamValidator
{
    public static async Task<ValidationResult> Validate(
        string? onParam, IEntryNavigationService nav, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(onParam))
            return new ValidationResult.Live();

        if (!DateOnly.TryParseExact(
                onParam, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var date))
            return new ValidationResult.RedirectTo("/");

        var latest = await nav.LatestAsync(ct);
        if (date > latest)
            return new ValidationResult.RedirectTo("/");

        var earliest = await nav.EarliestAsync(ct);
        if (date < earliest)
            return new ValidationResult.RedirectTo($"/?on={Format(earliest)}");

        var resolved = await nav.ResolveOrFallbackAsync(date, ct);
        if (resolved != date)
            return new ValidationResult.RedirectTo($"/?on={Format(resolved)}");

        return new ValidationResult.Historical(date);
    }

    private static string Format(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~OnParamValidatorTests"`
Expected: all 7 tests pass.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/Dashboard/Navigation/OnParamValidator.cs \
        TradyStrat.Tests/Dashboard/Navigation/OnParamValidatorTests.cs
git commit -m "feat(navigation): add OnParamValidator pure function for ?on= URL handling"
```

---

## Task 6: Add `LoadDashboardInput` record

**Files:**
- Create: `TradyStrat/Features/Dashboard/UseCases/LoadDashboardInput.cs`

- [ ] **Step 1: Create the record**

```csharp
// TradyStrat/Features/Dashboard/UseCases/LoadDashboardInput.cs
namespace TradyStrat.Features.Dashboard.UseCases;

public sealed record LoadDashboardInput(DateOnly TargetDate, bool IsHistorical);
```

- [ ] **Step 2: Build (will compile — not yet referenced)**

Run: `dotnet build TradyStrat.slnx`
Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat/Features/Dashboard/UseCases/LoadDashboardInput.cs
git commit -m "feat(dashboard): add LoadDashboardInput record"
```

---

## Task 7: Extend `DashboardViewModel`

Add the five navigation-related fields plus `IsHistorical`. Also relax `TodaysCall` to nullable for the historical-empty case.

**Files:**
- Modify: `TradyStrat/Features/Dashboard/DashboardViewModel.cs`

- [ ] **Step 1: Replace the record definition**

Replace the entire content of `TradyStrat/Features/Dashboard/DashboardViewModel.cs` with:

```csharp
using TradyStrat.Features.AiSuggestion.Backfill;
using TradyStrat.Features.AiSuggestion.CallDiff;
using TradyStrat.Features.Indicators;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Dashboard;

public sealed record DashboardViewModel(
    DateOnly Today,
    int EntryNumber,
    PortfolioSnapshot Portfolio,
    GoalConfig Goal,
    Suggestion? TodaysCall,
    IReadOnlyList<TickerView> Tickers,
    IReadOnlyList<GrowthPoint> Growth,
    DateOnly? LatestPriceDate,
    GoalPaceVm GoalPace,
    CallDiff CallDiff,
    BackfillStatus BackfillStatus,
    string PriceAsOfRelative,
    string CallAsOfRelative,
    string FxAsOfRelative,
    IReadOnlyDictionary<(string Ticker, IndicatorKind Kind), IndicatorSeries> IndicatorHistories,
    // new — time-travel
    bool IsHistorical,
    DateOnly EarliestTradingDay,
    DateOnly LatestTradingDay,
    DateOnly? PrevTradingDay,
    DateOnly? NextTradingDay);
```

- [ ] **Step 2: Build — expect failures in callers**

Run: `dotnet build TradyStrat.slnx`
Expected: errors in `LoadDashboardUseCase.cs` (constructor arity mismatch) and any consumer that pattern-matches on `TodaysCall`. We'll fix `LoadDashboardUseCase` next; `DashboardPage` and `TodaysCallCard` consume `vm.TodaysCall` but treat it as non-null today.

> Don't commit yet. The next two tasks fix the build.

---

## Task 8: Refit `LoadDashboardUseCase`

Switch the use case to `LoadDashboardInput`, route through the existing as-of overloads, branch the today's-call lookup, and populate the new view-model fields.

**Files:**
- Modify: `TradyStrat/Features/Dashboard/UseCases/LoadDashboardUseCase.cs`

- [ ] **Step 1: Replace the file contents**

Replace `TradyStrat/Features/Dashboard/UseCases/LoadDashboardUseCase.cs` with:

```csharp
using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.AiSuggestion.Backfill;
using TradyStrat.Features.AiSuggestion.CallDiff;
using TradyStrat.Features.AiSuggestion.Specifications;
using TradyStrat.Features.AiSuggestion.UseCases;
using TradyStrat.Features.Dashboard.Navigation;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Fx.Specifications;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Portfolio;
using TradyStrat.Features.PriceFeed.Specifications;
using TradyStrat.Features.Trades.Specifications;

namespace TradyStrat.Features.Dashboard.UseCases;

public sealed class LoadDashboardUseCase(
    IndicatorEngine indicators,
    PortfolioService portfolio,
    GrowthSeriesBuilder growth,
    FxConverter fx,
    IReadRepositoryBase<GoalConfig> goalRepo,
    IReadRepositoryBase<Trade> tradeRepo,
    IReadRepositoryBase<PriceBar> priceRepo,
    IReadRepositoryBase<Suggestion> suggestionRepo,
    IReadRepositoryBase<FxRate> fxRepo,
    GetTodaysSuggestionUseCase todaysSuggestion,
    ISuggestionBackfillCoordinator backfillCoord,
    IEntryNavigationService nav,
    ILogger<LoadDashboardUseCase> log)
    : UseCaseBase<LoadDashboardInput, DashboardViewModel>(log)
{
    private const string FocusTicker = "CON3.L";
    private const string FxPair      = "EURUSD";
    private const int    SparklineWindow = 30;

    private static readonly (string Ticker, string Currency)[] Catalog =
    [
        (FocusTicker, "USD"),
        ("COIN",      "USD"),
        ("BTC-USD",   "USD"),
    ];

    protected override async Task<DashboardViewModel> ExecuteCore(LoadDashboardInput input, CancellationToken ct)
    {
        var target = input.TargetDate;
        var goal   = await goalRepo.GetByIdAsync(1, ct) ?? GoalConfig.Default(DateTime.UtcNow);

        var tickers = new List<TickerView>();
        decimal? focusPriceEur = null;

        foreach (var (ticker, currency) in Catalog)
        {
            var reading = await indicators.ComputeFor(ticker, target, ct);
            decimal? eur = null;
            if (currency == "USD")
                eur = await fx.UsdToEurAsync(reading.Price, target, ct);
            if (ticker == FocusTicker) focusPriceEur = eur ?? reading.Price;

            var deltaPct = await ComputeDeltaPctAsync(ticker, ct);
            tickers.Add(new TickerView(
                ticker, currency, reading.Price, eur, deltaPct, reading.Zone));
        }

        var snap = await portfolio.SnapshotAsync(target, focusPriceEur ?? 0m, goal.TargetEur, ct);
        var growthSeries = await growth.BuildAsync(FocusTicker, ct);

        // Pin trailing growth point to the EUR-valued snapshot so chart and hero agree.
        // (Historical mode: this still anchors at the latest stored bar; documented limitation.)
        if (growthSeries.Count > 0)
        {
            var pinned = growthSeries.ToList();
            pinned[^1] = new GrowthPoint(target, snap.CurrentValueEur);
            growthSeries = pinned;
        }

        // Today's-call branch — read-only in historical mode, no AI invocation.
        Suggestion? todays;
        if (input.IsHistorical)
        {
            todays = await suggestionRepo.FirstOrDefaultAsync(new SuggestionForDateSpec(target), ct);
        }
        else
        {
            todays = await todaysSuggestion.ExecuteAsync(Unit.Value, ct);
        }

        var entryNum  = await tradeRepo.CountAsync(new TradesAsOfSpec(target), ct);
        var latestBar = await priceRepo.FirstOrDefaultAsync(new LatestPriceBarSpec(FocusTicker), ct);

        // Prior suggestion + call diff. Skip when today's call is null (historical-empty).
        Suggestion? prior = null;
        var callDiff = CallDiff.None;
        if (todays is not null)
        {
            prior = await suggestionRepo.FirstOrDefaultAsync(new PriorSuggestionSpec(target), ct);
            callDiff = new CallDiffBuilder()
                .WithToday(todays)
                .WithPrior(prior)
                .Build();
        }

        // Indicator histories per citation.
        var histories = new Dictionary<(string Ticker, IndicatorKind Kind), IndicatorSeries>();
        if (todays is not null)
        {
            foreach (var c in todays.Citations)
            {
                var kind = IndicatorKindParser.From(c.Indicator);
                if (kind is null) continue;
                var key = (c.Ticker, kind.Value);
                if (histories.ContainsKey(key)) continue;
                histories[key] = await indicators.HistoryFor(c.Ticker, kind.Value, SparklineWindow, target, ct);
            }
        }

        // Goal pace.
        var firstTrade = await tradeRepo.FirstOrDefaultAsync(new EarliestTradeSpec(), ct);
        var goalPace = GoalPaceCalculator.Compute(
            currentValueEur: snap.CurrentValueEur,
            goal: goal,
            today: target,
            firstTradeDate: firstTrade?.ExecutedOn);

        // Freshness pills.
        var nowUtc = DateTime.UtcNow;
        var fxLatest = await fxRepo.FirstOrDefaultAsync(new LatestFxRateSpec(FxPair, target), ct);
        var priceAsOf = latestBar is { } lb
            ? RelativeTimeFormatter.Format(lb.Date.ToDateTime(TimeOnly.MinValue), nowUtc)
            : "";
        var callAsOf = todays is null ? "" : RelativeTimeFormatter.Format(todays.CreatedAt, nowUtc);
        var fxAsOf   = fxLatest is { } fxr
            ? RelativeTimeFormatter.Format(fxr.FetchedAt, nowUtc)
            : "";

        // Backfill chain — live mode only.
        if (!input.IsHistorical && prior is { ForDate: var lastDate } && target.AddDays(-1) > lastDate)
        {
            _ = backfillCoord
                .EnsureBackfilledAsync(lastDate, target.AddDays(-1), CancellationToken.None)
                .ContinueWith(
                    t => LoadDashboardLog.BackfillCrashed(log, t.Exception),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }
        var backfillStatus = backfillCoord.Status;

        // Navigation fields.
        var earliest = await nav.EarliestAsync(ct);
        var latest   = await nav.LatestAsync(ct);
        var prev     = await nav.PreviousAsync(target, ct);
        var next     = target >= latest ? null : await nav.NextAsync(target, ct);

        return new DashboardViewModel(
            Today: target,
            EntryNumber: entryNum,
            Portfolio: snap,
            Goal: goal,
            TodaysCall: todays,
            Tickers: tickers,
            Growth: growthSeries,
            LatestPriceDate: latestBar?.Date,
            GoalPace: goalPace,
            CallDiff: callDiff,
            BackfillStatus: backfillStatus,
            PriceAsOfRelative: priceAsOf,
            CallAsOfRelative: callAsOf,
            FxAsOfRelative: fxAsOf,
            IndicatorHistories: histories,
            IsHistorical: input.IsHistorical,
            EarliestTradingDay: earliest,
            LatestTradingDay: latest,
            PrevTradingDay: prev,
            NextTradingDay: next);
    }

    private async Task<decimal?> ComputeDeltaPctAsync(string ticker, CancellationToken ct)
    {
        var bars = await priceRepo.ListAsync(new PriceBarsForTickerSpec(ticker), ct);
        if (bars.Count < 2) return null;
        var prev = bars[^2].Close;
        var curr = bars[^1].Close;
        if (prev == 0m) return null;
        return (curr - prev) / prev * 100m;
    }
}

internal static partial class LoadDashboardLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Backfill chain crashed unobserved")]
    public static partial void BackfillCrashed(ILogger logger, Exception? ex);
}
```

> The original file referenced `clock.UtcNow()` and `clock.TodayInExchangeTzFor(...)`. The use case no longer needs `IClock` — `target` arrives via input, and `DateTime.UtcNow` (used only for relative-time formatting) is fine inline. Removing the `IClock` parameter is part of this task.

- [ ] **Step 2: Build to confirm the use case compiles**

Run: `dotnet build TradyStrat.slnx`
Expected: build still fails — `DashboardPage.razor.cs` still calls `LoadDashboard.ExecuteAsync(Unit.Value, ...)`. We fix the page in Task 13. Don't commit yet — leave the use case change uncommitted with the view-model change for one logical commit at the end of Task 13.

---

## Task 9: Register `IEntryNavigationService` in DI

**Files:**
- Modify: `TradyStrat/Modules/DashboardModule.cs`

- [ ] **Step 1: Update the module**

Replace `TradyStrat/Modules/DashboardModule.cs` with:

```csharp
using TheAppManager.Modules;
using TradyStrat.Features.Dashboard.Navigation;
using TradyStrat.Features.Dashboard.UseCases;

namespace TradyStrat.Modules;

public sealed class DashboardModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<LoadDashboardUseCase>();
        builder.Services.AddScoped<IEntryNavigationService, EntryNavigationService>();
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build TradyStrat.slnx`
Expected: same errors as Task 8 (only `DashboardPage` remains). Don't commit yet — same accumulated commit at Task 13.

---

## Task 10: `RefreshFab` — `Historical` parameter

**Files:**
- Modify: `TradyStrat/Features/Dashboard/Components/RefreshFab.razor`
- Modify: `TradyStrat/Features/Dashboard/Components/RefreshFab.razor.cs`

- [ ] **Step 1: Update the code-behind**

Replace `TradyStrat/Features/Dashboard/Components/RefreshFab.razor.cs` with:

```csharp
using Microsoft.AspNetCore.Components;

namespace TradyStrat.Features.Dashboard.Components;

public partial class RefreshFab : ComponentBase
{
    [Parameter] public bool Busy { get; set; }
    [Parameter] public bool Historical { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
}
```

- [ ] **Step 2: Wrap the markup**

Replace `TradyStrat/Features/Dashboard/Components/RefreshFab.razor` with:

```razor
@if (!Historical)
{
    <button class="fab" @onclick="OnClick" disabled="@Busy" title="Refresh prices">
        @if (Busy) { <span class="spin">↻</span> } else { <span>↻</span> }
    </button>
}
```

- [ ] **Step 3: Build**

Run: `dotnet build TradyStrat.slnx`
Expected: still failing on `DashboardPage` only.

---

## Task 11: `TodaysCallCard` — `Historical` + nullable `Sug` + empty state

**Files:**
- Modify: `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor`
- Modify: `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.cs`

- [ ] **Step 1: Update the code-behind**

Replace `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.cs` with:

```csharp
using System.Globalization;
using Microsoft.AspNetCore.Components;
using TradyStrat.Features.AiSuggestion.Backfill;
using TradyStrat.Features.AiSuggestion.CallDiff;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Dashboard.Components;

public partial class TodaysCallCard : ComponentBase, IDisposable
{
    [Parameter] public Suggestion? Sug { get; set; }
    [Parameter] public DateOnly Today { get; set; }
    [Parameter] public string CallAsOfRelative { get; set; } = "";
    [Parameter] public bool Historical { get; set; }
    [Parameter] public EventCallback OnLogTrade { get; set; }
    [Parameter] public EventCallback OnRerun { get; set; }

    [Parameter, EditorRequired] public CallDiff CallDiff { get; set; } = CallDiff.None;
    [Parameter, EditorRequired] public BackfillStatus BackfillStatus { get; set; } = BackfillStatus.Idle.Instance;

    [Inject] private ISuggestionBackfillCoordinator Coordinator { get; set; } = null!;
    [Inject] private ILogger<TodaysCallCard> Log { get; set; } = null!;

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

    private string? _backfillLabel;
    private bool _disposed;

    private bool HasDiff =>
        Sug is not null &&
        !ReferenceEquals(CallDiff, CallDiff.None) &&
        !string.IsNullOrEmpty(CallDiff.SummaryParagraph);

    private string Verb => Sug?.Action switch
    {
        SuggestionAction.Acquire => "Acquire",
        SuggestionAction.Hold    => "Hold",
        SuggestionAction.Trim    => "Trim",
        SuggestionAction.Wait    => "Wait",
        _ => "—"
    };

    private string VerbStem => Sug?.Action switch
    {
        SuggestionAction.Acquire => "acquire",
        SuggestionAction.Hold    => "hold",
        SuggestionAction.Trim    => "trim",
        SuggestionAction.Wait    => "wait",
        _ => "none"
    };

    protected override void OnInitialized()
    {
        Coordinator.StatusChanged += OnBackfillStatus;
        UpdateBackfillLabel(Coordinator.Status);
    }

    private void OnBackfillStatus(BackfillStatus status)
    {
        if (_disposed) return;
        _ = InvokeAsync(() =>
        {
            if (_disposed) return;
            try
            {
                UpdateBackfillLabel(status);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                TodaysCallCardLog.StatusChangedCallbackFailed(Log, ex);
            }
        });
    }

    private void UpdateBackfillLabel(BackfillStatus status)
    {
        _backfillLabel = status switch
        {
            BackfillStatus.Running r => $"backfilling {r.Total - r.Remaining + 1} of {r.Total} — {r.CurrentDate:dd MMM}",
            BackfillStatus.Failed f  => $"stopped at {f.FailedAt:dd MMM} — {f.Reason}",
            _ => null,
        };
    }

    public void Dispose()
    {
        _disposed = true;
        Coordinator.StatusChanged -= OnBackfillStatus;
        GC.SuppressFinalize(this);
    }
}

internal static partial class TodaysCallCardLog
{
    [LoggerMessage(Level = LogLevel.Warning,
        Message = "TodaysCallCard StatusChanged callback threw")]
    public static partial void StatusChangedCallbackFailed(ILogger logger, Exception ex);
}
```

- [ ] **Step 2: Update the markup**

Replace `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor` with:

```razor
@using TradyStrat.Common.Domain
<div class="call">
    <div class="label">
        @(Historical ? $"Call for {Today.ToString("d MMMM", FrFr)}" : $"Today's call · {Today.ToString("d MMMM", FrFr)}")
        @if (!Historical && !string.IsNullOrEmpty(CallAsOfRelative))
        {
            <span class="freshness"> · @CallAsOfRelative</span>
        }
    </div>

    @if (Sug is null)
    {
        <p class="reasons empty">No AI call recorded for @Today.ToString("d MMMM yyyy", FrFr).</p>
    }
    else
    {
        @if (HasDiff)
        {
            <CallDiffList Rows="CallDiffRowProjector.Project(CallDiff)" />
        }
        @if (_backfillLabel is not null)
        {
            <div class="backfill-pill">@_backfillLabel</div>
        }

        <p class="reasons">
            <span class="verb-drop" data-verb="@VerbStem">@Verb</span><span class="verb-tail" data-verb="@VerbStem">.</span> @Sug.Rationale
        </p>

        @if (Sug is { Action: SuggestionAction.Acquire, QuantityHint: { } q and > 0m, MaxPriceHint: { } mp and > 0m })
        {
            <div class="order num">
                @q.ToString("F0", FrFr) sh CON3 · ≤ €@mp.ToString("F2", FrFr)
                · ≈ €@((q * mp).ToString("F2", FrFr))
            </div>
        }

        @if (!Historical)
        {
            <div class="actions">
                <button class="cta" disabled="@(Sug.Action != SuggestionAction.Acquire)"
                        @onclick="OnLogTrade">Log trade</button>
                <button class="cta ghost" @onclick="OnRerun">Re-run AI</button>
            </div>
        }
    }
</div>
```

- [ ] **Step 3: Build**

Run: `dotnet build TradyStrat.slnx`
Expected: still failing on `DashboardPage`; `TodaysCallCard` compiles.

---

## Task 12: `VaultMasthead` — nav controls

**Files:**
- Modify: `TradyStrat/Features/Dashboard/Components/VaultMasthead.razor`
- Modify: `TradyStrat/Features/Dashboard/Components/VaultMasthead.razor.cs`

- [ ] **Step 1: Update the code-behind**

Replace `TradyStrat/Features/Dashboard/Components/VaultMasthead.razor.cs` with:

```csharp
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace TradyStrat.Features.Dashboard.Components;

public partial class VaultMasthead : ComponentBase
{
    [Parameter] public DateOnly Today { get; set; }
    [Parameter] public int EntryNumber { get; set; }
    [Parameter] public string PriceAsOfRelative { get; set; } = "";

    // Time-travel — only set on the dashboard. Self-hide otherwise.
    [Parameter] public DateOnly? PrevTradingDay { get; set; }
    [Parameter] public DateOnly? NextTradingDay { get; set; }
    [Parameter] public DateOnly? EarliestTradingDay { get; set; }
    [Parameter] public DateOnly? LatestTradingDay { get; set; }
    [Parameter] public bool IsHistorical { get; set; }
    [Parameter] public EventCallback<DateOnly> OnDateSelected { get; set; }

    private bool ShowNav =>
        EarliestTradingDay is not null && LatestTradingDay is not null;

    private static string FormatDate(DateOnly d)
        => d.ToString("dd · MM · yyyy", CultureInfo.InvariantCulture);

    private static string FormatIso(DateOnly d)
        => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private string PrevHref =>
        PrevTradingDay is { } p ? $"/?on={FormatIso(p)}" : "#";

    private string NextHref =>
        NextTradingDay is { } n ? $"/?on={FormatIso(n)}" : "#";

    private async Task OnPickerChanged(ChangeEventArgs e)
    {
        if (e.Value is string s &&
            DateOnly.TryParseExact(
                s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var picked))
        {
            await OnDateSelected.InvokeAsync(picked);
        }
    }
}
```

- [ ] **Step 2: Update the markup**

Replace `TradyStrat/Features/Dashboard/Components/VaultMasthead.razor` with:

```razor
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
    @if (ShowNav)
    {
        <div class="entry-nav">
            <a class="nav-step @(PrevTradingDay is null ? "disabled" : "")"
               href="@PrevHref"
               aria-disabled="@(PrevTradingDay is null)"
               title="Previous trading day">‹</a>

            <label class="date-pill">
                <span>@FormatDate(Today)</span>
                <input type="date"
                       value="@FormatIso(Today)"
                       min="@FormatIso(EarliestTradingDay!.Value)"
                       max="@FormatIso(LatestTradingDay!.Value)"
                       @onchange="OnPickerChanged" />
            </label>

            <a class="nav-step @(NextTradingDay is null ? "disabled" : "")"
               href="@NextHref"
               aria-disabled="@(NextTradingDay is null)"
               title="Next trading day">›</a>

            @if (IsHistorical)
            {
                <a class="return-today" href="/">return to today</a>
            }
        </div>
    }
    <div class="meta">@FormatDate(Today) · entry no. @EntryNumber.ToString("D4", CultureInfo.InvariantCulture)@if (!string.IsNullOrEmpty(PriceAsOfRelative))
    {
        <span class="freshness"> · prices @PriceAsOfRelative</span>
    }</div>
</div>
```

- [ ] **Step 3: Build**

Run: `dotnet build TradyStrat.slnx`
Expected: still failing on `DashboardPage` only.

---

## Task 13: `DashboardPage` — wire it all together

This is the integration step. After this task, the build is green again.

**Files:**
- Modify: `TradyStrat/Features/Dashboard/DashboardPage.razor`
- Modify: `TradyStrat/Features/Dashboard/DashboardPage.razor.cs`

- [ ] **Step 1: Update the code-behind**

Replace `TradyStrat/Features/Dashboard/DashboardPage.razor.cs` with:

```csharp
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.Time;
using TradyStrat.Features.AiSuggestion.UseCases;
using TradyStrat.Features.Dashboard.Navigation;
using TradyStrat.Features.Dashboard.UseCases;
using TradyStrat.Features.PriceFeed.UseCases;

namespace TradyStrat.Features.Dashboard;

public partial class DashboardPage : ComponentBase, IAsyncDisposable
{
    [Inject] private LoadDashboardUseCase LoadDashboard { get; set; } = default!;
    [Inject] private ForceRefetchSuggestionUseCase ForceRefetch { get; set; } = default!;
    [Inject] private RefreshAllPricesUseCase RefreshPrices { get; set; } = default!;
    [Inject] private IEntryNavigationService Nav { get; set; } = default!;
    [Inject] private IClock Clock { get; set; } = default!;
    [Inject] private NavigationManager NavManager { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "on")] public string? OnParam { get; set; }

    private DashboardViewModel? _vm;
    private string? _error;
    private bool _busy;
    private bool _showRerunConfirm;

    private IJSObjectReference? _keysModule;
    private DotNetObjectReference<DashboardPage>? _selfRef;

    protected override async Task OnParametersSetAsync()
    {
        var ct = CancellationToken.None;
        var result = await OnParamValidator.Validate(OnParam, Nav, ct);

        DateOnly target;
        bool isHistorical;
        switch (result)
        {
            case ValidationResult.RedirectTo r:
                NavManager.NavigateTo(r.Url, replace: true);
                return;
            case ValidationResult.Live:
                target = Clock.TodayInExchangeTzFor("CON3.L");
                isHistorical = false;
                break;
            case ValidationResult.Historical h:
                target = h.Date;
                isHistorical = true;
                break;
            default:
                return;
        }

        try
        {
            _vm = await LoadDashboard.ExecuteAsync(
                new LoadDashboardInput(target, isHistorical), ct);
            _error = null;
        }
        catch (TradyStratException ex)
        {
            _vm = null;
            _error = ex.Message;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _keysModule = await JS.InvokeAsync<IJSObjectReference>(
                "import", "./js/dashboard-keys.js");
            _selfRef = DotNetObjectReference.Create(this);
            await _keysModule.InvokeVoidAsync("attach", _selfRef);
        }
    }

    [JSInvokable]
    public Task OnPrev()
    {
        if (_vm?.PrevTradingDay is { } prev)
            NavManager.NavigateTo($"/?on={prev.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnNext()
    {
        if (_vm?.NextTradingDay is { } next)
            NavManager.NavigateTo($"/?on={next.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        else if (_vm is { IsHistorical: true })
            NavManager.NavigateTo("/");
        return Task.CompletedTask;
    }

    private void OnDateSelected(DateOnly picked) =>
        NavManager.NavigateTo($"/?on={picked.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");

    private async Task OnRefreshClicked()
    {
        if (_vm?.IsHistorical == true) return;
        _busy = true;
        try   { await RefreshPrices.ExecuteAsync(Common.UseCases.Unit.Value, CancellationToken.None); await ReloadAsync(); }
        finally { _busy = false; }
    }

    private void OnRerunRequested()
    {
        if (_vm?.IsHistorical == true) return;
        _showRerunConfirm = true;
    }

    private async Task ConfirmRerun()
    {
        _showRerunConfirm = false;
        _busy = true;
        try   { await ForceRefetch.ExecuteAsync(Common.UseCases.Unit.Value, CancellationToken.None); await ReloadAsync(); }
        finally { _busy = false; }
    }

    private static void OnLogTradeRequested()
    {
        // Stub — see VaultMasthead nav for /trades.
    }

    private async Task ReloadAsync()
    {
        if (_vm is null) return;
        _vm = await LoadDashboard.ExecuteAsync(
            new LoadDashboardInput(_vm.Today, _vm.IsHistorical), CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_keysModule is not null)
            {
                await _keysModule.InvokeVoidAsync("detach");
                await _keysModule.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone — nothing to clean up.
        }
        _selfRef?.Dispose();
    }
}
```

- [ ] **Step 2: Update the markup**

Replace `TradyStrat/Features/Dashboard/DashboardPage.razor` with:

```razor
@page "/"
@rendermode InteractiveServer
@using TradyStrat.Features.Dashboard.Components

@if (_vm is null && _error is null)
{
    <p class="loading">Loading…</p>
}
else if (_error is not null)
{
    <div class="error">
        <p class="error-title">
            Could not load dashboard
        </p>
        <p class="error-message">
            @_error
        </p>
        <p class="error-hint">
            Common cause: no price bars for CON3.L (Leverage Shares 3x Long Coinbase ETP, LSE) in the database. Run with internet so the price feed can warm the cache, then refresh.
        </p>
        <button class="btn error-action" @onclick="OnRefreshClicked" disabled="@_busy">
            @(_busy ? "Refreshing…" : "Retry")
        </button>
    </div>
}
else
{
    var vm = _vm!;
    <div class="dash-stage">
        <VaultMasthead Today="vm.Today" EntryNumber="vm.EntryNumber"
                       PriceAsOfRelative="@vm.PriceAsOfRelative"
                       PrevTradingDay="vm.PrevTradingDay"
                       NextTradingDay="vm.NextTradingDay"
                       EarliestTradingDay="vm.EarliestTradingDay"
                       LatestTradingDay="vm.LatestTradingDay"
                       IsHistorical="vm.IsHistorical"
                       OnDateSelected="OnDateSelected" />
        <div class="hero-row">
            <HeroCapital Snap="vm.Portfolio" Goal="vm.Goal" Today="vm.Today"
                         GoalPace="vm.GoalPace" />
            <TodaysCallCard Sug="vm.TodaysCall" Today="vm.Today"
                            CallAsOfRelative="@vm.CallAsOfRelative"
                            CallDiff="vm.CallDiff"
                            BackfillStatus="vm.BackfillStatus"
                            Historical="vm.IsHistorical"
                            OnRerun="OnRerunRequested" OnLogTrade="OnLogTradeRequested" />
        </div>
        <CitationsBlock Sug="vm.TodaysCall" Today="vm.Today"
                        CallDiff="vm.CallDiff"
                        IndicatorHistories="vm.IndicatorHistories" />
        <PortfolioRail Snap="vm.Portfolio" Tickers="vm.Tickers" />
        <GrowthChart Points="vm.Growth" Goal="vm.Goal" Portfolio="vm.Portfolio" />
    </div>
    <RefreshFab Busy="_busy" Historical="vm.IsHistorical" OnClick="OnRefreshClicked" />

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
```

> Note: in Step 2 above, `<CitationsBlock Sug="vm.TodaysCall" ... />` is wrapped because `CitationsBlock` declares `[Parameter, EditorRequired] public Suggestion Sug { get; set; } = null!;` (non-nullable). The wrapper is in Step 3.

- [ ] **Step 3: Wrap `CitationsBlock` in a null guard**

`CitationsBlock`'s `Sug` parameter is non-nullable (`[EditorRequired] public Suggestion Sug`). Replace the bare `<CitationsBlock .../>` line in the markup from Step 2 with:

```razor
        @if (vm.TodaysCall is not null)
        {
            <CitationsBlock Sug="vm.TodaysCall" Today="vm.Today"
                            CallDiff="vm.CallDiff"
                            IndicatorHistories="vm.IndicatorHistories" />
        }
```

- [ ] **Step 4: Update existing `LoadDashboardUseCaseTests` for the new signature**

The use case now takes `LoadDashboardInput` (not `Unit`), accepts `IEntryNavigationService`, and no longer takes `IClock`. Replace `TradyStrat.Tests/Dashboard/UseCases/LoadDashboardUseCaseTests.cs` with:

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using TradyStrat.Features.Indicators.Zones;
using TradyStrat.Features.Indicators.History;
using Shouldly;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.AiSuggestion.UseCases;
using TradyStrat.Features.Dashboard.UseCases;
using TradyStrat.Features.AiSuggestion.Backfill;
using TradyStrat.Features.AiSuggestion.Snapshot;
using TradyStrat.Features.Dashboard.Navigation;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Portfolio;
using TradyStrat.Common.Domain;
using TradyStrat.Features.Indicators.Bollinger;
using TradyStrat.Features.Indicators.Ichimoku;
using TradyStrat.Features.Indicators.MovingAverage;
using TradyStrat.Features.Indicators.Rsi;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Indicators;        // SeriesLoader
using TradyStrat.Tests.Specifications;
using TradyStrat.Tests.Common.Time;
using TradyStrat.Tests.AiSuggestion.UseCases;  // StubSnapshotFactory, StubAiClient
using Xunit;

namespace TradyStrat.Tests.Dashboard.UseCases;

public class LoadDashboardUseCaseTests
{
    private static readonly DateOnly Target = new(2026, 5, 6);

    private sealed class NullCoordinator : ISuggestionBackfillCoordinator
    {
        public BackfillStatus Status => BackfillStatus.Idle.Instance;
#pragma warning disable CS0067 // event never raised in stub
        public event Action<BackfillStatus>? StatusChanged;
#pragma warning restore CS0067
        public Task EnsureBackfilledAsync(DateOnly fromExclusive, DateOnly toInclusive, CancellationToken ct)
        {
            EnsureBackfilledCalls++;
            return Task.CompletedTask;
        }
        public int EnsureBackfilledCalls;
    }

    private sealed class FakeNav : IEntryNavigationService
    {
        public DateOnly Earliest = new(2026, 1, 1);
        public DateOnly Latest   = Target;
        public DateOnly? Prev    = new(2026, 5, 5);
        public DateOnly? Next    = null;

        public Task<DateOnly>  EarliestAsync(CancellationToken ct) => Task.FromResult(Earliest);
        public Task<DateOnly>  LatestAsync(CancellationToken ct)   => Task.FromResult(Latest);
        public Task<DateOnly?> PreviousAsync(DateOnly current, CancellationToken ct) => Task.FromResult(Prev);
        public Task<DateOnly?> NextAsync(DateOnly current, CancellationToken ct)     => Task.FromResult(Next);
        public Task<DateOnly>  ResolveOrFallbackAsync(DateOnly requested, CancellationToken ct) => Task.FromResult(requested);
    }

    private static (LoadDashboardUseCase uc, NullCoordinator coord, FakeNav nav)
        BuildSut(TradyStrat.Data.AppDbContext db)
    {
        var classifier = new ZoneClassifier(new IZoneRule[] {
            new BollingerZoneRule(), new RsiZoneRule(),
            new MovingAverageZoneRule(), new IchimokuZoneRule() });
        var indicators = new IndicatorEngine(new TestRepo<PriceBar>(db), classifier, new IndicatorHistoryProviderFactory([]));
        var portfolio  = new PortfolioService(new TestRepo<Trade>(db));
        var growth     = new GrowthSeriesBuilder(new TestRepo<Trade>(db), new TestRepo<PriceBar>(db));
        var fx         = new FxConverter(new TestRepo<FxRate>(db));
        var clock      = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));

        var snapStub = new StubSnapshotFactory(new AiSnapshot(
            Target, GoalConfig.Default(DateTime.UtcNow),
            new(0,0,0,0,0,0), [], [], 1.08m, "h"));
        var aiStub = new StubAiClient(new Suggestion {
            Id = 0, ForDate = Target, Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "from-ai", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });
        var todays = new GetTodaysSuggestionUseCase(
            new TestRepo<Suggestion>(db), snapStub, aiStub, clock,
            NullLogger<GetTodaysSuggestionUseCase>.Instance);

        var coord = new NullCoordinator();
        var nav   = new FakeNav();

        var uc = new LoadDashboardUseCase(
            indicators, portfolio, growth, fx,
            new TestRepo<GoalConfig>(db),
            new TestRepo<Trade>(db),
            new TestRepo<PriceBar>(db),
            new TestRepo<Suggestion>(db),
            new TestRepo<FxRate>(db),
            todays,
            coord,
            nav,
            NullLogger<LoadDashboardUseCase>.Instance);

        return (uc, coord, nav);
    }

    private static async Task SeedBaseAsync(TradyStrat.Data.AppDbContext db, CancellationToken ct,
        Suggestion? seedSuggestion = null)
    {
        foreach (var b in SeriesLoader.LoadCloses("CON3.L")) db.PriceBars.Add(b);
        foreach (var t in new[] { "COIN", "BTC-USD" })
            db.PriceBars.Add(new PriceBar { Id=0, Ticker=t, Date=Target,
                Open=200, High=200, Low=200, Close=200, Volume=1 });
        db.FxRates.Add(new FxRate { Id=0, Pair="EURUSD", Date=Target,
            UsdPerEur = 1.08m, FetchedAt = DateTime.UtcNow });
        db.Goals.Add(GoalConfig.Default(DateTime.UtcNow));
        db.Trades.Add(new Trade {
            Id = 0, ExecutedOn = new(2025,12,7), Side = TradeSide.Buy,
            Quantity = 100m, PricePerShare = 4m, FeesEur = 0m, Note = null,
            CreatedAt = DateTime.UtcNow });
        if (seedSuggestion is not null) db.Suggestions.Add(seedSuggestion);
        await db.SaveChangesAsync(ct);
    }

    [Fact]
    public async Task Live_mode_composes_view_model_with_three_tickers_and_growth_series()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        await SeedBaseAsync(db, ct, new Suggestion {
            Id = 0, ForDate = Target, Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "stable", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });

        var (uc, _, _) = BuildSut(db);
        var vm = await uc.ExecuteAsync(new LoadDashboardInput(Target, IsHistorical: false), ct);

        vm.Today.ShouldBe(Target);
        vm.Tickers.Count.ShouldBe(3);
        vm.Growth.Count.ShouldBeGreaterThan(0);
        vm.Goal.TargetEur.ShouldBe(1_000_000m);
        vm.TodaysCall.ShouldNotBeNull();
        vm.TodaysCall.Rationale.ShouldBe("stable");
        vm.IsHistorical.ShouldBeFalse();
        vm.PrevTradingDay.ShouldBe(new DateOnly(2026, 5, 5));
        vm.NextTradingDay.ShouldBeNull();
    }

    [Fact]
    public async Task Last_growth_point_matches_hero_current_value_eur()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        await SeedBaseAsync(db, ct, new Suggestion {
            Id = 0, ForDate = Target, Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "stable", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });

        var (uc, _, _) = BuildSut(db);
        var vm = await uc.ExecuteAsync(new LoadDashboardInput(Target, IsHistorical: false), ct);

        vm.Growth.Count.ShouldBeGreaterThan(0);
        var lastPoint = vm.Growth[^1];
        lastPoint.ValueEur.ShouldBe(vm.Portfolio.CurrentValueEur);
        lastPoint.Date.ShouldBe(vm.Today);
    }

    [Fact]
    public async Task Historical_mode_uses_stored_suggestion_and_skips_backfill()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        await SeedBaseAsync(db, ct, new Suggestion {
            Id = 0, ForDate = Target, Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "from-db", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });

        var (uc, coord, _) = BuildSut(db);
        var vm = await uc.ExecuteAsync(new LoadDashboardInput(Target, IsHistorical: true), ct);

        vm.IsHistorical.ShouldBeTrue();
        vm.TodaysCall.ShouldNotBeNull();
        // Came from the suggestionRepo, not the StubAiClient (which would have set "from-ai").
        vm.TodaysCall.Rationale.ShouldBe("from-db");
        coord.EnsureBackfilledCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Historical_mode_renders_empty_call_when_no_suggestion_stored()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        await SeedBaseAsync(db, ct, seedSuggestion: null); // no Suggestion row

        var (uc, _, _) = BuildSut(db);
        var vm = await uc.ExecuteAsync(new LoadDashboardInput(Target, IsHistorical: true), ct);

        vm.TodaysCall.ShouldBeNull();
        vm.CallDiff.ShouldBe(CallDiff.None);
    }

    [Fact]
    public async Task Entry_number_uses_TradesAsOfSpec_target_date()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        await SeedBaseAsync(db, ct, new Suggestion {
            Id = 0, ForDate = Target, Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "x", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });

        // One additional trade *after* the target date — should NOT be counted.
        db.Trades.Add(new Trade {
            Id = 0, ExecutedOn = new(2026, 6, 1), Side = TradeSide.Buy,
            Quantity = 1m, PricePerShare = 1m, FeesEur = 0m, Note = null,
            CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);

        var (uc, _, _) = BuildSut(db);
        var vm = await uc.ExecuteAsync(new LoadDashboardInput(Target, IsHistorical: true), ct);

        vm.EntryNumber.ShouldBe(1); // only the original 2025-12-07 trade is on-or-before target
    }
}
```

> Note the `CallDiff` import. If `CallDiff.None` is in a namespace not imported above, add `using TradyStrat.Features.AiSuggestion.CallDiff;`.

- [ ] **Step 5: Build**

Run: `dotnet build TradyStrat.slnx`
Expected: build succeeds. (`dashboard-keys.js` doesn't exist yet but is only fetched at runtime — the build doesn't see it.)

- [ ] **Step 6: Run all tests**

Run: `dotnet test`
Expected: all green — existing 2 use-case tests refit + 3 new historical-mode tests + everything else.

- [ ] **Step 7: Commit Tasks 7–13 together**

```bash
git add TradyStrat/Features/Dashboard/DashboardViewModel.cs \
        TradyStrat/Features/Dashboard/UseCases/LoadDashboardUseCase.cs \
        TradyStrat/Modules/DashboardModule.cs \
        TradyStrat/Features/Dashboard/Components/RefreshFab.razor \
        TradyStrat/Features/Dashboard/Components/RefreshFab.razor.cs \
        TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor \
        TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.cs \
        TradyStrat/Features/Dashboard/Components/VaultMasthead.razor \
        TradyStrat/Features/Dashboard/Components/VaultMasthead.razor.cs \
        TradyStrat/Features/Dashboard/DashboardPage.razor \
        TradyStrat/Features/Dashboard/DashboardPage.razor.cs \
        TradyStrat.Tests/Dashboard/UseCases/LoadDashboardUseCaseTests.cs
git commit -m "feat(dashboard): wire ?on= time-travel through use case and UI components"
```

---

## Task 14: Add the `dashboard-keys.js` ES module

**Files:**
- Create: `TradyStrat/wwwroot/js/dashboard-keys.js`

- [ ] **Step 1: Create the module**

```javascript
// TradyStrat/wwwroot/js/dashboard-keys.js
let dotNetRef = null;
let listener = null;

const isEditableTarget = (el) => {
    if (!el) return false;
    if (el.matches?.('input, textarea, select')) return true;
    if (el.isContentEditable) return true;
    return false;
};

export function attach(ref) {
    dotNetRef = ref;
    listener = (e) => {
        if (isEditableTarget(document.activeElement)) return;
        if (e.key === 'ArrowLeft')  { e.preventDefault(); dotNetRef?.invokeMethodAsync('OnPrev'); }
        else if (e.key === 'ArrowRight') { e.preventDefault(); dotNetRef?.invokeMethodAsync('OnNext'); }
    };
    document.addEventListener('keydown', listener);
}

export function detach() {
    if (listener) {
        document.removeEventListener('keydown', listener);
        listener = null;
    }
    dotNetRef = null;
}
```

- [ ] **Step 2: Build**

Run: `dotnet build TradyStrat.slnx`
Expected: build succeeds; static asset is picked up automatically.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat/wwwroot/js/dashboard-keys.js
git commit -m "feat(dashboard): add dashboard-keys.js for arrow-key navigation"
```

---

## Task 15: Add CSS for the new masthead controls

**Files:**
- Modify: `TradyStrat/wwwroot/css/vault.css`

- [ ] **Step 1: Append styles**

Append the following at the end of `TradyStrat/wwwroot/css/vault.css`:

```css
/* ---- Time-travel masthead controls ----------------------------------- */

.masthead .entry-nav {
    display: flex;
    align-items: center;
    gap: 10px;
    flex-wrap: wrap;
}

.masthead .nav-step {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 28px;
    height: 28px;
    border: 1px solid var(--rule, #c8c0ad);
    border-radius: 4px;
    background: var(--paper-tint, #f4ede0);
    color: var(--ink, #2a2620);
    text-decoration: none;
    font-size: 18px;
    line-height: 1;
    user-select: none;
    transition: background-color 120ms ease;
}

.masthead .nav-step:hover { background: var(--paper-tint-hover, #ebe2cf); }

.masthead .nav-step.disabled,
.masthead .nav-step[aria-disabled="true"] {
    opacity: 0.35;
    pointer-events: none;
    cursor: default;
}

.masthead .date-pill {
    position: relative;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 4px 10px;
    border: 1px solid var(--rule, #c8c0ad);
    border-radius: 4px;
    background: var(--paper-tint, #f4ede0);
    font-family: 'JetBrains Mono', monospace;
    font-size: 12px;
    cursor: pointer;
}

.masthead .date-pill input[type="date"] {
    /* Native picker, hidden visually but still focusable for the calendar UI. */
    position: absolute;
    inset: 0;
    width: 100%;
    height: 100%;
    opacity: 0;
    cursor: pointer;
    border: none;
    background: transparent;
}

.masthead .return-today {
    font-family: 'Cormorant Garamond', serif;
    font-style: italic;
    font-size: 13px;
    color: var(--ink-muted, #6e6759);
    text-decoration: none;
    padding: 3px 8px;
    border: 1px solid transparent;
    border-radius: 3px;
}

.masthead .return-today:hover {
    border-color: var(--rule, #c8c0ad);
}
```

> CSS variables (`--rule`, `--paper-tint`, `--ink`, `--ink-muted`) follow the existing Vault palette conventions. If specific names already exist in `vault.css` with different identifiers, search the file and rename to match — leave a single visual style.

- [ ] **Step 2: Build to verify the static asset is valid**

Run: `dotnet build TradyStrat.slnx`
Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat/wwwroot/css/vault.css
git commit -m "feat(dashboard): add masthead time-travel control styles"
```

---

## Task 16: Manual smoke (per spec §10.4)

This task verifies the integration end-to-end. No code changes if all checks pass.

- [ ] **Step 1: Start the dev server**

```bash
dotnet run --project TradyStrat
```

Visit **http://127.0.0.1:5180** (the bind address from `appsettings.json`).

- [ ] **Step 2: Live mode baseline**

- Confirm the dashboard renders at `/` in live mode.
- All three actions visible: refresh FAB, "Re-run AI", "Log trade".
- Masthead shows prev / next chevrons; `›` is disabled (you're at today). "Return to today" is hidden.

- [ ] **Step 3: Click `‹` once**

- URL becomes `/?on={prev trading day}`.
- Dashboard updates to that date.
- FAB is gone. "Re-run AI" / "Log trade" are gone.
- "Return to today" link appears.
- Masthead "entry no." may be lower than live (correct: it's the trade count as of that date).

- [ ] **Step 4: Date picker → closed day**

- Click the date pill → picker opens.
- Pick a Sunday.
- Page redirects to nearest earlier Friday (`?on=` reflects that Friday).

- [ ] **Step 5: "Return to today"**

- Click the "return to today" link.
- URL collapses to `/`.
- FAB and action buttons return.

- [ ] **Step 6: Browser back / forward**

- Click browser ◀ → returns to historical view.
- Click browser ▶ → returns to today.

- [ ] **Step 7: Keyboard shortcuts**

- Press `←` → navigates to prev trading day.
- Press `→` → navigates back forward.
- At the floor / ceiling, the buttons are visibly `disabled`.
- Click into the date `<input>` (focus it). Press `←` / `→` — should move the picker, NOT navigate the page (focus short-circuit works).

- [ ] **Step 8: Garbage `?on=`**

- Type `/?on=garbage` in the address bar manually.
- Page redirects cleanly to `/`.

- [ ] **Step 9: Trades / Settings unchanged**

- Visit `/trades` → no date controls in masthead. Add / list trades work as before.
- Visit `/settings` → no date controls. Form works as before.

- [ ] **Step 10: Historical day with no AI suggestion**

- Pick a date the AI has not generated for (a date with a price bar but no row in the `Suggestions` table).
- `TodaysCallCard` shows "No AI call recorded for {date}." — no AI invocation fires (verify by checking logs at `~/Library/Application Support/TradyStrat/logs/tradystrat-yyyymmdd.log` — no `GetTodaysSuggestionUseCase ok` line for the page load).

- [ ] **Step 11: Stop dev server**

`Ctrl+C` in the running terminal.

- [ ] **Step 12: Commit (only if you fixed something during smoke)**

```bash
git status
# If anything is modified, fix or rerun smoke. Otherwise, no commit needed.
```

---

## Acceptance

The plan is fully executed when:

- All 16 tasks are checked off.
- `dotnet test` passes (existing + ~22 new tests).
- The manual smoke (Task 16) passes end-to-end.
- No new exception types beyond `NoTradingDaysException`.
- No new dependencies in `Directory.Packages.props` or the test project.
- No schema migrations.
