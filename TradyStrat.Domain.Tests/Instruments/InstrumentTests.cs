using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Instruments;

public class InstrumentTests
{
    private static readonly DateTime _now = new(2026, 5, 22, 12, 0, 0, DateTimeKind.Utc);

    private static Instrument ProbedSample() => Instrument.Probed(
        ticker:     "CON3.L",
        name:       "WisdomTree Coinbase 3x Daily Long",
        currency:   Currency.Eur,
        exchange:   Exchange.Of("LSE"),
        timezoneId: TimezoneId.Of("Europe/London"),
        kind:       InstrumentKind.Held);

    [Fact]
    public void Probed_assigns_zero_id_sentinel()
    {
        ProbedSample().Id.ShouldBe(InstrumentId.New());
    }

    [Fact]
    public void Probed_AddedAt_is_default_until_Confirm()
    {
        ProbedSample().AddedAt.ShouldBe(default);
    }

    [Fact]
    public void Confirm_sets_AddedAt_from_clock()
    {
        var inst = ProbedSample();
        inst.Confirm(StubClock.At(_now));
        inst.AddedAt.ShouldBe(_now);
    }

    [Fact]
    public void Confirm_throws_when_already_confirmed()
    {
        var inst = ProbedSample();
        inst.Confirm(StubClock.At(_now));
        Should.Throw<InvalidOperationException>(() => inst.Confirm(StubClock.At(_now.AddDays(1))));
    }

    [Fact]
    public void Rename_validates_non_empty()
    {
        var inst = ProbedSample();
        Should.Throw<ArgumentException>(() => inst.Rename(""));
        Should.Throw<ArgumentException>(() => inst.Rename("   "));
    }

    [Fact]
    public void Rename_trims_and_assigns()
    {
        var inst = ProbedSample();
        inst.Rename("  New Name  ");
        inst.Name.ShouldBe("New Name");
    }

    [Fact]
    public void Existing_rehydrates_persisted_state()
    {
        var inst = Instrument.Existing(
            id:         new InstrumentId(42),
            ticker:     "CON3.L",
            name:       "WisdomTree Coinbase 3x Daily Long",
            currency:   Currency.Eur,
            exchange:   Exchange.Of("LSE"),
            timezoneId: TimezoneId.Of("Europe/London"),
            kind:       InstrumentKind.Held,
            addedAt:    _now);

        inst.Id.ShouldBe(new InstrumentId(42));
        inst.AddedAt.ShouldBe(_now);
    }

    private sealed class StubClock(DateTime now) : IClock
    {
        public DateTime UtcNow() => now;
        public DateOnly TodayLocal() => DateOnly.FromDateTime(now);
        public DateOnly TodayInExchangeTzFor(string ticker) => DateOnly.FromDateTime(now);
        public static StubClock At(DateTime t) => new(t);
    }
}
