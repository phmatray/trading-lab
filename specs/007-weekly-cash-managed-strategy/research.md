# Research Decisions: Weekly Cash-Managed Trading Strategy

**Date**: 2025-01-16
**Phase**: 0 (Research & Technical Decisions)
**Status**: Completed

## Overview

This document captures all technical decisions for implementing the Weekly Cash-Managed Trading Strategy. Each decision includes the chosen solution, rationale, and alternatives considered to ensure alignment with the existing TradingBot architecture and project constitution.

---

## Decision 1: Scheduling Strategy

**Decision**: Custom BackgroundService with PeriodicTimer and NCrontab for cron expression parsing (no external scheduler dependency)

**Rationale**:
1. **Consistency with existing architecture**: The codebase already uses `BackgroundService` pattern (seen in `JobScheduler.cs`, `BacktestExecutionWorker.cs`, `RealtimeUpdateService.cs`) with `PeriodicTimer` for recurring tasks. This approach is proven and reliable in the existing system.
2. **Lightweight and sufficient**: For weekly/daily schedules (not requiring complex distributed coordination), BackgroundService provides all necessary features without introducing heavy dependencies like Quartz.NET. The existing `JobScheduler` base class demonstrates this pattern works well.
3. **Trading calendar support**: Can implement custom `ITradingCalendar` service to check market hours/holidays before execution. NCrontab (3.4.0) can handle cron expressions for "every Friday at market close" scheduling, providing flexible schedule configuration without full Quartz.NET overhead.

**Alternatives Considered**:
- **Quartz.NET**: Rejected due to unnecessary complexity for simple weekly/daily schedules. Adds significant dependency overhead (~500KB), requires persistence configuration, and provides clustering/distributed features not needed for single-instance web app.
- **Hangfire**: Rejected for similar reasons to Quartz.NET - designed for distributed job processing with dashboard UI, which duplicates existing Blazor UI functionality and adds unnecessary infrastructure.
- **TickerQ** (already in Directory.Packages.props v2.5.3): Rejected as it appears unused in current codebase and would require learning new API when BackgroundService pattern is already established.

**Implementation Plan**:
1. Add NCrontab NuGet package (if not already present)
2. Create `WeeklyCashManagedStrategyWorker : BackgroundService` extending existing pattern
3. Implement `ITradingCalendar` service to check market hours/holidays
4. Use `PeriodicTimer` for daily checks + NCrontab for weekly schedule expression parsing

---

## Decision 2: MA20 Calculation Algorithm

**Decision**: Sliding window with running sum (no external library like TALib)

**Rationale**:
1. **Performance**: O(1) amortized time per new data point using running sum (sum = sum - oldest + newest), easily meeting <100ms requirement. The existing `IndicatorLibrary.CalculateSMA()` uses LINQ `.TakeLast(period).Sum()` which is O(n) on each call - refactoring to sliding window improves from O(n) to O(1) for incremental updates.
2. **Accuracy and simplicity**: Simple addition/subtraction maintains decimal precision (0.01% accuracy) without floating-point errors. Existing codebase uses `decimal` type throughout for financial calculations, ensuring no precision loss.
3. **Handles missing data**: Can detect gaps in daily candle data (weekends/holidays) by checking timestamp differences. If gap detected, recalculate full 20-day window from available data. This hybrid approach balances performance (O(1) for normal days) with correctness (full recalc when needed).

**Alternatives Considered**:
- **TALib.NETCore**: Rejected because it adds native library dependency (C/C++ wrapper), increases deployment complexity, and provides 100+ indicators when only SMA is needed.
- **LINQ-based (current implementation)**: Rejected for incremental updates as it's O(n) on every call. Acceptable for backtest batch processing but inefficient for daily live updates where same MA20 is recalculated with only one new data point.
- **Cumulative sum array**: Rejected for live trading as it requires preprocessing entire history. Better suited for backtest scenarios with random window access, not daily streaming updates.

**Implementation Plan**:
1. Create `MA20CalculatorService` in Infrastructure layer
2. Implement sliding window with Queue<decimal> (max size 20)
3. Maintain running sum: `sum += newCandle.Close; sum -= oldestCandle.Close;`
4. Add gap detection: if `(newCandle.Date - lastCandle.Date).Days > 1`, recalculate from last 20 available candles
5. Unit tests: Verify accuracy within 0.01% against LINQ baseline, test gap handling

---

## Decision 3: State Persistence Pattern

**Decision**: Embedded properties in aggregate root `WeeklyCashManagedStrategy : EntityBase<Guid>` with EF Core persistence

**Rationale**:
1. **Aligns with DDD patterns**: Strategy state (`days_below_ma20`, `last_execution_timestamp`, `cash_ratio`, configuration) belongs to the strategy aggregate. The codebase uses Ardalis.SharedKernel with `EntityBase<Guid>` and `IAggregateRoot` - creating `WeeklyCashManagedStrategy : EntityBase<Guid>, IAggregateRoot` follows established patterns (similar to `Order`, `Position`, `Trade`).
2. **Atomic updates with domain events**: When weekly routine executes and generates orders, both strategy state update and order creation happen in single EF Core transaction via `SaveChangesAsync()`. Domain events (`StrategyExecutedEvent`, `StrategyStateChangedEvent`) are dispatched before commit (existing `MediatorDomainEventDispatcher` pattern in `TradingBotDbContext`).
3. **Backtest vs live mode**: Add `IsBacktest` boolean property to aggregate. Backtest mode uses in-memory context (already supported via `Microsoft.EntityFrameworkCore.InMemory` package), live mode uses SQLite. State recovery after restart is automatic via EF Core repository pattern (existing `EfRepository<T>` implementation).

**Alternatives Considered**:
- **Separate StrategyState entity**: Rejected as it creates unnecessary one-to-one relationship and violates DDD principle of keeping aggregate state cohesive.
- **Event sourcing**: Rejected due to significant architectural change required. Current system uses traditional CRUD with domain events for side effects, not event-sourced aggregates. Event sourcing would require new event store, projection infrastructure, and doesn't align with existing EF Core patterns.
- **In-memory state only**: Rejected as it cannot survive application restarts. Strategy must resume with correct `days_below_ma20` counter and last execution timestamp after deployment or crash.

**Implementation Plan**:
1. Create `WeeklyCashManagedStrategy : EntityBase<Guid>, IAggregateRoot` in Core
2. Add properties: `DaysBelowMA20`, `LastExecutionTimestamp`, `MinCashRatio`, `MaxCashRatio`, etc.
3. Create `WeeklyCashManagedStrategyConfiguration : IEntityTypeConfiguration<WeeklyCashManagedStrategy>` in Infrastructure
4. Add `DbSet<WeeklyCashManagedStrategy>` to `TradingBotDbContext`
5. Create EF Core migration: `dotnet ef migrations add AddWeeklyCashManagedStrategy`
6. Implement repository: `WeeklyCashManagedStrategyRepository : EfRepository<WeeklyCashManagedStrategy>`

---

## Decision 4: Breakout Rule Implementation

**Decision**: Strategy pattern with pluggable rule classes (no rules engine)

**Rationale**:
1. **Extensibility without complexity**: Create `IBreakoutRule` interface with implementations like `PriceChangeRule`, `VolumeThresholdRule`, `MA20CrossoverRule`. Each rule encapsulates single condition logic. Compose multiple rules via `CompositeBreakoutRule` that combines results (AND/OR logic). This provides clean extensibility matching existing strategy architecture (`IStrategy` interface with implementations).
2. **Runtime configuration**: Store rule configuration as JSON in `WeeklyCashManagedStrategy.BreakoutRuleConfigJson` property. Deserialize to strongly-typed rule parameters on strategy initialization. Enable/disable rules via boolean flags in configuration. Follows existing pattern seen in strategy configuration persistence.
3. **Performance**: In-process rule evaluation is <1ms for typical conditions (price comparisons, volume checks). No need for expression compilation or external rules engine overhead. Simple if-else with strategy pattern provides sufficient performance for weekly execution frequency.

**Alternatives Considered**:
- **NRules or similar rules engine**: Rejected as overkill for 3-5 simple conditions. NRules uses Rete algorithm optimized for hundreds of rules and complex fact patterns - unnecessary for weekly strategy with basic price/volume conditions.
- **Expression trees with runtime compilation**: Rejected due to complexity of parsing/validating user-provided expressions and security risks (code injection). Requires expression parser, compiler, and sandboxing. Strategy pattern with predefined rule types is safer and simpler.
- **Simple if-else in strategy class**: Rejected as it violates Open/Closed Principle. Adding new breakout conditions requires modifying strategy class. Strategy pattern allows adding new rule implementations without changing core strategy logic.

**Implementation Plan**:
1. Create `IBreakoutRule` interface in Core with `EvaluateAsync(MarketData) : Task<bool>` method
2. Implement concrete rules: `PriceChangePercentageRule`, `VolumeMultiplierRule`, `CompositeBreakoutRule`
3. Add `BreakoutRuleConfigJson` property to `WeeklyCashManagedStrategy` entity (nullable string)
4. Create `BreakoutRuleFactory` to deserialize JSON config and instantiate rule instances
5. Inject `IBreakoutRule` into `WeeklyRoutineService` as optional dependency

---

## Decision 5: Real-time Update Mechanism

**Decision**: Batch updates every 2-3 seconds via existing SignalR infrastructure with change detection hashing

**Rationale**:
1. **Proven pattern in codebase**: `RealtimeUpdateService.cs` already implements batch updates with change detection using SHA256 hashing (`ComputeHash()` method), 500ms minimum broadcast interval, and hash-based change detection. Strategy state changes fit same pattern - compute hash of strategy state, only broadcast if hash changed.
2. **Performance and cost optimization**: Batching reduces SignalR message volume and Azure SignalR Service costs (charged per message). Strategy state updates are low-frequency (daily MA20 calculations, weekly executions) so 2-second latency is acceptable. Avoids overwhelming clients with redundant updates.
3. **Existing MessagePack protocol**: Codebase uses `Microsoft.AspNetCore.SignalR.Protocols.MessagePack` (10.0.0) for compact serialization. Strategy state messages (symbol, MA20 value, days_below_ma20, next_execution) serialize to ~100 bytes, easily supporting 10+ concurrent users.

**Alternatives Considered**:
- **Broadcast on every state change**: Rejected as wasteful for low-frequency updates. Strategy state changes once daily (MA20 calculation) or weekly (execution). Immediate broadcast adds no value over 2-second batching and increases server load.
- **Server-Sent Events (SSE)**: Rejected because existing SignalR infrastructure handles bidirectional communication (future feature: user can trigger manual strategy execution). SSE is one-way only and would require parallel infrastructure. SignalR is already proven and configured.
- **Publish domain events to hub**: Rejected due to tight coupling between domain layer and presentation. Domain events (`StrategyExecutedEvent`) should remain in Core/Infrastructure. Create dedicated service (`StrategyUpdateBroadcaster`) that subscribes to domain events via MediatR and publishes to SignalR hub, maintaining layer separation.

**Implementation Plan**:
1. Create `StrategyUpdateBroadcaster : INotificationHandler<StrategyExecutedEvent>` in Web layer
2. Subscribe to domain events: `StrategyExecutedEvent`, `MA20UpdatedEvent`, `CashBufferAdjustedEvent`
3. Batch updates using `PeriodicTimer` (2-second interval) with hash-based change detection
4. Add hub methods to `TradingHub`: `SendStrategyStateUpdate(Guid strategyId, StrategyStateDto state)`
5. Serialize state to MessagePack DTO: `{ Symbol, MA20, DaysBelowMA20, CashRatio, NextExecution }`

---

## Decision 6: Test Data Generation

**Decision**: Builder pattern with embedded CSV fixture files for base data + randomization for variations

**Rationale**:
1. **Reproducible edge cases**: Embed CSV files in test project (set Build Action: EmbeddedResource) containing curated scenarios: `ma20_breakdown_scenario.csv` (2+ days below MA20), `price_gap_scenario.csv` (weekend gaps), `high_volatility_scenario.csv` (sharp moves). Tests load CSV via `Assembly.GetManifestResourceStream()`, providing guaranteed edge case coverage.
2. **Builder pattern for flexibility**: Implement `CandleDataBuilder` class (inspired by existing `CreateTestCandles()` method in `MomentumStrategyTests.cs`). Builder provides fluent API: `new CandleDataBuilder().WithSymbol("COIN").WithTrend(TrendType.Uptrend, 60).WithVolatility(0.02m).Build()`. Combines deterministic base patterns with controlled randomization.
3. **Fast execution**: CSV parsing + builder construction runs in <10ms per test (tested with 100+ candle datasets). No HTTP mocking needed since `IMarketDataService` is faked via FakeItEasy (existing pattern throughout test suite). Test data is in-memory, no I/O overhead.

**Alternatives Considered**:
- **WireMock for Yahoo Finance HTTP recording**: Rejected as over-engineered for unit tests. HTTP recording is valuable for integration tests but adds brittleness (recording expiration, API changes). Market data should be abstracted via `IMarketDataService` interface (already exists), which is easily faked. WireMock is HTTP-level mocking when we need domain-level data.
- **Pure randomization with AutoFixture**: Rejected because financial data has constraints (High >= Low, Volume > 0, realistic price movements). Random data often generates invalid scenarios. Builder pattern with constrained randomization (e.g., random walk within ±2% bands) produces realistic yet varied test data.
- **Historical JSON files only**: Rejected as inflexible - adding new edge case requires creating/editing JSON files. Builder pattern allows programmatic data construction in test methods, making test intent clearer (e.g., `.WithConsecutiveDaysBelow(MA20, days: 3)` is more readable than referencing external file).

**Implementation Plan**:
1. Create `CandleDataBuilder` class in test project with fluent API methods
2. Add embedded CSV resources for edge cases: `EmbeddedResources/MarketData/ma20_breakdown_scenario.csv`
3. Implement helper: `LoadEmbeddedCsv(string resourceName) : List<Candle>`
4. Create builder methods:
   - `WithTrend(TrendType type, decimal slope)`: Generate trending price series
   - `WithConsecutiveDaysBelow(decimal ma20, int days)`: Generate MA20 breakdown scenario
   - `WithGap(int days)`: Insert weekend/holiday gaps
   - `WithVolatility(decimal percentage)`: Add realistic price fluctuation
5. Use in tests: `var candles = new CandleDataBuilder().WithSymbol("COIN").WithConsecutiveDaysBelow(140m, 3).Build();`

---

## Integration Summary

These decisions align cohesively with the existing TradingBot architecture:

| Decision | Aligns With Existing Pattern |
|----------|------------------------------|
| **Scheduling** | Extends existing `BackgroundService` / `JobScheduler` base class pattern |
| **MA20 Calculation** | Refactors existing `IndicatorLibrary.CalculateSMA()` to sliding window |
| **State Persistence** | Follows established DDD patterns with `EntityBase<Guid>` and domain events |
| **Breakout Rules** | Mirrors existing `IStrategy` interface pattern for extensibility |
| **SignalR Updates** | Leverages proven `RealtimeUpdateService` batch update pattern with hash-based change detection |
| **Test Data** | Builds on existing test patterns (`MomentumStrategyTests` builder approach) |

**Key Principles**:
1. **No new architectural paradigms**: All decisions extend existing patterns rather than introducing new ones
2. **Minimal external dependencies**: Only NCrontab added; rejected Quartz.NET, Hangfire, TALib, NRules, WireMock
3. **DDD compliance**: Aggregate roots, domain events, repositories follow Ardalis.SharedKernel patterns
4. **Performance first**: Sliding window O(1), batch SignalR updates, in-memory test data (<10ms)
5. **Testability**: Builder pattern, interface-based design, FakeItEasy mocking throughout

---

## Next Phase

With all technical decisions finalized, proceed to **Phase 1: Design & Contracts** to create:
1. `data-model.md`: Entity definitions, relationships, validation rules
2. `contracts/`: Service interfaces, domain events, DTOs
3. `quickstart.md`: Developer setup guide
4. Update agent context with new patterns and technologies