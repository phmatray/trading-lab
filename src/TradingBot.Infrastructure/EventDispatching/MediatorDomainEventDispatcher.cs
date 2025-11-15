// <copyright file="MediatorDomainEventDispatcher.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Ardalis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TradingBot.Infrastructure.EventDispatching;

/// <summary>
/// Dispatches domain events using MediatR.
/// </summary>
public sealed class MediatorDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;
    private readonly ILogger<MediatorDomainEventDispatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediatorDomainEventDispatcher"/> class.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance.</param>
    /// <param name="logger">The logger instance.</param>
    public MediatorDomainEventDispatcher(
        IMediator mediator,
        ILogger<MediatorDomainEventDispatcher> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task DispatchAndClearEvents(IEnumerable<IHasDomainEvents> entitiesWithEvents)
    {
        foreach (var entity in entitiesWithEvents)
        {
            var events = entity.DomainEvents.ToArray();
            entity.ClearDomainEvents();

            foreach (var domainEvent in events)
            {
                _logger.LogDebug(
                    "Dispatching domain event: {EventType}",
                    domainEvent.GetType().Name);

                await _mediator.Publish(domainEvent).ConfigureAwait(false);
            }
        }
    }
}
