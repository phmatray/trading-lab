using TradingStrat.Domain.Common;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Commands;

/// <summary>
/// Command to create a new custom strategy.
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
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
        this.Definition = Definition;
    }
}

/// <summary>
/// Command to update an existing custom strategy.
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
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
        this.Definition = Definition;
    }
}

/// <summary>
/// Result returned when creating or retrieving a custom strategy.
/// </summary>
public record CustomStrategyResult(
    int Id,
    string Name,
    string Description,
    string Author,
    string Category,
    DateTime CreatedAt,
    DateTime LastUpdatedAt,
    StrategyDefinition Definition,
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
