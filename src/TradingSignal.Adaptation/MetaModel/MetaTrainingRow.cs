using Microsoft.ML.Data;

namespace TradingSignal.Adaptation.MetaModel;

internal sealed class MetaTrainingRow
{
    public float Rsi14 { get; set; }
    public float MacdHistogram { get; set; }
    public float EmaRatio { get; set; }
    public float Atr14 { get; set; }
    public float Return1 { get; set; }
    public float Return5 { get; set; }
    public float VolatilityPct { get; set; }
    public float LlmActionBuy { get; set; }
    public float LlmActionSell { get; set; }
    public float LlmConfidence { get; set; }
    public bool Label { get; set; }
}

internal sealed class MetaPredictionRow
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    [ColumnName("Probability")]
    public float Probability { get; set; }

    [ColumnName("Score")]
    public float Score { get; set; }
}
