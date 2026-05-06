using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.UseCases.Trades;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Specifications;
using Xunit;

namespace TradyStrat.Tests.UseCases.Trades;

public class EditTradeUseCaseTests
{
    [Fact]
    public async Task Updates_existing_trade_quantity_and_price()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        db.Trades.Add(new Trade {
            Id = 0, ExecutedOn = new(2026,5,6), Side = TradeSide.Buy,
            Quantity = 5m, PricePerShare = 4m, FeesEur = 0m, Note = null,
            CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        var existing = db.Trades.Single();

        var uc = new EditTradeUseCase(new TestRepo<Trade>(db), db,
            NullLogger<EditTradeUseCase>.Instance);

        var updated = await uc.ExecuteAsync(new EditTradeInput(
            Id: existing.Id, ExecutedOn: existing.ExecutedOn, Side: TradeSide.Buy,
            Quantity: 8m, PricePerShare: 4.25m, FeesEur: 0.10m, Note: "edited"), ct);

        updated.Quantity.ShouldBe(8m);
        db.Trades.Single().PricePerShare.ShouldBe(4.25m);
    }

    [Fact]
    public async Task Throws_when_id_not_found()
    {
        await using var db = InMemoryDb.Create();
        var uc = new EditTradeUseCase(new TestRepo<Trade>(db), db,
            NullLogger<EditTradeUseCase>.Instance);

        await Should.ThrowAsync<TradeValidationException>(() =>
            uc.ExecuteAsync(new EditTradeInput(
                999, new(2026,5,6), TradeSide.Buy, 1m, 1m, 0m, null),
                TestContext.Current.CancellationToken));
    }
}
