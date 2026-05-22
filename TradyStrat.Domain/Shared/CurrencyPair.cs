namespace TradyStrat.Domain.Shared;

public readonly record struct CurrencyPair
{
    public Currency Base  { get; }
    public Currency Quote { get; }

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
