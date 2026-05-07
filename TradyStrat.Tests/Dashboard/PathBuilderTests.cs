using Shouldly;
using TradyStrat.Features.Dashboard;
using TradyStrat.Common.Domain;
using Xunit;

namespace TradyStrat.Tests.Dashboard;

public class PathBuilderTests
{
    [Fact]
    public void Empty_input_yields_empty_path()
    {
        PathBuilder.Line([], width: 1200, height: 220, maxValue: 100m).ShouldBe("");
    }

    [Fact]
    public void Single_point_yields_single_M_command()
    {
        var pts = new[] { new GrowthPoint(new(2026,1,1), 50m) };
        PathBuilder.Line(pts, 1200, 220, 100m).ShouldStartWith("M");
    }

    [Fact]
    public void Two_points_produce_M_then_L()
    {
        var pts = new[] {
            new GrowthPoint(new(2026,1,1), 0m),
            new GrowthPoint(new(2026,1,2), 100m) };
        var d = PathBuilder.Line(pts, 1200, 220, 100m);

        d.ShouldStartWith("M");
        d.ShouldContain("L");
    }
}
