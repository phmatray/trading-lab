using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Commands;

/// <summary>
/// Command to create a new custom strategy.
/// </summary>
public record CreateCustomStrategyCommand(
    string Name,
    string Description,
    string Author,
    string Category,
    StrategyDefinition Definition
);

/// <summary>
/// Command to update an existing custom strategy.
/// </summary>
public record UpdateCustomStrategyCommand(
    int Id,
    string Name,
    string Description,
    string Category,
    StrategyDefinition Definition
);

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
