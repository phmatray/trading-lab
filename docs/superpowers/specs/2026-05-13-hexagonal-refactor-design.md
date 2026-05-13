# TradyStrat — Hexagonal architecture refactor

**Status:** Design approved · Ready for implementation plan
**Date:** 2026-05-13
**Author:** Philippe Matray (with Claude)
**Successor:** [`2026-05-13-ai-suggestion-improvements-design.md`](./2026-05-13-ai-suggestion-improvements-design.md) — ships on top of this refactor.

---

## 1. Purpose & goal

Today every feature module ties DI to the web host (`IAppModule.ConfigureServices(WebApplicationBuilder)`), and every production type — domain entities, use cases, EF context, Anthropic adapter, Polymarket HTTP client, Razor pages — lives in one `TradyStrat` project. That works for a Blazor-only app but blocks anything else: a CLI replay harness, future REST host, or unit-testing the Application layer without dragging EF and HTTP adapters in.

This refactor establishes a **hexagonal (ports & adapters) layout**: a Domain core with no dependencies, an Application layer that owns the ports it needs, an Infrastructure layer that implements those ports, and two driving adapters (Blazor + CLI) sitting on the outside. It also evolves `TheAppManager` to a host-neutral module signature so both driving adapters share the same registration pipeline.

The refactor ships **before** the AI suggestion improvements work — that successor spec ships against the new project layout.

## 2. Decisions at a glance

| Concern | Decision |
|---|---|
| Architecture style | **Hexagonal (Cockburn — ports & adapters).** Explicitly not Clean Architecture. |
| Project granularity | **4 projects inside + driving adapters.** Domain / Application / Infrastructure separated; Domain and Application split for size discipline, ports & adapters stay at the boundary. |
| Driving adapters | **`TradyStrat` (Blazor)** and **`TradyStrat.Cli`** (new, Spectre-based; built skeleton-only in this refactor, populated in the successor spec). |
| Module system | **TheAppManager v3.0.0 — hard break.** New signature `IAppModule.ConfigureServices(IServiceCollection, IConfiguration)`. Old `WebApplicationBuilder` overload removed. |
| Module granularity in TradyStrat | **Feature-per-layer.** Each feature contributes one Application module + one Infrastructure module. |
| Test projects | **Per-layer split: `Domain.Tests` / `Application.Tests` / `Infrastructure.Tests` / `E2E.Tests`.** Current single `TradyStrat.Tests` project is dissolved. |
| Migration shape | **One PR.** The whole refactor lands atomically; partial states have no value. |
| Behavioural change | **None.** No feature changes, no DB schema changes, no prompt changes. All existing tests pass in their new home. |
| EF migrations | **Stay byte-identical**, moved with `AppDbContext` into Infrastructure. |
| Razor namespacing | **Pages and components keep `TradyStrat.Features.*` namespaces** but only the presentation-layer files stay in the Blazor project; their underlying use cases, entities, and adapters move out. |

## 3. Target project layout

```
TradyStrat.Domain                ← pure types, no deps
TradyStrat.Application            ← use cases + ports
                                    ↳ depends on Domain
TradyStrat.Infrastructure         ← driven adapters
                                    ↳ depends on Application
TradyStrat                        ← Blazor driving adapter
                                    ↳ depends on Application + Infrastructure
TradyStrat.Cli                    ← Spectre driving adapter (skeleton)
                                    ↳ depends on Application + Infrastructure

TradyStrat.Domain.Tests           ← depends on Domain
TradyStrat.Application.Tests      ← depends on Application + Domain
TradyStrat.Infrastructure.Tests   ← depends on Infrastructure + Application + Domain
TradyStrat.E2E.Tests              ← depends on TradyStrat (Blazor) + all of the above
```

Strict reference rules — enforced by csproj `<ProjectReference>` graph:

- Domain references **nothing** in this solution.
- Application references **Domain** only.
- Infrastructure references **Application** (and transitively Domain).
- Driving adapters (Blazor + CLI) reference **Application + Infrastructure**.
- No driven adapter project references another driven adapter project.

### 3.1 Domain (TradyStrat.Domain)

Pure types — entities, value objects, enums. Everything in `TradyStrat/Common/Domain/` today moves here unchanged except namespace:

- `Suggestion`, `SuggestionAction`, `SuggestionActionDisplay`, `Citation`, `MarketCitation`, `Zone`, `GoalConfig`, `Trade`, `TradeSide`, `Instrument`, `InstrumentKind`, `PortfolioSnapshot`, `PredictionMarket`.
- The pure-domain exceptions in `TradyStrat/Common/Exceptions/`: `FxRateUnavailableException`, `PolymarketUnavailableException`, etc. — they're part of the domain vocabulary.
- `AnthropicCallFailedException` is **renamed to `AiCallFailedException`** and moves to **Application** (it's the abstract failure mode of the AI port). Vendor-named failures stay in Infrastructure; the Application port surfaces only the abstract type.

NuGet dependencies in Domain: **none beyond the BCL** (which includes `System.Text.Json` — Domain may use it for derived properties like `Suggestion.Citations` that deserializes `CitationsJson`).

### 3.2 Application (TradyStrat.Application)

Use cases + the ports they consume. Application services are NOT pure compute — they orchestrate the domain by calling ports. The hexagonal rule is that Application **owns the port interfaces** and never imports an adapter package directly.

**Use cases** (`UseCaseBase<TInput, TOutput>` subclasses) — all `UseCases/` folders move here:

- `GetTodaysSuggestionUseCase`, `GetAllTodaysSuggestionsUseCase`, `ForceRefetchSuggestionUseCase`, `BackfillSuggestionsUseCase`, `LoadDashboardUseCase`, `ListInstrumentsUseCase`, `UpdateSettingUseCase`, `ProbeInstrumentUseCase`, etc.
- `UseCaseBase<TInput, TOutput>` and `Unit` (the input sentinel).

**Ports owned by Application** (interfaces — the *contracts* the application demands from the outside):

- `IAiClient` — Application's abstract AI port. Wraps the concrete `IChatClient` (which itself stays an M.E.AI.Abstractions detail).
- `IAiSnapshotService` — snapshot construction port.
- `Ardalis.Specification.IReadRepositoryBase<T>` and `IRepositoryBase<T>` — generic data-access ports (the interfaces; EF-backed impls live in Infrastructure). All concrete `Spec` classes in `Specifications/` folders move here too — Specifications are domain queries.
- `IPredictionMarketProvider` — Polymarket fetch port.
- `IFxRateProvider` — live FX-rate fetch port (HTTP).
- `IPriceFeed` — live price-bar fetch port (HTTP).
- `ISettingsReader` — settings-read port.
- `IClock` — time port.

**Application services** (concrete classes that consume the ports above and produce domain results):

- `IndicatorEngine` — consumes `IReadRepositoryBase<PriceBar>` + `ZoneClassifier` + `IIndicatorHistoryProviderFactory`. Pure once its data dependencies are injected. Stays in Application.
- `FxConverter` — consumes `IReadRepositoryBase<FxRate>`. EUR↔ccy conversion against stored rates. Stays in Application.
- `PortfolioService` — consumes `IReadRepositoryBase<Trade>`. Stays in Application.
- `AiSnapshotService` — consumes `IndicatorEngine`, `FxConverter`, `PortfolioService`, several repos, `IPredictionMarketProvider`, `IClock`. Stays in Application.
- `DailyFxCache`, `DailyPriceCache` — consume their respective live providers and write into the repos. These are application-level *coordinators* (cache-and-persist policy), not adapters. Stay in Application.
- `SuggestionGate`, `ZoneClassifier`, `IIndicatorHistoryProviderFactory` — pure helpers, stay in Application.
- `JsonOpts` — serialization policy used at the use-case boundary.

NuGet dependencies in Application: `Ardalis.Specification` (interfaces only — no `.EntityFrameworkCore`), `Microsoft.Extensions.AI.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`, `Atypical.TechnicalAnalysis.*` (used by `IndicatorEngine` — pure compute library, no I/O). **No** `Microsoft.Extensions.AI` (concrete), **no** `Ardalis.Specification.EntityFrameworkCore`, **no** `Microsoft.EntityFrameworkCore.*`, **no** `Anthropic.SDK`, **no** `HttpClient` factory.

### 3.3 Infrastructure (TradyStrat.Infrastructure)

Driven adapters — everything that talks to the outside world. Each adapter implements a port owned by Application.

- `AppDbContext` and all EF configurations + migrations.
- `Ardalis.Specification.EntityFrameworkCore` repository implementations — registered as `IReadRepositoryBase<T>` / `IRepositoryBase<T>` for every aggregate.
- `SuggestionService` — Anthropic adapter implementing `IAiClient`. (Will be refactored to a thin orchestrator with stacked `IChatClient` decorators in the successor spec.)
- `PolymarketGammaProvider` — HTTP adapter implementing `IPredictionMarketProvider`.
- `YahooFxProvider` — HTTP adapter implementing `IFxRateProvider`.
- `YahooPriceFeed` — HTTP adapter implementing `IPriceFeed`.
- `PriceFeedHostedService` — background fetch loop; lives in Infrastructure because it owns the polling schedule and EF write path.
- `SettingsReader` — DB-backed adapter implementing `ISettingsReader`.
- `SystemClock` — adapter implementing `IClock`.
- Serilog wiring (today in `LoggingModule.cs`).
- HTTP resilience wiring (`Microsoft.Extensions.Http.Resilience`).
- The vendor-specific `AnthropicCallFailedException` — thrown by `SuggestionService`, caught and rewrapped as the Application-level `AiCallFailedException` at the port boundary.

NuGet dependencies in Infrastructure: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`, `Ardalis.Specification.EntityFrameworkCore`, `Microsoft.Extensions.AI` (concrete), `Anthropic.SDK`, `Microsoft.Extensions.Http.Resilience`, `Serilog.*`. **No** `Atypical.TechnicalAnalysis.*` — that's a pure-compute library used by the Application-side `IndicatorEngine`.

### 3.4 TradyStrat (Blazor)

Razor pages, components, layout, `Program.cs`. Loses everything else.

- `Features/<Feature>/*.razor` and `*.razor.cs` files stay.
- `Features/Settings/Components/*.razor` files stay.
- `Modules/*.cs` files are deleted (replaced by feature modules in Application + Infrastructure — see §5).
- `Program.cs` shrinks to a small composition root using TheAppManager v3 (see §4):

  ```csharp
  using TheAppManager.Startup;
  using TradyStrat.Application; // assembly marker
  using TradyStrat.Infrastructure; // assembly marker

  AppManager.Start(args, modules => modules
      .AddFromAssemblyOf<ApplicationAssemblyMarker>()
      .AddFromAssemblyOf<InfrastructureAssemblyMarker>());
  ```

  The `Application` and `Infrastructure` projects each ship a `public sealed class <Layer>AssemblyMarker;` empty type so module-discovery scans the correct assemblies without requiring callers to name a specific module class.

### 3.5 TradyStrat.Cli

New project. Initial content in this refactor: skeleton only.

```csharp
// TradyStrat.Cli/Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using TheAppManager.Startup;
using TradyStrat.Application;
using TradyStrat.Infrastructure;
using TradyStrat.Cli;
using TradyStrat.Cli.Commands;

var builder = Host.CreateApplicationBuilder(args);

// New v3 host-neutral entry point — see §4.2
AppManager.ConfigureServices(builder.Services, builder.Configuration, modules => modules
    .AddFromAssemblyOf<ApplicationAssemblyMarker>()
    .AddFromAssemblyOf<InfrastructureAssemblyMarker>());

using var host = builder.Build();

var app = new CommandApp(new HostTypeRegistrar(host.Services));
app.Configure(c => c.AddCommand<HelloCommand>("hello"));
return await app.RunAsync(args);
```

- `HostTypeRegistrar` is a small `Spectre.Console.Cli.ITypeRegistrar` adapter (12-line class) that wraps `host.Services` and forwards `Resolve` calls. It exists for both the skeleton and the successor's `ReplayCommand`.
- Commands folder exists but contains only `HelloCommand` to prove wiring; removed in the successor spec when `ReplayCommand` lands.

NuGet dependencies in Cli: `Spectre.Console`, `Spectre.Console.Cli`, `Microsoft.Extensions.Hosting`.

## 4. TheAppManager v3.0.0

Hard-break release. The library code lives in a separate repo (user-owned). Both API changes below build on the existing module-discovery surface (`AppModuleCollection`, `AddFromAssemblyOf<T>()`, `AddFromAssembly(Assembly)`) — those are unchanged.

### 4.1 New module signature

```csharp
public interface IAppModule
{
    void ConfigureServices(IServiceCollection services, IConfiguration config);
    // Default no-op middleware/endpoint hooks remain available for web-only modules.
    void ConfigureMiddleware(WebApplication app) { }
    void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
}
```

`ConfigureServices` no longer takes `WebApplicationBuilder` — only `IServiceCollection` + `IConfiguration`, which both the web host and a generic host can provide. `ConfigureMiddleware` and `ConfigureEndpoints` still take web-host types and are simply unused by non-web hosts. No `[Obsolete]` shim.

### 4.2 New entry-point shape

```csharp
public static class AppManager
{
    // Web overload — unchanged shape, signature delegates to ConfigureServices below.
    public static void Start(
        string[] args,
        Action<AppModuleCollection> configureModules,
        Action<WebApplicationBuilder>? configureBuilder = null);

    public static Task StartAsync(
        string[] args,
        Action<AppModuleCollection> configureModules,
        Action<WebApplicationBuilder>? configureBuilder = null);

    // NEW: host-neutral composition root, callable from any non-web host (CLI, worker).
    // Does NOT build or run anything — just composes services into the supplied collection.
    public static void ConfigureServices(
        IServiceCollection services,
        IConfiguration config,
        Action<AppModuleCollection> configureModules);
}
```

The web `Start` overload internally constructs a `WebApplicationBuilder`, then calls `ConfigureServices(builder.Services, builder.Configuration, configureModules)`, then invokes any registered `ConfigureMiddleware` / `ConfigureEndpoints` hooks on the built `WebApplication`, then `app.Run()`. The CLI uses `ConfigureServices` directly against a `HostApplicationBuilder` (see §3.5).

Both call sites share one module-discovery pipeline. The `AppModuleCollection` API (`Add`, `AddIf`, `AddFromAssemblyOf<T>`, `AddFromAssembly`, `Replace`) is unchanged from v2.

### 4.3 Consumer migration

The only consumer in this repo is TradyStrat. TheAppManager v3.0.0 is published from its own repo first, then TradyStrat upgrades inside this refactor's PR. The user owns all other consumers and bumps them in the same window.

## 5. Module organisation per feature

Each feature in TradyStrat splits its registration across two modules — one in Application, one in Infrastructure. The current `TradyStrat/Modules/*.cs` files all go away.

| Current module | → Becomes |
|---|---|
| `AiSuggestionModule` | `AiSuggestionApplicationModule` (in Application — registers `GetTodaysSuggestionUseCase`, `GetAllTodaysSuggestionsUseCase`, `ForceRefetchSuggestionUseCase`, `BackfillSuggestionsUseCase`, `AiSnapshotService`) + `AiSuggestionInfrastructureModule` (in Infrastructure — registers `IChatClient` (Anthropic), `SuggestionService` as `IAiClient`, `SuggestionBackfillCoordinator`) |
| `DashboardModule` | `DashboardApplicationModule` (LoadDashboardUseCase, etc.) — no infrastructure module needed |
| `DatabaseModule` | `DatabaseInfrastructureModule` (`AppDbContext`, repositories, `IClock` impl) |
| `FxModule` | `FxApplicationModule` (`FxConverter`) + `FxInfrastructureModule` (HTTP client + cache) |
| `HostingModule` | `HostingInfrastructureModule` (Serilog, resilience pipeline) — only registered when the driving adapter wants it (Blazor yes, CLI minimal) |
| `IndicatorsModule` | `IndicatorsApplicationModule` (`IndicatorEngine` + helpers) |
| `LoggingModule` | folded into `HostingInfrastructureModule` |
| `PortfolioModule` | `PortfolioApplicationModule` (`PortfolioService`) |
| `PredictionMarketsModule` | `PredictionMarketsApplicationModule` (port reg) + `PredictionMarketsInfrastructureModule` (`PolymarketGammaProvider` HTTP client) |
| `PriceFeedModule` | `PriceFeedInfrastructureModule` (currently injects fixture data — moves here) |
| `PricesUseCasesModule` | `PricesApplicationModule` |
| `SettingsModule` | `SettingsApplicationModule` (use cases) + `SettingsInfrastructureModule` (`SettingsReader` impl, seeder) |
| `TradesModule` | `TradesApplicationModule` (use cases) — Trades has no infrastructure of its own beyond the shared DbContext |

Each module is one file with one `ConfigureServices(IServiceCollection, IConfiguration)` method. Both driving adapters discover modules from both assemblies via `modules.AddFromAssemblyOf<ApplicationAssemblyMarker>().AddFromAssemblyOf<InfrastructureAssemblyMarker>()` inside the `configureModules` callback.

> **PriceFeedModule split:** the current single module both registers the live HTTP feed (`IPriceFeed → YahooPriceFeed`) and starts the `PriceFeedHostedService` background loop. After split: `PriceFeedInfrastructureModule` registers both. The CLI does not need the background loop running (replay reads stored bars), so `PriceFeedInfrastructureModule` is decomposed into two — `PriceFeedAdapterInfrastructureModule` (HTTP client registration, always loaded) and `PriceFeedBackgroundInfrastructureModule` (hosted-service registration, loaded only by the Blazor host). The CLI's discovery callback opts out of the background module via `.Remove<PriceFeedBackgroundInfrastructureModule>()` or by selective assembly loading.

## 6. Test project split

The current `TradyStrat.Tests` project is dissolved. Each existing test file moves to one of four new projects, by what it tests:

| New project | Contains | References |
|---|---|---|
| `TradyStrat.Domain.Tests` | `EntityDerivedPropertiesTests`, `SuggestionActionDisplayTests`, `ExceptionHierarchyTests`, value-object tests. | Domain only. |
| `TradyStrat.Application.Tests` | All `UseCases/*Tests`, `Specifications/*Tests`, `AiSnapshot/*Tests`, `Settings/UseCases/*Tests`, `Common/Domain` use-case tests, `CallDiff/*Tests`, `Citations/*Tests`. Uses `StubAiClient`, `FakeChatClient`, `FakeSettingsReader`, in-memory EF where the test is really about use-case orchestration. | Application + Domain. |
| `TradyStrat.Infrastructure.Tests` | `MultiTickerAiPhase2MigrationTests`, `MigrationBackwardCompatTests`, `PolymarketGammaProviderTests`, `SettingsReaderTests`, `SettingsSeederTests`, `SettingEntryRoundtripTests`, `SettingsRegistryTests`, `SuggestionBackfillCoordinatorTests` (real DbContext), `SuggestionService` decorator tests (added in the successor spec). | Infrastructure + Application + Domain. |
| `TradyStrat.E2E.Tests` | `ModuleSmokeTests`, any `SmokeTests.cs` at the current `TradyStrat.Tests/` root, and any future `WebApplicationFactory<Program>`-driven tests. | TradyStrat (Blazor) + everything else. |

Test fixtures (`FakeChatClient`, `StubAiClient`, `StubSnapshotFactory`, `FakeSettingsReader`) move with their primary consumers. If a fixture is used across two test projects, it lives in the lower-layer one and is referenced.

## 7. Migration plan

One PR. The refactor has no value partially complete — staged states would leave the project in mixed namespaces with build failures.

Suggested execution order inside that one PR (for the implementing agent's sanity, not for separate commits):

1. Bump and publish TheAppManager v3.0.0 (separate repo).
2. Create new `TradyStrat.Domain.csproj`, `TradyStrat.Application.csproj`, `TradyStrat.Infrastructure.csproj`, `TradyStrat.Cli.csproj` with `<ProjectReference>` graph as in §3.
3. Move files in dependency order: Domain → Application → Infrastructure. Razor stays in `TradyStrat`. Cli project gets `Program.cs` + placeholder `HelloCommand` only.
4. Update `using` directives and namespaces project-wide. The repo's existing namespace convention `TradyStrat.Features.<Feature>` stays where the file lands — Application's `AiSuggestion` feature folder becomes `TradyStrat.Application.AiSuggestion`; Infrastructure's becomes `TradyStrat.Infrastructure.AiSuggestion`.
5. Replace all `Modules/*.cs` files with the new per-layer modules in §5.
6. Update `TradyStrat/Program.cs` to call `AppManager.Start` with both module assemblies discovered.
7. Wire `TradyStrat.Cli/Program.cs`. Verify `dotnet run --project TradyStrat.Cli hello` prints a Spectre-rendered greeting.
8. Split the existing test project into the four new ones per §6.
9. Run full test suite — must pass with zero behavioural changes.
10. Run `dotnet build` on the solution — must succeed with no warnings beyond pre-existing ones.

## 8. Verification

The refactor is correct iff:

- `dotnet build TradyStrat.slnx` succeeds.
- `dotnet test TradyStrat.slnx` passes with the same number of tests as today (modulo the test project split).
- `dotnet run --project TradyStrat` starts the Blazor app and the dashboard renders identically.
- `dotnet run --project TradyStrat.Cli hello` prints the placeholder Spectre output.
- A snapshot diff of `AiSnapshotService` output (the existing `895EED53A280A470` sentinel hash) is byte-identical.
- The `Suggestions` table schema is unchanged (no migration generated).

## 9. Out of scope

- Adding the AI improvements (outcome feedback, caching, thinking, replay command) — that's the successor spec.
- Changing any prompt, indicator, or business rule.
- Renaming domain types, even where current names are awkward.
- DI container changes — stays `Microsoft.Extensions.DependencyInjection`.
- Test framework changes — stays `xunit.v3` + `Shouldly`.
- Solution file format — `.slnx` stays.

## 10. Risks

| Risk | Mitigation |
|---|---|
| Circular references discovered mid-move (e.g. an Application file ends up needing an Infrastructure type) | Means a port is missing. Add the port in Application, leave the impl in Infrastructure. This is the refactor doing its job. |
| TheAppManager v3 has consumers other than TradyStrat | User confirmed they own all consumers and will bump in lockstep. |
| Razor code-behind files reference types that move | Update namespaces; behaviour unchanged. Mechanical fix. |
| EF migrations break because `AppDbContext` moves | EF migrations are tied to the assembly that contains `DbContext`. Migrations move with the context — `DesignTime` migrations need re-running only if the tool can't find the new assembly. CI command updates accordingly (`dotnet ef --project TradyStrat.Infrastructure`). |
| The four-test-project split makes shared fixtures awkward | Promote shared fixtures to the lowest layer that needs them. Worst case: one extra `TestKit` class library if friction emerges (deferred). |
