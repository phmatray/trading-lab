namespace TradingSignal.Evaluation.Metrics;

public sealed record ReturnSeriesMetrics(
    int Periods,
    double CumulativeReturnPct,
    double AnnualizedSharpe,
    double MaxDrawdownPct,
    IReadOnlyList<double> EquityCurve);

public static class ReturnMetrics
{
    public static ReturnSeriesMetrics Compute(IReadOnlyList<double> returns, int periodsPerYear)
    {
        if (returns.Count == 0)
            return new ReturnSeriesMetrics(0, 0d, 0d, 0d, new[] { 1d });

        var equity = new double[returns.Count + 1];
        equity[0] = 1d;
        for (var i = 0; i < returns.Count; i++)
            equity[i + 1] = equity[i] * (1d + returns[i]);

        var cumret = equity[^1] - 1d;

        var mean = 0d;
        for (var i = 0; i < returns.Count; i++) mean += returns[i];
        mean /= returns.Count;

        var varSum = 0d;
        for (var i = 0; i < returns.Count; i++)
        {
            var d = returns[i] - mean;
            varSum += d * d;
        }
        var variance = varSum / returns.Count;
        var stddev = Math.Sqrt(variance);
        var sharpe = stddev == 0d ? 0d : mean / stddev * Math.Sqrt(periodsPerYear);

        var peak = equity[0];
        var maxDD = 0d;
        for (var i = 1; i < equity.Length; i++)
        {
            if (equity[i] > peak) peak = equity[i];
            var dd = peak == 0d ? 0d : (equity[i] - peak) / peak;
            if (dd < maxDD) maxDD = dd;
        }

        return new ReturnSeriesMetrics(returns.Count, cumret, sharpe, maxDD, equity);
    }
}
