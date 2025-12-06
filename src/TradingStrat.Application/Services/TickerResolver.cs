namespace TradingStrat.Application.Services;

public class TickerResolver : ITickerResolver
{
    private static readonly Dictionary<string, List<string>> IsinToTickersMap = new()
    {
        // XS2399367254 is Leverage Shares 3x Long Coinbase ETP
        // Available on multiple exchanges - try them in order of liquidity
        { "XS2399367254", new List<string> { "CON3.L", "3COI.DE", "3CON.AS" } }
    };

    public string? ResolveTickerFromIsin(string isin)
    {
        return IsinToTickersMap.TryGetValue(isin, out var tickers) ? tickers.First() : null;
    }

    public List<string>? GetAllTickersForIsin(string isin)
    {
        return IsinToTickersMap.TryGetValue(isin, out var tickers) ? tickers : null;
    }

    public bool TryResolveTickerFromIsin(string isin, out string? ticker)
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
