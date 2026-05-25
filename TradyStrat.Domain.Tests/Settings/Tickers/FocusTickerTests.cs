using Shouldly;
using TradyStrat.Domain.Settings;
using TradyStrat.Domain.Settings.Tickers;
using Xunit;

namespace TradyStrat.Domain.Tests.Settings.Tickers;

public class FocusTickerTests
{
    [Fact] public void Trims_and_uppercases() =>
        FocusTicker.Of("  con3.l  ").Value.ShouldBe("CON3.L");

    [Fact] public void Rejects_blank() =>
        Should.Throw<SettingValidationException>(() => FocusTicker.Of("   "));
}
