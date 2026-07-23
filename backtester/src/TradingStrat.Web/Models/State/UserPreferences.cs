namespace TradingStrat.Web.Models.State;

public class UserPreferences
{
    public string Theme { get; set; } = "system"; // "light", "dark", "system"
    public string DefaultTicker { get; set; } = "AAPL";
    public string? DefaultIsin { get; set; }
    public BacktestDefaults BacktestDefaults { get; set; } = new();
    public ChartPreferences ChartPreferences { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class BacktestDefaults
{
    public decimal InitialCapital { get; set; } = 10000m;
    public decimal CommissionPercentage { get; set; } = 0.001m;
    public decimal MinimumCommission { get; set; } = 1.0m;
    public string PreferredStrategy { get; set; } = "ma";
}

public class ChartPreferences
{
    public bool ShowVolume { get; set; } = true;
    public bool ShowIndicators { get; set; } = true;
    public string DefaultInterval { get; set; } = "D";
}
