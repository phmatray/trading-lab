using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Instruments.Events;
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
        kind:       InstrumentKind.Held,
        now:        _now);

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
        Should.Throw<ArgumentException>(() => inst.Rename("", StubClock.At(_now)));
        Should.Throw<ArgumentException>(() => inst.Rename("   ", StubClock.At(_now)));
    }

    [Fact]
    public void Rename_trims_and_assigns()
    {
        var inst = ProbedSample();
        inst.Rename("  New Name  ", StubClock.At(_now));
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

    [Fact]
    public void Probed_raises_InstrumentProbed()
    {
        var now = new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc);
        var inst = Instrument.Probed(
            "msft", "Microsoft",
            Currency.Usd, Exchange.Of("NASDAQ"), TimezoneId.Of("America/New_York"),
            InstrumentKind.Held,
            now);

        var evt = inst.DomainEvents.OfType<InstrumentProbed>().ShouldHaveSingleItem();
        evt.Ticker.ShouldBe("MSFT");
        evt.Currency.ShouldBe(Currency.Usd);
        evt.Exchange.ShouldBe(Exchange.Of("NASDAQ"));
        evt.OccurredAt.ShouldBe(now);
    }

    [Fact]
    public void Confirm_raises_InstrumentConfirmed()
    {
        var clock = StubClock.At(new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc));
        var inst = Instrument.Probed("MSFT", "Microsoft", Currency.Usd, Exchange.Of("NASDAQ"),
            TimezoneId.Of("America/New_York"), InstrumentKind.Held, clock.UtcNow());
        inst.DequeueDomainEvents();

        inst.Confirm(clock);

        var evt = inst.DomainEvents.OfType<InstrumentConfirmed>().ShouldHaveSingleItem();
        evt.OccurredAt.ShouldBe(clock.UtcNow());
    }

    [Fact]
    public void Rename_raises_InstrumentRenamed_with_old_and_new_names()
    {
        var clock = StubClock.At(new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc));
        var inst = Instrument.Probed("MSFT", "Microsoft", Currency.Usd, Exchange.Of("NASDAQ"),
            TimezoneId.Of("America/New_York"), InstrumentKind.Held, clock.UtcNow());
        inst.DequeueDomainEvents();

        inst.Rename("Microsoft Corp", clock);

        var evt = inst.DomainEvents.OfType<InstrumentRenamed>().ShouldHaveSingleItem();
        evt.OldName.ShouldBe("Microsoft");
        evt.NewName.ShouldBe("Microsoft Corp");
        evt.OccurredAt.ShouldBe(clock.UtcNow());
    }

    private sealed class StubClock(DateTime now) : IClock
    {
        public DateTime UtcNow() => now;
        public DateOnly TodayLocal() => DateOnly.FromDateTime(now);
        public DateOnly TodayInExchangeTzFor(string ticker) => DateOnly.FromDateTime(now);
        public static StubClock At(DateTime t) => new(t);
    }
}
