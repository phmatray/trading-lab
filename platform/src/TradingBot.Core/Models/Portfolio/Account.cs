// <copyright file="Account.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Events;
using TradingBot.Core.SharedKernel;

namespace TradingBot.Core.Models.Portfolio;

/// <summary>
/// Represents a trading account aggregate root.
/// </summary>
public sealed class Account : IAggregateRoot
{
    private readonly List<DomainEventBase> _domainEvents = new();

    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    public required string AccountId { get; set; }

    /// <summary>
    /// Gets the domain events collection.
    /// </summary>
    public IReadOnlyCollection<DomainEventBase> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Gets or sets the total equity (cash + position values).
    /// </summary>
    public required decimal Equity { get; set; }

    /// <summary>
    /// Gets or sets the available cash balance.
    /// </summary>
    public required decimal Cash { get; set; }

    /// <summary>
    /// Gets or sets the total value of open positions.
    /// </summary>
    public decimal PositionValue { get; set; }

    /// <summary>
    /// Gets or sets the buying power (cash * leverage).
    /// </summary>
    public decimal BuyingPower { get; set; }

    /// <summary>
    /// Gets or sets the current leverage being used.
    /// </summary>
    public decimal Leverage { get; set; }

    /// <summary>
    /// Gets or sets the total unrealized profit/loss from open positions.
    /// </summary>
    public decimal UnrealizedPnL { get; set; }

    /// <summary>
    /// Gets or sets the total realized profit/loss from closed trades.
    /// </summary>
    public decimal RealizedPnL { get; set; }

    /// <summary>
    /// Gets the total profit/loss (realized + unrealized).
    /// </summary>
    public decimal TotalPnL => RealizedPnL + UnrealizedPnL;

    /// <summary>
    /// Deducts cash from the account balance.
    /// </summary>
    /// <param name="amount">The amount to deduct.</param>
    public void DeductCash(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be positive", nameof(amount));
        }

        if (Cash < amount)
        {
            throw new InvalidOperationException($"Insufficient funds. Available: {Cash}, Required: {amount}");
        }

        Cash -= amount;

        RegisterDomainEvent(new CashUpdatedEvent(AccountId, Cash));
    }

    /// <summary>
    /// Adds cash to the account balance.
    /// </summary>
    /// <param name="amount">The amount to add.</param>
    public void AddCash(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be positive", nameof(amount));
        }

        Cash += amount;

        RegisterDomainEvent(new CashUpdatedEvent(AccountId, Cash));
    }

    /// <summary>
    /// Updates the total equity of the account.
    /// </summary>
    /// <param name="totalPositionValue">The total value of all positions.</param>
    public void UpdateEquity(decimal totalPositionValue)
    {
        PositionValue = totalPositionValue;
        Equity = Cash + totalPositionValue;

        RegisterDomainEvent(new EquityUpdatedEvent(AccountId, Equity));
    }

    /// <summary>
    /// Suspends the account.
    /// </summary>
    /// <param name="reason">The reason for suspension.</param>
    public void Suspend(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Suspension reason is required", nameof(reason));
        }

        RegisterDomainEvent(new AccountSuspendedEvent(AccountId, reason));
    }

    /// <summary>
    /// Clears all domain events.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Registers a domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to register.</param>
    private void RegisterDomainEvent(DomainEventBase domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
