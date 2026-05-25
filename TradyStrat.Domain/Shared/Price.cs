using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Shared;

public sealed class Price : ValueObject
{
    public Money    PerUnit  { get; private set; } = Money.Zero(Currency.Eur);
    public Currency Currency => PerUnit.Currency;
    public bool     IsEmpty  => PerUnit.IsEmpty;

    private Price() { }   // EF
    private Price(Money perUnit) => PerUnit = perUnit;

    public static Price Of(Money perUnit)        => new(perUnit);
    public static Price None(Currency currency)  => new(Money.None(currency));
    public static Price Zero(Currency currency)  => new(Money.Zero(currency));

    public static Money operator *(Price p, Quantity q)
    {
        if (p.IsEmpty || !q.IsSpecified)
            return Money.None(p.Currency);
        return p.PerUnit * q.Value;
    }

    public static Money operator -(Price a, Price b)
    {
        if (a.IsEmpty || b.IsEmpty)
            return Money.None(a.Currency);
        return a.PerUnit - b.PerUnit;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PerUnit;
    }

    public override string ToString() => IsEmpty ? $"None({Currency})" : PerUnit.ToString();
}
