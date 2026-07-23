using Microsoft.Extensions.Options;
using TradingStrat.Application.Configuration;
using TradingStrat.Web.Services.State;

namespace TradingStrat.ComponentTests.TestDoubles;

/// <summary>
/// Fake implementation of UserPreferencesService for testing.
/// Stores user preferences in-memory instead of localStorage.
/// </summary>
public class FakeUserPreferencesService : UserPreferencesService
{
    public FakeUserPreferencesService()
        : base(
            new FakeLocalStorageService(),
            Options.Create(CreateDefaultConfiguration()))
    {
    }

    /// <summary>
    /// Creates a default trading configuration for testing.
    /// </summary>
    private static TradingConfiguration CreateDefaultConfiguration()
    {
        return new TradingConfiguration
        {
            DefaultTicker = "AAPL",
            DefaultIsin = "TEST_ISIN",
            Database = new DatabaseSettings
            {
                Provider = "SQLite",
                ConnectionString = "Data Source=:memory:"
            },
            Backtest = new BacktestSettings
            {
                InitialCapital = 10000m,
                CommissionPercentage = 0.001m,
                MinimumCommission = 1.0m,
                DefaultPeriodYears = 2
            },
            MachineLearning = new MLSettings
            {
                MinTrainingBars = 100,
                DefaultThresholds = new ThresholdSettings
                {
                    BuyThreshold = 0.01m,
                    SellThreshold = -0.01m
                },
                ModelParameters = new ModelParameterSettings
                {
                    NumberOfLeaves = 31,
                    MinimumExampleCountPerLeaf = 20,
                    LearningRate = 0.1,
                    NumberOfTrees = 100
                }
            },
            Export = new ExportSettings
            {
                OutputDirectory = "./exports",
                DateFormat = "yyyyMMdd_HHmmss"
            },
            YahooFinance = new YahooFinanceSettings
            {
                TimeoutSeconds = 30,
                MaxRetries = 3
            },
            AlphaVantage = new AlphaVantageSettings
            {
                ApiKey = "test_key",
                TimeoutSeconds = 30,
                MaxRetries = 3,
                MaxCallsPerMinute = 5,
                MaxCallsPerDay = 500
            }
        };
    }
}
