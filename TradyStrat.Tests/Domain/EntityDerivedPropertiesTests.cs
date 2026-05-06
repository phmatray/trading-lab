using Shouldly;
using TradyStrat.Shared.Domain;
using Xunit;

namespace TradyStrat.Tests.Domain;

public class EntityDerivedPropertiesTests
{
    private static Trade Buy(decimal qty, decimal price, decimal fees = 0m) => new()
    {
        Id = 0,
        ExecutedOn = new DateOnly(2026, 5, 6),
        Side = TradeSide.Buy,
        Quantity = qty,
        PricePerShare = price,
        FeesEur = fees,
        Note = null,
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public void Trade_GrossEur_is_qty_times_price()
    {
        Buy(10m, 4.50m).GrossEur.ShouldBe(45.00m);
    }

    [Fact]
    public void Trade_NetEur_buy_adds_fees()
    {
        Buy(10m, 4.50m, fees: 1.20m).NetEur.ShouldBe(46.20m);
    }

    [Fact]
    public void Trade_NetEur_sell_subtracts_fees()
    {
        var sell = Buy(10m, 4.50m, fees: 1.20m) with { Side = TradeSide.Sell };
        sell.NetEur.ShouldBe(43.80m);
    }

    [Fact]
    public void PriceBar_derived_props()
    {
        var bar = new PriceBar
        {
            Id = 1, Ticker = "CON3.DE", Date = new(2026,5,6),
            Open = 4.0m, High = 4.5m, Low = 3.9m, Close = 4.4m, Volume = 1000
        };

        bar.Range.ShouldBe(0.6m);
        bar.Change.ShouldBe(0.4m);
        bar.IsUp.ShouldBeTrue();
    }

    [Fact]
    public void FxRate_EurPerUsd_is_inverse()
    {
        var fx = new FxRate { Id = 1, Date = new(2026,5,6), Pair = "EURUSD",
                              UsdPerEur = 1.0820m, FetchedAt = DateTime.UtcNow };

        fx.EurPerUsd.ShouldBe(1m / 1.0820m);
    }

    [Fact]
    public void Suggestion_OrderValueEur_is_qty_times_price_when_both_set()
    {
        var s = new Suggestion {
            Id = 0, ForDate = new(2026,5,6), Action = SuggestionAction.Acquire,
            QuantityHint = 8m, MaxPriceHint = 4.85m, Conviction = 4,
            Rationale = "x", CitationsJson = "[]", PromptHash = "h",
            CreatedAt = DateTime.UtcNow
        };

        s.OrderValueEur.ShouldBe(8m * 4.85m);
    }

    [Fact]
    public void Suggestion_OrderValueEur_is_null_when_either_missing()
    {
        var s = new Suggestion {
            Id = 0, ForDate = new(2026,5,6), Action = SuggestionAction.Hold,
            QuantityHint = null, MaxPriceHint = 4.85m, Conviction = 3,
            Rationale = "x", CitationsJson = "[]", PromptHash = "h",
            CreatedAt = DateTime.UtcNow
        };

        s.OrderValueEur.ShouldBeNull();
    }

    [Fact]
    public void GoalConfig_Default_is_one_million_with_focus_CON3()
    {
        var now = DateTime.UtcNow;
        var g = GoalConfig.Default(now);

        g.Id.ShouldBe(1);
        g.TargetEur.ShouldBe(1_000_000m);
        g.FocusTicker.ShouldBe("CON3.L");
        g.UpdatedAt.ShouldBe(now);
    }
}
