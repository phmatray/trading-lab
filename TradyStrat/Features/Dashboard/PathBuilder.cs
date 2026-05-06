using System.Globalization;
using System.Text;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Dashboard;

public static class PathBuilder
{
    public static string Line(IReadOnlyList<GrowthPoint> pts, int width, int height, decimal maxValue, DateOnly? endDate = null)
    {
        if (pts.Count == 0) return "";
        if (maxValue <= 0m) maxValue = 1m;

        var sb = new StringBuilder();
        for (var i = 0; i < pts.Count; i++)
        {
            var x = XForIndex(pts, i, width, endDate);
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

    public static string Area(IReadOnlyList<GrowthPoint> pts, int width, int height, decimal maxValue, DateOnly? endDate = null)
    {
        if (pts.Count == 0) return "";
        var line = Line(pts, width, height, maxValue, endDate);
        var lastX = XForIndex(pts, pts.Count - 1, width, endDate);
        var lastXStr = ((double)lastX).ToString("F1", CultureInfo.InvariantCulture);
        var heightStr = height.ToString(CultureInfo.InvariantCulture);
        return $"{line} L{lastXStr},{heightStr} L0,{heightStr} Z";
    }

    /// <summary>
    /// X mapped from the point's date along the timeline [first → endDate ?? last].
    /// When endDate is null or in the past, falls back to even index spacing.
    /// </summary>
    public static decimal XForIndex(IReadOnlyList<GrowthPoint> pts, int i, int width, DateOnly? endDate)
    {
        if (pts.Count <= 1) return 0;

        var first = pts[0].Date;
        var last  = pts[^1].Date;
        var end   = endDate is { } ed && ed > first ? ed : last;
        var span  = end.DayNumber - first.DayNumber;
        if (span <= 0)
            return (decimal)i * width / (pts.Count - 1);

        var offset = pts[i].Date.DayNumber - first.DayNumber;
        var x = (decimal)offset * width / span;
        return x > width ? width : x;
    }
}
