namespace TradingStrat.Application.Services;

public interface ITickerResolver
{
    string? ResolveTickerFromIsin(string isin);
    List<string>? GetAllTickersForIsin(string isin);
    bool TryResolveTickerFromIsin(string isin, out string? ticker);
}
