using System.Runtime.CompilerServices;
using System.Threading.Channels;
using TradyStrat.Application.Settings;

namespace TradyStrat.Application.AiSuggestion.UseCases;

/// <summary>
/// Streams today's AI suggestion for each held instrument as soon as it
/// arrives. Fans out per-instrument calls under a SemaphoreSlim gate; one
/// failure does not block other workers (each yields its own
/// <see cref="SuggestionStreamEvent.Failed"/>). Cancellation cascades from
/// the consumer to every in-flight Anthropic call.
///
/// The stream emits one event per input element (in the input
/// <see cref="IReadOnlyCollection{T}"/>) in completion order — not input
/// order. Duplicate ids produce duplicate events; the caller is responsible
/// for deduplication if needed.
/// </summary>
public sealed partial class StreamTodaysSuggestionsUseCase(
    GetTodaysSuggestionUseCase getOne,
    IAnthropicSettingsRepository anthropic,
    ILogger<StreamTodaysSuggestionsUseCase> log)
{
    public async IAsyncEnumerable<SuggestionStreamEvent> StreamAsync(
        IReadOnlyCollection<int> heldInstrumentIds,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (heldInstrumentIds.Count == 0) yield break;

        var ai = await anthropic.GetAsync(ct);
        var maxParallel = Math.Max(1, ai.MaxParallelSuggestions.Value);

        var chan = Channel.CreateUnbounded<SuggestionStreamEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });
        // Intentionally not `using`: workers can still be in their finally block
        // (sem.Release) when the iterator is cancelled and unwinds. Disposing here
        // would race with those releases and surface ObjectDisposedException as a
        // misleading Failed event. GC reclaim of an undisposed SemaphoreSlim is
        // safe — Dispose only frees the lazily-allocated AvailableWaitHandle.
        var sem = new SemaphoreSlim(maxParallel, maxParallel);

        var workers = heldInstrumentIds
            .Select(id => RunWorkerAsync(id, sem, chan.Writer, ct))
            .ToArray();

        // Fire-and-forget: complete the writer when every worker has finished,
        // so the reader loop terminates. Worker errors are already routed
        // through Failed events; ContinueWith just signals completion.
        _ = Task.WhenAll(workers).ContinueWith(
            _ => chan.Writer.TryComplete(),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        await foreach (var ev in chan.Reader.ReadAllAsync(ct))
            yield return ev;
    }

    private async Task RunWorkerAsync(
        int instrumentId,
        SemaphoreSlim sem,
        ChannelWriter<SuggestionStreamEvent> writer,
        CancellationToken ct)
    {
        try
        {
            await sem.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var s = await getOne
                    .ExecuteAsync(new GetTodaysSuggestionInput(instrumentId), ct)
                    .ConfigureAwait(false);
                writer.TryWrite(new SuggestionStreamEvent.Ready(instrumentId, s));
            }
            finally
            {
                sem.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // Cooperative cancellation — drop silently.
        }
        catch (Exception ex)
        {
            LogPerInstrumentFailure(log, ex, instrumentId);
            writer.TryWrite(new SuggestionStreamEvent.Failed(instrumentId, ex.Message));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stream worker failed for instrument {InstrumentId}")]
    private static partial void LogPerInstrumentFailure(ILogger logger, Exception ex, int instrumentId);
}
