using Shouldly;
using TradingSignal.Evaluation.Metrics;

namespace TradingSignal.Evaluation.Tests;

public sealed class ReturnMetricsTests
{
    [Fact]
    public void Empty_Returns_Are_All_Zero()
    {
        var m = ReturnMetrics.Compute(Array.Empty<double>(), periodsPerYear: 8760);

        m.CumulativeReturnPct.ShouldBe(0d);
        m.AnnualizedSharpe.ShouldBe(0d);
        m.MaxDrawdownPct.ShouldBe(0d);
    }

    [Fact]
    public void Cumulative_Return_Compounds()
    {
        // (1+0.10)(1+0.10) - 1 = 0.21
        var m = ReturnMetrics.Compute(new[] { 0.10, 0.10 }, periodsPerYear: 8760);

        m.CumulativeReturnPct.ShouldBe(0.21, 1e-12);
    }

    [Fact]
    public void Constant_Returns_Produce_Zero_Sharpe_Because_Variance_Is_Zero()
    {
        var m = ReturnMetrics.Compute(new[] { 0.01, 0.01, 0.01 }, periodsPerYear: 8760);

        m.AnnualizedSharpe.ShouldBe(0d);
    }

    [Fact]
    public void Max_Drawdown_Captures_Worst_Peak_To_Trough()
    {
        // Returns: -10%, +5%, -5%
        // Equity:  1.0 → 0.9 → 0.945 → 0.89775
        // Peak = 1.0 throughout. Worst drawdown = (0.89775 - 1.0)/1.0 = -0.10225
        var m = ReturnMetrics.Compute(new[] { -0.10, 0.05, -0.05 }, periodsPerYear: 8760);

        m.MaxDrawdownPct.ShouldBe(-0.10225, 1e-12);
    }

    [Fact]
    public void Sharpe_On_Hand_Computed_Series()
    {
        // Returns: 0.01, -0.01, 0.02, -0.02. Mean = 0. So Sharpe = 0 / stddev * sqrtN = 0.
        var m = ReturnMetrics.Compute(new[] { 0.01, -0.01, 0.02, -0.02 }, periodsPerYear: 8760);

        m.AnnualizedSharpe.ShouldBe(0d, 1e-12);
    }

    [Fact]
    public void Positive_Mean_Produces_Positive_Sharpe()
    {
        // Slight upward drift with low variance → positive Sharpe.
        var m = ReturnMetrics.Compute(new[] { 0.02, 0.01, 0.02, 0.01 }, periodsPerYear: 8760);

        m.AnnualizedSharpe.ShouldBeGreaterThan(0d);
    }

    [Fact]
    public void Equity_Curve_Length_Equals_Returns_Plus_One()
    {
        var m = ReturnMetrics.Compute(new[] { 0.01, 0.02, 0.03 }, periodsPerYear: 8760);

        m.EquityCurve.Count.ShouldBe(4);
        m.EquityCurve[0].ShouldBe(1d);
    }
}
