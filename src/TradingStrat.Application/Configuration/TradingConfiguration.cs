namespace TradingStrat.Application.Configuration;

public class TradingConfiguration
{
    public string DefaultTicker { get; set; } = "CON3.L";
    public string DefaultIsin { get; set; } = "XS2399367254";
    public DatabaseSettings Database { get; set; } = new();
    public BacktestSettings Backtest { get; set; } = new();
    public MLSettings MachineLearning { get; set; } = new();
    public ExportSettings Export { get; set; } = new();
    public YahooFinanceSettings YahooFinance { get; set; } = new();
}

public class DatabaseSettings
{
    public string Provider { get; set; } = "SQLite";
    public string ConnectionString { get; set; } = "Data Source=trading.db";
}

public class BacktestSettings
{
    public decimal InitialCapital { get; set; } = 10000m;
    public decimal CommissionPercentage { get; set; } = 0.001m;
    public decimal MinimumCommission { get; set; } = 1.0m;
    public int DefaultPeriodYears { get; set; } = 2;
}

public class MLSettings
{
    public int MinTrainingBars { get; set; } = 100;
    public ThresholdSettings DefaultThresholds { get; set; } = new();
    public ModelParameterSettings ModelParameters { get; set; } = new();
}

public class ThresholdSettings
{
    public decimal BuyThreshold { get; set; } = 0.01m;
    public decimal SellThreshold { get; set; } = -0.01m;
}

public class ModelParameterSettings
{
    public int NumberOfLeaves { get; set; } = 31;
    public int MinimumExampleCountPerLeaf { get; set; } = 20;
    public double LearningRate { get; set; } = 0.1;
    public int NumberOfTrees { get; set; } = 100;
}

public class ExportSettings
{
    public string OutputDirectory { get; set; } = "./exports";
    public string DateFormat { get; set; } = "yyyyMMdd_HHmmss";
}

public class YahooFinanceSettings
{
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
}
