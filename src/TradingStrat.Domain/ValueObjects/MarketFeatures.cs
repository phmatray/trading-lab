// ReSharper disable InconsistentNaming
using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.ValueObjects;

public sealed class MarketFeatures : ValueObject
{
    // Price-based (5)
    public float DailyReturn { get; set; }
    public float LogReturn { get; set; }
    public float HighLowRange { get; set; }
    public float OpenCloseRange { get; set; }
    public float PricePosition { get; set; }

    // Moving Averages (6)
    public float SMA_5 { get; set; }
    public float SMA_10 { get; set; }
    public float SMA_20 { get; set; }
    public float EMA_12 { get; set; }
    public float EMA_26 { get; set; }
    public float PriceToSMA20 { get; set; }

    // Momentum (4)
    public float RSI_14 { get; set; }
    public float Momentum_5 { get; set; }
    public float ROC_10 { get; set; }
    public float StochRSI { get; set; }

    // MACD (3)
    public float MACD { get; set; }
    public float MACDSignal { get; set; }
    public float MACDHistogram { get; set; }

    // Volatility (4)
    public float StdDev_10 { get; set; }
    public float StdDev_20 { get; set; }
    public float ATR_14 { get; set; }
    public float BollingerPosition { get; set; }

    // Volume (4)
    public float VolumeChange { get; set; }
    public float VolumeMA_10 { get; set; }
    public float VolumeRatio { get; set; }
    public float PriceVolumeCorrelation { get; set; }

    // Target (training only)
    public float NextDayReturn { get; set; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DailyReturn;
        yield return LogReturn;
        yield return HighLowRange;
        yield return OpenCloseRange;
        yield return PricePosition;
        yield return SMA_5;
        yield return SMA_10;
        yield return SMA_20;
        yield return EMA_12;
        yield return EMA_26;
        yield return PriceToSMA20;
        yield return RSI_14;
        yield return Momentum_5;
        yield return ROC_10;
        yield return StochRSI;
        yield return MACD;
        yield return MACDSignal;
        yield return MACDHistogram;
        yield return StdDev_10;
        yield return StdDev_20;
        yield return ATR_14;
        yield return BollingerPosition;
        yield return VolumeChange;
        yield return VolumeMA_10;
        yield return VolumeRatio;
        yield return PriceVolumeCorrelation;
        yield return NextDayReturn;
    }
}

public sealed class PricePrediction : ValueObject
{
    public float Score { get; set; } // Predicted next-day return

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Score;
    }
}
