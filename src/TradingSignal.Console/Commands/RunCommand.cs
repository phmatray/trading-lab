using Microsoft.Extensions.Logging;
using TradingSignal.Adaptation;
using TradingSignal.Backtest;
using TradingSignal.ConsoleApp.Configuration;
using TradingSignal.ConsoleApp.Reports;
using TradingSignal.Core;
using TradingSignal.Core.Abstractions;
using TradingSignal.Evaluation.Metrics;

namespace TradingSignal.ConsoleApp.Commands;

public sealed partial class RunCommand(
    IMarketDataSource marketData,
    IFeatureEngine featureEngine,
    ISignalGenerator signalGenerator,
    IPredictionStore? predictionStore,
    AppConfig config,
    ILogger<RunCommand> logger,
    ILoggerFactory loggerFactory)
{
    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        TimeSpan interval = IntervalParser.Parse(config.Market.Interval);
        DateTime endUtc = DateTime.UtcNow.Date.AddDays(-1);
        DateTime startUtc = endUtc.AddDays(-config.Market.HistoryDays);

        IReadOnlyList<Candle> candles = await marketData.GetCandlesAsync(
            config.Market.Symbol, interval, startUtc, endUtc, ct).ConfigureAwait(false);
        LogLoadedCandles(logger, candles.Count);

        if (candles.Count < featureEngine.WarmupPeriods + 10)
        {
            LogNotEnoughCandles(logger);
            return 2;
        }

        BacktestOptions options = new()
        {
            AdaptationDays = config.WalkForward.AdaptationDays,
            TestDays = config.WalkForward.TestDays,
            StepDays = config.WalkForward.StepDays,
            EvaluationHorizonCandles = config.WalkForward.EvaluationHorizonCandles,
            CandlesPerDay = IntervalParser.CandlesPerDay(interval),
            PeriodsPerYear = IntervalParser.CandlesPerDay(interval) * 365,
            FeeBps = config.Fees.TakerBps,
            EnableShort = false,
        };

        List<(IAdaptationStrategy Strategy, BacktestResult Result)> runs = new();

        IAdaptationStrategy llmOnly = new NullAdaptation();
        BacktestResult llmOnlyResult = await Run(llmOnly, candles, options, ct);
        runs.Add((llmOnly, llmOnlyResult));

        if (config.Adaptation.EnableThreshold)
        {
            IAdaptationStrategy thresholdOnly = new ThresholdOptimizer(featureEngine, signalGenerator,
                loggerFactory.CreateLogger<ThresholdOptimizer>());
            BacktestResult thresholdResult = await Run(thresholdOnly, candles, options, ct);
            runs.Add((thresholdOnly, thresholdResult));
        }

        if (config.Adaptation.EnableMetaModel)
        {
            IAdaptationStrategy composite = new CompositeStrategy(featureEngine, signalGenerator);
            BacktestResult compositeResult = await Run(composite, candles, options, ct);
            runs.Add((composite, compositeResult));
        }

        ReturnSeriesMetrics buyHold = BuyAndHoldMetrics.Compute(candles, options.PeriodsPerYear, options.FeeBps);

        RunReport report = BuildReport(candles, options, runs, buyHold);
        await ReportWriter.WriteAsync(report, config.Output.ReportPath, ct).ConfigureAwait(false);
        LogWroteReport(logger, config.Output.ReportPath);

        ReportPrinter.Print(report, Console.Out);
        return 0;
    }

    private async Task<BacktestResult> Run(
        IAdaptationStrategy strategy,
        IReadOnlyList<Candle> candles,
        BacktestOptions options,
        CancellationToken ct)
    {
        LogRunningStrategy(logger, strategy.Label);
        WalkForwardOrchestrator orchestrator = new(
            featureEngine, signalGenerator, strategy, predictionStore, options,
            loggerFactory.CreateLogger<WalkForwardOrchestrator>());
        return await orchestrator.RunAsync(candles, config.Market.Symbol, ct).ConfigureAwait(false);
    }

    private RunReport BuildReport(
        IReadOnlyList<Candle> candles,
        BacktestOptions options,
        IReadOnlyList<(IAdaptationStrategy Strategy, BacktestResult Result)> runs,
        ReturnSeriesMetrics buyHold)
    {
        List<StrategyReport> strategyReports = new();
        foreach ((IAdaptationStrategy strategy, BacktestResult result) in runs)
        {
            strategyReports.Add(BuildStrategyReport(strategy, result, options));
        }

        return new RunReport(
            Symbol: config.Market.Symbol,
            Interval: config.Market.Interval,
            DataStartUtc: candles[0].OpenTimeUtc,
            DataEndUtc: candles[^1].OpenTimeUtc,
            CandleCount: candles.Count,
            FeeBps: options.FeeBps,
            BuyAndHold: new BuyAndHoldReport(buyHold.CumulativeReturnPct, buyHold.AnnualizedSharpe, buyHold.MaxDrawdownPct),
            Strategies: strategyReports);
    }

    private static StrategyReport BuildStrategyReport(
        IAdaptationStrategy strategy, BacktestResult result, BacktestOptions options)
    {
        IReadOnlyList<(Prediction, Outcome)> allRecords = result.AllRecords;
        PredictionScores scores = PredictionMetrics.Compute(allRecords);
        ReturnSeriesMetrics aggReturns = ReturnMetrics.Compute(result.ConcatenatedReturns, options.PeriodsPerYear);

        List<SegmentReport> segReports = new();
        foreach (SegmentResult seg in result.Segments)
        {
            PredictionScores segScores = PredictionMetrics.Compute(seg.Predictions.Zip(seg.Outcomes).ToList());
            ReturnSeriesMetrics segReturns = ReturnMetrics.Compute(seg.PerBarReturns, options.PeriodsPerYear);

            double? threshold = seg.Diagnostics.TryGetValue("selected_threshold", out double t) ? t : null;
            double? metaAcc = seg.Diagnostics.TryGetValue("meta_train_accuracy", out double m) ? m : null;

            segReports.Add(new SegmentReport(
                Segment: seg.Segment,
                Label: seg.StrategyLabel,
                TestStartUtc: seg.TestStartUtc,
                TestEndUtc: seg.TestEndUtc,
                Predictions: seg.PredictionCount,
                Trades: seg.TradeCount,
                Accuracy: segScores.Accuracy,
                BrierScore: segScores.BrierScore,
                CumulativeReturnPct: segReturns.CumulativeReturnPct,
                AnnualizedSharpe: segReturns.AnnualizedSharpe,
                MaxDrawdownPct: segReturns.MaxDrawdownPct,
                SelectedThreshold: threshold,
                MetaModelTrainAccuracy: metaAcc));
        }

        return new StrategyReport(
            Label: strategy.Label,
            TotalPredictions: scores.Total,
            NonHoldPredictions: scores.NonHold,
            TradeCount: result.TotalTradeCount,
            Accuracy: scores.Accuracy,
            BrierScore: scores.BrierScore,
            CumulativeReturnPct: aggReturns.CumulativeReturnPct,
            AnnualizedSharpe: aggReturns.AnnualizedSharpe,
            MaxDrawdownPct: aggReturns.MaxDrawdownPct,
            Segments: segReports);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Loaded {Count} candles for backtest")]
    private static partial void LogLoadedCandles(ILogger logger, int count);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Not enough candles to run a backtest")]
    private static partial void LogNotEnoughCandles(ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Wrote report to {Path}")]
    private static partial void LogWroteReport(ILogger logger, string path);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Running strategy: {Label}")]
    private static partial void LogRunningStrategy(ILogger logger, string label);
}
