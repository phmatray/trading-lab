# Implementation Plan: Weekly Cash-Managed Trading Strategy

**Branch**: `007-weekly-cash-managed-strategy` | **Date**: 2025-01-16 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/007-weekly-cash-managed-strategy/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Implement a weekly cash-managed trading strategy that automatically buys ETP shares when the underlying asset (e.g., COIN) is above its 20-day moving average (MA20) and sells when it stays below MA20 for 2+ consecutive days. The strategy maintains a cash buffer between 15-25% of total equity using configurable buy/sell ratios (default 5% buy, 10% sell weekly). Includes optional breakout rule to double buy amount during strong momentum (>10% weekly gain + high volume). Strategy executes on a weekly schedule (default Friday), integrates with existing DDD architecture, and provides real-time dashboard updates via SignalR.

## Technical Context

**Language/Version**: C# / .NET 10 (LangVersion 14)
**Primary Dependencies**: ASP.NET Core Blazor Server, Entity Framework Core 10, Ardalis.SharedKernel (DDD patterns), MediatR (domain events), SignalR (real-time updates), Ardalis.SmartEnum (type-safe enums), Yahoo Finance API (market data)
**Storage**: SQLite via Entity Framework Core 10 with fluent API configuration
**Testing**: xUnit 3.2, bUnit 2.0.66 (Blazor components), FakeItEasy 8.3 (mocking), Shouldly 4.3 (assertions), in-memory SQLite for integration tests
**Target Platform**: Desktop web application (ASP.NET Core Blazor Server, Interactive Server render mode, desktop-only 1024px+ minimum)
**Project Type**: Multi-project web application (layered architecture: Core → Infrastructure → Engine/Strategies/Analytics → Web)
**Performance Goals**: Weekly routine completes in <10 seconds, MA20 calculation <100ms, SignalR updates within 2 seconds, API endpoints <200ms p95
**Constraints**: Must integrate with existing DDD architecture (EntityBase, IAggregateRoot, domain events via MediatR), all orders through OrderExecutionService and RiskManager validation, no CLI (web dashboard only), 80% code coverage minimum (100% for buy/sell/cash buffer logic)
**Scale/Scope**: Single strategy instance per symbol pair, executes once weekly (Friday default), manages one ETP position per strategy, supports multiple concurrent strategy instances for different symbol pairs

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Code Quality (Section 1)
- ✅ **Clean Code Standards**: Strategy will follow single responsibility (WeeklyCashManagedStrategy aggregate), descriptive naming (calculateCashRatio, executeBuyLogic), max function length 50 lines
- ✅ **C# Standards**: PascalCase for public members, async/await for I/O, IDisposable where needed, nullable reference types enabled
- ✅ **DDD Architecture**: WeeklyCashManagedStrategy is aggregate root extending EntityBase<Guid>, domain events via MediatR, repository pattern (IWeeklyCashManagedStrategyRepository)
- ✅ **Documentation**: XML doc comments for public APIs, inline comments for MA20 calculation logic, architecture decision for weekly execution timing

### Testing Standards (Section 2)
- ✅ **Coverage Requirements**: 80% minimum, 100% for critical paths (buy logic, sell logic, cash buffer adjustment, MA20 calculation)
- ✅ **Testing Pyramid**: Unit tests (70% - strategy logic, MA20 indicator), Integration tests (20% - database persistence, order execution), E2E tests (10% - web dashboard workflow)
- ✅ **Test Quality**: AAA pattern, meaningful names (ExecuteBuyLogic_WhenCoinAboveMA20AndSufficientCash_CreatesBuyOrder), test independence, fast execution
- ✅ **Test Data**: Use builders for test data (StrategyBuilder, MA20IndicatorBuilder), realistic market data for backtesting

### User Experience (Section 3)
- ✅ **UI Consistency**: Reuse existing Tb-prefixed atomic components (TbButton, TbFormField, TbCard), Tailwind CSS utilities, Heroicons
- ✅ **Accessibility**: WCAG 2.1 Level AA, keyboard navigation, ARIA labels for strategy configuration form
- ✅ **Responsive Design**: Desktop-first (1024px+ minimum), collapsible sidebar, no mobile optimization required
- ✅ **Error Handling**: User-friendly validation messages ("Cash ratio minimum must be less than maximum"), confirmation for strategy disable action
- ✅ **Data Visualization**: Real-time updates via SignalR (strategy state changes), use existing ApexCharts for equity curves if needed

### Performance Requirements (Section 4)
- ✅ **Response Time**: Weekly routine <10 seconds, MA20 calculation <100ms, SignalR updates <2 seconds, API endpoints <200ms p95
- ✅ **Scalability**: Stateless weekly routine execution, cache MA20 calculations with appropriate TTL
- ✅ **Resource Optimization**: Dispose market data connections properly, bulk load 20 days of historical data for MA20
- ✅ **Monitoring**: Structured logging for weekly routine execution, log buy/sell decisions with rationale, metrics for execution time

### Security Standards (Section 5)
- ✅ **Order Validation**: All orders through OrderExecutionService and RiskManager validation (position limits, cash limits)
- ✅ **Input Validation**: Validate configuration parameters (MIN_CASH_RATIO < MAX_CASH_RATIO, ratios in [0,1] range)
- ✅ **Audit Trail**: Domain events for all strategy actions (StrategyConfiguredEvent, StrategyExecutedEvent, OrderPlacedEvent)
- ✅ **Financial Security**: Enforce risk limits, log all trades immutably via domain events

### DevOps and CI/CD (Section 6)
- ✅ **Version Control**: Follow conventional commits, PR reviews required, no secrets in code
- ✅ **CI**: Automated tests on every PR, StyleCop/Roslynator/SonarAnalyzer checks, security scanning
- ✅ **Code Review**: Follow checklist (tests pass, coverage met, no vulnerabilities, documentation updated)

### Compliance (Section 8)
- ✅ **Audit Requirements**: Domain events provide complete audit trail for strategy execution and trades

**GATE STATUS**: ✅ **PASS** - All constitution requirements met. No violations requiring justification.

---

### Post-Design Re-evaluation (Phase 1 Complete)

After completing Phase 1 design artifacts (data-model.md, contracts/, quickstart.md), re-evaluating constitution compliance:

**Code Quality (Section 1)**:
- ✅ WeeklyCashManagedStrategy aggregate follows single responsibility (strategy execution only)
- ✅ Value objects (StrategyConfiguration, BreakoutRuleConfig) provide proper encapsulation
- ✅ Domain events (StrategyExecutedEvent, MA20UpdatedEvent, etc.) follow DomainEventBase pattern
- ✅ All interfaces use descriptive async method names (CalculateMA20Async, ExecuteWeeklyRoutineAsync)
- ✅ Repository pattern (IWeeklyCashManagedStrategyRepository) extends IRepositoryBase<T>
- ✅ Complete XML documentation in all contract files

**Testing Standards (Section 2)**:
- ✅ Test structure defined in quickstart.md with AAA pattern examples
- ✅ Example tests demonstrate meaningful naming (ExecuteBuyLogic_WhenCoinAboveMA20AndSufficientCash_CreatesBuyOrder)
- ✅ Builder pattern planned for test data generation (CandleDataBuilder)
- ✅ 100% coverage targets explicitly defined for buy/sell/cash buffer logic

**User Experience (Section 3)**:
- ✅ StrategyConfigurationDto uses validation attributes for user-friendly errors
- ✅ StrategyStateDto designed for real-time SignalR updates (<2 seconds)
- ✅ WeeklyRoutineResult provides detailed execution notes for transparency

**Performance Requirements (Section 4)**:
- ✅ MA20 sliding window algorithm designed for O(1) updates (<100ms target)
- ✅ SignalR batching strategy with hash-based change detection (2-second interval)
- ✅ Structured logging planned for execution time monitoring

**Security Standards (Section 5)**:
- ✅ All orders go through IOrderExecutionService and IRiskManager validation (per contracts)
- ✅ StrategyConfigurationDto includes validation for ratio ranges [0,1]
- ✅ Domain events provide immutable audit trail

**FINAL GATE STATUS**: ✅ **PASS** - Design artifacts fully compliant with project constitution. Ready for Phase 2 (tasks.md generation via /speckit.tasks).

## Project Structure

### Documentation (this feature)

```text
specs/007-weekly-cash-managed-strategy/
├── spec.md              # Feature specification (input)
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/TradingBot.Core/
├── Entities/
│   └── WeeklyCashManagedStrategy.cs         # New aggregate root
├── Events/
│   ├── StrategyConfiguredEvent.cs           # New domain event
│   ├── StrategyExecutedEvent.cs             # New domain event
│   └── StrategyDisabledEvent.cs             # New domain event
├── Interfaces/
│   ├── IWeeklyCashManagedStrategyRepository.cs  # New repository interface
│   ├── IMA20IndicatorService.cs             # New service interface
│   └── IWeeklyRoutineExecutor.cs            # New service interface
├── ValueObjects/
│   ├── BreakoutRuleConfig.cs                # New value object
│   └── StrategyConfiguration.cs             # New value object

src/TradingBot.Infrastructure/
├── Persistence/
│   ├── Configurations/
│   │   └── WeeklyCashManagedStrategyConfiguration.cs  # EF config
│   └── Repositories/
│       └── WeeklyCashManagedStrategyRepository.cs  # Repository implementation
└── Services/
    └── MA20IndicatorService.cs              # MA20 calculation service

src/TradingBot.Engine/
└── WeeklyRoutine/
    ├── WeeklyRoutineExecutor.cs             # Main weekly routine orchestrator
    ├── CashBufferManager.cs                 # Cash buffer adjustment logic
    └── BreakoutDetector.cs                  # Optional breakout rule detector

src/TradingBot.Web/
├── Components/
│   └── Features/
│       └── WeeklyCashStrategy/
│           ├── StrategyConfigurationForm.razor       # Configuration UI
│           ├── StrategyStateCard.razor               # Real-time state display
│           └── StrategyDetailsPanel.razor            # Detailed metrics
├── Services/
│   └── WeeklyCashStrategyService.cs         # Web-layer service (Scoped)
├── Hubs/
│   └── TradingHub.cs                        # Extended for strategy updates
└── BackgroundWorkers/
    └── WeeklyRoutineWorker.cs               # Hosted service for scheduled execution

tests/TradingBot.Core.Tests/
├── Entities/
│   └── WeeklyCashManagedStrategyTests.cs
├── ValueObjects/
│   ├── BreakoutRuleConfigTests.cs
│   └── StrategyConfigurationTests.cs
└── Events/
    └── StrategyEventTests.cs

tests/TradingBot.Infrastructure.Tests/
├── Repositories/
│   └── WeeklyCashManagedStrategyRepositoryTests.cs
└── Services/
    └── MA20IndicatorServiceTests.cs

tests/TradingBot.Engine.Tests/
└── WeeklyRoutine/
    ├── WeeklyRoutineExecutorTests.cs
    ├── CashBufferManagerTests.cs
    └── BreakoutDetectorTests.cs

tests/TradingBot.Web.Tests/
├── Components/
│   └── WeeklyCashStrategy/
│       ├── StrategyConfigurationFormTests.cs
│       ├── StrategyStateCardTests.cs
│       └── StrategyDetailsPanelTests.cs
└── Services/
    └── WeeklyCashStrategyServiceTests.cs
```

**Structure Decision**: Multi-project layered architecture following existing TradingBot pattern. Core contains domain entities (WeeklyCashManagedStrategy aggregate root), value objects (StrategyConfiguration, BreakoutRuleConfig), domain events, and repository interfaces. Infrastructure implements data access (EF Core configuration, repository) and MA20 calculation service. Engine contains weekly routine execution logic (orchestrator, cash buffer manager, breakout detector). Web layer provides Blazor components for configuration/monitoring and hosted service for scheduled execution. All layers follow DDD principles with strict dependency flow (Core ← Infrastructure ← Engine ← Web).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations - all constitution requirements met.

---

## Phase Summary

### ✅ Phase 0: Research (Completed)

**Output**: `research.md` (16K, 183 lines)

Generated 10 comprehensive research decisions covering:
- Background task scheduling (BackgroundService with NCrontab)
- MA20 calculation algorithm (sliding window O(1) performance)
- State persistence pattern (DDD aggregate with EF Core)
- Breakout rule implementation (strategy pattern)
- Real-time updates (SignalR with batching)
- Testing strategy (controllable clock abstraction)

All decisions aligned with existing TradingBot architecture patterns.

---

### ✅ Phase 1: Design & Contracts (Completed)

**Outputs**:
1. **data-model.md** (49K, 1,470 lines) - Complete data model with:
   - WeeklyCashManagedStrategy aggregate root
   - StrategyConfiguration and BreakoutRuleConfig value objects
   - 6 domain events (StrategyExecutedEvent, MA20UpdatedEvent, etc.)
   - State transition diagrams
   - Validation rules
   - EF Core configuration
   - Repository pattern

2. **contracts/** (8 files, 28K total) - Service interfaces and DTOs:
   - IWeeklyCashManagedStrategyRepository.cs
   - IMA20IndicatorService.cs
   - IWeeklyRoutineExecutor.cs
   - ICashBufferManager.cs
   - IBreakoutDetector.cs
   - StrategyStateDto.cs (SignalR updates)
   - StrategyConfigurationDto.cs (web forms)
   - WeeklyRoutineResult.cs (execution results)

3. **quickstart.md** (31K) - Developer onboarding guide:
   - Prerequisites and setup
   - Project structure overview
   - 7-step implementation guide
   - Build and run commands
   - Testing examples (3 complete scenarios)
   - Common pitfalls (6 detailed issues with solutions)
   - Debugging tips

4. **Agent context updated** - CLAUDE.md updated with new technologies from this feature

---

## Next Steps

**Phase 2**: Generate actionable task list using `/speckit.tasks`

The planning phase is now complete. All design artifacts are ready for task breakdown and implementation.

**Branch**: `007-weekly-cash-managed-strategy`
**Plan Location**: `/Users/phmatray/Repositories/github-phm/TradingBot/specs/007-weekly-cash-managed-strategy/plan.md`

**Generated Artifacts**:
- ✅ research.md - Technical decisions and rationale
- ✅ data-model.md - Complete entity and value object definitions
- ✅ contracts/ - 8 interface and DTO files
- ✅ quickstart.md - Developer implementation guide
- ✅ CLAUDE.md - Updated agent context
