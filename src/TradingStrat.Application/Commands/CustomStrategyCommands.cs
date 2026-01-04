using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Commands;

/// <summary>
/// Command to create a new custom strategy.
/// Supports both rule-based strategies (with StrategyDefinition) and Python strategies (with PythonCode).
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
public record CreateCustomStrategyCommand
{
    public string Name { get; init; }
    public string Description { get; init; }
    public string Author { get; init; }
    public string Category { get; init; }
    public CustomStrategyType StrategyType { get; init; }
    public StrategyDefinition? Definition { get; init; }
    public string? PythonCode { get; init; }

    /// <summary>
    /// Creates a new rule-based strategy command.
    /// </summary>
    public CreateCustomStrategyCommand(
        string Name,
        string Description,
        string Author,
        string Category,
        StrategyDefinition Definition)
    {
        // Validate parameters
        ValidationGuard.Require(Name).NotNullOrWhiteSpace();
        ValidationGuard.Require(Description).NotNullOrWhiteSpace();
        ValidationGuard.Require(Author).NotNullOrWhiteSpace();
        ValidationGuard.Require(Category).NotNullOrWhiteSpace();
        ValidationGuard.Require(Definition).NotNull();

        // Assign validated values
        this.Name = Name.Trim();
        this.Description = Description.Trim();
        this.Author = Author.Trim();
        this.Category = Category.Trim();
        StrategyType = CustomStrategyType.RuleBased;
        this.Definition = Definition;
        PythonCode = null;
    }

    /// <summary>
    /// Creates a new Python strategy command.
    /// </summary>
    public CreateCustomStrategyCommand(
        string Name,
        string Description,
        string Author,
        string Category,
        string PythonCode)
    {
        // Validate parameters
        ValidationGuard.Require(Name).NotNullOrWhiteSpace();
        ValidationGuard.Require(Description).NotNullOrWhiteSpace();
        ValidationGuard.Require(Author).NotNullOrWhiteSpace();
        ValidationGuard.Require(Category).NotNullOrWhiteSpace();
        ValidationGuard.Require(PythonCode).NotNullOrWhiteSpace();

        // Assign validated values
        this.Name = Name.Trim();
        this.Description = Description.Trim();
        this.Author = Author.Trim();
        this.Category = Category.Trim();
        StrategyType = CustomStrategyType.Python;
        Definition = null;
        this.PythonCode = PythonCode.Trim();
    }
}

/// <summary>
/// Command to update an existing custom strategy.
/// Supports both rule-based strategies (with StrategyDefinition) and Python strategies (with PythonCode).
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
public record UpdateCustomStrategyCommand
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public string Category { get; init; }
    public CustomStrategyType StrategyType { get; init; }
    public StrategyDefinition? Definition { get; init; }
    public string? PythonCode { get; init; }

    /// <summary>
    /// Updates a rule-based strategy.
    /// </summary>
    public UpdateCustomStrategyCommand(
        int Id,
        string Name,
        string Description,
        string Category,
        StrategyDefinition Definition)
    {
        // Validate parameters
        ValidationGuard.Require(Id).GreaterThan(0, "Strategy ID must be positive");
        ValidationGuard.Require(Name).NotNullOrWhiteSpace();
        ValidationGuard.Require(Description).NotNullOrWhiteSpace();
        ValidationGuard.Require(Category).NotNullOrWhiteSpace();
        ValidationGuard.Require(Definition).NotNull();

        // Assign validated values
        this.Id = Id;
        this.Name = Name.Trim();
        this.Description = Description.Trim();
        this.Category = Category.Trim();
        StrategyType = CustomStrategyType.RuleBased;
        this.Definition = Definition;
        PythonCode = null;
    }

    /// <summary>
    /// Updates a Python strategy.
    /// </summary>
    public UpdateCustomStrategyCommand(
        int Id,
        string Name,
        string Description,
        string Category,
        string PythonCode)
    {
        // Validate parameters
        ValidationGuard.Require(Id).GreaterThan(0, "Strategy ID must be positive");
        ValidationGuard.Require(Name).NotNullOrWhiteSpace();
        ValidationGuard.Require(Description).NotNullOrWhiteSpace();
        ValidationGuard.Require(Category).NotNullOrWhiteSpace();
        ValidationGuard.Require(PythonCode).NotNullOrWhiteSpace();

        // Assign validated values
        this.Id = Id;
        this.Name = Name.Trim();
        this.Description = Description.Trim();
        this.Category = Category.Trim();
        StrategyType = CustomStrategyType.Python;
        Definition = null;
        this.PythonCode = PythonCode.Trim();
    }
}

/// <summary>
/// Result returned when creating or retrieving a custom strategy.
/// Includes both rule-based and Python strategy fields.
/// </summary>
public record CustomStrategyResult(
    int Id,
    string Name,
    string Description,
    string Author,
    string Category,
    DateTime CreatedAt,
    DateTime LastUpdatedAt,
    CustomStrategyType StrategyType,
    StrategyDefinition? Definition,
    string? PythonCode,
    int? PythonCodeVersion,
    int TimesUsed,
    decimal? LastBacktestReturn,
    DateTime? LastBacktestDate
);

/// <summary>
/// Result of validating a strategy definition.
/// </summary>
public record ValidationResult(
    bool IsValid,
    List<string> Errors
);

/// <summary>
/// Command to validate Python code without persisting it.
/// </summary>
public record ValidatePythonCodeCommand
{
    public string PythonCode { get; init; }

    public ValidatePythonCodeCommand(string pythonCode)
    {
        ValidationGuard.Require(pythonCode).NotNullOrWhiteSpace();
        PythonCode = pythonCode.Trim();
    }
}

/// <summary>
/// Result of validating Python code (Application layer).
/// Wrapper around Domain.Services.PythonValidationResult for use in application layer.
/// </summary>
public record PythonValidationResult(
    bool IsValid,
    List<string> Errors
);

/// <summary>
/// Command to dry-run a Python strategy on historical data without persisting it.
/// </summary>
public record DryRunPythonStrategyCommand
{
    public string PythonCode { get; init; }
    public string Ticker { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public decimal InitialCash { get; init; }

    public DryRunPythonStrategyCommand(
        string PythonCode,
        string Ticker,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        decimal InitialCash = 100000m)
    {
        ValidationGuard.Require(PythonCode).NotNullOrWhiteSpace();
        ValidationGuard.Require(Ticker).NotNullOrWhiteSpace();
        ValidationGuard.Require(InitialCash).GreaterThan(0, "Initial cash must be positive");

        this.PythonCode = PythonCode.Trim();
        this.Ticker = Ticker.Trim().ToUpperInvariant();
        this.StartDate = StartDate;
        this.EndDate = EndDate;
        this.InitialCash = InitialCash;
    }
}

/// <summary>
/// Result of dry-running a Python strategy.
/// Includes validation errors and backtest results if successful.
/// </summary>
public record DryRunResult(
    bool IsValid,
    List<string> ValidationErrors,
    int? TotalTrades,
    decimal? FinalEquity,
    decimal? TotalReturn,
    decimal? SharpeRatio
);
