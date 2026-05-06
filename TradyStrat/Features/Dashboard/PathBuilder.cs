using System.Globalization;
using System.Text;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Dashboard;

public static class PathBuilder
{
    public static string Line(IReadOnlyList<GrowthPoint> pts, int width, int height, decimal maxValue)
    {
        if (pts.Count == 0) return "";
        if (maxValue <= 0m) maxValue = 1m;

        var sb = new StringBuilder();
        for (var i = 0; i < pts.Count; i++)
        {
            var x = pts.Count == 1 ? 0 : (decimal)i * width / (pts.Count - 1);
            var y = height - (pts[i].ValueEur / maxValue * height);
            var verb = i == 0 ? "M" : "L";
            sb.Append(verb)
              .Append(((double)x).ToString("F1", CultureInfo.InvariantCulture))
              .Append(',')
              .Append(((double)y).ToString("F1", CultureInfo.InvariantCulture))
              .Append(' ');
        }
        return sb.ToString().TrimEnd();
    }

    public static string Area(IReadOnlyList<GrowthPoint> pts, int width, int height, decimal maxValue)
    {
        if (pts.Count == 0) return "";
        var line = Line(pts, width, height, maxValue);
        return $"{line} L{width.ToString(CultureInfo.InvariantCulture)},{height.ToString(CultureInfo.InvariantCulture)} L0,{height.ToString(CultureInfo.InvariantCulture)} Z";
    }
}
