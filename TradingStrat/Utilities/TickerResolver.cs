namespace TradingStrat.Utilities;

public static class TickerResolver
{
    private static readonly Dictionary<string, List<string>> IsinToTickersMap = new()
    {
        // XS2399367254 is Leverage Shares 3x Long Coinbase ETP
        // Available on multiple exchanges - try them in order of liquidity
        { "XS2399367254", new List<string> { "CON3.L", "3COI.DE", "3CON.AS" } }
    };

    public static string? ResolveTickerFromIsin(string isin)
    {
        return IsinToTickersMap.TryGetValue(isin, out var tickers) ? tickers.First() : null;
    }

    public static List<string>? GetAllTickersForIsin(string isin)
    {
        return IsinToTickersMap.TryGetValue(isin, out var tickers) ? tickers : null;
    }

    public static bool TryResolveTickerFromIsin(string isin, out string? ticker)
    {
        if (IsinToTickersMap.TryGetValue(isin, out var tickers))
        {
            ticker = tickers.First();
            return true;
        }

        ticker = null;
        return false;
    }
}
