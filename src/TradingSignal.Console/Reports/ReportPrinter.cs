using System.Globalization;
using System.Text;

namespace TradingSignal.ConsoleApp.Reports;

public static class ReportPrinter
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static void Print(RunReport report, TextWriter writer)
    {
        writer.WriteLine();
        writer.WriteLine($"{report.Symbol} {report.Interval} | "
            + $"{report.DataStartUtc:yyyy-MM-dd}..{report.DataEndUtc:yyyy-MM-dd} | "
            + $"{report.CandleCount} candles | fee {report.FeeBps:F1} bps");
        writer.WriteLine();

        writer.WriteLine($"{"strategy",-32} {"trades",6}  {"accuracy",10}  {"brier",8}  {"sharpe",8}  {"max DD",10}  {"cum ret",10}");
        writer.WriteLine(new string('-', 90));

        writer.WriteLine($"{"buy-and-hold",-32} {"-",6}  {"-",10}  {"-",8}  "
            + $"{Fmt(report.BuyAndHold.AnnualizedSharpe),8}  "
            + $"{FmtPct(report.BuyAndHold.MaxDrawdownPct),10}  "
            + $"{FmtPct(report.BuyAndHold.CumulativeReturnPct),10}");

        foreach (StrategyReport s in report.Strategies)
        {
            writer.WriteLine($"{Truncate(s.Label, 32),-32} "
                + $"{s.TradeCount,6}  "
                + $"{FmtPct(s.Accuracy),10}  "
                + $"{Fmt(s.BrierScore),8}  "
                + $"{Fmt(s.AnnualizedSharpe),8}  "
                + $"{FmtPct(s.MaxDrawdownPct),10}  "
                + $"{FmtPct(s.CumulativeReturnPct),10}");
        }
        writer.WriteLine();

        foreach (StrategyReport s in report.Strategies)
        {
            if (s.Segments.Count == 0) continue;
            writer.WriteLine($"{s.Label} — per segment:");
            writer.WriteLine($"  {"seg",3}  {"test start",12}  {"preds",5}  {"trades",6}  {"acc",6}  {"brier",6}  {"sharpe",7}  {"ddn",7}  {"cum",7}  {"τ*",5}  {"meta acc",8}");
            foreach (SegmentReport seg in s.Segments)
            {
                StringBuilder line = new();
                line.Append("  ").Append(seg.Segment.ToString("D3", Inv));
                line.Append("  ").Append(seg.TestStartUtc.ToString("yyyy-MM-dd", Inv).PadRight(12));
                line.Append("  ").Append(seg.Predictions.ToString(Inv).PadLeft(5));
                line.Append("  ").Append(seg.Trades.ToString(Inv).PadLeft(6));
                line.Append("  ").Append(FmtPct(seg.Accuracy).PadLeft(6));
                line.Append("  ").Append(Fmt(seg.BrierScore).PadLeft(6));
                line.Append("  ").Append(Fmt(seg.AnnualizedSharpe).PadLeft(7));
                line.Append("  ").Append(FmtPct(seg.MaxDrawdownPct).PadLeft(7));
                line.Append("  ").Append(FmtPct(seg.CumulativeReturnPct).PadLeft(7));
                line.Append("  ").Append((seg.SelectedThreshold?.ToString("F2", Inv) ?? "-").PadLeft(5));
                line.Append("  ").Append((seg.MetaModelTrainAccuracy?.ToString("F3", Inv) ?? "-").PadLeft(8));
                writer.WriteLine(line.ToString());
            }
            writer.WriteLine();
        }
    }

    private static string Fmt(double v) => v.ToString("F3", Inv);
    private static string FmtPct(double v) => (v * 100d).ToString("F2", Inv) + "%";
    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
