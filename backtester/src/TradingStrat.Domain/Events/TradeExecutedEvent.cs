using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Domain.Events;

/// <summary>
/// Domain event raised when a trade is executed during backtesting or live trading.
/// </summary>
/// <param name="TradeId">The unique identifier of the trade.</param>
/// <param name="Ticker">The ticker symbol of the security.</param>
/// <param name="Type">The type of trade (Buy or Sell).</param>
/// <param name="Quantity">The number of shares traded.</param>
/// <param name="Price">The price per share.</param>
/// <param name="Commission">The commission paid for the trade.</param>
public record TradeExecutedEvent(
    int TradeId,
    string Ticker,
    TradeType Type,
    int Quantity,
    decimal Price,
    decimal Commission) : DomainEvent;
