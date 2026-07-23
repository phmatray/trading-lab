# Result<T> Pattern Migration Guide

## Overview

This guide documents the migration of use cases from exception-based error handling to the Result<T> pattern. This migration is part of Phase 2 (Standardization & Consistency) of the TradingStrat refactoring plan.

## Why Result<T> Pattern?

**Before (Exception-based):**
- Use cases throw exceptions for error cases
- UI catches exceptions as strings
- No structured error information
- Exception-based control flow (anti-pattern)
- Difficult to distinguish between different error types

**After (Result<T> Pattern):**
- Use cases return `Result<T>` with Success/Failure states
- Structured error types (NotFound, Validation, BusinessRule, etc.)
- Error codes for programmatic handling
- Functional error handling without exceptions
- Clear separation between expected failures and unexpected exceptions

## Migration Example: GetPortfolioSnapshotUseCase

This use case was migrated as an example to establish the pattern. All other use cases should follow this template.

### Files Modified

1. **Interface** (`IGetPortfolioSnapshotUseCase.cs`)
2. **Implementation** (`GetPortfolioSnapshotUseCase.cs`)
3. **Tests** (`GetPortfolioSnapshotUseCaseTests.cs`)
4. **Dependent Use Cases** (use cases that call this one)
5. **UI Pages** (Blazor components that call this use case)

---

## Step 1: Update Use Case Interface

**Location:** `src/TradingStrat.Application/Ports/Inbound/IGetPortfolioSnapshotUseCase.cs`

**Before:**
```csharp
public interface IGetPortfolioSnapshotUseCase
{
    Task<PortfolioSnapshot> ExecuteAsync(
        int portfolioId,
        IProgress<string>? progress = null);
}
```

**After:**
```csharp
using TradingStrat.Domain.Common;

public interface IGetPortfolioSnapshotUseCase
{
    Task<Result<PortfolioSnapshot>> ExecuteAsync(
        int portfolioId,
        IProgress<string>? progress = null);
}
```

**Changes:**
- Add `using TradingStrat.Domain.Common;`
- Change return type from `Task<PortfolioSnapshot>` to `Task<Result<PortfolioSnapshot>>`

---

## Step 2: Update Use Case Implementation

**Location:** `src/TradingStrat.Application/UseCases/GetPortfolioSnapshotUseCase.cs`

**Before:**
```csharp
public async Task<PortfolioSnapshot> ExecuteAsync(...)
{
    // Load portfolio
    Portfolio? portfolio = await _portfolioPort.GetPortfolioByIdAsync(portfolioId);
    if (portfolio == null)
    {
        throw new InvalidOperationException($"Portfolio {portfolioId} not found");
    }

    // Fetch prices
    Result<Dictionary<string, decimal>> priceResult = await _priceService.GetCurrentPricesAsync(...);
    if (priceResult.IsFailure)
    {
        throw new InvalidOperationException($"Failed to fetch prices: {...}");
    }

    // Calculate snapshot
    Result<PortfolioSnapshot> result = _valuationService.CalculateSnapshot(...);
    return result.Value;  // Dangerous if result is failure!
}
```

**After:**
```csharp
using TradingStrat.Domain.Common;

public async Task<Result<PortfolioSnapshot>> ExecuteAsync(...)
{
    progress?.Report("Loading portfolio...");

    // Load portfolio - return failure instead of throwing
    Portfolio? portfolio = await _portfolioPort.GetPortfolioByIdAsync(portfolioId);
    if (portfolio == null)
    {
        return Result<PortfolioSnapshot>.Failure(
            Error.NotFound($"Portfolio {portfolioId} not found", "PORTFOLIO_NOT_FOUND"));
    }

    // Handle empty portfolio - return success with empty snapshot
    if (!portfolio.Positions.Any())
    {
        progress?.Report("Portfolio has no positions");
        PortfolioSnapshot emptySnapshot = new PortfolioSnapshot(...);
        return Result<PortfolioSnapshot>.Success(emptySnapshot);
    }

    progress?.Report("Fetching current market prices...");

    // Fetch prices - propagate failure instead of throwing
    Result<Dictionary<string, decimal>> priceResult = await _priceService.GetCurrentPricesAsync(...);
    if (priceResult.IsFailure)
    {
        return Result<PortfolioSnapshot>.Failure(priceResult.Errors);
    }

    progress?.Report("Calculating portfolio valuation...");

    // Calculate snapshot - return result directly
    Result<PortfolioSnapshot> result = _valuationService.CalculateSnapshot(...);

    if (result.IsSuccess)
    {
        progress?.Report("Portfolio snapshot complete");
    }

    return result;
}
```

**Key Patterns:**
1. **Not Found Errors:** Use `Error.NotFound(message, code)` with a specific error code
2. **Propagating Failures:** Return `Result<T>.Failure(otherResult.Errors)` to forward errors
3. **Empty Cases:** Return `Result<T>.Success(value)` for valid empty results
4. **Never throw exceptions** for business logic errors (only for unexpected system failures)

---

## Step 3: Update Tests

**Location:** `tests/TradingStrat.Application.Tests/UseCases/GetPortfolioSnapshotUseCaseTests.cs`

### Pattern 1: Success Cases

**Before:**
```csharp
[Fact]
public async Task ExecuteAsync_WithValidPortfolio_ReturnsSnapshot()
{
    // Arrange
    var portfolio = CreateTestPortfolio();
    A.CallTo(() => _portfolioPort.GetPortfolioByIdAsync(portfolio.Id))
        .Returns(portfolio);

    // Act
    PortfolioSnapshot snapshot = await _useCase.ExecuteAsync(portfolio.Id);

    // Assert
    snapshot.ShouldNotBeNull();
    snapshot.PortfolioId.ShouldBe(portfolio.Id);
}
```

**After:**
```csharp
[Fact]
public async Task ExecuteAsync_WithValidPortfolio_ReturnsSnapshot()
{
    // Arrange
    var portfolio = CreateTestPortfolio();
    A.CallTo(() => _portfolioPort.GetPortfolioByIdAsync(portfolio.Id))
        .Returns(portfolio);

    // Act
    Result<PortfolioSnapshot> result = await _useCase.ExecuteAsync(portfolio.Id);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    PortfolioSnapshot snapshot = result.Value;
    snapshot.ShouldNotBeNull();
    snapshot.PortfolioId.ShouldBe(portfolio.Id);
}
```

### Pattern 2: Error Cases

**Before:**
```csharp
[Fact]
public async Task ExecuteAsync_WithNonExistentPortfolio_ThrowsException()
{
    // Arrange
    A.CallTo(() => _portfolioPort.GetPortfolioByIdAsync(9999))
        .Returns<Portfolio?>(null);

    // Act & Assert
    InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
        async () => await _useCase.ExecuteAsync(9999));

    ex.Message.ShouldContain("Portfolio 9999 not found");
}
```

**After:**
```csharp
[Fact]
public async Task ExecuteAsync_WithNonExistentPortfolio_ReturnsNotFoundError()
{
    // Arrange
    A.CallTo(() => _portfolioPort.GetPortfolioByIdAsync(9999))
        .Returns<Portfolio?>(null);

    // Act
    Result<PortfolioSnapshot> result = await _useCase.ExecuteAsync(9999);

    // Assert
    result.IsFailure.ShouldBeTrue();
    result.Errors.ShouldHaveSingleItem();
    result.Errors[0].Type.ShouldBe(ErrorType.NotFound);
    result.Errors[0].Code.ShouldBe("PORTFOLIO_NOT_FOUND");
    result.Errors[0].Message.ShouldContain("Portfolio 9999 not found");
}
```

### Pattern 3: Propagated Errors

**Before:**
```csharp
[Fact]
public async Task ExecuteAsync_WhenPriceFetchFails_ThrowsException()
{
    // Arrange
    var portfolio = CreateTestPortfolio();
    A.CallTo(() => _portfolioPort.GetPortfolioByIdAsync(portfolio.Id))
        .Returns(portfolio);

    var priceError = Result<Dictionary<string, decimal>>.Failure(
        Error.Validation("Failed to fetch prices"));
    A.CallTo(() => _priceService.GetCurrentPricesAsync(...))
        .Returns(priceError);

    // Act & Assert
    InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
        async () => await _useCase.ExecuteAsync(portfolio.Id));

    ex.Message.ShouldContain("Failed to fetch prices");
}
```

**After:**
```csharp
[Fact]
public async Task ExecuteAsync_WhenPriceFetchFails_ReturnsFailure()
{
    // Arrange
    var portfolio = CreateTestPortfolio();
    A.CallTo(() => _portfolioPort.GetPortfolioByIdAsync(portfolio.Id))
        .Returns(portfolio);

    var priceError = Result<Dictionary<string, decimal>>.Failure(
        Error.Validation("Failed to fetch prices", "PRICE_FETCH_FAILED"));
    A.CallTo(() => _priceService.GetCurrentPricesAsync(...))
        .Returns(priceError);

    // Act
    Result<PortfolioSnapshot> result = await _useCase.ExecuteAsync(portfolio.Id);

    // Assert
    result.IsFailure.ShouldBeTrue();
    result.Errors.ShouldHaveSingleItem();
    result.Errors[0].Type.ShouldBe(ErrorType.Validation);
    result.Errors[0].Code.ShouldBe("PRICE_FETCH_FAILED");
}
```

---

## Step 4: Update Dependent Use Cases

Other use cases that call `GetPortfolioSnapshotUseCase` must be updated to handle the Result<T> return type.

**Examples:** `CalculateRebalancingUseCase`, `GetPortfolioPerformanceUseCase`

**Pattern:**
```csharp
// In dependent use case implementation
Result<PortfolioSnapshot> snapshotResult = await _snapshotUseCase.ExecuteAsync(
    command.PortfolioId, progress);

if (snapshotResult.IsFailure)
{
    // Option 1: Throw exception (if this use case hasn't been migrated yet)
    throw new InvalidOperationException(
        $"Failed to get portfolio snapshot: {string.Join(", ", snapshotResult.Errors.Select(e => e.Message))}");

    // Option 2: Return failure (if this use case is also migrated to Result<T>)
    // return Result<RebalancingPlan>.Failure(snapshotResult.Errors);
}

PortfolioSnapshot snapshot = snapshotResult.Value;
// Continue with business logic...
```

**Note:** Dependent use cases can remain exception-based during incremental migration. Eventually, they should also be migrated to return Result<T>.

---

## Step 5: Update UI Pages

Blazor pages that call the use case must be updated to handle the Result<T> return type.

**Examples:** `PortfolioDashboard.razor.cs`, `Rebalancing.razor.cs`

**Pattern:**
```csharp
private async Task LoadPortfolioSnapshot()
{
    _isLoading = true;
    _errorMessage = null;

    try
    {
        // ... load portfolio ...

        var progress = new Progress<string>(message =>
        {
            InvokeAsync(() => ProgressService.UpdateProgress(message));
        });

        // Call use case and handle Result<T>
        var snapshotResult = await GetSnapshotUseCase.ExecuteAsync(PortfolioId, progress);

        if (snapshotResult.IsFailure)
        {
            _errorMessage = $"Failed to load portfolio snapshot: {string.Join(", ", snapshotResult.Errors.Select(e => e.Message))}";
            return;
        }

        _snapshot = snapshotResult.Value;

        // Continue with UI logic...
    }
    catch (Exception ex)
    {
        // This catch block is now only for unexpected system errors
        _errorMessage = $"Unexpected error: {ex.Message}";
    }
    finally
    {
        _isLoading = false;
        await InvokeAsync(() => ProgressService.Reset());
    }
}
```

**Key Points:**
- Check `IsFailure` immediately after calling the use case
- Extract the value with `result.Value` only after confirming success
- The try/catch block now only catches unexpected system errors, not business logic errors

---

## Error Types Reference

The `Error` class provides factory methods for different error types:

```csharp
// Not found errors (HTTP 404 equivalent)
Error.NotFound("Portfolio 123 not found", "PORTFOLIO_NOT_FOUND")

// Validation errors (HTTP 400 equivalent)
Error.Validation("Ticker is required", "TICKER_REQUIRED")
Error.Validation("Price must be positive", "INVALID_PRICE")

// Business rule violations (HTTP 422 equivalent)
Error.BusinessRule("Cannot sell more shares than owned", "INSUFFICIENT_SHARES")
Error.BusinessRule("Target allocations must sum to 100%", "INVALID_ALLOCATION_SUM")

// Conflict errors (HTTP 409 equivalent)
Error.Conflict("Portfolio name already exists", "DUPLICATE_PORTFOLIO_NAME")

// Insufficient data (domain-specific)
Error.InsufficientData("Not enough historical data for backtest", "INSUFFICIENT_HISTORICAL_DATA")
```

**Error Code Conventions:**
- Use SCREAMING_SNAKE_CASE
- Be specific and descriptive
- Prefix with entity name for clarity (e.g., `PORTFOLIO_NOT_FOUND`)
- Use consistent naming patterns across similar errors

---

## Result<T> Extension Methods

The `ResultExtensions` class provides functional-style operations:

```csharp
// Map: Transform success value
Result<PortfolioSnapshot> snapshotResult = await GetSnapshotAsync(...);
Result<decimal> totalValueResult = snapshotResult.Map(s => s.TotalValue);

// Bind: Chain operations that return Result<T>
Result<PortfolioSnapshot> snapshotResult = await GetSnapshotAsync(...);
Result<RebalancingPlan> planResult = snapshotResult.Bind(snapshot =>
    CalculateRebalancingAsync(snapshot));

// Tap: Perform side effect on success (e.g., logging)
Result<PortfolioSnapshot> result = await GetSnapshotAsync(...);
result.Tap(snapshot => _logger.LogInformation("Loaded portfolio {Id}", snapshot.PortfolioId));

// TapError: Perform side effect on failure
result.TapError(errors => _logger.LogError("Failed: {Errors}", string.Join(", ", errors)));

// Match: Pattern matching for success/failure
string message = result.Match(
    onSuccess: snapshot => $"Portfolio value: {snapshot.TotalValue:C}",
    onFailure: errors => $"Error: {string.Join(", ", errors.Select(e => e.Message))}"
);
```

---

## Migration Checklist

For each use case to be migrated:

- [ ] Update interface to return `Task<Result<T>>`
- [ ] Update implementation to return Result<T> instead of throwing exceptions
- [ ] Replace all `throw` statements with `return Result<T>.Failure(...)`
- [ ] Add error codes to all error cases
- [ ] Update all tests:
  - [ ] Success cases: Assert `IsSuccess` and extract `.Value`
  - [ ] Error cases: Assert `IsFailure` and check error properties
- [ ] Update dependent use cases to handle Result<T>
- [ ] Update UI pages to handle Result<T>
- [ ] Run tests to verify migration
- [ ] Update documentation

---

## Benefits Achieved

1. **Type Safety:** Compiler enforces error handling (can't ignore Result<T>)
2. **Consistency:** All use cases follow the same error handling pattern
3. **Structured Errors:** Rich error information with types, codes, and messages
4. **Testability:** Easy to test both success and failure paths
5. **Performance:** No exception overhead for expected failures
6. **Clarity:** Explicit success/failure in method signatures
7. **Maintainability:** Centralized error handling patterns

---

## Next Steps

After completing the Result<T> migration for all use cases:

1. Migrate `BaseDataPage<TFormModel, TResult>` to handle Result<T> automatically
2. Add command self-validation (commands validate in constructor)
3. Standardize progress reporting with `UseCaseProgress` record
4. Consider adding Result<T> extension methods for async operations
5. Add Result<T> guidelines to project documentation

---

## Common Pitfalls

### ❌ Mixing exceptions and Result<T>

```csharp
// Bad: Throwing exception in Result<T>-based use case
if (portfolio == null)
{
    throw new InvalidOperationException("Not found");
}
```

### ✅ Use Result<T> consistently

```csharp
// Good: Return failure
if (portfolio == null)
{
    return Result<PortfolioSnapshot>.Failure(
        Error.NotFound("Portfolio not found", "PORTFOLIO_NOT_FOUND"));
}
```

### ❌ Accessing .Value without checking IsSuccess

```csharp
// Bad: Can throw if result is failure
Result<PortfolioSnapshot> result = await GetSnapshotAsync(...);
var value = result.Value;  // Dangerous!
```

### ✅ Always check IsSuccess first

```csharp
// Good: Check before accessing
Result<PortfolioSnapshot> result = await GetSnapshotAsync(...);
if (result.IsFailure)
{
    return Result<RebalancingPlan>.Failure(result.Errors);
}
var snapshot = result.Value;  // Safe
```

### ❌ Losing error information

```csharp
// Bad: Generic error message
if (snapshotResult.IsFailure)
{
    throw new Exception("Failed to get snapshot");
}
```

### ✅ Preserve error details

```csharp
// Good: Forward all error information
if (snapshotResult.IsFailure)
{
    return Result<RebalancingPlan>.Failure(snapshotResult.Errors);
}
```

---

---

## BaseDataPage Pattern with Result<T>

The `BaseDataPage<TFormModel, TResult>` abstract base class has been updated to automatically handle Result<T> return types. Pages that inherit from BaseDataPage get automatic Result<T> error handling without additional code.

### BaseDataPage Features

**Automatic Handling:**
- Extracts the value from successful results
- Formats and displays error messages from failed results
- Differentiates between business logic errors (Result<T>.Failure) and unexpected system errors (exceptions)
- Manages progress reporting, form state persistence, and lifecycle

**Updated Signature:**
```csharp
protected abstract Task<Result<TResult>> ExecuteOperationAsync(
    TFormModel model,
    IProgress<string> progress);
```

### Example: Backtest Page

**Before Migration:**
```csharp
public partial class Backtest : BaseDataPage<BacktestFormModel, BacktestResult>
{
    protected override async Task<BacktestResult> ExecuteOperationAsync(
        BacktestFormModel model,
        IProgress<string> progress)
    {
        // ... execute backtest ...
        return result;
    }
}
```

**After Migration:**
```csharp
using TradingStrat.Domain.Common;

public partial class Backtest : BaseDataPage<BacktestFormModel, BacktestResult>
{
    protected override async Task<Result<BacktestResult>> ExecuteOperationAsync(
        BacktestFormModel model,
        IProgress<string> progress)
    {
        // ... execute backtest ...
        return Result<BacktestResult>.Success(result);
    }
}
```

**Error Handling Example:**
```csharp
protected override async Task<Result<BacktestResult>> ExecuteOperationAsync(
    BacktestFormModel model,
    IProgress<string> progress)
{
    if (string.IsNullOrWhiteSpace(model.Ticker))
    {
        return Result<BacktestResult>.Failure(
            Error.Validation("Ticker is required", "TICKER_REQUIRED"));
    }

    try
    {
        BacktestResult result = await BacktestUseCase.ExecuteAsync(command, progress);
        return Result<BacktestResult>.Success(result);
    }
    catch (Exception ex)
    {
        return Result<BacktestResult>.Failure(
            Error.BusinessRule($"Backtest failed: {ex.Message}", "BACKTEST_EXECUTION_FAILED"));
    }
}
```

**What BaseDataPage Does Automatically:**
1. Calls `ExecuteOperationAsync()` when form is submitted
2. If `result.IsSuccess`:
   - Extracts the value: `Result = result.Value`
   - Displays success message via `GetSuccessMessage()`
   - Persists form state to localStorage
3. If `result.IsFailure`:
   - Formats errors via `FormatErrors(result.Errors)`
   - Displays error message to user
   - Does NOT persist form state (keeps user's input)
4. Catches unexpected exceptions (system errors)
5. Always resets progress indicator in `finally` block

**Custom Error Formatting:**
```csharp
protected override string FormatErrors(IReadOnlyList<Error> errors)
{
    // Custom formatting for backtest errors
    if (errors.Any(e => e.Code == "INSUFFICIENT_DATA"))
    {
        return "Not enough historical data. Please fetch data first.";
    }

    // Fallback to default formatting
    return base.FormatErrors(errors);
}
```

### Pages Using BaseDataPage

Three pages currently use BaseDataPage with Result<T>:
1. **Backtest.razor.cs** - Executes backtests and auto-saves to archive
2. **LiveAnalysis.razor.cs** - Performs live market analysis with ML predictions
3. **Comparison.razor.cs** - Compares two strategy variants side-by-side

**Note:** Five more pages are identified for migration to BaseDataPage in the refactoring plan:
- DataManagement.razor.cs
- StrategyOptimization.razor.cs
- StrategyComparison.razor.cs (possibly same as Comparison)

---

## Summary

The Result<T> pattern migration establishes a consistent, type-safe approach to error handling across the TradingStrat application. By following this guide, all use cases can be migrated incrementally without breaking existing functionality, ultimately leading to a more maintainable and robust codebase.

**Migration Status:**
- **Use Cases:** 1 of 24 migrated (GetPortfolioSnapshotUseCase)
- **BaseDataPage:** Updated to handle Result<T> automatically ✅
- **Pages Using BaseDataPage:** 3 pages updated (Backtest, LiveAnalysis, Comparison) ✅

**Priority for Next Migration:**
1. Use cases called frequently from UI (high impact)
2. Use cases with complex error scenarios (high benefit)
3. Use cases that depend on already-migrated use cases (easier migration)
4. Migrate remaining pages to use BaseDataPage pattern (eliminates 500+ lines of boilerplate)
