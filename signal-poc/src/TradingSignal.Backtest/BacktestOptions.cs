namespace TradingSignal.Backtest;

public sealed record BacktestOptions
{
    public int AdaptationDays { get; init; } = 180;
    public int TestDays { get; init; } = 30;
    public int StepDays { get; init; } = 30;
    public int EvaluationHorizonCandles { get; init; } = 1;
    public int CandlesPerDay { get; init; } = 24;       // 1h interval default
    public int PeriodsPerYear { get; init; } = 24 * 365;
    public double FeeBps { get; init; } = 10d;
    public bool EnableShort { get; init; }

    /// <summary>
    /// Maximum number of causally-known few-shot cases to feed the signal generator
    /// per decision. 0 (default) disables few-shot memory entirely — exact parity with
    /// the previous behaviour of passing <c>Array.Empty&lt;FewShotCase&gt;()</c>.
    /// </summary>
    public int MaxFewShot { get; init; }

    public int AdaptationCandles => AdaptationDays * CandlesPerDay;
    public int TestCandles => TestDays * CandlesPerDay;
    public int StepCandles => StepDays * CandlesPerDay;
}
