using TradyStrat.Domain;

namespace TradyStrat.Application.PriceFeed.UseCases;

public sealed record GetPriceSeriesOutput(
    IReadOnlyList<PriceBar> Bars,
    IndicatorArrays? Indicators);

/// <summary>
/// Per-bar indicator arrays aligned index-wise to <see cref="GetPriceSeriesOutput.Bars"/>.
/// Null entries represent bars inside the indicator's warmup window (insufficient history).
///
/// Shape matches what the registered <see cref="TradyStrat.Application.Indicators.History.IIndicatorHistoryProvider"/>
/// implementations actually produce:
/// - <see cref="Rsi"/>: RSI(14) values; 14 warmup bars → first entries may be null.
/// - <see cref="BollingerMid"/>: Bollinger SMA(20) midline; 20 warmup bars → first entries may be null.
///   Full upper/lower bands are not available as per-bar arrays (the provider only exposes
///   the latest band edges as ThresholdHi/Lo scalars).
/// - <see cref="Sma200"/>: SMA(200) values; 200 warmup bars → first entries may be null.
/// - <see cref="Ichimoku"/>: Ichimoku sparkline proxy (Close price); always non-null.
/// Note: Sma50 is defined in <see cref="TradyStrat.Domain.IndicatorKind"/> but has no
/// registered history provider and is therefore omitted from this DTO.
/// </summary>
public sealed record IndicatorArrays(
    IReadOnlyList<decimal?> Rsi,
    IReadOnlyList<decimal?> BollingerMid,
    IReadOnlyList<decimal?> Sma200,
    IReadOnlyList<decimal?> Ichimoku);
