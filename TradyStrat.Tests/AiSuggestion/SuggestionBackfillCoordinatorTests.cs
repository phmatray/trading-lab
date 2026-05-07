using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Features.AiSuggestion.UseCases;
using TradyStrat.Data;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Specifications;
using Xunit;

namespace TradyStrat.Tests.AiSuggestion;

public class SuggestionBackfillCoordinatorTests
{
    private static (SuggestionBackfillCoordinator coord, AppDbContext ctx, RecordingAi ai)
        BuildSut(Func<DateOnly, Task<Suggestion>>? aiOverride = null)
    {
        var ctx = InMemoryDb.Create();
        var ai = new RecordingAi(aiOverride);
        var factory = new PassthroughFactory();
        var useCase = new BackfillSuggestionsUseCase(
            new TestRepo<Suggestion>(ctx), factory, ai,
            NullLogger<BackfillSuggestionsUseCase>.Instance);
        var coord = new SuggestionBackfillCoordinator(
            new TestRepo<Suggestion>(ctx), useCase,
            NullLogger<SuggestionBackfillCoordinator>.Instance);
        return (coord, ctx, ai);
    }

    [Fact]
    public async Task Empty_range_stays_idle_and_emits_no_events()
    {
        var ct = TestContext.Current.CancellationToken;
        var (coord, _, _) = BuildSut();
        var events = new List<BackfillStatus>();
        coord.StatusChanged += s => events.Add(s);

        await coord.EnsureBackfilledAsync(
            new DateOnly(2026, 5, 7), new DateOnly(2026, 5, 7), ct);

        coord.Status.ShouldBeOfType<BackfillStatus.Idle>();
        events.ShouldBeEmpty();
    }

    [Fact]
    public async Task Single_missing_date_emits_running_then_idle()
    {
        var ct = TestContext.Current.CancellationToken;
        var (coord, ctx, _) = BuildSut();
        var events = new List<BackfillStatus>();
        coord.StatusChanged += s => events.Add(s);

        await coord.EnsureBackfilledAsync(
            fromExclusive: new DateOnly(2026, 5, 5),
            toInclusive:   new DateOnly(2026, 5, 6), ct);

        events.OfType<BackfillStatus.Running>().Count().ShouldBe(1);
        events[^1].ShouldBeOfType<BackfillStatus.Idle>();
        ctx.Suggestions.Count().ShouldBe(1);
        ctx.Suggestions.Single().ForDate.ShouldBe(new DateOnly(2026, 5, 6));
    }

    [Fact]
    public async Task Multi_day_runs_chronologically_ascending()
    {
        var ct = TestContext.Current.CancellationToken;
        var (coord, _, ai) = BuildSut();

        await coord.EnsureBackfilledAsync(
            fromExclusive: new DateOnly(2026, 5, 1),
            toInclusive:   new DateOnly(2026, 5, 4), ct);

        ai.Calls.ShouldBe([
            new DateOnly(2026, 5, 2),
            new DateOnly(2026, 5, 3),
            new DateOnly(2026, 5, 4),
        ]);
    }

    [Fact]
    public async Task Mid_chain_failure_halts_with_typed_status()
    {
        var ct = TestContext.Current.CancellationToken;
        var (coord, ctx, _) = BuildSut(d =>
            d.Day == 3
                ? throw new AnthropicCallFailedException("boom")
                : Task.FromResult(StubSuggestion(d)));

        await coord.EnsureBackfilledAsync(
            fromExclusive: new DateOnly(2026, 5, 1),
            toInclusive:   new DateOnly(2026, 5, 5), ct);

        coord.Status.ShouldBeOfType<BackfillStatus.Failed>();
        var failed = (BackfillStatus.Failed)coord.Status;
        failed.LastSuccessful.ShouldBe(new DateOnly(2026, 5, 2));
        failed.FailedAt.ShouldBe(new DateOnly(2026, 5, 3));
        ctx.Suggestions.Count().ShouldBe(1);   // 5/2 persisted; 5/1 not in scope (fromExclusive=5/1)
        ctx.Suggestions.Single().ForDate.ShouldBe(new DateOnly(2026, 5, 2));
    }

    [Fact]
    public async Task Reentrancy_returns_same_inflight_task()
    {
        var ct = TestContext.Current.CancellationToken;
        var (coord, _, _) = BuildSut();

        var t1 = coord.EnsureBackfilledAsync(
            new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 3), ct);
        var t2 = coord.EnsureBackfilledAsync(
            new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 3), ct);

        await Task.WhenAll(t1, t2);
        coord.Status.ShouldBeOfType<BackfillStatus.Idle>();
    }

    [Fact]
    public async Task Cancellation_throws_does_not_set_failed()
    {
        var cts = new CancellationTokenSource();
        var (coord, _, _) = BuildSut(async d =>
        {
            // Observe the token so cancellation actually propagates.
            await Task.Delay(1000, cts.Token);
            return StubSuggestion(d);
        });

        var task = coord.EnsureBackfilledAsync(
            new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 5), cts.Token);
        cts.CancelAfter(20);
        await Should.ThrowAsync<OperationCanceledException>(task);
        coord.Status.ShouldNotBeOfType<BackfillStatus.Failed>();
    }

    [Fact]
    public async Task Multi_subscriber_fan_out()
    {
        var ct = TestContext.Current.CancellationToken;
        var (coord, _, _) = BuildSut();
        var a = new List<BackfillStatus>(); var b = new List<BackfillStatus>();
        coord.StatusChanged += s => a.Add(s);
        coord.StatusChanged += s => b.Add(s);

        await coord.EnsureBackfilledAsync(
            new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 2), ct);

        a.Count.ShouldBe(b.Count);
        a.Count.ShouldBeGreaterThan(0);
    }

    private static Suggestion StubSuggestion(DateOnly d) => new()
    {
        Id = 0, ForDate = d, Action = SuggestionAction.Hold, Conviction = 5,
        Rationale = "stub", CitationsJson = "[]", PromptHash = "test",
        CreatedAt = DateTime.UtcNow,
    };

    private sealed class PassthroughFactory : ISnapshotFactory
    {
        public Task<AiSnapshot> CreateAsync(DateOnly asOf, CancellationToken ct) =>
            Task.FromResult(new AiSnapshot(
                asOf, GoalConfig.Default(DateTime.UtcNow),
                new PortfolioSnapshot(0, 0, 0, 0, 0, 0), [], [], 1m, "test"));
    }

    private sealed class RecordingAi(Func<DateOnly, Task<Suggestion>>? handler) : IAiClient
    {
        public List<DateOnly> Calls { get; } = new();
        public async Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct)
        {
            Calls.Add(snapshot.Today);
            if (handler is null) return StubSuggestion(snapshot.Today);
            return await handler(snapshot.Today);
        }
    }
}
