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

- `Suggestion`, `SuggestionAction`, `SuggestionActionDisplay`, `Citation`, `MarketCitation`, `Zone`, `GoalConfig`, `Trade`, `TradeSide`, `Instrument`, `InstrumentKind`, `PortfolioSnapshot`, `PredictionMarket`, `Unit` (the `UseCaseBase` input sentinel — stays in Application since it's about the use-case shape, but its companion types live here).
- The custom exceptions in `TradyStrat/Common/Exceptions/` move here too — they're part of the domain vocabulary (`AnthropicCallFailedException` stays in Application; pure-domain ones like `FxRateUnavailableException` move).

NuGet dependencies in Domain: **none beyond the BCL**. No EF, no Json, no AI abstractions.

### 3.2 Application (TradyStrat.Application)

Use cases + ports. All `UseCases/` folders move here:

- `GetTodaysSuggestionUseCase`, `GetAllTodaysSuggestionsUseCase`, `ForceRefetchSuggestionUseCase`, `BackfillSuggestionsUseCase`, `LoadDashboardUseCase`, `ListInstrumentsUseCase`, `UpdateSettingUseCase`, etc.
- `UseCaseBase<TInput, TOutput>` and `Unit` (the input sentinel).
- All **ports** (interfaces the Application owns and consumes): `IAiClient`, `IAiSnapshotService`, `IRepositoryBase<>`, `IRepositoryBase<>` Specifications (Ardalis types are referenced; the *specification classes themselves* move here too), `IPredictionMarketProvider`, `ISettingsReader`, `IClock`, `IndicatorEngine` (currently a class — kept as a class but moved here since it has no external deps).
- The pure domain-service types that orchestrate but don't talk to adapters: `FxConverter`, `PortfolioService`, `AiSnapshotService` (the *service*, not the Anthropic adapter), `SuggestionGate`.
- `JsonOpts` stays here — it's a serialization policy used at the boundary by use cases.

NuGet dependencies in Application: `Ardalis.Specification` (interfaces only), `Microsoft.Extensions.AI.Abstractions` (the `IChatClient` interface — Application defines the port `IAiClient` that wraps it), `Microsoft.Extensions.Logging.Abstractions`. **No** `Microsoft.Extensions.AI` (concrete), **no** EF Core, **no** `Anthropic.SDK`, **no** HTTP clients.

### 3.3 Infrastructure (TradyStrat.Infrastructure)

Driven adapters — everything that talks to the outside world.

- `AppDbContext` and all EF configurations + migrations.
- `Ardalis.Specification.EntityFrameworkCore` repository implementations.
- `SuggestionService` (the Anthropic adapter — implements `IAiClient`).
- `PolymarketGammaProvider` (implements `IPredictionMarketProvider`).
- FX adapter implementation (the `FxConverter` *port* stays in Application; the HTTP client lives here).
- `SettingsReader` implementation (port `ISettingsReader` in Application).
- `SystemClock` implementation of `IClock`.
- Serilog wiring (today in `LoggingModule.cs`).
- HTTP resilience wiring (`Microsoft.Extensions.Http.Resilience`).

NuGet dependencies in Infrastructure: EF Core, `Microsoft.Extensions.AI` (concrete), `Anthropic.SDK`, `Microsoft.Extensions.Http.Resilience`, `Serilog.*`, `Atypical.TechnicalAnalysis.*` (the technical-analysis library is used by `IndicatorEngine`, but the engine itself is in Application — the library reference moves to Infrastructure only if the engine talks to live price feeds; today it doesn't, so the library reference stays in Application).

> **Note on IndicatorEngine:** Today it computes purely from injected price data and configuration — no I/O. It belongs in Application. If a future change makes it call live price feeds, an `IPriceFeed` port is introduced and the impl moves to Infrastructure.

### 3.4 TradyStrat (Blazor)

Razor pages, components, layout, `Program.cs`. Loses everything else.

- `Features/<Feature>/*.razor` and `*.razor.cs` files stay.
- `Features/Settings/Components/*.razor` files stay.
- `Modules/*.cs` files are deleted (replaced by feature modules in Application + Infrastructure — see §4).
- `Program.cs` becomes ~10 lines: build `WebApplication`, call `AppManager.Start(args, services, config, typeof(SomeApplicationModuleMarker).Assembly, typeof(SomeInfrastructureModuleMarker).Assembly)`, configure pipeline.

### 3.5 TradyStrat.Cli

New project. Initial content in this refactor: skeleton only.

- `Program.cs` builds an `IHostBuilder`, then calls `AppManager.Start(builder.Services, builder.Configuration, modules => modules.AddFromAssemblyOf<AiSuggestionApplicationModule>().AddFromAssemblyOf<AiSuggestionInfrastructureModule>())` (using the new neutral overload from §4.2). It then builds the host and hands `host.Services` to a `Spectre.Console.Cli.CommandApp` via a custom `ITypeRegistrar` that wraps the host's `IServiceProvider`.
- Commands folder exists but contains only a placeholder `HelloCommand` to prove the wiring (this is removed in the successor spec when `ReplayCommand` lands).

NuGet dependencies in Cli: `Spectre.Console`, `Spectre.Console.Cli`, `Microsoft.Extensions.Hosting`.

## 4. TheAppManager v3.0.0

Hard-break release. The library code lives in a separate repo (the user owns it).

### 4.1 New module signature

```csharp
public interface IAppModule
{
    void ConfigureServices(IServiceCollection services, IConfiguration config);
}
```

The `WebApplicationBuilder` overload is removed entirely. No `[Obsolete]` shim.

### 4.2 New entry-point overload

```csharp
public static class AppManager
{
    // Existing web overload, re-implemented on top of the neutral one
    public static void Start(string[] args, Action<ModuleDiscoveryBuilder>? configure = null) { ... }

    // New host-neutral overload — used by CLI, callable from any IHostBuilder consumer
    public static void Start(
        IServiceCollection services,
        IConfiguration config,
        Action<ModuleDiscoveryBuilder>? configure = null) { ... }
}
```

The web overload internally builds a `WebApplicationBuilder`, then forwards `builder.Services` and `builder.Configuration` to the neutral one. Both overloads share the same module discovery (assembly scan for `IAppModule` implementors).

### 4.3 Consumer migration

The only consumer in this repo is TradyStrat. The TheAppManager bump and the TradyStrat refactor land in the same PR boundary — TheAppManager v3.0.0 is published from its own repo first, then TradyStrat upgrades. If the user has other consumers, they're migrated in the same window.

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

Each module is one file with one `ConfigureServices` method. Both driving adapters discover modules from both assemblies via `AppManager.Start(..., typeof(AiSuggestionApplicationModule).Assembly, typeof(AiSuggestionInfrastructureModule).Assembly)`.

## 6. Test project split

The current `TradyStrat.Tests` project is dissolved. Each existing test file moves to one of four new projects, by what it tests:

| New project | Contains | References |
|---|---|---|
| `TradyStrat.Domain.Tests` | `EntityDerivedPropertiesTests`, `SuggestionActionDisplayTests`, `ExceptionHierarchyTests`, value-object tests. | Domain only. |
| `TradyStrat.Application.Tests` | All `UseCases/*Tests`, `Specifications/*Tests`, `AiSnapshot/*Tests`, `Settings/UseCases/*Tests`, `Common/Domain` use-case tests, `CallDiff/*Tests`, `Citations/*Tests`. Uses `StubAiClient`, `FakeChatClient`, `FakeSettingsReader`, in-memory EF where the test is really about use-case orchestration. | Application + Domain. |
| `TradyStrat.Infrastructure.Tests` | `MultiTickerAiPhase2MigrationTests`, `MigrationBackwardCompatTests`, `PolymarketGammaProviderTests`, `SettingsReaderTests`, `SettingsSeederTests`, `SettingEntryRoundtripTests`, `SettingsRegistryTests`, `SuggestionBackfillCoordinatorTests` (it hits a real DbContext today). | Infrastructure + Application + Domain. |
| `TradyStrat.E2E.Tests` | `ModuleSmokeTests` and any future `WebApplicationFactory<Program>`-driven tests. | TradyStrat (Blazor) + everything else. |

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
