# DDD Rework Phase 6 — Settings VOs + Per-Domain Repositories

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the string-keyed `ISettingsReader` / `ISettingsService` surface with typed value objects and per-domain repositories that mirror the Phase 3–5 AR pattern (`IGoalRepository`, `IInstrumentRepository`, etc.). All validation moves into VO factories; the EF `Settings` table stays as the persistence backplane but is no longer accessed through generic string accessors from production code.

**Architecture:**
- Per-setting VOs in `TradyStrat.Domain/Settings/` with `Of(...)` factories that validate.
- Typed records (`AnthropicSettings`, `PolymarketSettings`) now hold VOs instead of primitives.
- One repository port per domain — `IAnthropicSettingsRepository`, `IPolymarketSettingsRepository`, `IFocusTickerRepository` — each returning the typed record.
- The EF impls translate the typed record ↔ `SettingEntry` rows (4 rows for Anthropic, 4 for Polymarket, 1 for FocusTicker).
- Three small use cases replace the generic `UpdateSettingUseCase`.
- `SettingsRegistry` shrinks to just default values + format (for the seeder); `SettingDescriptor.Parse` / `Validate` go away.
- `ISettingsReader` / `ISettingsService` are deleted at the end.

**Tech Stack:** .NET 10, EF Core 10.0.7, xunit.v3 + Shouldly, Blazor Server.

---

## File Structure

**New (Domain):**
- `TradyStrat.Domain/Settings/Anthropic/AnthropicModel.cs`
- `TradyStrat.Domain/Settings/Anthropic/MaxTokens.cs`
- `TradyStrat.Domain/Settings/Anthropic/ThinkingBudget.cs`
- `TradyStrat.Domain/Settings/Anthropic/MaxParallelSuggestions.cs`
- `TradyStrat.Domain/Settings/Anthropic/AnthropicSettings.cs` (moves from Application)
- `TradyStrat.Domain/Settings/Polymarket/SearchQueries.cs`
- `TradyStrat.Domain/Settings/Polymarket/MaxMarkets.cs`
- `TradyStrat.Domain/Settings/Polymarket/MinVolumeUsd.cs`
- `TradyStrat.Domain/Settings/Polymarket/MaxHorizonDays.cs`
- `TradyStrat.Domain/Settings/Polymarket/PolymarketSettings.cs` (moves from Application)
- `TradyStrat.Domain/Settings/Tickers/FocusTicker.cs`

**New (Application — ports):**
- `TradyStrat.Application/Settings/IAnthropicSettingsRepository.cs`
- `TradyStrat.Application/Settings/IPolymarketSettingsRepository.cs`
- `TradyStrat.Application/Settings/IFocusTickerRepository.cs`
- `TradyStrat.Application/Settings/UseCases/UpdateAnthropicSettingsUseCase.cs`
- `TradyStrat.Application/Settings/UseCases/UpdatePolymarketSettingsUseCase.cs`
- `TradyStrat.Application/Settings/UseCases/UpdateFocusTickerUseCase.cs`

**New (Infrastructure):**
- `TradyStrat.Infrastructure/Settings/EfAnthropicSettingsRepository.cs`
- `TradyStrat.Infrastructure/Settings/EfPolymarketSettingsRepository.cs`
- `TradyStrat.Infrastructure/Settings/EfFocusTickerRepository.cs`

**Deleted (end of phase):**
- `TradyStrat.Application/Settings/Config/ISettingsReader.cs`
- `TradyStrat.Application/Settings/Config/ISettingsService.cs`
- `TradyStrat.Infrastructure/Settings/Config/SettingsService.cs`
- `TradyStrat.Infrastructure/Settings/Config/SettingsReader.cs`
- `TradyStrat.Application/Settings/UseCases/UpdateSettingUseCase.cs` (replaced by 3 new use cases)
- `TradyStrat.Application/Settings/Config/SettingDescriptor.cs` Parse/Validate fields (Default + Format stay for the seeder)
- `TradyStrat.TestKit/Settings/FakeSettingsReader.cs` (replaced by 3 fakes)

**Modified:**
- The 6 UI consumers (`AnthropicSettingsForm`, `PolymarketSettingsForm`, `FocusTickerForm`, `DashboardPage`, `TradesPage`, `SettingsPage`) — swap to typed repos.
- The Anthropic / Polymarket adapters that currently consume `ISettingsReader.Anthropic/PolymarketAsync` — swap to the new repos.
- `SettingsRegistry` — drop Parse/Validate; keep DefaultRaw + Format for the seeder only.

---

## Task 1 — Anthropic VOs (Model, MaxTokens, ThinkingBudget, MaxParallelSuggestions)

**Files:**
- Create: `TradyStrat.Domain/Settings/Anthropic/AnthropicModel.cs`
- Create: `TradyStrat.Domain/Settings/Anthropic/MaxTokens.cs`
- Create: `TradyStrat.Domain/Settings/Anthropic/ThinkingBudget.cs`
- Create: `TradyStrat.Domain/Settings/Anthropic/MaxParallelSuggestions.cs`
- Test: `TradyStrat.Domain.Tests/Settings/Anthropic/AnthropicVoTests.cs`

- [ ] **Step 1.1: Write the failing tests**

```csharp
using Shouldly;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Settings.Anthropic;
using Xunit;

namespace TradyStrat.Domain.Tests.Settings.Anthropic;

public class AnthropicVoTests
{
    [Fact] public void AnthropicModel_trims_input()
        => AnthropicModel.Of("  claude-opus-4-7  ").Value.ShouldBe("claude-opus-4-7");

    [Fact] public void AnthropicModel_rejects_blank()
        => Should.Throw<SettingValidationException>(() => AnthropicModel.Of(" "));

    [Theory]
    [InlineData(0)]
    [InlineData(100_001)]
    public void MaxTokens_rejects_out_of_range(int n)
        => Should.Throw<SettingValidationException>(() => MaxTokens.Of(n));

    [Fact] public void MaxTokens_accepts_in_range()
        => MaxTokens.Of(1500).Value.ShouldBe(1500);

    [Theory]
    [InlineData(1023)]
    [InlineData(16_001)]
    public void ThinkingBudget_rejects_out_of_range(int n)
        => Should.Throw<SettingValidationException>(() => ThinkingBudget.Of(n));

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void MaxParallel_rejects_out_of_range(int n)
        => Should.Throw<SettingValidationException>(() => MaxParallelSuggestions.Of(n));
}
```

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~AnthropicVoTests"`
Expected: 5 tests fail to compile (VOs don't exist).

- [ ] **Step 1.2: Implement the four VOs**

`AnthropicModel.cs`:
```csharp
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.Settings.Anthropic;

public readonly record struct AnthropicModel(string Value)
{
    public static AnthropicModel Of(string raw)
    {
        var trimmed = (raw ?? "").Trim();
        if (trimmed.Length == 0)
            throw new SettingValidationException("Anthropic model cannot be empty.");
        return new AnthropicModel(trimmed);
    }
    public override string ToString() => Value;
}
```

`MaxTokens.cs`:
```csharp
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.Settings.Anthropic;

public readonly record struct MaxTokens(int Value)
{
    public static MaxTokens Of(int n)
    {
        if (n < 1 || n > 100_000)
            throw new SettingValidationException($"Max tokens must be in [1, 100000], got {n}.");
        return new MaxTokens(n);
    }
}
```

`ThinkingBudget.cs`:
```csharp
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.Settings.Anthropic;

public readonly record struct ThinkingBudget(int Value)
{
    public static ThinkingBudget Of(int n)
    {
        if (n < 1024 || n > 16_000)
            throw new SettingValidationException($"Thinking budget must be in [1024, 16000], got {n}.");
        return new ThinkingBudget(n);
    }
}
```

`MaxParallelSuggestions.cs`:
```csharp
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.Settings.Anthropic;

public readonly record struct MaxParallelSuggestions(int Value)
{
    public static MaxParallelSuggestions Of(int n)
    {
        if (n < 1 || n > 10)
            throw new SettingValidationException($"Max parallel suggestions must be in [1, 10], got {n}.");
        return new MaxParallelSuggestions(n);
    }
}
```

- [ ] **Step 1.3: Run domain tests**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~AnthropicVoTests"`
Expected: 5/5 PASS.

- [ ] **Step 1.4: Commit**

```bash
git add -A
git commit -m "feat(domain): Anthropic settings VOs (Model/MaxTokens/ThinkingBudget/MaxParallel) — Phase 6 Task 1"
```

---

## Task 2 — Polymarket VOs (SearchQueries, MaxMarkets, MinVolumeUsd, MaxHorizonDays)

**Files:**
- Create: `TradyStrat.Domain/Settings/Polymarket/SearchQueries.cs`
- Create: `TradyStrat.Domain/Settings/Polymarket/MaxMarkets.cs`
- Create: `TradyStrat.Domain/Settings/Polymarket/MinVolumeUsd.cs`
- Create: `TradyStrat.Domain/Settings/Polymarket/MaxHorizonDays.cs`
- Test: `TradyStrat.Domain.Tests/Settings/Polymarket/PolymarketVoTests.cs`

- [ ] **Step 2.1: Write the failing tests**

```csharp
using Shouldly;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Settings.Polymarket;
using Xunit;

namespace TradyStrat.Domain.Tests.Settings.Polymarket;

public class PolymarketVoTests
{
    [Fact] public void SearchQueries_trims_and_lowercases()
        => SearchQueries.Of(["  BITCOIN ", "Eth"]).Values.ShouldBe(["bitcoin", "eth"]);

    [Fact] public void SearchQueries_rejects_empty_list()
        => Should.Throw<SettingValidationException>(() => SearchQueries.Of([]));

    [Fact] public void SearchQueries_rejects_blank_entries()
        => Should.Throw<SettingValidationException>(() => SearchQueries.Of(["btc", "  "]));

    [Fact] public void MaxMarkets_rejects_zero()
        => Should.Throw<SettingValidationException>(() => MaxMarkets.Of(0));

    [Fact] public void MinVolumeUsd_rejects_negative()
        => Should.Throw<SettingValidationException>(() => MinVolumeUsd.Of(-1m));

    [Fact] public void MaxHorizonDays_rejects_zero()
        => Should.Throw<SettingValidationException>(() => MaxHorizonDays.Of(0));
}
```

- [ ] **Step 2.2: Implement the four VOs**

`SearchQueries.cs` (sealed record, not struct — it holds a collection):
```csharp
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.Settings.Polymarket;

public sealed record SearchQueries
{
    public IReadOnlyList<string> Values { get; }

    private SearchQueries(IReadOnlyList<string> values) => Values = values;

    public static SearchQueries Of(IEnumerable<string> raw)
    {
        var normalized = new List<string>();
        foreach (var q in raw ?? Enumerable.Empty<string>())
        {
            var t = (q ?? "").Trim().ToLowerInvariant();
            if (t.Length == 0)
                throw new SettingValidationException("Search queries cannot contain blank entries.");
            normalized.Add(t);
        }
        if (normalized.Count == 0)
            throw new SettingValidationException("Search queries must contain at least one entry.");
        return new SearchQueries(normalized);
    }
}
```

`MaxMarkets.cs` (private-ctor pattern — mirrors `Percentage.cs`):
```csharp
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.Settings.Polymarket;

public readonly record struct MaxMarkets
{
    public int Value { get; }
    private MaxMarkets(int value) => Value = value;
    public static MaxMarkets Of(int n)
    {
        if (n < 1)
            throw new SettingValidationException($"Max markets must be >= 1, got {n}.");
        return new MaxMarkets(n);
    }
}
```

`MinVolumeUsd.cs` (private-ctor pattern):
```csharp
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.Settings.Polymarket;

public readonly record struct MinVolumeUsd
{
    public decimal Value { get; }
    private MinVolumeUsd(decimal value) => Value = value;
    public static MinVolumeUsd Of(decimal n)
    {
        if (n < 0m)
            throw new SettingValidationException($"Min volume USD cannot be negative, got {n}.");
        return new MinVolumeUsd(n);
    }
}
```

`MaxHorizonDays.cs` (private-ctor pattern):
```csharp
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.Settings.Polymarket;

public readonly record struct MaxHorizonDays
{
    public int Value { get; }
    private MaxHorizonDays(int value) => Value = value;
    public static MaxHorizonDays Of(int n)
    {
        if (n < 1)
            throw new SettingValidationException($"Max horizon days must be >= 1, got {n}.");
        return new MaxHorizonDays(n);
    }
}
```

- [ ] **Step 2.3: Tests + commit**

```bash
dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~PolymarketVoTests"
git add -A
git commit -m "feat(domain): Polymarket settings VOs (SearchQueries/MaxMarkets/MinVolumeUsd/MaxHorizonDays) — Phase 6 Task 2"
```

Expected: 6/6 PASS.

---

## Task 3 — FocusTicker VO

**Files:**
- Create: `TradyStrat.Domain/Settings/Tickers/FocusTicker.cs`
- Test: `TradyStrat.Domain.Tests/Settings/Tickers/FocusTickerTests.cs`

A pure-format VO. Cross-aggregate validation ("does this ticker map to an Instrument?") is enforced at the repository write path, NOT in the VO.

- [ ] **Step 3.1: Write the failing tests**

```csharp
using Shouldly;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Settings.Tickers;
using Xunit;

namespace TradyStrat.Domain.Tests.Settings.Tickers;

public class FocusTickerTests
{
    [Fact] public void Trims_and_uppercases() =>
        FocusTicker.Of("  con3.l  ").Value.ShouldBe("CON3.L");

    [Fact] public void Rejects_blank() =>
        Should.Throw<SettingValidationException>(() => FocusTicker.Of("   "));
}
```

- [ ] **Step 3.2: Implement (private-ctor pattern)**

```csharp
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.Settings.Tickers;

public readonly record struct FocusTicker
{
    public string Value { get; }
    private FocusTicker(string value) => Value = value;
    public static FocusTicker Of(string raw)
    {
        var t = (raw ?? "").Trim().ToUpperInvariant();
        if (t.Length == 0)
            throw new SettingValidationException("Focus ticker cannot be empty.");
        return new FocusTicker(t);
    }
    public override string ToString() => Value;
}
```

- [ ] **Step 3.3: Tests + commit**

```bash
dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~FocusTickerTests"
git add -A
git commit -m "feat(domain): FocusTicker VO (pure-format validation) — Phase 6 Task 3"
```

Expected: 2/2 PASS.

---

## Task 4 — Move AnthropicSettings + PolymarketSettings into Domain, swap primitives for VOs

**Files:**
- Create: `TradyStrat.Domain/Settings/Anthropic/AnthropicSettings.cs`
- Create: `TradyStrat.Domain/Settings/Polymarket/PolymarketSettings.cs`
- Delete: `TradyStrat.Application/Settings/Config/AnthropicSettings.cs` and `PolymarketSettings.cs`

The records move into Domain (they're pure VOs now). Application gets `using TradyStrat.Domain.Settings.*;` everywhere.

- [ ] **Step 4.1: Create the new records**

`AnthropicSettings.cs`:
```csharp
namespace TradyStrat.Domain.Settings.Anthropic;

public sealed record AnthropicSettings(
    AnthropicModel Model,
    MaxTokens MaxTokens,
    ThinkingBudget ThinkingBudget,
    MaxParallelSuggestions MaxParallelSuggestions);
```

`PolymarketSettings.cs`:
```csharp
namespace TradyStrat.Domain.Settings.Polymarket;

public sealed record PolymarketSettings(
    SearchQueries SearchQueries,
    MaxMarkets MaxMarkets,
    MinVolumeUsd MinVolumeUsd,
    MaxHorizonDays MaxHorizonDays);
```

- [ ] **Step 4.2: Delete the old Application-layer copies**

```bash
rm TradyStrat.Application/Settings/Config/AnthropicSettings.cs
rm TradyStrat.Application/Settings/Config/PolymarketSettings.cs
```

- [ ] **Step 4.3: Update consumers**

Find every reference to the old types and update the `using` + property access:

```bash
grep -rln 'TradyStrat.Application.Settings.Config' --include='*.cs' .
```

The records used to be primitive-backed (`Model: string`). Now they're VO-backed (`Model: AnthropicModel`). Every consumer that read `.Model` now gets an `AnthropicModel`; they need either `.Model.Value` or `.Model.ToString()`. Update each call site.

Key consumers: `AnthropicAdapter`, `PolymarketAdapter`, the two UI forms, FakeSettingsReader.

- [ ] **Step 4.4: Build + commit**

```bash
dotnet build TradyStrat.slnx
# Expected: 0 errors after consumer fixups.
git add -A
git commit -m "refactor(domain): AnthropicSettings + PolymarketSettings move to Domain and adopt VOs — Phase 6 Task 4"
```

---

## Task 5 — `IAnthropicSettingsRepository` + `EfAnthropicSettingsRepository`

**Files:**
- Create: `TradyStrat.Application/Settings/IAnthropicSettingsRepository.cs`
- Create: `TradyStrat.Infrastructure/Settings/EfAnthropicSettingsRepository.cs`
- Modify: `TradyStrat.Infrastructure/Settings/SettingsInfrastructureModule.cs` — DI registration
- Test: `TradyStrat.Infrastructure.Tests/Settings/EfAnthropicSettingsRepositoryTests.cs`

- [ ] **Step 5.1: Port**

```csharp
using TradyStrat.Domain.Settings.Anthropic;

namespace TradyStrat.Application.Settings;

public interface IAnthropicSettingsRepository
{
    Task<AnthropicSettings> GetAsync(CancellationToken ct);
    Task SaveAsync(AnthropicSettings settings, CancellationToken ct);
}
```

- [ ] **Step 5.2: EF impl**

The repo reads 4 setting rows and rehydrates the typed record via VO factories; on save, it upserts 4 rows. Use `ISettingsRegistry` to look up the format function per key (needed for the seeder; reuse here for consistency).

```csharp
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using TradyStrat.Application.Settings;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Domain;
using TradyStrat.Domain.Settings.Anthropic;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.Infrastructure.Settings;

public sealed class EfAnthropicSettingsRepository(AppDbContext db, IClock clock) : IAnthropicSettingsRepository
{
    public async Task<AnthropicSettings> GetAsync(CancellationToken ct)
    {
        var rows = await db.Set<SettingEntry>()
            .Where(s => s.Key.StartsWith("anthropic."))
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);

        return new AnthropicSettings(
            AnthropicModel.Of(rows[SettingsKeys.AnthropicModel]),
            MaxTokens.Of(int.Parse(rows[SettingsKeys.AnthropicMaxTokens], CultureInfo.InvariantCulture)),
            ThinkingBudget.Of(int.Parse(rows[SettingsKeys.AnthropicThinkingBudget], CultureInfo.InvariantCulture)),
            MaxParallelSuggestions.Of(int.Parse(rows[SettingsKeys.AnthropicMaxParallelSuggestions], CultureInfo.InvariantCulture)));
    }

    public async Task SaveAsync(AnthropicSettings s, CancellationToken ct)
    {
        var now = clock.UtcNow();
        await UpsertAsync(SettingsKeys.AnthropicModel, s.Model.Value, now, ct);
        await UpsertAsync(SettingsKeys.AnthropicMaxTokens, s.MaxTokens.Value.ToString(CultureInfo.InvariantCulture), now, ct);
        await UpsertAsync(SettingsKeys.AnthropicThinkingBudget, s.ThinkingBudget.Value.ToString(CultureInfo.InvariantCulture), now, ct);
        await UpsertAsync(SettingsKeys.AnthropicMaxParallelSuggestions, s.MaxParallelSuggestions.Value.ToString(CultureInfo.InvariantCulture), now, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task UpsertAsync(string key, string value, DateTime now, CancellationToken ct)
    {
        var existing = await db.Set<SettingEntry>().FindAsync([key], ct);
        if (existing is null) db.Add(new SettingEntry { Key = key, Value = value, UpdatedAt = now });
        else { existing.Value = value; existing.UpdatedAt = now; }
    }
}
```

- [ ] **Step 5.3: DI registration**

In `SettingsInfrastructureModule.cs`:
```csharp
services.AddScoped<IAnthropicSettingsRepository, EfAnthropicSettingsRepository>();
```

- [ ] **Step 5.4: Test**

```csharp
using Shouldly;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Domain;
using TradyStrat.Domain.Settings.Anthropic;
using TradyStrat.Infrastructure.Settings;
using TradyStrat.TestKit.Specifications;
using TradyStrat.TestKit.Time;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Settings;

public class EfAnthropicSettingsRepositoryTests
{
    [Fact]
    public async Task RoundTrips_through_VOs()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        // Seed 4 rows the way the seeder would.
        Seed(db, SettingsKeys.AnthropicModel, "claude-opus-4-7");
        Seed(db, SettingsKeys.AnthropicMaxTokens, "1500");
        Seed(db, SettingsKeys.AnthropicThinkingBudget, "8192");
        Seed(db, SettingsKeys.AnthropicMaxParallelSuggestions, "3");
        await db.SaveChangesAsync(ct);

        var repo = new EfAnthropicSettingsRepository(db, new FakeClock(DateTime.UtcNow));
        var loaded = await repo.GetAsync(ct);

        loaded.Model.Value.ShouldBe("claude-opus-4-7");
        loaded.MaxTokens.Value.ShouldBe(1500);

        var updated = loaded with { MaxTokens = MaxTokens.Of(2000) };
        await repo.SaveAsync(updated, ct);

        var reloaded = await new EfAnthropicSettingsRepository(db, new FakeClock(DateTime.UtcNow)).GetAsync(ct);
        reloaded.MaxTokens.Value.ShouldBe(2000);
    }

    private static void Seed(Infrastructure.Data.AppDbContext db, string key, string value)
        => db.Add(new SettingEntry { Key = key, Value = value, UpdatedAt = DateTime.UtcNow });
}
```

Run: `dotnet test TradyStrat.Infrastructure.Tests --filter "FullyQualifiedName~EfAnthropicSettingsRepositoryTests"`
Expected: PASS.

- [ ] **Step 5.5: Commit**

```bash
git add -A
git commit -m "feat(infra): IAnthropicSettingsRepository + EfAnthropicSettingsRepository — Phase 6 Task 5"
```

---

## Task 6 — `IPolymarketSettingsRepository` + impl

**Files:**
- Create: `TradyStrat.Application/Settings/IPolymarketSettingsRepository.cs`
- Create: `TradyStrat.Infrastructure/Settings/EfPolymarketSettingsRepository.cs`
- Modify: `SettingsInfrastructureModule.cs`
- Test: `TradyStrat.Infrastructure.Tests/Settings/EfPolymarketSettingsRepositoryTests.cs`

Mirror Task 5 exactly, but for Polymarket. `SearchQueries` round-trips as JSON via `System.Text.Json`.

- [ ] **Step 6.1: Port + impl**

```csharp
// IPolymarketSettingsRepository.cs
using TradyStrat.Domain.Settings.Polymarket;

namespace TradyStrat.Application.Settings;

public interface IPolymarketSettingsRepository
{
    Task<PolymarketSettings> GetAsync(CancellationToken ct);
    Task SaveAsync(PolymarketSettings settings, CancellationToken ct);
}
```

`EfPolymarketSettingsRepository.cs` follows the Anthropic shape; `SearchQueries.Values` serializes to JSON for storage:
```csharp
JsonSerializer.Serialize(s.SearchQueries.Values)
// on read:
SearchQueries.Of(JsonSerializer.Deserialize<string[]>(raw) ?? [])
```

- [ ] **Step 6.2: DI + test + commit**

```bash
dotnet build TradyStrat.slnx
dotnet test TradyStrat.Infrastructure.Tests --filter "FullyQualifiedName~EfPolymarketSettingsRepositoryTests"
git add -A
git commit -m "feat(infra): IPolymarketSettingsRepository + EfPolymarketSettingsRepository — Phase 6 Task 6"
```

---

## Task 7 — `IFocusTickerRepository` + impl (with Instrument-existence check on Save)

**Files:**
- Create: `TradyStrat.Application/Settings/IFocusTickerRepository.cs`
- Create: `TradyStrat.Infrastructure/Settings/EfFocusTickerRepository.cs`
- Modify: `SettingsInfrastructureModule.cs`
- Test: `TradyStrat.Infrastructure.Tests/Settings/EfFocusTickerRepositoryTests.cs`

The repo takes `IInstrumentRepository` and rejects unknown tickers on SaveAsync (cross-AR validation).

- [ ] **Step 7.1: Port**

```csharp
using TradyStrat.Domain.Settings.Tickers;

namespace TradyStrat.Application.Settings;

public interface IFocusTickerRepository
{
    Task<FocusTicker> GetAsync(CancellationToken ct);
    /// <summary>Throws <see cref="SettingValidationException"/> if the ticker doesn't match any registered Instrument.</summary>
    Task SaveAsync(FocusTicker ticker, CancellationToken ct);
}
```

- [ ] **Step 7.2: Impl**

```csharp
using Microsoft.EntityFrameworkCore;
using TradyStrat.Application.Settings;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Settings.Tickers;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.Infrastructure.Settings;

public sealed class EfFocusTickerRepository(
    AppDbContext db,
    IInstrumentRepository instruments,
    IClock clock) : IFocusTickerRepository
{
    public async Task<FocusTicker> GetAsync(CancellationToken ct)
    {
        var entry = await db.Set<SettingEntry>().FindAsync([SettingsKeys.TickersFocus], ct)
            ?? throw new InvalidOperationException("Focus ticker missing from Settings.");
        return FocusTicker.Of(entry.Value);
    }

    public async Task SaveAsync(FocusTicker ticker, CancellationToken ct)
    {
        var instrument = await instruments.FindByTickerAsync(ticker.Value, ct)
            ?? throw new SettingValidationException(
                $"Focus ticker '{ticker.Value}' does not match any registered instrument.");

        var now = clock.UtcNow();
        var existing = await db.Set<SettingEntry>().FindAsync([SettingsKeys.TickersFocus], ct);
        if (existing is null)
            db.Add(new SettingEntry { Key = SettingsKeys.TickersFocus, Value = ticker.Value, UpdatedAt = now });
        else
        { existing.Value = ticker.Value; existing.UpdatedAt = now; }
        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 7.3: DI + tests**

Tests cover: round-trip happy path; SaveAsync rejects an unknown ticker; SaveAsync accepts a registered ticker.

- [ ] **Step 7.4: Commit**

```bash
git add -A
git commit -m "feat(infra): IFocusTickerRepository + EfFocusTickerRepository (Instrument-existence check on save) — Phase 6 Task 7"
```

---

## Task 8 — Three replacement use cases

**Files:**
- Create: `TradyStrat.Application/Settings/UseCases/UpdateAnthropicSettingsUseCase.cs`
- Create: `TradyStrat.Application/Settings/UseCases/UpdatePolymarketSettingsUseCase.cs`
- Create: `TradyStrat.Application/Settings/UseCases/UpdateFocusTickerUseCase.cs`
- Modify: `SettingsApplicationModule.cs` — DI

Each use case takes a typed input (typed VOs from the UI form), saves via the new repo, returns the new `UpdatedAt`. The VOs are constructed at the UI layer from text/numeric input; the use case body is just delegation.

- [ ] **Step 8.1: Implement (template — repeat for the two others)**

```csharp
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Settings.Anthropic;

namespace TradyStrat.Application.Settings.UseCases;

public sealed record UpdateAnthropicSettingsInput(AnthropicSettings Settings);

public sealed class UpdateAnthropicSettingsUseCase(
    IAnthropicSettingsRepository repo,
    IClock clock,
    ILogger<UpdateAnthropicSettingsUseCase> log)
    : UseCaseBase<UpdateAnthropicSettingsInput, DateTime>(log)
{
    protected override async Task<DateTime> ExecuteCore(UpdateAnthropicSettingsInput input, CancellationToken ct)
    {
        await repo.SaveAsync(input.Settings, ct);
        return clock.UtcNow();
    }
}
```

`UpdatePolymarketSettingsInput(PolymarketSettings Settings)` — same shape.
`UpdateFocusTickerInput(FocusTicker Ticker)` — same shape; SetingValidationException bubbles up if unknown.

- [ ] **Step 8.2: DI registrations**

In `SettingsApplicationModule.cs`:
```csharp
services.AddScoped<UpdateAnthropicSettingsUseCase>();
services.AddScoped<UpdatePolymarketSettingsUseCase>();
services.AddScoped<UpdateFocusTickerUseCase>();
```

- [ ] **Step 8.3: Build + commit**

```bash
dotnet build TradyStrat.slnx
git add -A
git commit -m "feat(application): three typed Update*SettingsUseCases replace generic UpdateSettingUseCase — Phase 6 Task 8"
```

---

## Task 9 — Update UI forms to use the new repos and use cases

**Files:**
- Modify: `TradyStrat/Features/Settings/AnthropicSettingsForm.razor.cs`
- Modify: `TradyStrat/Features/Settings/PolymarketSettingsForm.razor.cs`
- Modify: `TradyStrat/Features/Settings/FocusTickerForm.razor.cs`

Each form:
1. Replace `[Inject] ISettingsReader Settings` with the matching new repo (`IAnthropicSettingsRepository`, etc.).
2. Replace `[Inject] UpdateSettingUseCase Update` with the typed use case.
3. On `OnInitializedAsync`, call `repo.GetAsync(ct)` to hydrate component state.
4. On save, build the typed VO record from form fields (wrapping each field in its VO constructor), call the typed use case with `new UpdateAnthropicSettingsInput(record)`.
5. Validation errors from VO constructors (`SettingValidationException`) are caught and rendered in the form's error banner (no behavior change vs today).

- [ ] **Step 9.1: Rewrite `AnthropicSettingsForm.razor.cs`**

Show the diff in full — read the current file first, then rewrite it. Particular care: today's form calls `UpdateSettingUseCase.ExecuteAsync` once per changed key (4 calls). The new shape calls `UpdateAnthropicSettingsUseCase.ExecuteAsync` once with the whole typed record.

- [ ] **Step 9.2: Rewrite `PolymarketSettingsForm.razor.cs`** (same shape)

- [ ] **Step 9.3: Rewrite `FocusTickerForm.razor.cs`** (single VO, single call)

- [ ] **Step 9.4: Manual smoke**

```bash
dotnet run --project TradyStrat --urls=http://localhost:5180
```

Open `http://localhost:5180/settings` in a browser. For each form:
- Load → values populate.
- Edit one field to an invalid value (e.g. MaxTokens = 0) → save → error banner shows.
- Edit to a valid value → save → success toast, `UpdatedAt` re-renders.

- [ ] **Step 9.5: Commit**

```bash
git add -A
git commit -m "refactor(ui): Settings forms consume typed repos + Update*SettingsUseCases — Phase 6 Task 9"
```

---

## Task 10 — Swap other consumers off `ISettingsReader`

**Files:**
- Modify: `TradyStrat.Infrastructure/AiSuggestion/Adapters/AnthropicSuggestionAdapter.cs`
- Modify: `TradyStrat.Infrastructure/AiSuggestion/Adapters/PolymarketResearchAdapter.cs`
- Modify: `TradyStrat.Application/Dashboard/Navigation/EntryNavigationService.cs` (currently uses `settings.FocusTickerAsync`)
- Modify: any other consumers found by `grep -rn 'ISettingsReader\|settings.FocusTickerAsync\|settings.AnthropicAsync\|settings.PolymarketAsync'`

For each consumer, inject the matching typed repo and call `GetAsync(ct)` in place of the `ISettingsReader` call.

- [ ] **Step 10.1: Inventory**

```bash
grep -rn 'ISettingsReader' --include='*.cs' . | grep -v '/bin/' | grep -v '/obj/'
```

List every file. For each, decide which repo it needs.

- [ ] **Step 10.2: Rewrite each consumer**

The mechanical replacement is:
```csharp
// before
private readonly ISettingsReader _settings;
var anth = await _settings.AnthropicAsync(ct);
var model = anth.Model;  // string

// after
private readonly IAnthropicSettingsRepository _anthropic;
var anth = await _anthropic.GetAsync(ct);
var model = anth.Model.Value;  // string from VO
```

- [ ] **Step 10.3: Build + commit**

```bash
dotnet build TradyStrat.slnx
git add -A
git commit -m "refactor: consumers swap from ISettingsReader to typed Settings repos — Phase 6 Task 10"
```

---

## Task 11 — Test doubles: split `FakeSettingsReader` into three fakes

**Files:**
- Create: `TradyStrat.TestKit/Settings/FakeAnthropicSettingsRepository.cs`
- Create: `TradyStrat.TestKit/Settings/FakePolymarketSettingsRepository.cs`
- Create: `TradyStrat.TestKit/Settings/FakeFocusTickerRepository.cs`
- Delete: `TradyStrat.TestKit/Settings/FakeSettingsReader.cs`
- Modify: every test that currently uses `FakeSettingsReader`

- [ ] **Step 11.1: Implement the three fakes**

Each fake takes the typed record (with sensible defaults) and returns it from GetAsync. SaveAsync stores in a `_state` field. They mirror the production repos but skip the EF layer.

```csharp
public sealed class FakeFocusTickerRepository(string ticker = "CON3.L") : IFocusTickerRepository
{
    private FocusTicker _state = FocusTicker.Of(ticker);
    public Task<FocusTicker> GetAsync(CancellationToken ct) => Task.FromResult(_state);
    public Task SaveAsync(FocusTicker t, CancellationToken ct) { _state = t; return Task.CompletedTask; }
}
```

Anthropic + Polymarket fakes follow the same pattern with default VOs (or throw `NotSupportedException` if defaults aren't set — match the existing `FakeSettingsReader` discipline).

- [ ] **Step 11.2: Find and update all callers**

```bash
grep -rln 'FakeSettingsReader' --include='*.cs' .
```

Each caller takes either Anthropic, Polymarket, or FocusTicker — replace with the matching fake. Most tests only need `FakeFocusTickerRepository`.

- [ ] **Step 11.3: Delete the old fake + run all tests**

```bash
rm TradyStrat.TestKit/Settings/FakeSettingsReader.cs
dotnet test TradyStrat.slnx
```

Expected: every test passes.

- [ ] **Step 11.4: Commit**

```bash
git add -A
git commit -m "test: replace FakeSettingsReader with three typed fakes — Phase 6 Task 11"
```

---

## Task 12 — Delete `ISettingsReader`, `ISettingsService`, `UpdateSettingUseCase`, descriptors' Parse/Validate

**Files:**
- Delete: `TradyStrat.Application/Settings/Config/ISettingsReader.cs`
- Delete: `TradyStrat.Application/Settings/Config/ISettingsService.cs`
- Delete: `TradyStrat.Infrastructure/Settings/Config/SettingsReader.cs`
- Delete: `TradyStrat.Infrastructure/Settings/Config/SettingsService.cs`
- Delete: `TradyStrat.Application/Settings/UseCases/UpdateSettingUseCase.cs`
- Modify: `TradyStrat.Application/Settings/Config/SettingDescriptor.cs` — drop `Parse` and `Validate` fields
- Modify: `TradyStrat.Application/Settings/Config/SettingsRegistry.cs` — descriptor list shrinks (only `Key`, `DefaultRaw`, `Format`)
- Modify: `TradyStrat.Infrastructure/Settings/SettingsSeederHostedService.cs` — consumes the slimmed descriptor

The seeder only needs `Key` + `DefaultRaw` (to back-fill missing rows). `Format` was only used by `UpdateSettingUseCase`; if it's no longer referenced anywhere after Task 11, drop it too.

- [ ] **Step 12.1: Confirm nothing else references the deleted types**

```bash
grep -rn 'ISettingsReader\|ISettingsService\|UpdateSettingUseCase' --include='*.cs' . | grep -v '/bin/' | grep -v '/obj/' | grep -v 'docs/superpowers/plans/'
```

Expected: 0 hits in code (matches in the plan doc itself are fine).

- [ ] **Step 12.2: Delete + slim the descriptor**

`SettingDescriptor` becomes:
```csharp
public sealed class SettingDescriptor
{
    public required string Key { get; init; }
    public required string DefaultRaw { get; init; }
}
```

`SettingsRegistry.Build()` removes the `Parse`/`Validate`/`Format` lambdas — every entry is just `{ Key = ..., DefaultRaw = ... }`.

- [ ] **Step 12.3: Build + tests**

```bash
dotnet build TradyStrat.slnx
dotnet test TradyStrat.slnx
```

Expected: 0 errors, every test passes.

- [ ] **Step 12.4: Commit**

```bash
git add -A
git commit -m "chore: delete ISettingsReader/ISettingsService/UpdateSettingUseCase + slim SettingDescriptor — Phase 6 Task 12"
```

---

## Task 13 — Final verification + smoke + tag

- [ ] **Step 13.1: Full clean build**

```bash
dotnet clean TradyStrat.slnx
dotnet build TradyStrat.slnx
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 13.2: Full test suite**

```bash
dotnet test TradyStrat.slnx --no-build
```

Expected: every test passes. Counts up vs Phase 5 baseline (471) by ~20–25 new Phase 6 tests (9 VO test files + 3 repo tests + extras).

- [ ] **Step 13.3: Grep forbidden references**

```bash
# Old generic settings surface should be gone.
grep -rn 'ISettingsReader\|ISettingsService\b' --include='*.cs' --include='*.razor*' . | grep -v '/bin/' | grep -v '/obj/' | grep -v 'docs/superpowers/plans/'
```

Expected: 0 hits.

```bash
# All 9 SettingsKeys should ONLY be referenced by repos + seeder.
grep -rn 'SettingsKeys\.' --include='*.cs' . | grep -v '/bin/' | grep -v '/obj/' | grep -v 'Infrastructure/Settings/' | grep -v 'SettingsRegistry'
```

Expected: 0 hits outside the repos / seeder / registry.

- [ ] **Step 13.4: Smoke-test the app**

```bash
dotnet run --project TradyStrat --urls=http://localhost:5180
```

In a second shell:
```bash
curl -sf http://localhost:5180/         > /dev/null && echo OK
curl -sf http://localhost:5180/settings > /dev/null && echo OK
curl -sf http://localhost:5180/trades   > /dev/null && echo OK
```

Then in a browser: visit `/settings`, edit one Anthropic field, save → expect success toast + new `UpdatedAt`. Reload → value persists.

Stop the app with `Ctrl-C`.

- [ ] **Step 13.5: Phase 6 checkpoint tag**

```bash
git tag -a phase6-settings-vos-done -m "Phase 6 (Settings VOs + per-domain repos) complete"
```

- [ ] **Step 13.6: Finish branch via the finishing-a-development-branch skill**

```
I'm using the finishing-a-development-branch skill to complete this work.
```

The skill runs tests, presents the 4-option menu, cleans up the worktree on merge.

---

## Notes on rollback / migration

- **No schema migration needed.** The `Settings` table stays exactly as it is — 9 rows, same `Key`/`Value`/`UpdatedAt` columns. Phase 6 is a pure-code refactor of the read/write surface.
- **No data backfill needed.** Existing rows hydrate cleanly through the new VOs (the VO factories accept any value the old `SettingDescriptor.Parse` accepted).
- **Rollback path:** revert the Phase 6 merge commit. The `Settings` table is unchanged so the previous code reads it without issue.
