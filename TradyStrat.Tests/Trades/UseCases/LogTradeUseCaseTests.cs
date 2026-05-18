using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Features.Trades.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Tests.Fx;             // TestRepo<T>
using TradyStrat.Tests.Specifications; // InMemoryDb
using TradyStrat.Tests.Common.Time;
using Xunit;

namespace TradyStrat.Tests.Trades.UseCases;

public class LogTradeUseCaseTests
{
    [Fact]
    public async Task Persists_trade_with_required_fields()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));
        var uc = new LogTradeUseCase(new TestRepo<Trade>(db), clock,
            NullLogger<LogTradeUseCase>.Instance);

        var t = await uc.ExecuteAsync(new LogTradeInput(
            InstrumentId: 1, ExecutedOn: new(2026,5,6), Side: TradeSide.Buy,
            Quantity: 10m, PricePerShare: 4.5m, FeesEur: 0.5m, Note: "first lot"), ct);

        t.Quantity.ShouldBe(10m);
        (await db.Trades.CountAsync(ct)).ShouldBe(1);
    }

    [Fact]
    public async Task Rejects_non_positive_quantity()
    {
        await using var db = InMemoryDb.Create();
        var uc = new LogTradeUseCase(new TestRepo<Trade>(db),
            new FakeClock(DateTime.UtcNow), NullLogger<LogTradeUseCase>.Instance);

        await Should.ThrowAsync<TradeValidationException>(() =>
            uc.ExecuteAsync(new LogTradeInput(InstrumentId: 1, new(2026,5,6), TradeSide.Buy,
                Quantity: 0m, PricePerShare: 4m, FeesEur: 0m, Note: null),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Rejects_non_positive_price()
    {
        await using var db = InMemoryDb.Create();
        var uc = new LogTradeUseCase(new TestRepo<Trade>(db),
            new FakeClock(DateTime.UtcNow), NullLogger<LogTradeUseCase>.Instance);

        await Should.ThrowAsync<TradeValidationException>(() =>
            uc.ExecuteAsync(new LogTradeInput(InstrumentId: 1, new(2026,5,6), TradeSide.Buy,
                10m, PricePerShare: -1m, 0m, null),
                TestContext.Current.CancellationToken));
    }
}
