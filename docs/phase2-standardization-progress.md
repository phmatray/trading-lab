# Phase 2: Standardization & Consistency - Progress Report

## Overview
Phase 2 focuses on standardizing error handling, progress reporting, and command validation patterns across the application to ensure consistent behavior and better developer experience.

## Completed ✓

### 1. Result<T> Pattern Foundation
**Status:** ✅ Complete

**Files Created:**
- `src/TradingStrat.Domain/Common/Result.cs` - Generic Result<T> type for type-safe error handling
- `src/TradingStrat.Domain/Common/Error.cs` - Structured error types (Validation, NotFound, BusinessRule, Conflict, InsufficientData)
- `src/TradingStrat.Domain/Common/ResultExtensions.cs` - Extension methods for Result<T> operations
- `docs/result-pattern-migration-guide.md` - Comprehensive migration guide with examples

**Benefits:**
- Eliminates exception-based control flow
- Provides structured error messages with error codes
- Enables better error handling and recovery logic
- Type-safe - compiler enforces error handling

**Example Usage:**
```csharp
// Use case returns Result<T>
public async Task<Result<CreatePortfolioResult>> ExecuteAsync(CreatePortfolioCommand command)
{
    try
    {
        var portfolio = await _portfolioPort.CreatePortfolioAsync(...);
        var result = new CreatePortfolioResult(...);
        return Result<CreatePortfolioResult>.Success(result);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
    {
        return Result<CreatePortfolioResult>.Failure(
            Error.Conflict($"Portfolio with name '{command.Name}' already exists", "PORTFOLIO_NAME_CONFLICT"));
    }
}

// Caller handles Result<T>
var result = await CreatePortfolioUseCase.ExecuteAsync(command);
if (result.IsSuccess)
{
    // Access result.Value
}
else
{
    // Access result.Errors
}
```

### 2. BaseDataPage Result<T> Support
**Status:** ✅ Complete

**File:** `src/TradingStrat.Web/Components/Pages/BaseDataPage.cs`

**Key Changes:**
- `ExecuteOperationAsync()` returns `Task<Result<TResult>>` instead of `Task<TResult>`
- `HandleSubmitAsync()` automatically handles success/failure branches
- `FormatErrors()` provides consistent error message formatting
- Eliminates repetitive try-catch blocks across pages

**Benefits:**
- Consistent error handling across all pages
- Automatic error message formatting
- Reduces boilerplate code in Blazor pages
- Better separation of concerns (business logic returns Result, UI handles display)

### 3. Command Self-Validation
**Status:** ✅ Complete

**Pattern Established:**
All command objects validate parameters in their constructor using `ValidationGuard`. Invalid commands cannot be created.

**Example:**
```csharp
public record CreatePortfolioCommand
{
    public string Name { get; init; }
    public decimal InitialCash { get; init; }

    public CreatePortfolioCommand(string Name, decimal InitialCash)
    {
        // Validate parameters
        ValidationGuard.Require(Name).NotNullOrWhiteSpace();
        ValidationGuard.Require(InitialCash).GreaterThanOrEqual(0m, "Initial cash cannot be negative");

        // Assign validated values
        this.Name = Name.Trim();
        this.InitialCash = InitialCash;
    }
}
```

**Migrated Commands:**
- ✅ BacktestCommand (11 validations)
- ✅ CreatePortfolioCommand (2 validations)
- ✅ AddPositionCommand (5 validations)
- ✅ UpdatePositionCommand (3 validations)
- ✅ And many more...

**Documentation:** `docs/command-validation-pattern.md`

### 4. Standardized Progress Reporting
**Status:** ✅ Complete

**Files Created:**
- `src/TradingStrat.Domain/ValueObjects/UseCaseProgress.cs` - Standardized progress type
- `tests/TradingStrat.Domain.Tests/ValueObjects/UseCaseProgressTests.cs` - 14 comprehensive tests (all passing)

**Features:**
- Three factory methods:
  - `UseCaseProgress.Simple("message")` - Simple progress update
  - `UseCaseProgress.WithSteps("message", current, total)` - Step tracking with auto-calculated percentage
  - `UseCaseProgress.WithPercentage("message", percent)` - Explicit percentage
- Automatic percentage calculation from steps
- Consistent ToString() formatting
- Immutable record type for thread safety

**Example Usage:**
```csharp
// Simple progress
progress?.Report(UseCaseProgress.Simple("Fetching data..."));

// Step-based progress (auto-calculates percentage)
progress?.Report(UseCaseProgress.WithSteps("Processing batch", 5, 10)); // 50%

// Explicit percentage
progress?.Report(UseCaseProgress.WithPercentage("Analyzing", 75));
```

**Benefits:**
- Consistent progress reporting across all use cases
- UI can display progress bars from structured data
- Better user experience with accurate percentage tracking
- Type-safe progress updates

### 5. Use Case Migrations Completed
**Status:** Partial (1 of ~20 migrated)

**Migrated:**
- ✅ ICreatePortfolioUseCase → Returns `Result<CreatePortfolioResult>`
  - Implementation updated with try-catch and Result<T> wrapping
  - 10 tests updated and passing
  - Web UI (Portfolios.razor.cs) updated to handle Result<T>

**Pattern Established:**
```csharp
// 1. Update interface
Task<Result<CreatePortfolioResult>> ExecuteAsync(CreatePortfolioCommand command);

// 2. Update implementation
public async Task<Result<CreatePortfolioResult>> ExecuteAsync(CreatePortfolioCommand command)
{
    try
    {
        // Business logic
        return Result<CreatePortfolioResult>.Success(result);
    }
    catch (SpecificException ex)
    {
        return Result<CreatePortfolioResult>.Failure(Error.Type("message", "CODE"));
    }
}

// 3. Update tests
var result = await _useCase.ExecuteAsync(command);
result.IsSuccess.ShouldBeTrue();
result.Value.ShouldNotBeNull();

// 4. Update UI callers
var result = await UseCase.ExecuteAsync(command);
if (result.IsSuccess)
{
    // Success path
}
else
{
    _errorMessage = string.Join(", ", result.Errors.Select(e => e.Message));
}
```

## In Progress ⏳

### Result<T> Migration for Remaining Use Cases
**Remaining:** ~19 use cases

**Not Yet Migrated:**
- IBacktestUseCase
- ILiveAnalysisUseCase
- IDataFetchingUseCase
- IBulkDataFetchingUseCase
- IManagePositionsUseCase (3 methods: Add, Update, Delete)
- IManageCashUseCase
- IGetPortfolioSnapshotUseCase (already returns Result<T> ✓)
- ICalculateRebalancingUseCase
- IGetPortfolioPerformanceUseCase
- IDashboardStatsUseCase
- IRecentActivityUseCase
- ITopStrategiesUseCase
- IBacktestArchiveUseCase
- IMultiStrategyComparisonUseCase
- IAnalyzeStrategyUseCase
- ISendChatMessageUseCase
- ICustomStrategyManagementUseCase
- IOptimizeStrategyParametersUseCase
- ISaveBacktestRunUseCase

**Approach:**
Migrate incrementally, one use case at a time. Each migration involves:
1. Update interface signature
2. Update implementation with Result<T> wrapping
3. Update tests to assert on Result<T>
4. Update UI callers to handle Result<T>

**Estimated Effort:** 2-3 days (one use case every ~30 minutes)

## Success Metrics

**Before Phase 2:**
- Inconsistent error handling (mix of exceptions and return values)
- Progress reporting: 3 different types (string, BacktestProgress, BulkSaveProgress)
- Command validation: Mix of constructor validation and use case validation
- Error handling: Exception-based control flow

**After Phase 2:**
- ✅ Consistent error handling via Result<T> pattern
- ✅ Standardized progress reporting via UseCaseProgress
- ✅ All commands validate in constructor (Fail Fast principle)
- ⏳ All use cases return Result<T> (1 of ~20 complete)
- ✅ BaseDataPage supports Result<T> automatically
- ✅ Structured errors with error codes for programmatic handling

## Next Steps

1. **Complete Result<T> Migration** - Migrate remaining ~19 use cases
2. **Update BacktestProgress** - Convert to UseCaseProgress for consistency
3. **Update BulkSaveProgress** - Convert to UseCaseProgress for consistency
4. **Performance Testing** - Ensure Result<T> pattern doesn't impact performance
5. **Documentation** - Update CLAUDE.md with Result<T> and UseCaseProgress patterns

## References

- `docs/result-pattern-migration-guide.md` - Complete migration guide
- `docs/command-validation-pattern.md` - Command validation patterns
- `src/TradingStrat.Web/Components/Pages/BaseDataPage.cs` - Result<T> integration example
- `src/TradingStrat.Application/UseCases/CreatePortfolioUseCase.cs` - Migration example
