using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Features.Trades.UseCases;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Specifications;
using Xunit;

namespace TradyStrat.Tests.Trades.UseCases;

public class DeleteTradeUseCaseTests
{
    [Fact]
    public async Task Removes_existing_trade()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        db.Trades.Add(new Trade {
            Id = 0, ExecutedOn = new(2026,5,6), Side = TradeSide.Buy,
            Quantity = 1m, PricePerShare = 1m, FeesEur = 0m, Note = null,
            CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        var existing = db.Trades.Single();

        var uc = new DeleteTradeUseCase(new TestRepo<Trade>(db),
            NullLogger<DeleteTradeUseCase>.Instance);

        await uc.ExecuteAsync(new DeleteTradeInput(existing.Id), ct);

        (await db.Trades.CountAsync(ct)).ShouldBe(0);
    }

    [Fact]
    public async Task Throws_when_id_missing()
    {
        await using var db = InMemoryDb.Create();
        var uc = new DeleteTradeUseCase(new TestRepo<Trade>(db),
            NullLogger<DeleteTradeUseCase>.Instance);

        await Should.ThrowAsync<TradeValidationException>(() =>
            uc.ExecuteAsync(new DeleteTradeInput(123),
                TestContext.Current.CancellationToken));
    }
}
