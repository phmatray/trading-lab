using TradingStrat.Application.Common;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Commands;

// ===== Portfolio Creation =====

/// <summary>
/// Command to create a new portfolio.
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
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
        // Validate parameters
        ValidationGuard.Require(Name).NotNullOrWhiteSpace();
        ValidationGuard.Require(InitialCash).GreaterThanOrEqual(0m, "Initial cash cannot be negative");

        // Assign validated values
        this.Name = Name.Trim();
        this.Description = Description?.Trim();
        this.InitialCash = InitialCash;
    }
}

/// <summary>
/// Result of creating a portfolio.
/// </summary>
/// <param name="PortfolioId">The ID of the created portfolio.</param>
/// <param name="Name">The portfolio name.</param>
/// <param name="InitialCash">The initial cash balance.</param>
/// <param name="CreatedAt">When the portfolio was created.</param>
public record CreatePortfolioResult(
    int PortfolioId,
    string Name,
    decimal InitialCash,
    DateTime CreatedAt
);

// ===== Position Management =====

/// <summary>
/// Command to add a new position to a portfolio.
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
public record AddPositionCommand
{
    public int PortfolioId { get; init; }
    public string Ticker { get; init; }
    public int Quantity { get; init; }
    public decimal EntryPrice { get; init; }
    public DateTime EntryDate { get; init; }
    public string? Notes { get; init; }

    public AddPositionCommand(
        int PortfolioId,
        string Ticker,
        int Quantity,
        decimal EntryPrice,
        DateTime EntryDate,
        string? Notes = null)
    {
        // Validate parameters
        ValidationGuard.Require(PortfolioId).GreaterThan(0, "Portfolio ID must be positive");
        ValidationGuard.Require(Quantity).GreaterThan(0, "Quantity must be positive");
        ValidationGuard.Require(EntryPrice).GreaterThan(0m, "Entry price must be positive");
        ValidationGuard.Require(EntryDate).LessThanOrEqual(DateTime.Today, "Entry date cannot be in the future");

        // Assign validated values
        this.PortfolioId = PortfolioId;
        this.Ticker = CommonValidators.NormalizeTicker(Ticker);
        this.Quantity = Quantity;
        this.EntryPrice = EntryPrice;
        this.EntryDate = EntryDate;
        this.Notes = Notes;
    }
}

/// <summary>
/// Command to update an existing position.
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
public record UpdatePositionCommand
{
    public int PositionId { get; init; }
    public int Quantity { get; init; }
    public decimal EntryPrice { get; init; }
    public string? Notes { get; init; }

    public UpdatePositionCommand(
        int PositionId,
        int Quantity,
        decimal EntryPrice,
        string? Notes = null)
    {
        // Validate parameters
        ValidationGuard.Require(PositionId).GreaterThan(0, "Position ID must be positive");
        ValidationGuard.Require(Quantity).GreaterThan(0, "Quantity must be positive");
        ValidationGuard.Require(EntryPrice).GreaterThan(0m, "Entry price must be positive");

        // Assign validated values
        this.PositionId = PositionId;
        this.Quantity = Quantity;
        this.EntryPrice = EntryPrice;
        this.Notes = Notes;
    }
}

// ===== Cash Management =====

/// <summary>
/// Command for cash transactions (deposit or withdrawal).
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
public record CashTransactionCommand
{
    public int PortfolioId { get; init; }
    public TransactionType Type { get; init; }
    public decimal Amount { get; init; }
    public string? Notes { get; init; }

    public CashTransactionCommand(
        int PortfolioId,
        TransactionType Type,
        decimal Amount,
        string? Notes = null)
    {
        // Validate parameters
        ValidationGuard.Require(PortfolioId).GreaterThan(0, "Portfolio ID must be positive");
        CommonValidators.ValidatePositive(Amount, nameof(Amount));

        // Assign validated values
        this.PortfolioId = PortfolioId;
        this.Type = Type;
        this.Amount = Amount;
        this.Notes = Notes;
    }
}

// ===== Rebalancing =====

/// <summary>
/// Command to calculate portfolio rebalancing.
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
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
        // Validate parameters
        ValidationGuard.Require(PortfolioId).GreaterThan(0, "Portfolio ID must be positive");
        ValidationGuard.Require(TargetWeights).NotNull();
        CommonValidators.ValidateCommission(CommissionPercentage, MinimumCommission);

        // Assign validated values
        this.PortfolioId = PortfolioId;
        this.TargetWeights = TargetWeights;
        this.CommissionPercentage = CommissionPercentage;
        this.MinimumCommission = MinimumCommission;
    }
}
