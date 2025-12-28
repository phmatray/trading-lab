using TradingStrat.Domain.Common;
using TradingStrat.Domain.Events;
using TradingStrat.Domain.Exceptions;

namespace TradingStrat.Domain.Entities;

/// <summary>
/// Portfolio aggregate root representing a collection of investment positions with cash balance.
/// Encapsulates business rules and ensures invariants are maintained.
/// Supports event sourcing with event replay capability.
/// </summary>
public class Portfolio : AggregateRoot
{
    private readonly List<Position> _positions = new();

    /// <summary>
    /// Gets or sets the unique identifier for the portfolio.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets the aggregate identifier for event sourcing (Portfolio ID as string).
    /// </summary>
    public override string AggregateId => Id.ToString();

    /// <summary>
    /// Gets or sets the portfolio name (e.g., "Growth", "Income", "Retirement").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional description of the portfolio.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the cash balance in the portfolio.
    /// </summary>
    public decimal Cash { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the portfolio was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the portfolio was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Gets the read-only collection of positions in this portfolio.
    /// Use AddPosition/RemovePosition/UpdatePositionQuantity methods to modify positions after initialization.
    /// Init setter provided for JSON deserialization and testing infrastructure.
    /// </summary>
    public IReadOnlyList<Position> Positions
    {
        get => _positions.AsReadOnly();
        init
        {
            _positions.Clear();
            _positions.AddRange(value);
        }
    }

    /// <summary>
    /// Adds a new position to the portfolio.
    /// Enforces the invariant that no two positions can have the same ticker.
    /// </summary>
    /// <param name="position">The position to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when position is null.</exception>
    /// <exception cref="DuplicatePositionException">Thrown when a position with the same ticker already exists.</exception>
    public void AddPosition(Position position)
    {
        ValidationGuard.Require(position).NotNull();

        // Validate business rules before raising event
        if (_positions.Any(p => p.Ticker == position.Ticker))
        {
            throw new DuplicatePositionException(position.Ticker);
        }

        // Raise event - Apply method will update state
        position.PortfolioId = Id;
        RaiseDomainEvent(new PositionAddedEvent(
            Id,
            position.Ticker,
            position.Quantity,
            position.EntryPrice,
            position.EntryDate
        ));
    }

    /// <summary>
    /// Removes a position from the portfolio by ticker symbol.
    /// </summary>
    /// <param name="ticker">The ticker symbol of the position to remove.</param>
    /// <exception cref="PositionNotFoundException">Thrown when no position with the given ticker exists.</exception>
    public void RemovePosition(string ticker)
    {
        // Validate business rules before raising event
        Position position = _positions.FirstOrDefault(p => p.Ticker == ticker)
                            ?? throw new PositionNotFoundException(ticker);

        // Raise event - Apply method will update state
        RaiseDomainEvent(new PositionRemovedEvent(
            Id,
            ticker,
            position.Quantity
        ));
    }

    /// <summary>
    /// Updates the quantity of an existing position.
    /// </summary>
    /// <param name="ticker">The ticker symbol of the position to update.</param>
    /// <param name="newQuantity">The new quantity for the position.</param>
    /// <exception cref="PositionNotFoundException">Thrown when no position with the given ticker exists.</exception>
    public void UpdatePositionQuantity(string ticker, int newQuantity)
    {
        // Validate business rules before raising event
        Position position = _positions.FirstOrDefault(p => p.Ticker == ticker)
                            ?? throw new PositionNotFoundException(ticker);

        int oldQuantity = position.Quantity;

        // Raise event - Apply method will update state
        RaiseDomainEvent(new PositionQuantityChangedEvent(
            Id,
            ticker,
            oldQuantity,
            newQuantity
        ));
    }

    /// <summary>
    /// Records a cash transaction (deposit or withdrawal) and updates the cash balance.
    /// </summary>
    /// <param name="type">The type of transaction (Deposit or Withdrawal).</param>
    /// <param name="amount">The amount of the transaction.</param>
    /// <param name="date">The date of the transaction.</param>
    /// <exception cref="InsufficientCashException">Thrown when attempting to withdraw more cash than available.</exception>
    public void RecordCashTransaction(TransactionType type, decimal amount, DateTime date)
    {
        // Validate business rules before raising event
        if (type == TransactionType.Withdrawal && Cash < amount)
        {
            throw new InsufficientCashException(Cash, amount);
        }

        // Raise event - Apply method will update state
        RaiseDomainEvent(new CashTransactionRecordedEvent(
            Id,
            type,
            amount,
            date
        ));
    }

    /// <summary>
    /// Applies a domain event to rebuild or update the aggregate state.
    /// Used for both event replay (sourcing) and new event application.
    /// </summary>
    /// <param name="domainEvent">The domain event to apply.</param>
    protected override void Apply(DomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case PortfolioCreatedEvent e:
                ApplyPortfolioCreated(e);
                break;

            case PositionAddedEvent e:
                ApplyPositionAdded(e);
                break;

            case PositionRemovedEvent e:
                ApplyPositionRemoved(e);
                break;

            case PositionQuantityChangedEvent e:
                ApplyPositionQuantityChanged(e);
                break;

            case CashTransactionRecordedEvent e:
                ApplyCashTransactionRecorded(e);
                break;

            case PortfolioRebalancedEvent:
                // No state changes for rebalancing event (informational only)
                break;

            default:
                throw new InvalidOperationException($"Unknown event type: {domainEvent.GetType().Name}");
        }
    }

    /// <summary>
    /// Applies PortfolioCreatedEvent to initialize the portfolio state.
    /// </summary>
    private void ApplyPortfolioCreated(PortfolioCreatedEvent e)
    {
        Id = e.PortfolioId;
        Name = e.Name;
        Cash = e.InitialCash;
        CreatedAt = e.OccurredAt;
        LastUpdated = e.OccurredAt;
    }

    /// <summary>
    /// Applies PositionAddedEvent to add a position to the portfolio.
    /// </summary>
    private void ApplyPositionAdded(PositionAddedEvent e)
    {
        var position = new Position
        {
            PortfolioId = e.PortfolioId,
            Ticker = e.Ticker,
            Quantity = e.Quantity,
            EntryPrice = e.EntryPrice,
            EntryDate = e.EntryDate
        };

        _positions.Add(position);
        LastUpdated = e.OccurredAt;
    }

    /// <summary>
    /// Applies PositionRemovedEvent to remove a position from the portfolio.
    /// </summary>
    private void ApplyPositionRemoved(PositionRemovedEvent e)
    {
        Position? position = _positions.FirstOrDefault(p => p.Ticker == e.Ticker);
        if (position is not null)
        {
            _positions.Remove(position);
        }

        LastUpdated = e.OccurredAt;
    }

    /// <summary>
    /// Applies PositionQuantityChangedEvent to update a position's quantity.
    /// </summary>
    private void ApplyPositionQuantityChanged(PositionQuantityChangedEvent e)
    {
        Position? position = _positions.FirstOrDefault(p => p.Ticker == e.Ticker);
        if (position is not null)
        {
            position.Quantity = e.NewQuantity;
        }

        LastUpdated = e.OccurredAt;
    }

    /// <summary>
    /// Applies CashTransactionRecordedEvent to update the cash balance.
    /// </summary>
    private void ApplyCashTransactionRecorded(CashTransactionRecordedEvent e)
    {
        if (e.Type == TransactionType.Deposit)
        {
            Cash += e.Amount;
        }
        else if (e.Type == TransactionType.Withdrawal)
        {
            Cash -= e.Amount;
        }

        LastUpdated = e.OccurredAt;
    }
}
