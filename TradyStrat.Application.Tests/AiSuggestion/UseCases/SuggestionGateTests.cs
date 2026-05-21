using Shouldly;
using TradyStrat.Application.AiSuggestion.UseCases;
using Xunit;

namespace TradyStrat.Application.Tests.AiSuggestion.UseCases;

public class SuggestionGateTests
{
    [Fact]
    public async Task Different_keys_do_not_block_each_other()
    {
        var date = new DateOnly(2026, 5, 21);
        var gateA = SuggestionGate.For(date, 1);
        var gateB = SuggestionGate.For(date, 2);

        await gateA.WaitAsync(TestContext.Current.CancellationToken);
        try
        {
            // gateB must be immediately acquirable while gateA is held.
            var acquiredB = await gateB.WaitAsync(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken);
            acquiredB.ShouldBeTrue();
            gateB.Release();
        }
        finally
        {
            gateA.Release();
        }
    }

    [Fact]
    public async Task Same_key_serializes()
    {
        var date = new DateOnly(2026, 5, 21);
        var first = SuggestionGate.For(date, 42);
        var second = SuggestionGate.For(date, 42);

        // Same key must return the same SemaphoreSlim instance.
        first.ShouldBeSameAs(second);

        await first.WaitAsync(TestContext.Current.CancellationToken);
        try
        {
            var acquired = await second.WaitAsync(TimeSpan.FromMilliseconds(20), TestContext.Current.CancellationToken);
            acquired.ShouldBeFalse();
        }
        finally
        {
            first.Release();
        }
    }

    [Fact]
    public void Different_dates_same_instrument_do_not_share_a_gate()
    {
        var monday = SuggestionGate.For(new DateOnly(2026, 5, 18), 1);
        var tuesday = SuggestionGate.For(new DateOnly(2026, 5, 19), 1);
        monday.ShouldNotBeSameAs(tuesday);
    }
}
