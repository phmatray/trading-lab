# Hexagonal Architecture Refactor — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restructure TradyStrat from one Blazor project into a four-project hexagonal layout (Domain / Application / Infrastructure + Blazor and new CLI driving adapters), and bump `TheAppManager` to a host-neutral v3 so both driving adapters share the same module system.

**Architecture:** Cockburn hexagonal — Domain (pure types), Application (use cases + ports), Infrastructure (driven adapters), Blazor + CLI (driving adapters). Module discovery is host-neutral via `TheAppManager` v3. The refactor is **behaviour-preserving**: every existing test must pass in its new home; no DB schema change; no prompt change. Spec: `docs/superpowers/specs/2026-05-13-hexagonal-refactor-design.md`.

**Tech Stack:** C# 13, .NET 10, EF Core 10 (Sqlite), Microsoft.Extensions.AI 10.3 + Anthropic.SDK 5.10, Ardalis.Specification 9.3, xunit.v3 + Shouldly, Blazor Server, Spectre.Console.Cli (new), TheAppManager (user-owned, bumped to v3.0.0 in this work).

**Worktree:** Use `superpowers:using-git-worktrees` to work in an isolated copy. Commits within the worktree are progress markers; the whole refactor merges as one atomic PR per spec §7.

**Out of scope:** AI suggestion improvements (separate spec, ships after this merges). DB schema changes. Behaviour changes of any kind.

> **`sed` portability:** every `sed -i ''` invocation below is BSD/macOS syntax. On GNU sed (Linux CI, devcontainers, etc.) the empty-string argument is illegal; use `sed -i` (no quotes) instead. Define a one-shot alias at the top of your shell session to stay portable: `sedi() { sed -i.bak "$@" && find . -name '*.bak' -delete; }` and substitute `sedi` for `sed -i ''` everywhere below. The plan keeps the BSD form throughout for readability — translate as needed.

---

## File structure — what moves where

This is the canonical destination map. Every existing file lands in exactly one new project. Razor pages, components, and `Program.cs` stay in the Blazor project.

### New project: `TradyStrat.Domain`

Pure types only — no NuGet deps beyond the BCL.

- All files currently in `TradyStrat/Common/Domain/*.cs` move here unchanged except namespace `TradyStrat.Common.Domain` → `TradyStrat.Domain`.
- Custom domain exceptions from `TradyStrat/Common/Exceptions/`:
  - `TradyStratException.cs`, `FxRateUnavailableException.cs`, `CsvImportException.cs`, `DuplicateInstrumentException.cs`, `IndicatorComputationException.cs`, `InstrumentMetadataIncompleteException.cs`, `InstrumentNotFoundException.cs`, `NoTradingDaysException.cs`, `PolymarketUnavailableException.cs`, `PriceFeedUnavailableException.cs`, `SettingValidationException.cs`, `TradeValidationException.cs`, `UnsupportedCurrencyException.cs`
- `IClock.cs` interface (the port itself is a domain concept; impl `SystemClock` goes to Infrastructure).
- An empty marker class `DomainAssemblyMarker` for assembly scanning.

### New project: `TradyStrat.Application`

Depends on Domain only. Consumes EF/HTTP/AI via ports, never directly.

NuGet: `Ardalis.Specification`, `Microsoft.Extensions.AI.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`, `Atypical.TechnicalAnalysis.Common`, `Atypical.TechnicalAnalysis.Functions`.

- All `TradyStrat/Features/<feature>/UseCases/*.cs` move here.
- All `TradyStrat/Features/<feature>/Specifications/*.cs` move here (Ardalis Specifications are domain queries — they live with the use cases that own them).
- All port interfaces:
  - `Features/AiSuggestion/IAiClient.cs` → `Application/AiSuggestion/IAiClient.cs`
  - `Features/AiSuggestion/Snapshot/IAiSnapshotService.cs`
  - `Features/Fx/Providers/IFxRateProvider.cs`
  - `Features/PriceFeed/Providers/IPriceFeed.cs`
  - `Features/PredictionMarkets/IPredictionMarketProvider.cs`
  - `Features/Settings/Config/ISettingsReader.cs`
- Application services (concrete classes that consume ports):
  - `Features/AiSuggestion/Snapshot/AiSnapshot.cs` (the record — pure, but lives with its consumer)
  - `Features/AiSuggestion/Snapshot/AiSnapshotService.cs`
  - `Features/AiSuggestion/Backfill/SuggestionBackfillCoordinator.cs` + `ISuggestionBackfillCoordinator.cs`
  - `Features/AiSuggestion/CallDiff/CallDiffBuilder.cs`
  - `Features/AiSuggestion/SuggestionService.cs` — **wait**: this is the Anthropic adapter, it goes to Infrastructure. (See Infrastructure section.)
  - `Features/AiSuggestion/JsonOpts.cs`
  - `Features/AiSuggestion/UseCases/SuggestionGate.cs`
  - `Features/Fx/FxConverter.cs`
  - `Features/Fx/DailyFxCache.cs`
  - `Features/Indicators/IndicatorEngine.cs` (and entire `Indicators/` subtree — Bollinger, History, Ichimoku, MovingAverage, Rsi, Zones folders. All pure-compute except for the bar repo dep.)
  - `Features/Portfolio/PortfolioService.cs`, `GrowthSeriesBuilder.cs`
  - `Features/PredictionMarkets/PolymarketFilter.cs`, `PolymarketRelevance.cs` (pure logic at the feature root). `PolymarketNormalizer.cs` lives under `Providers/` and is adapter-internal — it stays with the HTTP provider in Infrastructure (see §3.3).
  - `Features/PriceFeed/DailyPriceCache.cs`
  - `Features/PriceFeed/Specifications/*.cs`
  - `Features/Settings/Config/SettingDescriptor.cs`, `SettingsKeys.cs`, `SettingsModels.cs`, `SettingsRegistry.cs`, `SettingsService.cs`
  - `Features/Trades/CsvImportService.cs`
  - `Features/Dashboard/UseCases/*.cs`, `Features/Dashboard/Navigation/*.cs`, `Features/Dashboard/GoalPaceCalculator.cs`
- `Common/UseCases/UseCaseBase.cs`, `IUseCase.cs`, `Unit.cs`.
- `Common/Time/RelativeTimeFormatter.cs` (pure formatting).
- `Common/Formatting/NumberFormat.cs` (pure formatting).
- `Common/Exceptions/AnthropicCallFailedException.cs` → **renamed to `AiCallFailedException.cs`** in Application (abstract failure type at the port boundary).
- One feature module per feature (see Phase 5).
- An empty marker class `ApplicationAssemblyMarker` for assembly scanning.

### New project: `TradyStrat.Infrastructure`

Depends on Application + Domain.

NuGet: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`, `Ardalis.Specification.EntityFrameworkCore`, `Microsoft.Extensions.AI` (concrete), `Anthropic.SDK`, `Microsoft.Extensions.Http.Resilience`, `Serilog.AspNetCore`, `Serilog.Sinks.File`.

- `Data/` — EF Core: `AppDbContext.cs`, all entity configurations, all migrations. (Currently scattered; moved into `TradyStrat.Infrastructure/Data/`.)
- `Features/AiSuggestion/SuggestionService.cs` — Anthropic adapter implementing `IAiClient`.
- `Common/Exceptions/AnthropicCallFailedException.cs` — kept here as the vendor-specific exception; rewrapped to `AiCallFailedException` at the port boundary.
- `Common/Exceptions/AnthropicConfigurationException.cs` — vendor-specific.
- `Features/Fx/Providers/YahooFxProvider.cs` — HTTP adapter implementing `IFxRateProvider`.
- `Features/PriceFeed/Providers/YahooPriceFeed.cs` + `YahooParser.cs` — HTTP adapter implementing `IPriceFeed`.
- `Features/PriceFeed/PriceFeedHostedService.cs` — background loop, Blazor-host-only.
- `Features/PredictionMarkets/Providers/PolymarketGammaProvider.cs` — HTTP adapter.
- `Features/Settings/Config/SettingsReader.cs` — DB-backed adapter implementing `ISettingsReader`.
- `Features/Settings/Config/SettingsSeederHostedService.cs` — startup seeder; uses `AppDbContext` directly to seed default settings rows.
- `Common/Time/SystemClock.cs` — clock impl.
- `Data/SqlitePathResolver.cs` if it lives in TradyStrat (verify location).
- Per-feature Infrastructure modules (see Phase 5).
- An empty marker class `InfrastructureAssemblyMarker`.

### Existing project: `TradyStrat` (Blazor)

Loses everything above. Keeps:

- `Program.cs` (rewritten to use TheAppManager v3 — see Phase 6).
- All `*.razor` and `*.razor.cs` files in `Features/<feature>/` and `Features/Shell/`.
- `Features/Dashboard/Components/*`, `Features/Trades/Components/*`, `Features/Settings/Components/*`.
- `appsettings.json` files, `wwwroot/`.
- New project references: `TradyStrat.Application`, `TradyStrat.Infrastructure`.

Drops:
- The whole `Common/` folder (everything moved).
- The non-Razor parts of every `Features/<feature>/` folder.
- The entire `Modules/` folder (replaced by per-layer modules in Application + Infrastructure — see Phase 5).
- All PackageReferences for things now in Application/Infrastructure (`Ardalis.Specification.EntityFrameworkCore`, `Anthropic.SDK`, etc.).

### New project: `TradyStrat.Cli`

Depends on Application + Infrastructure.

NuGet: `Spectre.Console`, `Spectre.Console.Cli`, `Microsoft.Extensions.Hosting`.

- `Program.cs` — composition root for the CLI host.
- `HostTypeRegistrar.cs` — `Spectre.Console.Cli.ITypeRegistrar` adapter over `IServiceProvider`.
- `Commands/HelloCommand.cs` — placeholder command that proves the wiring (deleted by the successor spec).

### Test projects (four new ones replace `TradyStrat.Tests`)

- `TradyStrat.Domain.Tests` — pure-type tests (current `Common/Domain/EntityDerivedPropertiesTests`, `SuggestionActionDisplayTests`, `IndicatorKindParserTests`, `Common/Exceptions/ExceptionHierarchyTests`, `Common/Formatting/NumberFormatTests`, `Common/Time/RelativeTimeFormatterTests`).
- `TradyStrat.Application.Tests` — use cases, services, specifications, fakes (`FakeChatClient`, `StubAiClient`, `StubSnapshotFactory`, `StubFxProvider`, `StubPriceFeed`, `FakeSettingsReader`, `FakeClock`, `TestRepo`, `InMemoryDb`, `SeriesLoader`, `StubHttpHandler`).
- `TradyStrat.Infrastructure.Tests` — migration tests, real-DbContext tests (`MultiTickerAiPhase2MigrationTests`, `MigrationBackwardCompatTests`, `MultiTickerMigrationTests`, `SqlitePathResolverTests`, `PolymarketGammaProviderTests`, `YahooFxProviderTests`, `YahooPriceFeedTests`, `YahooParserTests`, `SuggestionBackfillCoordinatorTests`, `SettingsReaderTests`, `SettingsSeederTests`, `SettingEntryRoundtripTests`, `SettingsRegistryTests`, `SettingsServiceTests`).
- `TradyStrat.E2E.Tests` — module discovery + Blazor host smoke (`ModuleSmokeTests`, `SmokeTests.cs`).

---

## Phase 0 — Prerequisite: `TheAppManager` v3.0.0 (external repo)

The TheAppManager source lives in a separate user-owned repository. Bump to v3.0.0 and publish to NuGet **before** starting Phase 1. The TradyStrat refactor pulls the published package; it does not vendor TheAppManager source.

> **Scope note:** All work in this phase happens in the TheAppManager repo, not in TradyStrat. If the published v3.0.0 is already on the NuGet feed, skip directly to Phase 1.

### Task 0.1: Change `IAppModule` signature

**Files (in TheAppManager repo):**
- Modify: `src/TheAppManager/Modules/IAppModule.cs`

- [ ] **Step 1: Update the interface to a host-neutral signature**

```csharp
// src/TheAppManager/Modules/IAppModule.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TheAppManager.Modules;

/// <summary>
/// Defines a composable module for configuring an application's services and (for web hosts)
/// middleware and endpoints. Implement only what you need — all methods have no-op defaults.
/// </summary>
public interface IAppModule
{
    /// <summary>Host-neutral service registration. Both web and non-web hosts call this.</summary>
    void ConfigureServices(IServiceCollection services, IConfiguration config) { }

    /// <summary>Web-only middleware hook. Non-web hosts ignore this.</summary>
    void ConfigureMiddleware(WebApplication app) { }

    /// <summary>Web-only endpoint hook. Non-web hosts ignore this.</summary>
    void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
}
```

The breaking change: `ConfigureServices(WebApplicationBuilder)` → `ConfigureServices(IServiceCollection, IConfiguration)`. Default no-op bodies on all three methods so consumers can implement just one.

### Task 0.2: Add host-neutral `ConfigureServices` entry point — single-composition design

The v3 entry points all share one `AppModuleCollection` instance per call. `configureModules` is invoked **exactly once**; the resulting collection is the single source of truth for both DI registration and (where applicable) middleware/endpoint wiring. Re-invoking the user callback would diverge state if the user uses `AddIf`, `Replace`, or non-idempotent registrations.

**Files:**
- Modify: `src/TheAppManager/Startup/AppManager.cs`

- [ ] **Step 1: Add the host-neutral overload that composes a collection and returns it for further use**

```csharp
// New host-neutral overload — composes the modules into the supplied services and
// returns the composed AppModuleCollection so the web overload (and any future
// non-web host needing middleware-equivalent hooks) can reuse the same instance.
// Does NOT build or run anything.
public static AppModuleCollection ConfigureServices(
    IServiceCollection services,
    IConfiguration config,
    Action<AppModuleCollection> configureModules)
{
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(config);
    ArgumentNullException.ThrowIfNull(configureModules);

    var collection = new AppModuleCollection();
    configureModules(collection);

    foreach (var module in collection.GetModules())
    {
        module.ConfigureServices(services, config);
    }
    return collection;
}
```

- [ ] **Step 2: Rewrite the web `Start`/`StartAsync` overloads to reuse the same collection**

```csharp
public static void Start(
    string[] args,
    Action<AppModuleCollection> configureModules,
    Action<WebApplicationBuilder>? configureBuilder = null)
{
    var builder = WebApplication.CreateBuilder(args);
    configureBuilder?.Invoke(builder);

    // Compose once, hold onto the same collection for the middleware/endpoint passes.
    var modules = ConfigureServices(builder.Services, builder.Configuration, configureModules);

    var app = builder.Build();

    foreach (var module in modules.GetModules())
        module.ConfigureMiddleware(app);

    // WebApplication implements IEndpointRouteBuilder, so modules' ConfigureEndpoints
    // calls can target it directly. No MapWhen indirection needed.
    foreach (var module in modules.GetModules())
        module.ConfigureEndpoints(app);

    app.Run();
}

public static async Task StartAsync(
    string[] args,
    Action<AppModuleCollection> configureModules,
    Action<WebApplicationBuilder>? configureBuilder = null)
{
    var builder = WebApplication.CreateBuilder(args);
    configureBuilder?.Invoke(builder);
    var modules = ConfigureServices(builder.Services, builder.Configuration, configureModules);
    var app = builder.Build();
    foreach (var module in modules.GetModules()) module.ConfigureMiddleware(app);
    foreach (var module in modules.GetModules()) module.ConfigureEndpoints(app);
    await app.RunAsync();
}
```

The user's `configureModules` callback runs **once**; the same `AppModuleCollection` drives services + middleware + endpoints. `AddIf` / `Replace` semantics are now stable.

### Task 0.3: Add `AddFromAssemblyOf<T>` predicate overload for selective discovery

**Files:**
- Modify: `src/TheAppManager/Modules/AppModuleCollection.cs`

The CLI (Phase 7) and any future host that wants to skip background modules needs to exclude specific types during assembly scanning. Add a predicate overload.

- [ ] **Step 1: Add the predicate overload**

```csharp
public AppModuleCollection AddFromAssemblyOf<TMarker>(Func<Type, bool> predicate)
    => AddFromAssembly(typeof(TMarker).Assembly, predicate);

public AppModuleCollection AddFromAssembly(Assembly assembly, Func<Type, bool> predicate)
{
    ArgumentNullException.ThrowIfNull(assembly);
    ArgumentNullException.ThrowIfNull(predicate);

    foreach (var module in ModuleDiscovery.DiscoverModules(assembly))
    {
        if (predicate(module.GetType()))
            Add(module);
    }
    return this;
}
```

The existing parameterless `AddFromAssemblyOf<T>()` / `AddFromAssembly(Assembly)` overloads are kept unchanged — they're shorthand for `predicate = _ => true`.

- [ ] **Step 2: Verify with a quick unit test in TheAppManager's test project**

Skim the existing test fixtures; add one:

```csharp
[Fact]
public void AddFromAssemblyOf_with_predicate_skips_matching_types()
{
    var c = new AppModuleCollection();
    c.AddFromAssemblyOf<TestModuleA>(t => t != typeof(TestModuleB));
    c.GetModules().Should().ContainSingle(m => m is TestModuleA);
}
```

Run TheAppManager tests; verify the new test passes.

### Task 0.4: Bump version + publish

**Files:**
- Modify: `src/TheAppManager/TheAppManager.csproj` — `<Version>3.0.0</Version>` (and any nuspec metadata).
- Modify: `CHANGELOG.md` — add v3.0.0 entry noting the breaking change.

- [ ] **Step 1: Bump version to 3.0.0 and document the break**
- [ ] **Step 2: Build + pack: `dotnet pack -c Release`**
- [ ] **Step 3: Push to NuGet feed** (the user-controlled feed; mechanism depends on their pipeline)
- [ ] **Step 4: Verify the package resolves: `dotnet nuget locals all --clear && dotnet add <some-temp-project> package TheAppManager --version 3.0.0`**

Expected: package downloads from the feed.

- [ ] **Step 5: Commit in TheAppManager repo**

```bash
git add src/TheAppManager/Modules/IAppModule.cs src/TheAppManager/Startup/AppManager.cs src/TheAppManager/TheAppManager.csproj CHANGELOG.md
git commit -m "feat!: host-neutral module signature for v3.0.0

BREAKING CHANGE: IAppModule.ConfigureServices takes (IServiceCollection,
IConfiguration) instead of WebApplicationBuilder. New AppManager.ConfigureServices
overload composes modules into any IServiceCollection — enables non-web hosts
(CLI, worker) to share the same module pipeline."
git tag v3.0.0
git push --tags
```

---

## Phase 1 — Worktree + empty project scaffolding

Set up the worktree and create the four new csproj files plus the CLI csproj. No files move yet; the solution must build green at the end of this phase.

### Task 1.1: Create the worktree

- [ ] **Step 1: Invoke `superpowers:using-git-worktrees`** to create an isolated worktree for this refactor.

All subsequent task paths below are relative to the worktree root, which contains the TradyStrat solution.

### Task 1.2: Bump `TheAppManager` to v3.0.0 in central packages

**Files:**
- Modify: `Directory.Packages.props`

- [ ] **Step 1: Update the version pin**

Replace the existing `<PackageVersion Include="TheAppManager" Version="2.0.0" />` line with:

```xml
<PackageVersion Include="TheAppManager" Version="3.0.0" />
```

- [ ] **Step 2: Restore — this WILL break the build until Phase 6**

Run: `dotnet restore TradyStrat.slnx`

Expected: restore succeeds (the v3 package downloads), but `dotnet build` will fail with `IAppModule` signature errors. That's intentional — we'll fix module-by-module.

- [ ] **Step 3: Commit**

```bash
git add Directory.Packages.props
git commit -m "chore(deps): bump TheAppManager to v3.0.0

Breaks the build intentionally; modules will be migrated to the new
IAppModule(IServiceCollection, IConfiguration) signature across the
hexagonal refactor."
```

### Task 1.3: Create `TradyStrat.Domain` project

**Files:**
- Create: `TradyStrat.Domain/TradyStrat.Domain.csproj`
- Create: `TradyStrat.Domain/DomainAssemblyMarker.cs`

- [ ] **Step 1: Create the csproj**

```xml
<!-- TradyStrat.Domain/TradyStrat.Domain.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>TradyStrat.Domain</RootNamespace>
  </PropertyGroup>
</Project>
```

No package references, no project references. The Domain has zero deps beyond the BCL.

- [ ] **Step 2: Add the assembly marker**

```csharp
// TradyStrat.Domain/DomainAssemblyMarker.cs
namespace TradyStrat.Domain;

/// <summary>Empty marker for assembly-scanning module discovery.</summary>
public sealed class DomainAssemblyMarker;
```

- [ ] **Step 3: Verify Domain builds standalone**

Run: `dotnet build TradyStrat.Domain/TradyStrat.Domain.csproj`

Expected: build succeeds with no warnings.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat.Domain/
git commit -m "build: scaffold TradyStrat.Domain project skeleton"
```

### Task 1.4: Create `TradyStrat.Application` project

**Files:**
- Create: `TradyStrat.Application/TradyStrat.Application.csproj`
- Create: `TradyStrat.Application/ApplicationAssemblyMarker.cs`

- [ ] **Step 1: Create the csproj**

```xml
<!-- TradyStrat.Application/TradyStrat.Application.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>TradyStrat.Application</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\TradyStrat.Domain\TradyStrat.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="TheAppManager" />
    <PackageReference Include="Ardalis.Specification" />
    <PackageReference Include="Microsoft.Extensions.AI.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Atypical.TechnicalAnalysis.Common" />
    <PackageReference Include="Atypical.TechnicalAnalysis.Functions" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Add the marker**

```csharp
// TradyStrat.Application/ApplicationAssemblyMarker.cs
namespace TradyStrat.Application;

public sealed class ApplicationAssemblyMarker;
```

- [ ] **Step 3: Add `Microsoft.Extensions.AI.Abstractions` to central packages if not pinned separately**

Check `Directory.Packages.props`. If only `Microsoft.Extensions.AI` is pinned (the concrete), add the abstractions sibling at the same version:

```xml
<PackageVersion Include="Microsoft.Extensions.AI.Abstractions" Version="10.3.0" />
<PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.7" />
<PackageVersion Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.7" />
<PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.7" />
```

Versions must match the existing concrete packages.

- [ ] **Step 4: Verify**

Run: `dotnet build TradyStrat.Application/TradyStrat.Application.csproj`

Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat.Application/ Directory.Packages.props
git commit -m "build: scaffold TradyStrat.Application project skeleton"
```

### Task 1.5: Create `TradyStrat.Infrastructure` project

**Files:**
- Create: `TradyStrat.Infrastructure/TradyStrat.Infrastructure.csproj`
- Create: `TradyStrat.Infrastructure/InfrastructureAssemblyMarker.cs`

- [ ] **Step 1: Create the csproj**

```xml
<!-- TradyStrat.Infrastructure/TradyStrat.Infrastructure.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>TradyStrat.Infrastructure</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\TradyStrat.Application\TradyStrat.Application.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="TheAppManager" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" />
    <PackageReference Include="Ardalis.Specification.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.Extensions.AI" />
    <PackageReference Include="Anthropic.SDK" />
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" />
    <PackageReference Include="Microsoft.Extensions.Http" />
    <PackageReference Include="Serilog.AspNetCore" />
    <PackageReference Include="Serilog.Sinks.File" />
  </ItemGroup>
</Project>
```

If `Microsoft.Extensions.Http` is not in `Directory.Packages.props`, pin it at the matching `10.0.7` version.

- [ ] **Step 2: Add the marker**

```csharp
// TradyStrat.Infrastructure/InfrastructureAssemblyMarker.cs
namespace TradyStrat.Infrastructure;

public sealed class InfrastructureAssemblyMarker;
```

- [ ] **Step 3: Verify**

Run: `dotnet build TradyStrat.Infrastructure/TradyStrat.Infrastructure.csproj`

Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat.Infrastructure/ Directory.Packages.props
git commit -m "build: scaffold TradyStrat.Infrastructure project skeleton"
```

### Task 1.6: Create `TradyStrat.Cli` project skeleton

**Files:**
- Create: `TradyStrat.Cli/TradyStrat.Cli.csproj`
- Create: `TradyStrat.Cli/Program.cs` (stub — full content lands in Phase 7)

- [ ] **Step 1: Create the csproj**

```xml
<!-- TradyStrat.Cli/TradyStrat.Cli.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <RootNamespace>TradyStrat.Cli</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\TradyStrat.Application\TradyStrat.Application.csproj" />
    <ProjectReference Include="..\TradyStrat.Infrastructure\TradyStrat.Infrastructure.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="TheAppManager" />
    <PackageReference Include="Spectre.Console" />
    <PackageReference Include="Spectre.Console.Cli" />
    <PackageReference Include="Microsoft.Extensions.Hosting" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Pin the Spectre + Hosting packages in `Directory.Packages.props`**

Add (use the latest 0.49.x for Spectre, 10.0.7 for Hosting at time of writing — verify against current latest on NuGet):

```xml
<PackageVersion Include="Spectre.Console" Version="0.51.1" />
<PackageVersion Include="Spectre.Console.Cli" Version="0.51.1" />
<PackageVersion Include="Microsoft.Extensions.Hosting" Version="10.0.7" />
```

(`0.51.1` is the latest installed locally as of 2026-05-13; check `~/.nuget/packages/spectre.console/` for newer versions on NuGet before pinning.)

- [ ] **Step 3: Add a temporary `Program.cs` placeholder so the project compiles**

```csharp
// TradyStrat.Cli/Program.cs — placeholder, real content in Phase 7
System.Console.WriteLine("CLI placeholder — wiring lands in Phase 7.");
return 0;
```

- [ ] **Step 4: Verify**

Run: `dotnet build TradyStrat.Cli/TradyStrat.Cli.csproj`

Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat.Cli/ Directory.Packages.props
git commit -m "build: scaffold TradyStrat.Cli project skeleton with placeholder Program.cs"
```

### Task 1.7: Add the four projects to the solution

**Files:**
- Modify: `TradyStrat.slnx`

- [ ] **Step 1: Add the new project entries**

Replace:

```xml
  <Project Path="TradyStrat.Tests/TradyStrat.Tests.csproj" />
  <Project Path="TradyStrat/TradyStrat.csproj" />
```

with:

```xml
  <Project Path="TradyStrat.Domain/TradyStrat.Domain.csproj" />
  <Project Path="TradyStrat.Application/TradyStrat.Application.csproj" />
  <Project Path="TradyStrat.Infrastructure/TradyStrat.Infrastructure.csproj" />
  <Project Path="TradyStrat.Cli/TradyStrat.Cli.csproj" />
  <Project Path="TradyStrat.Tests/TradyStrat.Tests.csproj" />
  <Project Path="TradyStrat/TradyStrat.csproj" />
```

Test projects from Phase 8 are added later.

- [ ] **Step 2: Verify the solution builds the new projects**

Run: `dotnet build TradyStrat.Domain/TradyStrat.Domain.csproj TradyStrat.Application/TradyStrat.Application.csproj TradyStrat.Infrastructure/TradyStrat.Infrastructure.csproj TradyStrat.Cli/TradyStrat.Cli.csproj`

Expected: all four build successfully. The old `TradyStrat` project still won't build (TheAppManager v3 break) — that's expected and unblocks in later phases.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat.slnx
git commit -m "build: register four new projects in the solution"
```

---

## Phase 2 — Move the Domain layer

The Domain layer has zero internal dependencies, so it moves first. After this phase, every other layer's compile errors will say "type or namespace `TradyStrat.Common.Domain.X` could not be found" until they update their `using` directives — fix them progressively in later phases.

### Task 2.1: Move pure-domain types from `Common/Domain/`

**Files:**
- Move: every `*.cs` file in `TradyStrat/Common/Domain/` → `TradyStrat.Domain/<original-filename>.cs`

- [ ] **Step 1: Move all 26 files**

Run from the worktree root:

```bash
mkdir -p TradyStrat.Domain
git mv TradyStrat/Common/Domain/*.cs TradyStrat.Domain/
```

- [ ] **Step 2: Update namespaces in every moved file**

For each moved file, change the `namespace TradyStrat.Common.Domain;` line to `namespace TradyStrat.Domain;`. Use:

```bash
grep -l "namespace TradyStrat.Common.Domain" TradyStrat.Domain/*.cs | \
  xargs sed -i '' 's/namespace TradyStrat\.Common\.Domain;/namespace TradyStrat.Domain;/g'
```

(`sed -i ''` is BSD/macOS syntax. On GNU sed, drop the `''` after `-i`.)

- [ ] **Step 3: Update `using` directives inside Domain files**

Some Domain files reference each other and currently say `using TradyStrat.Common.Domain;` — that becomes redundant since they're now all in `TradyStrat.Domain`. Find and remove:

```bash
grep -l "using TradyStrat.Common.Domain" TradyStrat.Domain/*.cs | \
  xargs sed -i '' '/using TradyStrat\.Common\.Domain;/d'
```

- [ ] **Step 4: Verify Domain still builds**

Run: `dotnet build TradyStrat.Domain/TradyStrat.Domain.csproj`

Expected: build succeeds with zero warnings.

> **Watch for:** `Suggestion.cs` uses `System.Text.Json` for the `Citations` derived property. That's BCL — fine. If any file references types outside the BCL (e.g. `Ardalis.Specification`), that file is misplaced and belongs in Application — move it back to TradyStrat temporarily and revisit in Phase 3.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(domain): move pure-domain types from TradyStrat/Common/Domain/ to TradyStrat.Domain/

26 entities, value objects, and enums move with namespace rename only;
no behaviour change. The old project will not build until Application
+ Infrastructure are migrated in later phases."
```

### Task 2.2: Move `IClock` (the port lives in Domain)

**Files:**
- Move: `TradyStrat/Common/Time/IClock.cs` → `TradyStrat.Domain/IClock.cs`

- [ ] **Step 1: Move and update namespace**

```bash
git mv TradyStrat/Common/Time/IClock.cs TradyStrat.Domain/IClock.cs
sed -i '' 's/namespace TradyStrat\.Common\.Time;/namespace TradyStrat.Domain;/g' TradyStrat.Domain/IClock.cs
```

- [ ] **Step 2: Verify Domain still builds**

Run: `dotnet build TradyStrat.Domain/TradyStrat.Domain.csproj`

Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "refactor(domain): move IClock port to TradyStrat.Domain"
```

### Task 2.3: Move pure-domain exceptions

**Files:**
- Move 13 exception files from `TradyStrat/Common/Exceptions/` to `TradyStrat.Domain/Exceptions/`. The two vendor-specific exceptions stay behind for Infrastructure (Phase 4).

- [ ] **Step 1: Move the pure-domain exceptions**

```bash
mkdir -p TradyStrat.Domain/Exceptions
cd TradyStrat
for f in TradyStratException.cs FxRateUnavailableException.cs CsvImportException.cs \
         DuplicateInstrumentException.cs IndicatorComputationException.cs \
         InstrumentMetadataIncompleteException.cs InstrumentNotFoundException.cs \
         NoTradingDaysException.cs PolymarketUnavailableException.cs \
         PriceFeedUnavailableException.cs SettingValidationException.cs \
         TradeValidationException.cs UnsupportedCurrencyException.cs; do
  git mv "Common/Exceptions/$f" "../TradyStrat.Domain/Exceptions/$f"
done
cd ..
```

The two **left behind**: `AnthropicCallFailedException.cs`, `AnthropicConfigurationException.cs`. They move to Infrastructure in Phase 4.

- [ ] **Step 2: Update namespaces**

```bash
sed -i '' 's/namespace TradyStrat\.Common\.Exceptions;/namespace TradyStrat.Domain.Exceptions;/g' TradyStrat.Domain/Exceptions/*.cs
```

- [ ] **Step 3: Update internal references**

Some exceptions reference others (e.g. derive from `TradyStratException`). Change any `using TradyStrat.Common.Exceptions;` inside the moved files:

```bash
grep -l "using TradyStrat.Common.Exceptions" TradyStrat.Domain/Exceptions/*.cs | \
  xargs sed -i '' 's|using TradyStrat\.Common\.Exceptions;|using TradyStrat.Domain.Exceptions;|g'
```

- [ ] **Step 4: Verify Domain builds**

Run: `dotnet build TradyStrat.Domain/TradyStrat.Domain.csproj`

Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(domain): move pure-domain exceptions to TradyStrat.Domain/Exceptions/

Anthropic-specific exceptions stay in TradyStrat temporarily; they
relocate to Infrastructure in Phase 4 (and AnthropicCallFailedException
is renamed to AiCallFailedException at the Application boundary)."
```

---

## Phase 3 — Move the Application layer

This is the biggest phase. Application contains use cases, ports, application services, specifications, and pure utility code. It depends only on Domain.

### Task 3.1: Make `TradyStrat` reference Domain so it compiles incrementally

**Files:**
- Modify: `TradyStrat/TradyStrat.csproj`

- [ ] **Step 1: Add a `ProjectReference` to Domain**

Inside the existing `<ItemGroup>`:

```xml
<ProjectReference Include="..\TradyStrat.Domain\TradyStrat.Domain.csproj" />
```

- [ ] **Step 2: Update every `using TradyStrat.Common.Domain;` in TradyStrat to `using TradyStrat.Domain;`**

```bash
grep -rl "using TradyStrat\.Common\.Domain" TradyStrat --include="*.cs" --include="*.razor" | \
  xargs sed -i '' 's|using TradyStrat\.Common\.Domain;|using TradyStrat.Domain;|g'
```

- [ ] **Step 3: Update every `using TradyStrat.Common.Exceptions;` and `using TradyStrat.Common.Time;`**

```bash
grep -rl "using TradyStrat\.Common\.Exceptions" TradyStrat --include="*.cs" --include="*.razor" | \
  xargs sed -i '' 's|using TradyStrat\.Common\.Exceptions;|using TradyStrat.Domain.Exceptions;|g'

grep -rl "using TradyStrat\.Common\.Time" TradyStrat --include="*.cs" --include="*.razor" | \
  xargs sed -i '' 's|using TradyStrat\.Common\.Time;|using TradyStrat.Domain;|g'
```

> `IClock` moved into the `TradyStrat.Domain` root namespace — `using TradyStrat.Common.Time;` becomes `using TradyStrat.Domain;`.

- [ ] **Step 4: Tests project — apply the same find/replace**

```bash
grep -rl "using TradyStrat\.Common\.Domain" TradyStrat.Tests --include="*.cs" | \
  xargs sed -i '' 's|using TradyStrat\.Common\.Domain;|using TradyStrat.Domain;|g'

grep -rl "using TradyStrat\.Common\.Exceptions" TradyStrat.Tests --include="*.cs" | \
  xargs sed -i '' 's|using TradyStrat\.Common\.Exceptions;|using TradyStrat.Domain.Exceptions;|g'

grep -rl "using TradyStrat\.Common\.Time" TradyStrat.Tests --include="*.cs" | \
  xargs sed -i '' 's|using TradyStrat\.Common\.Time;|using TradyStrat.Domain;|g'
```

- [ ] **Step 5: Build TradyStrat and confirm only the *expected* errors remain**

Run: `dotnet build TradyStrat/TradyStrat.csproj`

Expected: the only remaining errors are about `IAppModule` (TheAppManager v3 signature mismatch on the 13 modules). Domain references are now satisfied. No `CS0246` errors against domain types.

> **Stop here and fix any unexpected errors before moving on.** Common surprises: `IClock` is referenced inside `Common/Time/SystemClock.cs` which is still in TradyStrat — it'll need `using TradyStrat.Domain;` to pick up the moved interface.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor: wire TradyStrat to TradyStrat.Domain + update usings

After this commit, TradyStrat compiles against the new Domain project.
The only outstanding errors are the 13 IAppModule signature mismatches
from TheAppManager v3, which clear in Phase 5."
```

### Task 3.2: Move `Common/UseCases/*` to Application

**Files:**
- Move: `TradyStrat/Common/UseCases/IUseCase.cs`, `Unit.cs`, `UseCaseBase.cs` → `TradyStrat.Application/UseCases/`

- [ ] **Step 1: Move + rename namespace**

```bash
mkdir -p TradyStrat.Application/UseCases
git mv TradyStrat/Common/UseCases/IUseCase.cs TradyStrat.Application/UseCases/
git mv TradyStrat/Common/UseCases/Unit.cs TradyStrat.Application/UseCases/
git mv TradyStrat/Common/UseCases/UseCaseBase.cs TradyStrat.Application/UseCases/

sed -i '' 's|namespace TradyStrat\.Common\.UseCases;|namespace TradyStrat.Application.UseCases;|g' TradyStrat.Application/UseCases/*.cs
```

- [ ] **Step 2: Update consumers in TradyStrat to reference Application**

Add Application reference to `TradyStrat/TradyStrat.csproj`:

```xml
<ProjectReference Include="..\TradyStrat.Application\TradyStrat.Application.csproj" />
```

Update usings across TradyStrat:

```bash
grep -rl "using TradyStrat\.Common\.UseCases" TradyStrat TradyStrat.Tests --include="*.cs" --include="*.razor" | \
  xargs sed -i '' 's|using TradyStrat\.Common\.UseCases;|using TradyStrat.Application.UseCases;|g'
```

- [ ] **Step 3: Verify Application builds**

Run: `dotnet build TradyStrat.Application/TradyStrat.Application.csproj`

Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(application): move UseCaseBase / Unit / IUseCase to TradyStrat.Application/UseCases"
```

### Task 3.3: Move `Common/Formatting/` and `Common/Time/RelativeTimeFormatter` to Application

**Files:**
- Move: `TradyStrat/Common/Formatting/NumberFormat.cs` → `TradyStrat.Application/Formatting/NumberFormat.cs`
- Move: `TradyStrat/Common/Time/RelativeTimeFormatter.cs` → `TradyStrat.Application/Time/RelativeTimeFormatter.cs`

> `SystemClock` (the impl) does NOT move yet — it goes to Infrastructure in Phase 4.

- [ ] **Step 1: Move + rename namespaces**

```bash
mkdir -p TradyStrat.Application/Formatting TradyStrat.Application/Time
git mv TradyStrat/Common/Formatting/NumberFormat.cs TradyStrat.Application/Formatting/
git mv TradyStrat/Common/Time/RelativeTimeFormatter.cs TradyStrat.Application/Time/

sed -i '' 's|namespace TradyStrat\.Common\.Formatting;|namespace TradyStrat.Application.Formatting;|g' TradyStrat.Application/Formatting/NumberFormat.cs
sed -i '' 's|namespace TradyStrat\.Common\.Time;|namespace TradyStrat.Application.Time;|g' TradyStrat.Application/Time/RelativeTimeFormatter.cs
```

- [ ] **Step 2: Update consumers**

```bash
grep -rl "using TradyStrat\.Common\.Formatting" TradyStrat TradyStrat.Tests --include="*.cs" --include="*.razor" | \
  xargs sed -i '' 's|using TradyStrat\.Common\.Formatting;|using TradyStrat.Application.Formatting;|g'

grep -rl "TradyStrat\.Common\.Time\.RelativeTimeFormatter" TradyStrat TradyStrat.Tests --include="*.cs" --include="*.razor" | \
  xargs sed -i '' 's|TradyStrat\.Common\.Time\.RelativeTimeFormatter|TradyStrat.Application.Time.RelativeTimeFormatter|g'
```

The `RelativeTimeFormatter` references may be qualified by type rather than `using`. The grep targets the qualified type name.

- [ ] **Step 3: Verify**

Run: `dotnet build TradyStrat.Application/TradyStrat.Application.csproj`

Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(application): move NumberFormat + RelativeTimeFormatter to TradyStrat.Application"
```

### Task 3.4: Move feature folder — AiSuggestion (Application side)

**Files:**
- Move: `TradyStrat/Features/AiSuggestion/` to `TradyStrat.Application/AiSuggestion/`, except `SuggestionService.cs` (Infrastructure — Phase 4).

The files moving:
- `IAiClient.cs`
- `JsonOpts.cs`
- `Snapshot/AiSnapshot.cs`
- `Snapshot/AiSnapshotService.cs`
- `Snapshot/IAiSnapshotService.cs`
- `Backfill/SuggestionBackfillCoordinator.cs`
- `Backfill/ISuggestionBackfillCoordinator.cs`
- `CallDiff/CallDiffBuilder.cs`
- `Specifications/*.cs` (all)
- `UseCases/*.cs` (all — `GetTodaysSuggestionUseCase`, `GetAllTodaysSuggestionsUseCase`, `ForceRefetchSuggestionUseCase`, `BackfillSuggestionsUseCase`, `SuggestionGate.cs`, plus the `*Input.cs` records)

- [ ] **Step 1: Move the directory tree (keeping `SuggestionService.cs` behind temporarily)**

```bash
mkdir -p TradyStrat.Application/AiSuggestion
mv TradyStrat/Features/AiSuggestion/SuggestionService.cs /tmp/SuggestionService.cs.bak
git mv TradyStrat/Features/AiSuggestion/* TradyStrat.Application/AiSuggestion/
mv /tmp/SuggestionService.cs.bak TradyStrat/Features/AiSuggestion/SuggestionService.cs
```

(The `SuggestionService.cs` shuffle keeps it under git tracking but out of the move — Phase 4 git-mvs it to Infrastructure.)

- [ ] **Step 2: Bulk-update namespaces in moved files**

```bash
find TradyStrat.Application/AiSuggestion -name "*.cs" -exec sed -i '' \
  's|namespace TradyStrat\.Features\.AiSuggestion|namespace TradyStrat.Application.AiSuggestion|g' {} +
```

Sub-namespaces like `TradyStrat.Features.AiSuggestion.Snapshot` become `TradyStrat.Application.AiSuggestion.Snapshot` automatically because the sed prefix-replace catches them.

- [ ] **Step 3: Update consumers in TradyStrat (Razor pages + remaining `SuggestionService.cs`)**

```bash
grep -rl "using TradyStrat\.Features\.AiSuggestion" TradyStrat TradyStrat.Tests --include="*.cs" --include="*.razor" | \
  xargs sed -i '' 's|using TradyStrat\.Features\.AiSuggestion|using TradyStrat.Application.AiSuggestion|g'
```

- [ ] **Step 4: Verify Application builds**

Run: `dotnet build TradyStrat.Application/TradyStrat.Application.csproj`

Expected: build succeeds.

> **Watch for:** any reference to `Microsoft.Extensions.AI` (concrete) inside a moved file. Application uses only `Microsoft.Extensions.AI.Abstractions`. If a use case touches `Microsoft.Extensions.AI`-only types, that's a signal the abstraction is leaking and needs a port.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(application): move AiSuggestion feature to TradyStrat.Application

SuggestionService.cs stays in TradyStrat temporarily; it relocates
to Infrastructure in Phase 4 (Anthropic adapter)."
```

### Task 3.5: Move feature folder — Indicators (Application)

**Files:**
- Move: `TradyStrat/Features/Indicators/` to `TradyStrat.Application/Indicators/` (entire subtree).

- [ ] **Step 1: Move + rename namespace**

```bash
mkdir -p TradyStrat.Application/Indicators
git mv TradyStrat/Features/Indicators/* TradyStrat.Application/Indicators/

find TradyStrat.Application/Indicators -name "*.cs" -exec sed -i '' \
  's|namespace TradyStrat\.Features\.Indicators|namespace TradyStrat.Application.Indicators|g' {} +
```

- [ ] **Step 2: Update consumers**

```bash
grep -rl "using TradyStrat\.Features\.Indicators" TradyStrat TradyStrat.Tests --include="*.cs" --include="*.razor" | \
  xargs sed -i '' 's|using TradyStrat\.Features\.Indicators|using TradyStrat.Application.Indicators|g'
```

- [ ] **Step 3: Verify**

Run: `dotnet build TradyStrat.Application/TradyStrat.Application.csproj`

Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(application): move Indicators feature to TradyStrat.Application"
```

### Task 3.6: Move feature folder — Fx (split: port + service to App, providers stay)

**Files:**
- Move to Application: `FxConverter.cs`, `DailyFxCache.cs`, `Specifications/*.cs`, `Providers/IFxRateProvider.cs`
- Stay in TradyStrat (will move to Infrastructure in Phase 4): `Providers/YahooFxProvider.cs`

- [ ] **Step 1: Move the Application-side files**

```bash
mkdir -p TradyStrat.Application/Fx/Providers TradyStrat.Application/Fx/Specifications
git mv TradyStrat/Features/Fx/FxConverter.cs TradyStrat.Application/Fx/
git mv TradyStrat/Features/Fx/DailyFxCache.cs TradyStrat.Application/Fx/
git mv TradyStrat/Features/Fx/Providers/IFxRateProvider.cs TradyStrat.Application/Fx/Providers/
git mv TradyStrat/Features/Fx/Specifications/*.cs TradyStrat.Application/Fx/Specifications/
```

- [ ] **Step 2: Rename namespaces**

```bash
find TradyStrat.Application/Fx -name "*.cs" -exec sed -i '' \
  's|namespace TradyStrat\.Features\.Fx|namespace TradyStrat.Application.Fx|g' {} +
```

- [ ] **Step 3: Update consumers + the YahooFxProvider that's still in TradyStrat**

```bash
grep -rl "using TradyStrat\.Features\.Fx" TradyStrat TradyStrat.Tests --include="*.cs" --include="*.razor" | \
  xargs sed -i '' 's|using TradyStrat\.Features\.Fx|using TradyStrat.Application.Fx|g'
```

> `YahooFxProvider.cs` references `IFxRateProvider` — after the using update it'll find it in `TradyStrat.Application.Fx.Providers`. That's expected; it stays a TradyStrat-resident implementation until Phase 4.

- [ ] **Step 4: Verify**

Run: `dotnet build TradyStrat.Application/TradyStrat.Application.csproj`

Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(application): move Fx port + FxConverter + DailyFxCache to TradyStrat.Application

YahooFxProvider stays in TradyStrat for now; it relocates to
Infrastructure in Phase 4."
```

### Task 3.7: Move feature folder — PriceFeed (split: port + cache to App, providers + hosted service stay)

**Files:**
- Move to Application: `DailyPriceCache.cs`, `Specifications/*.cs`, `UseCases/*.cs`, `Providers/IPriceFeed.cs`
- Stay in TradyStrat (Infrastructure in Phase 4): `Providers/YahooPriceFeed.cs`, `Providers/YahooParser.cs`, `PriceFeedHostedService.cs`

- [ ] **Step 1: Move Application-side files**

```bash
mkdir -p TradyStrat.Application/PriceFeed/{Providers,Specifications,UseCases}
git mv TradyStrat/Features/PriceFeed/DailyPriceCache.cs TradyStrat.Application/PriceFeed/
git mv TradyStrat/Features/PriceFeed/Specifications/*.cs TradyStrat.Application/PriceFeed/Specifications/
git mv TradyStrat/Features/PriceFeed/UseCases/*.cs TradyStrat.Application/PriceFeed/UseCases/
git mv TradyStrat/Features/PriceFeed/Providers/IPriceFeed.cs TradyStrat.Application/PriceFeed/Providers/
```

- [ ] **Step 2: Rename namespaces**

```bash
find TradyStrat.Application/PriceFeed -name "*.cs" -exec sed -i '' \
  's|namespace TradyStrat\.Features\.PriceFeed|namespace TradyStrat.Application.PriceFeed|g' {} +
```

- [ ] **Step 3: Update consumers**

```bash
grep -rl "using TradyStrat\.Features\.PriceFeed" TradyStrat TradyStrat.Tests --include="*.cs" --include="*.razor" | \
  xargs sed -i '' 's|using TradyStrat\.Features\.PriceFeed|using TradyStrat.Application.PriceFeed|g'
```

- [ ] **Step 4: Verify**

Run: `dotnet build TradyStrat.Application/TradyStrat.Application.csproj`

Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(application): move PriceFeed port + DailyPriceCache + use cases to TradyStrat.Application"
```

### Task 3.8: Move feature folder — PredictionMarkets (split)

**Verified actual structure** of `TradyStrat/Features/PredictionMarkets/`:
- Root: `IPredictionMarketProvider.cs`, `MarketCitation.cs`, `MarketSnapshot.cs`, `PolymarketFilter.cs`, `PolymarketRelevance.cs`, `PredictionMarket.cs`
- `Providers/`: `PolymarketGammaProvider.cs`, `PolymarketNormalizer.cs` (note: **Normalizer**, not Normalization — adapter-internal helper called by `PolymarketGammaProvider`)

**Files:**
- Move to Application: every `*.cs` at the PredictionMarkets root (the port + records + pure logic).
- Stay (Infrastructure in Phase 4): the entire `Providers/` folder — both `PolymarketGammaProvider.cs` AND `PolymarketNormalizer.cs` (the normalizer is adapter-internal, not pure logic).

- [ ] **Step 1: Move the Application-side files (root-level only — do NOT touch `Providers/`)**

```bash
mkdir -p TradyStrat.Application/PredictionMarkets
git mv TradyStrat/Features/PredictionMarkets/*.cs TradyStrat.Application/PredictionMarkets/
```

The shell glob `*.cs` matches only files at the immediate root, leaving `Providers/PolymarketGammaProvider.cs` and `Providers/PolymarketNormalizer.cs` behind for Phase 4.

- [ ] **Step 2: Rename namespaces**

```bash
find TradyStrat.Application/PredictionMarkets -name "*.cs" -exec sed -i '' \
  's|namespace TradyStrat\.Features\.PredictionMarkets|namespace TradyStrat.Application.PredictionMarkets|g' {} +
```

- [ ] **Step 3: Update consumers**

```bash
grep -rl "using TradyStrat\.Features\.PredictionMarkets" TradyStrat TradyStrat.Tests --include="*.cs" --include="*.razor" | \
  xargs sed -i '' 's|using TradyStrat\.Features\.PredictionMarkets|using TradyStrat.Application.PredictionMarkets|g'
```

- [ ] **Step 4: Verify**

Run: `dotnet build TradyStrat.Application/TradyStrat.Application.csproj`

Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(application): move PredictionMarkets port + pure logic to TradyStrat.Application

PolymarketGammaProvider HTTP adapter stays in TradyStrat for now;
moves to Infrastructure in Phase 4."
```

### Task 3.9: Move feature folder — Portfolio (entirely to Application)

**Files:**
- Move: every `*.cs` in `TradyStrat/Features/Portfolio/` → `TradyStrat.Application/Portfolio/`

- [ ] **Step 1: Move + rename**

```bash
mkdir -p TradyStrat.Application/Portfolio
git mv TradyStrat/Features/Portfolio/*.cs TradyStrat.Application/Portfolio/

find TradyStrat.Application/Portfolio -name "*.cs" -exec sed -i '' \
  's|namespace TradyStrat\.Features\.Portfolio|namespace TradyStrat.Application.Portfolio|g' {} +
```

- [ ] **Step 2: Update consumers**

```bash
grep -rl "using TradyStrat\.Features\.Portfolio" TradyStrat TradyStrat.Tests --include="*.cs" --include="*.razor" | \
  xargs sed -i '' 's|using TradyStrat\.Features\.Portfolio|using TradyStrat.Application.Portfolio|g'
```

- [ ] **Step 3: Verify + commit**

Run: `dotnet build TradyStrat.Application/TradyStrat.Application.csproj`

```bash
git add -A
git commit -m "refactor(application): move Portfolio feature to TradyStrat.Application"
```

### Task 3.10: Move feature folder — Trades (use cases + Csv service to Application; Components stay)

**Files:**
- Move to Application: `Trades/UseCases/*.cs`, `Trades/Specifications/*.cs`, `Trades/CsvImportService.cs`
- Stay in TradyStrat (Blazor presentation): `Trades/Components/*.razor`, `Trades/Components/*.razor.cs`, any other `*.razor*`

- [ ] **Step 1: Move Application-side files**

```bash
mkdir -p TradyStrat.Application/Trades/{UseCases,Specifications}
git mv TradyStrat/Features/Trades/UseCases/*.cs TradyStrat.Application/Trades/UseCases/
git mv TradyStrat/Features/Trades/Specifications/*.cs TradyStrat.Application/Trades/Specifications/
git mv TradyStrat/Features/Trades/CsvImportService.cs TradyStrat.Application/Trades/
# Any *.cs (not *.razor.cs) at the Trades root that isn't a code-behind moves too.
# Check manually: ls TradyStrat/Features/Trades/*.cs and discriminate.
```

> Manual check: list any other `.cs` files at the Trades root and decide on a case-by-case basis. If unsure, leave it in TradyStrat and revisit at the end of Phase 3.

- [ ] **Step 2: Rename namespaces in moved files**

```bash
find TradyStrat.Application/Trades -name "*.cs" -exec sed -i '' \
  's|namespace TradyStrat\.Features\.Trades|namespace TradyStrat.Application.Trades|g' {} +
```

- [ ] **Step 3: Update consumers**

```bash
grep -rl "using TradyStrat\.Features\.Trades\.UseCases\|using TradyStrat\.Features\.Trades\.Specifications" TradyStrat TradyStrat.Tests --include="*.cs" --include="*.razor" | \
  xargs sed -i '' 's|using TradyStrat\.Features\.Trades\.UseCases|using TradyStrat.Application.Trades.UseCases|g; s|using TradyStrat\.Features\.Trades\.Specifications|using TradyStrat.Application.Trades.Specifications|g'
```

For any reference to `CsvImportService`, use a qualified-name find/replace too. The Razor code-behinds in `Trades/Components/` likely consume the use cases — verify with `grep`.

- [ ] **Step 4: Verify + commit**

Run: `dotnet build TradyStrat.Application/TradyStrat.Application.csproj`

```bash
git add -A
git commit -m "refactor(application): move Trades use cases + CsvImportService to TradyStrat.Application

Razor components stay in TradyStrat (Blazor presentation layer)."
```

### Task 3.11: Move feature folder — Settings (Application-side)

**Verified actual structure** of `TradyStrat/Features/Settings/Config/`:
- Port interfaces: `ISettingsReader.cs`, `ISettingsService.cs`
- Records/registry: `SettingDescriptor.cs`, `SettingsKeys.cs`, `SettingsModels.cs`, `SettingsRegistry.cs`
- Concrete services: `SettingsService.cs` (pure-domain, uses repo via port), `SettingsReader.cs` (DB-backed adapter), `SettingsSeederHostedService.cs` (background seeder using DbContext)

There is **no** `SettingsSeeder.cs` (the original plan mistakenly named the seeder). There is **no** `Settings/Providers/` directory and **no** `YahooMetadataParser.cs` anywhere in the source.

**Files:**
- Move to Application: `Config/ISettingsReader.cs`, `Config/ISettingsService.cs`, `Config/SettingDescriptor.cs`, `Config/SettingsKeys.cs`, `Config/SettingsModels.cs`, `Config/SettingsRegistry.cs`, `Config/SettingsService.cs`, `Specifications/*.cs`, `UseCases/*.cs`.
- Stay (Infrastructure in Phase 4): `Config/SettingsReader.cs` (port impl), `Config/SettingsSeederHostedService.cs` (writes to DbContext on startup).
- Stay in TradyStrat: `Components/*.razor*`, `SettingsPage.razor*`.

- [ ] **Step 1: Move Application-side files**

```bash
mkdir -p TradyStrat.Application/Settings/{Config,Specifications,UseCases}

for f in ISettingsReader.cs ISettingsService.cs SettingDescriptor.cs SettingsKeys.cs SettingsModels.cs SettingsRegistry.cs SettingsService.cs; do
  [ -f "TradyStrat/Features/Settings/Config/$f" ] && \
    git mv "TradyStrat/Features/Settings/Config/$f" TradyStrat.Application/Settings/Config/
done

git mv TradyStrat/Features/Settings/Specifications/*.cs TradyStrat.Application/Settings/Specifications/
git mv TradyStrat/Features/Settings/UseCases/*.cs TradyStrat.Application/Settings/UseCases/
```

After this step, `TradyStrat/Features/Settings/Config/` contains exactly two files: `SettingsReader.cs` and `SettingsSeederHostedService.cs`. Both move to Infrastructure in Phase 4.

- [ ] **Step 2: Rename namespaces**

```bash
find TradyStrat.Application/Settings -name "*.cs" -exec sed -i '' \
  's|namespace TradyStrat\.Features\.Settings|namespace TradyStrat.Application.Settings|g' {} +
```

- [ ] **Step 3: Update consumers (Razor code-behinds, etc.)**

```bash
grep -rl "using TradyStrat\.Features\.Settings" TradyStrat TradyStrat.Tests --include="*.cs" --include="*.razor" | \
  xargs sed -i '' 's|using TradyStrat\.Features\.Settings|using TradyStrat.Application.Settings|g'
```

> `SettingsReader.cs` (still in TradyStrat) will pick up `using TradyStrat.Application.Settings.Config;` and find `ISettingsReader` correctly.

- [ ] **Step 4: Verify + commit**

Run: `dotnet build TradyStrat.Application/TradyStrat.Application.csproj`

```bash
git add -A
git commit -m "refactor(application): move Settings port + use cases + specs to TradyStrat.Application

SettingsReader (DB-backed adapter) and SettingsSeederHostedService
stay in TradyStrat for now; move to Infrastructure in Phase 4."
```

### Task 3.12: Move feature folder — Dashboard (Application-side)

**Files:**
- Move to Application: `Dashboard/UseCases/*`, `Dashboard/Navigation/*` (the pure-logic helpers), `Dashboard/GoalPaceCalculator.cs`
- Stay in TradyStrat: `Dashboard/Components/*.razor*`, `Dashboard/DashboardPage.razor*`

- [ ] **Step 1: Move pure-logic files**

```bash
mkdir -p TradyStrat.Application/Dashboard/{UseCases,Navigation}
git mv TradyStrat/Features/Dashboard/UseCases/*.cs TradyStrat.Application/Dashboard/UseCases/
git mv TradyStrat/Features/Dashboard/Navigation/*.cs TradyStrat.Application/Dashboard/Navigation/
git mv TradyStrat/Features/Dashboard/GoalPaceCalculator.cs TradyStrat.Application/Dashboard/
```

If any `Navigation/` files are `.razor` or `.razor.cs`, leave them in TradyStrat.

- [ ] **Step 2: Rename namespaces**

```bash
find TradyStrat.Application/Dashboard -name "*.cs" -exec sed -i '' \
  's|namespace TradyStrat\.Features\.Dashboard|namespace TradyStrat.Application.Dashboard|g' {} +
```

- [ ] **Step 3: Update consumers**

```bash
grep -rl "using TradyStrat\.Features\.Dashboard\.UseCases\|using TradyStrat\.Features\.Dashboard\.Navigation" TradyStrat TradyStrat.Tests --include="*.cs" --include="*.razor" | \
  xargs sed -i '' 's|using TradyStrat\.Features\.Dashboard\.UseCases|using TradyStrat.Application.Dashboard.UseCases|g; s|using TradyStrat\.Features\.Dashboard\.Navigation|using TradyStrat.Application.Dashboard.Navigation|g'
```

Any reference to the bare `TradyStrat.Features.Dashboard.GoalPaceCalculator` qualified name:

```bash
grep -rl "TradyStrat\.Features\.Dashboard\.GoalPaceCalculator" TradyStrat TradyStrat.Tests --include="*.cs" --include="*.razor" | \
  xargs sed -i '' 's|TradyStrat\.Features\.Dashboard\.GoalPaceCalculator|TradyStrat.Application.Dashboard.GoalPaceCalculator|g'
```

- [ ] **Step 4: Verify + commit**

```bash
git add -A
git commit -m "refactor(application): move Dashboard use cases + navigation logic to TradyStrat.Application"
```

### Task 3.13: Confirm Application builds cleanly

- [ ] **Step 1: Full Application build**

Run: `dotnet build TradyStrat.Application/TradyStrat.Application.csproj`

Expected: build succeeds with zero warnings.

- [ ] **Step 2: List anything still in `TradyStrat/Common/` or `TradyStrat/Features/<feature>/` that's NOT Razor**

Run:
```bash
find TradyStrat/Common -name "*.cs" 2>/dev/null
find TradyStrat/Features -name "*.cs" ! -name "*.razor.cs" 2>/dev/null
```

Expected after Phase 3:
- `TradyStrat/Common/Exceptions/AnthropicCallFailedException.cs` (moves Phase 4)
- `TradyStrat/Common/Exceptions/AnthropicConfigurationException.cs` (moves Phase 4)
- `TradyStrat/Common/Time/SystemClock.cs` (moves Phase 4)
- `TradyStrat/Features/AiSuggestion/SuggestionService.cs` (moves Phase 4)
- `TradyStrat/Features/Fx/Providers/YahooFxProvider.cs` (moves Phase 4)
- `TradyStrat/Features/PriceFeed/Providers/YahooPriceFeed.cs`, `YahooParser.cs`, `PriceFeedHostedService.cs` (Phase 4)
- `TradyStrat/Features/PredictionMarkets/Providers/PolymarketGammaProvider.cs` (Phase 4)
- `TradyStrat/Features/Settings/Config/SettingsReader.cs`, `SettingsSeederHostedService.cs` (Phase 4)
- `TradyStrat/Data/AppDbContext.cs`, all `Data/Migrations/*.cs`, `Data/SqlitePathResolver.cs` (Phase 4)
- `TradyStrat/Modules/*.cs` (Phase 5)
- `TradyStrat/Program.cs` (Phase 6)

If anything else remains, decide whether it's Application-bound (move now) or Blazor-presentation (leave). Do not skip this step — leftover files are how refactors leak.

- [ ] **Step 3: Commit a checkpoint**

```bash
git commit --allow-empty -m "checkpoint: Application layer migration complete"
```

---

## Phase 4 — Move the Infrastructure layer

Everything that talks to the outside world — EF, Anthropic SDK, HTTP providers, file logging, vendor exceptions.

### Task 4.1: Move EF `AppDbContext` + migrations + entity configurations

**Files:**
- Move: every file in `TradyStrat/Data/` → `TradyStrat.Infrastructure/Data/`

- [ ] **Step 1: Move the entire Data folder**

```bash
mkdir -p TradyStrat.Infrastructure/Data
git mv TradyStrat/Data/* TradyStrat.Infrastructure/Data/
```

- [ ] **Step 2: Rename namespaces**

```bash
find TradyStrat.Infrastructure/Data -name "*.cs" -exec sed -i '' \
  's|namespace TradyStrat\.Data|namespace TradyStrat.Infrastructure.Data|g' {} +
```

- [ ] **Step 3: Update consumers in TradyStrat**

```bash
grep -rl "using TradyStrat\.Data" TradyStrat TradyStrat.Tests --include="*.cs" --include="*.razor" | \
  xargs sed -i '' 's|using TradyStrat\.Data|using TradyStrat.Infrastructure.Data|g'
```

- [ ] **Step 4: Add Infrastructure reference to TradyStrat**

In `TradyStrat/TradyStrat.csproj`:

```xml
<ProjectReference Include="..\TradyStrat.Infrastructure\TradyStrat.Infrastructure.csproj" />
```

- [ ] **Step 5: Verify**

Run: `dotnet build TradyStrat.Infrastructure/TradyStrat.Infrastructure.csproj`

Expected: build succeeds. EF migration assembly attribute (if any) inside the migrations folder may need a tweak — check `Data/Migrations/*Designer.cs` for `[DbContext(typeof(...))]` references; they should resolve via the updated namespaces.

- [ ] **Step 6: Update CI/local EF commands**

Any docs / scripts referencing `dotnet ef --project TradyStrat ...` change to `dotnet ef --project TradyStrat.Infrastructure --startup-project TradyStrat ...`. Search the repo:

```bash
grep -rn "dotnet ef" README.md docs/ 2>/dev/null
```

Update any hits.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "refactor(infrastructure): move EF AppDbContext + migrations + configurations to TradyStrat.Infrastructure

EF commands now use --project TradyStrat.Infrastructure --startup-project TradyStrat."
```

### Task 4.2: Move SuggestionService (Anthropic adapter)

**Files:**
- Move: `TradyStrat/Features/AiSuggestion/SuggestionService.cs` → `TradyStrat.Infrastructure/AiSuggestion/SuggestionService.cs`

- [ ] **Step 1: Move + rename namespace**

```bash
mkdir -p TradyStrat.Infrastructure/AiSuggestion
git mv TradyStrat/Features/AiSuggestion/SuggestionService.cs TradyStrat.Infrastructure/AiSuggestion/

sed -i '' 's|namespace TradyStrat\.Features\.AiSuggestion;|namespace TradyStrat.Infrastructure.AiSuggestion;|g' TradyStrat.Infrastructure/AiSuggestion/SuggestionService.cs
```

- [ ] **Step 2: Add `using TradyStrat.Application.AiSuggestion;` at the top**

`SuggestionService` references `IAiClient`, `IAiSnapshotService`, `AiSnapshot`, `JsonOpts`, `IAiSnapshot...` — all in `TradyStrat.Application.AiSuggestion`. Update its imports:

Open `TradyStrat.Infrastructure/AiSuggestion/SuggestionService.cs` and add at the top, after the existing `using` block:

```csharp
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.Snapshot;
```

Remove any `using TradyStrat.Features.AiSuggestion;` (now invalid).

- [ ] **Step 3: Build Infrastructure**

Run: `dotnet build TradyStrat.Infrastructure/TradyStrat.Infrastructure.csproj`

Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(infrastructure): move SuggestionService (Anthropic adapter) to TradyStrat.Infrastructure"
```

### Task 4.3: Move + rename Anthropic exceptions

**Files:**
- Move: `TradyStrat/Common/Exceptions/AnthropicCallFailedException.cs` → `TradyStrat.Infrastructure/Exceptions/AnthropicCallFailedException.cs`
- Move: `TradyStrat/Common/Exceptions/AnthropicConfigurationException.cs` → `TradyStrat.Infrastructure/Exceptions/AnthropicConfigurationException.cs`
- Create: `TradyStrat.Application/Exceptions/AiCallFailedException.cs`

- [ ] **Step 1: Move the two vendor exceptions to Infrastructure**

```bash
mkdir -p TradyStrat.Infrastructure/Exceptions
git mv TradyStrat/Common/Exceptions/AnthropicCallFailedException.cs TradyStrat.Infrastructure/Exceptions/
git mv TradyStrat/Common/Exceptions/AnthropicConfigurationException.cs TradyStrat.Infrastructure/Exceptions/

sed -i '' 's|namespace TradyStrat\.Common\.Exceptions;|namespace TradyStrat.Infrastructure.Exceptions;|g' TradyStrat.Infrastructure/Exceptions/*.cs
```

- [ ] **Step 2: Create the abstract Application-layer exception**

```csharp
// TradyStrat.Application/Exceptions/AiCallFailedException.cs
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Application.Exceptions;

/// <summary>
/// Abstract failure mode at the IAiClient port boundary. Infrastructure
/// adapters (Anthropic, future Gemini, …) raise vendor-specific subclasses
/// of this type so Application catch sites use one abstract name and never
/// import vendor packages.
/// </summary>
public class AiCallFailedException : TradyStratException
{
    public AiCallFailedException(string message) : base(message) { }
    public AiCallFailedException(string message, Exception inner) : base(message, inner) { }
}
```

> **Why inherit from `TradyStratException` (not `Exception`):** the current `AnthropicCallFailedException` is `: TradyStratException`, and `SuggestionBackfillCoordinator.cs:123` catches `TradyStratException` to halt the backfill chain. If `AiCallFailedException` were `: Exception`, then `AnthropicCallFailedException` (re-parented in Step 3) would no longer be a `TradyStratException`, and the coordinator would silently stop handling AI failures during backfill — a behaviour-breaking change. Keeping the new abstraction inside the existing domain-exception hierarchy preserves every existing catch site. `ExceptionHierarchyTests` continues to pass; the test gains a new assertion that `AiCallFailedException` is itself `ShouldBeAssignableTo<TradyStratException>()`.

> Note the class is **not `sealed`**: `AnthropicCallFailedException` inherits from it in Step 3.

- [ ] **Step 3: Make `AnthropicCallFailedException` inherit from `AiCallFailedException`**

The current source (`TradyStrat/Common/Exceptions/AnthropicCallFailedException.cs:3`) is:

```csharp
public sealed class AnthropicCallFailedException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
```

After the move to Infrastructure, it becomes:

```csharp
// TradyStrat.Infrastructure/Exceptions/AnthropicCallFailedException.cs
using TradyStrat.Application.Exceptions;

namespace TradyStrat.Infrastructure.Exceptions;

public sealed class AnthropicCallFailedException(string message, Exception? inner = null)
    : AiCallFailedException(message, inner!);
```

`AiCallFailedException` itself inherits from `TradyStratException` (Step 2), so the existing transitive hierarchy `AnthropicCallFailedException → TradyStratException → Exception` is preserved — just with `AiCallFailedException` slotted in between. `SuggestionBackfillCoordinator.cs:123`'s `catch (TradyStratException)` still matches. CLR substitutability handles the Application/Infrastructure boundary without a try/catch rewrap.

- [ ] **Step 4: Update callers in Application that catch the old name**

```bash
grep -rln "AnthropicCallFailedException" TradyStrat.Application --include="*.cs"
```

For each hit, change `catch (AnthropicCallFailedException ...)` to `catch (AiCallFailedException ...)` and update the `using` import accordingly. Most catches will be inside use cases like `GetTodaysSuggestionUseCase` and `BackfillSuggestionsUseCase`.

- [ ] **Step 5: Update callers in TradyStrat (Razor code-behinds, Modules)**

```bash
grep -rln "AnthropicCallFailedException" TradyStrat TradyStrat.Tests --include="*.cs" --include="*.razor"
```

For Application callers (and Razor pages that catch the type): swap to `AiCallFailedException` + the Application namespace.
For Infrastructure callers (only `SuggestionService` itself throws it): keep `AnthropicCallFailedException`.

- [ ] **Step 6: Verify**

Run: `dotnet build TradyStrat.Infrastructure/TradyStrat.Infrastructure.csproj && dotnet build TradyStrat.Application/TradyStrat.Application.csproj`

Expected: both succeed.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "refactor: split AiCallFailedException (Application) from AnthropicCallFailedException (Infrastructure)

Vendor exception inherits from the abstract Application exception so
catch sites in use cases never import Anthropic.SDK."
```

### Task 4.4: Move `SystemClock` to Infrastructure

**Files:**
- Move: `TradyStrat/Common/Time/SystemClock.cs` → `TradyStrat.Infrastructure/Time/SystemClock.cs`

- [ ] **Step 1: Move + rename**

```bash
mkdir -p TradyStrat.Infrastructure/Time
git mv TradyStrat/Common/Time/SystemClock.cs TradyStrat.Infrastructure/Time/

sed -i '' 's|namespace TradyStrat\.Common\.Time;|namespace TradyStrat.Infrastructure.Time;|g' TradyStrat.Infrastructure/Time/SystemClock.cs
```

The `using TradyStrat.Common.Time;` already-replaced Phase 3 imports now miss `SystemClock` — but `SystemClock` is only used in module wiring, which is rewritten entirely in Phase 5. Leave the (broken-for-now) module references; we replace them wholesale.

- [ ] **Step 2: Delete the now-empty `TradyStrat/Common/Time/` directory**

```bash
[ -z "$(ls TradyStrat/Common/Time 2>/dev/null)" ] && rmdir TradyStrat/Common/Time
```

- [ ] **Step 3: Verify Infrastructure builds**

Run: `dotnet build TradyStrat.Infrastructure/TradyStrat.Infrastructure.csproj`

Expected: succeeds.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(infrastructure): move SystemClock to TradyStrat.Infrastructure"
```

### Task 4.5: Move HTTP adapters + DB-backed Settings impls + EfRepositoryShim

**Files to move:**
- `TradyStrat/Features/Fx/Providers/YahooFxProvider.cs` → `TradyStrat.Infrastructure/Fx/Providers/YahooFxProvider.cs`
- `TradyStrat/Features/PriceFeed/Providers/YahooPriceFeed.cs`, `YahooParser.cs` → `TradyStrat.Infrastructure/PriceFeed/Providers/`
- `TradyStrat/Features/PriceFeed/PriceFeedHostedService.cs` → `TradyStrat.Infrastructure/PriceFeed/`
- `TradyStrat/Features/PredictionMarkets/Providers/PolymarketGammaProvider.cs` → `TradyStrat.Infrastructure/PredictionMarkets/Providers/`
- `TradyStrat/Features/PredictionMarkets/Providers/PolymarketNormalizer.cs` → `TradyStrat.Infrastructure/PredictionMarkets/Providers/` (adapter-internal helper)
- `TradyStrat/Features/Settings/Config/SettingsReader.cs` → `TradyStrat.Infrastructure/Settings/Config/`
- `TradyStrat/Features/Settings/Config/SettingsSeederHostedService.cs` → `TradyStrat.Infrastructure/Settings/Config/`
- `EfRepositoryShim<T>` (the internal generic Ardalis adapter currently defined at the bottom of `TradyStrat/Modules/DatabaseModule.cs`) → extract to `TradyStrat.Infrastructure/Data/EfRepositoryShim.cs` before deleting `DatabaseModule.cs` in Phase 5.

There is **no** `YahooMetadataParser` in the source — earlier plan drafts named it incorrectly.

- [ ] **Step 1: Move FX provider**

```bash
mkdir -p TradyStrat.Infrastructure/Fx/Providers
git mv TradyStrat/Features/Fx/Providers/YahooFxProvider.cs TradyStrat.Infrastructure/Fx/Providers/

sed -i '' 's|namespace TradyStrat\.Features\.Fx\.Providers;|namespace TradyStrat.Infrastructure.Fx.Providers;|g' TradyStrat.Infrastructure/Fx/Providers/YahooFxProvider.cs
```

Add to the top of `YahooFxProvider.cs`:

```csharp
using TradyStrat.Application.Fx.Providers; // IFxRateProvider
```

- [ ] **Step 2: Move PriceFeed providers**

```bash
mkdir -p TradyStrat.Infrastructure/PriceFeed/Providers
git mv TradyStrat/Features/PriceFeed/Providers/YahooPriceFeed.cs TradyStrat.Infrastructure/PriceFeed/Providers/
git mv TradyStrat/Features/PriceFeed/Providers/YahooParser.cs TradyStrat.Infrastructure/PriceFeed/Providers/

find TradyStrat.Infrastructure/PriceFeed/Providers -name "*.cs" -exec sed -i '' \
  's|namespace TradyStrat\.Features\.PriceFeed\.Providers;|namespace TradyStrat.Infrastructure.PriceFeed.Providers;|g' {} +
```

Add `using TradyStrat.Application.PriceFeed.Providers;` to `YahooPriceFeed.cs`.

- [ ] **Step 3: Move PriceFeedHostedService**

```bash
git mv TradyStrat/Features/PriceFeed/PriceFeedHostedService.cs TradyStrat.Infrastructure/PriceFeed/
sed -i '' 's|namespace TradyStrat\.Features\.PriceFeed;|namespace TradyStrat.Infrastructure.PriceFeed;|g' TradyStrat.Infrastructure/PriceFeed/PriceFeedHostedService.cs
```

Add `using TradyStrat.Application.PriceFeed;` for the cache and any orchestration types.

- [ ] **Step 4: Move Polymarket provider**

```bash
mkdir -p TradyStrat.Infrastructure/PredictionMarkets/Providers
git mv TradyStrat/Features/PredictionMarkets/Providers/PolymarketGammaProvider.cs TradyStrat.Infrastructure/PredictionMarkets/Providers/

sed -i '' 's|namespace TradyStrat\.Features\.PredictionMarkets\.Providers;|namespace TradyStrat.Infrastructure.PredictionMarkets.Providers;|g' TradyStrat.Infrastructure/PredictionMarkets/Providers/PolymarketGammaProvider.cs
```

Add `using TradyStrat.Application.PredictionMarkets;` (or `...Providers;`, wherever `IPredictionMarketProvider` ended up).

- [ ] **Step 5: Move SettingsReader + SettingsSeederHostedService**

```bash
mkdir -p TradyStrat.Infrastructure/Settings/Config
git mv TradyStrat/Features/Settings/Config/SettingsReader.cs TradyStrat.Infrastructure/Settings/Config/
git mv TradyStrat/Features/Settings/Config/SettingsSeederHostedService.cs TradyStrat.Infrastructure/Settings/Config/

find TradyStrat.Infrastructure/Settings -name "*.cs" -exec sed -i '' \
  's|namespace TradyStrat\.Features\.Settings|namespace TradyStrat.Infrastructure.Settings|g' {} +
```

Add `using TradyStrat.Application.Settings.Config;` to both moved files (they reference `ISettingsReader`, `ISettingsService`, `ISettingsRegistry`, `SettingDescriptor`, etc., which all moved to Application in Phase 3.11).

- [ ] **Step 5b: Extract `EfRepositoryShim<T>` from `DatabaseModule.cs` into its own file in Infrastructure**

The current `TradyStrat/Modules/DatabaseModule.cs` ends with:

```csharp
internal sealed class EfRepositoryShim<T>(AppDbContext db) : RepositoryBase<T>(db) where T : class { }
```

Before deleting `DatabaseModule.cs` in Phase 5, extract this class:

```csharp
// TradyStrat.Infrastructure/Data/EfRepositoryShim.cs
using Ardalis.Specification.EntityFrameworkCore;

namespace TradyStrat.Infrastructure.Data;

internal sealed class EfRepositoryShim<T>(AppDbContext db) : RepositoryBase<T>(db) where T : class { }
```

The shim is used by `DatabaseInfrastructureModule` (Task 5.4) to register `IRepositoryBase<>` and `IReadRepositoryBase<>` against EF.

- [ ] **Step 6: Verify Infrastructure builds**

Run: `dotnet build TradyStrat.Infrastructure/TradyStrat.Infrastructure.csproj`

Expected: succeeds. Compile errors here mean a `using` is missing — read the error, add the import, rebuild.

- [ ] **Step 7: Clean up the now-empty Features subdirectories in TradyStrat**

```bash
find TradyStrat/Features -type d -empty -delete
find TradyStrat/Common -type d -empty -delete
```

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(infrastructure): move all HTTP adapters + PriceFeedHostedService to TradyStrat.Infrastructure"
```

### Task 4.6: Confirm Infrastructure builds cleanly + checkpoint

- [ ] **Step 1: Full Infrastructure build**

Run: `dotnet build TradyStrat.Infrastructure/TradyStrat.Infrastructure.csproj`

Expected: zero errors, zero warnings.

- [ ] **Step 2: List anything still in TradyStrat that isn't Razor or Modules or Program.cs**

```bash
find TradyStrat -name "*.cs" ! -name "*.razor.cs" ! -path "*/obj/*" ! -path "*/bin/*"
```

Expected: only `Modules/*.cs` and `Program.cs` should remain at this point.

Anything else is misplaced — investigate and move before continuing.

- [ ] **Step 3: Checkpoint commit**

```bash
git commit --allow-empty -m "checkpoint: Infrastructure layer migration complete; Modules and Program.cs remain"
```

---

## Phase 5 — Replace `Modules/*.cs` with per-layer feature modules

The 13 existing modules in `TradyStrat/Modules/` go away. Each feature gets one Application module (registering ports + use cases) and optionally one Infrastructure module (registering adapters).

### Task 5.1: Domain assembly marker (verify it exists)

Already created in Task 1.3. Confirm the file `TradyStrat.Domain/DomainAssemblyMarker.cs` exists and compiles.

- [ ] **Step 1: Verify**

```bash
test -f TradyStrat.Domain/DomainAssemblyMarker.cs && echo "OK" || echo "MISSING"
```

No commit needed.

### Task 5.2: Application feature modules — AiSuggestion

**Files:**
- Create: `TradyStrat.Application/AiSuggestion/AiSuggestionApplicationModule.cs`

- [ ] **Step 1: Write the failing test (decoupled — verifies the module registers the use cases against IServiceCollection)**

Defer detailed tests to Phase 8's test project split. For now, a build-time smoke verification is enough — the module gets exercised by Phase 9's full `dotnet test` run against `ModuleSmokeTests`.

- [ ] **Step 2: Write the module**

```csharp
// TradyStrat.Application/AiSuggestion/AiSuggestionApplicationModule.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Application.AiSuggestion.Backfill;

namespace TradyStrat.Application.AiSuggestion;

public sealed class AiSuggestionApplicationModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IAiSnapshotService, AiSnapshotService>();
        services.AddScoped<GetTodaysSuggestionUseCase>();
        services.AddScoped<GetAllTodaysSuggestionsUseCase>();
        services.AddScoped<ForceRefetchSuggestionUseCase>();
        services.AddScoped<BackfillSuggestionsUseCase>();
        services.AddSingleton<ISuggestionBackfillCoordinator>(sp =>
            new SuggestionBackfillCoordinator(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SuggestionBackfillCoordinator>>()));
    }
}
```

Cross-check against the existing `TradyStrat/Modules/AiSuggestionModule.cs` — every `services.Add...` registration in the old module that's for an Application-side type must appear here.

- [ ] **Step 3: Build Application**

Run: `dotnet build TradyStrat.Application/TradyStrat.Application.csproj`

Expected: succeeds.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat.Application/AiSuggestion/AiSuggestionApplicationModule.cs
git commit -m "refactor(application): add AiSuggestionApplicationModule"
```

### Task 5.3: Infrastructure feature module — AiSuggestion

**Files:**
- Create: `TradyStrat.Infrastructure/AiSuggestion/AiSuggestionInfrastructureModule.cs`

- [ ] **Step 1: Write the module**

```csharp
// TradyStrat.Infrastructure/AiSuggestion/AiSuggestionInfrastructureModule.cs
using Anthropic.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Infrastructure.Exceptions;

namespace TradyStrat.Infrastructure.AiSuggestion;

public sealed class AiSuggestionInfrastructureModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        var apiKey = config["Anthropic:ApiKey"]
            ?? throw new AnthropicConfigurationException("Anthropic:ApiKey is not configured.");

        services.AddSingleton<IChatClient>(_ =>
            new AnthropicClient(apiKey)
                .Messages
                .AsBuilder()
                .UseFunctionInvocation()
                .Build());

        services.AddScoped<IAiClient, SuggestionService>();
    }
}
```

This mirrors the Anthropic-related half of the old `AiSuggestionModule`. The Application module (5.2) handled the use cases + snapshot service + backfill coordinator.

- [ ] **Step 2: Build Infrastructure**

Run: `dotnet build TradyStrat.Infrastructure/TradyStrat.Infrastructure.csproj`

Expected: succeeds.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat.Infrastructure/AiSuggestion/AiSuggestionInfrastructureModule.cs
git commit -m "refactor(infrastructure): add AiSuggestionInfrastructureModule (Anthropic adapter wiring)"
```

### Task 5.4: Remaining Application + Infrastructure feature modules

Repeat the Task 5.2/5.3 pattern for each feature. Refer to the table in the spec (§5) for the full mapping. The list is reproduced here for convenience:

| Feature | Application module | Infrastructure module(s) | Where else |
|---|---|---|---|
| AiSuggestion | ✓ (Task 5.2) | ✓ (Task 5.3) | — |
| Dashboard | `DashboardApplicationModule` — `services.AddScoped<LoadDashboardUseCase>()`, `services.AddScoped<IEntryNavigationService, EntryNavigationService>()` | — | — |
| Database | — | `DatabaseInfrastructureModule` — resolves dbPath via `SqlitePathResolver`, registers `AppDbContext` with `UseSqlite`, registers open-generic `IRepositoryBase<>` and `IReadRepositoryBase<>` → `EfRepositoryShim<>`, registers `IClock` → `SystemClock`. Also implements `ConfigureMiddleware(WebApplication)` to call `Database.Migrate()` at startup (mirrors current `DatabaseModule.ConfigureMiddleware`). | — |
| Fx | `FxApplicationModule` — `services.AddScoped<DailyFxCache>()`, `services.AddScoped<FxConverter>()` | `FxInfrastructureModule` — `services.AddHttpClient<IFxRateProvider, YahooFxProvider>(...)` with base URL `https://query1.finance.yahoo.com`, 15s timeout, `TradyStrat/1.0` UA, `.AddStandardResilienceHandler()` | — |
| Logging | — | `LoggingInfrastructureModule` — full Serilog wiring from current `LoggingModule.cs` (file sink under `~/Library/Application Support/TradyStrat/logs`, console sink, daily rolling, 14-day retention) | — |
| Indicators | `IndicatorsApplicationModule` — four `AddSingleton<IZoneRule, ...>` (Bollinger, Rsi, MovingAverage, Ichimoku), `AddScoped<ZoneClassifier>()`, four `AddScoped<IIndicatorHistoryProvider, ...>`, `AddScoped<IIndicatorHistoryProviderFactory, IndicatorHistoryProviderFactory>()`, `AddScoped<IndicatorEngine>()` | — | — |
| Portfolio | `PortfolioApplicationModule` — `AddScoped<PortfolioService>()`, `AddScoped<GrowthSeriesBuilder>()` | — | — |
| PredictionMarkets | — | `PredictionMarketsInfrastructureModule` — `AddHttpClient<IPredictionMarketProvider, PolymarketGammaProvider>(...)` with base URL `https://gamma-api.polymarket.com`, 10s timeout, UA, resilience | — |
| PriceFeed | `PriceFeedApplicationModule` — `AddScoped<DailyPriceCache>()`, `AddScoped<RefreshAllPricesUseCase>()` (absorbed from the deleted `PricesUseCasesModule`) | `PriceFeedInfrastructureModule` — `AddHttpClient<IPriceFeed, YahooPriceFeed>(...)` only. AND `PriceFeedBackgroundInfrastructureModule` — `AddHostedService<PriceFeedHostedService>()` only (kept separate so the CLI can exclude it via the predicate overload from Task 0.3). | — |
| Settings | `SettingsApplicationModule` — `AddSingleton<ISettingsRegistry, SettingsRegistry>()`, `AddScoped<ISettingsService, SettingsService>()`, plus all use cases: `AddScoped<UpdateSettingUseCase>()`, `AddScoped<UpdateGoalUseCase>()`, `AddScoped<ProbeInstrumentUseCase>()`, `AddScoped<AddInstrumentUseCase>()`, `AddScoped<ListInstrumentsUseCase>()` | `SettingsInfrastructureModule` — `AddScoped<ISettingsReader, SettingsReader>()`, `AddHostedService<SettingsSeederHostedService>()` | — |
| Trades | `TradesApplicationModule` — `AddScoped<LogTradeUseCase>()`, `AddScoped<EditTradeUseCase>()`, `AddScoped<DeleteTradeUseCase>()`, `AddScoped<ImportTradesCsvUseCase>()` | — | — |
| Blazor hosting | — | — | `BlazorHostingModule` in **TradyStrat** (Razor) — `AddRazorComponents().AddInteractiveServerComponents()`; `ConfigureMiddleware`: `UseStaticFiles()`, `UseAntiforgery()`; `ConfigureEndpoints`: `MapRazorComponents<App>().AddInteractiveServerRenderMode()`. Kestrel port pin moves to `Program.cs` via the `configureBuilder` callback (web-host-specific, can't sit in a host-neutral module). |

> **`PolymarketFilter` / `PolymarketRelevance`:** these are pure-logic static helpers used by both `PolymarketGammaProvider` (Infrastructure) and `LoadDashboardUseCase` (Application). They moved to Application in Task 3.8 — Infrastructure references them via the Application project reference. No separate DI registration is needed (they are `static class`es).

> **`PolymarketNormalizer`:** moved to **Infrastructure** in Task 4.5 (adapter-internal, used only by `PolymarketGammaProvider`). No DI registration needed.

> **Kestrel pinning:** the previous `HostingModule.cs` called `builder.WebHost.ConfigureKestrel(opt => opt.ListenLocalhost(5180))`. The v3 `IAppModule.ConfigureServices(IServiceCollection, IConfiguration)` cannot reach `WebApplicationBuilder.WebHost`, so this configuration **moves to `Program.cs`** via the v3 `configureBuilder` callback (see Task 6.1).

For each row above (skip rows marked ✓ — they're done):

- [ ] **Step 1: Open the corresponding old module file in `TradyStrat/Modules/` to see exactly what was registered.**

E.g., for Dashboard: `cat TradyStrat/Modules/DashboardModule.cs`. Inspect the `services.Add...` calls and group them by target layer:
- Application-side types (use cases, application services) → Application module
- Infrastructure-side types (HTTP clients, EF stuff, hosted services, logging) → Infrastructure module

- [ ] **Step 2: Write the Application module(s) for that feature.**

Template:

```csharp
// TradyStrat.Application/<Feature>/<Feature>ApplicationModule.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
// + namespace imports for the registered types

namespace TradyStrat.Application.<Feature>;

public sealed class <Feature>ApplicationModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        // Translate every relevant services.Add... from the old module here.
    }
}
```

- [ ] **Step 3: Write the Infrastructure module(s) for that feature.**

Same pattern under `TradyStrat.Infrastructure/<Feature>/<Feature>InfrastructureModule.cs`. For HTTP clients:

```csharp
services.AddHttpClient<IFxRateProvider, YahooFxProvider>(c =>
{
    c.BaseAddress = new Uri(config["Yahoo:FxBaseUrl"] ?? "https://query1.finance.yahoo.com/");
    // copy any timeout / header config from the old module
});
```

- [ ] **Step 4: Build both projects after writing each module pair**

Run: `dotnet build TradyStrat.Application/TradyStrat.Application.csproj TradyStrat.Infrastructure/TradyStrat.Infrastructure.csproj`

- [ ] **Step 5: Commit per feature**

E.g.: `git commit -m "refactor(modules): add Dashboard application module"`.

> **Specific care for `PriceFeedInfrastructureModule`:** split into two classes per spec — `PriceFeedInfrastructureModule` (HTTP client only, always loaded) and `PriceFeedBackgroundInfrastructureModule` (registers `PriceFeedHostedService` as `IHostedService`). The CLI excludes the latter; the Blazor host includes both.

> **`DatabaseInfrastructureModule`:** owns `services.AddDbContext<AppDbContext>(...)`, the SQLite connection string resolution via `SqlitePathResolver`, the Ardalis `IRepositoryBase<T>` / `IReadRepositoryBase<T>` open-generic registrations, AND the `IClock` → `SystemClock` registration. The "database" module name is a misnomer — really "platform infrastructure" — but matches the existing convention.

### Task 5.4b: Create `BlazorHostingModule` in the Blazor project

**Files:**
- Create: `TradyStrat/BlazorHostingModule.cs`

This module is the Blazor presentation-layer's IAppModule. It lives in the **TradyStrat** project (not Application or Infrastructure) because Razor components and middleware are intrinsically Blazor concerns. The CLI does not load this module (no Razor in a CLI).

- [ ] **Step 1: Create the module**

```csharp
// TradyStrat/BlazorHostingModule.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Features.Shell;

namespace TradyStrat;

public sealed class BlazorHostingModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddRazorComponents().AddInteractiveServerComponents();
    }

    public void ConfigureMiddleware(WebApplication app)
    {
        app.UseStaticFiles();
        app.UseAntiforgery();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapRazorComponents<App>().AddInteractiveServerRenderMode();
    }
}
```

The `App` type is the existing top-level Razor component in `TradyStrat/Features/Shell/`. It's already exposed via `using TradyStrat.Features.Shell;`.

- [ ] **Step 2: Build TradyStrat**

Run: `dotnet build TradyStrat/TradyStrat.csproj`

Expected: succeeds (TheAppManager v3 signature, AddRazorComponents call OK on `IServiceCollection`).

- [ ] **Step 3: Commit**

```bash
git add TradyStrat/BlazorHostingModule.cs
git commit -m "refactor(blazor): add BlazorHostingModule (Razor components + middleware + endpoints)

Stays in TradyStrat — Blazor presentation concerns don't belong in
Application or Infrastructure. The CLI does not load this module."
```

### Task 5.5: Delete the old `TradyStrat/Modules/*.cs` files

**Files:**
- Delete: every file in `TradyStrat/Modules/`

- [ ] **Step 1: Delete the entire directory**

```bash
git rm -r TradyStrat/Modules/
```

- [ ] **Step 2: Build everything (will fail until Phase 6 rewrites Program.cs)**

Run: `dotnet build TradyStrat/TradyStrat.csproj`

Expected: build fails with "AppManager.Start" signature mismatch in `Program.cs`. That's Phase 6's job.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "refactor: delete obsolete TradyStrat/Modules/*.cs

Replaced by per-layer feature modules in Application + Infrastructure
(Tasks 5.2 - 5.4). Program.cs still uses the old AppManager.Start
signature and will be rewritten in Phase 6."
```

---

## Phase 6 — Update Blazor `Program.cs`

### Task 6.1: Rewrite `TradyStrat/Program.cs` for TheAppManager v3

**Files:**
- Modify: `TradyStrat/Program.cs`

- [ ] **Step 1: Replace `Program.cs` content**

```csharp
// TradyStrat/Program.cs
using TheAppManager.Startup;
using TradyStrat.Application;
using TradyStrat.Infrastructure;

AppManager.Start(args,
    modules => modules
        .AddFromAssemblyOf<ApplicationAssemblyMarker>()
        .AddFromAssemblyOf<InfrastructureAssemblyMarker>()
        .Add<TradyStrat.BlazorHostingModule>(),
    builder =>
    {
        // Web-host-specific configuration that v3 IAppModule cannot reach.
        builder.WebHost.ConfigureKestrel(opt => opt.ListenLocalhost(5180));
    });

namespace TradyStrat
{
    public partial class Program;
}
```

`BlazorHostingModule` is added explicitly because it lives in the TradyStrat assembly itself (not Application or Infrastructure), so the assembly-scan calls don't pick it up by accident.

The `Program` partial-class declaration is preserved so `WebApplicationFactory<Program>` in `E2E.Tests` continues to work. Make it `public partial class Program` to allow `internal`-default top-level statements to be referenced from the test project (top-level `Program` is implicitly `internal`; the partial declaration must match — both should be `public` for the test factory).

- [ ] **Step 2: Build TradyStrat**

Run: `dotnet build TradyStrat/TradyStrat.csproj`

Expected: build succeeds.

- [ ] **Step 3: Run the Blazor app to verify it starts**

Run: `dotnet run --project TradyStrat &`

Wait ~5 seconds. Visit `http://localhost:5180` (the Kestrel port pinned in `Program.cs` via `configureBuilder`) — the dashboard renders. Stop with `kill %1`.

If the app fails to start, the cause is typically a missing registration — `Required service for type X has not been registered`. Open the failing module's old equivalent in git history (`git show HEAD~N:TradyStrat/Modules/<Name>Module.cs`) and confirm every `services.Add...` line has a counterpart in the new modules.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Program.cs
git commit -m "refactor: rewrite Program.cs for TheAppManager v3 host-neutral module discovery"
```

### Task 6.2: Prune `TradyStrat.csproj` package references

**Files:**
- Modify: `TradyStrat/TradyStrat.csproj`

The Blazor project no longer needs EF, Anthropic.SDK, Ardalis.Specification.EntityFrameworkCore, etc. — those moved to Infrastructure. Remove them from the Blazor csproj.

- [ ] **Step 1: Replace the `<PackageReference>` list with the minimal Blazor-only set**

```xml
<!-- TradyStrat/TradyStrat.csproj — keep only what Blazor itself needs -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <UserSecretsId>tradystrat-local</UserSecretsId>
    <RootNamespace>TradyStrat</RootNamespace>
    <NoWarn>$(NoWarn);CA1716</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\TradyStrat.Application\TradyStrat.Application.csproj" />
    <ProjectReference Include="..\TradyStrat.Infrastructure\TradyStrat.Infrastructure.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="TheAppManager" />
  </ItemGroup>
</Project>
```

Removed packages (now transitive via Infrastructure): `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`, `Ardalis.Specification`, `Ardalis.Specification.EntityFrameworkCore`, `Atypical.TechnicalAnalysis.Common`, `Atypical.TechnicalAnalysis.Functions`, `Microsoft.Extensions.AI`, `Anthropic.SDK`, `Microsoft.Extensions.Http.Resilience`, `Serilog.AspNetCore`, `Serilog.Sinks.File`.

- [ ] **Step 2: Rebuild**

Run: `dotnet restore TradyStrat.slnx && dotnet build TradyStrat/TradyStrat.csproj`

Expected: succeeds. If any Razor file references a type no longer transitively visible (e.g. an `EF.Functions` extension), add a targeted `using` or move the logic into a use case in Application.

- [ ] **Step 3: Re-run the Blazor smoke**

Run: `dotnet run --project TradyStrat` — verify the dashboard renders. Stop.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/TradyStrat.csproj
git commit -m "build: prune TradyStrat package refs to Blazor-only deps

EF, Anthropic.SDK, Ardalis.Specification.EntityFrameworkCore, and the
indicator-analysis libraries are transitively available via the
Infrastructure project reference."
```

---

## Phase 7 — Wire `TradyStrat.Cli` with HelloCommand

### Task 7.1: Implement `HostTypeRegistrar` and `HostTypeResolver`

**Files:**
- Create: `TradyStrat.Cli/HostTypeRegistrar.cs`

**Design:** the CLI's host owns the lifetime — we let `IHost` build the provider so `IHostedService`s start correctly, `IHostLifetime` is wired, and Serilog/logging are available. Spectre's registrar interface lets us register additional services (Spectre adds its own command types via `Register*`). We add those to the host's `IServiceCollection` **before** `builder.Build()` runs, then hand Spectre a resolver wrapping `host.Services` (a true Adapter — no second `BuildServiceProvider`).

The lifecycle: (1) configure services via TheAppManager v3, (2) construct `HostTypeRegistrar(builder.Services)` and pass it to `CommandApp`, (3) Spectre calls `Register*` synchronously inside `app.Configure(...)` to register its command types into the still-mutable collection, (4) call `builder.Build()` to materialize `IHost`, (5) hand `host.Services` to the registrar via `BindHost(host)` before Spectre asks for the resolver via `Build()`.

- [ ] **Step 1: Write the registrar + resolver**

```csharp
// TradyStrat.Cli/HostTypeRegistrar.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;

namespace TradyStrat.Cli;

/// <summary>
/// Spectre.Console.Cli registrar that adds Spectre's command/settings types to the
/// host's IServiceCollection *before* the host is built, then resolves via the
/// host's built IServiceProvider. Two-phase: (1) collect registrations; (2) bind
/// to the built IHost so resolution sees the same provider that started hosted
/// services and configured logging.
/// </summary>
internal sealed class HostTypeRegistrar(IServiceCollection services) : ITypeRegistrar
{
    private IHost? _host;

    /// <summary>Called after builder.Build() to provide the resolver's backing provider.</summary>
    public void BindHost(IHost host) => _host = host;

    public ITypeResolver Build()
    {
        if (_host is null)
            throw new InvalidOperationException(
                "HostTypeRegistrar.BindHost(host) must be called after builder.Build() and before CommandApp.Run.");
        return new HostTypeResolver(_host.Services);
    }

    public void Register(Type service, Type implementation) =>
        services.AddSingleton(service, implementation);

    public void RegisterInstance(Type service, object implementation) =>
        services.AddSingleton(service, implementation);

    public void RegisterLazy(Type service, Func<object> func) =>
        services.AddSingleton(service, _ => func());

    private sealed class HostTypeResolver(IServiceProvider provider) : ITypeResolver
    {
        public object? Resolve(Type? type) => type is null ? null : provider.GetService(type);
    }
}
```

Notes:
- The resolver does **not** implement `IDisposable` — the host owns the provider, and disposing it would tear down `IHost` prematurely. `await host.RunAsync()` or `await using var host = ...` controls disposal.
- `BindHost` enforces the ordering invariant: a fresh registrar can't be `Build()`-ed before the host is materialized.

- [ ] **Step 2: Build the CLI project**

Run: `dotnet build TradyStrat.Cli/TradyStrat.Cli.csproj`

Expected: succeeds (Program.cs is still the placeholder; HostTypeRegistrar compiles on its own).

- [ ] **Step 3: Commit**

```bash
git add TradyStrat.Cli/HostTypeRegistrar.cs
git commit -m "feat(cli): add HostTypeRegistrar bridging IServiceProvider to Spectre.Console.Cli"
```

### Task 7.2: Write `HelloCommand` placeholder

**Files:**
- Create: `TradyStrat.Cli/Commands/HelloCommand.cs`

- [ ] **Step 1: Write the command**

```csharp
// TradyStrat.Cli/Commands/HelloCommand.cs
using Spectre.Console;
using Spectre.Console.Cli;

namespace TradyStrat.Cli.Commands;

internal sealed class HelloCommand : Command
{
    public override int Execute(CommandContext context)
    {
        AnsiConsole.MarkupLine("[green]TradyStrat.Cli is wired.[/]");
        return 0;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add TradyStrat.Cli/Commands/
git commit -m "feat(cli): add HelloCommand placeholder to prove Spectre wiring"
```

### Task 7.3: Replace placeholder `Program.cs` with the real composition root

**Files:**
- Modify: `TradyStrat.Cli/Program.cs`

- [ ] **Step 1: Rewrite `Program.cs`**

```csharp
// TradyStrat.Cli/Program.cs
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using TheAppManager.Modules;
using TheAppManager.Startup;
using TradyStrat.Application;
using TradyStrat.Cli;
using TradyStrat.Cli.Commands;
using TradyStrat.Infrastructure;
using TradyStrat.Infrastructure.PriceFeed; // for typeof(PriceFeedBackgroundInfrastructureModule)

var builder = Host.CreateApplicationBuilder(args);

// Compose modules into the host's service collection. The CLI excludes
// PriceFeedBackgroundInfrastructureModule (background hosted service that
// polls Yahoo for prices) — replay reads stored bars, doesn't need it.
// typeof() is used (not string name) so the compiler catches renames.
AppManager.ConfigureServices(builder.Services, builder.Configuration, modules => modules
    .AddFromAssemblyOf<ApplicationAssemblyMarker>()
    .AddFromAssemblyOf<InfrastructureAssemblyMarker>(t =>
        t != typeof(PriceFeedBackgroundInfrastructureModule)));

// Construct Spectre registrar; it adds its own command types into builder.Services.
var registrar = new HostTypeRegistrar(builder.Services);
var app = new CommandApp(registrar);
app.Configure(c =>
{
    c.AddCommand<HelloCommand>("hello").WithDescription("Verifies the CLI is wired.");
});

// Build the host AFTER Spectre has added its registrations. The built host's
// IServiceProvider is what Spectre resolves commands from.
using var host = builder.Build();
registrar.BindHost(host);

// IHostedService startup / shutdown is handled by host.RunAsync — but for a one-shot
// CLI command we explicitly start, run the command, then stop.
await host.StartAsync();
try
{
    return await app.RunAsync(args);
}
finally
{
    await host.StopAsync();
}
```

The startup/shutdown bracketing ensures any `IHostedService`s registered by the CLI's selected modules get a chance to initialize before the command runs (and dispose cleanly afterward). For the placeholder `hello` command there are no hosted services, but the pattern is right for `ReplayCommand` (successor spec) which expects EF migrations to have been applied.

- [ ] **Step 2: Build the CLI**

Run: `dotnet build TradyStrat.Cli/TradyStrat.Cli.csproj`

Expected: succeeds.

- [ ] **Step 3: Run the placeholder command**

Run: `dotnet run --project TradyStrat.Cli -- hello`

Expected stdout (Spectre may colour it):

```
TradyStrat.Cli is wired.
```

Exit code: `0`.

- [ ] **Step 4: Run without arguments — verify Spectre help renders**

Run: `dotnet run --project TradyStrat.Cli`

Expected: Spectre's auto-generated help table lists `hello` as an available command.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat.Cli/Program.cs
git commit -m "feat(cli): wire CLI composition root via TheAppManager v3 + Spectre HostTypeRegistrar

Verified: 'dotnet run --project TradyStrat.Cli -- hello' renders the
placeholder output through Spectre.Console."
```

---

## Phase 8 — Split the test project

The current `TradyStrat.Tests` project becomes four — Domain.Tests, Application.Tests, Infrastructure.Tests, E2E.Tests.

### Task 8.1: Scaffold four test projects

**Files:**
- Create: `TradyStrat.Domain.Tests/TradyStrat.Domain.Tests.csproj`
- Create: `TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj`
- Create: `TradyStrat.Infrastructure.Tests/TradyStrat.Infrastructure.Tests.csproj`
- Create: `TradyStrat.E2E.Tests/TradyStrat.E2E.Tests.csproj`

- [ ] **Step 1: Template csproj for each test project**

For each new project, write a csproj with the right `ProjectReference` graph and common test packages. Example for Application.Tests:

```xml
<!-- TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <RootNamespace>TradyStrat.Application.Tests</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\TradyStrat.Application\TradyStrat.Application.csproj" />
    <ProjectReference Include="..\TradyStrat.Domain\TradyStrat.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" />
    <PackageReference Include="Microsoft.Data.Sqlite" />
  </ItemGroup>
</Project>
```

For each test project, vary only the `<ProjectReference>` lines:
- Domain.Tests: `TradyStrat.Domain` only.
- Application.Tests: Application + Domain (above).
- Infrastructure.Tests: Infrastructure + Application + Domain. Adds `Microsoft.AspNetCore.Mvc.Testing` if any test uses real `HttpClient`.
- E2E.Tests: TradyStrat (Blazor) + Infrastructure + Application + Domain. Adds `Microsoft.AspNetCore.Mvc.Testing`.

- [ ] **Step 2: Build all four to confirm they compile empty**

Run: `dotnet build TradyStrat.Domain.Tests/TradyStrat.Domain.Tests.csproj TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj TradyStrat.Infrastructure.Tests/TradyStrat.Infrastructure.Tests.csproj TradyStrat.E2E.Tests/TradyStrat.E2E.Tests.csproj`

Expected: all four succeed.

- [ ] **Step 3: Add them to the solution**

In `TradyStrat.slnx`, insert each project entry alongside the new layer projects:

```xml
<Project Path="TradyStrat.Domain.Tests/TradyStrat.Domain.Tests.csproj" />
<Project Path="TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj" />
<Project Path="TradyStrat.Infrastructure.Tests/TradyStrat.Infrastructure.Tests.csproj" />
<Project Path="TradyStrat.E2E.Tests/TradyStrat.E2E.Tests.csproj" />
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "build: scaffold four test projects (Domain.Tests, Application.Tests, Infrastructure.Tests, E2E.Tests)"
```

### Task 8.1b: Add `TradyStrat.TestKit` shared-fixtures project

`StubAiClient` is used by both Application.Tests (use-case tests) and Infrastructure.Tests (decorator + integration tests). Linking the same source into two test assemblies produces two CLR types with the same fully-qualified name — referenced together from E2E.Tests, this yields CS0433 (ambiguous reference). The clean alternative is one shared assembly that both test projects reference.

**Files:**
- Create: `TradyStrat.TestKit/TradyStrat.TestKit.csproj`
- Create: `TradyStrat.TestKit/AiSuggestion/StubAiClient.cs`

- [ ] **Step 1: Create the TestKit csproj**

```xml
<!-- TradyStrat.TestKit/TradyStrat.TestKit.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <RootNamespace>TradyStrat.TestKit</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\TradyStrat.Application\TradyStrat.Application.csproj" />
    <ProjectReference Include="..\TradyStrat.Domain\TradyStrat.Domain.csproj" />
  </ItemGroup>
</Project>
```

It references Application + Domain (the layers `StubAiClient` needs). Not Infrastructure — fixtures must remain layer-agnostic so any test project can pull them in.

- [ ] **Step 2: Move `StubAiClient.cs` from the old test project to TestKit**

```bash
mkdir -p TradyStrat.TestKit/AiSuggestion
git mv TradyStrat.Tests/AiSuggestion/UseCases/StubAiClient.cs TradyStrat.TestKit/AiSuggestion/
sed -i '' 's|namespace TradyStrat\.Tests\.AiSuggestion\.UseCases;|namespace TradyStrat.TestKit.AiSuggestion;|g' TradyStrat.TestKit/AiSuggestion/StubAiClient.cs
sed -i '' 's|using TradyStrat\.Features\.AiSuggestion;|using TradyStrat.Application.AiSuggestion;|g' TradyStrat.TestKit/AiSuggestion/StubAiClient.cs
sed -i '' 's|using TradyStrat\.Common\.Domain;|using TradyStrat.Domain;|g' TradyStrat.TestKit/AiSuggestion/StubAiClient.cs
```

Open the file and verify the `using` block: only `TradyStrat.Domain`, `TradyStrat.Application.AiSuggestion`, BCL types. No Infrastructure imports.

- [ ] **Step 3: Add `TestKit` to the solution + ProjectReference from Application.Tests and Infrastructure.Tests**

In `TradyStrat.slnx`:

```xml
<Project Path="TradyStrat.TestKit/TradyStrat.TestKit.csproj" />
```

In `TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj`:

```xml
<ProjectReference Include="..\TradyStrat.TestKit\TradyStrat.TestKit.csproj" />
```

In `TradyStrat.Infrastructure.Tests/TradyStrat.Infrastructure.Tests.csproj`:

```xml
<ProjectReference Include="..\TradyStrat.TestKit\TradyStrat.TestKit.csproj" />
```

Test code in either project that previously did `using TradyStrat.Tests.AiSuggestion.UseCases;` now uses `using TradyStrat.TestKit.AiSuggestion;`. Bulk-fix is in Task 8.3 Step 3 / Task 8.4 Step 3.

- [ ] **Step 4: Build + commit**

```bash
dotnet build TradyStrat.TestKit/TradyStrat.TestKit.csproj
```

Expected: succeeds (StubAiClient depends only on Application + Domain).

```bash
git add TradyStrat.TestKit/ TradyStrat.slnx TradyStrat.Application.Tests/ TradyStrat.Infrastructure.Tests/
git commit -m "test: add TradyStrat.TestKit shared-fixtures project

StubAiClient is consumed by both Application.Tests and
Infrastructure.Tests. Hosting it in a separate assembly avoids the
duplicate-type-identity issue that a cross-project <Compile Link>
would create when E2E.Tests references both downstream test projects."
```

### Task 8.2: Move Domain tests

**Files:**
- Move test files into `TradyStrat.Domain.Tests/`:
  - `Common/Domain/EntityDerivedPropertiesTests.cs`
  - `Common/Domain/IndicatorKindParserTests.cs`
  - `Common/Domain/SuggestionActionDisplayTests.cs`
  - `Common/Exceptions/ExceptionHierarchyTests.cs`
  - `Common/Formatting/NumberFormatTests.cs`
  - `Common/Time/RelativeTimeFormatterTests.cs`

> `Common/Time/RelativeTimeFormatterTests.cs` belongs in Application.Tests because `RelativeTimeFormatter` moved to Application in Task 3.3. Move accordingly.

- [ ] **Step 1: Move and re-namespace**

```bash
mkdir -p TradyStrat.Domain.Tests/Domain TradyStrat.Domain.Tests/Exceptions TradyStrat.Domain.Tests/Formatting
git mv TradyStrat.Tests/Common/Domain/EntityDerivedPropertiesTests.cs TradyStrat.Domain.Tests/Domain/
git mv TradyStrat.Tests/Common/Domain/IndicatorKindParserTests.cs TradyStrat.Domain.Tests/Domain/
git mv TradyStrat.Tests/Common/Domain/SuggestionActionDisplayTests.cs TradyStrat.Domain.Tests/Domain/
git mv TradyStrat.Tests/Common/Exceptions/ExceptionHierarchyTests.cs TradyStrat.Domain.Tests/Exceptions/
git mv TradyStrat.Tests/Common/Formatting/NumberFormatTests.cs TradyStrat.Domain.Tests/Formatting/

find TradyStrat.Domain.Tests -name "*.cs" -exec sed -i '' \
  's|namespace TradyStrat\.Tests\.Common|namespace TradyStrat.Domain.Tests|g' {} +
```

`NumberFormatTests` references `TradyStrat.Application.Formatting.NumberFormat` (post-refactor). Since this test moves to Domain.Tests but the SUT is in Application, it should move to **Application.Tests** instead. Reconsider:

```bash
mkdir -p TradyStrat.Application.Tests/Formatting
git mv TradyStrat.Domain.Tests/Formatting/NumberFormatTests.cs TradyStrat.Application.Tests/Formatting/
rmdir TradyStrat.Domain.Tests/Formatting
```

The rule: a test goes wherever its **system under test** lives. `IndicatorKindParser` is in Domain → test in Domain.Tests. `NumberFormat` is in Application → test in Application.Tests.

- [ ] **Step 2: Update `using` directives in moved tests**

```bash
grep -rl "using TradyStrat\.Common" TradyStrat.Domain.Tests --include="*.cs" | \
  xargs sed -i '' 's|using TradyStrat\.Common\.Domain|using TradyStrat.Domain|g; s|using TradyStrat\.Common\.Exceptions|using TradyStrat.Domain.Exceptions|g'
```

- [ ] **Step 3: Verify**

Run: `dotnet build TradyStrat.Domain.Tests/TradyStrat.Domain.Tests.csproj && dotnet test TradyStrat.Domain.Tests/TradyStrat.Domain.Tests.csproj`

Expected: both succeed; tests pass.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "test(domain): move Domain-side tests to TradyStrat.Domain.Tests"
```

### Task 8.3: Move Application tests

**Files:** every test file whose SUT is now in Application moves to `TradyStrat.Application.Tests/`.

Full list (preserve the folder names for ease of mapping):
- `AiSuggestion/Backfill/SuggestionBackfillCoordinatorTests.cs` → **Infrastructure.Tests** (hits real DbContext)
- `AiSuggestion/CallDiff/CallDiffBuilderTests.cs` → Application.Tests
- `AiSuggestion/Citations/SuggestionServiceCitationTests.cs` → **Infrastructure.Tests** (SUT is the Anthropic adapter)
- `AiSuggestion/FakeChatClient.cs` → Application.Tests (fixture for both, lives lower)
- `AiSuggestion/Snapshot/AiSnapshotServiceTests.cs` → Application.Tests
- `AiSuggestion/SuggestionServiceTests.cs` → **Infrastructure.Tests** (SUT is the adapter)
- `AiSuggestion/UseCases/*.cs` → Application.Tests (use cases + stubs)
- `Common/Time/FakeClock.cs` → Application.Tests
- `Common/Time/RelativeTimeFormatterTests.cs` → Application.Tests
- `Common/Time/SystemClockTests.cs` → **Infrastructure.Tests** (SUT is in Infrastructure)
- `Common/UseCases/UseCaseBaseTests.cs` → Application.Tests
- `Dashboard/*Tests.cs` (all) → Application.Tests
- `Fx/DailyFxCacheTests.cs` → Application.Tests
- `Fx/FxConverterTests.cs` → Application.Tests
- `Fx/Providers/StubFxProvider.cs` → Application.Tests
- `Fx/Providers/YahooFxProviderTests.cs` → **Infrastructure.Tests**
- `Fx/TestRepo.cs` → Application.Tests
- `Indicators/**` → Application.Tests
- `Portfolio/*Tests.cs` → Application.Tests
- `PredictionMarkets/PolymarketFilterTests.cs`, `PolymarketRelevanceTests.cs` → Application.Tests (their SUTs `PolymarketFilter` / `PolymarketRelevance` are pure Application logic)
- `PredictionMarkets/PolymarketNormalizationTests.cs` → **Infrastructure.Tests** (the test's SUT is `PolymarketNormalizer` from `Providers/`, which lives in Infrastructure)
- `PredictionMarkets/Providers/PolymarketGammaProviderTests.cs` → **Infrastructure.Tests**
- `PriceFeed/DailyPriceCacheTests.cs` → Application.Tests
- `PriceFeed/PriceFeedHostedServiceTests.cs` → **Infrastructure.Tests**
- `PriceFeed/Providers/StubPriceFeed.cs` → Application.Tests
- `PriceFeed/Providers/YahooParserTests.cs` → **Infrastructure.Tests**
- `PriceFeed/Providers/YahooPriceFeedTests.cs` → **Infrastructure.Tests**
- `PriceFeed/StubHttpHandler.cs` → Infrastructure.Tests (used by HTTP-provider tests)
- `Settings/Config/SettingEntryRoundtripTests.cs` → Application.Tests
- `Settings/Config/SettingsReaderTests.cs` → **Infrastructure.Tests** (SUT is the DB-backed reader)
- `Settings/Config/SettingsRegistryTests.cs` → Application.Tests
- `Settings/Config/SettingsSeederTests.cs` → **Infrastructure.Tests**
- `Settings/Config/SettingsServiceTests.cs` → Application.Tests
- `Settings/FakeSettingsReader.cs` → Application.Tests
- `Settings/Providers/YahooMetadataParserTests.cs` → **Infrastructure.Tests** (despite its name + folder, this test exercises `YahooParser.ParseMetadata(...)` from `PriceFeed/Providers/` — the SUT is the price-feed parser. Keep the file at `Infrastructure.Tests/Settings/Providers/` to preserve fixture-path resolution, OR move to `PriceFeed/Providers/`; either works, but if moving, update the `Path.Combine("Settings", "Providers", "Fixtures", ...)` calls inside the test.)
- `Settings/Specifications/InstrumentSpecTests.cs` → Application.Tests
- `Settings/UseCases/*.cs` → Application.Tests
- `Specifications/InMemoryDb.cs` → Application.Tests
- `Specifications/SpecsRoundtripTests.cs` → **Infrastructure.Tests** (round-trips against real EF context)
- `Trades/CsvImportServiceTests.cs` → Application.Tests
- `Trades/UseCases/*.cs` → Application.Tests
- `Data/MigrationBackwardCompatTests.cs` → Infrastructure.Tests
- `Data/MultiTickerAiPhase2MigrationTests.cs` → Infrastructure.Tests
- `Data/MultiTickerMigrationTests.cs` → Infrastructure.Tests
- `Data/SqlitePathResolverTests.cs` → Infrastructure.Tests
- `Modules/ModuleSmokeTests.cs` → E2E.Tests
- `SmokeTests.cs` → E2E.Tests

- [ ] **Step 1: Move every Application.Tests-bound file**

```bash
# AiSuggestion (Application-side)
mkdir -p TradyStrat.Application.Tests/AiSuggestion/{CallDiff,Snapshot,UseCases}
git mv TradyStrat.Tests/AiSuggestion/CallDiff/CallDiffBuilderTests.cs TradyStrat.Application.Tests/AiSuggestion/CallDiff/
git mv TradyStrat.Tests/AiSuggestion/FakeChatClient.cs TradyStrat.Application.Tests/AiSuggestion/
git mv TradyStrat.Tests/AiSuggestion/Snapshot/AiSnapshotServiceTests.cs TradyStrat.Application.Tests/AiSuggestion/Snapshot/

# Move use-case tests and Application-only fixtures (StubSnapshotFactory).
# StubAiClient is shared between Application.Tests and Infrastructure.Tests —
# it's moved into a separate shared project (TradyStrat.TestKit) in Task 8.2b
# to avoid duplicate-type-identity issues when both test assemblies are
# referenced from E2E.Tests.
git mv TradyStrat.Tests/AiSuggestion/UseCases/StubSnapshotFactory.cs TradyStrat.Application.Tests/AiSuggestion/UseCases/
for f in BackfillSuggestionsUseCaseTests.cs ForceRefetchSuggestionUseCaseTests.cs \
         GetAllTodaysSuggestionsUseCaseTests.cs GetTodaysSuggestionUseCaseTests.cs; do
  [ -f "TradyStrat.Tests/AiSuggestion/UseCases/$f" ] && \
    git mv "TradyStrat.Tests/AiSuggestion/UseCases/$f" TradyStrat.Application.Tests/AiSuggestion/UseCases/
done

# Common
mkdir -p TradyStrat.Application.Tests/{Time,UseCases,Formatting}
git mv TradyStrat.Tests/Common/Time/FakeClock.cs TradyStrat.Application.Tests/Time/
git mv TradyStrat.Tests/Common/Time/RelativeTimeFormatterTests.cs TradyStrat.Application.Tests/Time/
git mv TradyStrat.Tests/Common/UseCases/UseCaseBaseTests.cs TradyStrat.Application.Tests/UseCases/

# Dashboard
mkdir -p TradyStrat.Application.Tests/Dashboard/{Navigation,UseCases}
git mv TradyStrat.Tests/Dashboard/GoalPaceCalculatorTests.cs TradyStrat.Application.Tests/Dashboard/
git mv TradyStrat.Tests/Dashboard/Navigation/*.cs TradyStrat.Application.Tests/Dashboard/Navigation/
git mv TradyStrat.Tests/Dashboard/UseCases/*.cs TradyStrat.Application.Tests/Dashboard/UseCases/

# Fx
mkdir -p TradyStrat.Application.Tests/Fx/Providers
git mv TradyStrat.Tests/Fx/DailyFxCacheTests.cs TradyStrat.Application.Tests/Fx/
git mv TradyStrat.Tests/Fx/FxConverterTests.cs TradyStrat.Application.Tests/Fx/
git mv TradyStrat.Tests/Fx/Providers/StubFxProvider.cs TradyStrat.Application.Tests/Fx/Providers/
git mv TradyStrat.Tests/Fx/TestRepo.cs TradyStrat.Application.Tests/Fx/

# Indicators (entire tree)
mkdir -p TradyStrat.Application.Tests/Indicators
git mv TradyStrat.Tests/Indicators/* TradyStrat.Application.Tests/Indicators/

# Portfolio
mkdir -p TradyStrat.Application.Tests/Portfolio
git mv TradyStrat.Tests/Portfolio/*.cs TradyStrat.Application.Tests/Portfolio/

# PredictionMarkets (Application-side)
mkdir -p TradyStrat.Application.Tests/PredictionMarkets
git mv TradyStrat.Tests/PredictionMarkets/PolymarketFilterTests.cs TradyStrat.Application.Tests/PredictionMarkets/
git mv TradyStrat.Tests/PredictionMarkets/PolymarketRelevanceTests.cs TradyStrat.Application.Tests/PredictionMarkets/
# PolymarketNormalizationTests tests the Normalizer in Providers/ (Infrastructure) — moved in Task 8.4 instead.

# PriceFeed (Application-side)
mkdir -p TradyStrat.Application.Tests/PriceFeed/Providers
git mv TradyStrat.Tests/PriceFeed/DailyPriceCacheTests.cs TradyStrat.Application.Tests/PriceFeed/
git mv TradyStrat.Tests/PriceFeed/Providers/StubPriceFeed.cs TradyStrat.Application.Tests/PriceFeed/Providers/

# Settings (Application-side)
mkdir -p TradyStrat.Application.Tests/Settings/{Config,Specifications,UseCases}
git mv TradyStrat.Tests/Settings/Config/SettingEntryRoundtripTests.cs TradyStrat.Application.Tests/Settings/Config/
git mv TradyStrat.Tests/Settings/Config/SettingsRegistryTests.cs TradyStrat.Application.Tests/Settings/Config/
git mv TradyStrat.Tests/Settings/Config/SettingsServiceTests.cs TradyStrat.Application.Tests/Settings/Config/
git mv TradyStrat.Tests/Settings/FakeSettingsReader.cs TradyStrat.Application.Tests/Settings/
git mv TradyStrat.Tests/Settings/Specifications/InstrumentSpecTests.cs TradyStrat.Application.Tests/Settings/Specifications/
git mv TradyStrat.Tests/Settings/UseCases/*.cs TradyStrat.Application.Tests/Settings/UseCases/

# Specifications
mkdir -p TradyStrat.Application.Tests/Specifications
git mv TradyStrat.Tests/Specifications/InMemoryDb.cs TradyStrat.Application.Tests/Specifications/

# Trades
mkdir -p TradyStrat.Application.Tests/Trades/UseCases
git mv TradyStrat.Tests/Trades/CsvImportServiceTests.cs TradyStrat.Application.Tests/Trades/
git mv TradyStrat.Tests/Trades/UseCases/*.cs TradyStrat.Application.Tests/Trades/UseCases/

# Formatting (the file that landed in Domain.Tests by accident from Task 8.2)
git mv TradyStrat.Tests/Common/Formatting/NumberFormatTests.cs TradyStrat.Application.Tests/Formatting/ 2>/dev/null || true
```

- [ ] **Step 2: Rename namespaces**

```bash
find TradyStrat.Application.Tests -name "*.cs" -exec sed -i '' \
  's|namespace TradyStrat\.Tests\.|namespace TradyStrat.Application.Tests.|g' {} +
```

- [ ] **Step 3: Update `using` directives**

```bash
grep -rl "using TradyStrat\." TradyStrat.Application.Tests --include="*.cs" | \
  xargs sed -i '' \
    -e 's|using TradyStrat\.Common\.Domain|using TradyStrat.Domain|g' \
    -e 's|using TradyStrat\.Common\.Exceptions|using TradyStrat.Domain.Exceptions|g' \
    -e 's|using TradyStrat\.Common\.Time|using TradyStrat.Application.Time|g' \
    -e 's|using TradyStrat\.Common\.Formatting|using TradyStrat.Application.Formatting|g' \
    -e 's|using TradyStrat\.Common\.UseCases|using TradyStrat.Application.UseCases|g' \
    -e 's|using TradyStrat\.Features\.|using TradyStrat.Application.|g' \
    -e 's|using TradyStrat\.Tests\.AiSuggestion\.UseCases|using TradyStrat.TestKit.AiSuggestion|g'
```

`SystemClock` references in test fixtures (likely just one or two) need a manual touch to `using TradyStrat.Infrastructure.Time;` — Application.Tests already references Infrastructure transitively but you need the `using`. Grep:

```bash
grep -rn "SystemClock" TradyStrat.Application.Tests --include="*.cs"
```

For each hit, ensure the appropriate `using` is present.

> Application.Tests should NOT directly reference Infrastructure. If a test needs `SystemClock`, it's testing Infrastructure and should move there. If a test legitimately needs a clock, use `FakeClock` (also moved to Application.Tests).

- [ ] **Step 4: Build + run**

Run: `dotnet build TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj && dotnet test TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj`

Expected: build succeeds; all tests pass.

> Compile errors at this stage are typically missing `using` directives or fixtures. Read the error, add the `using`, re-run. If a test fundamentally needs Infrastructure types, **move it to Infrastructure.Tests**.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "test(application): move Application-side tests to TradyStrat.Application.Tests"
```

### Task 8.4: Move Infrastructure tests

**Files:** every test file whose SUT lives in Infrastructure (see list in Task 8.3) moves to `TradyStrat.Infrastructure.Tests/`.

- [ ] **Step 1: Move the Infrastructure.Tests-bound files**

```bash
mkdir -p TradyStrat.Infrastructure.Tests/{AiSuggestion,Data,Fx/Providers,PredictionMarkets/Providers,PriceFeed/Providers,Settings/{Config,Providers},Specifications,Time}

git mv TradyStrat.Tests/AiSuggestion/Backfill/SuggestionBackfillCoordinatorTests.cs TradyStrat.Infrastructure.Tests/AiSuggestion/
git mv TradyStrat.Tests/AiSuggestion/Citations/SuggestionServiceCitationTests.cs TradyStrat.Infrastructure.Tests/AiSuggestion/
git mv TradyStrat.Tests/AiSuggestion/SuggestionServiceTests.cs TradyStrat.Infrastructure.Tests/AiSuggestion/

# StubAiClient is in TradyStrat.TestKit (Task 8.1b). Infrastructure.Tests
# references TestKit via <ProjectReference>; tests that previously used
# `using TradyStrat.Tests.AiSuggestion.UseCases;` now use
# `using TradyStrat.TestKit.AiSuggestion;` — caught by the Task 8.4 Step 3
# sed pass that retargets all old TradyStrat.Tests.* test-fixture namespaces.

git mv TradyStrat.Tests/Common/Time/SystemClockTests.cs TradyStrat.Infrastructure.Tests/Time/

git mv TradyStrat.Tests/Data/*.cs TradyStrat.Infrastructure.Tests/Data/

git mv TradyStrat.Tests/Fx/Providers/YahooFxProviderTests.cs TradyStrat.Infrastructure.Tests/Fx/Providers/

git mv TradyStrat.Tests/PredictionMarkets/PolymarketNormalizationTests.cs TradyStrat.Infrastructure.Tests/PredictionMarkets/Providers/
git mv TradyStrat.Tests/PredictionMarkets/Providers/PolymarketGammaProviderTests.cs TradyStrat.Infrastructure.Tests/PredictionMarkets/Providers/

git mv TradyStrat.Tests/PriceFeed/PriceFeedHostedServiceTests.cs TradyStrat.Infrastructure.Tests/PriceFeed/
git mv TradyStrat.Tests/PriceFeed/Providers/YahooParserTests.cs TradyStrat.Infrastructure.Tests/PriceFeed/Providers/
git mv TradyStrat.Tests/PriceFeed/Providers/YahooPriceFeedTests.cs TradyStrat.Infrastructure.Tests/PriceFeed/Providers/
git mv TradyStrat.Tests/PriceFeed/StubHttpHandler.cs TradyStrat.Infrastructure.Tests/PriceFeed/

git mv TradyStrat.Tests/Settings/Config/SettingsReaderTests.cs TradyStrat.Infrastructure.Tests/Settings/Config/
git mv TradyStrat.Tests/Settings/Config/SettingsSeederTests.cs TradyStrat.Infrastructure.Tests/Settings/Config/
git mv TradyStrat.Tests/Settings/Providers/YahooMetadataParserTests.cs TradyStrat.Infrastructure.Tests/Settings/Providers/

git mv TradyStrat.Tests/Specifications/SpecsRoundtripTests.cs TradyStrat.Infrastructure.Tests/Specifications/
```

- [ ] **Step 2: Rename namespaces**

```bash
find TradyStrat.Infrastructure.Tests -name "*.cs" -exec sed -i '' \
  's|namespace TradyStrat\.Tests\.|namespace TradyStrat.Infrastructure.Tests.|g' {} +
```

- [ ] **Step 3: Update `using` directives**

```bash
grep -rl "using TradyStrat\." TradyStrat.Infrastructure.Tests --include="*.cs" | \
  xargs sed -i '' \
    -e 's|using TradyStrat\.Common\.Domain|using TradyStrat.Domain|g' \
    -e 's|using TradyStrat\.Common\.Exceptions|using TradyStrat.Domain.Exceptions|g' \
    -e 's|using TradyStrat\.Common\.Time|using TradyStrat.Infrastructure.Time|g' \
    -e 's|using TradyStrat\.Common\.UseCases|using TradyStrat.Application.UseCases|g' \
    -e 's|using TradyStrat\.Data|using TradyStrat.Infrastructure.Data|g' \
    -e 's|using TradyStrat\.Tests\.AiSuggestion\.UseCases|using TradyStrat.TestKit.AiSuggestion|g'
```

For `using TradyStrat.Features.<feature>;`, expand carefully because the resulting namespace splits across Application and Infrastructure depending on what the test references. Run:

```bash
grep -rn "using TradyStrat\.Features\." TradyStrat.Infrastructure.Tests --include="*.cs"
```

For each hit, decide:
- Test references a port or pure-logic type → `using TradyStrat.Application.<feature>;`
- Test references an adapter or DB-backed type → `using TradyStrat.Infrastructure.<feature>;`

This is the slow part — but the test compile errors guide you.

- [ ] **Step 4: Build + test**

Run: `dotnet build TradyStrat.Infrastructure.Tests/TradyStrat.Infrastructure.Tests.csproj && dotnet test TradyStrat.Infrastructure.Tests/TradyStrat.Infrastructure.Tests.csproj`

Expected: build succeeds; all tests pass.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "test(infrastructure): move Infrastructure-side tests to TradyStrat.Infrastructure.Tests"
```

### Task 8.5: Move E2E tests

- [ ] **Step 1: Move ModuleSmokeTests + SmokeTests**

```bash
mkdir -p TradyStrat.E2E.Tests/Modules
git mv TradyStrat.Tests/Modules/ModuleSmokeTests.cs TradyStrat.E2E.Tests/Modules/
git mv TradyStrat.Tests/SmokeTests.cs TradyStrat.E2E.Tests/
```

- [ ] **Step 2: Rename namespaces + fix `using`s**

```bash
find TradyStrat.E2E.Tests -name "*.cs" -exec sed -i '' \
  's|namespace TradyStrat\.Tests\.|namespace TradyStrat.E2E.Tests.|g; s|namespace TradyStrat\.Tests;|namespace TradyStrat.E2E.Tests;|g' {} +

grep -rl "using TradyStrat\.Common\.\|using TradyStrat\.Features\." TradyStrat.E2E.Tests --include="*.cs" | \
  xargs sed -i '' \
    -e 's|using TradyStrat\.Common\.Domain|using TradyStrat.Domain|g' \
    -e 's|using TradyStrat\.Common\.Exceptions|using TradyStrat.Domain.Exceptions|g' \
    -e 's|using TradyStrat\.Features\.|using TradyStrat.Application.|g'
```

`ModuleSmokeTests` builds a `WebApplicationFactory<Program>` and asserts every key service resolves. The list of services it probes must now reference the new namespaces — open the file, walk each `sp.GetRequiredService<T>()` call, update each `T` to its new namespace.

- [ ] **Step 3: Build + test**

Run: `dotnet test TradyStrat.E2E.Tests/TradyStrat.E2E.Tests.csproj`

Expected: tests pass.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "test(e2e): move module smoke + Blazor smoke to TradyStrat.E2E.Tests"
```

### Task 8.6: Retire the old `TradyStrat.Tests` project

**Files:**
- Delete: `TradyStrat.Tests/` (entire directory, after verifying empty of `.cs` files)

- [ ] **Step 1: Confirm the old project has no remaining test files**

```bash
find TradyStrat.Tests -name "*.cs" ! -path "*/obj/*" ! -path "*/bin/*"
```

Expected output: empty. If any files remain, they were skipped — move them to the right new project before continuing.

- [ ] **Step 2: Remove `TradyStrat.Tests` from `TradyStrat.slnx` and delete the directory**

Edit `TradyStrat.slnx` and remove the `<Project Path="TradyStrat.Tests/TradyStrat.Tests.csproj" />` line.

```bash
git rm -r TradyStrat.Tests/
```

- [ ] **Step 3: Build the whole solution**

Run: `dotnet build TradyStrat.slnx`

Expected: succeeds.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "test: retire TradyStrat.Tests project (split across four layer-specific test projects)"
```

---

## Phase 9 — Final verification

### Task 9.1: Full solution build + test sweep

- [ ] **Step 1: Clean build**

Run: `dotnet clean TradyStrat.slnx && dotnet build TradyStrat.slnx`

Expected: build succeeds with no warnings beyond pre-existing ones (check `Directory.Build.props` for any analyzer suppressions that may need to migrate to the new projects).

- [ ] **Step 2: Full test sweep**

Run: `dotnet test TradyStrat.slnx`

Expected: every test in every project passes. Count matches (or exceeds, if any tests were duplicated during the move) the pre-refactor count. Pre-refactor count can be obtained from the merge-base commit (typically the count is logged in CI).

> If any test fails, the cause is almost certainly a missing service registration in a new feature module. Read the test name; identify which service it expects; trace back to the corresponding old `Modules/<X>Module.cs` in git history; ensure that registration is present in the new module.

### Task 9.2: Blazor runtime smoke

- [ ] **Step 1: Run the Blazor app and verify the dashboard renders**

The Kestrel pin from `HostingModule.cs` moves to `Program.cs` (Task 6.1) but keeps the same port: `5180`. If the smoke fails on this port, check that `Program.cs`'s `builder.WebHost.ConfigureKestrel(opt => opt.ListenLocalhost(5180))` is present.

Run:
```bash
dotnet run --project TradyStrat &
sleep 8
curl -s http://localhost:5180 | head -40
kill %1
```

Expected: HTML output containing dashboard markup. No exceptions in stderr.

- [ ] **Step 2: Confirm sentinel hash stability**

The `AiSnapshotService` sentinel hash `895EED53A280A470` (from Phase 2 multi-ticker) MUST still pass. If `AiSnapshotServiceTests` has a test that asserts this hash, ensure it ran in step 9.1.

If you renamed any JSON property names during the move (you shouldn't have — namespaces only), the hash will drift. Revert the rename and re-run.

### Task 9.3: CLI runtime smoke

- [ ] **Step 1: Run the placeholder CLI command**

Run: `dotnet run --project TradyStrat.Cli -- hello`

Expected stdout: `TradyStrat.Cli is wired.` (with Spectre colour markup applied).
Exit code: `0`.

### Task 9.4: EF migration command smoke

- [ ] **Step 1: Verify EF can find migrations in the new project**

Run: `dotnet ef migrations list --project TradyStrat.Infrastructure --startup-project TradyStrat`

Expected: lists all existing migrations including the most recent (`MultiTickerAiPhase2` or later).

- [ ] **Step 2: Run a dry-run migration to confirm tooling works**

```bash
dotnet ef migrations script 0 --project TradyStrat.Infrastructure --startup-project TradyStrat --output /tmp/migration-dryrun.sql
head -40 /tmp/migration-dryrun.sql
```

Expected: SQL script outputs successfully — no exception about missing assembly or missing DbContext.

### Task 9.5: Cleanup + final commit

- [ ] **Step 1: Confirm no orphan files remain in `TradyStrat/`**

```bash
find TradyStrat -name "*.cs" ! -name "*.razor.cs" ! -path "*/obj/*" ! -path "*/bin/*"
```

Expected: only `Program.cs`. Any other `.cs` file is misplaced.

- [ ] **Step 2: Delete any empty directories**

```bash
find TradyStrat -type d -empty -delete
find TradyStrat.Domain TradyStrat.Application TradyStrat.Infrastructure TradyStrat.Cli -type d -empty -delete
find TradyStrat.Domain.Tests TradyStrat.Application.Tests TradyStrat.Infrastructure.Tests TradyStrat.E2E.Tests -type d -empty -delete
```

- [ ] **Step 3: Final commit**

```bash
git add -A
git commit --allow-empty -m "chore: hexagonal refactor complete

- 4-project hexagonal layout: Domain / Application / Infrastructure
  + Blazor driving adapter + new TradyStrat.Cli driving adapter
- TheAppManager v3.0.0 host-neutral module signature
- 13 monolithic modules replaced by feature-per-layer modules
- TradyStrat.Tests split into Domain.Tests / Application.Tests
  / Infrastructure.Tests / E2E.Tests
- No behaviour change; full test sweep passes; AiSnapshot sentinel
  hash stable; Blazor + CLI runtime smokes green"
```

### Task 9.6: Open the PR

- [ ] **Step 1: Push the worktree branch**

```bash
git push -u origin HEAD
```

- [ ] **Step 2: Open the PR**

```bash
gh pr create --title "refactor: hexagonal architecture (Domain/Application/Infrastructure + CLI)" --body "$(cat <<'EOF'
## Summary
- Restructures TradyStrat from a single Blazor project into a 4-project hexagonal layout
- TheAppManager bumped to v3.0.0 (host-neutral module signature)
- Adds TradyStrat.Cli skeleton with a placeholder Spectre command
- Splits TradyStrat.Tests into four layer-specific test projects
- Zero behaviour changes; same DB schema, same prompt, same UI

Spec: `docs/superpowers/specs/2026-05-13-hexagonal-refactor-design.md`
Plan: `docs/superpowers/plans/2026-05-13-hexagonal-refactor.md`

## Test plan
- [ ] `dotnet build TradyStrat.slnx` — succeeds, no new warnings
- [ ] `dotnet test TradyStrat.slnx` — full sweep passes
- [ ] `dotnet run --project TradyStrat` — Blazor dashboard renders identically
- [ ] `dotnet run --project TradyStrat.Cli -- hello` — Spectre output renders
- [ ] `dotnet ef migrations list --project TradyStrat.Infrastructure --startup-project TradyStrat` — lists migrations
- [ ] `AiSnapshotService` sentinel hash `895EED53A280A470` still passes

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

- [ ] **Step 3: Return the PR URL to the user**

---

## Self-review checklist (run before handing the plan to an executing agent)

Run this checklist against the spec (`docs/superpowers/specs/2026-05-13-hexagonal-refactor-design.md`):

- [ ] **§3.1 Domain** — all listed types have a destination task (Phase 2 covers Common/Domain, Common/Time/IClock, Common/Exceptions).
- [ ] **§3.2 Application** — every Application service + port + use case has a destination task (Phase 3, broken out per feature).
- [ ] **§3.3 Infrastructure** — every adapter + EF + vendor exception has a destination (Phase 4).
- [ ] **§3.4 Blazor** — `Program.cs` rewritten (Task 6.1); old `Modules/` deleted (Task 5.5); package refs pruned (Task 6.2).
- [ ] **§3.5 Cli** — `Program.cs` real implementation (Task 7.3); `HostTypeRegistrar` (Task 7.1); `HelloCommand` (Task 7.2).
- [ ] **§4 TheAppManager v3** — covered in Phase 0 with three sub-tasks.
- [ ] **§5 Per-feature modules** — Phase 5 covers all 13 features. The Phase 5 table in Task 5.4 is the canonical mapping.
- [ ] **§6 Test split** — Phase 8 covers all four new test projects with explicit per-file mapping (Task 8.3).
- [ ] **§7 Migration plan** — followed exactly; one PR (Task 9.6).
- [ ] **§8 Verification** — Phase 9 mechanises every bullet from §8 (build, test, runtime smoke, sentinel hash, EF tooling).
- [ ] **§10 Risks** — all five risk rows have mitigations baked into specific tasks (port discovery → Task 3.13 + 4.6 checkpoints; TheAppManager → Phase 0; Razor refs → Task 3.1 step 3; EF → Task 4.1 step 6; test split → Task 8.6).

Issues found during self-review (none expected — fix inline if any surface).
