# Tasks: Weekly Cash-Managed Trading Strategy

**Input**: Design documents from `/specs/007-weekly-cash-managed-strategy/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Tests ARE included as this is a critical trading strategy requiring 100% coverage for buy/sell/cash buffer logic.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `- [ ] [ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Multi-project layered architecture:
- **Core**: `src/TradingBot.Core/`
- **Infrastructure**: `src/TradingBot.Infrastructure/`
- **Engine**: `src/TradingBot.Engine/`
- **Web**: `src/TradingBot.Web/`
- **Tests**: `tests/TradingBot.{Layer}.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, database migration, and NuGet package setup

- [ ] T001 Add NCrontab NuGet package to Directory.Packages.props if not already present
- [ ] T002 Register WeeklyCashManagedStrategy services in src/TradingBot.Infrastructure/ServiceCollectionExtensions.cs
- [ ] T003 [P] Add copyright headers template to all new files per code quality standards

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain entities, value objects, domain events, and database schema that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Domain Model (Core Layer)

- [ ] T004 [P] Create StrategyConfiguration value object in src/TradingBot.Core/ValueObjects/StrategyConfiguration.cs
- [ ] T005 [P] Create BreakoutRuleConfig value object in src/TradingBot.Core/ValueObjects/BreakoutRuleConfig.cs
- [ ] T006 Create WeeklyCashManagedStrategy aggregate root in src/TradingBot.Core/Entities/WeeklyCashManagedStrategy.cs

### Domain Events (Core Layer)

- [ ] T007 [P] Create StrategyEnabledEvent in src/TradingBot.Core/Events/StrategyEnabledEvent.cs
- [ ] T008 [P] Create StrategyDisabledEvent in src/TradingBot.Core/Events/StrategyDisabledEvent.cs
- [ ] T009 [P] Create StrategyExecutedEvent in src/TradingBot.Core/Events/StrategyExecutedEvent.cs
- [ ] T010 [P] Create MA20UpdatedEvent in src/TradingBot.Core/Events/MA20UpdatedEvent.cs
- [ ] T011 [P] Create StrategyConfigurationUpdatedEvent in src/TradingBot.Core/Events/StrategyConfigurationUpdatedEvent.cs
- [ ] T012 [P] Create CashBufferAdjustedEvent in src/TradingBot.Core/Events/CashBufferAdjustedEvent.cs

### Repository Interfaces (Core Layer)

- [ ] T013 [P] Create IWeeklyCashManagedStrategyRepository interface in src/TradingBot.Core/Interfaces/IWeeklyCashManagedStrategyRepository.cs
- [ ] T014 [P] Create IMA20IndicatorService interface in src/TradingBot.Core/Interfaces/IMA20IndicatorService.cs
- [ ] T015 [P] Create IWeeklyRoutineExecutor interface in src/TradingBot.Core/Interfaces/IWeeklyRoutineExecutor.cs
- [ ] T016 [P] Create ICashBufferManager interface in src/TradingBot.Core/Interfaces/ICashBufferManager.cs
- [ ] T017 [P] Create IBreakoutDetector interface in src/TradingBot.Core/Interfaces/IBreakoutDetector.cs

### Database Configuration (Infrastructure Layer)

- [ ] T018 Create WeeklyCashManagedStrategyConfiguration EF Core configuration in src/TradingBot.Infrastructure/Persistence/Configurations/WeeklyCashManagedStrategyConfiguration.cs
- [ ] T019 Add DbSet<WeeklyCashManagedStrategy> to TradingBotDbContext in src/TradingBot.Infrastructure/Persistence/TradingBotDbContext.cs
- [ ] T020 Create EF Core migration AddWeeklyCashManagedStrategy using dotnet ef migrations add
- [ ] T021 Review and apply migration to database using dotnet ef database update

### Repository Implementation (Infrastructure Layer)

- [ ] T022 Create WeeklyCashManagedStrategyRepository implementation in src/TradingBot.Infrastructure/Persistence/Repositories/WeeklyCashManagedStrategyRepository.cs

### MA20 Indicator Service (Infrastructure Layer)

- [ ] T023 Create MA20IndicatorService with sliding window algorithm in src/TradingBot.Infrastructure/Services/MA20IndicatorService.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Configure and Enable Weekly Cash-Managed Strategy (Priority: P1) 🎯 MVP

**Goal**: Allow traders to configure and activate the strategy with customizable parameters (cash ratios, buy/sell ratios, symbol mappings) and verify configuration is saved and visible in dashboard

**Independent Test**: Navigate to strategy configuration page, enter valid parameters (MIN_CASH_RATIO=0.15, MAX_CASH_RATIO=0.25, WEEKLY_BUY_RATIO=0.05, WEEKLY_SELL_RATIO=0.10, ETP="BTCW", underlying="COIN"), enable strategy, verify it appears in active strategies list

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T024 [P] [US1] Unit test for StrategyConfiguration validation in tests/TradingBot.Core.Tests/ValueObjects/StrategyConfigurationTests.cs
- [ ] T025 [P] [US1] Unit test for BreakoutRuleConfig validation in tests/TradingBot.Core.Tests/ValueObjects/BreakoutRuleConfigTests.cs
- [ ] T026 [P] [US1] Unit test for WeeklyCashManagedStrategy.Enable domain behavior in tests/TradingBot.Core.Tests/Entities/WeeklyCashManagedStrategyTests.cs
- [ ] T027 [P] [US1] Unit test for WeeklyCashManagedStrategy.Disable domain behavior in tests/TradingBot.Core.Tests/Entities/WeeklyCashManagedStrategyTests.cs
- [ ] T028 [P] [US1] Unit test for WeeklyCashManagedStrategy.UpdateConfiguration domain behavior in tests/TradingBot.Core.Tests/Entities/WeeklyCashManagedStrategyTests.cs
- [ ] T029 [P] [US1] Repository persistence test for WeeklyCashManagedStrategy in tests/TradingBot.Infrastructure.Tests/Repositories/WeeklyCashManagedStrategyRepositoryTests.cs
- [ ] T030 [P] [US1] Blazor component test for StrategyConfigurationForm using bUnit in tests/TradingBot.Web.Tests/Components/WeeklyCashStrategy/StrategyConfigurationFormTests.cs

### Implementation for User Story 1

**DTOs and Web Services**

- [ ] T031 [P] [US1] Create StrategyConfigurationDto with validation attributes in src/TradingBot.Web/Models/StrategyConfigurationDto.cs
- [ ] T032 [P] [US1] Create StrategyStateDto for real-time updates in src/TradingBot.Web/Models/StrategyStateDto.cs
- [ ] T033 [US1] Create WeeklyCashStrategyService (Scoped) for configuration operations in src/TradingBot.Web/Services/WeeklyCashStrategyService.cs
- [ ] T034 [US1] Register WeeklyCashStrategyService in DI container in src/TradingBot.Web/Program.cs

**Blazor Components**

- [ ] T035 [US1] Create StrategyConfigurationForm.razor component in src/TradingBot.Web/Components/Features/WeeklyCashStrategy/StrategyConfigurationForm.razor
- [ ] T036 [US1] Create StrategyConfigurationForm.razor.cs code-behind with form logic in src/TradingBot.Web/Components/Features/WeeklyCashStrategy/StrategyConfigurationForm.razor.cs
- [ ] T037 [US1] Add strategy configuration page route in src/TradingBot.Web/Components/Pages/WeeklyCashStrategyConfig.razor

**Integration and Validation**

- [ ] T038 [US1] Add validation for MIN_CASH_RATIO < MAX_CASH_RATIO in StrategyConfiguration value object
- [ ] T039 [US1] Add validation for ratios in [0,1] range in StrategyConfiguration value object
- [ ] T040 [US1] Test configuration save and enable flow end-to-end
- [ ] T041 [US1] Verify domain events (StrategyEnabledEvent, StrategyConfigurationUpdatedEvent) are raised

**Checkpoint**: At this point, User Story 1 should be fully functional - users can configure and enable strategies via web UI

---

## Phase 4: User Story 2 - Automated Weekly Buy Execution Based on MA20 Trend (Priority: P2)

**Goal**: Automatically purchase ETP shares when underlying asset is above MA20 and sufficient cash buffer exists, executing on weekly schedule

**Independent Test**: Set up strategy with known parameters, mock data where COIN > MA20, ensure cash_ratio > MIN_CASH_RATIO, trigger weekly routine (simulate Friday), verify correct buy amount calculated and market order executed

### Tests for User Story 2

- [ ] T042 [P] [US2] Unit test for MA20 calculation accuracy in tests/TradingBot.Infrastructure.Tests/Services/MA20IndicatorServiceTests.cs
- [ ] T043 [P] [US2] Unit test for MA20 sliding window update (O(1) performance) in tests/TradingBot.Infrastructure.Tests/Services/MA20IndicatorServiceTests.cs
- [ ] T044 [P] [US2] Unit test for MA20 gap handling (weekends/holidays) in tests/TradingBot.Infrastructure.Tests/Services/MA20IndicatorServiceTests.cs
- [ ] T045 [P] [US2] Unit test for WeeklyRoutineExecutor buy logic when COIN > MA20 in tests/TradingBot.Engine.Tests/WeeklyRoutine/WeeklyRoutineExecutorTests.cs
- [ ] T046 [P] [US2] Unit test for buy amount calculation (5% of equity) in tests/TradingBot.Engine.Tests/WeeklyRoutine/WeeklyRoutineExecutorTests.cs
- [ ] T047 [P] [US2] Unit test for buy logic when cash_ratio <= MIN_CASH_RATIO (no buy) in tests/TradingBot.Engine.Tests/WeeklyRoutine/WeeklyRoutineExecutorTests.cs
- [ ] T048 [P] [US2] Unit test for buy logic when insufficient cash (buy only available amount) in tests/TradingBot.Engine.Tests/WeeklyRoutine/WeeklyRoutineExecutorTests.cs

### Implementation for User Story 2

**Engine Layer - Weekly Routine Executor**

- [ ] T049 [US2] Create WeeklyRoutineExecutor class in src/TradingBot.Engine/WeeklyRoutine/WeeklyRoutineExecutor.cs
- [ ] T050 [US2] Implement ExecuteWeeklyRoutineAsync method with buy/sell orchestration in WeeklyRoutineExecutor
- [ ] T051 [US2] Implement ShouldExecuteBuyAsync method (checks COIN > MA20, cash_ratio > MIN) in WeeklyRoutineExecutor
- [ ] T052 [US2] Implement CalculateBuyAmountAsync method (min(WEEKLY_BUY_RATIO × equity, cash)) in WeeklyRoutineExecutor
- [ ] T053 [US2] Integrate with OrderExecutionService for buy order execution in WeeklyRoutineExecutor
- [ ] T054 [US2] Integrate with RiskManager for order validation in WeeklyRoutineExecutor
- [ ] T055 [US2] Add structured logging for buy decisions in WeeklyRoutineExecutor

**Background Worker for Scheduling**

- [ ] T056 [US2] Create WeeklyRoutineWorker BackgroundService in src/TradingBot.Web/BackgroundWorkers/WeeklyRoutineWorker.cs
- [ ] T057 [US2] Implement ExecuteAsync with PeriodicTimer (24-hour interval) in WeeklyRoutineWorker
- [ ] T058 [US2] Add NCrontab schedule parsing for weekly execution day in WeeklyRoutineWorker
- [ ] T059 [US2] Create ITradingCalendar service interface in src/TradingBot.Core/Interfaces/ITradingCalendar.cs
- [ ] T060 [US2] Implement TradingCalendar service (checks market hours/holidays) in src/TradingBot.Infrastructure/Services/TradingCalendar.cs
- [ ] T061 [US2] Register WeeklyRoutineWorker as hosted service in src/TradingBot.Web/Program.cs

**Daily Routine for MA20 Updates**

- [ ] T062 [US2] Implement ExecuteDailyRoutineAsync method in WeeklyRoutineExecutor (updates MA20, prices, days_below_ma20)
- [ ] T063 [US2] Add daily routine scheduling to WeeklyRoutineWorker (runs every day at market close)
- [ ] T064 [US2] Implement UpdateDailyData domain method in WeeklyCashManagedStrategy entity

**Integration and Validation**

- [ ] T065 [US2] Test MA20 calculation with real Yahoo Finance data (verify 0.01% accuracy)
- [ ] T066 [US2] Test weekly buy execution end-to-end with mocked dependencies
- [ ] T067 [US2] Verify domain event StrategyExecutedEvent is raised with buy order details
- [ ] T068 [US2] Test scenario: COIN > MA20, cash_ratio = 20%, verify 5% buy amount
- [ ] T069 [US2] Test scenario: COIN > MA20, cash_ratio = 15% (minimum), verify no buy
- [ ] T070 [US2] Test scenario: Mid-week, verify no buy execution (weekly schedule only)

**Checkpoint**: At this point, User Story 2 should be fully functional - automated weekly buying when conditions met

---

## Phase 5: User Story 3 - Automated Weekly Sell Execution Based on MA20 Breakdown (Priority: P2)

**Goal**: Automatically sell portion of ETP holdings when underlying asset stays below MA20 for 2+ consecutive days, executing on weekly schedule

**Independent Test**: Set up strategy with existing position, mock daily data where COIN < MA20 for 2+ consecutive days, trigger weekly routine on Friday, verify correct sell amount (10% of position) and market sell order executed

### Tests for User Story 3

- [ ] T071 [P] [US3] Unit test for days_below_ma20 counter increment logic in tests/TradingBot.Core.Tests/Entities/WeeklyCashManagedStrategyTests.cs
- [ ] T072 [P] [US3] Unit test for days_below_ma20 counter reset when COIN >= MA20 in tests/TradingBot.Core.Tests/Entities/WeeklyCashManagedStrategyTests.cs
- [ ] T073 [P] [US3] Unit test for WeeklyRoutineExecutor sell logic when days_below_ma20 >= 2 in tests/TradingBot.Engine.Tests/WeeklyRoutine/WeeklyRoutineExecutorTests.cs
- [ ] T074 [P] [US3] Unit test for sell quantity calculation (10% of position) in tests/TradingBot.Engine.Tests/WeeklyRoutine/WeeklyRoutineExecutorTests.cs
- [ ] T075 [P] [US3] Unit test for sell logic when days_below_ma20 = 1 (no sell, threshold not met) in tests/TradingBot.Engine.Tests/WeeklyRoutine/WeeklyRoutineExecutorTests.cs
- [ ] T076 [P] [US3] Unit test for sell logic when COIN crosses back above MA20 (counter resets, no sell) in tests/TradingBot.Engine.Tests/WeeklyRoutine/WeeklyRoutineExecutorTests.cs

### Implementation for User Story 3

**Engine Layer - Sell Logic**

- [ ] T077 [US3] Implement ShouldExecuteSellAsync method (checks days_below_ma20 >= 2, position > 0) in WeeklyRoutineExecutor
- [ ] T078 [US3] Implement CalculateSellQuantityAsync method (WEEKLY_SELL_RATIO × position_size) in WeeklyRoutineExecutor
- [ ] T079 [US3] Integrate sell logic into ExecuteWeeklyRoutineAsync method in WeeklyRoutineExecutor
- [ ] T080 [US3] Integrate with OrderExecutionService for sell order execution in WeeklyRoutineExecutor
- [ ] T081 [US3] Add structured logging for sell decisions in WeeklyRoutineExecutor

**Daily Counter Management**

- [ ] T082 [US3] Update ExecuteDailyRoutineAsync to increment days_below_ma20 when COIN < MA20
- [ ] T083 [US3] Update ExecuteDailyRoutineAsync to reset days_below_ma20 to 0 when COIN >= MA20
- [ ] T084 [US3] Raise MA20UpdatedEvent with updated days_below_ma20 value

**Integration and Validation**

- [ ] T085 [US3] Test sell execution end-to-end with mocked dependencies
- [ ] T086 [US3] Verify domain event StrategyExecutedEvent is raised with sell order details
- [ ] T087 [US3] Test scenario: days_below_ma20 = 2, position = 100 shares, verify 10 shares sell
- [ ] T088 [US3] Test scenario: days_below_ma20 = 1, verify no sell (threshold not met)
- [ ] T089 [US3] Test scenario: COIN below MA20 for 2 days then crosses above, verify counter reset and no sell
- [ ] T090 [US3] Test scenario: Mid-week, days_below_ma20 increments but no sell order (weekly schedule only)

**Checkpoint**: At this point, User Stories 1, 2, AND 3 should all work independently - full buy/sell automation operational

---

## Phase 6: User Story 4 - Automated Cash Buffer Rebalancing (Priority: P3)

**Goal**: Automatically maintain cash buffer between 15-25% of total equity by making additional small buys or sells at week-end after primary logic executes

**Independent Test**: Set up scenarios where cash ratio falls below 15% or exceeds 25% after primary buy/sell logic, trigger weekly routine, verify system makes corrective trades to bring cash ratio back into range

### Tests for User Story 4

- [ ] T091 [P] [US4] Unit test for CashBufferManager when cash_ratio < MIN_CASH_RATIO in tests/TradingBot.Engine.Tests/WeeklyRoutine/CashBufferManagerTests.cs
- [ ] T092 [P] [US4] Unit test for CashBufferManager when cash_ratio > MAX_CASH_RATIO in tests/TradingBot.Engine.Tests/WeeklyRoutine/CashBufferManagerTests.cs
- [ ] T093 [P] [US4] Unit test for CashBufferManager when cash_ratio within range (no action) in tests/TradingBot.Engine.Tests/WeeklyRoutine/CashBufferManagerTests.cs
- [ ] T094 [P] [US4] Unit test for CashBufferManager respecting COIN > MA20 condition for excess cash buys in tests/TradingBot.Engine.Tests/WeeklyRoutine/CashBufferManagerTests.cs

### Implementation for User Story 4

**Engine Layer - Cash Buffer Manager**

- [ ] T095 [US4] Create CashBufferManager class in src/TradingBot.Engine/WeeklyRoutine/CashBufferManager.cs
- [ ] T096 [US4] Implement AdjustCashBufferAsync method (main rebalancing logic) in CashBufferManager
- [ ] T097 [US4] Implement logic: if cash_ratio < MIN_CASH_RATIO, sell WEEKLY_SELL_RATIO of position in CashBufferManager
- [ ] T098 [US4] Implement logic: if cash_ratio > MAX_CASH_RATIO and COIN > MA20, buy with excess cash in CashBufferManager
- [ ] T099 [US4] Integrate CashBufferManager into WeeklyRoutineExecutor after primary buy/sell logic
- [ ] T100 [US4] Add structured logging for cash buffer adjustments with before/after ratios
- [ ] T101 [US4] Raise CashBufferAdjustedEvent when adjustment executes

**Integration and Validation**

- [ ] T102 [US4] Test cash buffer rebalancing end-to-end with mocked dependencies
- [ ] T103 [US4] Verify domain event CashBufferAdjustedEvent is raised with adjustment details
- [ ] T104 [US4] Test scenario: cash_ratio = 12% after primary logic, verify 10% sell to rebuild buffer
- [ ] T105 [US4] Test scenario: cash_ratio = 30% after primary logic and COIN > MA20, verify buy to reduce excess
- [ ] T106 [US4] Test scenario: cash_ratio = 30% but COIN < MA20, verify no buy (only buys if bullish)
- [ ] T107 [US4] Test scenario: cash_ratio = 20% (within range), verify no adjustment

**Checkpoint**: All core trading logic complete - strategy maintains healthy cash buffer automatically

---

## Phase 7: User Story 5 - Daily MA20 Tracking and Status Monitoring (Priority: P3)

**Goal**: Display daily updates to MA20, consecutive days below MA20, current prices, and strategy state in web dashboard for visibility and anticipation of upcoming actions

**Independent Test**: Activate strategy, navigate to strategy details/dashboard page, verify real-time or daily-refreshed data displays: current COIN price, ETP price, MA20 value, days_below_ma20, cash ratio, next scheduled action

### Tests for User Story 5

- [ ] T108 [P] [US5] Blazor component test for StrategyStateCard real-time updates in tests/TradingBot.Web.Tests/Components/WeeklyCashStrategy/StrategyStateCardTests.cs
- [ ] T109 [P] [US5] Blazor component test for StrategyDetailsPanel metrics display in tests/TradingBot.Web.Tests/Components/WeeklyCashStrategy/StrategyDetailsPanelTests.cs
- [ ] T110 [P] [US5] Integration test for SignalR strategy state broadcasts in tests/TradingBot.Web.Tests/Hubs/TradingHubTests.cs

### Implementation for User Story 5

**SignalR Hub Extension**

- [ ] T111 [US5] Extend TradingHub with SendStrategyStateUpdate method in src/TradingBot.Web/Hubs/TradingHub.cs
- [ ] T112 [US5] Create StrategyUpdateBroadcaster event handler for StrategyExecutedEvent in src/TradingBot.Web/Services/StrategyUpdateBroadcaster.cs
- [ ] T113 [US5] Create StrategyUpdateBroadcaster event handler for MA20UpdatedEvent in src/TradingBot.Web/Services/StrategyUpdateBroadcaster.cs
- [ ] T114 [US5] Implement batching with 2-second interval and hash-based change detection in StrategyUpdateBroadcaster
- [ ] T115 [US5] Register StrategyUpdateBroadcaster as MediatR notification handler in src/TradingBot.Web/Program.cs

**Blazor Components**

- [ ] T116 [P] [US5] Create StrategyStateCard.razor component with real-time SignalR connection in src/TradingBot.Web/Components/Features/WeeklyCashStrategy/StrategyStateCard.razor
- [ ] T117 [P] [US5] Create StrategyDetailsPanel.razor component with metrics and charts in src/TradingBot.Web/Components/Features/WeeklyCashStrategy/StrategyDetailsPanel.razor
- [ ] T118 [US5] Add strategy details page route in src/TradingBot.Web/Components/Pages/WeeklyCashStrategyDetails.razor
- [ ] T119 [US5] Add strategy summary card to strategies overview page in src/TradingBot.Web/Components/Pages/Strategies.razor

**Dashboard Integration**

- [ ] T120 [US5] Extend DashboardService to include weekly strategy metrics in src/TradingBot.Web/Services/DashboardService.cs
- [ ] T121 [US5] Add strategy state section to main dashboard page in src/TradingBot.Web/Components/Pages/Index.razor
- [ ] T122 [US5] Implement SignalR connection lifecycle management (connect, reconnect, dispose) in components
- [ ] T123 [US5] Add MessagePack serialization for StrategyStateDto

**Integration and Validation**

- [ ] T124 [US5] Test SignalR real-time updates when strategy state changes
- [ ] T125 [US5] Verify StrategyStateCard updates within 2 seconds of state change
- [ ] T126 [US5] Test dashboard displays: COIN price, ETP price, MA20, days_below_ma20, cash_ratio, next_execution
- [ ] T127 [US5] Test daily routine updates trigger SignalR broadcasts to connected clients
- [ ] T128 [US5] Test multiple concurrent strategy instances display correctly in dashboard

**Checkpoint**: Full monitoring and visibility in web dashboard - users can track strategy behavior in real-time

---

## Phase 8: User Story 6 - Optional Breakout Rule for Accelerated Buying (Priority: P4)

**Goal**: Enable optional breakout rule that doubles weekly buy amount when underlying asset shows significant price increase (+10% weekly) and high volume, capitalizing on strong momentum

**Independent Test**: Enable breakout rule in configuration, set up scenario where COIN > MA20 AND weekly price increase > 10% AND volume > average, trigger weekly routine, verify system doubles buy ratio (invests 10% instead of 5%)

### Tests for User Story 6

- [ ] T129 [P] [US6] Unit test for BreakoutDetector when all conditions met (price +12%, volume 150%) in tests/TradingBot.Engine.Tests/WeeklyRoutine/BreakoutDetectorTests.cs
- [ ] T130 [P] [US6] Unit test for BreakoutDetector when price threshold not met (+8% only) in tests/TradingBot.Engine.Tests/WeeklyRoutine/BreakoutDetectorTests.cs
- [ ] T131 [P] [US6] Unit test for BreakoutDetector when volume threshold not met in tests/TradingBot.Engine.Tests/WeeklyRoutine/BreakoutDetectorTests.cs
- [ ] T132 [P] [US6] Unit test for BreakoutDetector when rule disabled (ignore conditions) in tests/TradingBot.Engine.Tests/WeeklyRoutine/BreakoutDetectorTests.cs

### Implementation for User Story 6

**Engine Layer - Breakout Detector**

- [ ] T133 [US6] Create BreakoutDetector class in src/TradingBot.Engine/WeeklyRoutine/BreakoutDetector.cs
- [ ] T134 [US6] Implement DetectBreakoutAsync method (checks price increase + volume conditions) in BreakoutDetector
- [ ] T135 [US6] Calculate weekly price increase percentage from historical candles in BreakoutDetector
- [ ] T136 [US6] Calculate 20-day average volume from historical candles in BreakoutDetector
- [ ] T137 [US6] Return buy ratio multiplier (default 2x) when breakout detected in BreakoutDetector
- [ ] T138 [US6] Integrate BreakoutDetector into CalculateBuyAmountAsync method in WeeklyRoutineExecutor
- [ ] T139 [US6] Add structured logging for breakout detection (conditions met/not met)

**Configuration UI Extension**

- [ ] T140 [US6] Add breakout rule configuration section to StrategyConfigurationForm.razor
- [ ] T141 [US6] Add input fields: enable toggle, price threshold %, volume multiplier, buy multiplier
- [ ] T142 [US6] Serialize BreakoutRuleConfig to JSON and store in WeeklyCashManagedStrategy.BreakoutRuleConfigJson
- [ ] T143 [US6] Deserialize BreakoutRuleConfig from JSON when loading strategy configuration

**Integration and Validation**

- [ ] T144 [US6] Test breakout detection end-to-end with mocked market data
- [ ] T145 [US6] Test scenario: Breakout detected, verify buy amount doubles (10% instead of 5%)
- [ ] T146 [US6] Test scenario: Price increase 8% (below threshold), verify normal buy amount (5%)
- [ ] T147 [US6] Test scenario: Price increase 12% but low volume, verify normal buy amount
- [ ] T148 [US6] Test scenario: Breakout rule disabled in config, verify breakout logic ignored
- [ ] T149 [US6] Verify breakout detection works with different configurable thresholds

**Checkpoint**: All user stories complete - full weekly cash-managed strategy with optional breakout rule operational

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Code quality, performance optimization, documentation, and final validation

### Code Quality and Coverage

- [ ] T150 [P] Run dotnet build with analyzers enabled (StyleCop, Roslynator, SonarAnalyzer) and fix all warnings
- [ ] T151 [P] Run dotnet test with code coverage collection and verify 80% minimum coverage overall
- [ ] T152 Verify 100% code coverage for buy logic, sell logic, and cash buffer adjustment (critical paths)
- [ ] T153 [P] Add XML documentation comments to all public APIs (entities, services, interfaces)
- [ ] T154 [P] Run dotnet format to ensure consistent code formatting

### Performance Validation

- [ ] T155 Verify MA20 calculation completes in <100ms with sliding window implementation
- [ ] T156 Verify weekly routine execution completes in <10 seconds end-to-end
- [ ] T157 Verify SignalR strategy state updates broadcast within 2 seconds
- [ ] T158 Load test: 10+ concurrent strategy instances executing weekly routine simultaneously

### Security and Risk Management

- [ ] T159 Verify all orders go through OrderExecutionService and RiskManager validation (no bypasses)
- [ ] T160 Test scenario: Order exceeds risk limits, verify order is rejected and logged
- [ ] T161 Add audit trail logging for all strategy configuration changes
- [ ] T162 Verify domain events provide immutable audit trail for all trades

### Error Handling and Edge Cases

- [ ] T163 [P] Test edge case: ETP market closed during weekly routine, verify order queued for next session
- [ ] T164 [P] Test edge case: MA20 calculation fails due to insufficient data (<20 days), verify strategy prevents activation
- [ ] T165 [P] Test edge case: Cash ratio drops below MIN during week (unrealized losses), verify rebalancing at next weekly routine
- [ ] T166 [P] Test edge case: User manually closes position while strategy active, verify state recalculates correctly
- [ ] T167 [P] Test edge case: Yahoo Finance API failure, verify routine skips execution and logs error
- [ ] T168 [P] Test edge case: Calculated buy/sell amount below 1 share, verify order skipped with log message
- [ ] T169 [P] Test edge case: User changes configuration mid-week, verify new parameters apply at next weekly routine
- [ ] T170 [P] Test edge case: Extreme volatility causes ETP price divergence, verify strategy continues per logic

### Database and Migration Validation

- [ ] T171 Verify migration creates correct schema with all columns and indexes
- [ ] T172 Test database rollback to previous migration works correctly
- [ ] T173 Verify strategy state persists correctly across application restarts
- [ ] T174 Test concurrent access: multiple users configuring different strategies simultaneously

### Documentation

- [ ] T175 [P] Update CLAUDE.md with weekly cash strategy patterns and gotchas
- [ ] T176 [P] Verify quickstart.md is accurate and all commands work
- [ ] T177 [P] Add architecture decision record (ADR) for sliding window MA20 algorithm choice
- [ ] T178 [P] Document breakout rule configuration examples in user guide

### Integration Testing

- [ ] T179 End-to-end test: Configure strategy → Enable → Daily routine runs → Weekly routine executes buy → Dashboard updates
- [ ] T180 End-to-end test: Strategy enabled → COIN drops below MA20 for 2 days → Weekly routine executes sell → Position reduced
- [ ] T181 End-to-end test: Cash ratio falls below minimum → Cash buffer adjustment sells to rebuild → Cash ratio restored
- [ ] T182 Run quickstart.md validation: Follow guide step-by-step, verify all steps work

### Deployment Preparation

- [ ] T183 Create database backup before applying migration to production
- [ ] T184 Test strategy behavior with real Yahoo Finance API data (not mocked)
- [ ] T185 Verify strategy works correctly in staging environment
- [ ] T186 Document rollback plan if issues discovered after deployment

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-8)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P2 → P3 → P3 → P4)
- **Polish (Phase 9)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Requires US1 for configuration to exist
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Requires US2 for daily routine infrastructure
- **User Story 4 (P3)**: Can start after Foundational (Phase 2) - Requires US2/US3 for buy/sell logic
- **User Story 5 (P3)**: Can start after Foundational (Phase 2) - Can run in parallel with US2-US4 (monitoring layer)
- **User Story 6 (P4)**: Can start after Foundational (Phase 2) - Requires US2 for buy logic to extend

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Domain entities and value objects before services
- Services before background workers and web components
- Core implementation before integration tests
- Story complete before moving to next priority

### Parallel Opportunities

**Setup Phase:**
- T001, T002, T003 can run in parallel

**Foundational Phase:**
- T004, T005 can run in parallel (value objects)
- T007-T012 can run in parallel (domain events)
- T013-T017 can run in parallel (interfaces)

**User Story 1 Tests:**
- T024, T025, T026, T027, T028, T029, T030 can all run in parallel

**User Story 1 Implementation:**
- T031, T032 can run in parallel (DTOs)

**User Story 2 Tests:**
- T042, T043, T044 can run in parallel (MA20 tests)
- T045, T046, T047, T048 can run in parallel (buy logic tests)

**User Story 3 Tests:**
- T071, T072 can run in parallel (counter tests)
- T073, T074, T075, T076 can run in parallel (sell logic tests)

**User Story 4 Tests:**
- T091, T092, T093, T094 can all run in parallel

**User Story 5 Tests:**
- T108, T109, T110 can all run in parallel

**User Story 5 Implementation:**
- T116, T117 can run in parallel (Blazor components)

**User Story 6 Tests:**
- T129, T130, T131, T132 can all run in parallel

**Polish Phase:**
- T150, T151, T153, T154 can run in parallel (code quality)
- T163-T170 can run in parallel (edge case tests)
- T175, T176, T177, T178 can run in parallel (documentation)

---

## Parallel Example: User Story 2 (Buy Logic)

```bash
# Launch all MA20 tests together:
Task: "Unit test for MA20 calculation accuracy in tests/TradingBot.Infrastructure.Tests/Services/MA20IndicatorServiceTests.cs"
Task: "Unit test for MA20 sliding window update in tests/TradingBot.Infrastructure.Tests/Services/MA20IndicatorServiceTests.cs"
Task: "Unit test for MA20 gap handling in tests/TradingBot.Infrastructure.Tests/Services/MA20IndicatorServiceTests.cs"

# Launch all buy logic tests together:
Task: "Unit test for WeeklyRoutineExecutor buy logic when COIN > MA20"
Task: "Unit test for buy amount calculation (5% of equity)"
Task: "Unit test for buy logic when cash_ratio <= MIN_CASH_RATIO (no buy)"
Task: "Unit test for buy logic when insufficient cash (buy only available amount)"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only)

1. Complete Phase 1: Setup (T001-T003)
2. Complete Phase 2: Foundational (T004-T023) - **CRITICAL - blocks all stories**
3. Complete Phase 3: User Story 1 (T024-T041) - Configuration and enable
4. Complete Phase 4: User Story 2 (T042-T070) - Automated weekly buying
5. **STOP and VALIDATE**: Test US1 and US2 independently, verify buy execution
6. Deploy/demo if ready (MVP provides configuration + automated buying)

### Incremental Delivery (Recommended)

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Configuration works
3. Add User Story 2 → Test independently → Automated buying works (MVP!)
4. Add User Story 3 → Test independently → Automated selling works
5. Add User Story 4 → Test independently → Cash buffer management works
6. Add User Story 5 → Test independently → Dashboard monitoring works
7. Add User Story 6 → Test independently → Breakout rule works
8. Polish phase → Production ready

Each story adds value without breaking previous stories.

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (Configuration UI)
   - Developer B: User Story 2 (Buy logic + MA20)
   - Developer C: User Story 5 (Dashboard monitoring)
3. Then sequentially:
   - Developer A or B: User Story 3 (Sell logic)
   - Developer A or B: User Story 4 (Cash buffer)
   - Developer A, B, or C: User Story 6 (Breakout rule)
4. Team: Polish phase together

---

## Summary

**Total Tasks**: 186 tasks
- Phase 1 (Setup): 3 tasks
- Phase 2 (Foundational): 20 tasks
- Phase 3 (User Story 1 - Configuration): 18 tasks
- Phase 4 (User Story 2 - Buy Logic): 28 tasks
- Phase 5 (User Story 3 - Sell Logic): 20 tasks
- Phase 6 (User Story 4 - Cash Buffer): 17 tasks
- Phase 7 (User Story 5 - Monitoring): 21 tasks
- Phase 8 (User Story 6 - Breakout Rule): 21 tasks
- Phase 9 (Polish): 38 tasks

**Parallel Opportunities**: 80+ tasks marked [P] can run in parallel (different files, no dependencies)

**Independent Test Criteria**: Each user story has clear acceptance criteria and independent testing approach

**MVP Scope**: User Stories 1 + 2 (configuration + automated buying) - 49 tasks total after foundational phase

**Format Validation**: ✅ ALL tasks follow checklist format (checkbox, ID, [P] for parallel, [Story] label, file paths)

---

## Notes

- [P] tasks = different files, no dependencies, can run in parallel
- [Story] label maps task to specific user story (US1-US6) for traceability
- Each user story should be independently completable and testable
- Tests are written FIRST (TDD approach) for all critical trading logic
- 100% coverage required for buy logic, sell logic, and cash buffer adjustment
- 80% minimum coverage required overall
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Follow existing DDD patterns: EntityBase, IAggregateRoot, domain events, repositories
- Use SmartEnum pattern for all enums
- All orders MUST go through OrderExecutionService and RiskManager
- Use existing Tb-prefixed atomic components for UI (TbButton, TbFormField, etc.)
- SignalR with MessagePack for real-time updates
- NCrontab for cron schedule parsing
- Sliding window algorithm for MA20 calculation (O(1) performance)