using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class RomanNumeralIdTests
{
    [Theory]
    [InlineData("i")]
    [InlineData("ii")]
    [InlineData("iii")]
    [InlineData("iv")]
    [InlineData("v")]
    [InlineData("ix")]
    [InlineData("x")]
    [InlineData("xiv")]
    public void Of_accepts_canonical_lowercase_roman_numerals(string raw)
    {
        RomanNumeralId.Of(raw).Value.ShouldBe(raw);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Of_rejects_empty(string? value)
    {
        Should.Throw<ArgumentException>(() => RomanNumeralId.Of(value!));
    }

    [Theory]
    [InlineData("I")]                  // uppercase rejected
    [InlineData("II")]
    [InlineData("1")]
    [InlineData("abc")]
    [InlineData("iiii")]                // non-canonical (should be "iv")
    public void Of_rejects_non_canonical(string raw)
    {
        Should.Throw<ArgumentException>(() => RomanNumeralId.Of(raw));
    }
}
