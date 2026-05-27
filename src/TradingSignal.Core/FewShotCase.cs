namespace TradingSignal.Core;

public sealed record FewShotCase(
    FeatureSet Features,
    TradeAction ActualBestAction,
    double RealizedReturnPct);
