using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Forms.Listbox;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Forms.Listbox;

/// <summary>
/// Tests for the Catalyst Listbox component.
/// </summary>
public class ListboxTests : BunitTestContext
{
    private static readonly List<string> TestOptions = ["Small", "Medium", "Large", "Extra Large"];

    [Fact]
    public void Listbox_WithoutParameters_RendersButton()
    {
        // Arrange & Act
        IRenderedComponent<Listbox<string>> cut = Render<Listbox<string>>(parameters => parameters
            .Add(p => p.Options, TestOptions));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        IElement button = cut.Find("button");
        button.ShouldNotBeNull();
        button.GetAttribute("type").ShouldBe("button");
    }

    [Fact]
    public void Listbox_WithPlaceholder_DisplaysPlaceholder()
    {
        // Arrange & Act
        IRenderedComponent<Listbox<string>> cut = Render<Listbox<string>>(parameters => parameters
            .Add(p => p.Options, TestOptions)
            .Add(p => p.Placeholder, (RenderFragment)(builder => builder.AddContent(0, "Select size"))));

        // Assert
        cut.Markup.ShouldContain("Select size");
    }

    [Fact]
    public void Listbox_WithValue_DisplaysSelectedValue()
    {
        // Arrange & Act
        IRenderedComponent<Listbox<string>> cut = Render<Listbox<string>>(parameters => parameters
            .Add(p => p.Options, TestOptions)
            .Add(p => p.Value, "Medium"));

        // Assert
        cut.Markup.ShouldContain("Medium");
    }

    [Fact]
    public void Listbox_WithDisabled_DisablesButton()
    {
        // Arrange & Act
        IRenderedComponent<Listbox<string>> cut = Render<Listbox<string>>(parameters => parameters
            .Add(p => p.Options, TestOptions)
            .Add(p => p.Disabled, true));

        // Assert
        IElement button = cut.Find("button");
        button.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Listbox_RendersChevronIcon()
    {
        // Arrange & Act
        IRenderedComponent<Listbox<string>> cut = Render<Listbox<string>>(parameters => parameters
            .Add(p => p.Options, TestOptions));

        // Assert
        IElement svg = cut.Find("svg");
        svg.ShouldNotBeNull();
    }

    [Fact]
    public void Listbox_WhenClicked_TogglesDropdown()
    {
        // Arrange
        IRenderedComponent<Listbox<string>> cut = Render<Listbox<string>>(parameters => parameters
            .Add(p => p.Options, TestOptions));

        IElement button = cut.Find("button");

        // Act - Click to open
        button.Click();
        cut.Render();

        // Assert - Dropdown should be visible
        cut.Markup.ShouldContain("Small");
        cut.Markup.ShouldContain("Medium");
    }

    [Fact]
    public void Listbox_WithAriaLabel_AppliesLabel()
    {
        // Arrange & Act
        IRenderedComponent<Listbox<string>> cut = Render<Listbox<string>>(parameters => parameters
            .Add(p => p.Options, TestOptions)
            .Add(p => p.AriaLabel, "Select size"));

        // Assert
        IElement button = cut.Find("button");
        button.GetAttribute("aria-label").ShouldBe("Select size");
    }

    [Fact]
    public void Listbox_WithCustomDisplayValue_UsesCustomFunction()
    {
        // Arrange
        List<Size> sizes = [
            new Size { Name = "Small", Code = "S" },
            new Size { Name = "Medium", Code = "M" }
        ];

        // Act
        IRenderedComponent<Listbox<Size>> cut = Render<Listbox<Size>>(parameters => parameters
            .Add(p => p.Options, sizes)
            .Add(p => p.Value, sizes[1])
            .Add(p => p.DisplayValue, s => s.Name));

        // Assert
        cut.Markup.ShouldContain("Medium");
    }

    private class Size
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
