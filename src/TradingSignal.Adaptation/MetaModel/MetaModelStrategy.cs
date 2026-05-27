using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.ML;
using Microsoft.ML.Data;
using TradingSignal.Core;
using TradingSignal.Core.Abstractions;

namespace TradingSignal.Adaptation.MetaModel;

// Spec §9.2: LbfgsLogisticRegression that predicts P(signal is profitable net of
// fees) from indicator features + one-hot LLM action + LLM confidence. Trained
// on adaptation-window predictions; gates by P ≥ 0.5 at test time.
public sealed partial class MetaModelStrategy(
    IFeatureEngine featureEngine,
    ISignalGenerator signalGenerator,
    ILogger<MetaModelStrategy>? logger = null)
    : IAdaptationStrategy
{
    private readonly ILogger<MetaModelStrategy> _logger = logger ?? NullLogger<MetaModelStrategy>.Instance;
    private readonly MLContext _ml = new(seed: 1);

    private PredictionEngine<MetaTrainingRow, MetaPredictionRow>? _predictionEngine;
    private double _lastTrainAccuracy;

    public string Label => "+meta-model";

    public double LastTrainAccuracy => _lastTrainAccuracy;

    public IReadOnlyDictionary<string, double> Diagnostics => new Dictionary<string, double>
    {
        ["meta_train_accuracy"] = _lastTrainAccuracy,
    };

    public async Task FitAsync(AdaptationContext context, CancellationToken ct)
    {
        IReadOnlyList<AdaptationSample> samples = await AdaptationDatasetBuilder
            .BuildAsync(context, featureEngine, signalGenerator, ct)
            .ConfigureAwait(false);

        TrainOnSamples(samples);
        LogTrained(_logger, context.Segment, samples.Count, _lastTrainAccuracy);
    }

    internal void TrainOnSamples(IReadOnlyList<AdaptationSample> samples)
    {
        if (samples.Count < 20)
        {
            LogSkippingFit(_logger, samples.Count);
            _predictionEngine = null;
            return;
        }

        List<MetaTrainingRow> rows = samples.Select(BuildRow).ToList();
        IDataView data = _ml.Data.LoadFromEnumerable(rows);

        string[] featureColumns =
        {
            nameof(MetaTrainingRow.Rsi14),
            nameof(MetaTrainingRow.MacdHistogram),
            nameof(MetaTrainingRow.EmaRatio),
            nameof(MetaTrainingRow.Atr14),
            nameof(MetaTrainingRow.Return1),
            nameof(MetaTrainingRow.Return5),
            nameof(MetaTrainingRow.VolatilityPct),
            nameof(MetaTrainingRow.LlmActionBuy),
            nameof(MetaTrainingRow.LlmActionSell),
            nameof(MetaTrainingRow.LlmConfidence),
        };

        IEstimator<ITransformer> pipeline = _ml.Transforms
            .Concatenate("Features", featureColumns)
            .Append(_ml.Transforms.NormalizeMinMax("Features"))
            .Append(_ml.BinaryClassification.Trainers.LbfgsLogisticRegression(
                labelColumnName: nameof(MetaTrainingRow.Label),
                featureColumnName: "Features"));

        ITransformer model = pipeline.Fit(data);
        IDataView scored = model.Transform(data);
        BinaryClassificationMetrics metrics = _ml.BinaryClassification.EvaluateNonCalibrated(scored,
            labelColumnName: nameof(MetaTrainingRow.Label));
        _lastTrainAccuracy = metrics.Accuracy;

        _predictionEngine = _ml.Model.CreatePredictionEngine<MetaTrainingRow, MetaPredictionRow>(model);
    }

    public FinalDecision Apply(RawSignal raw, FeatureSet features)
    {
        if (_predictionEngine is null)
            return new FinalDecision(raw.Action, raw.Confidence);

        if (raw.Action == TradeAction.Hold)
            return new FinalDecision(TradeAction.Hold, raw.Confidence);

        MetaTrainingRow input = BuildPredictionRow(raw, features);
        MetaPredictionRow prediction = _predictionEngine.Predict(input);
        double pProfit = prediction.Probability;

        if (pProfit < 0.5d)
            return new FinalDecision(TradeAction.Hold, pProfit);

        return new FinalDecision(raw.Action, pProfit);
    }

    private static MetaTrainingRow BuildRow(AdaptationSample s)
    {
        MetaTrainingRow row = BuildPredictionRow(s.Signal, s.Features);
        row.Label = s.Outcome.RealizedReturnPct > 0d;
        return row;
    }

    private static MetaTrainingRow BuildPredictionRow(RawSignal signal, FeatureSet features)
    {
        float emaRatio = features.Ema50 == 0d ? 1f : (float)(features.Ema20 / features.Ema50);
        return new MetaTrainingRow
        {
            Rsi14 = (float)features.Rsi14,
            MacdHistogram = (float)features.MacdHistogram,
            EmaRatio = emaRatio,
            Atr14 = (float)features.Atr14,
            Return1 = (float)features.Return1,
            Return5 = (float)features.Return5,
            VolatilityPct = (float)features.VolatilityPct,
            LlmActionBuy = signal.Action == TradeAction.Buy ? 1f : 0f,
            LlmActionSell = signal.Action == TradeAction.Sell ? 1f : 0f,
            LlmConfidence = (float)signal.Confidence,
        };
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Segment {Segment}: meta-model trained on {N} samples, in-sample acc={Acc:F3}")]
    private static partial void LogTrained(ILogger logger, int segment, int n, double acc);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Skipping meta-model fit: only {N} adaptation samples (need ≥20)")]
    private static partial void LogSkippingFit(ILogger logger, int n);
}
