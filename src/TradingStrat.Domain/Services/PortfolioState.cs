namespace TradingStrat.Domain.Services;

public class PortfolioState
{
    public decimal Cash { get; set; }
    public int Position { get; set; }
    public decimal AverageEntryPrice { get; set; }
    public decimal TotalCommissionPaid { get; set; }

    public decimal GetEquity(decimal currentPrice)
    {
        return Cash + (Position * currentPrice);
    }

    public void ExecuteBuy(int quantity, decimal price, decimal commission)
    {
        var totalCost = (quantity * price) + commission;
        Cash -= totalCost;

        if (Position == 0)
        {
            AverageEntryPrice = price;
        }
        else
        {
            AverageEntryPrice = ((AverageEntryPrice * Position) + (price * quantity)) / (Position + quantity);
        }

        Position += quantity;
        TotalCommissionPaid += commission;
    }

    public void ExecuteSell(int quantity, decimal price, decimal commission)
    {
        var totalProceeds = (quantity * price) - commission;
        Cash += totalProceeds;
        Position -= quantity;
        TotalCommissionPaid += commission;

        if (Position == 0)
        {
            AverageEntryPrice = 0;
        }
    }
}
