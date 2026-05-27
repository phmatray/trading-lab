using TradingSignal.Core;

namespace TradingSignal.Backtest;

// Single-asset portfolio simulator. Long/flat by default; long/short when
// EnableShort=true. Execution always at the next bar's open, mark-to-market at
// the close of that same bar so per-bar returns reflect both the trade and the
// price move within the bar.
public sealed class Portfolio
{
    private readonly double _feeRate;
    private readonly bool _enableShort;

    private readonly List<double> _equityCurve = new();
    private readonly List<double> _perBarReturns = new();

    public double Cash { get; private set; } = 1d;
    public double Position { get; private set; }
    public IReadOnlyList<double> EquityCurve => _equityCurve;
    public IReadOnlyList<double> PerBarReturns => _perBarReturns;
    public int TradeCount { get; private set; }

    private double _previousEquity = 1d;

    public Portfolio(double feeBps, bool enableShort)
    {
        _feeRate = feeBps / 10_000d;
        _enableShort = enableShort;
    }

    public void Execute(TradeAction action, decimal executionPrice, decimal markPrice)
    {
        double execPx = (double)executionPrice;
        if (execPx <= 0d) throw new ArgumentOutOfRangeException(nameof(executionPrice));

        switch (action)
        {
            case TradeAction.Buy when Position == 0d:
                EnterLong(execPx);
                TradeCount++;
                break;
            case TradeAction.Buy when Position < 0d && _enableShort:
                ExitShort(execPx);
                EnterLong(execPx);
                TradeCount += 2;
                break;
            case TradeAction.Sell when Position > 0d:
                ExitLong(execPx);
                TradeCount++;
                break;
            case TradeAction.Sell when Position == 0d && _enableShort:
                EnterShort(execPx);
                TradeCount++;
                break;
            default:
                // Hold, or signal incompatible with current state.
                break;
        }

        double mark = (double)markPrice;
        double equity = Cash + Position * mark;
        _equityCurve.Add(equity);
        double r = _previousEquity == 0d ? 0d : (equity - _previousEquity) / _previousEquity;
        _perBarReturns.Add(r);
        _previousEquity = equity;
    }

    private void EnterLong(double execPx)
    {
        double spend = Cash;
        Position = spend / execPx * (1d - _feeRate);
        Cash = 0d;
    }

    private void ExitLong(double execPx)
    {
        double gross = Position * execPx;
        Cash = gross * (1d - _feeRate);
        Position = 0d;
    }

    private void EnterShort(double execPx)
    {
        // Short an amount equivalent to current cash. Position becomes negative;
        // cash holds (collateral + proceeds − fee). Mark-to-market: cash + pos*mark
        // = collateral − units*(mark − exec) − fee. Profitable when price falls.
        double units = Cash / execPx;
        Position = -units;
        Cash += units * execPx * (1d - _feeRate);
    }

    private void ExitShort(double execPx)
    {
        double buyBackCost = -Position * execPx;
        Cash -= buyBackCost * (1d + _feeRate);
        Position = 0d;
    }
}
