using AngleSharp.Dom;
using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;
using static TradingStrat.Web.Components.Shared.DateRangeInput;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the DateRangeInput component - date range picker with presets.
/// </summary>
public class DateRangeInputTests : BunitTestContext
{
    [Fact]
    public void DateRangeInput_InitialRender_DisplaysStartAndEndDateInputs()
    {
        // Arrange & Act
        IRenderedComponent<DateRangeInput> cut = Render<DateRangeInput>();

        // Assert
        cut.Markup.ShouldContain("Start Date");
        cut.Markup.ShouldContain("End Date");
        IElement startInput = cut.Find("input[type='date']");
        startInput.ShouldNotBeNull();
    }

    [Fact]
    public void DateRangeInput_WithShowPresetsTrue_DisplaysPresetButtons()
    {
        // Arrange & Act
        IRenderedComponent<DateRangeInput> cut = Render<DateRangeInput>(parameters => parameters
            .Add(p => p.ShowPresets, true));

        // Assert
        cut.Markup.ShouldContain("Last 7 days");
        cut.Markup.ShouldContain("Last 30 days");
        cut.Markup.ShouldContain("Last 3 months");
        cut.Markup.ShouldContain("Last 6 months");
        cut.Markup.ShouldContain("Last year");
        cut.Markup.ShouldContain("YTD");
    }

    [Fact]
    public void DateRangeInput_WithShowPresetsFalse_HidesPresetButtons()
    {
        // Arrange & Act
        IRenderedComponent<DateRangeInput> cut = Render<DateRangeInput>(parameters => parameters
            .Add(p => p.ShowPresets, false));

        // Assert
        cut.Markup.ShouldNotContain("Last 7 days");
        cut.Markup.ShouldNotContain("Last 30 days");
    }

    [Fact]
    public void DateRangeInput_WithStartDate_DisplaysValue()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 15);

        // Act
        IRenderedComponent<DateRangeInput> cut = Render<DateRangeInput>(parameters => parameters
            .Add(p => p.StartDate, startDate));

        // Assert
        IElement startInput = cut.Find("input[type='date']");
        startInput.GetAttribute("value").ShouldBe("2024-01-15");
    }

    [Fact]
    public void DateRangeInput_WithEndDate_DisplaysValue()
    {
        // Arrange
        var endDate = new DateTime(2024, 12, 31);

        // Act
        IRenderedComponent<DateRangeInput> cut = Render<DateRangeInput>(parameters => parameters
            .Add(p => p.EndDate, endDate));

        // Assert
        IReadOnlyList<IElement> inputs = cut.FindAll("input[type='date']");
        inputs[1].GetAttribute("value").ShouldBe("2024-12-31");
    }

    [Fact]
    public void DateRangeInput_WithNullDates_DisplaysEmptyInputs()
    {
        // Arrange & Act
        IRenderedComponent<DateRangeInput> cut = Render<DateRangeInput>();

        // Assert
        IReadOnlyList<IElement> inputs = cut.FindAll("input[type='date']");
        inputs.Count.ShouldBe(2);
        // Null dates should render as empty strings
        inputs[0].GetAttribute("value")?.ShouldBeNullOrEmpty();
        inputs[1].GetAttribute("value")?.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void DateRangeInput_WithCustomPresets_DisplaysCustomPresets()
    {
        // Arrange
        var customPresets = new List<DateRangePreset>
        {
            new() { Label = "Custom 1", StartDate = DateTime.Today.AddDays(-10), EndDate = DateTime.Today },
            new() { Label = "Custom 2", StartDate = DateTime.Today.AddDays(-20), EndDate = DateTime.Today }
        };

        // Act
        IRenderedComponent<DateRangeInput> cut = Render<DateRangeInput>(parameters => parameters
            .Add(p => p.CustomPresets, customPresets));

        // Assert
        cut.Markup.ShouldContain("Custom 1");
        cut.Markup.ShouldContain("Custom 2");
        cut.Markup.ShouldNotContain("Last 7 days");
    }

    [Fact]
    public void DateRangeInput_HasCorrectTestId()
    {
        // Arrange & Act
        IRenderedComponent<DateRangeInput> cut = Render<DateRangeInput>();

        // Assert
        IElement component = cut.Find("[data-testid='date-range-input']");
        component.ShouldNotBeNull();
    }

    [Fact]
    public void DateRangeInput_StartDateInput_HasCorrectLabel()
    {
        // Arrange & Act
        IRenderedComponent<DateRangeInput> cut = Render<DateRangeInput>();

        // Assert
        IElement label = cut.FindAll("label")[0];
        label.TextContent.ShouldContain("Start Date");
    }

    [Fact]
    public void DateRangeInput_EndDateInput_HasCorrectLabel()
    {
        // Arrange & Act
        IRenderedComponent<DateRangeInput> cut = Render<DateRangeInput>();

        // Assert
        IElement label = cut.FindAll("label")[1];
        label.TextContent.ShouldContain("End Date");
    }

    [Fact]
    public void DateRangeInput_PresetButtons_HaveCorrectStyling()
    {
        // Arrange & Act
        IRenderedComponent<DateRangeInput> cut = Render<DateRangeInput>(parameters => parameters
            .Add(p => p.ShowPresets, true));

        // Assert
        IReadOnlyList<IElement> buttons = cut.FindAll("button[type='button']");
        buttons.Count.ShouldBeGreaterThan(0);
        buttons[0].ClassList.ShouldContain("px-3");
        buttons[0].ClassList.ShouldContain("py-1");
        buttons[0].ClassList.ShouldContain("text-sm");
    }

    [Fact]
    public void DateRangeInput_DateInputs_HaveCorrectStyling()
    {
        // Arrange & Act
        IRenderedComponent<DateRangeInput> cut = Render<DateRangeInput>();

        // Assert
        IReadOnlyList<IElement> inputs = cut.FindAll("input[type='date']");
        inputs[0].ClassList.ShouldContain("w-full");
        inputs[0].ClassList.ShouldContain("rounded-lg");
    }

    [Fact]
    public void DateRangeInput_WithBothDates_DisplaysBothValues()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

        // Act
        IRenderedComponent<DateRangeInput> cut = Render<DateRangeInput>(parameters => parameters
            .Add(p => p.StartDate, startDate)
            .Add(p => p.EndDate, endDate));

        // Assert
        IReadOnlyList<IElement> inputs = cut.FindAll("input[type='date']");
        inputs[0].GetAttribute("value").ShouldBe("2024-01-01");
        inputs[1].GetAttribute("value").ShouldBe("2024-12-31");
    }
}
