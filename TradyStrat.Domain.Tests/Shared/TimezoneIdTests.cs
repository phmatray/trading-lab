using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class TimezoneIdTests
{
    [Theory]
    [InlineData("Europe/London")]
    [InlineData("America/New_York")]
    [InlineData("Asia/Tokyo")]
    [InlineData("UTC")]
    public void Of_accepts_known_iana_ids(string id)
    {
        TimezoneId.Of(id).Value.ShouldBe(id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Of_rejects_empty(string? value)
    {
        Should.Throw<ArgumentException>(() => TimezoneId.Of(value!));
    }

    [Theory]
    [InlineData("NotARealZone")]
    [InlineData("Europe/Atlantis")]
    public void Of_rejects_unknown_iana_ids(string id)
    {
        Should.Throw<ArgumentException>(() => TimezoneId.Of(id));
    }
}
