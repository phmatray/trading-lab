using Shouldly;
using TradyStrat.Domain.SeedWork;
using Xunit;

namespace TradyStrat.Domain.Tests.SeedWork;

public class ValueObjectEqualityTests
{
    private sealed class Pair(int a, string b) : ValueObject
    {
        public int A { get; } = a;
        public string B { get; } = b;
        protected override IEnumerable<object?> GetEqualityComponents()
        { yield return A; yield return B; }
    }

    private sealed class OtherPair(int a, string b) : ValueObject
    {
        public int A { get; } = a;
        public string B { get; } = b;
        protected override IEnumerable<object?> GetEqualityComponents()
        { yield return A; yield return B; }
    }

    [Fact]
    public void Same_components_are_equal()
    {
        new Pair(1, "x").ShouldBe(new Pair(1, "x"));
    }

    [Fact]
    public void Different_components_are_not_equal()
    {
        new Pair(1, "x").ShouldNotBe(new Pair(2, "x"));
        new Pair(1, "x").ShouldNotBe(new Pair(1, "y"));
    }

    [Fact]
    public void Different_subtype_with_same_components_is_not_equal()
    {
        ValueObject p = new Pair(1, "x");
        ValueObject o = new OtherPair(1, "x");
        p.ShouldNotBe(o);
    }

    [Fact]
    public void Equal_values_have_equal_hash_codes()
    {
        new Pair(1, "x").GetHashCode().ShouldBe(new Pair(1, "x").GetHashCode());
    }

    [Fact]
    public void Equality_operators_match_Equals()
    {
        var p = new Pair(1, "x");
        var q = new Pair(1, "x");
        (p == q).ShouldBeTrue();
        (p != q).ShouldBeFalse();
    }
}
