using Ardalis.Specification;
using TradyStrat.Features.AiSuggestion.UseCases;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Features.AiSuggestion.Specifications;

namespace TradyStrat.Features.AiSuggestion.Backfill;

public sealed partial class SuggestionBackfillCoordinator : ISuggestionBackfillCoordinator
{
    private readonly object _gate = new();
    private Task? _inflight;
    private volatile BackfillStatus _status = BackfillStatus.Idle.Instance;
    private readonly Func<(IReadRepositoryBase<Suggestion> Suggestions, BackfillSuggestionsUseCase Backfill, IDisposable? Scope)> _resolveDeps;
    private readonly ILogger<SuggestionBackfillCoordinator> _log;

    // Test-friendly: direct deps (existing)
    public SuggestionBackfillCoordinator(
        IReadRepositoryBase<Suggestion> suggestions,
        BackfillSuggestionsUseCase backfillOne,
        ILogger<SuggestionBackfillCoordinator> log)
    {
        _resolveDeps = () => (suggestions, backfillOne, null);
        _log = log;
    }

    // Production: scope-factory (new)
    [ActivatorUtilitiesConstructor]
    public SuggestionBackfillCoordinator(
        IServiceScopeFactory scopeFactory,
        ILogger<SuggestionBackfillCoordinator> log)
    {
        _resolveDeps = () =>
        {
            var scope = scopeFactory.CreateScope();
            return (
                scope.ServiceProvider.GetRequiredService<IReadRepositoryBase<Suggestion>>(),
                scope.ServiceProvider.GetRequiredService<BackfillSuggestionsUseCase>(),
                scope);
        };
        _log = log;
    }

    public BackfillStatus Status => _status;
    public event Action<BackfillStatus>? StatusChanged;

    public Task EnsureBackfilledAsync(DateOnly fromExclusive, DateOnly toInclusive, CancellationToken ct)
    {
        lock (_gate)
        {
            if (_inflight is { IsCompleted: false }) return _inflight;
            _inflight = RunChainAsync(fromExclusive, toInclusive, ct);
            return _inflight;
        }
    }

    private async Task RunChainAsync(DateOnly fromExclusive, DateOnly toInclusive, CancellationToken ct)
    {
        var (suggestions, backfillOne, scope) = _resolveDeps();
        try
        {
            // Build the set of dates already present (range is fromExclusive+1 .. toInclusive).
            var firstNeeded = fromExclusive.AddDays(1);
            var existing = await suggestions.ListAsync(
                new SuggestionsInRangeSpec(firstNeeded, toInclusive), ct);
            var existingDates = existing.Select(s => s.ForDate).ToHashSet();

            var missing = new List<DateOnly>();
            for (var d = firstNeeded; d <= toInclusive; d = d.AddDays(1))
                if (!existingDates.Contains(d)) missing.Add(d);

            // Empty range — stay Idle, emit no event.
            if (missing.Count == 0)
            {
                _status = BackfillStatus.Idle.Instance;
                return;
            }

            DateOnly? lastOk = null;
            for (var i = 0; i < missing.Count; i++)
            {
                var date = missing[i];
                SetStatus(new BackfillStatus.Running(missing.Count - i, missing.Count, date));

                try
                {
                    await backfillOne.ExecuteAsync(date, ct);
                    lastOk = date;
                }
                catch (OperationCanceledException)
                {
                    SetStatus(BackfillStatus.Idle.Instance);   // notify subscribers the chain is no longer running
                    throw;
                }
                catch (TradyStratException ex)
                {
                    LogChainHalted(_log, date, lastOk, ex);
                    SetStatus(new BackfillStatus.Failed(
                        LastSuccessful: lastOk ?? fromExclusive,
                        FailedAt: date,
                        Reason: ex.Message));
                    return;
                }
            }

            SetStatus(BackfillStatus.Idle.Instance);
        }
        finally
        {
            scope?.Dispose();
        }
    }

    private void SetStatus(BackfillStatus next)
    {
        _status = next;
        StatusChanged?.Invoke(next);
    }

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Backfill chain halted at {BackfillDate} (last successful: {LastSuccessfulDate})")]
    private static partial void LogChainHalted(
        ILogger logger, DateOnly backfillDate, DateOnly? lastSuccessfulDate, Exception ex);
}
