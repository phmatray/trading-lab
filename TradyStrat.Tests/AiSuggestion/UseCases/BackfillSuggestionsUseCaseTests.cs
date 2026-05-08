using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Features.AiSuggestion.UseCases;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Features.AiSuggestion.Snapshot;
using TradyStrat.Common.Domain;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Specifications;
using Xunit;

namespace TradyStrat.Tests.AiSuggestion.UseCases;

public class BackfillSuggestionsUseCaseTests
{
    private sealed class StubFactory : IAiSnapshotService
    {
        public DateOnly? CapturedDate { get; private set; }
        public int? CapturedInstrumentId { get; private set; }

        public Task<AiSnapshot> CreateAsync(int instrumentId, DateOnly asOf, CancellationToken ct)
        {
            CapturedDate = asOf;
            CapturedInstrumentId = instrumentId;
            return Task.FromResult(new AiSnapshot(
                Today: asOf,
                InstrumentId: instrumentId,
                Goal: GoalConfig.Default(DateTime.UtcNow),
                Portfolio: new PortfolioSnapshot([], 0, 0, 0, 0, 0, 0, 0),
                Tickers: [],
                RecentTrades: [],
                UsdPerEur: 1m,
                Markets: [],
                PromptHash: "test"));
        }
    }

    private sealed class StubAiClient : IAiClient
    {
        public Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct) =>
            Task.FromResult(new Suggestion
            {
                Id = 0,
                InstrumentId = snapshot.InstrumentId,    // NEW
                ForDate = snapshot.Today,
                Action = SuggestionAction.Hold,
                Conviction = 5,
                Rationale = "stub",
                CitationsJson = "[]",
                PromptHash = snapshot.PromptHash,
                CreatedAt = DateTime.UtcNow,
            });
    }

    [Fact]
    public async Task Persists_suggestion_with_ForDate_from_asOf()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var factory = new StubFactory();
        var ai = new StubAiClient();
        var sut = new BackfillSuggestionsUseCase(
            new TestRepo<Suggestion>(db), factory, ai,
            NullLogger<BackfillSuggestionsUseCase>.Instance);

        var asOf = new DateOnly(2026, 5, 4);
        const int focusId = 7;
        var s = await sut.ExecuteAsync(new BackfillSuggestionsInput(asOf, focusId), ct);

        s.ForDate.ShouldBe(asOf);
        factory.CapturedDate.ShouldBe(asOf);
        factory.CapturedInstrumentId.ShouldBe(focusId);
        db.Suggestions.Single().ForDate.ShouldBe(asOf);
    }
}
