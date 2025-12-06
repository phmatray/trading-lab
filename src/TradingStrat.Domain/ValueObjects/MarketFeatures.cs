namespace TradingStrat.Domain.ValueObjects;

public class MarketFeatures
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
}

public class PricePrediction
{
    public float Score { get; set; } // Predicted next-day return
}
