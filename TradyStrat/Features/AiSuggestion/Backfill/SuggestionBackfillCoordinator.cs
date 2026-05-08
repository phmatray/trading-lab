using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Features.AiSuggestion.Specifications;
using TradyStrat.Features.AiSuggestion.UseCases;
using TradyStrat.Features.Settings.Specifications;

namespace TradyStrat.Features.AiSuggestion.Backfill;

public sealed partial class SuggestionBackfillCoordinator : ISuggestionBackfillCoordinator
{
    private readonly object _gate = new();
    private Task? _inflight;
    private volatile BackfillStatus _status = BackfillStatus.Idle.Instance;
    private readonly Func<Resolved> _resolveDeps;
    private readonly ILogger<SuggestionBackfillCoordinator> _log;

    private sealed record Resolved(
        IReadRepositoryBase<Suggestion> Suggestions,
        IReadRepositoryBase<Instrument> Instruments,
        BackfillSuggestionsUseCase Backfill,
        IConfiguration Config,
        IDisposable? Scope);

    public SuggestionBackfillCoordinator(
        IReadRepositoryBase<Suggestion> suggestions,
        IReadRepositoryBase<Instrument> instruments,
        BackfillSuggestionsUseCase backfillOne,
        IConfiguration config,
        ILogger<SuggestionBackfillCoordinator> log)
    {
        _resolveDeps = () => new Resolved(suggestions, instruments, backfillOne, config, null);
        _log = log;
    }

    [ActivatorUtilitiesConstructor]
    public SuggestionBackfillCoordinator(
        IServiceScopeFactory scopeFactory,
        ILogger<SuggestionBackfillCoordinator> log)
    {
        _resolveDeps = () =>
        {
            var scope = scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            return new Resolved(
                sp.GetRequiredService<IReadRepositoryBase<Suggestion>>(),
                sp.GetRequiredService<IReadRepositoryBase<Instrument>>(),
                sp.GetRequiredService<BackfillSuggestionsUseCase>(),
                sp.GetRequiredService<IConfiguration>(),
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
        var resolved = _resolveDeps();
        try
        {
            var focusTicker = resolved.Config["Tickers:Focus"]
                ?? throw new InvalidOperationException("Tickers:Focus is not configured.");
            var focus = await resolved.Instruments.FirstOrDefaultAsync(
                new InstrumentByTickerSpec(focusTicker), ct)
                ?? throw new InvalidOperationException(
                    $"Focus instrument '{focusTicker}' is not registered.");

            var firstNeeded = fromExclusive.AddDays(1);
            var existing = await resolved.Suggestions.ListAsync(
                new SuggestionsInRangeSpec(firstNeeded, toInclusive, focus.Id), ct);
            var existingDates = existing.Select(s => s.ForDate).ToHashSet();

            var missing = new List<DateOnly>();
            for (var d = firstNeeded; d <= toInclusive; d = d.AddDays(1))
                if (!existingDates.Contains(d)) missing.Add(d);

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
                    await resolved.Backfill.ExecuteAsync(
                        new BackfillSuggestionsInput(date, focus.Id), ct);
                    lastOk = date;
                }
                catch (OperationCanceledException)
                {
                    SetStatus(BackfillStatus.Idle.Instance);
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
            resolved.Scope?.Dispose();
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
