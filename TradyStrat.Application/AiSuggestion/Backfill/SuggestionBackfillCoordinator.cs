using Microsoft.Extensions.DependencyInjection;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Application.Settings;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Application.AiSuggestion.Backfill;

/// <summary>
/// Saga (first-fail-stop): replays missing daily AI calls for the focus ticker
/// in chronological order. Halts at the first failed day and records
/// <see cref="BackfillStatus.Failed"/> with the date that broke the chain.
///
/// Distinct from <c>GetAllTodaysSuggestionsUseCase</c> (Phase 2 Task 2) which
/// uses a swallow-and-continue policy: backfill is an explicit user-initiated
/// repair operation where knowing exactly which day failed is the goal,
/// while the daily fan-out is best-effort and should never block other
/// tickers' calls. Don't unify the two policies.
/// </summary>
public sealed partial class SuggestionBackfillCoordinator : ISuggestionBackfillCoordinator
{
    private readonly object _gate = new();
    private Task? _inflight;
    private volatile BackfillStatus _status = BackfillStatus.Idle.Instance;
    private readonly Func<Resolved> _resolveDeps;
    private readonly ILogger<SuggestionBackfillCoordinator> _log;

    private sealed record Resolved(
        ISuggestionRepository Suggestions,
        IInstrumentRepository Instruments,
        BackfillSuggestionsUseCase Backfill,
        IFocusTickerRepository FocusTickerRepo,
        IDisposable? Scope);

    public SuggestionBackfillCoordinator(
        IServiceScopeFactory scopeFactory,
        ILogger<SuggestionBackfillCoordinator> log)
    {
        _resolveDeps = () =>
        {
            var scope = scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            return new Resolved(
                sp.GetRequiredService<ISuggestionRepository>(),
                sp.GetRequiredService<IInstrumentRepository>(),
                sp.GetRequiredService<BackfillSuggestionsUseCase>(),
                sp.GetRequiredService<IFocusTickerRepository>(),
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
            var focusTicker = (await resolved.FocusTickerRepo.GetAsync(ct)).Value;
            var focus = await resolved.Instruments.FindByTickerAsync(focusTicker, ct)
                ?? throw new InvalidOperationException(
                    $"Focus instrument '{focusTicker}' is not registered.");

            var firstNeeded = fromExclusive.AddDays(1);
            var existing = await resolved.Suggestions.ListForAsync(
                focus.Id,
                new DateRange(firstNeeded, toInclusive),
                ct);
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
                        new BackfillSuggestionsInput(date, focus.Id.Value), ct);
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
