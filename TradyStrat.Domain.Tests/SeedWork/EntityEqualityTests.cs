using Shouldly;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.SeedWork;
using Xunit;

namespace TradyStrat.Domain.Tests.SeedWork;

public class EntityEqualityTests
{
    private sealed class Foo(InstrumentId id) : Entity<InstrumentId>(id) { }
    private sealed class Bar(InstrumentId id) : Entity<InstrumentId>(id) { }

    [Fact]
    public void Two_persisted_entities_with_same_type_and_id_are_equal()
    {
        var a = new Foo(new InstrumentId(7));
        var b = new Foo(new InstrumentId(7));
        a.Equals(b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void Different_subtype_with_same_id_is_not_equal()
    {
        var a = new Foo(new InstrumentId(7));
        var b = new Bar(new InstrumentId(7));
        a.Equals(b).ShouldBeFalse();
    }

    [Fact]
    public void Two_transient_entities_are_not_equal_to_each_other()
    {
        var a = new Foo(InstrumentId.New());
        var b = new Foo(InstrumentId.New());
        a.Equals(b).ShouldBeFalse();
    }

    [Fact]
    public void Transient_entity_is_equal_to_itself()
    {
        var a = new Foo(InstrumentId.New());
        a.Equals(a).ShouldBeTrue();
    }
}
