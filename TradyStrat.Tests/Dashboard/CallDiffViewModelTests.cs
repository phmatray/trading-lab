using Shouldly;
using TradyStrat.Features.AiSuggestion.CallDiff;
using TradyStrat.Features.Dashboard.Components;
using Xunit;

namespace TradyStrat.Tests.Dashboard;

public class CallDiffViewModelTests
{
    [Fact]
    public void Project_emits_changed_added_and_removed_rows()
    {
        var diff = new CallDiff(
            ActionChanged: false,
            PriorAction: null,
            ConvictionDelta: null,
            AddedCitationKeys: ["RSI(14):BTC-USD"],
            RemovedCitationKeys: ["Bollinger:BTC-USD"],
            ChangedCitations: [new CitationChange(
                Key: "200-SMA:COIN",
                PriorValue: "Below 200-SMA (260.87)",
                NewValue: "Below 200-SMA (255.10)")],
            SummaryParagraph: "ignored");

        var rows = CallDiffRowProjector.Project(diff);

        rows.Count.ShouldBe(3);
        rows[0].Kind.ShouldBe("changed");
        rows[0].Indicator.ShouldBe("200-SMA");
        rows[0].Ticker.ShouldBe("COIN");
        rows[0].Detail.ShouldContain("→");
        rows[1].Kind.ShouldBe("added");
        rows[1].Indicator.ShouldBe("RSI(14)");
        rows[1].Ticker.ShouldBe("BTC-USD");
        rows[2].Kind.ShouldBe("removed");
        rows[2].Indicator.ShouldBe("Bollinger");
        rows[2].Ticker.ShouldBe("BTC-USD");
    }

    [Fact]
    public void Project_returns_empty_for_None()
    {
        var rows = CallDiffRowProjector.Project(CallDiff.None);
        rows.ShouldBeEmpty();
    }
}
