using Ardalis.Specification;
using TradyStrat.Application.UseCases.AiSuggestion;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Specifications.Suggestions;

namespace TradyStrat.Features.AiSuggestion;

public sealed partial class SuggestionBackfillCoordinator(
    IReadRepositoryBase<Suggestion> suggestions,
    BackfillSuggestionsUseCase backfillOne,
    ILogger<SuggestionBackfillCoordinator> log) : ISuggestionBackfillCoordinator
{
    private readonly object _gate = new();
    private Task? _inflight;
    private BackfillStatus _status = BackfillStatus.Idle.Instance;

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
                // Restore to Idle without emitting Failed, then propagate.
                _status = BackfillStatus.Idle.Instance;
                throw;
            }
            catch (TradyStratException ex)
            {
                LogChainHalted(log, date, lastOk, ex);
                SetStatus(new BackfillStatus.Failed(
                    LastSuccessful: lastOk ?? fromExclusive,
                    FailedAt: date,
                    Reason: ex.Message));
                return;
            }
        }

        SetStatus(BackfillStatus.Idle.Instance);
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
