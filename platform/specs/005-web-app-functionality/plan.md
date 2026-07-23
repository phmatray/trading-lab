# Implementation Plan: Interactive Web Application Functionality

**Branch**: `005-web-app-functionality` | **Date**: 2025-01-14 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/005-web-app-functionality/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Enable full interactive functionality in the Blazor Server web application for portfolio management, strategy configuration, backtesting, and risk management. Users will be able to close positions, configure strategy parameters, run backtests interactively, adjust risk settings, and see real-time updates via SignalR - all through a polished Tailwind CSS interface following Atomic Design patterns with Tb-prefixed components.

**Technical Approach**: Extend existing Blazor Server web application with interactive components and background services. Leverage TickerQ for background task processing (backtest execution, position management), Yahoo Finance API for symbol search and market data, and SignalR for real-time UI updates. Implement new service methods for write operations (close positions, save configurations), create interactive UI components with confirmation dialogs and loading states, and add background workers for long-running operations like backtests.

## Technical Context

**Language/Version**: C# 14 / .NET 10.0
**Primary Dependencies**: ASP.NET Core Blazor Server 10.0, SignalR 10.0 with MessagePack, Blazor-ApexCharts 6.0.2, Tailwind CSS 4.x, Entity Framework Core 10.0 (SQLite), Serilog 4.3.0, Ardalis.SmartEnum
**Storage**: SQLite via Entity Framework Core 10.0 (existing: Orders, Positions, Trades, Candles, Accounts, UserPreferences; new tables: StrategyConfigurations, BacktestResults, RiskSettings)
**Testing**: xUnit 3.2, bUnit 2.0.66 for Blazor components, FakeItEasy 8.3 for mocking, Shouldly 4.3 for assertions, Spectre.Console.Testing 0.54.0 for CLI
**Target Platform**: Cross-platform desktop web browsers (desktop-first, minimum 1024px width), ASP.NET Core Blazor Server Interactive Server render mode
**Project Type**: Web application (Blazor Server frontend + existing backend services integrated)
**Performance Goals**: Position closure confirmation < 3s, strategy parameter saves < 1min, backtest execution < 30s for 1-year daily data, real-time updates < 2s, page interactions < 200ms p95
**Constraints**: Desktop-only (no mobile), single-user application (no auth), WCAG 2.1 Level AA compliance, 80% test coverage minimum (100% for position closure and risk validation), no third-party component libraries (Tailwind + custom components only)
**Scale/Scope**: ~15-20 new Blazor components (forms, dialogs, interactive controls), ~10 new service methods across 5 services, 3 new database entities, 2-3 background workers, ~50 unit/integration tests, ~2000 lines of new code

**User Input Context**: User specified "blazor server, only tailwind, tickerq integrated into the blazor app for background services, yahoo finance for searching a symbol... the ui should be good."

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Code Quality ✅
- **Clean Code**: All new components follow Atomic Design with Tb-prefix, max 50 lines per function, max 300 lines per class
- **C# Standards**: PascalCase public, camelCase private with underscore, async/await for I/O, nullable references enabled
- **SOLID Principles**: Services use interfaces, DI for all dependencies, single responsibility maintained
- **Documentation**: XML docs for all public APIs, inline comments for complex logic

### Testing ✅
- **Coverage**: Target 80% overall, 100% for ClosePositionAsync, ValidateRiskSettings, RunBacktestAsync (critical paths)
- **Test Pyramid**: Unit tests (70%) for services/validators, integration tests (20%) for EF/SignalR, E2E tests (10%) for critical flows
- **Test Quality**: AAA pattern, descriptive names (MethodName_Scenario_ExpectedBehavior), no test logic, <5min full suite
- **Component Testing**: bUnit 2.0.66 for Blazor component rendering, event handling, parameter passing

### User Experience ✅
- **Consistency**: Tailwind CSS utilities, Atomic Design (Atoms→Molecules→Organisms→Features→Pages), Tb-prefixed components, Heroicons
- **Accessibility**: WCAG 2.1 Level AA, keyboard navigation (Tab/Enter/Escape), ARIA labels, 4.5:1 contrast ratios, visible focus rings
- **Responsive**: Desktop-first 1024px-4K, collapsible sidebar, no mobile requirement
- **Feedback**: User-friendly errors, actionable messages, progress indicators >1s, confirmation dialogs for destructive actions

### Performance ✅
- **Response Times**: API <200ms p95, page load <1.5s FCP, <3.5s TTI, dashboard updates <2s
- **Scalability**: Stateless services, SignalR for real-time push, background workers for long-running tasks (backtests)
- **Optimization**: Proper EF tracking (.AsNoTracking() for reads), connection pooling, memory disposal (IDisposable), efficient queries

### Security ✅
- **Input Validation**: All form inputs validated (client + server), parameterized queries (EF prevents SQL injection), XSS prevention via Blazor escaping
- **Data Protection**: Existing encryption for API keys (Infrastructure.Services.EncryptionService), TLS for all traffic
- **Financial Security**: Risk validation before order execution, audit trail for all trades (existing), compliance with limits

### DevOps ✅
- **CI/CD**: Existing GitHub Actions workflows (ci.yml runs build/test/coverage on all PRs), no changes needed
- **Version Control**: GitFlow on feature branch 005-web-app-functionality, conventional commits, PR review required for main
- **Code Analysis**: StyleCop, Roslynator, SonarAnalyzer, TreatWarningsAsErrors=true (already enforced via Directory.Build.props)

**Status**: ✅ All constitution gates PASS - No violations requiring justification

## Project Structure

### Documentation (this feature)

```text
specs/005-web-app-functionality/
├── spec.md              # Feature specification (/speckit.specify output)
├── plan.md              # This file (/speckit.plan output)
├── research.md          # Phase 0 output (/speckit.plan - TickerQ integration, symbol search patterns)
├── data-model.md        # Phase 1 output (/speckit.plan - StrategyConfiguration, RiskSettings, BacktestResult entities)
├── quickstart.md        # Phase 1 output (/speckit.plan - Developer setup and testing guide)
├── contracts/           # Phase 1 output (/speckit.plan - Service interfaces and DTOs)
│   ├── IPortfolioService.cs
│   ├── IStrategyManagementService.cs
│   ├── IBacktestService.cs
│   ├── IRiskSettingsService.cs
│   └── SignalRHubContracts.md
├── checklists/          # Custom quality checklists
│   └── requirements.md  # Spec quality checklist (already created)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

**Existing Structure** (Clean Architecture with 7 projects):

```text
src/
├── TradingBot.Core/              # Domain models, interfaces, SmartEnums (no dependencies)
│   ├── Models/
│   │   ├── Trading/ (Order, Position, Trade, Signal, Account, Candle)
│   │   └── Backtest/ (BacktestResult - ADD for this feature)
│   └── Interfaces/
│       ├── IStrategy, IStrategyEngine, IPortfolioManager, IRiskManager
│       ├── IOrderExecutionService, IPositionSizeCalculator, IStopLossManager
│       └── Repositories/ (IOrderRepository, IPositionRepository, ITradeRepository)
│
├── TradingBot.Infrastructure/    # Data access, external services (depends on Core)
│   ├── Persistence/
│   │   ├── TradingBotDbContext.cs
│   │   ├── Configurations/ (entity configurations)
│   │   │   ├── EXISTING: OrderConfiguration, PositionConfiguration, TradeConfiguration
│   │   │   └── NEW: StrategyConfigurationEntityConfig, RiskSettingsEntityConfig, BacktestResultEntityConfig
│   │   └── Repositories/ (implementations)
│   ├── Services/
│   │   ├── EncryptionService.cs (existing)
│   │   └── YahooFinanceService.cs (existing - ADD symbol search method)
│   └── Migrations/ (NEW: AddStrategyConfigurationsTable, AddRiskSettingsTable, AddBacktestResultsTable)
│
├── TradingBot.Engine/            # Core trading engine (depends on Core)
│   ├── StrategyEngine.cs
│   ├── SignalProcessor.cs
│   ├── OrderExecutionService.cs
│   ├── PortfolioManager.cs
│   ├── RiskManager.cs
│   ├── PositionSizeCalculator.cs
│   └── StopLossManager.cs
│
├── TradingBot.Strategies/        # Strategy implementations (depends on Core)
│   ├── Base/ (SMA, EMA, RSI, MACD, BollingerBands, ATR)
│   ├── MomentumStrategy.cs
│   └── MeanReversionStrategy.cs
│
├── TradingBot.Analytics/         # Performance analytics (depends on Core)
│   └── PerformanceMetrics.cs
│
├── TradingBot.Cli/               # CLI application (composition root)
│   └── (no changes for this feature)
│
└── TradingBot.Web/               # Blazor Server web app ⭐ PRIMARY FOCUS
    ├── Components/
    │   ├── Atoms/ (TbButton, TbInput, TbLabel, TbBadge, TbIcon, TbSpinner, TbToggle)
    │   ├── Molecules/ (TbFormField, TbMenuItem, TbToast)
    │   │   └── NEW: TbConfirmDialog.razor, TbLoadingOverlay.razor
    │   ├── Organisms/ (TbNavigationSidebar, TbThemeProvider, TbToastContainer, TbSettingsForm)
    │   │   └── NEW: TbSymbolSearchInput.razor
    │   ├── Features/
    │   │   ├── Dashboard/ (existing display components)
    │   │   ├── Portfolio/
    │   │   │   ├── EXISTING: TbTradeHistoryFilter, TbTradeHistoryTable
    │   │   │   └── NEW: TbOpenPositionsTable.razor (with Close button), TbClosePositionDialog.razor
    │   │   ├── Strategy/
    │   │   │   ├── EXISTING: TbStrategyCard (display only)
    │   │   │   └── NEW: TbStrategyConfigForm.razor, TbStrategyParameterInput.razor
    │   │   ├── Backtest/
    │   │   │   ├── EXISTING: TbBacktestResultsList, TbBacktestDetail (display only)
    │   │   │   └── NEW: TbBacktestForm.razor, TbBacktestProgress.razor, TbBacktestRunner.razor
    │   │   └── Risk/
    │   │       └── NEW: TbRiskSettingsForm.razor
    │   ├── Pages/ (full page components)
    │   │   ├── Index.razor (Dashboard - ENHANCE with real-time updates)
    │   │   ├── Portfolio.razor (ENHANCE with close position functionality)
    │   │   ├── Strategies.razor (ENHANCE with configuration form)
    │   │   ├── Backtest.razor (ENHANCE with run backtest form)
    │   │   ├── RiskSettingsPage.razor (ENHANCE with save functionality)
    │   │   ├── Performance.razor, Settings.razor, Help.razor (no changes)
    │   │   └── _Imports.razor
    │   └── Layout/ (MainLayout - no changes)
    │
    ├── Services/
    │   ├── EXISTING (read-only): DashboardService, PortfolioService, PerformanceService
    │   ├── EXISTING (partial): StrategyManagementService (enable/disable - ADD configure methods)
    │   ├── EXISTING (stub): BacktestService (display results - ADD run backtest methods)
    │   ├── EXISTING (display): RiskSettingsService (ADD save/reset methods)
    │   ├── EXISTING: ToastService, NavigationService, UIStateService (no changes)
    │   ├── ENHANCE: RealtimeUpdateService (add more SignalR event types)
    │   └── NEW: BackgroundTaskService.cs (TickerQ wrapper for backtest execution)
    │
    ├── Hubs/
    │   └── TradingHub.cs (ENHANCE with new event methods: OnPositionClosed, OnBacktestProgress, OnRiskSettingsChanged)
    │
    ├── Workers/
    │   └── NEW: BacktestExecutionWorker.cs (background service using TickerQ for async backtest execution)
    │
    ├── Models/
    │   └── NEW: SymbolSearchResult.cs, BacktestRequest.cs, StrategyParameterDto.cs
    │
    ├── wwwroot/
    │   └── (Tailwind CSS, compiled app.css - no changes)
    │
    └── Program.cs (ENHANCE with new service registrations, background workers)

tests/
├── TradingBot.Core.Tests/ (no changes)
├── TradingBot.Infrastructure.Tests/ (ADD tests for new repositories)
├── TradingBot.Engine.Tests/ (no changes)
├── TradingBot.Strategies.Tests/ (no changes)
├── TradingBot.Analytics.Tests/ (no changes)
└── TradingBot.Web.Tests/
    ├── Services/
    │   ├── ENHANCE: PortfolioServiceTests (test ClosePositionAsync)
    │   ├── ENHANCE: StrategyManagementServiceTests (test ConfigureStrategyAsync)
    │   ├── ENHANCE: BacktestServiceTests (test RunBacktestAsync)
    │   └── NEW: RiskSettingsServiceTests (test SaveAsync, ResetToDefaultsAsync)
    ├── Components/
    │   ├── NEW: TbConfirmDialogTests.razor
    │   ├── NEW: TbStrategyConfigFormTests.razor
    │   ├── NEW: TbBacktestFormTests.razor
    │   └── NEW: TbRiskSettingsFormTests.razor
    └── Integration/
        ├── NEW: PortfolioManagementIntegrationTests (end-to-end close position flow)
        ├── NEW: BacktestExecutionIntegrationTests (end-to-end backtest flow)
        └── NEW: SignalRIntegrationTests (test real-time updates)
```

**Structure Decision**: The project uses Clean Architecture with a clear separation of concerns:
- **Core** layer contains pure domain models and interfaces (no dependencies)
- **Infrastructure** layer handles data persistence (EF Core) and external APIs (Yahoo Finance)
- **Engine/Strategies/Analytics** layers implement business logic
- **Web** layer is the Blazor Server presentation layer where this feature is primarily implemented
- **Cli** layer is an alternative presentation layer (no changes needed)

This feature enhances the existing **TradingBot.Web** project with interactive functionality while adding new entities to **TradingBot.Core** and persistence to **TradingBot.Infrastructure**. The architecture remains unchanged - we're filling in the interactive gaps in the web UI.

## Complexity Tracking

**No violations** - All constitution checks pass. The feature leverages existing architecture and follows established patterns.
