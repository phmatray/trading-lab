namespace TradyStrat.Domain.Shared.Money;

public sealed record CurrencyPair
{
    public Currency Base  { get; private set; } = Currency.Eur;
    public Currency Quote { get; private set; } = Currency.Usd;

    private CurrencyPair() { }   // EF

    private CurrencyPair(Currency @base, Currency quote)
    {
        Base = @base;
        Quote = quote;
    }

    public static CurrencyPair Of(Currency @base, Currency quote)
    {
        if (@base == quote)
            throw new ArgumentException(
                $"CurrencyPair Base and Quote must differ (both {@base}).", nameof(quote));
        return new CurrencyPair(@base, quote);
    }

    public override string ToString() => $"{Base}/{Quote}";
}
