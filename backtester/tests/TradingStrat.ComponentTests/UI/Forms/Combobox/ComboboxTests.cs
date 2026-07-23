using AngleSharp.Dom;
using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Forms.Combobox;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Forms.Combobox;

/// <summary>
/// Tests for the Catalyst Combobox component.
/// </summary>
public class ComboboxTests : BunitTestContext
{
    private static readonly List<string> TestOptions = ["Apple", "Banana", "Cherry", "Date"];

    [Fact]
    public void Combobox_WithoutParameters_RendersInputElement()
    {
        // Arrange & Act
        IRenderedComponent<Combobox<string>> cut = Render<Combobox<string>>(parameters => parameters
            .Add(p => p.Options, TestOptions));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        IElement input = cut.Find("input");
        input.ShouldNotBeNull();
        input.GetAttribute("type").ShouldBe("text");
    }

    [Fact]
    public void Combobox_WithPlaceholder_AppliesPlaceholder()
    {
        // Arrange & Act
        IRenderedComponent<Combobox<string>> cut = Render<Combobox<string>>(parameters => parameters
            .Add(p => p.Options, TestOptions)
            .Add(p => p.Placeholder, "Select a fruit"));

        // Assert
        IElement input = cut.Find("input");
        input.GetAttribute("placeholder").ShouldBe("Select a fruit");
    }

    [Fact]
    public void Combobox_WithDisabled_DisablesInput()
    {
        // Arrange & Act
        IRenderedComponent<Combobox<string>> cut = Render<Combobox<string>>(parameters => parameters
            .Add(p => p.Options, TestOptions)
            .Add(p => p.Disabled, true));

        // Assert
        IElement input = cut.Find("input");
        input.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Combobox_WithValue_DisplaysSelectedValue()
    {
        // Arrange & Act
        IRenderedComponent<Combobox<string>> cut = Render<Combobox<string>>(parameters => parameters
            .Add(p => p.Options, TestOptions)
            .Add(p => p.Value, "Apple"));

        // Assert
        IElement input = cut.Find("input");
        input.GetAttribute("value").ShouldBe("Apple");
    }

    [Fact]
    public void Combobox_WhenTyping_FiltersOptions()
    {
        // Arrange
        IRenderedComponent<Combobox<string>> cut = Render<Combobox<string>>(parameters => parameters
            .Add(p => p.Options, TestOptions));

        IElement input = cut.Find("input");

        // Act - Type to filter
        input.Input("Ban");
        cut.Render();

        // Assert - Should show dropdown with filtered results
        cut.Markup.ShouldContain("Banana");
        cut.Markup.ShouldNotContain("Apple");
    }

    [Fact]
    public void Combobox_RendersChevronIcon()
    {
        // Arrange & Act
        IRenderedComponent<Combobox<string>> cut = Render<Combobox<string>>(parameters => parameters
            .Add(p => p.Options, TestOptions));

        // Assert
        IElement svg = cut.Find("svg");
        svg.ShouldNotBeNull();
    }

    [Fact]
    public void Combobox_WithCustomDisplayValue_UsesCustomFunction()
    {
        // Arrange
        List<Person> people = [
            new Person { Name = "John", Age = 30 },
            new Person { Name = "Jane", Age = 25 }
        ];

        // Act
        IRenderedComponent<Combobox<Person>> cut = Render<Combobox<Person>>(parameters => parameters
            .Add(p => p.Options, people)
            .Add(p => p.Value, people[0])
            .Add(p => p.DisplayValue, p => p.Name));

        // Assert
        IElement input = cut.Find("input");
        input.GetAttribute("value").ShouldBe("John");
    }

    [Fact]
    public void Combobox_WithAriaLabel_AppliesLabel()
    {
        // Arrange & Act
        IRenderedComponent<Combobox<string>> cut = Render<Combobox<string>>(parameters => parameters
            .Add(p => p.Options, TestOptions)
            .Add(p => p.AriaLabel, "Select fruit"));

        // Assert
        IElement input = cut.Find("input");
        input.GetAttribute("aria-label").ShouldBe("Select fruit");
    }

    private class Person
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}
