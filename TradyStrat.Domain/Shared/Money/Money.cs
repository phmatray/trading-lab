using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Shared.Money;

public sealed class Money : ValueObject
{
    public decimal  Amount   { get; }
    public Currency Currency { get; }
    public bool     IsEmpty  { get; }

    private Money() { }   // EF
    private Money(decimal amount, Currency currency, bool isEmpty)
    {
        Amount = amount;
        Currency = currency;
        IsEmpty = isEmpty;
    }

    public static Money Of(decimal amount, Currency currency) => new(amount, currency, isEmpty: false);
    public static Money Zero(Currency currency)               => new(0m, currency, isEmpty: false);
    public static Money None(Currency currency)               => new(0m, currency, isEmpty: true);

    private static void RequireSpecified(Money m, string op)
    {
        if (m.IsEmpty)
            throw new InvalidOperationException($"Cannot perform '{op}' on Money.None({m.Currency}).");
    }

    private static void RequireMatchingCurrency(Money a, Money b, string op)
    {
        if (a.Currency != b.Currency)
            throw new CurrencyMismatchException(
                $"Cannot {op} {a.Currency} and {b.Currency}.");
    }

    public static Money operator +(Money a, Money b)
    {
        RequireSpecified(a, "+"); RequireSpecified(b, "+");
        RequireMatchingCurrency(a, b, "add");
        return Of(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        RequireSpecified(a, "-"); RequireSpecified(b, "-");
        RequireMatchingCurrency(a, b, "subtract");
        return Of(a.Amount - b.Amount, a.Currency);
    }

    public static Money operator *(Money m, decimal scalar)
    {
        RequireSpecified(m, "*");
        return Of(m.Amount * scalar, m.Currency);
    }

    public static Money operator *(decimal scalar, Money m) => m * scalar;

    public static Money operator /(Money m, decimal scalar)
    {
        RequireSpecified(m, "/");
        if (scalar == 0m) throw new DivideByZeroException();
        return Of(m.Amount / scalar, m.Currency);
    }

    public static decimal operator /(Money a, Money b)
    {
        RequireSpecified(a, "/"); RequireSpecified(b, "/");
        RequireMatchingCurrency(a, b, "divide");
        if (b.Amount == 0m) throw new DivideByZeroException();
        return a.Amount / b.Amount;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
        yield return IsEmpty;
    }

    public override string ToString() => IsEmpty ? $"None({Currency})" : $"{Amount} {Currency}";
}
