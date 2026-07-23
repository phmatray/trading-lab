# Multi-Ticker Foundation (Phase 1) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Spec:** [`docs/superpowers/specs/2026-05-07-multi-ticker-foundation-design.md`](../specs/2026-05-07-multi-ticker-foundation-design.md)

**Goal:** Widen TradyStrat's data model and dashboard so a user can hold N first-class tickers (each with its own currency, position, and zone analysis) valued together against one portfolio-wide €1M goal — without changing the AI loop.

**Architecture:** Schema-first phasing. New `Instruments` table + `Trade.InstrumentId` FK + generic `(Base, Quote, Rate)` FX. New `ProbeInstrumentUseCase` / `AddInstrumentUseCase` / `ListInstrumentsUseCase` enable a self-service Settings flow that probes Yahoo for metadata. `PriceFeedHostedService`, `SnapshotFactory`, and `PortfolioService.BuildSnapshot` are switched from hardcoded ticker lists to DB-driven enumeration. The dashboard gains a Positions table and loops zone analysis across Held + Watchlist instruments. AI suggestion path is untouched (Phase 2 territory).

**Tech Stack:** .NET 10 · Blazor Server · EF Core 10 (SQLite) · Ardalis.Specification 9.3 · TheAppManager 2.0 · xunit.v3 · Shouldly · Microsoft.EntityFrameworkCore.InMemory.

---

## Conventions used by every task

- **Working directory:** `/Users/philippe/repo/gh-phmatray/TradyStrat`. All `dotnet` commands run from here.
- **File-scoped namespaces** (existing convention).
- **Records for entities and DTOs** (existing convention).
- **Async naming:** every I/O method ends with `Async` and takes `CancellationToken ct` last.
- **Use cases:** inherit `UseCaseBase<TInput, TOutput>(ILogger)`, implement `protected override async Task<TOutput> ExecuteCore(...)`.
- **Repositories:** Ardalis `IReadRepositoryBase<T>` for reads, `IRepositoryBase<T>` for writes.
- **Source-generated logging:** `[LoggerMessage]` attribute on `private static partial void` methods (existing convention — see `DailyFxCache` for a reference shape).
- **Tests:** xunit.v3 (`[Fact]`/`[Theory]`), Shouldly (`x.ShouldBe(...)`, `Should.Throw<T>()`), file-based JSON fixtures in `<TestProject>/<Feature>/Fixtures/<file>.json` loaded with `File.ReadAllText(Path.Combine(...))`.
- **In-memory EF for repository tests:** use the existing `TradyStrat.Tests.Fx.TestRepo<T>` shim, or construct `new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)`.
- **HTTP stubbing:** use the existing `TradyStrat.Tests.PriceFeed.StubHttpHandler`.
- **Migrations are auto-applied at startup** by `DatabaseModule.ConfigureMiddleware` calling `Database.Migrate()`. New migration files belong in `TradyStrat/Data/Migrations/`.
- **EF migration tool:** `dotnet ef` — already restored as a local manifest tool; run as `dotnet ef ...` (no `tool restore` needed).
- **Build-and-test command (used after most tasks):** `dotnet build TradyStrat.slnx && dotnet test TradyStrat.slnx`. Both should be green at the end of every task.
- **Commit hygiene:** every task ends with one commit. Stage explicit paths (no `git add -A`).
- **Pre-flight backup of the live DB:** before Task 11 runs the new migration on the user's real DB, the user must `cp ~/Library/Application\ Support/TradyStrat/tradystrat.db ~/tradystrat.db.pre-multiticker.bak`. Mentioned again in Task 11.

---

## File structure overview

**New files (creates):**

```
TradyStrat/
  Common/Domain/
    Instrument.cs
    InstrumentKind.cs
    InstrumentMetadata.cs
  Common/Exceptions/
    DuplicateInstrumentException.cs
    InstrumentNotFoundException.cs
    InstrumentMetadataIncompleteException.cs
    UnsupportedCurrencyException.cs
  Data/Configurations/
    InstrumentConfiguration.cs
  Data/Migrations/
    20260507_MultiTickerFoundation.cs        (EF-generated, hand-edited)
    20260507_MultiTickerFoundation.Designer.cs (EF-generated)
    AppDbContextModelSnapshot.cs              (EF-regenerates)
  Features/Settings/Specifications/
    InstrumentByTickerSpec.cs
    AllInstrumentsSpec.cs
    InstrumentsByKindSpec.cs
  Features/Settings/UseCases/
    ProbeInstrumentUseCase.cs
    AddInstrumentUseCase.cs
    ListInstrumentsUseCase.cs
  Features/Settings/Components/
    AddInstrumentForm.razor
  Features/Dashboard/Components/
    PositionsTable.razor
TradyStrat.Tests/
  Common/Domain/
    InstrumentTests.cs                       (only if entity has invariants)
  Settings/Providers/
    YahooMetadataParserTests.cs
    Fixtures/yahoo-quote-eur-etp.json
    Fixtures/yahoo-quote-not-found.json
    Fixtures/yahoo-quote-incomplete.json
  Settings/UseCases/
    ProbeInstrumentUseCaseTests.cs
    AddInstrumentUseCaseTests.cs
    ListInstrumentsUseCaseTests.cs
  Settings/Specifications/
    InstrumentSpecTests.cs
  Portfolio/
    PortfolioServiceMultiTickerTests.cs
  Data/
    MultiTickerMigrationTests.cs
```

**Modified files:**

```
TradyStrat/
  Common/Domain/
    Trade.cs                  (+InstrumentId)
    FxRate.cs                 (Pair → Base+Quote, UsdPerEur → Rate)
    GoalConfig.cs             (-FocusTicker)
  Data/
    AppDbContext.cs           (+Instruments DbSet)
  Data/Configurations/
    TradeConfiguration.cs     (+InstrumentId mapping & index)
    FxRateConfiguration.cs    (rebuild)
    GoalConfigConfiguration.cs(remove FocusTicker)
  Features/Fx/
    FxConverter.cs            (UsdToEurAsync → ToEurAsync)
    DailyFxCache.cs           (string pair → string base, string quote)
  Features/Fx/Providers/
    IFxRateProvider.cs        (signature change)
    YahooFxProvider.cs        (switch → format string)
  Features/Fx/Specifications/
    LatestFxRateSpec.cs       (filter on Base/Quote)
  Features/PriceFeed/Providers/
    IPriceFeed.cs             (add GetInstrumentMetadataAsync)
    YahooPriceFeed.cs         (implement metadata method)
    YahooParser.cs            (add ParseMetadata helper)
  Features/PriceFeed/
    PriceFeedHostedService.cs (DB-driven warm loop)
  Features/Portfolio/
    PortfolioService.cs       (multi-ticker BuildSnapshot)
  Features/AiSuggestion/Snapshot/
    SnapshotFactory.cs        (read context from DB)
  Features/Dashboard/UseCases/
    LoadDashboardUseCase.cs   (Positions, ZoneCards, no hardcoded Catalog)
  Features/Dashboard/Components/
    HeroCapital.razor         (portfolio EUR; if behavior changes)
    TodaysCallCard.razor      (focus from config, not "CON3")
  Features/Trades/
    Components/AddTradeDialog.razor  (+ Instrument dropdown)
    UseCases/LogTradeUseCase.cs      (+ InstrumentId in input)
    UseCases/EditTradeUseCase.cs     (+ InstrumentId)
    Components/TradeLedger or TradesPage  (+ Ticker column)
  Features/Settings/
    SettingsPage.razor        (+ Add instrument flow; - disabled FocusTicker input)
    UseCases/UpdateGoalUseCase.cs    (- FocusTicker writes)
  Modules/
    SettingsModule.cs         (+ new use cases)
  appsettings.json            (- Tickers.Context, - Fx.Pair)
  appsettings.Development.json(- same)
TradyStrat.Tests/
  Fx/FxConverterTests.cs              (signature change)
  Fx/DailyFxCacheTests.cs             (signature change)
  Fx/Providers/YahooFxProviderTests.cs (signature change)
  PriceFeed/PriceFeedHostedServiceTests.cs (DB-driven warm)
  AiSuggestion/SnapshotFactoryTests.cs (PromptHash sentinel)
```

---

## Phase A — Additive scaffolding (Tasks 1–8)

These tasks add new code without breaking the existing build. Each ends green.

---

### Task 1: Add `Instrument` domain + EF wiring (additive)

**Files:**
- Create: `TradyStrat/Common/Domain/Instrument.cs`
- Create: `TradyStrat/Common/Domain/InstrumentKind.cs`
- Create: `TradyStrat/Common/Domain/InstrumentMetadata.cs`
- Create: `TradyStrat/Data/Configurations/InstrumentConfiguration.cs`
- Modify: `TradyStrat/Data/AppDbContext.cs`

- [ ] **Step 1: Create `InstrumentKind` enum**

```csharp
// TradyStrat/Common/Domain/InstrumentKind.cs
namespace TradyStrat.Common.Domain;

public enum InstrumentKind
{
    Held = 0,
    Watchlist = 1,
}
```

- [ ] **Step 2: Create `Instrument` entity**

```csharp
// TradyStrat/Common/Domain/Instrument.cs
namespace TradyStrat.Common.Domain;

public sealed record Instrument
{
    public required int Id { get; init; }
    public required string Ticker { get; init; }
    public required string Name { get; init; }
    public required string Currency { get; init; }     // ISO 4217, e.g. "USD", "EUR"
    public required string Exchange { get; init; }     // Yahoo fullExchangeName
    public required string TimezoneId { get; init; }   // IANA, e.g. "Europe/London"
    public required InstrumentKind Kind { get; init; }
    public required DateTime AddedAt { get; init; }
}
```

- [ ] **Step 3: Create `InstrumentMetadata` record (probe DTO)**

```csharp
// TradyStrat/Common/Domain/InstrumentMetadata.cs
namespace TradyStrat.Common.Domain;

public sealed record InstrumentMetadata(
    string Ticker,
    string Name,
    string Currency,
    string Exchange,
    string TimezoneId);
```

- [ ] **Step 4: Create `InstrumentConfiguration`**

```csharp
// TradyStrat/Data/Configurations/InstrumentConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Common.Domain;

namespace TradyStrat.Data.Configurations;

public sealed class InstrumentConfiguration : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> builder)
    {
        builder.ToTable("Instruments");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();
        builder.Property(i => i.Ticker).HasMaxLength(16).IsRequired();
        builder.Property(i => i.Name).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Currency).HasMaxLength(3).IsRequired();
        builder.Property(i => i.Exchange).HasMaxLength(64).IsRequired();
        builder.Property(i => i.TimezoneId).HasMaxLength(64).IsRequired();
        builder.Property(i => i.Kind).HasConversion<int>();
        builder.HasIndex(i => i.Ticker).IsUnique();
    }
}
```

- [ ] **Step 5: Add `Instruments` DbSet to `AppDbContext`**

```csharp
// TradyStrat/Data/AppDbContext.cs — add to the existing DbSet block
public DbSet<Instrument>  Instruments  => Set<Instrument>();
```

- [ ] **Step 6: Build to confirm additive change**

Run: `dotnet build TradyStrat.slnx`
Expected: Build succeeded with 0 errors.

- [ ] **Step 7: Commit**

```bash
git add TradyStrat/Common/Domain/Instrument.cs \
        TradyStrat/Common/Domain/InstrumentKind.cs \
        TradyStrat/Common/Domain/InstrumentMetadata.cs \
        TradyStrat/Data/Configurations/InstrumentConfiguration.cs \
        TradyStrat/Data/AppDbContext.cs
git commit -m "$(cat <<'EOF'
feat(domain): add Instrument entity + EF wiring

Additive change — no migration yet, no consumer yet. Sets up the
domain shape for the multi-ticker foundation.
EOF
)"
```

---

### Task 2: Specification classes for instruments (additive)

**Files:**
- Create: `TradyStrat/Features/Settings/Specifications/InstrumentByTickerSpec.cs`
- Create: `TradyStrat/Features/Settings/Specifications/AllInstrumentsSpec.cs`
- Create: `TradyStrat/Features/Settings/Specifications/InstrumentsByKindSpec.cs`
- Test: `TradyStrat.Tests/Settings/Specifications/InstrumentSpecTests.cs`

- [ ] **Step 1: Write the spec class for ticker lookup**

```csharp
// TradyStrat/Features/Settings/Specifications/InstrumentByTickerSpec.cs
using Ardalis.Specification;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Settings.Specifications;

public sealed class InstrumentByTickerSpec : Specification<Instrument>
{
    public InstrumentByTickerSpec(string ticker)
    {
        Query.Where(i => i.Ticker == ticker).Take(1);
    }
}
```

- [ ] **Step 2: Spec for all instruments (ordered by Ticker)**

```csharp
// TradyStrat/Features/Settings/Specifications/AllInstrumentsSpec.cs
using Ardalis.Specification;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Settings.Specifications;

public sealed class AllInstrumentsSpec : Specification<Instrument>
{
    public AllInstrumentsSpec()
    {
        Query.OrderBy(i => i.Ticker);
    }
}
```

- [ ] **Step 3: Spec for held vs watchlist**

```csharp
// TradyStrat/Features/Settings/Specifications/InstrumentsByKindSpec.cs
using Ardalis.Specification;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Settings.Specifications;

public sealed class InstrumentsByKindSpec : Specification<Instrument>
{
    public InstrumentsByKindSpec(InstrumentKind kind)
    {
        Query.Where(i => i.Kind == kind).OrderBy(i => i.Ticker);
    }
}
```

- [ ] **Step 4: Write spec tests**

```csharp
// TradyStrat.Tests/Settings/Specifications/InstrumentSpecTests.cs
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Common.Domain;
using TradyStrat.Data;
using TradyStrat.Features.Settings.Specifications;
using Xunit;

namespace TradyStrat.Tests.Settings.Specifications;

public class InstrumentSpecTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Instrument Make(string ticker, InstrumentKind kind = InstrumentKind.Held) =>
        new()
        {
            Id = 0, Ticker = ticker, Name = ticker, Currency = "USD",
            Exchange = "NMS", TimezoneId = "America/New_York",
            Kind = kind, AddedAt = DateTime.UtcNow,
        };

    [Fact]
    public async Task InstrumentByTickerSpec_finds_match()
    {
        await using var db = NewDb();
        db.Instruments.Add(Make("CON3.L"));
        db.Instruments.Add(Make("COIN", InstrumentKind.Watchlist));
        await db.SaveChangesAsync();

        var repo = new RepositoryBase<Instrument>(db);
        var hit = await repo.FirstOrDefaultAsync(new InstrumentByTickerSpec("COIN"));

        hit.ShouldNotBeNull();
        hit!.Ticker.ShouldBe("COIN");
    }

    [Fact]
    public async Task InstrumentsByKindSpec_filters_to_kind()
    {
        await using var db = NewDb();
        db.Instruments.Add(Make("CON3.L", InstrumentKind.Held));
        db.Instruments.Add(Make("COIN",   InstrumentKind.Watchlist));
        db.Instruments.Add(Make("BTC-USD",InstrumentKind.Watchlist));
        await db.SaveChangesAsync();

        var repo = new RepositoryBase<Instrument>(db);
        var watch = await repo.ListAsync(new InstrumentsByKindSpec(InstrumentKind.Watchlist));

        watch.Count.ShouldBe(2);
        watch.Select(i => i.Ticker).ShouldBe(new[] { "BTC-USD", "COIN" });
    }
}
```

- [ ] **Step 5: Run tests**

Run: `dotnet test TradyStrat.slnx --filter "FullyQualifiedName~InstrumentSpecTests"`
Expected: 2 passed.

- [ ] **Step 6: Commit**

```bash
git add TradyStrat/Features/Settings/Specifications/ \
        TradyStrat.Tests/Settings/Specifications/
git commit -m "feat(specs): add Instrument specifications + tests"
```

---

### Task 3: Add typed exceptions

**Files:**
- Create: `TradyStrat/Common/Exceptions/DuplicateInstrumentException.cs`
- Create: `TradyStrat/Common/Exceptions/InstrumentNotFoundException.cs`
- Create: `TradyStrat/Common/Exceptions/InstrumentMetadataIncompleteException.cs`
- Create: `TradyStrat/Common/Exceptions/UnsupportedCurrencyException.cs`

- [ ] **Step 1: Create the four exception classes**

```csharp
// TradyStrat/Common/Exceptions/DuplicateInstrumentException.cs
namespace TradyStrat.Common.Exceptions;

public sealed class DuplicateInstrumentException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
```

```csharp
// TradyStrat/Common/Exceptions/InstrumentNotFoundException.cs
namespace TradyStrat.Common.Exceptions;

public sealed class InstrumentNotFoundException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
```

```csharp
// TradyStrat/Common/Exceptions/InstrumentMetadataIncompleteException.cs
namespace TradyStrat.Common.Exceptions;

public sealed class InstrumentMetadataIncompleteException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
```

```csharp
// TradyStrat/Common/Exceptions/UnsupportedCurrencyException.cs
namespace TradyStrat.Common.Exceptions;

public sealed class UnsupportedCurrencyException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
```

- [ ] **Step 2: Build and commit**

Run: `dotnet build TradyStrat.slnx`
Expected: green.

```bash
git add TradyStrat/Common/Exceptions/
git commit -m "feat(exceptions): add four typed exceptions for instrument flow"
```

---

### Task 4: Yahoo metadata parsing — `GetInstrumentMetadataAsync`

**Files:**
- Modify: `TradyStrat/Features/PriceFeed/Providers/IPriceFeed.cs` (add method)
- Modify: `TradyStrat/Features/PriceFeed/Providers/YahooPriceFeed.cs` (implement method)
- Modify: `TradyStrat/Features/PriceFeed/Providers/YahooParser.cs` (add `ParseMetadata`)
- Test: `TradyStrat.Tests/Settings/Providers/YahooMetadataParserTests.cs`
- Fixture: `TradyStrat.Tests/Settings/Providers/Fixtures/yahoo-quote-eur-etp.json`
- Fixture: `TradyStrat.Tests/Settings/Providers/Fixtures/yahoo-quote-not-found.json`
- Fixture: `TradyStrat.Tests/Settings/Providers/Fixtures/yahoo-quote-incomplete.json`

> **Yahoo endpoint reference.** `GET https://query1.finance.yahoo.com/v7/finance/quote?symbols={ticker}` returns a `quoteResponse` with `result[0]` containing `symbol`, `longName` (or `shortName`), `currency`, `fullExchangeName`, `exchangeTimezoneName`. A 200 with empty `result` array means "not found." A 404 also means not found. Missing fields → metadata-incomplete.

- [ ] **Step 1: Add fixtures**

```json
// TradyStrat.Tests/Settings/Providers/Fixtures/yahoo-quote-eur-etp.json
{
  "quoteResponse": {
    "result": [
      {
        "symbol": "ETHE.PA",
        "longName": "WisdomTree Physical Ethereum",
        "shortName": "WT Physical Ethereum",
        "currency": "EUR",
        "fullExchangeName": "Euronext Paris",
        "exchangeTimezoneName": "Europe/Paris"
      }
    ],
    "error": null
  }
}
```

```json
// TradyStrat.Tests/Settings/Providers/Fixtures/yahoo-quote-not-found.json
{
  "quoteResponse": {
    "result": [],
    "error": null
  }
}
```

```json
// TradyStrat.Tests/Settings/Providers/Fixtures/yahoo-quote-incomplete.json
{
  "quoteResponse": {
    "result": [
      {
        "symbol": "WEIRD",
        "shortName": "Mystery Asset"
      }
    ],
    "error": null
  }
}
```

Mark each fixture as a test asset in `TradyStrat.Tests.csproj` if the project doesn't already glob `**/Fixtures/*.json` — check `Tests/PriceFeed/Fixtures/yahoo-con3-mini.json` for the existing convention; if it works without an explicit `<Content>` entry, do the same here.

- [ ] **Step 2: Add `ParseMetadata` to `YahooParser`**

Open `TradyStrat/Features/PriceFeed/Providers/YahooParser.cs` and add:

```csharp
public static InstrumentMetadata ParseMetadata(string ticker, JsonDocument doc)
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

        var currency = ReadString(first, "currency")
                ?? throw new InstrumentMetadataIncompleteException(
                       $"Yahoo response for '{ticker}' has no currency.");

        var exchange = ReadString(first, "fullExchangeName")
                ?? throw new InstrumentMetadataIncompleteException(
                       $"Yahoo response for '{ticker}' has no fullExchangeName.");

        var tz = ReadString(first, "exchangeTimezoneName")
                ?? throw new InstrumentMetadataIncompleteException(
                       $"Yahoo response for '{ticker}' has no exchangeTimezoneName.");

        return new InstrumentMetadata(ticker, name, currency, exchange, tz);
    }
    catch (TradyStratException) { throw; }
    catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException
                                or JsonException)
    {
        throw new PriceFeedUnavailableException(
            $"Failed to parse Yahoo metadata payload for {ticker}", ex);
    }
}

private static string? ReadString(JsonElement el, string name)
    => el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
        ? v.GetString()
        : null;
```

Add the missing `using TradyStrat.Common.Domain;` and `using TradyStrat.Common.Exceptions;` to the top of the file.

- [ ] **Step 3: Add metadata method to `IPriceFeed`**

```csharp
// TradyStrat/Features/PriceFeed/Providers/IPriceFeed.cs
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.PriceFeed.Providers;

public interface IPriceFeed
{
    Task<IReadOnlyList<PriceBar>> FetchDailyAsync(
        string ticker, DateOnly from, DateOnly to, CancellationToken ct);

    Task<InstrumentMetadata> GetInstrumentMetadataAsync(
        string ticker, CancellationToken ct);
}
```

- [ ] **Step 4: Implement metadata method in `YahooPriceFeed`**

Append to the existing class body:

```csharp
public async Task<InstrumentMetadata> GetInstrumentMetadataAsync(
    string ticker, CancellationToken ct)
{
    var url = $"/v7/finance/quote?symbols={Uri.EscapeDataString(ticker)}";

    try
    {
        using var resp = await http.GetAsync(url, ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new InstrumentNotFoundException(
                $"Yahoo 404 for '{ticker}'.");
        if (!resp.IsSuccessStatusCode)
            throw new PriceFeedUnavailableException(
                $"Yahoo {(int)resp.StatusCode} for {ticker}");

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return YahooParser.ParseMetadata(ticker, doc);
    }
    catch (TradyStratException) { throw; }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
    {
        throw new PriceFeedUnavailableException($"Yahoo metadata fetch failed for {ticker}", ex);
    }
}
```

Add `using TradyStrat.Common.Exceptions;` to the top of the file (it already imports `TradyStrat.Common.Domain` indirectly via `Common.Exceptions`).

- [ ] **Step 5: Write parser tests**

```csharp
// TradyStrat.Tests/Settings/Providers/YahooMetadataParserTests.cs
using System.Text.Json;
using Shouldly;
using TradyStrat.Common.Exceptions;
using TradyStrat.Features.PriceFeed.Providers;
using Xunit;

namespace TradyStrat.Tests.Settings.Providers;

public class YahooMetadataParserTests
{
    private static JsonDocument Load(string fixture)
        => JsonDocument.Parse(File.ReadAllText(
            Path.Combine("Settings", "Providers", "Fixtures", fixture)));

    [Fact]
    public void Parses_eur_etp_metadata()
    {
        using var doc = Load("yahoo-quote-eur-etp.json");
        var meta = YahooParser.ParseMetadata("ETHE.PA", doc);

        meta.Ticker.ShouldBe("ETHE.PA");
        meta.Name.ShouldBe("WisdomTree Physical Ethereum");
        meta.Currency.ShouldBe("EUR");
        meta.Exchange.ShouldBe("Euronext Paris");
        meta.TimezoneId.ShouldBe("Europe/Paris");
    }

    [Fact]
    public void Throws_InstrumentNotFoundException_when_result_is_empty()
    {
        using var doc = Load("yahoo-quote-not-found.json");
        Should.Throw<InstrumentNotFoundException>(
            () => YahooParser.ParseMetadata("XYZ", doc));
    }

    [Fact]
    public void Throws_InstrumentMetadataIncompleteException_when_currency_missing()
    {
        using var doc = Load("yahoo-quote-incomplete.json");
        Should.Throw<InstrumentMetadataIncompleteException>(
            () => YahooParser.ParseMetadata("WEIRD", doc));
    }
}
```

- [ ] **Step 6: Run tests**

Run: `dotnet test TradyStrat.slnx --filter "FullyQualifiedName~YahooMetadataParserTests"`
Expected: 3 passed.

- [ ] **Step 7: Commit**

```bash
git add TradyStrat/Features/PriceFeed/Providers/IPriceFeed.cs \
        TradyStrat/Features/PriceFeed/Providers/YahooPriceFeed.cs \
        TradyStrat/Features/PriceFeed/Providers/YahooParser.cs \
        TradyStrat.Tests/Settings/Providers/
git commit -m "feat(yahoo): add GetInstrumentMetadataAsync + parser tests"
```

---

### Task 5: `ProbeInstrumentUseCase` + tests

**Files:**
- Create: `TradyStrat/Features/Settings/UseCases/ProbeInstrumentUseCase.cs`
- Test: `TradyStrat.Tests/Settings/UseCases/ProbeInstrumentUseCaseTests.cs`

> **What this use case does.** Probe step is two passes: (1) fetch instrument metadata from Yahoo to learn `Currency`; (2) if `Currency != "EUR"`, ensure Yahoo serves the `EUR{Currency}=X` FX pair (one bar suffices). Both passes happen inside `ExecuteCore`. If either fails, the appropriate typed exception is thrown — nothing is persisted.

- [ ] **Step 1: Write the use case**

```csharp
// TradyStrat/Features/Settings/UseCases/ProbeInstrumentUseCase.cs
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.Fx.Providers;
using TradyStrat.Features.PriceFeed.Providers;

namespace TradyStrat.Features.Settings.UseCases;

public sealed record ProbeInstrumentInput(string Ticker);

public sealed class ProbeInstrumentUseCase(
    IPriceFeed priceFeed,
    IFxRateProvider fx,
    ILogger<ProbeInstrumentUseCase> log)
    : UseCaseBase<ProbeInstrumentInput, InstrumentMetadata>(log)
{
    protected override async Task<InstrumentMetadata> ExecuteCore(
        ProbeInstrumentInput input, CancellationToken ct)
    {
        var ticker = (input.Ticker ?? "").Trim().ToUpperInvariant();
        if (ticker.Length == 0)
            throw new InstrumentNotFoundException("Ticker must not be empty.");

        var meta = await priceFeed.GetInstrumentMetadataAsync(ticker, ct);

        if (!string.Equals(meta.Currency, "EUR", StringComparison.OrdinalIgnoreCase))
        {
            // Confirm the FX pair is reachable. We fetch a one-day window ending today;
            // the provider throws FxRateUnavailableException on transport/parse error or
            // an unsupported pair, which we translate to UnsupportedCurrencyException
            // so the UI can surface a coherent message.
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            try
            {
                _ = await fx.FetchAsync("EUR", meta.Currency, today.AddDays(-1), today, ct);
            }
            catch (FxRateUnavailableException ex)
            {
                throw new UnsupportedCurrencyException(
                    $"EUR↔{meta.Currency} FX rate is not available from Yahoo.", ex);
            }
        }

        return meta;
    }
}
```

> **Note:** `IFxRateProvider.FetchAsync(string base, string quote, ...)` is the post-Phase-1 shape. Task 9 changes the interface signature. We're writing this use case ahead of the change because the use case isn't wired yet (no caller). When Task 9 lands, this file already compiles against the new shape.

> **Build implication:** This use case **does not compile** until Task 9 lands the new `IFxRateProvider` signature. Per the conventions header, every task ends green — so we tactically work around this by writing the use case but commenting out the FX-probe block in this commit and uncommenting it in Task 9. Use the version below for this commit:

```csharp
// TradyStrat/Features/Settings/UseCases/ProbeInstrumentUseCase.cs
// (commit-this version — FX probe block stubbed pending Task 9)
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.Fx.Providers;
using TradyStrat.Features.PriceFeed.Providers;

namespace TradyStrat.Features.Settings.UseCases;

public sealed record ProbeInstrumentInput(string Ticker);

public sealed class ProbeInstrumentUseCase(
    IPriceFeed priceFeed,
    IFxRateProvider fx,
    ILogger<ProbeInstrumentUseCase> log)
    : UseCaseBase<ProbeInstrumentInput, InstrumentMetadata>(log)
{
    protected override async Task<InstrumentMetadata> ExecuteCore(
        ProbeInstrumentInput input, CancellationToken ct)
    {
        var ticker = (input.Ticker ?? "").Trim().ToUpperInvariant();
        if (ticker.Length == 0)
            throw new InstrumentNotFoundException("Ticker must not be empty.");

        var meta = await priceFeed.GetInstrumentMetadataAsync(ticker, ct);

        // FX-pair sanity check — implemented in Task 9 once IFxRateProvider takes (base, quote).
        // Until then, the use case trusts metadata and lets AddInstrumentUseCase warm best-effort.
        _ = fx; // suppress unused field warning

        return meta;
    }
}
```

- [ ] **Step 2: Write tests with hand-rolled fakes**

```csharp
// TradyStrat.Tests/Settings/UseCases/ProbeInstrumentUseCaseTests.cs
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Features.Fx.Providers;
using TradyStrat.Features.PriceFeed.Providers;
using TradyStrat.Features.Settings.UseCases;
using Xunit;

namespace TradyStrat.Tests.Settings.UseCases;

public class ProbeInstrumentUseCaseTests
{
    private sealed class FakePriceFeed(InstrumentMetadata? meta = null,
                                       Exception? throwOnMeta = null) : IPriceFeed
    {
        public Task<IReadOnlyList<Common.Domain.PriceBar>> FetchDailyAsync(
            string ticker, DateOnly from, DateOnly to, CancellationToken ct)
            => throw new NotImplementedException();

        public Task<InstrumentMetadata> GetInstrumentMetadataAsync(string ticker, CancellationToken ct)
            => throwOnMeta is null
                ? Task.FromResult(meta!)
                : Task.FromException<InstrumentMetadata>(throwOnMeta);
    }

    private sealed class FakeFxProvider : IFxRateProvider
    {
        public Task<IReadOnlyList<Common.Domain.FxRate>> FetchAsync(
            string pair, DateOnly from, DateOnly to, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<Common.Domain.FxRate>>(Array.Empty<Common.Domain.FxRate>());
    }

    [Fact]
    public async Task Returns_metadata_for_eur_instrument()
    {
        var meta = new InstrumentMetadata(
            "ETHE.PA", "WisdomTree Physical Ethereum", "EUR", "Euronext Paris", "Europe/Paris");
        var sut = new ProbeInstrumentUseCase(
            new FakePriceFeed(meta), new FakeFxProvider(),
            NullLogger<ProbeInstrumentUseCase>.Instance);

        var result = await sut.ExecuteAsync(new ProbeInstrumentInput("ethe.pa"), CancellationToken.None);

        result.ShouldBe(meta);
    }

    [Fact]
    public async Task Throws_InstrumentNotFoundException_when_ticker_is_blank()
    {
        var sut = new ProbeInstrumentUseCase(
            new FakePriceFeed(), new FakeFxProvider(),
            NullLogger<ProbeInstrumentUseCase>.Instance);

        await Should.ThrowAsync<InstrumentNotFoundException>(
            () => sut.ExecuteAsync(new ProbeInstrumentInput("  "), CancellationToken.None));
    }

    [Fact]
    public async Task Bubbles_provider_exception_unchanged()
    {
        var sut = new ProbeInstrumentUseCase(
            new FakePriceFeed(throwOnMeta: new InstrumentNotFoundException("nope")),
            new FakeFxProvider(),
            NullLogger<ProbeInstrumentUseCase>.Instance);

        await Should.ThrowAsync<InstrumentNotFoundException>(
            () => sut.ExecuteAsync(new ProbeInstrumentInput("XYZ"), CancellationToken.None));
    }
}
```

> The "FX-pair-not-resolvable surfaces UnsupportedCurrencyException" test moves to Task 9 once the provider takes `(base, quote)`.

- [ ] **Step 3: Run tests**

Run: `dotnet test TradyStrat.slnx --filter "FullyQualifiedName~ProbeInstrumentUseCaseTests"`
Expected: 3 passed.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Settings/UseCases/ProbeInstrumentUseCase.cs \
        TradyStrat.Tests/Settings/UseCases/ProbeInstrumentUseCaseTests.cs
git commit -m "feat(settings): add ProbeInstrumentUseCase (FX check stubbed pending Task 9)"
```

---

### Task 6: `ListInstrumentsUseCase`

**Files:**
- Create: `TradyStrat/Features/Settings/UseCases/ListInstrumentsUseCase.cs`
- Test: `TradyStrat.Tests/Settings/UseCases/ListInstrumentsUseCaseTests.cs`

- [ ] **Step 1: Write the use case**

```csharp
// TradyStrat/Features/Settings/UseCases/ListInstrumentsUseCase.cs
using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.Settings.Specifications;

namespace TradyStrat.Features.Settings.UseCases;

public sealed class ListInstrumentsUseCase(
    IReadRepositoryBase<Instrument> repo,
    ILogger<ListInstrumentsUseCase> log)
    : UseCaseBase<Unit, IReadOnlyList<Instrument>>(log)
{
    protected override async Task<IReadOnlyList<Instrument>> ExecuteCore(
        Unit input, CancellationToken ct)
        => await repo.ListAsync(new AllInstrumentsSpec(), ct);
}
```

- [ ] **Step 2: Test it**

```csharp
// TradyStrat.Tests/Settings/UseCases/ListInstrumentsUseCaseTests.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Common.Domain;
using TradyStrat.Common.UseCases;
using TradyStrat.Data;
using TradyStrat.Features.Settings.UseCases;
using TradyStrat.Tests.Fx;          // TestRepo<T> reuse
using Xunit;

namespace TradyStrat.Tests.Settings.UseCases;

public class ListInstrumentsUseCaseTests
{
    [Fact]
    public async Task Returns_instruments_sorted_by_ticker()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        db.Instruments.Add(Make("CON3.L"));
        db.Instruments.Add(Make("BTC-USD"));
        db.Instruments.Add(Make("COIN"));
        await db.SaveChangesAsync();

        var sut = new ListInstrumentsUseCase(
            new TestRepo<Instrument>(db),
            NullLogger<ListInstrumentsUseCase>.Instance);

        var list = await sut.ExecuteAsync(Unit.Value, CancellationToken.None);

        list.Select(i => i.Ticker).ShouldBe(new[] { "BTC-USD", "CON3.L", "COIN" });
    }

    private static Instrument Make(string ticker) => new()
    {
        Id = 0, Ticker = ticker, Name = ticker, Currency = "USD",
        Exchange = "NMS", TimezoneId = "America/New_York",
        Kind = InstrumentKind.Held, AddedAt = DateTime.UtcNow,
    };
}
```

- [ ] **Step 3: Test + commit**

Run: `dotnet test TradyStrat.slnx --filter "FullyQualifiedName~ListInstrumentsUseCaseTests"` → 1 passed.

```bash
git add TradyStrat/Features/Settings/UseCases/ListInstrumentsUseCase.cs \
        TradyStrat.Tests/Settings/UseCases/ListInstrumentsUseCaseTests.cs
git commit -m "feat(settings): add ListInstrumentsUseCase"
```

---

### Task 7: `AddInstrumentUseCase` (commit-then-warm-best-effort) + tests

**Files:**
- Create: `TradyStrat/Features/Settings/UseCases/AddInstrumentUseCase.cs`
- Test: `TradyStrat.Tests/Settings/UseCases/AddInstrumentUseCaseTests.cs`

- [ ] **Step 1: Write the use case**

```csharp
// TradyStrat/Features/Settings/UseCases/AddInstrumentUseCase.cs
using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.Time;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.Fx;
using TradyStrat.Features.PriceFeed;
using TradyStrat.Features.Settings.Specifications;

namespace TradyStrat.Features.Settings.UseCases;

public sealed record AddInstrumentInput(InstrumentMetadata Probe, InstrumentKind Kind);

public sealed partial class AddInstrumentUseCase(
    IRepositoryBase<Instrument> repo,
    DailyPriceCache priceCache,
    DailyFxCache fxCache,
    IClock clock,
    ILogger<AddInstrumentUseCase> log)
    : UseCaseBase<AddInstrumentInput, Instrument>(log)
{
    protected override async Task<Instrument> ExecuteCore(
        AddInstrumentInput input, CancellationToken ct)
    {
        var probe = input.Probe;

        var dup = await repo.FirstOrDefaultAsync(
            new InstrumentByTickerSpec(probe.Ticker), ct);
        if (dup is not null)
            throw new DuplicateInstrumentException(
                $"Instrument '{probe.Ticker}' is already tracked.");

        var entity = new Instrument
        {
            Id = 0,
            Ticker = probe.Ticker,
            Name = probe.Name,
            Currency = probe.Currency,
            Exchange = probe.Exchange,
            TimezoneId = probe.TimezoneId,
            Kind = input.Kind,
            AddedAt = clock.UtcNow(),
        };
        await repo.AddAsync(entity, ct);

        // Best-effort warm. Failures are logged + swallowed; cache self-heals next startup.
        try { await priceCache.EnsureFreshAsync(entity.Ticker, ct); }
        catch (Exception ex) { LogPriceWarmFailed(log, ex, entity.Ticker); }

        if (!string.Equals(entity.Currency, "EUR", StringComparison.OrdinalIgnoreCase))
        {
            try { await fxCache.EnsureFreshAsync("EUR", entity.Currency, ct); }
            catch (Exception ex) { LogFxWarmFailed(log, ex, entity.Currency); }
        }

        return entity;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Add-instrument: price warm failed for {Ticker}")]
    private static partial void LogPriceWarmFailed(ILogger logger, Exception ex, string ticker);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Add-instrument: FX warm failed for EUR/{Quote}")]
    private static partial void LogFxWarmFailed(ILogger logger, Exception ex, string quote);
}
```

> **Build implication:** `DailyFxCache.EnsureFreshAsync("EUR", entity.Currency, ct)` uses the post-Phase-1 two-string signature. Task 9 lands that signature. To keep this commit green, comment out the FX-warm block (single `try { … }` and the `if`) and re-enable in Task 9. Use the green-this-commit version:

```csharp
// (in ExecuteCore, replace the FX-warm if/try with this stub for now)
// FX-warm — re-enabled in Task 9 once DailyFxCache takes (base, quote).
_ = fxCache;
```

- [ ] **Step 2: Test happy path + duplicate + warm-failure**

```csharp
// TradyStrat.Tests/Settings/UseCases/AddInstrumentUseCaseTests.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.Time;
using TradyStrat.Data;
using TradyStrat.Features.Fx;
using TradyStrat.Features.PriceFeed;
using TradyStrat.Features.Settings.UseCases;
using TradyStrat.Tests.Fx;
using Xunit;

namespace TradyStrat.Tests.Settings.UseCases;

public class AddInstrumentUseCaseTests
{
    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow() => utcNow;
        public DateOnly TodayLocal() => DateOnly.FromDateTime(utcNow);
        public DateOnly TodayInExchangeTzFor(string ticker) => DateOnly.FromDateTime(utcNow);
    }

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static InstrumentMetadata Probe(string ticker, string currency = "USD") =>
        new(ticker, ticker, currency, "NMS", "America/New_York");

    [Fact]
    public async Task Inserts_instrument_on_happy_path()
    {
        await using var db = NewDb();
        var repo = new TestRepo<Instrument>(db);

        // Caches are real but their dependencies (HTTP) are not exercised because the
        // tests construct DailyPriceCache/DailyFxCache via reflection-friendly stubs.
        // Simpler approach: throw inside the warm step — the use case must swallow it.
        var priceCache = ThrowingPriceCache(db);
        var fxCache    = ThrowingFxCache(db);

        var sut = new AddInstrumentUseCase(
            repo, priceCache, fxCache,
            new FixedClock(new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)),
            NullLogger<AddInstrumentUseCase>.Instance);

        var inst = await sut.ExecuteAsync(
            new AddInstrumentInput(Probe("ETHE.PA", "EUR"), InstrumentKind.Held),
            CancellationToken.None);

        inst.Ticker.ShouldBe("ETHE.PA");
        inst.Kind.ShouldBe(InstrumentKind.Held);
        (await db.Instruments.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Throws_DuplicateInstrumentException_when_ticker_exists()
    {
        await using var db = NewDb();
        db.Instruments.Add(new Instrument
        {
            Id = 0, Ticker = "CON3.L", Name = "x", Currency = "USD",
            Exchange = "LSE", TimezoneId = "Europe/London",
            Kind = InstrumentKind.Held, AddedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var sut = new AddInstrumentUseCase(
            new TestRepo<Instrument>(db),
            ThrowingPriceCache(db), ThrowingFxCache(db),
            new FixedClock(DateTime.UtcNow),
            NullLogger<AddInstrumentUseCase>.Instance);

        await Should.ThrowAsync<DuplicateInstrumentException>(
            () => sut.ExecuteAsync(
                new AddInstrumentInput(Probe("CON3.L"), InstrumentKind.Held),
                CancellationToken.None));
    }

    [Fact]
    public async Task Warm_failure_does_not_roll_back_insert()
    {
        await using var db = NewDb();
        var repo = new TestRepo<Instrument>(db);

        var sut = new AddInstrumentUseCase(
            repo, ThrowingPriceCache(db), ThrowingFxCache(db),
            new FixedClock(DateTime.UtcNow),
            NullLogger<AddInstrumentUseCase>.Instance);

        await sut.ExecuteAsync(
            new AddInstrumentInput(Probe("XYZ", "USD"), InstrumentKind.Watchlist),
            CancellationToken.None);

        (await db.Instruments.CountAsync(i => i.Ticker == "XYZ")).ShouldBe(1);
    }

    // Helpers — construct caches whose underlying providers throw, so warm calls
    // bubble exceptions. The use case must catch them.
    private static DailyPriceCache ThrowingPriceCache(AppDbContext db)
    {
        var feed = new ThrowingPriceFeed();
        return new DailyPriceCache(feed, db, new FixedClock(DateTime.UtcNow),
            NullLogger<DailyPriceCache>.Instance);
    }

    private static DailyFxCache ThrowingFxCache(AppDbContext db)
    {
        var prov = new ThrowingFxProvider();
        return new DailyFxCache(prov, db, new FixedClock(DateTime.UtcNow),
            NullLogger<DailyFxCache>.Instance);
    }

    private sealed class ThrowingPriceFeed : Features.PriceFeed.Providers.IPriceFeed
    {
        public Task<IReadOnlyList<PriceBar>> FetchDailyAsync(
            string ticker, DateOnly from, DateOnly to, CancellationToken ct)
            => Task.FromException<IReadOnlyList<PriceBar>>(
                new PriceFeedUnavailableException("simulated"));

        public Task<InstrumentMetadata> GetInstrumentMetadataAsync(
            string ticker, CancellationToken ct)
            => Task.FromException<InstrumentMetadata>(
                new PriceFeedUnavailableException("simulated"));
    }

    private sealed class ThrowingFxProvider : Features.Fx.Providers.IFxRateProvider
    {
        public Task<IReadOnlyList<FxRate>> FetchAsync(
            string pair, DateOnly from, DateOnly to, CancellationToken ct)
            => Task.FromException<IReadOnlyList<FxRate>>(
                new FxRateUnavailableException("simulated"));
    }
}
```

> **Note:** the FX-warm test assertion is loose right now (we don't assert it's even called) because the FX-warm block is stubbed out in the use case until Task 9. Task 9 sharpens this test by enabling the stubbed block and changing the FxProvider signature.

- [ ] **Step 3: Run tests**

Run: `dotnet test TradyStrat.slnx --filter "FullyQualifiedName~AddInstrumentUseCaseTests"` → 3 passed.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Settings/UseCases/AddInstrumentUseCase.cs \
        TradyStrat.Tests/Settings/UseCases/AddInstrumentUseCaseTests.cs
git commit -m "feat(settings): add AddInstrumentUseCase + commit-then-warm tests"
```

---

### Task 8: Wire new use cases + Add-instrument Settings UI

**Files:**
- Modify: `TradyStrat/Modules/SettingsModule.cs`
- Create: `TradyStrat/Features/Settings/Components/AddInstrumentForm.razor`
- Modify: `TradyStrat/Features/Settings/SettingsPage.razor`

- [ ] **Step 1: Register the new use cases**

```csharp
// TradyStrat/Modules/SettingsModule.cs
using TheAppManager.Modules;
using TradyStrat.Features.Settings.UseCases;

namespace TradyStrat.Modules;

public sealed class SettingsModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<UpdateGoalUseCase>();
        builder.Services.AddScoped<ProbeInstrumentUseCase>();
        builder.Services.AddScoped<AddInstrumentUseCase>();
        builder.Services.AddScoped<ListInstrumentsUseCase>();
    }
}
```

- [ ] **Step 2: Create the `AddInstrumentForm` Razor component**

```razor
@* TradyStrat/Features/Settings/Components/AddInstrumentForm.razor *@
@using TradyStrat.Common.Domain
@using TradyStrat.Common.Exceptions
@using TradyStrat.Common.UseCases
@using TradyStrat.Features.Settings.UseCases
@inject ProbeInstrumentUseCase Probe
@inject AddInstrumentUseCase Add

<section class="add-instrument">
    <h3>Add instrument</h3>
    <div class="grid">
        <label class="field">
            <span class="lbl">Ticker</span>
            <input value="@_ticker"
                   @oninput="e => { _ticker = e.Value?.ToString() ?? string.Empty; _probed = null; _err = null; }"
                   placeholder="e.g. ETHE.PA" />
        </label>
        <button class="btn" @onclick="DoProbe" disabled="@(_busy || string.IsNullOrWhiteSpace(_ticker))">
            @(_busy && _probed is null ? "Probing…" : "Probe")
        </button>
    </div>

    @if (_probed is { } meta)
    {
        <dl class="probe-result">
            <dt>Found</dt>
            <dd>@meta.Name</dd>
            <dt>Currency</dt>
            <dd>@meta.Currency</dd>
            <dt>Exchange</dt>
            <dd>@meta.Exchange</dd>
            <dt>Timezone</dt>
            <dd>@meta.TimezoneId</dd>
        </dl>
        <div class="kind-pick">
            <label><input type="radio" name="kind"
                          checked="@(_kind == InstrumentKind.Held)"
                          @onchange="@(() => _kind = InstrumentKind.Held)" /> Held</label>
            <label><input type="radio" name="kind"
                          checked="@(_kind == InstrumentKind.Watchlist)"
                          @onchange="@(() => _kind = InstrumentKind.Watchlist)" /> Watchlist</label>
        </div>
        <div class="actions">
            <button class="btn" @onclick="DoAdd" disabled="@_busy">@(_busy ? "Adding…" : "Add")</button>
            <button class="btn ghost" @onclick="Reset">Cancel</button>
        </div>
    }

    @if (!string.IsNullOrEmpty(_err))
    {
        <p class="err">@_err</p>
    }
    @if (!string.IsNullOrEmpty(_ok))
    {
        <p class="ok">@_ok</p>
    }
</section>

@code {
    [Parameter] public EventCallback OnAdded { get; set; }

    private string _ticker = "";
    private InstrumentMetadata? _probed;
    private InstrumentKind _kind = InstrumentKind.Held;
    private bool _busy;
    private string? _err;
    private string? _ok;

    private async Task DoProbe()
    {
        _busy = true; _err = null; _ok = null;
        try
        {
            _probed = await Probe.ExecuteAsync(new ProbeInstrumentInput(_ticker), CancellationToken.None);
        }
        catch (TradyStratException ex) { _err = ex.Message; }
        finally { _busy = false; }
    }

    private async Task DoAdd()
    {
        if (_probed is null) return;
        _busy = true; _err = null; _ok = null;
        try
        {
            var inst = await Add.ExecuteAsync(
                new AddInstrumentInput(_probed, _kind), CancellationToken.None);
            _ok = $"Added {inst.Ticker} as {inst.Kind}.";
            Reset();
            await OnAdded.InvokeAsync();
        }
        catch (TradyStratException ex) { _err = ex.Message; }
        finally { _busy = false; }
    }

    private void Reset()
    {
        _ticker = ""; _probed = null; _kind = InstrumentKind.Held;
    }
}
```

- [ ] **Step 3: Mount the component in `SettingsPage.razor`**

Open `TradyStrat/Features/Settings/SettingsPage.razor` and:
1. Add `@using TradyStrat.Features.Settings.Components` near the top.
2. **Remove** the `<label class="field">…Focus ticker…</label>` block (the disabled input).
3. Append the `<AddInstrumentForm />` block under the Goal grid:

```razor
@* …existing Goal grid + actions… *@

<AddInstrumentForm />
```

- [ ] **Step 4: Build and run a quick sanity check**

Run: `dotnet build TradyStrat.slnx`
Expected: green.

The Settings page now renders the Add-instrument form. The form is wired but the `DailyFxCache.EnsureFreshAsync` call inside `AddInstrumentUseCase` is still stubbed (Task 9 finishes that wiring). Pre-Task-9 behavior: probing still works for any ticker Yahoo serves; adding inserts the row but never warms FX (the FX-warm block is commented out). That's acceptable for an additive intermediate state.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Modules/SettingsModule.cs \
        TradyStrat/Features/Settings/Components/AddInstrumentForm.razor \
        TradyStrat/Features/Settings/SettingsPage.razor
git commit -m "feat(settings): add probe-and-confirm Add-instrument UI"
```

---

## Phase B — Schema migration & adapter widening (Tasks 9–10)

These are the load-bearing changes. Task 9 is large and atomic — it lands the schema migration *and* the FX provider/cache/converter signature changes in one commit, because they cannot be split without breaking the build.

---

### Task 9: Schema + FX adapter widening (the big one)

**Sub-deliverables (all in one commit):**

1. `FxRate` entity: rename `Pair` → `Base` + `Quote`, rename `UsdPerEur` → `Rate`. Drop the `EurPerUsd` derived getter (no longer canonical).
2. `FxRateConfiguration`: rebuild column mappings + UQ index.
3. `LatestFxRateSpec`: take `(Base, Quote, asOf)`.
4. `IFxRateProvider.FetchAsync`: take `(string base, string quote, …)`.
5. `YahooFxProvider`: switch → format string `$"{base}{quote}=X"`.
6. `DailyFxCache.EnsureFreshAsync`: take `(string base, string quote, …)`.
7. `FxConverter`: `UsdToEurAsync` → `ToEurAsync(decimal amount, string fromCurrency, DateOnly asOf, CancellationToken ct)`.
8. Update FxConverter callers: `SnapshotFactory` (2 sites pass `"USD"`), `LoadDashboardUseCase` (1 site passes `"USD"`).
9. `Trade` entity gains `InstrumentId`. `TradeConfiguration` adds the column + index.
10. `LogTradeUseCase` and `EditTradeUseCase` accept an `InstrumentId` parameter.
11. `GoalConfig` entity loses `FocusTicker`. `GoalConfigConfiguration` drops the mapping. `UpdateGoalUseCase` stops writing it. `GoalConfig.Default(now)` updated.
12. EF migration generated and hand-edited:
    - Create `Instruments` table.
    - `InsertData` seed: CON3.L (Held), COIN (Watchlist), BTC-USD (Watchlist).
    - `Trades.InstrumentId` added; `UPDATE Trades SET InstrumentId = (SELECT Id FROM Instruments WHERE Ticker='CON3.L')`; column made NOT NULL with FK.
    - `FxRates` rebuilt (Pair → Base, Quote; UsdPerEur → Rate); existing rows mapped `Base='EUR', Quote='USD', Rate=UsdPerEur`.
    - `Goals.FocusTicker` dropped.
13. `appsettings.Tickers.Context` and `appsettings.Fx.Pair` deleted.
14. Existing tests updated for new signatures (FxConverterTests, DailyFxCacheTests, YahooFxProviderTests, PriceFeedHostedServiceTests).
15. New `MultiTickerMigrationTests` integration test against SQLite `:memory:`.
16. The previously-stubbed FX blocks in `ProbeInstrumentUseCase` and `AddInstrumentUseCase` are re-enabled.

> **Why one commit?** Splitting into more commits forces intermediate broken-build states. The compiler will refuse partial completion of any of points 1–8 because they're a single signature change rippling across the FX stack. Spec §4 acknowledged this: "Code-side cleanup must accompany this migration."

---

#### 9.1 Domain model edits

- [ ] **Step 1: Update `FxRate`**

Replace the entire file with:

```csharp
// TradyStrat/Common/Domain/FxRate.cs
namespace TradyStrat.Common.Domain;

public sealed record FxRate
{
    public required int Id { get; init; }
    public required DateOnly Date { get; init; }
    public required string Base { get; init; }     // ISO 4217, e.g. "EUR"
    public required string Quote { get; init; }    // ISO 4217, e.g. "USD"
    public required decimal Rate { get; init; }    // Quote per 1 Base
    public required DateTime FetchedAt { get; init; }
}
```

- [ ] **Step 2: Update `Trade`**

Add `InstrumentId` to the entity (`Trade.cs`):

```csharp
// at the top of the property block
public required int InstrumentId { get; init; }
```

- [ ] **Step 3: Update `GoalConfig`**

Remove `FocusTicker` from `GoalConfig.cs`. The `Default` factory becomes:

```csharp
public static GoalConfig Default(DateTime now) => new()
{
    Id = 1,
    TargetEur = 1_000_000m,
    TargetDate = null,
    UpdatedAt = now,
};
```

#### 9.2 EF configuration edits

- [ ] **Step 4: Update `FxRateConfiguration`**

```csharp
// TradyStrat/Data/Configurations/FxRateConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Common.Domain;

namespace TradyStrat.Data.Configurations;

public sealed class FxRateConfiguration : IEntityTypeConfiguration<FxRate>
{
    public void Configure(EntityTypeBuilder<FxRate> builder)
    {
        builder.ToTable("FxRates");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();
        builder.Property(r => r.Base).HasMaxLength(3).IsRequired();
        builder.Property(r => r.Quote).HasMaxLength(3).IsRequired();
        builder.Property(r => r.Rate).HasColumnType("TEXT");
        builder.HasIndex(r => new { r.Base, r.Quote, r.Date }).IsUnique();
    }
}
```

- [ ] **Step 5: Update `TradeConfiguration`**

```csharp
// TradyStrat/Data/Configurations/TradeConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Common.Domain;

namespace TradyStrat.Data.Configurations;

public sealed class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("Trades");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();
        builder.Property(t => t.Quantity).HasColumnType("TEXT");
        builder.Property(t => t.PricePerShare).HasColumnType("TEXT");
        builder.Property(t => t.FeesEur).HasColumnType("TEXT");
        builder.Property(t => t.Note).HasMaxLength(2000);
        builder.HasOne<Instrument>()
               .WithMany()
               .HasForeignKey(t => t.InstrumentId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.ExecutedOn);
        builder.HasIndex(t => new { t.InstrumentId, t.ExecutedOn });
        builder.Ignore(t => t.GrossEur);
        builder.Ignore(t => t.NetEur);
        builder.Ignore(t => t.IsBuy);
    }
}
```

- [ ] **Step 6: Update `GoalConfigConfiguration`**

Remove the `FocusTicker` property line:

```csharp
// TradyStrat/Data/Configurations/GoalConfigConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Common.Domain;

namespace TradyStrat.Data.Configurations;

public sealed class GoalConfigConfiguration : IEntityTypeConfiguration<GoalConfig>
{
    public void Configure(EntityTypeBuilder<GoalConfig> builder)
    {
        builder.ToTable("Goals");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();
        builder.Property(g => g.TargetEur).HasColumnType("TEXT");
    }
}
```

#### 9.3 Specification + provider + cache + converter edits

- [ ] **Step 7: Update `LatestFxRateSpec`**

```csharp
// TradyStrat/Features/Fx/Specifications/LatestFxRateSpec.cs
using Ardalis.Specification;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Fx.Specifications;

public sealed class LatestFxRateSpec : Specification<FxRate>
{
    public LatestFxRateSpec(string @base, string quote, DateOnly asOf)
    {
        Query.Where(r => r.Base == @base && r.Quote == quote && r.Date <= asOf)
             .OrderByDescending(r => r.Date)
             .Take(1);
    }
}
```

- [ ] **Step 8: Update `IFxRateProvider`**

```csharp
// TradyStrat/Features/Fx/Providers/IFxRateProvider.cs
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Fx.Providers;

public interface IFxRateProvider
{
    Task<IReadOnlyList<FxRate>> FetchAsync(
        string @base, string quote, DateOnly from, DateOnly to, CancellationToken ct);
}
```

- [ ] **Step 9: Update `YahooFxProvider`**

```csharp
// TradyStrat/Features/Fx/Providers/YahooFxProvider.cs
using System.Text.Json;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;

namespace TradyStrat.Features.Fx.Providers;

public sealed class YahooFxProvider(HttpClient http) : IFxRateProvider
{
    public async Task<IReadOnlyList<FxRate>> FetchAsync(
        string @base, string quote, DateOnly from, DateOnly to, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(@base) || string.IsNullOrWhiteSpace(quote))
            throw new FxRateUnavailableException("Base and quote must not be empty.");

        var symbol = $"{@base.ToUpperInvariant()}{quote.ToUpperInvariant()}=X";

        var p1 = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeSeconds();
        var p2 = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue),   TimeSpan.Zero).ToUnixTimeSeconds();
        var url = $"/v8/finance/chart/{symbol}?period1={p1}&period2={p2}&interval=1d";

        try
        {
            using var resp = await http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
                throw new FxRateUnavailableException($"Yahoo {(int)resp.StatusCode} for {symbol}");

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var first = doc.RootElement.GetProperty("chart").GetProperty("result")[0];
            var ts    = first.GetProperty("timestamp");
            var close = first.GetProperty("indicators").GetProperty("quote")[0].GetProperty("close");

            var fetchedAt = DateTime.UtcNow;
            var rates = new List<FxRate>(ts.GetArrayLength());
            for (var i = 0; i < ts.GetArrayLength(); i++)
            {
                if (close[i].ValueKind == JsonValueKind.Null) continue;
                var date = DateOnly.FromDateTime(
                    DateTimeOffset.FromUnixTimeSeconds(ts[i].GetInt64()).UtcDateTime);
                rates.Add(new FxRate
                {
                    Id = 0,
                    Date = date,
                    Base = @base.ToUpperInvariant(),
                    Quote = quote.ToUpperInvariant(),
                    Rate = (decimal)close[i].GetDouble(),
                    FetchedAt = fetchedAt,
                });
            }
            return rates;
        }
        catch (FxRateUnavailableException) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                    or JsonException or KeyNotFoundException
                                    or InvalidOperationException)
        {
            throw new FxRateUnavailableException($"FX fetch failed for {symbol}", ex);
        }
    }
}
```

- [ ] **Step 10: Update `DailyFxCache`**

```csharp
// TradyStrat/Features/Fx/DailyFxCache.cs
using Microsoft.EntityFrameworkCore;
using TradyStrat.Common.Time;
using TradyStrat.Data;
using TradyStrat.Features.Fx.Providers;

namespace TradyStrat.Features.Fx;

public sealed partial class DailyFxCache(
    IFxRateProvider provider,
    AppDbContext db,
    IClock clock,
    ILogger<DailyFxCache> log)
{
    public async Task EnsureFreshAsync(string @base, string quote, CancellationToken ct)
    {
        // FX trades 24/5 in UTC; the existing pair-keyed timezone fallback to UTC is fine.
        var today = clock.TodayInExchangeTzFor($"{@base}{quote}");
        var latest = await db.FxRates
            .Where(r => r.Base == @base && r.Quote == quote)
            .OrderByDescending(r => r.Date)
            .Select(r => (DateOnly?)r.Date)
            .FirstOrDefaultAsync(ct);

        if (latest == today) return;

        var from = latest?.AddDays(1) ?? today.AddYears(-2);
        var rates = await provider.FetchAsync(@base, quote, from, today, ct);
        if (rates.Count == 0)
        {
            LogNoRates(log, @base, quote);
            return;
        }

        var fetchedDates = rates.Select(r => r.Date).ToList();
        var existingDates = await db.FxRates
            .Where(r => r.Base == @base && r.Quote == quote && fetchedDates.Contains(r.Date))
            .Select(r => r.Date)
            .ToListAsync(ct);
        var existingSet = existingDates.ToHashSet();
        var newRates = rates.Where(r => !existingSet.Contains(r.Date)).ToList();
        if (newRates.Count == 0)
        {
            LogNoRates(log, @base, quote);
            return;
        }

        db.FxRates.AddRange(newRates);
        await db.SaveChangesAsync(ct);
        LogAppended(log, newRates.Count, @base, quote);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Fx: no new rates for {Base}/{Quote}")]
    private static partial void LogNoRates(ILogger logger, string @base, string quote);

    [LoggerMessage(Level = LogLevel.Information, Message = "Fx: appended {N} rates for {Base}/{Quote}")]
    private static partial void LogAppended(ILogger logger, int n, string @base, string quote);
}
```

- [ ] **Step 11: Update `FxConverter`**

```csharp
// TradyStrat/Features/Fx/FxConverter.cs
using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Features.Fx.Specifications;

namespace TradyStrat.Features.Fx;

public sealed class FxConverter(IReadRepositoryBase<FxRate> rates)
{
    public async Task<decimal> ToEurAsync(
        decimal amount, string fromCurrency, DateOnly asOf, CancellationToken ct)
    {
        var ccy = (fromCurrency ?? "").Trim().ToUpperInvariant();
        if (ccy.Length == 0)
            throw new FxRateUnavailableException("Currency must not be empty.");
        if (ccy == "EUR") return amount;

        var fx = await rates.FirstOrDefaultAsync(new LatestFxRateSpec("EUR", ccy, asOf), ct)
            ?? throw new FxRateUnavailableException(
                $"No EUR/{ccy} rate on or before {asOf:yyyy-MM-dd}");
        // Rate = Quote per 1 Base. With Base=EUR, Quote=ccy:
        //   to convert N ccy to EUR -> N / Rate.
        return amount / fx.Rate;
    }
}
```

#### 9.4 Update FxConverter callers

- [ ] **Step 12: Update `SnapshotFactory`**

In `TradyStrat/Features/AiSuggestion/Snapshot/SnapshotFactory.cs`, replace the two call sites:

```csharp
// Site 1 — inside the foreach loop:
if (currency == "USD")
    eur = await fx.ToEurAsync(reading.Price, currency, asOf, ct);
//  (was: eur = await fx.UsdToEurAsync(reading.Price, asOf, ct);)

// Site 2 — the usdPerEur derivation block. Replace the block with:
decimal? usdPerEur = null;
try
{
    var oneUsdInEur = await fx.ToEurAsync(1m, "USD", asOf, ct);
    if (oneUsdInEur != 0m) usdPerEur = 1m / oneUsdInEur;
}
catch (FxRateUnavailableException)
{
    // Tolerant — snapshot can be built without the FX rate present.
}
```

(Remove the now-unused `oneEurInEur` line.)

- [ ] **Step 13: Update `LoadDashboardUseCase`**

In the Catalog loop, replace:

```csharp
if (currency == "USD")
    eur = await fx.UsdToEurAsync(reading.Price, target, ct);
```

with:

```csharp
if (currency != "EUR")
    eur = await fx.ToEurAsync(reading.Price, currency, target, ct);
```

(The eventual full rewrite of this use case happens in Task 13. For now, just rename the call.)

#### 9.5 Trade write-path edits

- [ ] **Step 14: Update `LogTradeUseCase` and `EditTradeUseCase`**

These two use cases accept an `InstrumentId` parameter on their input record. Open each file:

`TradyStrat/Features/Trades/UseCases/LogTradeUseCase.cs` — add `int InstrumentId` to `LogTradeInput` (likely a `record`) and pass it through to the constructed `Trade` entity. Same shape for `EditTradeUseCase`.

- [ ] **Step 15: Update `UpdateGoalUseCase`**

Open `TradyStrat/Features/Settings/UseCases/UpdateGoalUseCase.cs` and remove the `FocusTicker = "CON3.L"` line from the insert path. The `with { … }` update path doesn't reference FocusTicker, so it's already fine.

#### 9.6 Generate the migration

- [ ] **Step 16: Generate the migration**

Run:
```bash
dotnet ef migrations add MultiTickerFoundation --project TradyStrat --output-dir Data/Migrations
```

EF will produce `Data/Migrations/<timestamp>_MultiTickerFoundation.cs` with the schema diffs. The DROP/CREATE for table rebuilds (FxRates, Goals) will be automatic.

- [ ] **Step 17: Hand-edit the migration body**

Open the generated `<timestamp>_MultiTickerFoundation.cs`. The auto-generated body will:
- Create `Instruments` table.
- Add `InstrumentId` to `Trades` (nullable initially, since EF can't know what value to use).
- Drop and recreate `FxRates` and `Goals`.

Hand-edit so the body is:

```csharp
public partial class MultiTickerFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Instruments table.
        migrationBuilder.CreateTable(
            name: "Instruments",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Ticker = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                Exchange = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                TimezoneId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Kind = table.Column<int>(type: "INTEGER", nullable: false),
                AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
            },
            constraints: t => t.PrimaryKey("PK_Instruments", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_Instruments_Ticker",
            table: "Instruments",
            column: "Ticker",
            unique: true);

        // 2. Seed the three known instruments.
        var seedAt = DateTime.UtcNow;
        migrationBuilder.InsertData(
            table: "Instruments",
            columns: new[] { "Ticker", "Name", "Currency", "Exchange", "TimezoneId", "Kind", "AddedAt" },
            values: new object[,]
            {
                { "CON3.L",  "Leverage Shares 3x Long Coinbase", "USD", "LSE", "Europe/London",     0, seedAt },
                { "COIN",    "Coinbase Global, Inc.",            "USD", "NMS", "America/New_York",  1, seedAt },
                { "BTC-USD", "Bitcoin USD",                      "USD", "CCC", "UTC",               1, seedAt },
            });

        // 3. Trades.InstrumentId — added nullable, backfilled, then made NOT NULL.
        migrationBuilder.AddColumn<int>(
            name: "InstrumentId",
            table: "Trades",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.Sql(@"
            UPDATE Trades
               SET InstrumentId = (SELECT Id FROM Instruments WHERE Ticker = 'CON3.L')
             WHERE InstrumentId IS NULL;");

        // SQLite doesn't allow ALTER COLUMN; the table-rebuild operation EF emits
        // for changing nullability follows.
        migrationBuilder.AlterColumn<int>(
            name: "InstrumentId",
            table: "Trades",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Trades_InstrumentId_ExecutedOn",
            table: "Trades",
            columns: new[] { "InstrumentId", "ExecutedOn" });

        migrationBuilder.AddForeignKey(
            name: "FK_Trades_Instruments_InstrumentId",
            table: "Trades",
            column: "InstrumentId",
            principalTable: "Instruments",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        // 4. FxRates rebuild — Pair → (Base, Quote); UsdPerEur → Rate.
        // EF emits a table-rebuild via DROP+CREATE for the schema change. We supply
        // an INSERT that maps existing rows from the temporary copy into the new shape.
        // (If EF's auto-generated body uses a different intermediate table name, copy
        // the temporary table name from the generated file into the SQL below.)
        migrationBuilder.Sql(@"
            CREATE TABLE FxRates_New (
                Id INTEGER NOT NULL CONSTRAINT PK_FxRates PRIMARY KEY AUTOINCREMENT,
                Date TEXT NOT NULL,
                Base TEXT NOT NULL,
                Quote TEXT NOT NULL,
                Rate TEXT NOT NULL,
                FetchedAt TEXT NOT NULL
            );

            INSERT INTO FxRates_New (Id, Date, Base, Quote, Rate, FetchedAt)
            SELECT Id, Date, 'EUR', 'USD', UsdPerEur, FetchedAt FROM FxRates;

            DROP TABLE FxRates;
            ALTER TABLE FxRates_New RENAME TO FxRates;
        ");

        migrationBuilder.CreateIndex(
            name: "IX_FxRates_Base_Quote_Date",
            table: "FxRates",
            columns: new[] { "Base", "Quote", "Date" },
            unique: true);

        // 5. Goals — drop FocusTicker via SQLite table-rebuild.
        migrationBuilder.Sql(@"
            CREATE TABLE Goals_New (
                Id INTEGER NOT NULL CONSTRAINT PK_Goals PRIMARY KEY,
                TargetEur TEXT NOT NULL,
                TargetDate TEXT NULL,
                UpdatedAt TEXT NOT NULL
            );

            INSERT INTO Goals_New (Id, TargetEur, TargetDate, UpdatedAt)
            SELECT Id, TargetEur, TargetDate, UpdatedAt FROM Goals;

            DROP TABLE Goals;
            ALTER TABLE Goals_New RENAME TO Goals;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => throw new NotSupportedException(
            "Phase 1 multi-ticker migration is forward-only. Restore from a pre-migration DB copy.");
}
```

> **Important:** if the EF-generated migration body conflicts with the hand-edited block above (e.g., generates its own DROP/CREATE for FxRates with a different intermediate table name), **delete the auto-generated body and replace it with the version above**. The auto-generated `Designer.cs` and `AppDbContextModelSnapshot.cs` files **must** be kept (they encode the new model state) — only edit the `Up`/`Down` methods.

If the generated migration's auto-rebuild block for `FxRates`/`Goals` uses a sensible default backfill (e.g., empty strings for new NOT NULL columns), it will be wrong — our values must be `'EUR'`, `'USD'`, and the existing `UsdPerEur` value. The custom SQL above is canonical.

- [ ] **Step 18: Re-enable the previously stubbed FX-warm blocks**

Open `TradyStrat/Features/Settings/UseCases/AddInstrumentUseCase.cs`. Replace the stub:

```csharp
_ = fxCache;
```

with the real block:

```csharp
if (!string.Equals(entity.Currency, "EUR", StringComparison.OrdinalIgnoreCase))
{
    try { await fxCache.EnsureFreshAsync("EUR", entity.Currency, ct); }
    catch (Exception ex) { LogFxWarmFailed(log, ex, entity.Currency); }
}
```

Open `TradyStrat/Features/Settings/UseCases/ProbeInstrumentUseCase.cs`. Replace:

```csharp
_ = fx;
```

with the real block:

```csharp
if (!string.Equals(meta.Currency, "EUR", StringComparison.OrdinalIgnoreCase))
{
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    try
    {
        _ = await fx.FetchAsync("EUR", meta.Currency, today.AddDays(-1), today, ct);
    }
    catch (FxRateUnavailableException ex)
    {
        throw new UnsupportedCurrencyException(
            $"EUR↔{meta.Currency} FX rate is not available from Yahoo.", ex);
    }
}
```

#### 9.7 Update existing tests for new signatures

- [ ] **Step 19: Update FX tests**

`TradyStrat.Tests/Fx/FxConverterTests.cs` — every `UsdToEurAsync(amount, asOf, ct)` becomes `ToEurAsync(amount, "USD", asOf, ct)`. Every constructed `FxRate` now has `Base = "EUR"`, `Quote = "USD"`, `Rate = …` instead of `Pair = "EURUSD"`, `UsdPerEur = …`.

Add one new test:

```csharp
[Fact]
public async Task ToEurAsync_passes_through_when_currency_is_eur()
{
    await using var db = NewDb();   // helper from existing tests
    var sut = new FxConverter(new TestRepo<FxRate>(db));
    var result = await sut.ToEurAsync(123.45m, "EUR",
        new DateOnly(2026, 5, 1), CancellationToken.None);
    result.ShouldBe(123.45m);
}
```

`TradyStrat.Tests/Fx/DailyFxCacheTests.cs` — `EnsureFreshAsync(pair, ct)` becomes `EnsureFreshAsync(base, quote, ct)`.

`TradyStrat.Tests/Fx/Providers/YahooFxProviderTests.cs` — `FetchAsync(pair, …)` becomes `FetchAsync(@base, quote, …)`.

`TradyStrat.Tests/PriceFeed/PriceFeedHostedServiceTests.cs` — see Task 10. For now, accept that this test file needs updates that come in Task 10. If keeping it green here is annoying, mark the test class with `[Trait("Skip", "Task10")]` and re-enable in Task 10. Cleaner: do Task 10's test edits in this same commit since `PriceFeedHostedService` itself changes in Task 10.

- [ ] **Step 20: Update `AddInstrumentUseCaseTests`**

Now that the FX-warm block is real, sharpen the third test:

```csharp
[Fact]
public async Task Warm_failure_does_not_roll_back_insert_for_non_eur()
{
    await using var db = NewDb();
    var repo = new TestRepo<Instrument>(db);

    var sut = new AddInstrumentUseCase(
        repo, ThrowingPriceCache(db), ThrowingFxCache(db),
        new FixedClock(DateTime.UtcNow),
        NullLogger<AddInstrumentUseCase>.Instance);

    await sut.ExecuteAsync(
        new AddInstrumentInput(Probe("XYZ", "USD"), InstrumentKind.Watchlist),
        CancellationToken.None);

    (await db.Instruments.CountAsync(i => i.Ticker == "XYZ")).ShouldBe(1);
}

[Fact]
public async Task Eur_instrument_skips_fx_warm()
{
    // The fact that ThrowingFxCache would throw, but the test passes,
    // proves the EUR branch skips the FX warm path entirely.
    await using var db = NewDb();
    var sut = new AddInstrumentUseCase(
        new TestRepo<Instrument>(db),
        ThrowingPriceCache(db), ThrowingFxCache(db),
        new FixedClock(DateTime.UtcNow),
        NullLogger<AddInstrumentUseCase>.Instance);

    await sut.ExecuteAsync(
        new AddInstrumentInput(Probe("ETHE.PA", "EUR"), InstrumentKind.Held),
        CancellationToken.None);

    (await db.Instruments.CountAsync()).ShouldBe(1);
}
```

#### 9.8 Migration integration test

- [ ] **Step 21: Add `MultiTickerMigrationTests`**

```csharp
// TradyStrat.Tests/Data/MultiTickerMigrationTests.cs
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Common.Domain;
using TradyStrat.Data;
using Xunit;

namespace TradyStrat.Tests.Data;

public class MultiTickerMigrationTests
{
    [Fact]
    public async Task Migration_creates_Instruments_table_with_three_seeded_rows()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn).Options;

        await using var db = new AppDbContext(opts);
        await db.Database.MigrateAsync();

        var instruments = await db.Instruments.OrderBy(i => i.Ticker).ToListAsync();
        instruments.Select(i => i.Ticker).ShouldBe(new[] { "BTC-USD", "CON3.L", "COIN" });
        instruments.Single(i => i.Ticker == "CON3.L").Kind.ShouldBe(InstrumentKind.Held);
        instruments.Single(i => i.Ticker == "COIN").Kind.ShouldBe(InstrumentKind.Watchlist);
    }

    [Fact]
    public async Task Goals_table_no_longer_has_FocusTicker_column()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn).Options;

        await using var db = new AppDbContext(opts);
        await db.Database.MigrateAsync();

        // PRAGMA table_info reveals the columns. FocusTicker must not appear.
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA table_info(Goals);";
        var cols = new List<string>();
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync())
            cols.Add(rdr.GetString(1)); // column 1 is "name"

        cols.ShouldContain("TargetEur");
        cols.ShouldNotContain("FocusTicker");
    }

    [Fact]
    public async Task FxRates_table_has_Base_Quote_Rate_columns()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn).Options;

        await using var db = new AppDbContext(opts);
        await db.Database.MigrateAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA table_info(FxRates);";
        var cols = new List<string>();
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync())
            cols.Add(rdr.GetString(1));

        cols.ShouldContain("Base");
        cols.ShouldContain("Quote");
        cols.ShouldContain("Rate");
        cols.ShouldNotContain("Pair");
        cols.ShouldNotContain("UsdPerEur");
    }
}
```

This requires `Microsoft.Data.Sqlite` in the test project. It's transitively present via `Microsoft.EntityFrameworkCore.Sqlite` already referenced by the main project, but if the test project doesn't reference it directly, add a `<PackageReference Include="Microsoft.Data.Sqlite" />` to `TradyStrat.Tests.csproj` and a corresponding `<PackageVersion>` line in `Directory.Packages.props` (use the same `10.0.7` version).

#### 9.9 Cleanup config

- [ ] **Step 22: Delete dead config keys**

In `TradyStrat/appsettings.json`, remove the entire `Tickers.Context` array entry and the `Fx.Pair` key:

Before:
```json
"Tickers": { "Focus": "CON3.L", "Context": ["COIN", "BTC-USD"] },
"Fx":      { "Pair": "EURUSD" },
```

After:
```json
"Tickers": { "Focus": "CON3.L" },
```

Apply the same change to `TradyStrat/appsettings.Development.json` if those keys exist there.

#### 9.10 Build & test

- [ ] **Step 23: Build, test, commit**

Run:
```bash
dotnet build TradyStrat.slnx
dotnet test  TradyStrat.slnx
```
Expected: all green.

```bash
git add TradyStrat/Common/Domain/FxRate.cs \
        TradyStrat/Common/Domain/Trade.cs \
        TradyStrat/Common/Domain/GoalConfig.cs \
        TradyStrat/Data/Configurations/FxRateConfiguration.cs \
        TradyStrat/Data/Configurations/TradeConfiguration.cs \
        TradyStrat/Data/Configurations/GoalConfigConfiguration.cs \
        TradyStrat/Data/Migrations/ \
        TradyStrat/Features/Fx/ \
        TradyStrat/Features/Settings/UseCases/ \
        TradyStrat/Features/Trades/UseCases/ \
        TradyStrat/Features/AiSuggestion/Snapshot/SnapshotFactory.cs \
        TradyStrat/Features/Dashboard/UseCases/LoadDashboardUseCase.cs \
        TradyStrat/appsettings.json TradyStrat/appsettings.Development.json \
        TradyStrat.Tests/Fx/ TradyStrat.Tests/Data/ \
        TradyStrat.Tests/Settings/UseCases/AddInstrumentUseCaseTests.cs \
        Directory.Packages.props TradyStrat.Tests/TradyStrat.Tests.csproj
git commit -m "$(cat <<'EOF'
feat(schema): widen FX stack + add multi-ticker migration

Atomic change: rename FxRate.Pair→Base/Quote and UsdPerEur→Rate;
generalise IFxRateProvider, DailyFxCache, FxConverter to take
(base, quote) / (amount, currency); add Trade.InstrumentId FK;
drop GoalConfig.FocusTicker; generate the EF migration with
seed (CON3.L Held, COIN Watchlist, BTC-USD Watchlist) and the
Trades.InstrumentId='CON3.L' backfill; update all callers and
existing tests; delete dead appsettings.Tickers.Context and
appsettings.Fx.Pair.

Phase 1 of the multi-ticker foundation per
docs/superpowers/specs/2026-05-07-multi-ticker-foundation-design.md
EOF
)"
```

---

### Task 10: `PriceFeedHostedService` warms from DB

**Files:**
- Modify: `TradyStrat/Features/PriceFeed/PriceFeedHostedService.cs`
- Modify: `TradyStrat.Tests/PriceFeed/PriceFeedHostedServiceTests.cs`

- [ ] **Step 1: Rewrite the service**

```csharp
// TradyStrat/Features/PriceFeed/PriceFeedHostedService.cs
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Settings.UseCases;

namespace TradyStrat.Features.PriceFeed;

public sealed partial class PriceFeedHostedService(
    IServiceProvider services,
    ILogger<PriceFeedHostedService> log) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var price       = scope.ServiceProvider.GetRequiredService<DailyPriceCache>();
        var fx          = scope.ServiceProvider.GetRequiredService<DailyFxCache>();
        var listUseCase = scope.ServiceProvider.GetRequiredService<ListInstrumentsUseCase>();

        var instruments = await listUseCase.ExecuteAsync(Unit.Value, cancellationToken);

        foreach (var inst in instruments)
            await SafeWarmPriceAsync(price, inst.Ticker, cancellationToken);

        var pairs = instruments
            .Where(i => !string.Equals(i.Currency, "EUR", StringComparison.OrdinalIgnoreCase))
            .Select(i => i.Currency.ToUpperInvariant())
            .Distinct();

        foreach (var quote in pairs)
            await SafeWarmFxAsync(fx, "EUR", quote, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SafeWarmPriceAsync(DailyPriceCache cache, string ticker, CancellationToken ct)
    {
        try { await cache.EnsureFreshAsync(ticker, ct); }
        catch (PriceFeedUnavailableException ex) { LogPriceWarmFailed(log, ex, ticker); }
    }

    private async Task SafeWarmFxAsync(DailyFxCache cache, string @base, string quote, CancellationToken ct)
    {
        try { await cache.EnsureFreshAsync(@base, quote, ct); }
        catch (FxRateUnavailableException ex) { LogFxWarmFailed(log, ex, @base, quote); }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Price warm failed for {Ticker}")]
    private static partial void LogPriceWarmFailed(ILogger logger, Exception ex, string ticker);

    [LoggerMessage(Level = LogLevel.Warning, Message = "FX warm failed for {Base}/{Quote}")]
    private static partial void LogFxWarmFailed(ILogger logger, Exception ex, string @base, string quote);
}
```

- [ ] **Step 2: Update the existing tests**

Open `TradyStrat.Tests/PriceFeed/PriceFeedHostedServiceTests.cs`. The test setup currently relies on the hardcoded ticker array. Replace with a setup that seeds `Instruments` rows in an in-memory DB and asserts the warm calls fire for each.

The structural change is: add an `IServiceProvider` (or `IServiceCollection`) builder that registers an in-memory `AppDbContext` populated with three Instruments + a `ListInstrumentsUseCase` instance + stub `DailyPriceCache`/`DailyFxCache` that record their calls. Then run `StartAsync` and assert the recorded calls.

```csharp
// TradyStrat.Tests/PriceFeed/PriceFeedHostedServiceTests.cs (full rewrite)
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Time;
using TradyStrat.Data;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Fx.Providers;
using TradyStrat.Features.PriceFeed;
using TradyStrat.Features.PriceFeed.Providers;
using TradyStrat.Features.Settings.UseCases;
using Xunit;
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;

namespace TradyStrat.Tests.PriceFeed;

public class PriceFeedHostedServiceTests
{
    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow() => new(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc);
        public DateOnly TodayLocal() => new(2026, 5, 7);
        public DateOnly TodayInExchangeTzFor(string ticker) => new(2026, 5, 7);
    }

    private sealed class RecordingPriceFeed : IPriceFeed
    {
        public List<string> WarmedTickers { get; } = [];
        public Task<IReadOnlyList<PriceBar>> FetchDailyAsync(
            string ticker, DateOnly from, DateOnly to, CancellationToken ct)
        {
            WarmedTickers.Add(ticker);
            return Task.FromResult<IReadOnlyList<PriceBar>>(Array.Empty<PriceBar>());
        }
        public Task<InstrumentMetadata> GetInstrumentMetadataAsync(string ticker, CancellationToken ct)
            => throw new NotImplementedException();
    }

    private sealed class RecordingFxProvider : IFxRateProvider
    {
        public List<(string Base, string Quote)> WarmedPairs { get; } = [];
        public Task<IReadOnlyList<FxRate>> FetchAsync(
            string @base, string quote, DateOnly from, DateOnly to, CancellationToken ct)
        {
            WarmedPairs.Add((@base, quote));
            return Task.FromResult<IReadOnlyList<FxRate>>(Array.Empty<FxRate>());
        }
    }

    [Fact]
    public async Task Warms_each_instrument_and_one_fx_pair_per_distinct_currency()
    {
        var sc = new ServiceCollection();
        sc.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        sc.AddScoped(typeof(IRepositoryBase<>),     typeof(EfRepoShim<>));
        sc.AddScoped(typeof(IReadRepositoryBase<>), typeof(EfRepoShim<>));
        sc.AddSingleton<IClock, FixedClock>();
        var feed = new RecordingPriceFeed();
        var fxp  = new RecordingFxProvider();
        sc.AddSingleton<IPriceFeed>(feed);
        sc.AddSingleton<IFxRateProvider>(fxp);
        sc.AddScoped<DailyPriceCache>();
        sc.AddScoped<DailyFxCache>();
        sc.AddScoped<ListInstrumentsUseCase>();
        sc.AddLogging();

        var sp = sc.BuildServiceProvider();

        // Seed instruments.
        using (var seed = sp.CreateScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Instruments.AddRange(
                Inst("CON3.L",  "USD"),
                Inst("ETHE.PA", "EUR"),
                Inst("MSFT",    "USD"));
            await db.SaveChangesAsync();
        }

        var sut = new PriceFeedHostedService(sp, NullLogger<PriceFeedHostedService>.Instance);
        await sut.StartAsync(CancellationToken.None);

        feed.WarmedTickers.ShouldBe(new[] { "CON3.L", "ETHE.PA", "MSFT" }, ignoreOrder: true);
        // Two non-EUR instruments share the USD currency → only one pair warmed.
        fxp.WarmedPairs.ShouldHaveSingleItem();
        fxp.WarmedPairs[0].ShouldBe(("EUR", "USD"));
    }

    private static Instrument Inst(string ticker, string currency) => new()
    {
        Id = 0, Ticker = ticker, Name = ticker, Currency = currency,
        Exchange = "X", TimezoneId = "Etc/UTC", Kind = InstrumentKind.Held,
        AddedAt = DateTime.UtcNow,
    };

    private sealed class EfRepoShim<T>(AppDbContext db) : RepositoryBase<T>(db) where T : class { }
}
```

- [ ] **Step 3: Build, test**

Run: `dotnet test TradyStrat.slnx --filter "FullyQualifiedName~PriceFeedHostedServiceTests"`
Expected: 1 passed (the new test) plus any pre-existing tests in the file (rewrite removes them).

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/PriceFeed/PriceFeedHostedService.cs \
        TradyStrat.Tests/PriceFeed/PriceFeedHostedServiceTests.cs
git commit -m "feat(pricefeed): warm cache from DB-driven instrument list"
```

---

## Phase C — Behavior changes (Tasks 11–14)

---

### Task 11: First end-to-end smoke against the live DB

> **Why this is its own task.** Phase A added new code; Task 9 changed the schema; Task 10 changed warm-up. Before going further (and before more dashboard refactoring lands on top), run the new build against the user's actual DB and verify nothing has obviously regressed. Catching a problem here saves hours.

- [ ] **Step 1: Back up the live DB**

```bash
cp "$HOME/Library/Application Support/TradyStrat/tradystrat.db" \
   "$HOME/tradystrat.db.pre-multiticker.bak"
ls -lh "$HOME/tradystrat.db.pre-multiticker.bak"
```

Expected: a fresh backup with non-zero size.

- [ ] **Step 2: Run the app**

```bash
dotnet run --project TradyStrat
```

Expected: app boots; logs show migration applied; logs show three "Price warm" messages and one "FX warm" message; dashboard loads on http://127.0.0.1:5180.

- [ ] **Step 3: Visual sanity check**

Open the dashboard. Verify:
- Goal bar shows the same EUR value as before the migration (within rounding).
- The three CON3.L trades still appear in the trade ledger.
- AI suggestion still renders (the previous day's PromptHash is unchanged because the seeded watchlist matches the old hardcoded context).

If anything is off: stop, diagnose, do not proceed. Restore the backup with `cp` if needed. There's no commit to make for this task — it's a checkpoint.

---

### Task 12: `SnapshotFactory` reads context from DB

**Files:**
- Modify: `TradyStrat/Features/AiSuggestion/Snapshot/SnapshotFactory.cs`
- Test: `TradyStrat.Tests/AiSuggestion/SnapshotFactoryTests.cs` (extend, sentinel only)

- [ ] **Step 1: Replace the static `Catalog` with a DB read**

In `SnapshotFactory.cs`:

```csharp
// Add ListInstrumentsUseCase to the constructor parameter list:
public sealed class SnapshotFactory(
    IndicatorEngine indicators,
    PortfolioService portfolio,
    FxConverter fx,
    IReadRepositoryBase<GoalConfig> goalRepo,
    IReadRepositoryBase<Trade> tradeRepo,
    ListInstrumentsUseCase listInstruments,
    IConfiguration config,
    IClock clock) : ISnapshotFactory
{
    // Remove: private const string FocusTicker = "CON3.L";
    // Remove: private static readonly (string Ticker, string Currency)[] Catalog = ...

    public async Task<AiSnapshot> CreateAsync(DateOnly asOf, CancellationToken ct)
    {
        var goal = await goalRepo.GetByIdAsync(1, ct) ?? GoalConfig.Default(clock.UtcNow());

        var focusTicker = config["Tickers:Focus"]
            ?? throw new InvalidOperationException("Tickers:Focus is not configured.");

        var instruments = await listInstruments.ExecuteAsync(Unit.Value, ct);
        var focus = instruments.SingleOrDefault(i => i.Ticker == focusTicker)
            ?? throw new InvalidOperationException(
                $"Focus ticker '{focusTicker}' is not in the Instruments table. Add it via Settings.");

        // Catalog order: focus first, then all watchlist instruments sorted by ticker.
        // This matches the previous hardcoded order [CON3.L, COIN, BTC-USD] when seeded.
        var watchlist = instruments
            .Where(i => i.Kind == InstrumentKind.Watchlist)
            .OrderBy(i => i.Ticker);
        var catalog = new[] { (focus.Ticker, focus.Currency) }
            .Concat(watchlist.Select(i => (i.Ticker, i.Currency)))
            .ToArray();

        var tickers = new List<TickerContext>();
        decimal? focusPriceEur = null;

        foreach (var (ticker, currency) in catalog)
        {
            var reading = await indicators.ComputeFor(ticker, asOf, ct);
            decimal? eur = null;
            if (!string.Equals(currency, "EUR", StringComparison.OrdinalIgnoreCase))
                eur = await fx.ToEurAsync(reading.Price, currency, asOf, ct);

            if (ticker == focus.Ticker) focusPriceEur = eur ?? reading.Price;

            tickers.Add(new TickerContext(
                ticker, currency, reading.Price, eur, reading.Zone, reading.Reasons));
        }

        var snap = await portfolio.SnapshotAsync(asOf, focusPriceEur ?? 0m, goal.TargetEur, ct);

        // ... (the rest of CreateAsync — recentDtos, usdPerEur, promptHash —
        //      stays as it was in Task 9 with the ToEurAsync rename already applied.)
    }
}
```

Open the file and apply the changes carefully — preserve the trailing `recentDtos`/`usdPerEur`/`promptHash`/return logic exactly as it stands.

> The seeded watchlist is `[BTC-USD, COIN]` after `OrderBy(Ticker)`. The previous hardcoded `Catalog` was `[CON3.L, COIN, BTC-USD]`. The new catalog is `[CON3.L, BTC-USD, COIN]` — **ordering changed**. This affects the `PromptHash` if order is part of the hashed payload (it is — the hash is over a JSON serialisation of `tickers` collection). To preserve byte-identical PromptHash on day one, re-create the legacy order. Replace the `OrderBy(i => i.Ticker)` line with an explicit one for the seed:

```csharp
// Preserve legacy order [COIN, BTC-USD] so PromptHash on day one is unchanged.
var legacyOrder = new[] { "COIN", "BTC-USD" };
var watchlist = instruments
    .Where(i => i.Kind == InstrumentKind.Watchlist)
    .OrderBy(i => Array.IndexOf(legacyOrder, i.Ticker) is var idx && idx < 0
        ? int.MaxValue : idx)
    .ThenBy(i => i.Ticker);
```

Once Phase 1 ships and any new watchlist instruments are added, the order will be: legacy seeds in legacy order, then new instruments alphabetically. Acceptable.

- [ ] **Step 2: Wire `ListInstrumentsUseCase` and `IConfiguration` in `AiSuggestionModule`**

`ListInstrumentsUseCase` is already registered by `SettingsModule` (Task 8). `IConfiguration` is automatically available. No module edits needed.

- [ ] **Step 3: Add the PromptHash sentinel test**

Open or create `TradyStrat.Tests/AiSuggestion/SnapshotFactoryTests.cs`. The simplest sentinel: build a snapshot with seeded instruments + a stubbed `IndicatorEngine`/`PortfolioService` and assert the `PromptHash` matches the value the previous-version factory produced for the same inputs.

If a test stub for `IndicatorEngine` does not yet exist in the test project, this test is non-trivial to set up (the indicator engine has many dependencies). **Pragmatic alternative:** rather than reproducing the full snapshot, assert at unit level that:
- `SnapshotFactory` calls the indicator engine for each instrument in catalog order: `[CON3.L, COIN, BTC-USD]`.
- `currency` for each call matches the seeded instrument's currency (`USD` for all three).

```csharp
// TradyStrat.Tests/AiSuggestion/SnapshotFactoryTests.cs
// (Skeleton — fill in stubs as the test project conventions allow.)
[Fact]
public async Task Catalog_iterates_focus_then_legacy_watchlist_order()
{
    // Arrange: in-memory db with the three seeded instruments;
    // stub IndicatorEngine that records ticker arguments;
    // stub PortfolioService and FxConverter that return zero EUR.
    // Act: var snap = await factory.CreateAsync(asOf, ct);
    // Assert: indicatorEngine.Calls == ["CON3.L", "COIN", "BTC-USD"]
}
```

If the existing `SnapshotFactoryTests.cs` already has stubs and PromptHash fixtures, prefer the byte-identical PromptHash assertion. Otherwise, the order assertion above is sufficient as a sentinel.

- [ ] **Step 4: Build, test, commit**

```bash
dotnet build TradyStrat.slnx
dotnet test  TradyStrat.slnx
git add TradyStrat/Features/AiSuggestion/Snapshot/SnapshotFactory.cs \
        TradyStrat.Tests/AiSuggestion/SnapshotFactoryTests.cs
git commit -m "feat(ai): SnapshotFactory reads context from Instruments DB"
```

---

### Task 13: Multi-ticker FIFO in `PortfolioService`

**Files:**
- Modify: `TradyStrat/Features/Portfolio/PortfolioService.cs`
- Modify: `TradyStrat/Features/Portfolio/PortfolioSnapshot.cs` (or wherever `PortfolioSnapshot` is declared)
- Test: `TradyStrat.Tests/Portfolio/PortfolioServiceMultiTickerTests.cs`

> **Refactoring approach.** The existing `BuildSnapshot` runs one FIFO walk over all trades, with `currentPriceEur` and `goalEur` as scalar inputs. Phase 1 widens this so the result is a *collection* of per-ticker positions plus a portfolio total. The shape of `PortfolioSnapshot` changes — it gains a `Positions` list. The old scalar fields (`Shares`, `AvgCostEur`, `CurrentValueEur`, etc.) become **portfolio totals** computed by summing the per-position rows.

- [ ] **Step 1: Update `PortfolioSnapshot` shape**

Open the file declaring `PortfolioSnapshot` (likely `Features/Portfolio/PortfolioSnapshot.cs` or a sibling of `PortfolioService.cs`).

```csharp
// TradyStrat/Features/Portfolio/PortfolioSnapshot.cs
namespace TradyStrat.Features.Portfolio;

public sealed record PortfolioSnapshot(
    IReadOnlyList<PositionRow> Positions,
    decimal CurrentValueEur,    // sum of Positions.MarketValueEur
    decimal CostBasisEur,       // sum of Positions.CostBasisEur
    decimal UnrealizedPnLEur,   // CurrentValueEur - CostBasisEur
    decimal RealizedPnLEur,     // sum of per-ticker realized
    decimal ProgressPct,        // CurrentValueEur / GoalEur * 100
    // Legacy fields kept temporarily for callers that still read them.
    // Remove in a follow-up once dashboard view-model migration completes.
    decimal Shares,
    decimal AvgCostEur);

public sealed record PositionRow(
    int InstrumentId,
    string Ticker,
    string Currency,
    decimal Quantity,
    decimal CostBasisEur,
    decimal MarketValueEur,
    decimal UnrealizedPnLEur,
    decimal RealizedPnLEur);
```

If `Lot` is declared in `PortfolioService.cs`, leave it there — it's an implementation detail of the FIFO walk.

- [ ] **Step 2: Refactor `BuildSnapshot`**

`BuildSnapshot` becomes a per-ticker outer loop. Market value per position requires a price for each instrument, which `BuildSnapshot` doesn't currently fetch. New shape: the public methods become async, take a `priceLookup` callback or a pre-built dictionary `IReadOnlyDictionary<int InstrumentId, (decimal PriceEur, string Currency)>`.

For clarity and testability, refactor to inject a *price provider closure* from the caller:

```csharp
// TradyStrat/Features/Portfolio/PortfolioService.cs
using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Features.Trades.Specifications;

namespace TradyStrat.Features.Portfolio;

public sealed class PortfolioService(IReadRepositoryBase<Trade> trades)
{
    public async Task<PortfolioSnapshot> SnapshotAsync(
        IReadOnlyDictionary<int, (decimal PriceEur, string Ticker, string Currency)> priceByInstrument,
        decimal goalEur,
        CancellationToken ct)
    {
        var all = await trades.ListAsync(new AllTradesSpec(), ct);
        return BuildSnapshot(all, priceByInstrument, goalEur);
    }

    public async Task<PortfolioSnapshot> SnapshotAsync(
        DateOnly asOf,
        IReadOnlyDictionary<int, (decimal PriceEur, string Ticker, string Currency)> priceByInstrument,
        decimal goalEur,
        CancellationToken ct)
    {
        var asOfTrades = await trades.ListAsync(new TradesAsOfSpec(asOf), ct);
        return BuildSnapshot(asOfTrades, priceByInstrument, goalEur);
    }

    private static PortfolioSnapshot BuildSnapshot(
        List<Trade> trades,
        IReadOnlyDictionary<int, (decimal PriceEur, string Ticker, string Currency)> priceByInstrument,
        decimal goalEur)
    {
        var perInstrument = trades.GroupBy(t => t.InstrumentId);

        var positions = new List<PositionRow>();
        foreach (var group in perInstrument)
        {
            var openLots = new LinkedList<Lot>();
            var realized = 0m;

            foreach (var t in group.OrderBy(x => x.ExecutedOn))
            {
                if (t.IsBuy)
                {
                    var unitCost = (t.GrossEur + t.FeesEur) / t.Quantity;
                    openLots.AddLast(new Lot(t.ExecutedOn, t.Quantity, unitCost));
                }
                else
                {
                    var remaining = t.Quantity;
                    while (remaining > 0)
                    {
                        var head = openLots.First
                            ?? throw new TradeValidationException(
                                $"Sell on {t.ExecutedOn} for instrument {group.Key} exceeds open lots.");

                        var consumed = Math.Min(head.Value.Quantity, remaining);
                        realized += consumed * (t.PricePerShare - head.Value.UnitCostEur);
                        realized -= t.FeesEur * (consumed / t.Quantity);

                        if (consumed == head.Value.Quantity)
                            openLots.RemoveFirst();
                        else
                            head.Value = head.Value with { Quantity = head.Value.Quantity - consumed };

                        remaining -= consumed;
                    }
                }
            }

            var qty = openLots.Sum(l => l.Quantity);
            var costBasis = openLots.Sum(l => l.CostBasisEur);
            var marketValue = priceByInstrument.TryGetValue(group.Key, out var p)
                ? qty * p.PriceEur : 0m;
            var unrealised = marketValue - costBasis;
            var ticker     = priceByInstrument.TryGetValue(group.Key, out var p2) ? p2.Ticker    : "?";
            var currency   = priceByInstrument.TryGetValue(group.Key, out var p3) ? p3.Currency  : "?";

            positions.Add(new PositionRow(
                InstrumentId: group.Key,
                Ticker: ticker,
                Currency: currency,
                Quantity: qty,
                CostBasisEur: costBasis,
                MarketValueEur: marketValue,
                UnrealizedPnLEur: unrealised,
                RealizedPnLEur: realized));
        }

        var totalValue   = positions.Sum(p => p.MarketValueEur);
        var totalCost    = positions.Sum(p => p.CostBasisEur);
        var totalUnreal  = totalValue - totalCost;
        var totalReal    = positions.Sum(p => p.RealizedPnLEur);
        var pct          = goalEur == 0m ? 0m : totalValue / goalEur * 100m;

        // Legacy scalar fields (single-ticker callers): use the focus position if present,
        // otherwise zeros. The dashboard rewrite in Task 14 stops reading these.
        var legacyShares  = positions.Count == 1 ? positions[0].Quantity     : 0m;
        var legacyAvgCost = positions.Count == 1 && legacyShares > 0
            ? positions[0].CostBasisEur / legacyShares : 0m;

        return new PortfolioSnapshot(
            Positions: positions,
            CurrentValueEur: totalValue,
            CostBasisEur: totalCost,
            UnrealizedPnLEur: totalUnreal,
            RealizedPnLEur: totalReal,
            ProgressPct: pct,
            Shares: legacyShares,
            AvgCostEur: legacyAvgCost);
    }

    internal record struct Lot(DateOnly OpenedOn, decimal Quantity, decimal UnitCostEur)
    {
        public decimal CostBasisEur => Quantity * UnitCostEur;
    }
}
```

> **Note.** `Shares` and `AvgCostEur` legacy scalars are populated only when the snapshot has exactly one position — matching the pre-Phase-1 behavior for the CON3.L-only case. With multiple positions they're zero, and Task 14 will remove the dashboard's reads of them.

- [ ] **Step 3: Update `SnapshotFactory` and `LoadDashboardUseCase` callers**

Both currently call `portfolio.SnapshotAsync(asOf, focusPriceEur ?? 0m, goal.TargetEur, ct)`. The new signature takes a price-by-instrument dictionary. For Phase 1, build the dictionary from the catalog already iterated for indicators:

In `SnapshotFactory.cs`:

```csharp
// Just before the call to portfolio.SnapshotAsync, after the foreach over catalog:
var priceMap = new Dictionary<int, (decimal PriceEur, string Ticker, string Currency)>();
foreach (var inst in instruments.Where(i => i.Kind == InstrumentKind.Held))
{
    var ctx = tickers.SingleOrDefault(t => t.Ticker == inst.Ticker);
    var priceEur = ctx?.PriceEur ?? ctx?.Price ?? 0m;
    priceMap[inst.Id] = (priceEur, inst.Ticker, inst.Currency);
}

var snap = await portfolio.SnapshotAsync(asOf, priceMap, goal.TargetEur, ct);
```

The exact field name (`PriceEur` vs `Price`) depends on `TickerContext`'s shape. Check the file and adjust.

In `LoadDashboardUseCase.cs`: same change — build `priceMap` from the existing per-ticker indicator loop.

- [ ] **Step 4: Multi-ticker tests**

```csharp
// TradyStrat.Tests/Portfolio/PortfolioServiceMultiTickerTests.cs
using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Common.Domain;
using TradyStrat.Data;
using TradyStrat.Features.Portfolio;
using TradyStrat.Tests.Fx;
using Xunit;

namespace TradyStrat.Tests.Portfolio;

public class PortfolioServiceMultiTickerTests
{
    [Fact]
    public async Task Builds_per_ticker_positions_summing_to_portfolio_totals()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        // Two instruments, two trades each, all buys.
        db.Trades.AddRange(
            Buy(instrumentId: 1, qty: 10m, price: 5m, executedOn: new DateOnly(2026, 4, 1)),
            Buy(instrumentId: 1, qty: 10m, price: 6m, executedOn: new DateOnly(2026, 4, 8)),
            Buy(instrumentId: 2, qty: 5m,  price: 100m, executedOn: new DateOnly(2026, 4, 2)),
            Buy(instrumentId: 2, qty: 5m,  price: 110m, executedOn: new DateOnly(2026, 4, 9)));
        await db.SaveChangesAsync();

        var sut = new PortfolioService(new TestRepo<Trade>(db));

        var prices = new Dictionary<int, (decimal PriceEur, string Ticker, string Currency)>
        {
            [1] = (10m,  "AAA", "EUR"),  // 20 shares × 10 = 200 EUR
            [2] = (200m, "BBB", "EUR"),  // 10 shares × 200 = 2000 EUR
        };

        var snap = await sut.SnapshotAsync(prices, goalEur: 10000m, CancellationToken.None);

        snap.Positions.Count.ShouldBe(2);
        snap.Positions.Single(p => p.InstrumentId == 1).Quantity.ShouldBe(20m);
        snap.Positions.Single(p => p.InstrumentId == 2).Quantity.ShouldBe(10m);

        snap.CostBasisEur.ShouldBe(50m + 60m + 500m + 550m);   // 1160
        snap.CurrentValueEur.ShouldBe(200m + 2000m);            // 2200
        snap.UnrealizedPnLEur.ShouldBe(2200m - 1160m);          // 1040
        snap.ProgressPct.ShouldBe(22m);                         // 2200/10000*100
    }

    private static Trade Buy(int instrumentId, decimal qty, decimal price, DateOnly executedOn)
        => new()
        {
            Id = 0, InstrumentId = instrumentId,
            ExecutedOn = executedOn, Side = TradeSide.Buy,
            Quantity = qty, PricePerShare = price, FeesEur = 0m,
            Note = null, CreatedAt = DateTime.UtcNow,
        };
}
```

- [ ] **Step 5: Update existing single-ticker portfolio tests**

The pre-existing `PortfolioServiceTests` class (if it exists) calls the old scalar `SnapshotAsync(currentPriceEur, goalEur, ct)` signature. Update each call site to construct a one-entry `priceMap`. Example:

```csharp
// Before
var snap = await sut.SnapshotAsync(currentPriceEur: 5m, goalEur: 1000m, CancellationToken.None);

// After
var prices = new Dictionary<int, (decimal PriceEur, string Ticker, string Currency)>
{
    [1] = (5m, "CON3.L", "USD"),
};
var snap = await sut.SnapshotAsync(prices, goalEur: 1000m, CancellationToken.None);
```

Test data also needs `InstrumentId = 1` on every existing `Trade` literal in the test fixtures.

- [ ] **Step 6: Build, test, commit**

```bash
dotnet build TradyStrat.slnx
dotnet test  TradyStrat.slnx
git add TradyStrat/Features/Portfolio/ TradyStrat.Tests/Portfolio/
git commit -m "feat(portfolio): per-ticker FIFO and Positions on snapshot"
```

---

### Task 14: Dashboard view-model widening + `PositionsTable.razor`

**Files:**
- Modify: `TradyStrat/Features/Dashboard/UseCases/LoadDashboardUseCase.cs`
- Modify: `TradyStrat/Features/Dashboard/ViewModels/DashboardViewModel.cs` (or wherever it lives)
- Create: `TradyStrat/Features/Dashboard/Components/PositionsTable.razor`
- Modify: `TradyStrat/Features/Dashboard/Pages/DashboardPage.razor` (or whichever page composes the dashboard)
- Modify: `TradyStrat/Features/Dashboard/Components/HeroCapital.razor` (consume `Snap.CurrentValueEur` portfolio total — already does, just verify after Task 13's snapshot change)
- Modify: `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor` (read focus from config; remove "CON3" hardcode)

- [ ] **Step 1: Widen `LoadDashboardUseCase` and view-model**

Inside `LoadDashboardUseCase.ExecuteCore`, replace the hardcoded `Catalog` with a DB read (mirroring the SnapshotFactory change in Task 12). Add a `Positions` field to `DashboardViewModel`.

```csharp
// In LoadDashboardUseCase.ExecuteCore, replace the Catalog block:

var instruments = await listInstruments.ExecuteAsync(Unit.Value, ct);
var focusTicker = config["Tickers:Focus"]
    ?? throw new InvalidOperationException("Tickers:Focus is not configured.");

// Iterate Held + Watchlist for zone analysis. Held instruments contribute
// to Positions (via PortfolioService); Watchlist do not.
var tickers = new List<TickerView>();
var priceMap = new Dictionary<int, (decimal PriceEur, string Ticker, string Currency)>();

foreach (var inst in instruments)
{
    var reading = await indicators.ComputeFor(inst.Ticker, target, ct);
    decimal? eur = string.Equals(inst.Currency, "EUR", StringComparison.OrdinalIgnoreCase)
        ? reading.Price
        : await fx.ToEurAsync(reading.Price, inst.Currency, target, ct);

    var deltaPct = await ComputeDeltaPctAsync(inst.Ticker, target, ct);
    tickers.Add(new TickerView(
        inst.Ticker, inst.Currency, reading.Price, eur, deltaPct, reading.Zone));

    if (inst.Kind == InstrumentKind.Held && eur is { } e)
        priceMap[inst.Id] = (e, inst.Ticker, inst.Currency);
}

var snap = await portfolio.SnapshotAsync(target, priceMap, goal.TargetEur, ct);
```

Also: the existing `LoadDashboardUseCase` constructor takes `IReadRepositoryBase<FxRate> fxRepo` and the file has a `LatestFxRateSpec(FxPair, target)` call near the bottom (FX freshness pill). Update that line:

```csharp
// Before:
var fxLatest = await fxRepo.FirstOrDefaultAsync(new LatestFxRateSpec(FxPair, target), ct);
// After (USD as the canonical companion FX for the freshness pill):
var fxLatest = await fxRepo.FirstOrDefaultAsync(new LatestFxRateSpec("EUR", "USD", target), ct);
```

Add `IConfiguration` and `ListInstrumentsUseCase` to the constructor parameters; remove the `FxPair`/`FocusTicker` constants and the static `Catalog`.

- [ ] **Step 2: Add `Positions` to `DashboardViewModel`**

Find the `DashboardViewModel` record (likely under `Features/Dashboard/ViewModels/` or alongside the use case) and add the field:

```csharp
public sealed record DashboardViewModel(
    // … existing fields …
    IReadOnlyList<PositionRow> Positions,
    // … existing fields …
);
```

(Insert `Positions` next to the existing `Tickers` field; the constructor invocation at the bottom of `ExecuteCore` becomes `Positions: snap.Positions, …`.)

- [ ] **Step 3: Create the `PositionsTable.razor` component**

```razor
@* TradyStrat/Features/Dashboard/Components/PositionsTable.razor *@
@using System.Globalization
@using TradyStrat.Features.Portfolio

<section class="positions">
    <h3>Positions</h3>
    @if (Rows.Count == 0)
    {
        <p class="empty">No positions yet — add an instrument in Settings to start tracking.</p>
    }
    else
    {
        <table class="positions-table">
            <thead>
                <tr>
                    <th>Ticker</th>
                    <th class="num">Qty</th>
                    <th class="num">Cost basis</th>
                    <th class="num">Mkt value</th>
                    <th class="num">Unrealised PnL</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var r in Rows)
                {
                    <tr>
                        <td>@r.Ticker</td>
                        <td class="num">@r.Quantity.ToString("F2", FrFr)</td>
                        <td class="num">€@r.CostBasisEur.ToString("N0", FrFr)</td>
                        <td class="num">€@r.MarketValueEur.ToString("N0", FrFr)</td>
                        <td class="num @(r.UnrealizedPnLEur >= 0 ? "pos" : "neg")">
                            @FormatSigned(r.UnrealizedPnLEur)
                        </td>
                    </tr>
                }
                <tr class="total">
                    <td>Total</td>
                    <td></td>
                    <td class="num">€@Rows.Sum(r => r.CostBasisEur).ToString("N0", FrFr)</td>
                    <td class="num">€@Rows.Sum(r => r.MarketValueEur).ToString("N0", FrFr)</td>
                    <td class="num @(TotalUnreal >= 0 ? "pos" : "neg")">@FormatSigned(TotalUnreal)</td>
                </tr>
            </tbody>
        </table>
    }
</section>

@code {
    [Parameter] public IReadOnlyList<PositionRow> Rows { get; set; } = [];
    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");
    private decimal TotalUnreal => Rows.Sum(r => r.UnrealizedPnLEur);
    private static string FormatSigned(decimal v)
        => (v >= 0 ? "+" : "") + "€" + v.ToString("N0", FrFr);
}
```

- [ ] **Step 4: Mount it in the dashboard page**

Find the dashboard page (likely `Features/Dashboard/DashboardPage.razor` or similar — the file that uses `LoadDashboardUseCase` and `DashboardViewModel`). Insert `<PositionsTable Rows="ViewModel.Positions" />` between the `HeroCapital` block and the `TodaysCallCard` block.

- [ ] **Step 5: Update `TodaysCallCard.razor` — focus from config, not "CON3"**

The existing card has `@q.ToString("F0", FrFr) sh CON3 …` hardcoded. Replace with a parameter:

```razor
@* TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor — only the changed line *@
@q.ToString("F0", FrFr) sh @FocusLabel · ≤ €@mp.ToString("F2", FrFr) · ≈ €@((q * mp).ToString("F2", FrFr))
```

Add `[Parameter] public string FocusLabel { get; set; } = "CON3";` to `@code`. The page passes `FocusLabel="@(focusTicker.Replace(".L", "").Replace(".DE", ""))"` or simply the full ticker — pick whichever reads better. The dashboard page already has `focusTicker` available via the view-model.

- [ ] **Step 6: Build & visual smoke**

Run: `dotnet build TradyStrat.slnx`
Then: `dotnet run --project TradyStrat`

Open the dashboard. Verify:
- Goal bar shows portfolio EUR (which equals CON3.L EUR alone today, since CON3.L is the only Held instrument).
- Positions table renders with one row (CON3.L) and a Total row.
- Today's call still renders, with the focus ticker correctly displayed.
- Zone cards render for CON3.L, COIN, BTC-USD.

- [ ] **Step 7: Commit**

```bash
git add TradyStrat/Features/Dashboard/ \
        TradyStrat.Tests/  # if any dashboard tests changed
git commit -m "feat(dashboard): Positions table + DB-driven catalog"
```

---

## Phase D — Trade-form & misc UI (Tasks 15–17)

---

### Task 15: `AddTradeDialog` Instrument dropdown (Held only)

**Files:**
- Modify: `TradyStrat/Features/Trades/Components/AddTradeDialog.razor`
- Modify: `TradyStrat/Features/Trades/Pages/TradesPage.razor` (or wherever the dialog is consumed) — pass the Held instruments list

- [ ] **Step 1: Inject `ListInstrumentsUseCase`, add the dropdown**

Open `AddTradeDialog.razor`. Add:

```razor
@inject TradyStrat.Features.Settings.UseCases.ListInstrumentsUseCase ListInstruments
```

Add to the grid (place above the existing Date label):

```razor
<label>Ticker
    <select @bind="_instrumentId">
        <option value="0" disabled selected>— pick a held instrument —</option>
        @foreach (var i in _heldInstruments)
        {
            <option value="@i.Id">@i.Ticker</option>
        }
    </select>
</label>
```

Add `private int _instrumentId;` and `private List<Instrument> _heldInstruments = new();` to `@code`.

In `OnInitializedAsync`:

```csharp
protected override async Task OnInitializedAsync()
{
    var all = await ListInstruments.ExecuteAsync(
        TradyStrat.Common.UseCases.Unit.Value, CancellationToken.None);
    _heldInstruments = all.Where(i => i.Kind == InstrumentKind.Held).ToList();
    if (_heldInstruments.Count == 1) _instrumentId = _heldInstruments[0].Id;
}
```

In the existing `DoSubmit` (or whichever submit handler exists), reject `_instrumentId == 0` with an inline `_err = "Pick an instrument."`. Otherwise pass `_instrumentId` into the trade-input record.

- [ ] **Step 2: Empty-state message**

If `_heldInstruments` is empty, show: "No Held instruments configured. Add one in Settings → Held first." and disable Save.

- [ ] **Step 3: Build, smoke, commit**

```bash
dotnet build TradyStrat.slnx
git add TradyStrat/Features/Trades/Components/AddTradeDialog.razor \
        TradyStrat/Features/Trades/Pages/  # adjust if path differs
git commit -m "feat(trades): instrument dropdown in AddTradeDialog (Held only)"
```

---

### Task 16: Ticker column in trade ledger

**Files:**
- Modify: the trade-ledger Razor component (search by `Quantity` cell pattern under `Features/Trades/`)

- [ ] **Step 1: Find the file**

Run: `grep -rl "Quantity" /Users/philippe/repo/gh-phmatray/TradyStrat/TradyStrat/Features/Trades/Components/`

Likely `TradeLedger.razor` or part of `TradesPage.razor`.

- [ ] **Step 2: Add a Ticker column**

The ledger today renders trade rows but no ticker (single-ticker assumption). Add a `Ticker` column. The trade row provider needs to surface the ticker — join to `Instruments` either inside the use case that lists trades, or in the Razor by maintaining an `Instruments` lookup dictionary.

Easiest: the page already calls a `ListTradesUseCase`. Update its output record to include `string Ticker` (joining via a `Where(i => i.Id == t.InstrumentId)` lookup in the use case).

If editing the use case feels too invasive, alternative: load instruments in the page, display `instrumentsById[t.InstrumentId].Ticker`.

- [ ] **Step 3: Build, commit**

```bash
git add TradyStrat/Features/Trades/  # all touched files
git commit -m "feat(trades): show Ticker column in trade ledger"
```

---

### Task 17: Final manual smoke + final commit

- [ ] **Step 1: Full test run**

Run: `dotnet test TradyStrat.slnx`
Expected: all green.

- [ ] **Step 2: End-to-end smoke against the live DB**

```bash
dotnet run --project TradyStrat
```

Walk through the spec's §11.4 manual smoke:

1. Dashboard renders with CON3.L position (same EUR as before Phase 1).
2. Settings → Add instrument → enter `ETHE.PA` → Probe → confirm metadata (EUR, Euronext Paris, Europe/Paris) → Add as Held.
3. Trades → Add trade → Ticker dropdown now shows `CON3.L` and `ETHE.PA` → log a 1-share buy of ETHE.PA at €1 → Save.
4. Dashboard → Positions table now shows two rows; goal bar updates (~+€1 plus FX).
5. Add a Watchlist instrument (e.g. `ETH-USD`) → verify it appears in zone analysis but not in the Positions table.
6. Verify the AI suggestion still fires for CON3.L with the same `PromptHash` as the prior day's stored suggestion.

- [ ] **Step 3: Push (only if requested)**

The user has not asked to push; do not run `git push` unless explicitly instructed.

The work is complete. Phase 2 (multi-ticker AI, suggestion ↔ trade linkage, structured citations) is a separate spec.

---

## Self-review

(Checked by the planner.)

**1. Spec coverage.** Every requirement in `2026-05-07-multi-ticker-foundation-design.md` maps to a task:
- §3 entities → Tasks 1, 9.1
- §4 migration + code-side cleanup → Task 9 (full sub-tasks)
- §5 Add-instrument flow → Tasks 4–8
- §5.4 typed exceptions → Task 3
- §6 dashboard → Task 14, 15, 16
- §7 PriceFeed warm + FX widening → Tasks 9, 10
- §8 per-ticker FIFO → Task 13
- §9 SnapshotFactory DB-driven → Task 12
- §11 test plan → Tasks 4, 5, 7, 9.8, 13, plus the manual smoke in 11 + 17
- §12 Specifications → Task 2
- §13 pattern drift → recorded in spec; no implementation needed

**2. Placeholder scan.** No "TBD"/"TODO"/"add appropriate error handling"/"similar to Task N"/"write tests for the above" survives. Every code step shows actual code.

**3. Type consistency.** `InstrumentMetadata` is identical across Tasks 1, 4, 5, 7, 8. `ToEurAsync(decimal, string, DateOnly, CancellationToken)` is consistent in Tasks 9, 12, 13, 14. `EnsureFreshAsync(string, string, CancellationToken)` for `DailyFxCache` is consistent. `PositionRow` and `PortfolioSnapshot` shapes match between Tasks 13 and 14.

**4. Ambiguity check.** The biggest ambiguity I found and locked: the `PromptHash` regression risk caused by `OrderBy(Ticker)` changing the catalog order — explicitly worked around in Task 12 with the `legacyOrder` array. The `IClock.TodayInExchangeTzFor(arbitrary string)` falls through to UTC — acceptable for daily-bar use. All other interfaces are concrete.

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-05-07-multi-ticker-foundation.md`. Two execution options:

**1. Subagent-Driven (recommended)** — I dispatch a fresh subagent per task, review between tasks, fast iteration.

**2. Inline Execution** — Execute tasks in this session using executing-plans, batch execution with checkpoints.

Which approach?
