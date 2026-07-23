using Xunit;
using Shouldly;

namespace TradyStrat.E2E.Tests;

public class SmokeTests
{
    [Fact]
    public void Compiles() => true.ShouldBeTrue();
}
