using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;
using static TradingStrat.Web.Components.Shared.FilterPanel;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the FilterPanel component.
/// </summary>
public class FilterPanelTests : BunitTestContext
{
    [Fact]
    public void FilterPanel_RendersWithAllFilterControls()
    {
        // Arrange & Act
        IRenderedComponent<FilterPanel> cut = Render<FilterPanel>(parameters => parameters
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create<FilterValues>(this, _ => { }))
            .Add(p => p.OnReset, EventCallback.Factory.Create(this, () => { })));

        // Assert
        cut.Find("select#status-filter").ShouldNotBeNull();
        cut.Find("input#min-coverage").ShouldNotBeNull();
        cut.Find("input#max-coverage").ShouldNotBeNull();
        cut.Markup.ShouldContain("Status");
        cut.Markup.ShouldContain("Coverage Range");
    }

    [Fact]
    public void FilterPanel_StartsExpanded()
    {
        // Arrange & Act
        IRenderedComponent<FilterPanel> cut = Render<FilterPanel>(parameters => parameters
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create<FilterValues>(this, _ => { }))
            .Add(p => p.OnReset, EventCallback.Factory.Create(this, () => { })));

        // Assert - Filter controls should be visible
        cut.Find("select#status-filter").ShouldNotBeNull();
    }

    [Fact]
    public void FilterPanel_CollapseButtonTogglesVisibility()
    {
        // Arrange
        IRenderedComponent<FilterPanel> cut = Render<FilterPanel>(parameters => parameters
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create<FilterValues>(this, _ => { }))
            .Add(p => p.OnReset, EventCallback.Factory.Create(this, () => { })));

        // Act - Click collapse button
        IElement collapseButton = cut.Find("button[aria-label*='Collapse']");
        collapseButton.Click();

        // Assert - Filter controls should be hidden
        cut.FindAll("select#status-filter").ShouldBeEmpty();

        // Act - Click expand button
        IElement expandButton = cut.Find("button[aria-label*='Expand']");
        expandButton.Click();

        // Assert - Filter controls should be visible again
        cut.Find("select#status-filter").ShouldNotBeNull();
    }

    [Fact]
    public void FilterPanel_StatusFilterChangeTriggersCallback()
    {
        // Arrange
        FilterValues? capturedValues = null;
        IRenderedComponent<FilterPanel> cut = Render<FilterPanel>(parameters => parameters
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create<FilterValues>(this, values => capturedValues = values))
            .Add(p => p.OnReset, EventCallback.Factory.Create(this, () => { })));

        // Act
        IElement select = cut.Find("select#status-filter");
        select.Change(DataStatusFilter.Complete.ToString());

        // Assert
        capturedValues.ShouldNotBeNull();
        capturedValues.StatusFilter.ShouldBe(DataStatusFilter.Complete);
    }

    [Fact]
    public void FilterPanel_MinCoverageChangeTriggersCallback()
    {
        // Arrange
        FilterValues? capturedValues = null;
        IRenderedComponent<FilterPanel> cut = Render<FilterPanel>(parameters => parameters
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create<FilterValues>(this, values => capturedValues = values))
            .Add(p => p.OnReset, EventCallback.Factory.Create(this, () => { })));

        // Act
        IElement input = cut.Find("input#min-coverage");
        input.Change("80");

        // Assert
        capturedValues.ShouldNotBeNull();
        capturedValues.MinCoverage.ShouldBe(80m);
    }

    [Fact]
    public void FilterPanel_MaxCoverageChangeTriggersCallback()
    {
        // Arrange
        FilterValues? capturedValues = null;
        IRenderedComponent<FilterPanel> cut = Render<FilterPanel>(parameters => parameters
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create<FilterValues>(this, values => capturedValues = values))
            .Add(p => p.OnReset, EventCallback.Factory.Create(this, () => { })));

        // Act
        IElement input = cut.Find("input#max-coverage");
        input.Change("95");

        // Assert
        capturedValues.ShouldNotBeNull();
        capturedValues.MaxCoverage.ShouldBe(95m);
    }

    [Fact]
    public void FilterPanel_ResetButtonHiddenWhenNoFilters()
    {
        // Arrange & Act
        IRenderedComponent<FilterPanel> cut = Render<FilterPanel>(parameters => parameters
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create<FilterValues>(this, _ => { }))
            .Add(p => p.OnReset, EventCallback.Factory.Create(this, () => { })));

        // Assert
        cut.Markup.ShouldNotContain("Reset");
    }

    [Fact]
    public void FilterPanel_ResetButtonAppearsWhenStatusFilterActive()
    {
        // Arrange & Act
        IRenderedComponent<FilterPanel> cut = Render<FilterPanel>(parameters => parameters
            .Add(p => p.StatusFilter, DataStatusFilter.Complete)
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create<FilterValues>(this, _ => { }))
            .Add(p => p.OnReset, EventCallback.Factory.Create(this, () => { })));

        // Assert
        cut.Markup.ShouldContain("Reset");
    }

    [Fact]
    public void FilterPanel_ResetButtonAppearsWhenMinCoverageActive()
    {
        // Arrange & Act
        IRenderedComponent<FilterPanel> cut = Render<FilterPanel>(parameters => parameters
            .Add(p => p.MinCoverage, 50m)
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create<FilterValues>(this, _ => { }))
            .Add(p => p.OnReset, EventCallback.Factory.Create(this, () => { })));

        // Assert
        cut.Markup.ShouldContain("Reset");
    }

    [Fact]
    public void FilterPanel_ResetButtonAppearsWhenMaxCoverageActive()
    {
        // Arrange & Act
        IRenderedComponent<FilterPanel> cut = Render<FilterPanel>(parameters => parameters
            .Add(p => p.MaxCoverage, 90m)
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create<FilterValues>(this, _ => { }))
            .Add(p => p.OnReset, EventCallback.Factory.Create(this, () => { })));

        // Assert
        cut.Markup.ShouldContain("Reset");
    }

    [Fact]
    public void FilterPanel_ResetButtonTriggersCallback()
    {
        // Arrange
        bool resetCalled = false;
        IRenderedComponent<FilterPanel> cut = Render<FilterPanel>(parameters => parameters
            .Add(p => p.StatusFilter, DataStatusFilter.Complete)
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create<FilterValues>(this, _ => { }))
            .Add(p => p.OnReset, EventCallback.Factory.Create(this, () => resetCalled = true)));

        // Act - Find the Reset button by its aria-label or data attribute
        IReadOnlyList<IRenderedComponent<Button>> buttons = cut.FindComponents<Button>();
        IRenderedComponent<Button> resetButton = buttons.First(b => b.Instance.Text == "Reset");
        resetButton.Find("button").Click();

        // Assert
        resetCalled.ShouldBeTrue();
    }

    [Fact]
    public void FilterPanel_StatusFilterHasAllOptions()
    {
        // Arrange & Act
        IRenderedComponent<FilterPanel> cut = Render<FilterPanel>(parameters => parameters
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create<FilterValues>(this, _ => { }))
            .Add(p => p.OnReset, EventCallback.Factory.Create(this, () => { })));

        // Assert
        IElement select = cut.Find("select#status-filter");
        IHtmlCollection<IElement> options = select.QuerySelectorAll("option");
        options.Length.ShouldBe(4); // All Statuses, Complete, Partial, WithGaps

        cut.Markup.ShouldContain("All Statuses");
        cut.Markup.ShouldContain("Complete");
        cut.Markup.ShouldContain("Partial");
        cut.Markup.ShouldContain("With Gaps");
    }

    [Fact]
    public void FilterPanel_CoverageInputsHaveCorrectRange()
    {
        // Arrange & Act
        IRenderedComponent<FilterPanel> cut = Render<FilterPanel>(parameters => parameters
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create<FilterValues>(this, _ => { }))
            .Add(p => p.OnReset, EventCallback.Factory.Create(this, () => { })));

        // Assert
        IElement minInput = cut.Find("input#min-coverage");
        minInput.GetAttribute("min").ShouldBe("0");
        minInput.GetAttribute("max").ShouldBe("100");
        minInput.GetAttribute("step").ShouldBe("1");

        IElement maxInput = cut.Find("input#max-coverage");
        maxInput.GetAttribute("min").ShouldBe("0");
        maxInput.GetAttribute("max").ShouldBe("100");
        maxInput.GetAttribute("step").ShouldBe("1");
    }

    [Fact]
    public void FilterPanel_PreservesFilterValuesOnRender()
    {
        // Arrange & Act
        IRenderedComponent<FilterPanel> cut = Render<FilterPanel>(parameters => parameters
            .Add(p => p.StatusFilter, DataStatusFilter.Partial)
            .Add(p => p.MinCoverage, 60m)
            .Add(p => p.MaxCoverage, 85m)
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create<FilterValues>(this, _ => { }))
            .Add(p => p.OnReset, EventCallback.Factory.Create(this, () => { })));

        // Assert
        IElement statusSelect = cut.Find("select#status-filter");
        statusSelect.GetAttribute("value").ShouldBe("Partial");

        IElement minInput = cut.Find("input#min-coverage");
        minInput.GetAttribute("value").ShouldBe("60");

        IElement maxInput = cut.Find("input#max-coverage");
        maxInput.GetAttribute("value").ShouldBe("85");
    }

    [Fact]
    public void FilterPanel_CollapseButtonHasCorrectAriaLabel()
    {
        // Arrange
        IRenderedComponent<FilterPanel> cut = Render<FilterPanel>(parameters => parameters
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create<FilterValues>(this, _ => { }))
            .Add(p => p.OnReset, EventCallback.Factory.Create(this, () => { })));

        // Assert - Initially expanded
        IElement collapseButton = cut.Find("button[aria-label*='filters']");
        collapseButton.GetAttribute("aria-label").ShouldBe("Collapse filters");

        // Act - Collapse
        collapseButton.Click();

        // Assert - Now collapsed
        IElement expandButton = cut.Find("button[aria-label*='filters']");
        expandButton.GetAttribute("aria-label").ShouldBe("Expand filters");
    }
}
