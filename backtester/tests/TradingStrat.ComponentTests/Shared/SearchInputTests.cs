using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the SearchInput component with debouncing.
/// </summary>
public class SearchInputTests : BunitTestContext
{
    [Fact]
    public void SearchInput_RendersWithPlaceholder()
    {
        // Arrange & Act
        IRenderedComponent<SearchInput> cut = Render<SearchInput>(parameters => parameters
            .Add(p => p.Placeholder, "Search by ticker..."));

        // Assert
        IElement input = cut.Find("input[type='text']");
        input.GetAttribute("placeholder").ShouldBe("Search by ticker...");
    }

    [Fact]
    public void SearchInput_RendersWithInitialValue()
    {
        // Arrange & Act
        IRenderedComponent<SearchInput> cut = Render<SearchInput>(parameters => parameters
            .Add(p => p.Value, "AAPL")
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, _ => { })));

        // Assert
        IElement input = cut.Find("input[type='text']");
        input.GetAttribute("value").ShouldBe("AAPL");
    }

    [Fact]
    public void SearchInput_HasSearchIcon()
    {
        // Arrange & Act
        IRenderedComponent<SearchInput> cut = Render<SearchInput>(parameters => parameters
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, _ => { })));

        // Assert - Check for search icon SVG element with search path
        IElement icon = cut.Find("svg path");
        icon.ShouldNotBeNull();
        icon.GetAttribute("d").ShouldNotBeNull(); // Has the path data
        cut.Markup.ShouldContain("m21 21-5.197-5.197"); // Part of magnifying glass icon path
    }

    [Fact]
    public void SearchInput_ShowsClearButtonWhenHasValue()
    {
        // Arrange & Act
        IRenderedComponent<SearchInput> cut = Render<SearchInput>(parameters => parameters
            .Add(p => p.Value, "AAPL")
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, _ => { })));

        // Assert
        IElement clearButton = cut.Find("button");
        clearButton.ShouldNotBeNull();
        clearButton.ClassList.ShouldContain("absolute");
    }

    [Fact]
    public void SearchInput_HidesClearButtonWhenEmpty()
    {
        // Arrange & Act
        IRenderedComponent<SearchInput> cut = Render<SearchInput>(parameters => parameters
            .Add(p => p.Value, "")
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, _ => { })));

        // Assert
        IReadOnlyList<IElement> buttons = cut.FindAll("button");
        buttons.ShouldBeEmpty();
    }

    [Fact]
    public void SearchInput_ClearButtonClearsInput()
    {
        // Arrange
        string? capturedValue = null;
        IRenderedComponent<SearchInput> cut = Render<SearchInput>(parameters => parameters
            .Add(p => p.Value, "AAPL")
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, value => capturedValue = value)));

        // Act
        IElement clearButton = cut.Find("button");
        clearButton.Click();

        // Assert
        capturedValue.ShouldBeNull();
    }

    [Fact]
    public void SearchInput_InputChangeTriggersdebounce()
    {
        // Arrange
        string? capturedValue = null;
        IRenderedComponent<SearchInput> cut = Render<SearchInput>(parameters => parameters
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, value => capturedValue = value))
            .Add(p => p.DebounceMs, 100));

        // Act - Type into the input (using Input for @oninput event)
        IElement input = cut.Find("input[type='text']");
        input.Input("MSFT");

        // Assert - Value should not be captured immediately (debounced)
        capturedValue.ShouldBeNull();
    }

    [Fact]
    public void SearchInput_HasCorrectAriaLabel()
    {
        // Arrange & Act
        IRenderedComponent<SearchInput> cut = Render<SearchInput>(parameters => parameters
            .Add(p => p.Placeholder, "Search tickers")
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, _ => { })));

        // Assert
        IElement input = cut.Find("input[type='text']");
        input.GetAttribute("aria-label").ShouldBe("Search tickers");
    }

    [Fact]
    public void SearchInput_CustomAriaLabelOverridesPlaceholder()
    {
        // Arrange & Act
        IRenderedComponent<SearchInput> cut = Render<SearchInput>(parameters => parameters
            .Add(p => p.Placeholder, "Search...")
            .Add(p => p.AriaLabel, "Custom search label")
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, _ => { })));

        // Assert
        IElement input = cut.Find("input[type='text']");
        input.GetAttribute("aria-label").ShouldBe("Custom search label");
    }

    [Fact]
    public void SearchInput_HasCorrectStyling()
    {
        // Arrange & Act
        IRenderedComponent<SearchInput> cut = Render<SearchInput>(parameters => parameters
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, _ => { })));

        // Assert
        IElement container = cut.Find(".relative");
        container.ShouldNotBeNull();

        IElement input = cut.Find("input[type='text']");
        input.ClassList.ShouldContain("w-full");
        input.ClassList.ShouldContain("pl-10"); // Space for search icon
        input.ClassList.ShouldContain("pr-10"); // Space for clear button
    }

    [Fact]
    public void SearchInput_CustomDebounceDelay()
    {
        // Arrange & Act
        IRenderedComponent<SearchInput> cut = Render<SearchInput>(parameters => parameters
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, _ => { }))
            .Add(p => p.DebounceMs, 500));

        // Assert - Component should render without errors
        cut.Find("input[type='text']").ShouldNotBeNull();
    }
}
