using Shouldly;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.SeedWork;
using Xunit;

namespace TradyStrat.Domain.Tests.SeedWork;

public class BusinessRuleViolationTests
{
    private sealed class AlwaysBroken : IBusinessRule
    {
        public bool IsBroken() => true;
        public string Message => "broken";
    }
    private sealed class NeverBroken : IBusinessRule
    {
        public bool IsBroken() => false;
        public string Message => "ok";
    }

    private sealed class TestAr : AggregateRoot<InstrumentId>
    {
        public TestAr(InstrumentId id) : base(id) { }
        public static void EnforceBroken() => CheckRule(new AlwaysBroken());
        public static void EnforceOk()     => CheckRule(new NeverBroken());
    }

    [Fact]
    public void CheckRule_throws_when_rule_is_broken()
    {
        var ex = Should.Throw<BusinessRuleViolationException>(TestAr.EnforceBroken);
        ex.Message.ShouldBe("broken");
        ex.BrokenRule.ShouldBeOfType<AlwaysBroken>();
    }

    [Fact]
    public void CheckRule_passes_when_rule_is_satisfied()
    {
        Should.NotThrow(TestAr.EnforceOk);
    }
}
