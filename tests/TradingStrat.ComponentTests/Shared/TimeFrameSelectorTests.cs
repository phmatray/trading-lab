using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the TimeFrameSelector component.
/// </summary>
public class TimeFrameSelectorTests : BunitTestContext
{
    [Fact]
    public void TimeFrameSelector_RendersWithDefaultLabel()
    {
        // Arrange & Act
        var cut = Render<TimeFrameSelector>(parameters => parameters
            .Add(p => p.OnTimeFrameChanged, EventCallback.Factory.Create<TimeFrame>(this, _ => { })));

        // Assert
        cut.Markup.ShouldContain("Timeframe:");
    }

    [Fact]
    public void TimeFrameSelector_RendersWithCustomLabel()
    {
        // Arrange & Act
        var cut = Render<TimeFrameSelector>(parameters => parameters
            .Add(p => p.Label, "Select Period:")
            .Add(p => p.OnTimeFrameChanged, EventCallback.Factory.Create<TimeFrame>(this, _ => { })));

        // Assert
        cut.Markup.ShouldContain("Select Period:");
    }

    [Fact]
    public void TimeFrameSelector_DisplaysAllAvailableTimeFrames()
    {
        // Arrange & Act
        var cut = Render<TimeFrameSelector>(parameters => parameters
            .Add(p => p.OnTimeFrameChanged, EventCallback.Factory.Create<TimeFrame>(this, _ => { })));

        // Assert
        var select = cut.Find("select");
        var options = select.QuerySelectorAll("option");
        options.Length.ShouldBe(9); // M1, M5, M15, M30, H1, H4, D1, W1, MN1

        // Verify all timeframes are present
        cut.Markup.ShouldContain("1 Minute (M1)");
        cut.Markup.ShouldContain("5 Minutes (M5)");
        cut.Markup.ShouldContain("15 Minutes (M15)");
        cut.Markup.ShouldContain("30 Minutes (M30)");
        cut.Markup.ShouldContain("1 Hour (H1)");
        cut.Markup.ShouldContain("4 Hours (H4)");
        cut.Markup.ShouldContain("Daily (D1)");
        cut.Markup.ShouldContain("Weekly (W1)");
        cut.Markup.ShouldContain("Monthly (MN1)");
    }

    [Fact]
    public void TimeFrameSelector_DisplaysCustomSubsetOfTimeFrames()
    {
        // Arrange & Act
        var cut = Render<TimeFrameSelector>(parameters => parameters
            .Add(p => p.AvailableTimeFrames, new[] { TimeFrameUnit.D1, TimeFrameUnit.W1, TimeFrameUnit.MN1 })
            .Add(p => p.OnTimeFrameChanged, EventCallback.Factory.Create<TimeFrame>(this, _ => { })));

        // Assert
        var select = cut.Find("select");
        var options = select.QuerySelectorAll("option");
        options.Length.ShouldBe(3);
        cut.Markup.ShouldContain("Daily (D1)");
        cut.Markup.ShouldContain("Weekly (W1)");
        cut.Markup.ShouldContain("Monthly (MN1)");
        cut.Markup.ShouldNotContain("1 Minute (M1)");
    }

    [Fact]
    public void TimeFrameSelector_HasDefaultSelectionD1()
    {
        // Arrange & Act
        var cut = Render<TimeFrameSelector>(parameters => parameters
            .Add(p => p.OnTimeFrameChanged, EventCallback.Factory.Create<TimeFrame>(this, _ => { })));

        // Assert
        var select = cut.Find("select");
        select.GetAttribute("value").ShouldBe("D1");
    }

    [Fact]
    public void TimeFrameSelector_RespectsCustomInitialSelection()
    {
        // Arrange & Act
        var cut = Render<TimeFrameSelector>(parameters => parameters
            .Add(p => p.SelectedTimeFrame, new TimeFrame { Unit = TimeFrameUnit.H1 })
            .Add(p => p.OnTimeFrameChanged, EventCallback.Factory.Create<TimeFrame>(this, _ => { })));

        // Assert
        var select = cut.Find("select");
        select.GetAttribute("value").ShouldBe("H1");
    }

    [Fact]
    public void TimeFrameSelector_ChangeEventTriggersCallback()
    {
        // Arrange
        TimeFrame? capturedTimeFrame = null;
        var cut = Render<TimeFrameSelector>(parameters => parameters
            .Add(p => p.OnTimeFrameChanged, EventCallback.Factory.Create<TimeFrame>(this, tf => capturedTimeFrame = tf)));

        // Act
        var select = cut.Find("select");
        select.Change("W1");

        // Assert
        capturedTimeFrame.ShouldNotBeNull();
        capturedTimeFrame.Unit.ShouldBe(TimeFrameUnit.W1);
    }

    [Fact]
    public void TimeFrameSelector_DisplaysCorrectTimeFrameNames()
    {
        // Arrange & Act
        var cut = Render<TimeFrameSelector>(parameters => parameters
            .Add(p => p.OnTimeFrameChanged, EventCallback.Factory.Create<TimeFrame>(this, _ => { })));

        // Assert - Verify display names match expected format
        var options = cut.FindAll("option");
        options[0].TextContent.ShouldBe("1 Minute (M1)");
        options[1].TextContent.ShouldBe("5 Minutes (M5)");
        options[2].TextContent.ShouldBe("15 Minutes (M15)");
        options[3].TextContent.ShouldBe("30 Minutes (M30)");
        options[4].TextContent.ShouldBe("1 Hour (H1)");
        options[5].TextContent.ShouldBe("4 Hours (H4)");
        options[6].TextContent.ShouldBe("Daily (D1)");
        options[7].TextContent.ShouldBe("Weekly (W1)");
        options[8].TextContent.ShouldBe("Monthly (MN1)");
    }

    [Fact]
    public void TimeFrameSelector_HasCorrectAriaLabel()
    {
        // Arrange & Act
        var cut = Render<TimeFrameSelector>(parameters => parameters
            .Add(p => p.Label, "Choose timeframe")
            .Add(p => p.OnTimeFrameChanged, EventCallback.Factory.Create<TimeFrame>(this, _ => { })));

        // Assert
        var select = cut.Find("select");
        select.GetAttribute("aria-label").ShouldBe("Choose timeframe");
    }

    [Fact]
    public void TimeFrameSelector_HasCorrectStyling()
    {
        // Arrange & Act
        var cut = Render<TimeFrameSelector>(parameters => parameters
            .Add(p => p.OnTimeFrameChanged, EventCallback.Factory.Create<TimeFrame>(this, _ => { })));

        // Assert
        var select = cut.Find("select");
        select.ClassList.ShouldContain("px-4");
        select.ClassList.ShouldContain("py-2");
        select.ClassList.ShouldContain("rounded-lg");
        select.ClassList.ShouldContain("border");
        select.ClassList.ShouldContain("dark:bg-gray-700");
    }

    [Fact]
    public void TimeFrameSelector_SelectElementHasCorrectId()
    {
        // Arrange & Act
        var cut = Render<TimeFrameSelector>(parameters => parameters
            .Add(p => p.OnTimeFrameChanged, EventCallback.Factory.Create<TimeFrame>(this, _ => { })));

        // Assert
        var select = cut.Find("select");
        select.GetAttribute("id").ShouldBe("timeframe-selector");
    }

    [Fact]
    public void TimeFrameSelector_LabelHasCorrectForAttribute()
    {
        // Arrange & Act
        var cut = Render<TimeFrameSelector>(parameters => parameters
            .Add(p => p.OnTimeFrameChanged, EventCallback.Factory.Create<TimeFrame>(this, _ => { })));

        // Assert
        var label = cut.Find("label");
        label.GetAttribute("for").ShouldBe("timeframe-selector");
    }
}
