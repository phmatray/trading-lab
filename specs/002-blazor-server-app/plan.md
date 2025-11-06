# Implementation Plan: Blazor Server Trading Dashboard

**Branch**: `002-blazor-server-app` | **Date**: 2025-11-07 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-blazor-server-app/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Build a Blazor Server web application that replicates the functionality of the existing TradingBot CLI, providing a browser-based dashboard for real-time portfolio monitoring, position management, performance analytics, strategy configuration, and risk management. The application will use Blazor Server rendering mode with Tailwind CSS for styling, SignalR for real-time updates, and bUnit for component testing.

## Technical Context

**Language/Version**: C# / .NET 9
**Primary Dependencies**: ASP.NET Core Blazor Server, Tailwind CSS, SignalR, bUnit (testing), existing TradingBot layers (Core, Infrastructure, Engine, Analytics, Strategies)
**Storage**: SQLite via Entity Framework Core 9 (shared with CLI application)
**Testing**: xUnit + bUnit for Blazor component tests, FakeItEasy for mocking, Shouldly for assertions
**Target Platform**: Web browsers (Chrome, Edge, Firefox, Safari - last 2 versions), desktop-only responsive design
**Project Type**: Web application (Blazor Server)
**Performance Goals**: Dashboard load < 2s, real-time updates < 2s lag, API responses < 200ms p95, support 50+ concurrent users
**Constraints**: Must reuse existing Core/Infrastructure/Engine layers unchanged, desktop-only (no mobile), same machine deployment initially
**Scale/Scope**: 6 user stories (P1-P4 priority), ~15 pages/components, real-time SignalR integration, up to 10,000 trades per export

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Code Quality Gates
- ✅ **Layered Architecture**: Blazor project will be a new presentation layer, reusing existing Core/Infrastructure/Engine/Analytics layers unchanged
- ✅ **SOLID Principles**: Component-based Blazor architecture naturally supports SRP and DIP through dependency injection
- ✅ **Naming Conventions**: All C# code will follow PascalCase for public, camelCase with underscore for private fields
- ✅ **File Headers**: All .cs files will include copyright headers per existing standard
- ✅ **SmartEnum Pattern**: Will use existing SmartEnum types from TradingBot.Core

### Testing Gates
- ✅ **80% Code Coverage**: Component tests with bUnit, service tests with xUnit, integration tests for SignalR hubs
- ✅ **100% Critical Path Coverage**: All real-time update logic, data display components, and position closing operations
- ✅ **AAA Pattern**: All tests will follow Arrange-Act-Assert structure
- ✅ **Test Naming**: `ComponentName_Scenario_ExpectedBehavior` format (e.g., `Dashboard_WithPositions_DisplaysPositionCards`)
- ✅ **Fast Execution**: Component tests expected < 50ms each, full suite < 5 minutes

### Performance Gates
- ✅ **Response Time**: Dashboard load < 2s (FCP < 1.5s requirement from constitution)
- ✅ **API p95 < 200ms**: All data fetching endpoints meet constitution requirement
- ✅ **Scalability**: Stateless Blazor Server circuits, horizontal scaling ready
- ✅ **Caching**: Market data cached with 5-60s TTL per constitution
- ✅ **Monitoring**: Structured logging with Serilog, correlation IDs for request tracking

### Security Gates
- ✅ **Authentication**: ASP.NET Core Identity integration (authentication mechanism already exists per spec assumptions)
- ✅ **Authorization**: Role-based access control for trading operations
- ✅ **Input Validation**: All user inputs validated client-side and server-side
- ✅ **XSS Prevention**: Blazor automatic escaping, parameterized queries for database access
- ✅ **Audit Trail**: All position closures and risk setting changes logged to database

### UX Consistency Gates
- ✅ **Responsive Design**: Desktop-only per spec, follows Tailwind CSS design system
- ✅ **Accessibility**: WCAG 2.1 Level AA compliance for keyboard navigation and screen readers
- ✅ **Error Handling**: User-friendly messages, connection status indicators, actionable feedback
- ✅ **Real-time Updates**: SignalR for live data, < 2s update lag per spec
- ✅ **Color Coding**: Green for positive P&L, red for negative, yellow for warnings (per constitution)

### Status: ✅ PASS
All gates align with constitution requirements. No violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/002-blazor-server-app/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── api-spec.yaml    # OpenAPI/REST endpoint contracts
│   └── signalr-hub.md   # SignalR hub contracts
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

**Existing Structure** (unchanged):
```text
src/
├── TradingBot.Core/           # Domain models, interfaces, enums (SmartEnum)
├── TradingBot.Infrastructure/ # EF Core DbContext, repositories, Yahoo Finance integration
├── TradingBot.Engine/         # Trading engine, signal processing, order execution
├── TradingBot.Analytics/      # Performance metrics calculations
├── TradingBot.Strategies/     # Strategy implementations (momentum, mean reversion)
└── TradingBot.Cli/            # Existing CLI application (Spectre.Console)

tests/
├── TradingBot.Core.Tests/
├── TradingBot.Infrastructure.Tests/
├── TradingBot.Engine.Tests/
├── TradingBot.Analytics.Tests/
└── TradingBot.Strategies.Tests/
```

**New Structure** (to be created for this feature):
```text
src/
└── TradingBot.Web/            # NEW: Blazor Server web application
    ├── Components/            # Blazor components
    │   ├── Layout/           # MainLayout, NavMenu
    │   ├── Dashboard/        # Dashboard widgets (AccountSummary, PositionList, RecentTrades)
    │   ├── Portfolio/        # Portfolio history components
    │   ├── Performance/      # Performance charts and metrics
    │   ├── Strategy/         # Strategy management components
    │   ├── Risk/             # Risk settings components
    │   └── Shared/           # Shared UI components (cards, tables, charts)
    ├── Pages/                # Routable pages
    │   ├── Index.razor       # Dashboard page
    │   ├── Portfolio.razor   # Portfolio history page
    │   ├── Performance.razor # Performance analytics page
    │   ├── Strategies.razor  # Strategy management page
    │   ├── RiskSettings.razor # Risk configuration page
    │   └── Backtest.razor    # Backtest results page
    ├── Hubs/                 # SignalR hubs
    │   └── TradingHub.cs     # Real-time data hub
    ├── Services/             # Application services
    │   ├── DashboardService.cs
    │   ├── PortfolioService.cs
    │   ├── PerformanceService.cs
    │   └── RealtimeUpdateService.cs
    ├── wwwroot/              # Static assets
    │   ├── css/
    │   │   └── app.css       # Tailwind CSS output
    │   └── js/
    │       └── signalr-client.js
    ├── Program.cs            # Application entry point, DI configuration
    ├── appsettings.json      # Configuration
    └── TradingBot.Web.csproj

tests/
└── TradingBot.Web.Tests/     # NEW: Web application tests
    ├── Components/           # bUnit component tests
    ├── Services/             # Service unit tests
    ├── Hubs/                 # SignalR hub integration tests
    └── TradingBot.Web.Tests.csproj
```

**Tailwind CSS Setup**:
```text
src/TradingBot.Web/
├── Styles/
│   └── app.css               # Tailwind input file
├── tailwind.config.js        # Tailwind configuration
├── package.json              # Node.js dependencies (Tailwind CLI)
└── package-lock.json
```

**Structure Decision**:
- This is a **web application** that adds a new presentation layer (TradingBot.Web) to the existing layered architecture
- The Blazor Server project will reference all existing projects (Core, Infrastructure, Engine, Analytics, Strategies) to reuse business logic
- Blazor components follow a hierarchical structure: Pages → Components → Shared
- SignalR hubs are isolated in their own directory for real-time functionality
- Services provide an abstraction between Blazor components and the existing business logic layers
- bUnit tests mirror the component structure for maintainability
- Tailwind CSS is built using npm scripts during development and build processes

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
