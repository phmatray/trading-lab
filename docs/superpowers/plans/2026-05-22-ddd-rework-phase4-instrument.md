# DDD Rework — Phase 4 (Instrument aggregate) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rewrite the anemic `Instrument` record as a small reference-data aggregate root with VO fields (`Exchange`, `TimezoneId`), absorb the duplicate `InstrumentMetadata` DTO into the AR, expose `Probed`/`Existing` factories + `Confirm`/`Rename` behavior, and swap every `IReadRepositoryBase<Instrument>` / `IRepositoryBase<Instrument>` consumer to a per-aggregate `IInstrumentRepository` port that enforces system-wide ticker uniqueness at the write path.

**Architecture:** `Instrument` becomes a `sealed class` with private setters and an EF-friendly parameterless ctor. Two static factories — `Probed` (id-zero sentinel, no `AddedAt`) and `Existing` (rehydration with persisted id + AddedAt) — encode the lifecycle. A new `IInstrumentRepository` port lives in Application; `EfInstrumentRepository` provides duplicate-check + add. `ProbeInstrumentUseCase` now returns the probed `Instrument` directly (no more two-type juggling); `AddInstrumentUseCase` receives the probed `Instrument`, calls `Confirm(clock)`, and delegates to the repo.

**Tech Stack:** .NET 10, EF Core 10.0.7 (Sqlite + value converters), Ardalis.Specification 9.3.1 (deletions only — no new specs), xunit.v3 3.2.2, Shouldly 4.3.0.

**Spec reference:** [`docs/superpowers/specs/2026-05-22-ddd-domain-rework-design.md`](../specs/2026-05-22-ddd-domain-rework-design.md) §4, §9.

**Phase 2+3 conventions preserved:** sealed-class AR + private setters + parameterless ctor for EF, factory methods (no public ctors), no-null in domain (`Empty`/`None` + `IsEmpty`), per-aggregate repository (replacing `IRepositoryBase<T>`), EF mapping via value converters for the new VOs, `PendingModelChangesWarning` suppression in `AppDbContext.OnConfiguring` (Phase 2 workaround stays).

**No DB schema migration.** The `Instruments` table columns (`Ticker`, `Name`, `Currency`, `Exchange`, `TimezoneId`, `Kind`, `AddedAt`) are unchanged — only the projection into the domain type changes. The unique `Ticker` index already exists.

---

## Pre-work: Worktree setup

- [ ] **Step 0.1: Create an isolated worktree**

Per the user's memory note (prefers isolated worktrees for multi-commit work), do not work on `main`. Use the harness `EnterWorktree` tool, OR:

```bash
git worktree add .claude/worktrees/ddd-phase4 -b worktree-ddd-phase4
cd .claude/worktrees/ddd-phase4
```

All subsequent paths are relative to the worktree root.

- [ ] **Step 0.2: Verify baseline green**

```bash
dotnet tool restore
dotnet build TradyStrat.slnx
dotnet test TradyStrat.slnx --no-build
```

Expected: 413 / 413 tests pass. If failing on `main`, stop and surface before proceeding.

---

# Phase 4 — Instrument aggregate

Goal: `Instrument` is the AR for `(Ticker)` reference data. Aggregate structure (final, per spec §9):

```
Instrument (AR — small reference-data root)
├── InstrumentId Id
├── Ticker Ticker
├── string Name
├── Currency Currency
├── Exchange Exchange                    ← new VO; today free-string column
├── TimezoneId Timezone                  ← new VO; today free-string column
├── InstrumentKind Kind
└── DateTime AddedAt
```

`InstrumentMetadata` (today a near-duplicate DTO used as the probe result type) is **absorbed into `Instrument`** and deleted. `ProbeInstrumentUseCase` returns a probed (id-zero) `Instrument`; `AddInstrumentUseCase` calls `instrument.Confirm(clock)` and persists.

---

## Task 1: `Exchange` VO

**Files:**
- Create: `TradyStrat.Domain/Shared/Exchange.cs`
- Test:   `TradyStrat.Domain.Tests/Shared/ExchangeTests.cs`

`Exchange` is the Yahoo `fullExchangeName` string (e.g. `"LSE"`, `"NASDAQ"`, `"NYSEArca"`). The VO trims and validates non-empty; it does NOT enumerate known exchanges (the universe is unbounded — new ETPs introduce new exchanges).

- [ ] **Step 1.1: Write failing tests**

`TradyStrat.Domain.Tests/Shared/ExchangeTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class ExchangeTests
{
    [Fact]
    public void Of_trims_and_preserves_casing()
    {
        Exchange.Of("  LSE  ").Code.ShouldBe("LSE");
        Exchange.Of("NYSEArca").Code.ShouldBe("NYSEArca");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Of_rejects_empty(string? value)
    {
        Should.Throw<ArgumentException>(() => Exchange.Of(value!));
    }

    [Fact]
    public void Equality_is_structural_and_case_sensitive()
    {
        // Yahoo returns mixed-case exchange names ("NYSEArca" not "NYSEARCA").
        // We preserve the wire casing rather than normalize — uppercase here
        // would diverge from the round-trip JSON the snapshot service relies on.
        Exchange.Of("LSE").ShouldBe(Exchange.Of("LSE"));
        Exchange.Of("LSE").ShouldNotBe(Exchange.Of("lse"));
    }
}
```

- [ ] **Step 1.2: Run failing**

```bash
dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~ExchangeTests"
```

Expected: build error.

- [ ] **Step 1.3: Implement `Exchange`**

`TradyStrat.Domain/Shared/Exchange.cs`:

```csharp
namespace TradyStrat.Domain.Shared;

public readonly record struct Exchange
{
    public string Code { get; }

    private Exchange(string code) => Code = code;

    public static Exchange Of(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Exchange code must not be empty.", nameof(code));
        return new Exchange(code.Trim());
    }

    public override string ToString() => Code;
}
```

- [ ] **Step 1.4: Run tests passing**

```bash
dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~ExchangeTests"
```

Expected: 5 pass.

- [ ] **Step 1.5: Commit**

```bash
git add TradyStrat.Domain/Shared/Exchange.cs TradyStrat.Domain.Tests/Shared/ExchangeTests.cs
git commit -m "feat(domain): Exchange VO — Phase 4"
```

---

## Task 2: `TimezoneId` VO

**Files:**
- Create: `TradyStrat.Domain/Shared/TimezoneId.cs`
- Test:   `TradyStrat.Domain.Tests/Shared/TimezoneIdTests.cs`

`TimezoneId` validates an IANA timezone id by attempting `TimeZoneInfo.FindSystemTimeZoneById`. The validation must be cross-platform; .NET 10 honors IANA names on every OS via ICU. The current `Instrument.TimezoneId` is consumed by `IClock.TodayInExchangeTzFor(string)` which already calls `TimeZoneInfo.FindSystemTimeZoneById` — moving the validation up to construction surfaces bad data at probe time instead of at first dashboard render.

- [ ] **Step 2.1: Write failing tests**

`TradyStrat.Domain.Tests/Shared/TimezoneIdTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class TimezoneIdTests
{
    [Theory]
    [InlineData("Europe/London")]
    [InlineData("America/New_York")]
    [InlineData("Asia/Tokyo")]
    [InlineData("UTC")]
    public void Of_accepts_known_iana_ids(string id)
    {
        TimezoneId.Of(id).Value.ShouldBe(id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Of_rejects_empty(string? value)
    {
        Should.Throw<ArgumentException>(() => TimezoneId.Of(value!));
    }

    [Theory]
    [InlineData("NotARealZone")]
    [InlineData("Europe/Atlantis")]
    public void Of_rejects_unknown_iana_ids(string id)
    {
        Should.Throw<ArgumentException>(() => TimezoneId.Of(id));
    }
}
```

- [ ] **Step 2.2: Run failing**

```bash
dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~TimezoneIdTests"
```

Expected: build error.

- [ ] **Step 2.3: Implement `TimezoneId`**

`TradyStrat.Domain/Shared/TimezoneId.cs`:

```csharp
namespace TradyStrat.Domain.Shared;

public readonly record struct TimezoneId
{
    public string Value { get; }

    private TimezoneId(string value) => Value = value;

    public static TimezoneId Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("TimezoneId must not be empty.", nameof(value));

        var trimmed = value.Trim();
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(trimmed);
        }
        catch (TimeZoneNotFoundException ex)
        {
            throw new ArgumentException(
                $"'{trimmed}' is not a known IANA timezone id.", nameof(value), ex);
        }

        return new TimezoneId(trimmed);
    }

    public override string ToString() => Value;
}
```

- [ ] **Step 2.4: Run tests passing**

```bash
dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~TimezoneIdTests"
```

Expected: 9 pass (4 + 3 + 2).

- [ ] **Step 2.5: Commit**

```bash
git add TradyStrat.Domain/Shared/TimezoneId.cs TradyStrat.Domain.Tests/Shared/TimezoneIdTests.cs
git commit -m "feat(domain): TimezoneId VO with IANA validation — Phase 4"
```

---

## Task 3: Move `Instrument` to `Domain/Instruments/`-namespace AR shape

**Files:**
- Modify: `TradyStrat.Domain/Instruments/Instrument.cs`
- Create: `TradyStrat.Domain.Tests/Instruments/InstrumentTests.cs`
- Delete: `TradyStrat.Domain/Instruments/InstrumentMetadata.cs` (after Task 5)

The `Instrument` namespace declaration stays at the top-level `TradyStrat.Domain` (mirrors Phase 3's decision to keep `Suggestion` under `TradyStrat.Domain.Suggestions` while small reference-data types like `PriceBar`, `FxRate`, `GoalConfig` live at the root). Keeping `Instrument` at `TradyStrat.Domain` matches its existing location and minimizes import churn across the codebase — there's no name conflict.

- [ ] **Step 3.1: Read the existing `Instrument.cs`**

```bash
cat TradyStrat.Domain/Instruments/Instrument.cs
```

Inventory the public fields/getters that consumers use: `Id` (int), `Ticker` (string), `Name` (string), `Currency` (string), `Exchange` (string), `TimezoneId` (string), `Kind` (InstrumentKind), `AddedAt` (DateTime).

The new shape changes `Id` from `int` to `InstrumentId`, `Currency` from `string` to `Currency` (VO), `Exchange` from `string` to `Exchange` (VO), `TimezoneId` from `string` to `TimezoneId` (VO). `Ticker` stays a `string` for now to keep the EF column shape unchanged (the `Ticker` VO is wider-scope and will be adopted in Phase 5 alongside `PriceBar.Ticker`).

- [ ] **Step 3.2: Write failing tests**

`TradyStrat.Domain.Tests/Instruments/InstrumentTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Instruments;

public class InstrumentTests
{
    private static readonly DateTime _now = new(2026, 5, 22, 12, 0, 0, DateTimeKind.Utc);

    private static Instrument ProbedSample() => Instrument.Probed(
        ticker:     "CON3.L",
        name:       "WisdomTree Coinbase 3x Daily Long",
        currency:   Currency.Eur,
        exchange:   Exchange.Of("LSE"),
        timezoneId: TimezoneId.Of("Europe/London"),
        kind:       InstrumentKind.Held);

    [Fact]
    public void Probed_assigns_zero_id_sentinel()
    {
        ProbedSample().Id.ShouldBe(InstrumentId.New());
    }

    [Fact]
    public void Probed_AddedAt_is_default_until_Confirm()
    {
        ProbedSample().AddedAt.ShouldBe(default);
    }

    [Fact]
    public void Confirm_sets_AddedAt_from_clock()
    {
        var inst = ProbedSample();
        inst.Confirm(StubClock.At(_now));
        inst.AddedAt.ShouldBe(_now);
    }

    [Fact]
    public void Confirm_throws_when_already_confirmed()
    {
        var inst = ProbedSample();
        inst.Confirm(StubClock.At(_now));
        Should.Throw<InvalidOperationException>(() => inst.Confirm(StubClock.At(_now.AddDays(1))));
    }

    [Fact]
    public void Rename_validates_non_empty()
    {
        var inst = ProbedSample();
        Should.Throw<ArgumentException>(() => inst.Rename(""));
        Should.Throw<ArgumentException>(() => inst.Rename("   "));
    }

    [Fact]
    public void Rename_trims_and_assigns()
    {
        var inst = ProbedSample();
        inst.Rename("  New Name  ");
        inst.Name.ShouldBe("New Name");
    }

    [Fact]
    public void Existing_rehydrates_persisted_state()
    {
        var inst = Instrument.Existing(
            id:         new InstrumentId(42),
            ticker:     "CON3.L",
            name:       "WisdomTree Coinbase 3x Daily Long",
            currency:   Currency.Eur,
            exchange:   Exchange.Of("LSE"),
            timezoneId: TimezoneId.Of("Europe/London"),
            kind:       InstrumentKind.Held,
            addedAt:    _now);

        inst.Id.ShouldBe(new InstrumentId(42));
        inst.AddedAt.ShouldBe(_now);
    }

    private sealed class StubClock(DateTime now) : IClock
    {
        public DateTime UtcNow() => now;
        public DateOnly TodayLocal() => DateOnly.FromDateTime(now);
        public DateOnly TodayInExchangeTzFor(string ticker) => DateOnly.FromDateTime(now);
        public static StubClock At(DateTime t) => new(t);
    }
}
```

- [ ] **Step 3.3: Run failing**

```bash
dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~InstrumentTests"
```

Expected: build error.

- [ ] **Step 3.4: Rewrite `Instrument.cs`**

```csharp
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain;

public sealed class Instrument
{
    public InstrumentId   Id         { get; private set; }
    public string         Ticker     { get; private set; } = "";
    public string         Name       { get; private set; } = "";
    public Currency       Currency   { get; private set; } = Currency.Eur;
    public Exchange       Exchange   { get; private set; } = Exchange.Of("UNKNOWN");
    public TimezoneId     Timezone   { get; private set; } = TimezoneId.Of("UTC");
    public InstrumentKind Kind       { get; private set; }
    public DateTime       AddedAt    { get; private set; }

    private Instrument() { }   // EF

    private Instrument(
        InstrumentId id, string ticker, string name,
        Currency currency, Exchange exchange, TimezoneId timezone,
        InstrumentKind kind, DateTime addedAt)
    {
        Id        = id;
        Ticker    = ticker;
        Name      = name;
        Currency  = currency;
        Exchange  = exchange;
        Timezone  = timezone;
        Kind      = kind;
        AddedAt   = addedAt;
    }

    /// <summary>
    /// Candidate instrument returned from a probe (e.g. Yahoo metadata fetch)
    /// but not yet persisted. Sets Id to the zero sentinel; AddedAt is the
    /// default DateTime until Confirm(clock) runs.
    /// </summary>
    public static Instrument Probed(
        string ticker, string name,
        Currency currency, Exchange exchange, TimezoneId timezoneId,
        InstrumentKind kind)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            throw new ArgumentException("Ticker is required.", nameof(ticker));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        return new Instrument(
            id:       InstrumentId.New(),
            ticker:   ticker.Trim().ToUpperInvariant(),
            name:     name.Trim(),
            currency: currency,
            exchange: exchange,
            timezone: timezoneId,
            kind:     kind,
            addedAt:  default);
    }

    /// <summary>
    /// Rehydration factory — used by EF-side mapping to recreate the AR from
    /// persisted state. Skips Confirm because AddedAt is already known.
    /// </summary>
    public static Instrument Existing(
        InstrumentId id, string ticker, string name,
        Currency currency, Exchange exchange, TimezoneId timezoneId,
        InstrumentKind kind, DateTime addedAt)
        => new(id, ticker, name, currency, exchange, timezoneId, kind, addedAt);

    /// <summary>
    /// Promote a Probed instrument to persistable by stamping AddedAt from the
    /// clock. Throws if already confirmed (AddedAt != default).
    /// </summary>
    public void Confirm(IClock clock)
    {
        if (AddedAt != default)
            throw new InvalidOperationException(
                $"Instrument '{Ticker}' is already confirmed (AddedAt = {AddedAt:O}).");
        AddedAt = clock.UtcNow();
    }

    /// <summary>
    /// Renames the instrument. No UI surfaces this today but the lifecycle is
    /// explicit so future write-paths don't reach for an init-style setter.
    /// </summary>
    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name must not be empty.", nameof(newName));
        Name = newName.Trim();
    }
}
```

- [ ] **Step 3.5: Run tests passing**

```bash
dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~InstrumentTests"
```

Expected: 7 pass.

**At this point the rest of the solution will fail to build** because every consumer references the old anemic-record shape (`new Instrument { Id = 0, Ticker = "...", ... }`, init-style). That's expected; Tasks 5-12 progressively re-green them. Only `TradyStrat.Domain` + `TradyStrat.Domain.Tests` need to build cleanly at this checkpoint.

- [ ] **Step 3.6: Verify Domain builds**

```bash
dotnet build TradyStrat.Domain
dotnet build TradyStrat.Domain.Tests
```

Both must succeed.

- [ ] **Step 3.7: Commit**

```bash
git add TradyStrat.Domain/Instruments/Instrument.cs TradyStrat.Domain.Tests/Instruments/InstrumentTests.cs
git commit -m "feat(domain): Instrument becomes AR with VO fields + Probed/Existing factories + Confirm/Rename — Phase 4"
```

---

## Task 4: `IInstrumentRepository` port

**Files:**
- Create: `TradyStrat.Application/Settings/IInstrumentRepository.cs`

Spec §9 repository interface.

- [ ] **Step 4.1: Create the port**

`TradyStrat.Application/Settings/IInstrumentRepository.cs`:

```csharp
using TradyStrat.Domain;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Application.Settings;

/// <summary>
/// Per-aggregate repository for Instrument. AddAsync enforces system-wide
/// ticker uniqueness at the write path (the aggregate itself can't check
/// uniqueness — that spans aggregates).
/// </summary>
public interface IInstrumentRepository
{
    Task<Instrument?> GetAsync(InstrumentId id, CancellationToken ct);

    /// <summary>Returns null when no instrument matches the given ticker.</summary>
    Task<Instrument?> FindByTickerAsync(string ticker, CancellationToken ct);

    Task<IReadOnlyList<Instrument>> ListAsync(CancellationToken ct);

    /// <summary>Throws DuplicateInstrumentException when Ticker already exists.</summary>
    Task AddAsync(Instrument instrument, CancellationToken ct);
}
```

- [ ] **Step 4.2: Build**

```bash
dotnet build TradyStrat.Application
```

Expected: succeeds.

- [ ] **Step 4.3: Commit**

```bash
git add TradyStrat.Application/Settings/IInstrumentRepository.cs
git commit -m "feat(application): IInstrumentRepository port — Phase 4"
```

---

## Task 5: Delete `InstrumentMetadata` and widen `IPriceFeed` to return `Instrument`

**Files:**
- Delete: `TradyStrat.Domain/Instruments/InstrumentMetadata.cs`
- Modify: `TradyStrat.Application/PriceFeed/Providers/IPriceFeed.cs`
- Modify: `TradyStrat.Infrastructure/PriceFeed/Providers/YahooParser.cs`
- Modify: `TradyStrat.Infrastructure/PriceFeed/Providers/YahooPriceFeed.cs` (if present — verify the file owns `GetInstrumentMetadataAsync`)
- Modify: `TradyStrat.TestKit/StubPriceFeed.cs`

`InstrumentMetadata` was a near-clone of `Instrument`. Replace its single consumer surface (`IPriceFeed.GetInstrumentMetadataAsync`) with a probe-returning method.

- [ ] **Step 5.1: Inspect `IPriceFeed` implementations**

```bash
grep -rln 'GetInstrumentMetadataAsync\|InstrumentMetadata' --include='*.cs' . \
  | grep -v '/bin/' | grep -v '/obj/' | grep -v '/worktrees/'
```

Expect three call-site categories: the interface declaration, the Yahoo/Stub implementations, and `ProbeInstrumentUseCase`.

- [ ] **Step 5.2: Rewrite `IPriceFeed`**

```csharp
using TradyStrat.Domain;

namespace TradyStrat.Application.PriceFeed.Providers;

public interface IPriceFeed
{
    Task<IReadOnlyList<PriceBar>> FetchDailyAsync(
        string ticker, DateOnly from, DateOnly to, CancellationToken ct);

    /// <summary>
    /// Probes the upstream provider (Yahoo) for instrument metadata. Returns a
    /// Probed Instrument with Id = InstrumentId.New() (zero sentinel) and
    /// AddedAt = default — the caller invokes Confirm(clock) before persisting.
    /// </summary>
    Task<Instrument> ProbeAsync(string ticker, CancellationToken ct);
}
```

- [ ] **Step 5.3: Rewrite `YahooParser.ParseMetadata`**

In `TradyStrat.Infrastructure/PriceFeed/Providers/YahooParser.cs`, change the signature + return path:

```csharp
public static Instrument ParseMetadata(string ticker, JsonDocument doc)
{
    try
    {
        var root = doc.RootElement.GetProperty("quoteResponse");
        if (root.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.Object)
            throw new PriceFeedUnavailableException(
                $"Yahoo error for {ticker}: {err.GetRawText()}");

        var result = root.GetProperty("result");
        if (result.ValueKind != JsonValueKind.Array || result.GetArrayLength() == 0)
            throw new InstrumentNotFoundException(
                $"Yahoo returned no quote for '{ticker}'.");

        var first = result[0];
        var name = ReadString(first, "longName")
                ?? ReadString(first, "shortName")
                ?? throw new InstrumentMetadataIncompleteException(
                       $"Yahoo response for '{ticker}' has no longName or shortName.");

        var currencyCode = ReadString(first, "currency")
                ?? throw new InstrumentMetadataIncompleteException(
                       $"Yahoo response for '{ticker}' has no currency.");

        var exchangeCode = ReadString(first, "fullExchangeName")
                ?? throw new InstrumentMetadataIncompleteException(
                       $"Yahoo response for '{ticker}' has no fullExchangeName.");

        var tzId = ReadString(first, "exchangeTimezoneName")
                ?? throw new InstrumentMetadataIncompleteException(
                       $"Yahoo response for '{ticker}' has no exchangeTimezoneName.");

        return Instrument.Probed(
            ticker:     ticker,
            name:       name,
            currency:   Currency.Parse(currencyCode),
            exchange:   Exchange.Of(exchangeCode),
            timezoneId: TimezoneId.Of(tzId),
            kind:       InstrumentKind.Held); // caller (AddInstrumentUseCase) overrides if needed
    }
    catch (TradyStratException) { throw; }
    catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException
                                or JsonException)
    {
        throw new PriceFeedUnavailableException(
            $"Failed to parse Yahoo metadata payload for {ticker}", ex);
    }
}
```

Add `using TradyStrat.Domain;` and `using TradyStrat.Domain.Shared;` at the top of `YahooParser.cs` if not present.

**Note on default Kind:** the probe returns `InstrumentKind.Held` by default; `AddInstrumentUseCase` re-stamps it via a new factory call before persisting (Task 6). The probe Kind is never persisted directly.

- [ ] **Step 5.4: Find + rewrite the `IPriceFeed` implementation**

```bash
grep -rln 'class YahooPriceFeed\|: IPriceFeed' --include='*.cs' TradyStrat.Infrastructure | head
```

In that file's `GetInstrumentMetadataAsync` method (rename to `ProbeAsync`), update the return type accordingly. The method body already calls `YahooParser.ParseMetadata(...)` — that now returns `Instrument`.

- [ ] **Step 5.5: Rewrite `StubPriceFeed`**

`TradyStrat.TestKit/StubPriceFeed.cs` — update the metadata method:

```csharp
public Task<Instrument> ProbeAsync(string ticker, CancellationToken ct)
    => Task.FromResult(Instrument.Probed(
        ticker:     ticker,
        name:       $"Stub {ticker}",
        currency:   Currency.Eur,
        exchange:   Exchange.Of("STUB"),
        timezoneId: TimezoneId.Of("UTC"),
        kind:       InstrumentKind.Held));
```

Adjust namespaces (`using TradyStrat.Domain;`, `using TradyStrat.Domain.Shared;`) and delete the old `GetInstrumentMetadataAsync` override.

- [ ] **Step 5.6: Delete `InstrumentMetadata.cs`**

```bash
git rm TradyStrat.Domain/Instruments/InstrumentMetadata.cs
```

- [ ] **Step 5.7: Build + commit**

```bash
dotnet build TradyStrat.Domain TradyStrat.Application TradyStrat.Infrastructure
```

Expected: `TradyStrat.Domain` succeeds; Application + Infrastructure may still fail on remaining consumers (use cases, EF config, MCP tool, CLI). That's expected — Tasks 6-12 mop up.

```bash
git add -A
git commit -m "refactor: delete InstrumentMetadata DTO; IPriceFeed.ProbeAsync returns Instrument — Phase 4"
```

---

## Task 6: Rewrite `ProbeInstrumentUseCase`

**Files:**
- Modify: `TradyStrat.Application/Settings/UseCases/ProbeInstrumentUseCase.cs`

`ProbeInstrumentUseCase` becomes a thin pass-through plus the FX sanity check it already performs. The input grows a `Kind` field so the probe carries through what the caller intended to register the instrument as (the Yahoo response doesn't tell us Held vs Watchlist).

- [ ] **Step 6.1: Inspect existing input**

```bash
cat TradyStrat.Application/Settings/UseCases/ProbeInstrumentUseCase.cs
```

Note the existing `ProbeInstrumentInput(string Ticker)`.

- [ ] **Step 6.2: Rewrite the input + use case**

```csharp
using TradyStrat.Application.Fx.Providers;
using TradyStrat.Application.PriceFeed.Providers;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Application.Settings.UseCases;

public sealed record ProbeInstrumentInput(string Ticker, InstrumentKind Kind);

public sealed class ProbeInstrumentUseCase(
    IPriceFeed priceFeed,
    IFxRateProvider fx,
    ILogger<ProbeInstrumentUseCase> log)
    : UseCaseBase<ProbeInstrumentInput, Instrument>(log)
{
    protected override async Task<Instrument> ExecuteCore(
        ProbeInstrumentInput input, CancellationToken ct)
    {
        var ticker = (input.Ticker ?? "").Trim().ToUpperInvariant();
        if (ticker.Length == 0)
            throw new InstrumentNotFoundException("Ticker must not be empty.");

        var probed = await priceFeed.ProbeAsync(ticker, ct);

        // FX-pair sanity check — surface unsupported currencies before commit.
        if (probed.Currency != TradyStrat.Domain.Shared.Currency.Eur)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            try
            {
                _ = await fx.FetchAsync(
                    "EUR", probed.Currency.Code, today.AddDays(-1), today, ct);
            }
            catch (FxRateUnavailableException ex)
            {
                throw new UnsupportedCurrencyException(
                    $"EUR/{probed.Currency.Code} FX rate is not available from Yahoo.", ex);
            }
        }

        // The probe defaulted Kind to Held; re-stamp from the input. The Confirm
        // call happens later in AddInstrumentUseCase.
        if (probed.Kind != input.Kind)
            probed = Instrument.Probed(
                ticker:     probed.Ticker,
                name:       probed.Name,
                currency:   probed.Currency,
                exchange:   probed.Exchange,
                timezoneId: probed.Timezone,
                kind:       input.Kind);

        return probed;
    }
}
```

- [ ] **Step 6.3: Update the consumer (Settings page or wherever `ProbeInstrumentInput` is constructed)**

```bash
grep -rln 'new ProbeInstrumentInput\b' --include='*.cs' --include='*.razor*' . \
  | grep -v '/bin/' | grep -v '/obj/' | grep -v '/worktrees/'
```

For each match (typically `SettingsPage.razor.cs` or similar UI code-behind), pass the chosen `InstrumentKind` (the user already chose Held vs Watchlist before triggering the probe). The current UI tracks this as a local enum/checkbox — wire that through.

- [ ] **Step 6.4: Build + commit**

```bash
dotnet build TradyStrat.Application
git add -A
git commit -m "refactor(application): ProbeInstrumentUseCase returns Instrument; input carries Kind — Phase 4"
```

---

## Task 7: Rewrite `AddInstrumentUseCase`

**Files:**
- Modify: `TradyStrat.Infrastructure/Settings/UseCases/AddInstrumentUseCase.cs`

`AddInstrumentUseCase` lives in Infrastructure today because of its price-cache + fx-cache warm calls. It now receives a probed `Instrument` (already validated, including IANA timezone), calls `Confirm(clock)` to stamp `AddedAt`, then delegates to `IInstrumentRepository.AddAsync` (which throws `DuplicateInstrumentException` if `Ticker` already exists).

- [ ] **Step 7.1: Rewrite the use case**

```csharp
using TradyStrat.Application.Settings;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Fx;
using TradyStrat.Infrastructure.PriceFeed;

namespace TradyStrat.Infrastructure.Settings.UseCases;

public sealed record AddInstrumentInput(Instrument Probed);

public sealed partial class AddInstrumentUseCase(
    IInstrumentRepository repo,
    DailyPriceCache priceCache,
    DailyFxCache fxCache,
    IClock clock,
    ILogger<AddInstrumentUseCase> log)
    : UseCaseBase<AddInstrumentInput, Instrument>(log)
{
    protected override async Task<Instrument> ExecuteCore(
        AddInstrumentInput input, CancellationToken ct)
    {
        var instrument = input.Probed;
        instrument.Confirm(clock);

        // Repository enforces ticker uniqueness — throws DuplicateInstrumentException.
        await repo.AddAsync(instrument, ct);

        // Best-effort warm. Failures are logged and swallowed — cache self-heals next startup.
        try { await priceCache.EnsureFreshAsync(instrument.Ticker, ct); }
        catch (Exception ex) { LogPriceWarmFailed(log, ex, instrument.Ticker); }

        // FX-warm — skip for EUR-denominated instruments.
        if (instrument.Currency.Code != "EUR")
        {
            try { await fxCache.EnsureFreshAsync("EUR", instrument.Currency.Code, ct); }
            catch (Exception ex) { LogFxWarmFailed(log, ex, instrument.Currency.Code); }
        }

        return instrument;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Price-cache warm failed for {Ticker}")]
    private static partial void LogPriceWarmFailed(ILogger logger, Exception ex, string ticker);

    [LoggerMessage(Level = LogLevel.Warning, Message = "FX-cache warm failed for currency {Currency}")]
    private static partial void LogFxWarmFailed(ILogger logger, Exception ex, string currency);
}
```

Read the existing file before writing — there may be `LoggerMessage` partial-method declarations already present. Preserve them if so.

- [ ] **Step 7.2: Update the call site**

```bash
grep -rln 'new AddInstrumentInput\b' --include='*.cs' --include='*.razor*' . \
  | grep -v '/bin/' | grep -v '/obj/' | grep -v '/worktrees/'
```

For each match (typically the Settings page handler), construct `AddInstrumentInput(probedInstrument)` directly from the probe-use-case result. The `Kind` is already baked into the probed instrument.

- [ ] **Step 7.3: Build + commit**

```bash
dotnet build TradyStrat.Infrastructure
git add -A
git commit -m "refactor(infrastructure): AddInstrumentUseCase consumes probed Instrument + IInstrumentRepository — Phase 4"
```

---

## Task 8: Update `InstrumentConfiguration` for the new VOs

**Files:**
- Modify: `TradyStrat.Infrastructure/Data/Configurations/InstrumentConfiguration.cs`

The DB columns are unchanged (string Currency, string Exchange, string TimezoneId). EF needs value converters for `Currency`, `Exchange`, `TimezoneId` so the round-trip works. The `Id` property is now `InstrumentId`, but the existing `StronglyTypedIdConventions.ApplyTo` already covers it — no per-property converter needed.

- [ ] **Step 8.1: Rewrite the config**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Domain;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Infrastructure.Data.Configurations;

public sealed class InstrumentConfiguration : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> builder)
    {
        builder.ToTable("Instruments");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();
        builder.Property(i => i.Ticker).HasMaxLength(16).IsRequired();
        builder.Property(i => i.Name).HasMaxLength(200).IsRequired();

        builder.Property(i => i.Currency)
               .HasConversion(c => c.Code, s => Currency.Parse(s))
               .HasMaxLength(3).IsRequired();

        builder.Property(i => i.Exchange)
               .HasConversion(e => e.Code, s => Exchange.Of(s))
               .HasMaxLength(64).IsRequired();

        builder.Property(i => i.Timezone)
               .HasColumnName("TimezoneId")           // preserve existing column name
               .HasConversion(t => t.Value, s => TimezoneId.Of(s))
               .HasMaxLength(64).IsRequired();

        builder.Property(i => i.Kind).HasConversion<int>();
        builder.Property(i => i.AddedAt);
        builder.HasIndex(i => i.Ticker).IsUnique();
    }
}
```

The `Timezone` property on the new AR maps to the existing `TimezoneId` column via `HasColumnName("TimezoneId")` — no migration needed.

- [ ] **Step 8.2: Build Infrastructure**

```bash
dotnet build TradyStrat.Infrastructure
```

Expected: succeeds (Domain + Application already green at this point).

- [ ] **Step 8.3: Commit**

```bash
git add TradyStrat.Infrastructure/Data/Configurations/InstrumentConfiguration.cs
git commit -m "feat(infra): InstrumentConfiguration with Currency/Exchange/TimezoneId VO converters — Phase 4"
```

---

## Task 9: `EfInstrumentRepository`

**Files:**
- Create: `TradyStrat.Infrastructure/Settings/EfInstrumentRepository.cs`
- Modify: `TradyStrat.Infrastructure/Settings/SettingsInfrastructureModule.cs`

- [ ] **Step 9.1: Implement the repo**

```csharp
using Microsoft.EntityFrameworkCore;
using TradyStrat.Application.Settings;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Shared;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.Infrastructure.Settings;

public sealed class EfInstrumentRepository(AppDbContext db) : IInstrumentRepository
{
    public Task<Instrument?> GetAsync(InstrumentId id, CancellationToken ct)
        => db.Instruments.SingleOrDefaultAsync(i => i.Id == id, ct);

    public Task<Instrument?> FindByTickerAsync(string ticker, CancellationToken ct)
    {
        var normalized = (ticker ?? "").Trim().ToUpperInvariant();
        return db.Instruments.SingleOrDefaultAsync(i => i.Ticker == normalized, ct);
    }

    public async Task<IReadOnlyList<Instrument>> ListAsync(CancellationToken ct)
        => await db.Instruments.OrderBy(i => i.Ticker).ToListAsync(ct);

    public async Task AddAsync(Instrument instrument, CancellationToken ct)
    {
        var dup = await FindByTickerAsync(instrument.Ticker, ct);
        if (dup is not null)
            throw new DuplicateInstrumentException(
                $"Instrument '{instrument.Ticker}' is already tracked.");

        db.Instruments.Add(instrument);
        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 9.2: Register in the module**

Read `TradyStrat.Infrastructure/Settings/SettingsInfrastructureModule.cs` first to confirm the existing pattern. Add the registration:

```csharp
services.AddScoped<IInstrumentRepository, EfInstrumentRepository>();
```

If the module also registers `AddInstrumentUseCase` (it does — `services.AddScoped<AddInstrumentUseCase>();`), keep that line; the repo registration goes alongside.

- [ ] **Step 9.3: Build + commit**

```bash
dotnet build TradyStrat.Infrastructure
git add TradyStrat.Infrastructure/Settings/EfInstrumentRepository.cs \
        TradyStrat.Infrastructure/Settings/SettingsInfrastructureModule.cs
git commit -m "feat(infra): EfInstrumentRepository + DI registration — Phase 4"
```

---

## Task 10: Swap remaining `IReadRepositoryBase<Instrument>` consumers

The following files still inject `IReadRepositoryBase<Instrument>` and call `repo.ListAsync(new AllInstrumentsSpec(), ...)` or `repo.FirstOrDefaultAsync(new InstrumentByTickerSpec(...))`. Each rewrites to use `IInstrumentRepository`.

**Files to update:**

1. `TradyStrat.Application/Settings/UseCases/ListInstrumentsUseCase.cs`
2. `TradyStrat.Application/Settings/UseCases/UpdateSettingUseCase.cs`
3. `TradyStrat.Application/Trades/UseCases/LogTradeUseCase.cs`
4. `TradyStrat.Application/Trades/UseCases/ImportTradesCsvUseCase.cs`
5. `TradyStrat.Application/AiSuggestion/ForwardReturnCalculator.cs`
6. `TradyStrat.Application/AiSuggestion/Backfill/SuggestionBackfillCoordinator.cs`
7. `TradyStrat.Application/AiSuggestion/UseCases/GetTodaysSuggestionUseCase.cs`
8. `TradyStrat.Application/AiSuggestion/UseCases/ForceRefetchSuggestionUseCase.cs`
9. `TradyStrat.Application/AiSuggestion/UseCases/ReplaySuggestionsUseCase.cs`
10. `TradyStrat.Cli/Mcp/Tools/PortfolioTool.cs`
11. `TradyStrat.Cli/Commands/ReplayCommand.cs`

For each: swap the injected port, swap the call.

- [ ] **Step 10.1: ListInstrumentsUseCase**

```csharp
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;

namespace TradyStrat.Application.Settings.UseCases;

public sealed class ListInstrumentsUseCase(
    IInstrumentRepository repo,
    ILogger<ListInstrumentsUseCase> log)
    : UseCaseBase<Unit, IReadOnlyList<Instrument>>(log)
{
    protected override Task<IReadOnlyList<Instrument>> ExecuteCore(
        Unit input, CancellationToken ct)
        => repo.ListAsync(ct);
}
```

- [ ] **Step 10.2: UpdateSettingUseCase**

Read the file first, then replace the existing `instruments.AnyAsync(new InstrumentByTickerSpec(ticker), ct)` call with:

```csharp
var known = await instruments.FindByTickerAsync(ticker, ct) is not null;
```

Change the constructor parameter type from `IReadRepositoryBase<Instrument>` to `IInstrumentRepository`. Drop the `using TradyStrat.Application.Settings.Specifications;` import.

- [ ] **Step 10.3: LogTradeUseCase + ImportTradesCsvUseCase**

Both call `instruments.FirstOrDefaultAsync(new InstrumentByTickerSpec(ticker), ct)`. Swap to:

```csharp
var instrument = await instruments.FindByTickerAsync(ticker, ct);
```

Update constructor parameter types accordingly. Drop the spec import.

- [ ] **Step 10.4: ForwardReturnCalculator**

Today calls `instrumentRepo.GetByIdAsync(suggestion.InstrumentId.Value, ct)`. Swap to:

```csharp
var instrument = await instrumentRepo.GetAsync(suggestion.InstrumentId, ct);
```

The port's `GetAsync(InstrumentId, ct)` takes the strongly-typed id — no `.Value` extraction.

- [ ] **Step 10.5: SuggestionBackfillCoordinator**

Inside the `Resolved` record + constructor:

```csharp
private sealed record Resolved(
    ISuggestionRepository Suggestions,
    IInstrumentRepository Instruments,
    BackfillSuggestionsUseCase Backfill,
    ISettingsReader Settings,
    IDisposable? Scope);

// In the [ActivatorUtilitiesConstructor] body:
sp.GetRequiredService<IInstrumentRepository>(),
```

Replace the `resolved.Instruments.FirstOrDefaultAsync(new InstrumentByTickerSpec(focusTicker), ct)` call with `resolved.Instruments.FindByTickerAsync(focusTicker, ct)`.

- [ ] **Step 10.6: GetTodaysSuggestionUseCase / ForceRefetchSuggestionUseCase / ReplaySuggestionsUseCase**

All three inject `IReadRepositoryBase<Instrument>` to look up an instrument by id. Swap to `IInstrumentRepository`. The `instruments.GetByIdAsync(input.InstrumentId, ct)` → `instruments.GetAsync(new InstrumentId(input.InstrumentId), ct)`.

For `ReplaySuggestionsUseCase` specifically: also drop the `Ardalis.Specification` import if it becomes unused.

- [ ] **Step 10.7: TradyStrat.Cli/Mcp/Tools/PortfolioTool.cs + ReplayCommand.cs**

Same swap. The MCP tool and CLI command both use `IReadRepositoryBase<Instrument>` today. `PortfolioTool` calls `ListAsync(...)`; `ReplayCommand` calls `FirstOrDefaultAsync(new InstrumentByTickerSpec(settings.Instrument), ct)`. Use the new methods.

- [ ] **Step 10.8: Build after each swap**

```bash
dotnet build TradyStrat.slnx
```

The Application + Infrastructure + UI + CLI should all build green by the end of Task 10. The Tests projects may still have references to the old spec types — that's Task 12's cleanup.

- [ ] **Step 10.9: Commit**

```bash
git add -A
git commit -m "refactor: swap all Instrument consumers from IReadRepositoryBase<Instrument> to IInstrumentRepository — Phase 4 Task 10"
```

---

## Task 11: Delete `InstrumentByTickerSpec` + `AllInstrumentsSpec`

**Files:**
- Delete: `TradyStrat.Application/Settings/Specifications/InstrumentByTickerSpec.cs`
- Delete: `TradyStrat.Application/Settings/Specifications/AllInstrumentsSpec.cs`
- Verify: `TradyStrat.Application/Settings/Specifications/` directory is empty after deletion (or contains only unrelated specs)

- [ ] **Step 11.1: Check for remaining consumers**

```bash
grep -rln 'InstrumentByTickerSpec\|AllInstrumentsSpec' --include='*.cs' . \
  | grep -v '/bin/' | grep -v '/obj/' | grep -v '/worktrees/'
```

Expected: only the two files themselves (or their tests). If the spec files have any other reference, fix the consumer first.

- [ ] **Step 11.2: Delete the specs**

```bash
git rm TradyStrat.Application/Settings/Specifications/InstrumentByTickerSpec.cs \
       TradyStrat.Application/Settings/Specifications/AllInstrumentsSpec.cs
```

If `TradyStrat.Application/Settings/Specifications/` is empty after deletion, the empty folder is harmless — leave it (git won't track it). If it's used by other specs (FocusTicker etc.), leave those alone.

- [ ] **Step 11.3: Check the test mirror**

```bash
find TradyStrat.Application.Tests TradyStrat.Infrastructure.Tests \
  -name 'InstrumentByTickerSpecTests.cs' -o -name 'AllInstrumentsSpecTests.cs' \
  -o -name 'InstrumentSpecTests.cs' 2>/dev/null
```

Delete any matching test file (the spec it tests is gone).

- [ ] **Step 11.4: Build + commit**

```bash
dotnet build TradyStrat.slnx
git add -A
git commit -m "chore: delete dead InstrumentByTickerSpec + AllInstrumentsSpec — Phase 4 Task 11"
```

---

## Task 12: Update remaining `Instrument` test fixtures

Any tests that constructed `new Instrument { Id = ..., Ticker = ..., ... }` need to use the new factories.

- [ ] **Step 12.1: Find init-style Instrument constructions**

```bash
grep -rln 'new Instrument\s*{' --include='*.cs' . \
  | grep -v '/bin/' | grep -v '/obj/' | grep -v '/worktrees/'
```

For each match, rewrite as `Instrument.Existing(...)` (when the test seeds a persisted-shape row) or `Instrument.Probed(...)` (when the test is exercising the probe flow). Examples:

```csharp
// Before (init-style):
db.Instruments.Add(new Instrument
{
    Id = 1, Ticker = "TST", Name = "Test", Currency = "EUR",
    Exchange = "X", TimezoneId = "Etc/UTC", Kind = InstrumentKind.Held,
    AddedAt = DateTime.UtcNow,
});

// After (factory):
db.Instruments.Add(Instrument.Existing(
    id:         new InstrumentId(1),
    ticker:     "TST",
    name:       "Test",
    currency:   Currency.Eur,
    exchange:   Exchange.Of("X"),
    timezoneId: TimezoneId.Of("Etc/UTC"),
    kind:       InstrumentKind.Held,
    addedAt:    DateTime.UtcNow));
```

The likely sites are in:
- `TradyStrat.Application.Tests/AiSuggestion/UseCases/QuerySuggestionsUseCaseTests.cs` (helper `SeedInstrument`)
- `TradyStrat.Application.Tests/Dashboard/UseCases/BuildFocusDerivedSliceUseCaseTests.cs` (`SeedInstrument`)
- `TradyStrat.Infrastructure.Tests/Settings/AddInstrumentUseCaseTests.cs` (the canonical example)
- `TradyStrat.Cli.Tests/Mcp/...` (a couple of MCP test fixtures)

- [ ] **Step 12.2: Build + run tests**

```bash
dotnet build TradyStrat.slnx
dotnet test TradyStrat.slnx --no-build
```

Expected: every project builds; tests pass except possibly `AddInstrumentUseCaseTests` (which Task 13 rewrites against the new shape).

- [ ] **Step 12.3: Commit**

```bash
git add -A
git commit -m "test: migrate Instrument test fixtures to factory shape — Phase 4 Task 12"
```

---

## Task 13: Rewrite `AddInstrumentUseCaseTests`

**Files:**
- Modify: `TradyStrat.Infrastructure.Tests/Settings/AddInstrumentUseCaseTests.cs`

The existing tests cover: the happy path, the duplicate-ticker case, the price-warm failure (best-effort), and the FX-warm failure (best-effort). They construct `AddInstrumentInput` with the old `InstrumentMetadata Probe` shape — that's gone. Now `AddInstrumentInput(Instrument Probed)`.

- [ ] **Step 13.1: Read the existing tests**

```bash
cat TradyStrat.Infrastructure.Tests/Settings/AddInstrumentUseCaseTests.cs
```

Note which test names exist; preserve their coverage.

- [ ] **Step 13.2: Rewrite using the new shape**

Replace the existing `InstrumentMetadata` construction with `Instrument.Probed(...)` and pass that into `AddInstrumentInput(probed)`. The duplicate-test assertion should expect `DuplicateInstrumentException` from `IInstrumentRepository.AddAsync` (no behavior change — the throw point moved from the use case body to the repo).

The use case now depends on `IInstrumentRepository`. Inject an in-process `EfInstrumentRepository(db)` (same pattern as Phase 3's repository tests). For the test that exercises a pre-existing duplicate, seed via `db.Instruments.Add(Instrument.Existing(...))` + `await db.SaveChangesAsync(...)` before invoking the use case.

- [ ] **Step 13.3: Run tests**

```bash
dotnet test TradyStrat.Infrastructure.Tests --filter "FullyQualifiedName~AddInstrumentUseCaseTests"
```

Expected: all tests pass.

- [ ] **Step 13.4: Commit**

```bash
git add TradyStrat.Infrastructure.Tests/Settings/AddInstrumentUseCaseTests.cs
git commit -m "test(infra): AddInstrumentUseCaseTests adopts probed-Instrument input — Phase 4 Task 13"
```

---

## Task 14: `EfInstrumentRepositoryTests`

**Files:**
- Create: `TradyStrat.Infrastructure.Tests/Settings/EfInstrumentRepositoryTests.cs`

Round-trip tests verifying the EF mapping (VO converters) + duplicate-detection contract.

- [ ] **Step 14.1: Write tests**

```csharp
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Shared;
using TradyStrat.Infrastructure.Data;
using TradyStrat.Infrastructure.Settings;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Settings;

public class EfInstrumentRepositoryTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly DbContextOptions<AppDbContext> _opts;

    public EfInstrumentRepositoryTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        _opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        using var bootstrap = new AppDbContext(_opts);
        bootstrap.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _conn.Dispose();
        GC.SuppressFinalize(this);
    }

    private AppDbContext NewContext() => new(_opts);

    private static Instrument Probed(string ticker = "TST") => Instrument.Probed(
        ticker:     ticker,
        name:       $"Stub {ticker}",
        currency:   Currency.Eur,
        exchange:   Exchange.Of("LSE"),
        timezoneId: TimezoneId.Of("Europe/London"),
        kind:       InstrumentKind.Held);

    [Fact]
    public async Task Add_round_trips_VO_fields()
    {
        var ct = TestContext.Current.CancellationToken;
        var probed = Probed("ABC");
        // Confirm-then-persist matches AddInstrumentUseCase's flow.
        probed.Confirm(new StubClock(new DateTime(2026, 5, 22, 0, 0, 0, DateTimeKind.Utc)));

        await using (var db = NewContext())
        {
            await new EfInstrumentRepository(db).AddAsync(probed, ct);
        }

        await using var read = NewContext();
        var loaded = await new EfInstrumentRepository(read).FindByTickerAsync("ABC", ct);

        loaded.ShouldNotBeNull();
        loaded.Ticker.ShouldBe("ABC");
        loaded.Currency.Code.ShouldBe("EUR");
        loaded.Exchange.Code.ShouldBe("LSE");
        loaded.Timezone.Value.ShouldBe("Europe/London");
        loaded.AddedAt.ShouldBe(new DateTime(2026, 5, 22, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Add_throws_DuplicateInstrumentException_on_repeat_ticker()
    {
        var ct = TestContext.Current.CancellationToken;
        var first = Probed("DUP");
        first.Confirm(new StubClock(DateTime.UtcNow));

        await using (var db = NewContext())
        {
            await new EfInstrumentRepository(db).AddAsync(first, ct);
        }

        var second = Probed("DUP");
        second.Confirm(new StubClock(DateTime.UtcNow));

        await using var db2 = NewContext();
        await Should.ThrowAsync<DuplicateInstrumentException>(
            () => new EfInstrumentRepository(db2).AddAsync(second, ct));
    }

    [Fact]
    public async Task FindByTickerAsync_normalizes_input_casing()
    {
        var ct = TestContext.Current.CancellationToken;
        var probed = Probed("XYZ");
        probed.Confirm(new StubClock(DateTime.UtcNow));

        await using (var db = NewContext())
        {
            await new EfInstrumentRepository(db).AddAsync(probed, ct);
        }

        await using var read = NewContext();
        var loaded = await new EfInstrumentRepository(read).FindByTickerAsync("  xyz  ", ct);
        loaded.ShouldNotBeNull();
        loaded.Ticker.ShouldBe("XYZ");
    }

    [Fact]
    public async Task ListAsync_orders_by_ticker_alphabetically()
    {
        var ct = TestContext.Current.CancellationToken;
        await using (var db = NewContext())
        {
            var repo = new EfInstrumentRepository(db);
            foreach (var t in new[] { "ZZZ", "AAA", "MMM" })
            {
                var p = Probed(t);
                p.Confirm(new StubClock(DateTime.UtcNow));
                await repo.AddAsync(p, ct);
            }
        }

        await using var read = NewContext();
        var list = await new EfInstrumentRepository(read).ListAsync(ct);
        list.Select(i => i.Ticker).ShouldBe(["AAA", "MMM", "ZZZ"]);
    }

    private sealed class StubClock(DateTime now) : IClock
    {
        public DateTime UtcNow() => now;
        public DateOnly TodayLocal() => DateOnly.FromDateTime(now);
        public DateOnly TodayInExchangeTzFor(string ticker) => DateOnly.FromDateTime(now);
    }
}
```

- [ ] **Step 14.2: Run tests**

```bash
dotnet test TradyStrat.Infrastructure.Tests --filter "FullyQualifiedName~EfInstrumentRepositoryTests"
```

Expected: 4 pass.

- [ ] **Step 14.3: Commit**

```bash
git add TradyStrat.Infrastructure.Tests/Settings/EfInstrumentRepositoryTests.cs
git commit -m "test(infra): EfInstrumentRepository VO-round-trip + duplicate-detection — Phase 4 Task 14"
```

---

## Task 15: Final verification + smoke test

- [ ] **Step 15.1: Full clean build**

```bash
dotnet clean TradyStrat.slnx
dotnet build TradyStrat.slnx
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 15.2: Full test suite**

```bash
dotnet test TradyStrat.slnx --no-build
```

Expected: every test passes. Counts up vs Phase 3 baseline (413) by ~15 new Phase 4 tests.

- [ ] **Step 15.3: Grep forbidden references**

```bash
# The InstrumentMetadata DTO should be entirely gone.
grep -rn 'InstrumentMetadata\b' --include='*.cs' . | grep -v '/bin/' | grep -v '/obj/' | grep -v '/worktrees/'
```

Expected: only hits in `Exceptions/InstrumentMetadataIncompleteException.cs` (the exception keeps the name — it's about parsing the upstream payload, not the deleted DTO).

```bash
# Old per-aggregate ports should be gone from Instrument call sites.
grep -rn 'IReadRepositoryBase<Instrument>\|IRepositoryBase<Instrument>\|InstrumentByTickerSpec\|AllInstrumentsSpec' \
  --include='*.cs' . | grep -v '/bin/' | grep -v '/obj/' | grep -v '/worktrees/'
```

Expected: no hits at all.

- [ ] **Step 15.4: Run the app, hit each page**

```bash
dotnet run --project TradyStrat
```

In another shell (or via the background task pattern from Phase 3 verification):

```bash
curl -sf http://localhost:5180/         | head -5    # dashboard renders
curl -sf http://localhost:5180/trades   | head -5    # trades page renders
curl -sf http://localhost:5180/settings | head -5    # settings renders
```

Each should return HTML, not 500. The Settings page in particular exercises the probe + add flow — if a manual "add instrument" probe succeeds (Yahoo returns metadata, the probed VO validation passes), the new path is exercised end-to-end.

Stop the app with `Ctrl-C`.

- [ ] **Step 15.5: Phase 4 checkpoint tag**

```bash
git tag -a phase4-instrument-ar-done -m "Phase 4 (Instrument aggregate) complete"
```

- [ ] **Step 15.6: Finish branch via the finishing-a-development-branch skill**

The user's collaboration-style preference is to merge isolated worktrees back via a `--no-ff` merge once tests are green. Hand off to the skill:

```
I'm using the finishing-a-development-branch skill to complete this work.
```

The skill will run tests, present the 4-option menu (merge / PR / keep / discard), and clean up the worktree on merge.

---

# Plan summary

**Phase 4 tasks (15):**
- 1-2: kernel VOs (Exchange, TimezoneId)
- 3: Instrument rewritten as AR with Probed/Existing factories + Confirm/Rename
- 4: IInstrumentRepository port
- 5: InstrumentMetadata DTO deleted; IPriceFeed.ProbeAsync returns Instrument
- 6: ProbeInstrumentUseCase returns Instrument; input carries Kind
- 7: AddInstrumentUseCase consumes probed Instrument + IInstrumentRepository
- 8: InstrumentConfiguration with VO converters (no migration)
- 9: EfInstrumentRepository + DI registration
- 10: swap remaining ~11 consumers to IInstrumentRepository
- 11: delete InstrumentByTickerSpec + AllInstrumentsSpec
- 12: migrate test fixtures to factory shape
- 13: rewrite AddInstrumentUseCaseTests
- 14: EfInstrumentRepository round-trip + duplicate-detection tests
- 15: final verification + smoke

**Behavioral guarantee:** every page renders identically; the probe flow now surfaces invalid timezones at probe time instead of at first dashboard render; duplicate-ticker errors come from the same exception (`DuplicateInstrumentException`) but originate in the repo write path.

**Risks called out:**
- Task 5 changes the wire shape of `IPriceFeed.GetInstrumentMetadataAsync` → `ProbeAsync`. The Yahoo implementation, the Stub, AND the parser all need to be touched in one commit; otherwise the Application+Infrastructure projects diverge.
- Task 8's `InstrumentConfiguration` reads existing string columns through the new VO converters. If any persisted row has a TimezoneId that doesn't validate (`Europe/Atlantis`), the rehydration throws on load. Pre-checked: the dev DB rows are all standard IANA names (Europe/London, America/New_York, etc.) — should be fine.
- Task 10's consumer-swap is the broadest change; the build will be red until every site is updated. The 11 consumer files are small; expect ~30 minutes of careful sed-like edits.

---

## Self-review notes

**Spec coverage** (against §4 new VOs + §9 Phase 4):
- §4 Exchange VO — Task 1 ✓
- §4 TimezoneId VO — Task 2 ✓
- §9 Instrument AR with Probed/Existing factories — Task 3 ✓
- §9 Confirm/Rename behavior — Task 3 ✓
- §9 InstrumentMetadata absorbed (deleted) — Task 5 ✓
- §9 ProbeInstrumentUseCase returns Instrument — Task 6 ✓
- §9 AddInstrumentUseCase calls Confirm(clock) — Task 7 ✓
- §9 IInstrumentRepository per-aggregate port — Task 4 ✓
- §9 EfInstrumentRepository with DuplicateInstrumentException — Task 9 ✓
- §9 InstrumentConfiguration with VO mapping — Task 8 ✓
- §9 No DB migration — Task 8 confirms ✓
- §9 Use cases / sections / MCP tool swap — Task 10 ✓
- §9 Specifications deleted — Task 11 ✓
- §9 Domain.Tests/Instruments/InstrumentTests — Task 3 ✓
- §9 Domain.Tests/Shared/{ExchangeTests, TimezoneIdTests} — Tasks 1, 2 ✓
- §9 DuplicateInstrumentException flow → Infrastructure.Tests — Task 14 ✓

**Type consistency:** `Instrument.Probed(ticker, name, currency, exchange, timezoneId, kind)` consistent across Tasks 3, 5, 6, 12, 14. `Instrument.Existing(id, ticker, name, currency, exchange, timezoneId, kind, addedAt)` consistent across Tasks 3, 12, 14. `IInstrumentRepository` methods consistent across Tasks 4, 9, 10, 13, 14.

**Placeholder scan:** no "TBD" / "implement later" / "similar to Task N" handwaves. Task 10 lists each of the 11 consumer files explicitly with the specific swap; Task 12 lists the four likely test files. The probe-vs-existing decision in Task 12 ("when the test seeds a persisted-shape row" vs "exercising the probe flow") is a judgment call — both branches are shown in the example.
