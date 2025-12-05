namespace TradingStrat.Services.Strategies;

public enum SignalType
{
    Hold,
    Buy,
    Sell
}

public record TradeSignal(
    SignalType Type,
    decimal Price,
    int Quantity,
    string Reason
);
