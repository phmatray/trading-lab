namespace TradingStrat.Application.Services;

public class TickerResolver : ITickerResolver
{
    private static readonly Dictionary<string, List<string>> _isinToTickersMap = new()
    {
        // XS2399367254 is Leverage Shares 3x Long Coinbase ETP
        // Available on multiple exchanges - try them in order of liquidity
        { "XS2399367254", ["CON3.L", "3COI.DE", "3CON.AS"] }
    };

    public string? ResolveTickerFromIsin(string isin)
    {
        return _isinToTickersMap.TryGetValue(isin, out List<string>? tickers) ? tickers.First() : null;
    }

    public List<string>? GetAllTickersForIsin(string isin)
    {
        return _isinToTickersMap.TryGetValue(isin, out List<string>? tickers) ? tickers : null;
    }

    public bool TryResolveTickerFromIsin(string isin, out string? ticker)
    {
        if (_isinToTickersMap.TryGetValue(isin, out List<string>? tickers))
        {
            ticker = tickers.First();
            return true;
        }

        ticker = null;
        return false;
    }
}
