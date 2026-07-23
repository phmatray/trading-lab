# Command Self-Validation Pattern

## Overview

This document describes the command self-validation pattern implemented in TradingStrat, where commands validate their parameters in constructors to ensure only valid commands can be instantiated.

## Why Command Self-Validation?

**Before (Validation in Use Cases):**
- Validation logic scattered across multiple use cases
- Same validation duplicated for each command parameter
- Possible to create invalid commands
- Use cases mixing validation with business logic
- Easy to forget validation checks

**After (Validation in Commands):**
- Validation centralized in command constructors
- Impossible to create invalid commands (fail-fast principle)
- Use cases focus purely on business logic
- Single source of truth for parameter requirements
- Compiler enforces validation through construction

## Pattern Structure

### Record with Explicit Constructor

Commands are implemented as records with properties and an explicit constructor that validates all parameters:

```csharp
using TradingStrat.Domain.Common;

public record MyCommand
{
    // Properties with init-only setters
    public int Id { get; init; }
    public string Name { get; init; }
    public decimal Amount { get; init; }

    // Constructor validates and assigns
    public MyCommand(
        int Id,
        string Name,
        decimal Amount = 0m)
    {
        // Validate parameters using ValidationGuard
        ValidationGuard.Require(Id).GreaterThan(0, "ID must be positive");
        ValidationGuard.Require(Name).NotNullOrWhiteSpace();
        ValidationGuard.Require(Amount).GreaterThanOrEqual(0m, "Amount cannot be negative");

        // Normalize/transform data if needed
        this.Id = Id;
        this.Name = Name.Trim().ToUpperInvariant();
        this.Amount = Amount;
    }
}
```

## ValidationGuard API

The `ValidationGuard` class provides fluent validation methods:

```csharp
// Basic null check
ValidationGuard.Require(value).NotNull();

// String validation
ValidationGuard.Require(ticker).NotNullOrWhiteSpace();

// Numeric comparisons
ValidationGuard.Require(quantity).GreaterThan(0);
ValidationGuard.Require(quantity).GreaterThanOrEqual(0);
ValidationGuard.Require(price).LessThan(1000m);
ValidationGuard.Require(price).LessThanOrEqual(1000m);

// Range validation
ValidationGuard.Require(percentage).InRange(0m, 100m);

// Custom conditions
ValidationGuard.Require(condition, "Error message", "parameterName");
ValidationGuard.Require(value).Satisfies(condition, "Error message");

// Custom messages
ValidationGuard.Require(amount).GreaterThan(0m, "Custom error message");
```

## Migrated Commands

### Portfolio Management Commands

**1. AddPositionCommand** - Adds a position to a portfolio
```csharp
public record AddPositionCommand
{
    public int PortfolioId { get; init; }
    public string Ticker { get; init; }
    public int Quantity { get; init; }
    public decimal EntryPrice { get; init; }
    public DateTime EntryDate { get; init; }
    public string? Notes { get; init; }

    public AddPositionCommand(...)
    {
        ValidationGuard.Require(PortfolioId).GreaterThan(0, "Portfolio ID must be positive");
        ValidationGuard.Require(Ticker).NotNullOrWhiteSpace();
        ValidationGuard.Require(Quantity).GreaterThan(0, "Quantity must be positive");
        ValidationGuard.Require(EntryPrice).GreaterThan(0m, "Entry price must be positive");
        ValidationGuard.Require(EntryDate).LessThanOrEqual(DateTime.Today, "Entry date cannot be in the future");

        this.PortfolioId = PortfolioId;
        this.Ticker = Ticker.ToUpperInvariant().Trim();  // Normalization
        this.Quantity = Quantity;
        this.EntryPrice = EntryPrice;
        this.EntryDate = EntryDate;
        this.Notes = Notes;
    }
}
```

**Validations:**
- PortfolioId > 0
- Ticker not empty
- Quantity > 0
- EntryPrice > 0
- EntryDate ≤ Today
- Ticker normalized (uppercase, trimmed)

**2. UpdatePositionCommand** - Updates an existing position
```csharp
public record UpdatePositionCommand
{
    public int PositionId { get; init; }
    public int Quantity { get; init; }
    public decimal EntryPrice { get; init; }
    public string? Notes { get; init; }

    public UpdatePositionCommand(...)
    {
        ValidationGuard.Require(PositionId).GreaterThan(0, "Position ID must be positive");
        ValidationGuard.Require(Quantity).GreaterThan(0, "Quantity must be positive");
        ValidationGuard.Require(EntryPrice).GreaterThan(0m, "Entry price must be positive");

        this.PositionId = PositionId;
        this.Quantity = Quantity;
        this.EntryPrice = EntryPrice;
        this.Notes = Notes;
    }
}
```

**Validations:**
- PositionId > 0
- Quantity > 0
- EntryPrice > 0

**3. CashTransactionCommand** - Deposits or withdraws cash
```csharp
public record CashTransactionCommand
{
    public int PortfolioId { get; init; }
    public TransactionType Type { get; init; }
    public decimal Amount { get; init; }
    public string? Notes { get; init; }

    public CashTransactionCommand(...)
    {
        ValidationGuard.Require(PortfolioId).GreaterThan(0, "Portfolio ID must be positive");
        ValidationGuard.Require(Amount).GreaterThan(0m, "Transaction amount must be positive");

        this.PortfolioId = PortfolioId;
        this.Type = Type;
        this.Amount = Amount;
        this.Notes = Notes;
    }
}
```

**Validations:**
- PortfolioId > 0
- Amount > 0

### Backtest & Analysis Commands

**4. BacktestCommand** - Executes a strategy backtest
```csharp
public record BacktestCommand
{
    public string Ticker { get; init; }
    public StrategyType StrategyType { get; init; }
    public Dictionary<string, object>? StrategyParameters { get; init; }
    public decimal InitialCapital { get; init; }
    public decimal CommissionPercentage { get; init; }
    public decimal MinimumCommission { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public TimeFrame? TimeFrame { get; init; }
    public TradingStyle? TradingStyle { get; init; }
    public int? CustomStrategyId { get; init; }

    public BacktestCommand(...)
    {
        ValidationGuard.Require(Ticker).NotNullOrWhiteSpace();
        ValidationGuard.Require(InitialCapital).GreaterThan(0m, "Initial capital must be positive");
        ValidationGuard.Require(CommissionPercentage).GreaterThanOrEqual(0m, "Commission percentage cannot be negative");
        ValidationGuard.Require(CommissionPercentage).LessThan(1m, "Commission percentage must be less than 100%");
        ValidationGuard.Require(MinimumCommission).GreaterThanOrEqual(0m, "Minimum commission cannot be negative");

        // Date range validation
        if (StartDate.HasValue && EndDate.HasValue)
        {
            ValidationGuard.Require(StartDate.Value <= EndDate.Value,
                "Start date must be before or equal to end date",
                nameof(StartDate));
        }

        if (EndDate.HasValue)
        {
            ValidationGuard.Require(EndDate.Value <= DateTime.Today,
                "End date cannot be in the future",
                nameof(EndDate));
        }

        if (CustomStrategyId.HasValue)
        {
            ValidationGuard.Require(CustomStrategyId.Value).GreaterThan(0, "Custom strategy ID must be positive");
        }

        this.Ticker = Ticker.ToUpperInvariant().Trim();
        this.StrategyType = StrategyType;
        this.StrategyParameters = StrategyParameters;
        this.InitialCapital = InitialCapital;
        this.CommissionPercentage = CommissionPercentage;
        this.MinimumCommission = MinimumCommission;
        this.StartDate = StartDate;
        this.EndDate = EndDate;
        this.TimeFrame = TimeFrame;
        this.TradingStyle = TradingStyle;
        this.CustomStrategyId = CustomStrategyId;
    }
}
```

**Validations:**
- Ticker not empty
- InitialCapital > 0
- 0 ≤ CommissionPercentage < 1
- MinimumCommission ≥ 0
- StartDate ≤ EndDate (if both provided)
- EndDate ≤ Today (if provided)
- CustomStrategyId > 0 (if provided)
- Ticker normalized (uppercase, trimmed)

**5. AnalysisCommand** - Analyzes current market position
```csharp
public record AnalysisCommand
{
    public string Ticker { get; init; }
    public PredictionThresholds? Thresholds { get; init; }
    public bool FetchFreshData { get; init; }
    public TimeFrame? TimeFrame { get; init; }

    public AnalysisCommand(...)
    {
        ValidationGuard.Require(Ticker).NotNullOrWhiteSpace();

        this.Ticker = Ticker.ToUpperInvariant().Trim();
        this.Thresholds = Thresholds;
        this.FetchFreshData = FetchFreshData;
        this.TimeFrame = TimeFrame;
    }
}
```

**Validations:**
- Ticker not empty
- Ticker normalized (uppercase, trimmed)

## Use Case Changes

### Before Migration

```csharp
public async Task<Position> AddPositionAsync(AddPositionCommand command)
{
    // Validation scattered in use case
    if (string.IsNullOrWhiteSpace(command.Ticker))
        throw new ArgumentException("Ticker is required", nameof(command));

    if (command.Quantity <= 0)
        throw new ArgumentException("Quantity must be positive", nameof(command));

    if (command.EntryPrice <= 0)
        throw new ArgumentException("Entry price must be positive", nameof(command));

    // Business logic
    var portfolio = await _portfolioPort.GetPortfolioByIdAsync(command.PortfolioId);
    if (portfolio == null)
        throw new InvalidOperationException($"Portfolio {command.PortfolioId} not found");

    var position = new Position { ... };
    return await _portfolioPort.AddPositionAsync(position);
}
```

### After Migration

```csharp
public async Task<Position> AddPositionAsync(AddPositionCommand command)
{
    // Command is guaranteed to be valid here - no validation needed!

    // Business rule: verify portfolio exists
    var portfolio = await _portfolioPort.GetPortfolioByIdAsync(command.PortfolioId);
    if (portfolio == null)
        throw new InvalidOperationException($"Portfolio {command.PortfolioId} not found");

    // Business logic (Ticker already normalized by command)
    var position = new Position
    {
        PortfolioId = command.PortfolioId,
        Ticker = command.Ticker,  // Already normalized
        Quantity = command.Quantity,
        EntryPrice = command.EntryPrice,
        EntryDate = command.EntryDate,
        Notes = command.Notes
    };

    return await _portfolioPort.AddPositionAsync(position);
}
```

## Benefits

### 1. Fail-Fast Principle
Invalid commands cannot be instantiated - validation happens at construction time:

```csharp
// This will throw ArgumentException immediately
var command = new AddPositionCommand(
    PortfolioId: -1,  // Invalid!
    Ticker: "",       // Invalid!
    Quantity: 0,      // Invalid!
    EntryPrice: -10m, // Invalid!
    EntryDate: DateTime.Today.AddDays(10), // Invalid!
    Notes: null
);
```

### 2. Single Responsibility
- Commands: Validate and hold data
- Use Cases: Execute business logic
- Clear separation of concerns

### 3. No Duplication
Validation logic exists in exactly one place - the command constructor.

### 4. Type Safety
The compiler enforces creating commands through the validated constructor.

### 5. Data Normalization
Commands can normalize data (uppercase tickers, trim strings) in one place.

### 6. Better Error Messages
ValidationGuard provides clear, contextual error messages with parameter names automatically captured.

## Migration Checklist

For each command to migrate:

- [ ] Add `using TradingStrat.Domain.Common;`
- [ ] Convert positional record to record with explicit constructor
- [ ] Add properties with `{ get; init; }` setters
- [ ] Create constructor with same signature as original record
- [ ] Add validation using `ValidationGuard.Require(...)`
- [ ] Normalize/transform data if needed (uppercase tickers, etc.)
- [ ] Assign validated values to properties
- [ ] Remove validation from corresponding use case
- [ ] Add comment in use case: "Command is guaranteed to be valid here"
- [ ] Update tests to expect validation in constructor (not use case)
- [ ] Build and verify tests pass

## Recently Migrated Commands (Phase 2.3 Extension)

**6. CreatePortfolioCommand** - Creates a new portfolio
```csharp
public record CreatePortfolioCommand
{
    public string Name { get; init; }
    public string? Description { get; init; }
    public decimal InitialCash { get; init; }

    public CreatePortfolioCommand(
        string Name,
        string? Description = null,
        decimal InitialCash = 0m)
    {
        ValidationGuard.Require(Name).NotNullOrWhiteSpace();
        ValidationGuard.Require(InitialCash).GreaterThanOrEqual(0m, "Initial cash cannot be negative");

        this.Name = Name.Trim();
        this.Description = Description?.Trim();
        this.InitialCash = InitialCash;
    }
}
```

**Validations:**
- Name not empty
- InitialCash ≥ 0
- Name normalized (trimmed)

**7. FetchDataCommand** - Fetches historical market data
```csharp
public record FetchDataCommand
{
    public string Ticker { get; init; }
    public string? Isin { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public TimeFrame? TimeFrame { get; init; }

    public FetchDataCommand(
        string Ticker,
        string? Isin = null,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        TimeFrame? TimeFrame = null)
    {
        ValidationGuard.Require(Ticker).NotNullOrWhiteSpace();

        if (StartDate.HasValue && EndDate.HasValue)
        {
            ValidationGuard.Require(StartDate.Value <= EndDate.Value,
                "Start date must be before or equal to end date",
                nameof(StartDate));
        }

        if (EndDate.HasValue)
        {
            ValidationGuard.Require(EndDate.Value <= DateTime.Today,
                "End date cannot be in the future",
                nameof(EndDate));
        }

        if (StartDate.HasValue)
        {
            ValidationGuard.Require(StartDate.Value <= DateTime.Today,
                "Start date cannot be in the future",
                nameof(StartDate));
        }

        this.Ticker = Ticker.ToUpperInvariant().Trim();
        this.Isin = Isin?.ToUpperInvariant().Trim();
        this.StartDate = StartDate;
        this.EndDate = EndDate;
        this.TimeFrame = TimeFrame;
    }
}
```

**Validations:**
- Ticker not empty
- StartDate ≤ EndDate (if both provided)
- StartDate ≤ Today (if provided)
- EndDate ≤ Today (if provided)
- Ticker and ISIN normalized (uppercase, trimmed)

**8. BulkFetchDataCommand** - Fetches data for multiple tickers
```csharp
public record BulkFetchDataCommand
{
    public List<string> Tickers { get; init; }
    public TimeFrame TimeFrame { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public bool SkipExisting { get; init; }

    public BulkFetchDataCommand(
        List<string> Tickers,
        TimeFrame TimeFrame,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        bool SkipExisting = false)
    {
        ValidationGuard.Require(Tickers).NotNull();
        ValidationGuard.Require(Tickers.Count > 0, "Tickers list cannot be empty", nameof(Tickers));

        foreach (string ticker in Tickers)
        {
            ValidationGuard.Require(ticker).NotNullOrWhiteSpace();
        }

        if (StartDate.HasValue && EndDate.HasValue)
        {
            ValidationGuard.Require(StartDate.Value <= EndDate.Value,
                "Start date must be before or equal to end date",
                nameof(StartDate));
        }

        if (EndDate.HasValue)
        {
            ValidationGuard.Require(EndDate.Value <= DateTime.Today,
                "End date cannot be in the future",
                nameof(EndDate));
        }

        if (StartDate.HasValue)
        {
            ValidationGuard.Require(StartDate.Value <= DateTime.Today,
                "Start date cannot be in the future",
                nameof(StartDate));
        }

        this.Tickers = Tickers.Select(t => t.ToUpperInvariant().Trim()).ToList();
        this.TimeFrame = TimeFrame;
        this.StartDate = StartDate;
        this.EndDate = EndDate;
        this.SkipExisting = SkipExisting;
    }
}
```

**Validations:**
- Tickers not null and not empty
- Each ticker not empty
- Date range validation (same as FetchDataCommand)
- Tickers normalized (uppercase, trimmed)

**9. RebalancingCommand** - Calculates portfolio rebalancing
```csharp
public record RebalancingCommand
{
    public int PortfolioId { get; init; }
    public AllocationWeights TargetWeights { get; init; }
    public decimal CommissionPercentage { get; init; }
    public decimal MinimumCommission { get; init; }

    public RebalancingCommand(
        int PortfolioId,
        AllocationWeights TargetWeights,
        decimal CommissionPercentage,
        decimal MinimumCommission)
    {
        ValidationGuard.Require(PortfolioId).GreaterThan(0, "Portfolio ID must be positive");
        ValidationGuard.Require(TargetWeights).NotNull();
        ValidationGuard.Require(CommissionPercentage).GreaterThanOrEqual(0m, "Commission percentage cannot be negative");
        ValidationGuard.Require(CommissionPercentage).LessThan(1m, "Commission percentage must be less than 100%");
        ValidationGuard.Require(MinimumCommission).GreaterThanOrEqual(0m, "Minimum commission cannot be negative");

        this.PortfolioId = PortfolioId;
        this.TargetWeights = TargetWeights;
        this.CommissionPercentage = CommissionPercentage;
        this.MinimumCommission = MinimumCommission;
    }
}
```

**Validations:**
- PortfolioId > 0
- TargetWeights not null
- 0 ≤ CommissionPercentage < 1
- MinimumCommission ≥ 0

**10. CreateCustomStrategyCommand** - Creates a custom strategy
```csharp
public record CreateCustomStrategyCommand
{
    public string Name { get; init; }
    public string Description { get; init; }
    public string Author { get; init; }
    public string Category { get; init; }
    public StrategyDefinition Definition { get; init; }

    public CreateCustomStrategyCommand(
        string Name,
        string Description,
        string Author,
        string Category,
        StrategyDefinition Definition)
    {
        ValidationGuard.Require(Name).NotNullOrWhiteSpace();
        ValidationGuard.Require(Description).NotNullOrWhiteSpace();
        ValidationGuard.Require(Author).NotNullOrWhiteSpace();
        ValidationGuard.Require(Category).NotNullOrWhiteSpace();
        ValidationGuard.Require(Definition).NotNull();

        this.Name = Name.Trim();
        this.Description = Description.Trim();
        this.Author = Author.Trim();
        this.Category = Category.Trim();
        this.Definition = Definition;
    }
}
```

**Validations:**
- All string fields not empty
- Definition not null
- All strings normalized (trimmed)

**11. UpdateCustomStrategyCommand** - Updates an existing custom strategy
```csharp
public record UpdateCustomStrategyCommand
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public string Category { get; init; }
    public StrategyDefinition Definition { get; init; }

    public UpdateCustomStrategyCommand(
        int Id,
        string Name,
        string Description,
        string Category,
        StrategyDefinition Definition)
    {
        ValidationGuard.Require(Id).GreaterThan(0, "Strategy ID must be positive");
        ValidationGuard.Require(Name).NotNullOrWhiteSpace();
        ValidationGuard.Require(Description).NotNullOrWhiteSpace();
        ValidationGuard.Require(Category).NotNullOrWhiteSpace();
        ValidationGuard.Require(Definition).NotNull();

        this.Id = Id;
        this.Name = Name.Trim();
        this.Description = Description.Trim();
        this.Category = Category.Trim();
        this.Definition = Definition;
    }
}
```

**Validations:**
- Id > 0
- All string fields not empty
- Definition not null
- All strings normalized (trimmed)

**12. OptimizeParametersCommand** - Optimizes custom strategy parameters
```csharp
public record OptimizeParametersCommand
{
    public int CustomStrategyId { get; init; }
    public OptimizationType Type { get; init; }
    public Dictionary<string, ParameterRange> ParameterRanges { get; init; }
    public OptimizationObjective Objective { get; init; }
    public BacktestConfig BacktestSettings { get; init; }
    public GeneticAlgorithmSettings? GeneticSettings { get; init; }

    public OptimizeParametersCommand(
        int CustomStrategyId,
        OptimizationType Type,
        Dictionary<string, ParameterRange> ParameterRanges,
        OptimizationObjective Objective,
        BacktestConfig BacktestSettings,
        GeneticAlgorithmSettings? GeneticSettings = null)
    {
        ValidationGuard.Require(CustomStrategyId).GreaterThan(0, "Custom strategy ID must be positive");
        ValidationGuard.Require(ParameterRanges).NotNull();
        ValidationGuard.Require(ParameterRanges.Count > 0, "Parameter ranges cannot be empty", nameof(ParameterRanges));
        ValidationGuard.Require(BacktestSettings).NotNull();

        if (Type == OptimizationType.Genetic)
        {
            ValidationGuard.Require(GeneticSettings).NotNull();
        }

        this.CustomStrategyId = CustomStrategyId;
        this.Type = Type;
        this.ParameterRanges = ParameterRanges;
        this.Objective = Objective;
        this.BacktestSettings = BacktestSettings;
        this.GeneticSettings = GeneticSettings;
    }
}
```

**Validations:**
- CustomStrategyId > 0
- ParameterRanges not null and not empty
- BacktestSettings not null
- GeneticSettings required if Type == Genetic

**13. SendChatMessageCommand** - Sends a message to the AI trading assistant
```csharp
public record SendChatMessageCommand
{
    public string UserMessage { get; init; }
    public string? Ticker { get; init; }
    public string? SessionId { get; init; }

    public SendChatMessageCommand(
        string UserMessage,
        string? Ticker = null,
        string? SessionId = null)
    {
        ValidationGuard.Require(UserMessage).NotNullOrWhiteSpace();

        this.UserMessage = UserMessage.Trim();
        this.Ticker = Ticker?.ToUpperInvariant().Trim();
        this.SessionId = SessionId?.Trim();
    }
}
```

**Validations:**
- UserMessage not empty
- UserMessage normalized (trimmed)
- Ticker normalized (uppercase, trimmed) if provided
- SessionId normalized (trimmed) if provided

**14. AnalyzeStrategyCommand** - Requests AI-powered strategy analysis
```csharp
public record AnalyzeStrategyCommand
{
    public string Ticker { get; init; }
    public StrategyType StrategyType { get; init; }
    public Dictionary<string, object>? StrategyParameters { get; init; }

    public AnalyzeStrategyCommand(
        string Ticker,
        StrategyType StrategyType,
        Dictionary<string, object>? StrategyParameters = null)
    {
        ValidationGuard.Require(Ticker).NotNullOrWhiteSpace();

        this.Ticker = Ticker.ToUpperInvariant().Trim();
        this.StrategyType = StrategyType;
        this.StrategyParameters = StrategyParameters;
    }
}
```

**Validations:**
- Ticker not empty
- Ticker normalized (uppercase, trimmed)

**15. ParameterOptimizationCommand** - Runs A/B parameter optimization tests
```csharp
public record ParameterOptimizationCommand
{
    public string Ticker { get; init; }
    public StrategyVariant VariantA { get; init; }
    public StrategyVariant VariantB { get; init; }
    public decimal InitialCapital { get; init; }
    public decimal CommissionPercentage { get; init; }
    public decimal MinimumCommission { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public TimeFrame? TimeFrame { get; init; }

    public ParameterOptimizationCommand(
        string Ticker,
        StrategyVariant VariantA,
        StrategyVariant VariantB,
        decimal InitialCapital = 10000m,
        decimal CommissionPercentage = 0.001m,
        decimal MinimumCommission = 1.0m,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        TimeFrame? TimeFrame = null)
    {
        ValidationGuard.Require(Ticker).NotNullOrWhiteSpace();
        ValidationGuard.Require(VariantA).NotNull();
        ValidationGuard.Require(VariantB).NotNull();
        ValidationGuard.Require(InitialCapital).GreaterThan(0m, "Initial capital must be positive");
        ValidationGuard.Require(CommissionPercentage).GreaterThanOrEqual(0m, "Commission percentage cannot be negative");
        ValidationGuard.Require(CommissionPercentage).LessThan(1m, "Commission percentage must be less than 100%");
        ValidationGuard.Require(MinimumCommission).GreaterThanOrEqual(0m, "Minimum commission cannot be negative");

        if (StartDate.HasValue && EndDate.HasValue)
        {
            ValidationGuard.Require(StartDate.Value <= EndDate.Value,
                "Start date must be before or equal to end date",
                nameof(StartDate));
        }

        if (EndDate.HasValue)
        {
            ValidationGuard.Require(EndDate.Value <= DateTime.Today,
                "End date cannot be in the future",
                nameof(EndDate));
        }

        this.Ticker = Ticker.ToUpperInvariant().Trim();
        this.VariantA = VariantA;
        this.VariantB = VariantB;
        this.InitialCapital = InitialCapital;
        this.CommissionPercentage = CommissionPercentage;
        this.MinimumCommission = MinimumCommission;
        this.StartDate = StartDate;
        this.EndDate = EndDate;
        this.TimeFrame = TimeFrame;
    }
}
```

**Validations:**
- Ticker not empty
- VariantA and VariantB not null
- InitialCapital > 0
- 0 ≤ CommissionPercentage < 1
- MinimumCommission ≥ 0
- StartDate ≤ EndDate (if both provided)
- EndDate ≤ Today (if provided)
- Ticker normalized (uppercase, trimmed)

## Common Patterns

### Optional Parameters with Validation

```csharp
public MyCommand(
    int RequiredId,
    string? OptionalName = null,
    decimal? OptionalAmount = null)
{
    ValidationGuard.Require(RequiredId).GreaterThan(0);

    // Validate optional parameters only if provided
    if (OptionalAmount.HasValue)
    {
        ValidationGuard.Require(OptionalAmount.Value).GreaterThan(0m);
    }

    this.RequiredId = RequiredId;
    this.OptionalName = OptionalName?.Trim();
    this.OptionalAmount = OptionalAmount;
}
```

### Conditional Validation

```csharp
public BacktestCommand(...)
{
    // Validate date range only if both dates are provided
    if (StartDate.HasValue && EndDate.HasValue)
    {
        ValidationGuard.Require(StartDate.Value <= EndDate.Value,
            "Start date must be before or equal to end date",
            nameof(StartDate));
    }

    // ...
}
```

### Data Normalization

```csharp
public MyCommand(string Ticker, ...)
{
    ValidationGuard.Require(Ticker).NotNullOrWhiteSpace();

    // Normalize ticker: uppercase and trim
    this.Ticker = Ticker.ToUpperInvariant().Trim();
}
```

## Testing

### Testing Valid Commands

```csharp
[Fact]
public async Task UseCase_WithValidCommand_Succeeds()
{
    // Arrange - command creation validates automatically
    var command = new AddPositionCommand(
        PortfolioId: 1,
        Ticker: "AAPL",
        Quantity: 100,
        EntryPrice: 150.50m,
        EntryDate: DateTime.Today,
        Notes: null);

    // Act
    var result = await _useCase.ExecuteAsync(command);

    // Assert
    result.ShouldNotBeNull();
    result.Ticker.ShouldBe("AAPL");
}
```

### Testing Invalid Commands

```csharp
[Fact]
public void Command_WithInvalidQuantity_ThrowsArgumentException()
{
    // Act & Assert - validation happens in constructor
    var ex = Should.Throw<ArgumentException>(() =>
        new AddPositionCommand(
            PortfolioId: 1,
            Ticker: "AAPL",
            Quantity: 0,  // Invalid!
            EntryPrice: 150.50m,
            EntryDate: DateTime.Today,
            Notes: null));

    ex.Message.ShouldContain("Quantity must be positive");
    ex.ParamName.ShouldBe("Quantity");
}
```

## Summary

The command self-validation pattern provides:
- ✅ Fail-fast validation (invalid commands cannot exist)
- ✅ Single source of truth for parameter requirements
- ✅ Cleaner use cases focused on business logic
- ✅ Better error messages with automatic parameter names
- ✅ Data normalization in one place
- ✅ Type safety enforced by the compiler

**Migration Status:**
- Portfolio Commands: 4/4 migrated ✅ (AddPosition, UpdatePosition, CashTransaction, CreatePortfolio)
- Backtest/Analysis Commands: 2/2 migrated ✅ (Backtest, Analysis)
- Data Management Commands: 2/2 migrated ✅ (FetchData, BulkFetchData)
- Rebalancing Commands: 1/1 migrated ✅ (Rebalancing)
- Custom Strategy Commands: 3/3 migrated ✅ (CreateCustomStrategy, UpdateCustomStrategy, OptimizeParameters)
- AI/Chat Commands: 1/1 migrated ✅ (SendChatMessage)
- Strategy Analysis Commands: 1/1 migrated ✅ (AnalyzeStrategy)
- Parameter Optimization Commands: 1/1 migrated ✅ (ParameterOptimization)

**Total: 15/15 commands migrated (100% complete) 🎉**
