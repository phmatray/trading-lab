namespace TradingSignal.ConsoleApp.Configuration;

public sealed class AppConfig
{
    public LmStudioConfig LmStudio { get; set; } = new();
    public MarketConfig Market { get; set; } = new();
    public FeesConfig Fees { get; set; } = new();
    public WalkForwardConfig WalkForward { get; set; } = new();
    public AdaptationConfig Adaptation { get; set; } = new();
    public OutputConfig Output { get; set; } = new();
}

public sealed class LmStudioConfig
{
    public string Endpoint { get; set; } = "http://localhost:1234/v1";
    public string ModelId { get; set; } = "qwen2.5-14b-instruct";
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxFewShot { get; set; } = 3;
    public int MaxOutputTokens { get; set; } = 4096;
    public string ModelFamily { get; set; } = "instruct";
    public string ReasoningEffort { get; set; } = "medium";
}

public sealed class MarketConfig
{
    public string Symbol { get; set; } = "BTCUSDT";
    public string Interval { get; set; } = "1h";
    public int HistoryDays { get; set; } = 730;
}

public sealed class FeesConfig
{
    public double TakerBps { get; set; } = 10;
}

public sealed class WalkForwardConfig
{
    public int AdaptationDays { get; set; } = 180;
    public int TestDays { get; set; } = 30;
    public int StepDays { get; set; } = 30;
    public int EvaluationHorizonCandles { get; set; } = 1;
}

public sealed class AdaptationConfig
{
    public bool EnableThreshold { get; set; } = true;
    public bool EnableMetaModel { get; set; } = true;
}

public sealed class OutputConfig
{
    public string DataCacheDir { get; set; } = "./data-cache";
    public string DbPath { get; set; } = "./runs/predictions.db";
    public string LlmCachePath { get; set; } = "./runs/llm-cache.db";
    public string ReportPath { get; set; } = "./runs/report.json";
}

public static class IntervalParser
{
    public static TimeSpan Parse(string interval)
    {
        if (string.IsNullOrWhiteSpace(interval))
            throw new ArgumentException("Interval is empty", nameof(interval));

        char unit = interval[^1];
        if (!int.TryParse(interval[..^1], out int value) || value <= 0)
            throw new ArgumentException($"Bad interval: {interval}", nameof(interval));

        return unit switch
        {
            'm' => TimeSpan.FromMinutes(value),
            'h' => TimeSpan.FromHours(value),
            'd' => TimeSpan.FromDays(value),
            _ => throw new ArgumentException($"Bad interval unit: {unit}", nameof(interval)),
        };
    }

    public static int CandlesPerDay(TimeSpan interval)
    {
        double dayMinutes = TimeSpan.FromDays(1).TotalMinutes;
        return (int)(dayMinutes / interval.TotalMinutes);
    }
}
