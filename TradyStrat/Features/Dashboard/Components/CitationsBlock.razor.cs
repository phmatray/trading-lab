using System.Globalization;
using Microsoft.AspNetCore.Components;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Features.AiSuggestion.Backfill;
using TradyStrat.Features.AiSuggestion.CallDiff;
using TradyStrat.Features.AiSuggestion.Snapshot;
using TradyStrat.Features.Indicators;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Dashboard.Components;

public partial class CitationsBlock : ComponentBase
{
    [Parameter, EditorRequired] public Suggestion Sug { get; set; } = null!;
    [Parameter] public DateOnly Today { get; set; }

    [Parameter, EditorRequired] public CallDiff CallDiff { get; set; } = CallDiff.None;

    [Parameter, EditorRequired]
    public IReadOnlyDictionary<(string Ticker, IndicatorKind Kind), IndicatorSeries> IndicatorHistories { get; set; }
        = new Dictionary<(string, IndicatorKind), IndicatorSeries>();

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

    private static string RomanLowercase(int i) =>
        i switch
        {
            1 => "i", 2 => "ii", 3 => "iii", 4 => "iv", 5 => "v",
            6 => "vi", 7 => "vii", 8 => "viii", 9 => "ix", 10 => "x",
            11 => "xi", 12 => "xii",
            _ => i.ToString(CultureInfo.InvariantCulture),
        };

    /// <summary>
    /// Renders a small inline SVG sparkline for an <see cref="IndicatorSeries"/>.
    /// All interpolated values are numeric/constant — safe for <c>MarkupString</c>.
    /// Larger here than in the old card view because the citations block is full-width:
    /// 92×22 with a small dot at the latest value.
    /// </summary>
    private static string RenderSparklineSvg(IndicatorSeries s)
    {
        if (s.Values.Count < 2) return "";
        decimal min = s.Values.Min(), max = s.Values.Max();
        if (s.ThresholdHi is { } hi) { if (hi < min) min = hi; if (hi > max) max = hi; }
        if (s.ThresholdLo is { } lo) { if (lo < min) min = lo; if (lo > max) max = lo; }
        var range = max - min;
        if (range == 0m) range = 1m;

        const int W = 92, H = 22;
        var pts = new System.Text.StringBuilder();
        double lastX = 0, lastY = 0;
        for (int i = 0; i < s.Values.Count; i++)
        {
            var x = (double)i * W / (s.Values.Count - 1);
            var y = H - (double)((s.Values[i] - min) / range) * H;
            if (i > 0) pts.Append(' ');
            pts.Append(string.Create(CultureInfo.InvariantCulture, $"{x:0.##},{y:0.##}"));
            lastX = x; lastY = y;
        }

        var thresholdLines = new System.Text.StringBuilder();
        void DrawThreshold(decimal? t, string strokeStyle)
        {
            if (t is null) return;
            var ty = H - (double)((t.Value - min) / range) * H;
            var tyStr = ty.ToString("0.##", CultureInfo.InvariantCulture);
            thresholdLines.Append(
                "<line x1=\"0\" y1=\"" + tyStr + "\" " +
                "x2=\"" + W.ToString(CultureInfo.InvariantCulture) + "\" y2=\"" + tyStr + "\" " +
                "stroke=\"" + strokeStyle + "\" stroke-dasharray=\"2 3\" />");
        }
        DrawThreshold(s.ThresholdHi, "rgba(196,154,86,0.30)");
        DrawThreshold(s.ThresholdLo, "rgba(196,154,86,0.30)");

        var lastXStr = lastX.ToString("0.##", CultureInfo.InvariantCulture);
        var lastYStr = lastY.ToString("0.##", CultureInfo.InvariantCulture);

        return $"<svg viewBox=\"0 0 {W} {H}\" width=\"{W}\" height=\"{H}\" preserveAspectRatio=\"xMaxYMid meet\">" +
               thresholdLines +
               $"<polyline points=\"{pts}\" fill=\"none\" stroke=\"#c49a56\" stroke-width=\"1.3\" stroke-linejoin=\"round\" stroke-linecap=\"round\" />" +
               $"<circle cx=\"{lastXStr}\" cy=\"{lastYStr}\" r=\"2.2\" fill=\"#ece6d6\" stroke=\"#c49a56\" stroke-width=\"1\" />" +
               "</svg>";
    }
}
