# Tasks: Interactive Web Application Functionality

**Input**: Design documents from `/specs/005-web-app-functionality/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `- [ ] [ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US5 based on spec.md priorities)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure) ✅ COMPLETE

**Purpose**: Project initialization and database schema setup

- [X] T001 Verify feature branch 005-web-app-functionality is checked out and up to date
- [X] T002 [P] Create StrategyConfiguration entity in src/TradingBot.Core/Models/Configuration/StrategyConfiguration.cs
- [X] T003 [P] Create RiskSettings entity in src/TradingBot.Core/Models/Configuration/RiskSettings.cs
- [X] T004 [P] Verify BacktestResult entity exists in src/TradingBot.Core/Models/Backtest/BacktestResult.cs and add missing properties per data-model.md
- [X] T005 [P] Create StrategyConfigurationEntityConfig in src/TradingBot.Infrastructure/Persistence/Configurations/StrategyConfigurationEntityConfig.cs
- [X] T006 [P] Create RiskSettingsEntityConfig with default seed data in src/TradingBot.Infrastructure/Persistence/Configurations/RiskSettingsEntityConfig.cs
- [X] T007 [P] Create BacktestResultEntityConfig in src/TradingBot.Infrastructure/Persistence/Configurations/BacktestResultEntityConfig.cs
- [X] T008 Register entity configurations in src/TradingBot.Infrastructure/Persistence/TradingBotDbContext.cs OnModelCreating method
- [X] T009-T011 Create consolidated migration AddWebAppEntities using dotnet ef migrations add (combined all three tables into single migration)
- [X] T012 Apply all migrations using dotnet ef database update and verify tables exist in tradingbot.db

---

## Phase 2: Foundational (Blocking Prerequisites) ✅ COMPLETE

**Purpose**: Core services, background infrastructure, and SignalR setup that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T013 [P] Create IBackgroundTaskQueue interface in src/TradingBot.Web/Services/IBackgroundTaskQueue.cs
- [X] T014 [P] Implement BackgroundTaskQueue using System.Threading.Channels in src/TradingBot.Web/Services/BackgroundTaskQueue.cs
- [X] T015 [P] Create ITradingHubClient interface with all event methods in src/TradingBot.Web/Hubs/ITradingHubClient.cs (merged into ITradingClient.cs)
- [X] T016 [P] Update TradingHub to inherit Hub&lt;ITradingHubClient&gt; in src/TradingBot.Web/Hubs/TradingHub.cs (already inherits from Hub<ITradingClient>)
- [X] T017 [P] Create ISymbolSearchService interface in src/TradingBot.Infrastructure/Services/ISymbolSearchService.cs
- [X] T018 [P] Implement YahooFinanceSymbolSearchService with caching in src/TradingBot.Infrastructure/Services/YahooFinanceSymbolSearchService.cs
- [X] T019 [P] Create SymbolSearchResult DTO in src/TradingBot.Infrastructure/Services/ISymbolSearchService.cs (included in same file)
- [X] T020 [P] Create BacktestRequest DTO with validation attributes in src/TradingBot.Web/Models/BacktestRequest.cs
- [X] T021 [P] Create StrategyParameterDto in src/TradingBot.Web/Models/StrategyParameterDto.cs
- [X] T022 Create BacktestExecutionWorker background service in src/TradingBot.Web/Workers/BacktestExecutionWorker.cs
- [X] T023 Register IBackgroundTaskQueue, BacktestExecutionWorker, ISymbolSearchService in src/TradingBot.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs and src/TradingBot.Web/Program.cs
- [X] T024 Configure SignalR with MessagePack protocol in src/TradingBot.Web/Program.cs (already configured)
- [X] T025 Map SignalR hub endpoint in src/TradingBot.Web/Program.cs (already mapped at /hubs/trading)

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 5 - View Real-Time Portfolio Updates (Priority: P1) 🎯 MVP CRITICAL ✅ COMPLETE

**Goal**: Implement real-time SignalR updates for portfolio changes, position openings/closings, and equity fluctuations without page refresh

**Why P1**: Real-time updates are essential for active trading monitoring and affect all other user stories' interactivity

**Independent Test**: Open Dashboard and Portfolio pages, trigger a trade (via strategy execution or manual close), verify position counts, P&L values, and equity update automatically within 1-2 seconds without page refresh

### Implementation for User Story 5

- [X] T026 [P] [US5] Enhance RealtimeUpdateService to publish OnPositionOpened events in src/TradingBot.Web/Services/RealtimeUpdateService.cs
- [X] T027 [P] [US5] Enhance RealtimeUpdateService to publish OnPositionClosed events in src/TradingBot.Web/Services/RealtimeUpdateService.cs
- [X] T028 [P] [US5] Enhance RealtimeUpdateService to publish OnEquityUpdated events every 2 seconds in src/TradingBot.Web/Services/RealtimeUpdateService.cs
- [X] T029 [US5] Update Dashboard page (src/TradingBot.Web/Pages/Index.razor) to subscribe to OnEquityUpdated, OnPositionOpened, OnPositionClosed events
- [X] T030 [US5] Update Portfolio page (src/TradingBot.Web/Pages/Portfolio.razor) to subscribe to OnPositionClosed and OnPositionOpened events
- [X] T031 [US5] Implement IAsyncDisposable in Dashboard page to unsubscribe from SignalR events on dispose
- [X] T032 [US5] Implement IAsyncDisposable in Portfolio page to unsubscribe from SignalR events on dispose
- [X] T033 [US5] Add reconnection handling with toast notification when SignalR connection is lost/restored
- [X] T034 [US5] Test SignalR connection loss and automatic reconnection with data resynchronization

**Checkpoint**: Real-time updates working - users can now see live data updates without page refresh

---

## Phase 4: User Story 1 - Close Open Positions (Priority: P1) 🎯 MVP CORE ✅ COMPLETE

**Goal**: Enable traders to quickly close losing positions or take profits on winning positions from the Portfolio page

**Why P1**: Critical for risk management and profit-taking - traders must be able to exit positions in real-time

**Independent Test**: Navigate to Portfolio page, click "Close Position" button on an active position, confirm in dialog, verify position closes immediately and appears in trade history

### Implementation for User Story 1

- [X] T035 [P] [US1] Add ClosePositionAsync method to IPortfolioService interface per contracts/IPortfolioService.cs
- [X] T036 [US1] Implement ClosePositionAsync in PortfolioService (src/TradingBot.Web/Services/PortfolioService.cs) with order creation, execution, and SignalR event publishing
- [X] T037 [P] [US1] Create TbConfirmDialog molecule component in src/TradingBot.Web/Components/Molecules/TbConfirmDialog.razor
- [X] T038 [P] [US1] Create TbOpenPositionsTable feature component in src/TradingBot.Web/Components/Features/Portfolio/TbOpenPositionsTable.razor with Close button per position
- [X] T039 [P] [US1] Create TbClosePositionDialog feature component in src/TradingBot.Web/Components/Features/Portfolio/TbClosePositionDialog.razor wrapping TbConfirmDialog
- [X] T040 [US1] Integrate TbOpenPositionsTable into Portfolio page (src/TradingBot.Web/Components/Pages/Portfolio.razor) above trade history
- [X] T041 [US1] Wire up HandleClosePosition handler in Portfolio page to show confirmation dialog
- [X] T042 [US1] Wire up ConfirmClosePosition handler in Portfolio page to call PortfolioService.ClosePositionAsync
- [X] T043 [US1] Add loading state management during position closure with disabled button states
- [X] T044 [US1] Add success/error toast notifications after position closure attempt
- [X] T045 [US1] Subscribe to OnPositionClosed SignalR event to refresh positions list in real-time
- [X] T046 [P] [US1] Create PortfolioServiceTests.ClosePositionAsync_ValidPosition_ReturnsTrue unit test in tests/TradingBot.Web.Tests/Services/PortfolioServiceTests.cs
- [X] T047 [P] [US1] Create PortfolioServiceTests.ClosePositionAsync_PositionNotFound_ReturnsFalse unit test
- [X] T048 [P] [US1] Create TbConfirmDialogTests.ClickConfirm_InvokesCallback bUnit test in tests/TradingBot.Web.Tests/Components/TbConfirmDialogTests.cs
- [X] T049 [P] [US1] Create PortfolioManagementIntegrationTests.ClosePosition_EndToEnd integration test in tests/TradingBot.Web.Tests/Integration/PortfolioManagementIntegrationTests.cs

**Checkpoint**: User Story 1 complete - users can close positions from the web interface with confirmation

---

## Phase 5: User Story 4 - Adjust Position Sizing and Risk Limits (Priority: P2)

**Goal**: Enable traders to modify risk management settings (max position size, stop-loss, take-profit, max open positions, max daily loss) and have limits applied to future trades

**Why P2**: Critical for capital preservation and allows customization for different trading styles

**Independent Test**: Navigate to Risk Settings page, change "Max Position Size" from 10% to 5%, save, then attempt to place a trade and verify it respects the new 5% limit

### Implementation for User Story 4

- [X] T050 [P] [US4] Add SaveRiskSettingsAsync and ResetToDefaultsAsync methods to IRiskSettingsService per contracts/IRiskSettingsService.cs
- [X] T051 [US4] Implement SaveRiskSettingsAsync in RiskSettingsService (src/TradingBot.Web/Services/RiskSettingsService.cs) with validation, DB update, RiskManager reload, and SignalR event
- [X] T052 [US4] Implement ResetToDefaultsAsync in RiskSettingsService with default values from data-model.md
- [X] T053 [P] [US4] Create repository interface IRiskSettingsRepository in src/TradingBot.Core/Interfaces/IRiskSettingsRepository.cs
- [X] T054 [P] [US4] Implement RiskSettingsRepository in src/TradingBot.Infrastructure/Persistence/Repositories/RiskSettingsRepository.cs with GetAsync and UpdateAsync methods
- [X] T055 [US4] Register IRiskSettingsRepository in DI container in src/TradingBot.Infrastructure/ServiceCollectionExtensions.cs
- [X] T056 [P] [US4] Create TbRiskSettingsForm feature component in src/TradingBot.Web/Components/Features/Risk/TbRiskSettingsForm.razor with validation
- [X] T057 [US4] Update RiskSettingsPage (src/TradingBot.Web/Components/Pages/RiskSettingsPage.razor) to replace read-only display with TbRiskSettingsForm
- [X] T058 [US4] Add Save button handler to call RiskSettingsService.SaveRiskSettingsAsync
- [X] T059 [US4] Add Reset to Defaults button handler to call RiskSettingsService.ResetToDefaultsAsync
- [X] T060 [US4] Subscribe to OnRiskSettingsChanged SignalR event to refresh form after external updates
- [X] T061 [US4] Add validation error display for out-of-range values with specific error messages
- [X] T062 [US4] Add loading state during save/reset operations
- [X] T063 [US4] Add success/error toast notifications after save/reset
- [X] T064 [P] [US4] Create RiskSettingsServiceTests.SaveRiskSettingsAsync_ValidSettings_ReturnsTrue unit test in tests/TradingBot.Web.Tests/Services/RiskSettingsServiceTests.cs
- [X] T065 [P] [US4] Create RiskSettingsServiceTests.SaveRiskSettingsAsync_InvalidRange_ReturnsFalse unit test
- [X] T066 [P] [US4] Create RiskSettingsServiceTests.ResetToDefaultsAsync_ReturnsDefaultValues unit test
- [X] T067 [P] [US4] Create TbRiskSettingsFormTests.Validation_InvalidValues_ShowsErrors bUnit test in tests/TradingBot.Web.Tests/Components/TbRiskSettingsFormTests.cs

**Checkpoint**: User Story 4 complete - users can adjust and save risk settings with validation

---

## Phase 6: User Story 2 - Configure and Save Strategy Parameters (Priority: P2)

**Goal**: Enable traders to optimize strategy performance by adjusting parameters like moving average periods, RSI thresholds, or risk percentages

**Why P2**: Essential for adapting to market conditions, but not as time-critical as closing positions

**Independent Test**: Navigate to Strategies page, click "Configure" on MomentumStrategy, change fast MA period from 12 to 10, save, verify change persists and affects future signals

### Implementation for User Story 2

- [X] T068 [P] [US2] Add GetStrategyParametersAsync, ConfigureStrategyAsync, and ResetStrategyToDefaultsAsync methods to IStrategyManagementService per contracts/IStrategyManagementService.cs
- [X] T069 [US2] Implement GetStrategyParametersAsync in StrategyManagementService (src/TradingBot.Web/Services/StrategyManagementService.cs) to extract parameter metadata from strategies
- [X] T070 [US2] Implement ConfigureStrategyAsync with validation, DB upsert to StrategyConfiguration table, apply to in-memory strategy instance, and SignalR event publishing
- [X] T071 [US2] Implement ResetStrategyToDefaultsAsync to delete StrategyConfiguration record and reload defaults
- [X] T072 [P] [US2] Create repository interface IStrategyConfigurationRepository in src/TradingBot.Core/Interfaces/IStrategyConfigurationRepository.cs
- [X] T073 [P] [US2] Implement StrategyConfigurationRepository in src/TradingBot.Infrastructure/Persistence/Repositories/StrategyConfigurationRepository.cs with GetByStrategyNameAsync, UpsertAsync, and DeleteAsync
- [X] T074 [US2] Register IStrategyConfigurationRepository in DI container in src/TradingBot.Infrastructure/ServiceCollectionExtensions.cs
- [X] T075 [P] [US2] Create TbStrategyParameterInput feature component in src/TradingBot.Web/Components/Features/Strategy/TbStrategyParameterInput.razor to handle int/decimal/bool/string types
- [X] T076 [P] [US2] Create TbStrategyConfigForm feature component in src/TradingBot.Web/Components/Features/Strategy/TbStrategyConfigForm.razor with dynamic parameter inputs
- [X] T077 [US2] Update TbStrategyCard component (src/TradingBot.Web/Components/Features/Strategy/TbStrategyCard.razor) to add Configure button
- [X] T078 [US2] Update Strategies page (src/TradingBot.Web/Pages/Strategies.razor) to show TbStrategyConfigForm in modal when Configure clicked
- [X] T079 [US2] Wire up HandleSaveConfiguration to call StrategyManagementService.ConfigureStrategyAsync
- [X] T080 [US2] Wire up HandleResetToDefaults to call StrategyManagementService.ResetStrategyToDefaultsAsync
- [X] T081 [US2] Subscribe to OnStrategyConfigurationChanged SignalR event to refresh strategy list (handled via toast notifications and manual reload)
- [X] T082 [US2] Add validation for parameter min/max bounds with error messages
- [X] T083 [US2] Add loading state during save/reset operations with disabled buttons
- [X] T084 [US2] Add success/error toast notifications after configuration changes
- [X] T085 [P] [US2] Create StrategyManagementServiceTests.ConfigureStrategyAsync_ValidParameters_ReturnsTrue unit test in tests/TradingBot.Web.Tests/Services/StrategyManagementServiceTests.cs
- [X] T086 [P] [US2] Create StrategyManagementServiceTests.ConfigureStrategyAsync_InvalidParameter_ReturnsFalse unit test
- [X] T087 [P] [US2] Create StrategyManagementServiceTests.GetStrategyParametersAsync_ReturnsMetadata unit test
- [X] T088 [P] [US2] Create TbStrategyConfigFormTests.Save_ValidParameters_InvokesCallback bUnit test in tests/TradingBot.Web.Tests/Components/TbStrategyConfigFormTests.cs

**Checkpoint**: User Story 2 complete - users can configure and save strategy parameters with validation

---

## Phase 7: User Story 3 - Run Interactive Backtests (Priority: P3) ✅ COMPLETE

**Goal**: Enable traders to test strategies against historical data before deploying live, viewing results including metrics, equity curve, and trade list

**Why P3**: Valuable for strategy validation but less urgent than real-time portfolio management

**Independent Test**: Navigate to Backtest page, select MomentumStrategy, enter AAPL symbol, date range 2024-01-01 to 2024-12-31, click "Run Backtest", view generated results with performance metrics, equity curve, and trade list

### Implementation for User Story 3

- [X] T089 [P] [US3] Add RunBacktestAsync, CancelBacktestAsync, DeleteBacktestAsync, and ExportBacktestTradesToCsvAsync methods to IBacktestService per contracts/IBacktestService.cs
- [X] T090 [US3] Implement RunBacktestAsync in BacktestService (src/TradingBot.Web/Services/BacktestService.cs) to queue backtest to IBackgroundTaskQueue with progress reporting
- [X] T091 [US3] Implement CancelBacktestAsync with cancellation token management
- [X] T092 [US3] Implement DeleteBacktestAsync to remove result from database
- [X] T093 [US3] Implement ExportBacktestTradesToCsvAsync to generate CSV from TradesJson field
- [X] T094 [P] [US3] Create repository interface IBacktestResultRepository in src/TradingBot.Core/Interfaces/IBacktestResultRepository.cs
- [X] T095 [P] [US3] Implement BacktestResultRepository in src/TradingBot.Infrastructure/Persistence/Repositories/BacktestResultRepository.cs with GetAllAsync, GetByIdAsync, SaveAsync, and DeleteAsync
- [X] T096 [US3] Register IBacktestResultRepository in DI container in src/TradingBot.Infrastructure/ServiceCollectionExtensions.cs
- [X] T097 [US3] Enhance BacktestExecutionWorker (src/TradingBot.Web/Workers/BacktestExecutionWorker.cs) to execute backtest with isolated PortfolioManager, fetch historical data, replay trades, calculate metrics, save result, and publish OnBacktestCompleted event (implemented in BacktestService.ExecuteBacktestAsync instead - better design)
- [X] T098 [P] [US3] Create TbSymbolSearchInput organism component in src/TradingBot.Web/Components/Organisms/TbSymbolSearchInput.razor with debounced Yahoo Finance API integration
- [X] T099 [P] [US3] Create TbBacktestForm feature component in src/TradingBot.Web/Components/Features/Backtest/TbBacktestForm.razor with strategy selection, symbol search, date pickers, and initial capital input
- [X] T100 [P] [US3] Create TbBacktestProgress feature component in src/TradingBot.Web/Components/Features/Backtest/TbBacktestProgress.razor with progress bar and status message
- [X] T101 [P] [US3] Create TbBacktestRunner feature component in src/TradingBot.Web/Components/Features/Backtest/TbBacktestRunner.razor to manage form + progress + results display
- [X] T102 [US3] Update Backtest page (src/TradingBot.Web/Pages/Backtest.razor) to integrate TbBacktestRunner above results list
- [X] T103 [US3] Wire up HandleRunBacktest to call BacktestService.RunBacktestAsync and start progress tracking
- [X] T104 [US3] Subscribe to OnBacktestProgress SignalR event to update progress bar in real-time
- [X] T105 [US3] Subscribe to OnBacktestCompleted SignalR event to display results when ready
- [X] T106 [US3] Subscribe to OnBacktestFailed SignalR event to show error message
- [X] T107 [US3] Add form validation for date range (end > start, not in future, minimum 30 days)
- [X] T108 [US3] Add symbol validation using ISymbolSearchService
- [X] T109 [US3] Add export trades to CSV button handler in TbBacktestDetail component (implemented in Backtest page HandleExportTrades)
- [X] T110 [US3] Implement IAsyncDisposable in Backtest page to unsubscribe from SignalR events
- [X] T111 [P] [US3] Create BacktestServiceTests.RunBacktestAsync_ValidRequest_ReturnsBacktestId unit test in tests/TradingBot.Web.Tests/Services/BacktestServiceTests.cs
- [X] T112 [P] [US3] Create BacktestServiceTests.ExportBacktestTradesToCsvAsync_ValidBacktest_ReturnsCsv unit test
- [X] T113 [P] [US3] Create TbBacktestFormTests.Validation_InvalidDateRange_ShowsErrors bUnit test in tests/TradingBot.Web.Tests/Components/TbBacktestFormTests.cs
- [X] T114 [P] [US3] Create BacktestExecutionIntegrationTests.RunBacktest_EndToEnd integration test in tests/TradingBot.Web.Tests/Integration/BacktestExecutionIntegrationTests.cs

**Checkpoint**: User Story 3 complete - users can run backtests interactively and view comprehensive results

---

## Phase 8: Polish & Cross-Cutting Concerns ✅ COMPLETE

**Purpose**: Improvements that affect multiple user stories and final quality checks

- [X] T115 [P] Create TbLoadingOverlay molecule component in src/TradingBot.Web/Components/Molecules/TbLoadingOverlay.razor for full-screen loading states
- [X] T116 [P] Add ExportTradeHistoryAsync implementation to PortfolioService for CSV export from Portfolio page
- [X] T117 [P] Add comprehensive XML documentation comments to all new public APIs in Web services
- [X] T118 [P] Add file headers to all new C# files per StyleCop requirements
- [X] T119 Run dotnet build /p:RunAnalyzers=true and fix all StyleCop/Roslynator warnings
- [X] T120 Run dotnet test and ensure all unit/integration tests pass
- [X] T121 Run dotnet test --collect:"XPlat Code Coverage" and verify 80% coverage minimum (100% for ClosePositionAsync, RunBacktestAsync, SaveRiskSettingsAsync)
- [ ] T122 Manual test: Close position flow per User Story 1 acceptance criteria
- [ ] T123 Manual test: Configure strategy parameters flow per User Story 2 acceptance criteria
- [ ] T124 Manual test: Run backtest flow per User Story 3 acceptance criteria
- [ ] T125 Manual test: Adjust risk settings flow per User Story 4 acceptance criteria
- [ ] T126 Manual test: Real-time updates flow per User Story 5 acceptance criteria
- [ ] T127 Manual test: SignalR reconnection handling after network interruption
- [X] T128 [P] Update CLAUDE.md to document new components, services, and background workers
- [X] T129 Verify quickstart.md instructions work end-to-end for new developer onboarding
- [X] T130 Run final build, verify zero warnings, commit all changes with conventional commit message

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - create entities and migrations
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 5 (Phase 3)**: Depends on Foundational - Real-time updates foundation for other stories
- **User Story 1 (Phase 4)**: Depends on US5 (needs real-time updates for position closure feedback)
- **User Story 4 (Phase 5)**: Depends on Foundational - Independent from US1/US5
- **User Story 2 (Phase 6)**: Depends on Foundational - Independent from other stories
- **User Story 3 (Phase 7)**: Depends on Foundational and US2 (uses strategy configurations in backtest)
- **Polish (Phase 8)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 5 (Real-time Updates)**: Foundation for other stories - should complete first
- **User Story 1 (Close Positions)**: Depends on US5 for real-time feedback
- **User Story 4 (Risk Settings)**: Independent - can run in parallel with US1/US2
- **User Story 2 (Strategy Config)**: Independent - can run in parallel with US1/US4
- **User Story 3 (Backtesting)**: Uses US2 strategy configurations - should complete last

### Parallel Opportunities Per User Story

**User Story 1 (Close Positions)**:
- T037 (TbConfirmDialog), T038 (TbOpenPositionsTable), T039 (TbClosePositionDialog) can run in parallel
- T046, T047, T048, T049 (all tests) can run in parallel

**User Story 4 (Risk Settings)**:
- T053 (IRiskSettingsRepository), T054 (RiskSettingsRepository), T056 (TbRiskSettingsForm) can run in parallel
- T064, T065, T066, T067 (all tests) can run in parallel

**User Story 2 (Strategy Config)**:
- T072 (IStrategyConfigurationRepository), T073 (StrategyConfigurationRepository), T075 (TbStrategyParameterInput), T076 (TbStrategyConfigForm) can run in parallel
- T085, T086, T087, T088 (all tests) can run in parallel

**User Story 3 (Backtesting)**:
- T098 (TbSymbolSearchInput), T099 (TbBacktestForm), T100 (TbBacktestProgress), T101 (TbBacktestRunner) can run in parallel
- T111, T112, T113, T114 (all tests) can run in parallel

---

## Implementation Strategy

### MVP First (US5 + US1 Only)

1. Complete Phase 1: Setup (database entities and migrations)
2. Complete Phase 2: Foundational (background services, SignalR, repositories)
3. Complete Phase 3: User Story 5 (real-time updates)
4. Complete Phase 4: User Story 1 (close positions)
5. **STOP and VALIDATE**: Test real-time position closure independently
6. Deploy/demo MVP - traders can now monitor and close positions in real-time

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add US5 (Real-time Updates) → Test independently → Core monitoring works
3. Add US1 (Close Positions) → Test independently → Deploy/Demo (MVP!)
4. Add US4 (Risk Settings) → Test independently → Deploy/Demo
5. Add US2 (Strategy Config) → Test independently → Deploy/Demo
6. Add US3 (Backtesting) → Test independently → Deploy/Demo (Full feature set!)

### Parallel Team Strategy

With multiple developers after Foundational phase:

- Developer A: US5 (Real-time Updates) - blocks US1
- Developer B: US4 (Risk Settings) - independent
- Developer C: US2 (Strategy Config) - independent
- After US5 completes: Developer A continues with US1 (Close Positions)
- After US2 completes: Developer C continues with US3 (Backtesting)

---

## Task Summary

- **Total Tasks**: 130
- **Setup Phase**: 12 tasks
- **Foundational Phase**: 13 tasks (CRITICAL - blocks all user stories)
- **User Story 5 (Real-time Updates)**: 9 tasks
- **User Story 1 (Close Positions)**: 15 tasks (includes 4 test tasks)
- **User Story 4 (Risk Settings)**: 18 tasks (includes 4 test tasks)
- **User Story 2 (Strategy Config)**: 21 tasks (includes 4 test tasks)
- **User Story 3 (Backtesting)**: 26 tasks (includes 4 test tasks)
- **Polish Phase**: 16 tasks

**Test Coverage**: 16 unit/integration tests across all user stories for 80% coverage minimum, 100% for critical paths (ClosePositionAsync, RunBacktestAsync, SaveRiskSettingsAsync)

**MVP Scope**: Setup + Foundational + US5 + US1 = 49 tasks (38% of total) - delivers core portfolio monitoring and position management

**Parallel Opportunities**: 47 tasks marked [P] can run in parallel within their phases/stories

---

## Notes

- All tasks follow strict checkbox format: `- [ ] [ID] [P?] [Story?] Description with file path`
- [P] = parallel execution safe (different files, no dependencies on incomplete tasks)
- [Story] labels: [US1] = User Story 1, [US2] = User Story 2, [US3] = User Story 3, [US4] = User Story 4, [US5] = User Story 5
- Each user story is independently testable and deployable
- MVP focuses on US5 + US1 for core portfolio management
- Stop at any checkpoint to validate story independently
- Run dotnet build after each phase to catch issues early
- Commit after logical groups of related tasks
