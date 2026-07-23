using System.Globalization;
using System.Text;

namespace TradyStrat.Features.Dashboard.Components;

/// <summary>
/// Small inline-SVG spark renderer shared by the citation rows and the
/// portfolio rail. All interpolated values are numeric — safe for use with
/// <c>MarkupString</c>.
/// </summary>
internal static class Sparkline
{
    public static string Render(IReadOnlyList<decimal> values, int width, int height)
    {
        if (values.Count < 2) return "";

        decimal min = values[0], max = values[0];
        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] < min) min = values[i];
            if (values[i] > max) max = values[i];
        }
        var range = max - min;
        if (range == 0m) range = 1m;

        var pts = new StringBuilder();
        double lastX = 0, lastY = 0;
        for (int i = 0; i < values.Count; i++)
        {
            var x = (double)i * width / (values.Count - 1);
            var y = height - (double)((values[i] - min) / range) * height;
            if (i > 0) pts.Append(' ');
            pts.Append(string.Create(CultureInfo.InvariantCulture, $"{x:0.##},{y:0.##}"));
            lastX = x; lastY = y;
        }

        var lastXStr = lastX.ToString("0.##", CultureInfo.InvariantCulture);
        var lastYStr = lastY.ToString("0.##", CultureInfo.InvariantCulture);

        return $"<svg viewBox=\"0 0 {width} {height}\" width=\"{width}\" height=\"{height}\" preserveAspectRatio=\"xMaxYMid meet\">" +
               $"<polyline points=\"{pts}\" fill=\"none\" stroke=\"#c49a56\" stroke-width=\"1.3\" stroke-linejoin=\"round\" stroke-linecap=\"round\" />" +
               $"<circle cx=\"{lastXStr}\" cy=\"{lastYStr}\" r=\"2.2\" fill=\"#ece6d6\" stroke=\"#c49a56\" stroke-width=\"1\" />" +
               "</svg>";
    }
}
