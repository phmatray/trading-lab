# Tasks: Blazor Server Trading Dashboard

**Input**: Design documents from `/specs/002-blazor-server-app/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Tests are NOT explicitly requested in the specification, so test tasks are excluded. Focus is on implementation only.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

This project follows the existing TradingBot repository structure:
- **Web project**: `src/TradingBot.Web/`
- **Test project**: `tests/TradingBot.Web.Tests/`
- **Existing projects**: `src/TradingBot.{Core,Infrastructure,Engine,Analytics,Strategies}/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create Blazor Server project at src/TradingBot.Web with .NET 9 and ASP.NET Core Blazor Server dependencies
- [X] T002 Add project references to TradingBot.Core, TradingBot.Infrastructure, TradingBot.Engine, TradingBot.Analytics, and TradingBot.Strategies in src/TradingBot.Web/TradingBot.Web.csproj
- [X] T003 [P] Install NuGet packages: Blazor-ApexCharts (6.0.2) in src/TradingBot.Web/TradingBot.Web.csproj
- [X] T004 [P] Install NuGet packages: Microsoft.AspNetCore.SignalR.Protocols.MessagePack (9.0.0) in src/TradingBot.Web/TradingBot.Web.csproj
- [X] T005 Initialize npm project and install Tailwind CSS dependencies (tailwindcss) in src/TradingBot.Web/
- [X] T006 Create tailwind.config.js with content paths for all Razor components in src/TradingBot.Web/
- [X] T007 Create Tailwind input CSS file at src/TradingBot.Web/Styles/app.css with base, components, and utilities layers
- [X] T008 Add npm scripts for Tailwind CSS watch and build in src/TradingBot.Web/package.json
- [X] T009 Add MSBuild targets for automatic Tailwind CSS compilation in src/TradingBot.Web/TradingBot.Web.csproj
- [X] T010 Create directory structure: Components/{Layout,Dashboard,Portfolio,Performance,Strategy,Risk,Shared,Charts} in src/TradingBot.Web/
- [X] T011 Create directory structure: Pages/ in src/TradingBot.Web/
- [X] T012 Create directory structure: Services/ and Hubs/ in src/TradingBot.Web/
- [X] T013 Update appsettings.json with ConnectionStrings, TradingBot configuration, and SignalR settings in src/TradingBot.Web/
- [X] T014 Create xUnit test project at tests/TradingBot.Web.Tests with references to TradingBot.Web and TradingBot.Core
- [X] T015 [P] Install test packages: bUnit (1.28.9), FakeItEasy (8.3.0), Shouldly (4.2.1) in tests/TradingBot.Web.Tests/TradingBot.Web.Tests.csproj

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T016 Create view model classes: DashboardViewModel, PortfolioHistoryFilter, PortfolioHistoryResult, EquityCurveDataPoint, ConnectionStatusViewModel in src/TradingBot.Web/Models/
- [X] T017 Create service interfaces: IDashboardService, IPortfolioService, IPerformanceService, IStrategyManagementService, IRiskSettingsService, IBacktestService in src/TradingBot.Web/Services/
- [X] T018 Create ITradingClient interface with SignalR callback methods (ReceiveAccountUpdate, ReceivePositionUpdate, ReceiveTradeUpdate, ReceiveConnectionStatus) in src/TradingBot.Web/Hubs/
- [X] T019 Implement TradingHub with strongly-typed ITradingClient, lifecycle events (OnConnectedAsync, OnDisconnectedAsync), and client-to-server methods in src/TradingBot.Web/Hubs/TradingHub.cs
- [X] T020 Implement DashboardService with GetDashboardDataAsync method aggregating data from all sources in src/TradingBot.Web/Services/DashboardService.cs
- [X] T021 Implement PortfolioService with GetTradeHistoryAsync, ClosePositionAsync, and ExportTradeHistoryAsync methods in src/TradingBot.Web/Services/PortfolioService.cs
- [X] T022 Implement PerformanceService with GetCurrentMetricsAsync and GetEquityCurveAsync methods in src/TradingBot.Web/Services/PerformanceService.cs
- [X] T023 Implement StrategyManagementService with GetAllStrategiesAsync, EnableStrategyAsync, and DisableStrategyAsync methods in src/TradingBot.Web/Services/StrategyManagementService.cs
- [X] T024 Implement RiskSettingsService with GetCurrentSettingsAsync and UpdateSettingsAsync (with validation) in src/TradingBot.Web/Services/RiskSettingsService.cs
- [X] T025 Implement BacktestService with GetBacktestResultsAsync and GetBacktestByIdAsync methods in src/TradingBot.Web/Services/BacktestService.cs
- [X] T026 Create RealtimeUpdateService as IHostedService broadcasting account/position/trade updates every 100ms in src/TradingBot.Web/Services/RealtimeUpdateService.cs
- [X] T027 Update Program.cs with Blazor Server configuration, SignalR registration with performance tuning, and all service registrations in src/TradingBot.Web/Program.cs
- [X] T028 Create base layout component MainLayout.razor with navigation, header, and connection status indicator in src/TradingBot.Web/Components/Layout/MainLayout.razor
- [X] T029 Create NavMenu.razor component with links to all pages (Dashboard, Portfolio, Performance, Strategies, Risk Settings, Backtest) in src/TradingBot.Web/Components/Layout/NavMenu.razor
- [X] T030 Create shared UI components: Card.razor, Table.razor, Button.razor, Modal.razor with Tailwind CSS styling in src/TradingBot.Web/Components/Shared/
- [X] T031 Update App.razor with Blazor Server reconnection configuration and route configuration in src/TradingBot.Web/App.razor
- [X] T032 Create CSS custom component classes (card-trading, stat-positive, stat-negative) in Tailwind @layer components in src/TradingBot.Web/Styles/app.css

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Real-Time Trading Dashboard (Priority: P1) 🎯 MVP

**Goal**: Provide web-based access to real-time trading information (account, positions, trades, metrics, risk settings)

**Independent Test**: Access the web dashboard and verify that all key metrics are displayed and update in real-time

### Implementation for User Story 1

- [ ] T033 [P] [US1] Create AccountSummary.razor component displaying equity, cash, position value, buying power, unrealized/realized P&L in src/TradingBot.Web/Components/Dashboard/AccountSummary.razor
- [ ] T034 [P] [US1] Create PositionList.razor component displaying open positions with symbol, quantity, entry/current price, P&L, strategy in src/TradingBot.Web/Components/Dashboard/PositionList.razor
- [ ] T035 [P] [US1] Create RecentTrades.razor component displaying last 5 trades with date, symbol, side, quantity, realized P&L in src/TradingBot.Web/Components/Dashboard/RecentTrades.razor
- [ ] T036 [P] [US1] Create PerformanceMetrics.razor component displaying total return, win rate, Sharpe/Sortino/Calmar ratios, max drawdown, profit factor in src/TradingBot.Web/Components/Dashboard/PerformanceMetrics.razor
- [ ] T037 [P] [US1] Create RiskSettingsCard.razor component displaying current risk settings (read-only) in src/TradingBot.Web/Components/Dashboard/RiskSettingsCard.razor
- [ ] T038 [P] [US1] Create ActiveStrategies.razor component displaying enabled strategies with their status in src/TradingBot.Web/Components/Dashboard/ActiveStrategies.razor
- [ ] T039 [US1] Create Index.razor (Dashboard page) integrating all dashboard components and establishing SignalR connection in src/TradingBot.Web/Pages/Index.razor
- [ ] T040 [US1] Add SignalR client-side handlers for ReceiveAccountUpdate in Index.razor to update AccountSummary component in src/TradingBot.Web/Pages/Index.razor
- [ ] T041 [US1] Add SignalR client-side handlers for ReceivePositionUpdate in Index.razor to update PositionList component in src/TradingBot.Web/Pages/Index.razor
- [ ] T042 [US1] Add SignalR client-side handlers for ReceiveTradeUpdate in Index.razor to update RecentTrades component in src/TradingBot.Web/Pages/Index.razor
- [ ] T043 [US1] Implement automatic reconnection logic with exponential backoff in Index.razor in src/TradingBot.Web/Pages/Index.razor
- [ ] T044 [US1] Add connection status indicator showing connected/reconnecting/disconnected states in Index.razor in src/TradingBot.Web/Pages/Index.razor
- [ ] T045 [US1] Implement color coding: green for positive P&L, red for negative, yellow for warnings across all dashboard components in src/TradingBot.Web/Components/Dashboard/
- [ ] T046 [US1] Add last updated timestamps to dashboard data displays in src/TradingBot.Web/Components/Dashboard/
- [ ] T047 [US1] Implement empty state handling: "No open positions" message when positions list is empty in src/TradingBot.Web/Components/Dashboard/PositionList.razor
- [ ] T048 [US1] Implement empty state handling: "No recent trades" message when trades list is empty in src/TradingBot.Web/Components/Dashboard/RecentTrades.razor

**Checkpoint**: At this point, User Story 1 should be fully functional - dashboard displays real-time data with automatic updates

---

## Phase 4: User Story 2 - Portfolio Management Interface (Priority: P2)

**Goal**: Enable viewing complete portfolio history and closing positions through the web interface

**Independent Test**: Navigate to portfolio section, view trade history with filters, and successfully close an open position

### Implementation for User Story 2

- [ ] T049 [P] [US2] Create Portfolio.razor page with trade history table and filter controls in src/TradingBot.Web/Pages/Portfolio.razor
- [ ] T050 [P] [US2] Create TradeHistoryFilter.razor component with date range, symbol, and strategy filters in src/TradingBot.Web/Components/Portfolio/TradeHistoryFilter.razor
- [ ] T051 [P] [US2] Create TradeHistoryTable.razor component displaying paginated trade list with all trade details in src/TradingBot.Web/Components/Portfolio/TradeHistoryTable.razor
- [ ] T052 [P] [US2] Create PositionCard.razor component with position details and "Close Position" button in src/TradingBot.Web/Components/Portfolio/PositionCard.razor
- [ ] T053 [P] [US2] Create ClosePositionModal.razor confirmation dialog showing position details and estimated proceeds in src/TradingBot.Web/Components/Portfolio/ClosePositionModal.razor
- [ ] T054 [US2] Implement filter logic in Portfolio.razor calling PortfolioService.GetTradeHistoryAsync with PortfolioHistoryFilter in src/TradingBot.Web/Pages/Portfolio.razor
- [ ] T055 [US2] Implement pagination controls (previous/next, page numbers) in TradeHistoryTable.razor in src/TradingBot.Web/Components/Portfolio/TradeHistoryTable.razor
- [ ] T056 [US2] Implement "Close Position" handler invoking ClosePositionModal and calling PortfolioService.ClosePositionAsync in src/TradingBot.Web/Components/Portfolio/PositionCard.razor
- [ ] T057 [US2] Add success notification when position closed successfully in Portfolio.razor in src/TradingBot.Web/Pages/Portfolio.razor
- [ ] T058 [US2] Add error handling and user-friendly error messages for close position failures in Portfolio.razor in src/TradingBot.Web/Pages/Portfolio.razor
- [ ] T059 [US2] Implement real-time position removal via SignalR when position closed in Portfolio.razor in src/TradingBot.Web/Pages/Portfolio.razor
- [ ] T060 [US2] Implement CSV export button calling PortfolioService.ExportTradeHistoryAsync in Portfolio.razor in src/TradingBot.Web/Pages/Portfolio.razor
- [ ] T061 [US2] Create JavaScript helper function for downloading exported file in wwwroot/js/download.js in src/TradingBot.Web/wwwroot/js/download.js
- [ ] T062 [US2] Add client-side validation to filter inputs (date range validation, valid symbols) in TradeHistoryFilter.razor in src/TradingBot.Web/Components/Portfolio/TradeHistoryFilter.razor

**Checkpoint**: At this point, User Stories 1 AND 2 should both work - dashboard shows live data, portfolio page shows history and allows position closure

---

## Phase 5: User Story 3 - Performance Analytics Visualization (Priority: P2)

**Goal**: Display performance metrics and statistics with visual charts

**Independent Test**: Navigate to performance page and verify that all performance metrics are displayed with appropriate visualizations

### Implementation for User Story 3

- [ ] T063 [P] [US3] Create Performance.razor page layout with metrics grid and equity curve chart in src/TradingBot.Web/Pages/Performance.razor
- [ ] T064 [P] [US3] Create PerformanceMetricsGrid.razor component displaying all key metrics (total return, win rate, Sharpe/Sortino/Calmar ratios, max drawdown, profit factor) in src/TradingBot.Web/Components/Performance/PerformanceMetricsGrid.razor
- [ ] T065 [P] [US3] Create TradingStatistics.razor component displaying total trades, winning/losing counts, average win/loss, expectancy in src/TradingBot.Web/Components/Performance/TradingStatistics.razor
- [ ] T066 [P] [US3] Create EquityCurveChart.razor component using ApexCharts to display portfolio value over time in src/TradingBot.Web/Components/Charts/EquityCurveChart.razor
- [ ] T067 [US3] Integrate PerformanceService.GetCurrentMetricsAsync in Performance.razor to load performance data in src/TradingBot.Web/Pages/Performance.razor
- [ ] T068 [US3] Integrate PerformanceService.GetEquityCurveAsync in Performance.razor to load equity curve data in src/TradingBot.Web/Pages/Performance.razor
- [ ] T069 [US3] Implement color coding: green for positive metrics, red for negative metrics in PerformanceMetricsGrid.razor in src/TradingBot.Web/Components/Performance/PerformanceMetricsGrid.razor
- [ ] T070 [US3] Add date range filter for equity curve chart in Performance.razor in src/TradingBot.Web/Pages/Performance.razor
- [ ] T071 [US3] Configure ApexCharts with responsive design and Tailwind CSS theming in EquityCurveChart.razor in src/TradingBot.Web/Components/Charts/EquityCurveChart.razor
- [ ] T072 [US3] Implement empty state handling: "No trading data available" when no trades exist in Performance.razor in src/TradingBot.Web/Pages/Performance.razor

**Checkpoint**: All three user stories (Dashboard, Portfolio, Performance) should now be independently functional with rich visualizations

---

## Phase 6: User Story 4 - Strategy Management (Priority: P3)

**Goal**: View and manage trading strategies (enable/disable) through the web interface

**Independent Test**: View the strategies list, enable/disable a strategy, and verify the changes persist

### Implementation for User Story 4

- [ ] T073 [P] [US4] Create Strategies.razor page with strategy list table in src/TradingBot.Web/Pages/Strategies.razor
- [ ] T074 [P] [US4] Create StrategyCard.razor component displaying strategy name, type, symbols, timeframe, and status in src/TradingBot.Web/Components/Strategy/StrategyCard.razor
- [ ] T075 [P] [US4] Create StrategyStatusToggle.razor component with enable/disable button in src/TradingBot.Web/Components/Strategy/StrategyStatusToggle.razor
- [ ] T076 [US4] Integrate StrategyManagementService.GetAllStrategiesAsync in Strategies.razor to load strategy list in src/TradingBot.Web/Pages/Strategies.razor
- [ ] T077 [US4] Implement "Enable" button handler calling StrategyManagementService.EnableStrategyAsync in StrategyStatusToggle.razor in src/TradingBot.Web/Components/Strategy/StrategyStatusToggle.razor
- [ ] T078 [US4] Implement "Disable" button handler calling StrategyManagementService.DisableStrategyAsync in StrategyStatusToggle.razor in src/TradingBot.Web/Components/Strategy/StrategyStatusToggle.razor
- [ ] T079 [US4] Add visual indicator showing active/disabled status with color coding (green for active, red for disabled) in StrategyCard.razor in src/TradingBot.Web/Components/Strategy/StrategyCard.razor
- [ ] T080 [US4] Add success notification when strategy status changed in Strategies.razor in src/TradingBot.Web/Pages/Strategies.razor
- [ ] T081 [US4] Add error handling for strategy enable/disable failures in Strategies.razor in src/TradingBot.Web/Pages/Strategies.razor
- [ ] T082 [US4] Implement empty state handling: "No strategies configured" when strategy list is empty in Strategies.razor in src/TradingBot.Web/Pages/Strategies.razor

**Checkpoint**: User Story 4 complete - strategies can be managed through web interface

---

## Phase 7: User Story 5 - Risk Management Configuration (Priority: P3)

**Goal**: View and adjust risk management settings through the web interface

**Independent Test**: Navigate to risk settings, modify a parameter, and verify the change is saved and reflected in the dashboard

### Implementation for User Story 5

- [ ] T083 [P] [US5] Create RiskSettings.razor page with risk configuration form in src/TradingBot.Web/Pages/RiskSettings.razor
- [ ] T084 [P] [US5] Create RiskSettingsForm.razor component with inputs for leverage, stop-loss %, take-profit %, daily loss limit, max drawdown %, max position size % in src/TradingBot.Web/Components/Risk/RiskSettingsForm.razor
- [ ] T085 [P] [US5] Create RiskStatusIndicator.razor component showing current risk status (enabled/disabled) in src/TradingBot.Web/Components/Risk/RiskStatusIndicator.razor
- [ ] T086 [US5] Integrate RiskSettingsService.GetCurrentSettingsAsync in RiskSettings.razor to load current settings in src/TradingBot.Web/Pages/RiskSettings.razor
- [ ] T087 [US5] Implement form validation: leverage (1.0-10.0), stop-loss (0.1-50.0%), take-profit (0.1-100.0%), max position size (1.0-100.0%) in RiskSettingsForm.razor in src/TradingBot.Web/Components/Risk/RiskSettingsForm.razor
- [ ] T088 [US5] Add client-side validation with DataAnnotations and EditForm in RiskSettingsForm.razor in src/TradingBot.Web/Components/Risk/RiskSettingsForm.razor
- [ ] T089 [US5] Implement "Save" button handler calling RiskSettingsService.UpdateSettingsAsync in RiskSettings.razor in src/TradingBot.Web/Pages/RiskSettings.razor
- [ ] T090 [US5] Add validation error messages for each field in RiskSettingsForm.razor in src/TradingBot.Web/Components/Risk/RiskSettingsForm.razor
- [ ] T091 [US5] Add success notification when settings saved successfully in RiskSettings.razor in src/TradingBot.Web/Pages/RiskSettings.razor
- [ ] T092 [US5] Add error handling for validation failures and save errors in RiskSettings.razor in src/TradingBot.Web/Pages/RiskSettings.razor
- [ ] T093 [US5] Add visual indicator for current risk status (enabled = green, disabled = red) in RiskStatusIndicator.razor in src/TradingBot.Web/Components/Risk/RiskStatusIndicator.razor
- [ ] T094 [US5] Implement dashboard update via SignalR when risk settings changed in RealtimeUpdateService in src/TradingBot.Web/Services/RealtimeUpdateService.cs

**Checkpoint**: User Story 5 complete - risk settings can be modified through web interface with full validation

---

## Phase 8: User Story 6 - Backtesting Results Viewer (Priority: P4)

**Goal**: View backtesting results through the web interface with rich visualizations

**Independent Test**: Run a backtest (via CLI or API) and view the results through the web interface with performance charts and trade details

### Implementation for User Story 6

- [ ] T095 [P] [US6] Create Backtest.razor page with backtest results list in src/TradingBot.Web/Pages/Backtest.razor
- [ ] T096 [P] [US6] Create BacktestResultsList.razor component displaying all backtest runs with strategy, symbol, date range, return in src/TradingBot.Web/Components/Backtest/BacktestResultsList.razor
- [ ] T097 [P] [US6] Create BacktestDetail.razor component showing detailed backtest information, performance summary, and trading statistics in src/TradingBot.Web/Components/Backtest/BacktestDetail.razor
- [ ] T098 [P] [US6] Create BacktestTradesList.razor component displaying top 5 winning and top 5 losing trades in src/TradingBot.Web/Components/Backtest/BacktestTradesList.razor
- [ ] T099 [P] [US6] Create BacktestEquityCurveChart.razor component using ApexCharts to display backtest equity curve in src/TradingBot.Web/Components/Charts/BacktestEquityCurveChart.razor
- [ ] T100 [US6] Integrate BacktestService.GetBacktestResultsAsync in Backtest.razor to load backtest list in src/TradingBot.Web/Pages/Backtest.razor
- [ ] T101 [US6] Implement backtest selection logic calling BacktestService.GetBacktestByIdAsync for detailed view in Backtest.razor in src/TradingBot.Web/Pages/Backtest.razor
- [ ] T102 [US6] Add performance metrics display (initial capital, final equity, total P&L, return %) in BacktestDetail.razor in src/TradingBot.Web/Components/Backtest/BacktestDetail.razor
- [ ] T103 [US6] Configure BacktestEquityCurveChart.razor with backtest equity curve data in src/TradingBot.Web/Components/Charts/BacktestEquityCurveChart.razor
- [ ] T104 [US6] Implement backtest comparison table for multiple selected results in Backtest.razor in src/TradingBot.Web/Pages/Backtest.razor
- [ ] T105 [US6] Implement empty state handling: "No backtest results available" when no backtests exist in Backtest.razor in src/TradingBot.Web/Pages/Backtest.razor

**Checkpoint**: All six user stories complete - full web application functionality implemented

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T106 [P] Add loading spinners during data fetching in all pages (Dashboard, Portfolio, Performance, Strategies, RiskSettings, Backtest)
- [ ] T107 [P] Add error boundary components with user-friendly error messages for unexpected errors in all pages
- [ ] T108 [P] Implement toast notifications service for success/error/warning messages across all components
- [ ] T109 [P] Add keyboard navigation support for accessibility (tab order, focus indicators) in all interactive components
- [ ] T110 [P] Add ARIA labels and roles for screen reader support in all components
- [ ] T111 Optimize SignalR update throttling to prevent excessive re-renders (debounce updates) in RealtimeUpdateService in src/TradingBot.Web/Services/RealtimeUpdateService.cs
- [ ] T112 Add logging with Serilog for all service operations (info, warning, error levels) in all service classes in src/TradingBot.Web/Services/
- [ ] T113 Add structured logging with correlation IDs for request tracking in Program.cs in src/TradingBot.Web/Program.cs
- [ ] T114 Run CSS production build (npm run css:prod) and verify minified output < 20KB in src/TradingBot.Web/
- [ ] T115 Run quickstart.md validation: verify setup instructions, verify all commands work, verify database connection in /Users/phmatray/Repositories/github-phm/TradingBot/specs/002-blazor-server-app/quickstart.md
- [ ] T116 Add copyright file headers to all .cs files in src/TradingBot.Web/ and tests/TradingBot.Web.Tests/
- [ ] T117 Run code quality analysis: StyleCop, Roslynator, and ensure no build warnings in src/TradingBot.Web/
- [ ] T118 Verify nullable reference types enabled and null checks in place across all service classes in src/TradingBot.Web/Services/
- [ ] T119 Test dashboard load time < 2s and API responses < 200ms p95 with sample data
- [ ] T120 Test SignalR reconnection behavior: disconnect network, verify automatic reconnect, verify data refresh after reconnect

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-8)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3 → P4)
- **Polish (Phase 9)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Independently testable, integrates with US1 for position closure
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Independently testable
- **User Story 4 (P3)**: Can start after Foundational (Phase 2) - Independently testable
- **User Story 5 (P3)**: Can start after Foundational (Phase 2) - Independently testable
- **User Story 6 (P4)**: Can start after Foundational (Phase 2) - Independently testable

### Within Each User Story

- Components marked [P] within the same user story can be developed in parallel (different files)
- Page integration tasks depend on component completion
- SignalR integration depends on page and component completion
- Empty state handling and error handling are final polish for each story

### Parallel Opportunities

- **Phase 1 (Setup)**: Tasks T003, T004, T005, T006, T015 can run in parallel (different configuration files/packages)
- **Phase 2 (Foundational)**: Tasks T016-T018 can run in parallel (different model/interface files), then services T020-T026 can run in parallel after interfaces complete
- **User Story 1**: Tasks T033-T038 (all dashboard components) can run in parallel
- **User Story 2**: Tasks T049-T053 (all portfolio components) can run in parallel
- **User Story 3**: Tasks T063-T066 (all performance components) can run in parallel
- **User Story 4**: Tasks T073-T075 (all strategy components) can run in parallel
- **User Story 5**: Tasks T083-T085 (all risk settings components) can run in parallel
- **User Story 6**: Tasks T095-T099 (all backtest components) can run in parallel
- **Phase 9 (Polish)**: Tasks T106-T110 (cross-cutting UI improvements) can run in parallel
- Once Foundational phase completes, **all 6 user stories can be worked on in parallel** by different team members

---

## Parallel Example: User Story 1 (Dashboard)

```bash
# Launch all dashboard components together (different files, no dependencies):
Task T033: "Create AccountSummary.razor component"
Task T034: "Create PositionList.razor component"
Task T035: "Create RecentTrades.razor component"
Task T036: "Create PerformanceMetrics.razor component"
Task T037: "Create RiskSettingsCard.razor component"
Task T038: "Create ActiveStrategies.razor component"

# Then integrate in Index.razor (depends on all above components)
Task T039: "Create Index.razor (Dashboard page)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T015)
2. Complete Phase 2: Foundational (T016-T032) - CRITICAL, blocks all stories
3. Complete Phase 3: User Story 1 (T033-T048) - Real-Time Dashboard
4. **STOP and VALIDATE**: Test User Story 1 independently - verify dashboard displays all data with real-time updates
5. Deploy/demo if ready - **Minimum viable product delivered!**

### Incremental Delivery (Recommended)

1. Setup + Foundational → Foundation ready (32 tasks)
2. Add User Story 1 (Dashboard) → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 (Portfolio) → Test independently → Deploy/Demo
4. Add User Story 3 (Performance) → Test independently → Deploy/Demo
5. Add User Story 4 (Strategies) → Test independently → Deploy/Demo
6. Add User Story 5 (Risk Settings) → Test independently → Deploy/Demo
7. Add User Story 6 (Backtest Viewer) → Test independently → Deploy/Demo
8. Polish (Phase 9) → Final release

Each story adds value without breaking previous stories. After User Story 1, you have a functional trading dashboard!

### Parallel Team Strategy

With multiple developers:

1. **All team members** complete Setup + Foundational together (critical path)
2. Once Foundational is done:
   - **Developer A**: User Story 1 (Dashboard) - P1
   - **Developer B**: User Story 2 (Portfolio) - P2
   - **Developer C**: User Story 3 (Performance) - P2
   - **Developer D**: User Story 4 (Strategies) - P3
   - **Developer E**: User Story 5 (Risk Settings) - P3
   - **Developer F**: User Story 6 (Backtest Viewer) - P4
3. Stories complete and integrate independently
4. Team reconvenes for Polish phase

---

## Notes

- **[P] tasks**: Different files, no dependencies - can run in parallel
- **[Story] label**: Maps task to specific user story for traceability (US1, US2, US3, US4, US5, US6)
- **Each user story** is independently completable and testable
- **No tests included**: Tests not requested in specification, focus is on implementation
- **Technology stack**: C# / .NET 9, ASP.NET Core Blazor Server, Tailwind CSS, SignalR, ApexCharts
- **Database**: Shared SQLite database with CLI application via TradingBot.Infrastructure
- **Reuse existing layers**: No changes to TradingBot.Core, Infrastructure, Engine, Analytics, Strategies
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Follow constitution requirements: 80% code coverage (tests phase), <200ms API p95, WCAG 2.1 Level AA accessibility