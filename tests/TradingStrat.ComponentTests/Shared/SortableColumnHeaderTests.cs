using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;
using SortColumn = TradingStrat.Application.Ports.Inbound.SortColumn;
using SortDirection = TradingStrat.Application.Ports.Inbound.SortDirection;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the SortableColumnHeader component.
/// </summary>
public class SortableColumnHeaderTests : BunitTestContext
{
    [Fact]
    public void SortableColumnHeader_RendersLabel()
    {
        // Arrange & Act
        var cut = Render<SortableColumnHeader>(parameters => parameters
            .Add(p => p.Label, "Ticker")
            .Add(p => p.Column, SortColumn.Ticker)
            .Add(p => p.OnSort, EventCallback.Factory.Create<(SortColumn, SortDirection)>(this, _ => { })));

        // Assert
        cut.Find("span").TextContent.ShouldBe("Ticker");
    }

    [Fact]
    public void SortableColumnHeader_ShowsNoIconWhenNotSorted()
    {
        // Arrange & Act
        var cut = Render<SortableColumnHeader>(parameters => parameters
            .Add(p => p.Label, "Coverage %")
            .Add(p => p.Column, SortColumn.Coverage)
            .Add(p => p.CurrentSortColumn, SortColumn.Ticker) // Different column is sorted
            .Add(p => p.OnSort, EventCallback.Factory.Create<(SortColumn, SortDirection)>(this, _ => { })));

        // Assert
        var icons = cut.FindAll("[data-name]");
        icons.ShouldBeEmpty(); // No sort icon when not the current sort column
    }

    [Fact]
    public void SortableColumnHeader_ShowsUpArrowForAscending()
    {
        // Arrange & Act
        var cut = Render<SortableColumnHeader>(parameters => parameters
            .Add(p => p.Label, "Ticker")
            .Add(p => p.Column, SortColumn.Ticker)
            .Add(p => p.CurrentSortColumn, SortColumn.Ticker)
            .Add(p => p.CurrentSortDirection, SortDirection.Ascending)
            .Add(p => p.OnSort, EventCallback.Factory.Create<(SortColumn, SortDirection)>(this, _ => { })));

        // Assert
        cut.Markup.ShouldContain("chevron-up");
    }

    [Fact]
    public void SortableColumnHeader_ShowsDownArrowForDescending()
    {
        // Arrange & Act
        var cut = Render<SortableColumnHeader>(parameters => parameters
            .Add(p => p.Label, "Records")
            .Add(p => p.Column, SortColumn.RecordCount)
            .Add(p => p.CurrentSortColumn, SortColumn.RecordCount)
            .Add(p => p.CurrentSortDirection, SortDirection.Descending)
            .Add(p => p.OnSort, EventCallback.Factory.Create<(SortColumn, SortDirection)>(this, _ => { })));

        // Assert
        cut.Markup.ShouldContain("chevron-down");
    }

    [Fact]
    public void SortableColumnHeader_ClickTogglesFromAscendingToDescending()
    {
        // Arrange
        (SortColumn, SortDirection)? captured = null;
        var cut = Render<SortableColumnHeader>(parameters => parameters
            .Add(p => p.Label, "Ticker")
            .Add(p => p.Column, SortColumn.Ticker)
            .Add(p => p.CurrentSortColumn, SortColumn.Ticker)
            .Add(p => p.CurrentSortDirection, SortDirection.Ascending)
            .Add(p => p.OnSort, EventCallback.Factory.Create<(SortColumn, SortDirection)>(this, value => captured = value)));

        // Act
        var header = cut.Find("th");
        header.Click();

        // Assert
        captured.ShouldNotBeNull();
        captured.Value.Item1.ShouldBe(SortColumn.Ticker);
        captured.Value.Item2.ShouldBe(SortDirection.Descending);
    }

    [Fact]
    public void SortableColumnHeader_ClickTogglesFromDescendingToAscending()
    {
        // Arrange
        (SortColumn, SortDirection)? captured = null;
        var cut = Render<SortableColumnHeader>(parameters => parameters
            .Add(p => p.Label, "Coverage %")
            .Add(p => p.Column, SortColumn.Coverage)
            .Add(p => p.CurrentSortColumn, SortColumn.Coverage)
            .Add(p => p.CurrentSortDirection, SortDirection.Descending)
            .Add(p => p.OnSort, EventCallback.Factory.Create<(SortColumn, SortDirection)>(this, value => captured = value)));

        // Act
        cut.Find("th").Click();

        // Assert
        captured.ShouldNotBeNull();
        captured.Value.Item1.ShouldBe(SortColumn.Coverage);
        captured.Value.Item2.ShouldBe(SortDirection.Ascending);
    }

    [Fact]
    public void SortableColumnHeader_ClickOnNewColumnSortsAscending()
    {
        // Arrange
        (SortColumn, SortDirection)? captured = null;
        var cut = Render<SortableColumnHeader>(parameters => parameters
            .Add(p => p.Label, "Latest Date")
            .Add(p => p.Column, SortColumn.LatestDate)
            .Add(p => p.CurrentSortColumn, SortColumn.Ticker) // Different column is current
            .Add(p => p.CurrentSortDirection, SortDirection.Descending)
            .Add(p => p.OnSort, EventCallback.Factory.Create<(SortColumn, SortDirection)>(this, value => captured = value)));

        // Act
        cut.Find("th").Click();

        // Assert
        captured.ShouldNotBeNull();
        captured.Value.Item1.ShouldBe(SortColumn.LatestDate);
        captured.Value.Item2.ShouldBe(SortDirection.Ascending); // Always starts with ascending
    }

    [Fact]
    public void SortableColumnHeader_IsAccessible()
    {
        // Arrange & Act
        var cut = Render<SortableColumnHeader>(parameters => parameters
            .Add(p => p.Label, "Ticker")
            .Add(p => p.Column, SortColumn.Ticker)
            .Add(p => p.OnSort, EventCallback.Factory.Create<(SortColumn, SortDirection)>(this, _ => { })));

        // Assert
        var header = cut.Find("th");
        header.GetAttribute("role").ShouldBe("button");
        header.GetAttribute("tabindex").ShouldBe("0");
    }

    [Fact]
    public void SortableColumnHeader_KeyboardEnterTriggersSort()
    {
        // Arrange
        (SortColumn, SortDirection)? captured = null;
        var cut = Render<SortableColumnHeader>(parameters => parameters
            .Add(p => p.Label, "Records")
            .Add(p => p.Column, SortColumn.RecordCount)
            .Add(p => p.OnSort, EventCallback.Factory.Create<(SortColumn, SortDirection)>(this, value => captured = value)));

        // Act
        var header = cut.Find("th");
        header.KeyDown(key: "Enter");

        // Assert
        captured.ShouldNotBeNull();
        captured.Value.Item1.ShouldBe(SortColumn.RecordCount);
    }

    [Fact]
    public void SortableColumnHeader_HasCorrectStyling()
    {
        // Arrange & Act
        var cut = Render<SortableColumnHeader>(parameters => parameters
            .Add(p => p.Label, "Ticker")
            .Add(p => p.Column, SortColumn.Ticker)
            .Add(p => p.OnSort, EventCallback.Factory.Create<(SortColumn, SortDirection)>(this, _ => { })));

        // Assert
        var header = cut.Find("th");
        header.ClassList.ShouldContain("px-6");
        header.ClassList.ShouldContain("py-3");
        header.ClassList.ShouldContain("cursor-pointer");
        header.ClassList.ShouldContain("hover:bg-gray-100");
    }
}
