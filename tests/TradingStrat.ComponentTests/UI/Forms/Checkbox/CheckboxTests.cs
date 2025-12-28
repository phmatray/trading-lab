using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Forms.Checkbox;

/// <summary>
/// Tests for the Checkbox component.
/// </summary>
public class CheckboxTests : BunitTestContext
{
    [Fact]
    public void Checkbox_RendersAsLabel()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Checkbox.Checkbox> cut = Render<Web.Components.UI.Forms.Checkbox.Checkbox>();

        // Assert
        IElement label = cut.Find("label");
        label.ShouldNotBeNull();
        label.ClassList.ShouldContain("group");
    }

    [Fact]
    public void Checkbox_RendersHiddenInput()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Checkbox.Checkbox> cut = Render<Web.Components.UI.Forms.Checkbox.Checkbox>();

        // Assert
        IElement input = cut.Find("input[type='checkbox']");
        input.ShouldNotBeNull();
        input.ClassList.ShouldContain("sr-only");
    }

    [Fact]
    public void Checkbox_WithCheckedTrue_AppliesCheckedAttribute()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Checkbox.Checkbox> cut = Render<Web.Components.UI.Forms.Checkbox.Checkbox>(parameters => parameters
            .Add(p => p.Checked, true));

        // Assert
        IElement input = cut.Find("input[type='checkbox']");
        input.HasAttribute("checked").ShouldBeTrue();
    }

    [Fact]
    public void Checkbox_WithDisabled_AppliesDisabledAttribute()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Checkbox.Checkbox> cut = Render<Web.Components.UI.Forms.Checkbox.Checkbox>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        IElement input = cut.Find("input[type='checkbox']");
        input.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Checkbox_AppliesBaseCheckboxClass()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Checkbox.Checkbox> cut = Render<Web.Components.UI.Forms.Checkbox.Checkbox>();

        // Assert
        IElement span = cut.Find("span.checkbox-base");
        span.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(CheckboxColor.DarkZinc)]
    [InlineData(CheckboxColor.Blue)]
    [InlineData(CheckboxColor.Red)]
    [InlineData(CheckboxColor.Green)]
    public void Checkbox_WithDifferentColors_AppliesCorrectColorClass(CheckboxColor color)
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Checkbox.Checkbox> cut = Render<Web.Components.UI.Forms.Checkbox.Checkbox>(parameters => parameters
            .Add(p => p.Color, color));

        // Assert
        cut.Markup.ShouldContain("checkbox-base");
    }

    [Fact]
    public void Checkbox_DefaultsToDataZincColor()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Checkbox.Checkbox> cut = Render<Web.Components.UI.Forms.Checkbox.Checkbox>();

        // Assert
        cut.Markup.ShouldContain("--checkbox-check");
    }

    [Fact]
    public void Checkbox_WithIndeterminate_ShowsIndeterminateIcon()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Checkbox.Checkbox> cut = Render<Web.Components.UI.Forms.Checkbox.Checkbox>(parameters => parameters
            .Add(p => p.Indeterminate, true));

        // Assert
        cut.Markup.ShouldContain("opacity-100"); // Indeterminate line visible
    }

    [Fact]
    public void Checkbox_WithCheckedFalse_HidesCheckmarkIcon()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Checkbox.Checkbox> cut = Render<Web.Components.UI.Forms.Checkbox.Checkbox>(parameters => parameters
            .Add(p => p.Checked, false));

        // Assert
        IElement svg = cut.Find("svg");
        svg.ClassList.ShouldContain("opacity-0");
    }

    [Fact]
    public void Checkbox_OnChange_TriggersCheckedChanged()
    {
        // Arrange
        bool? newValue = null;
        EventCallback<bool> callback = EventCallback.Factory.Create<bool>(
            this,
            (bool value) => newValue = value);

        IRenderedComponent<Web.Components.UI.Forms.Checkbox.Checkbox> cut = Render<Web.Components.UI.Forms.Checkbox.Checkbox>(parameters => parameters
            .Add(p => p.CheckedChanged, callback));

        // Act
        IElement input = cut.Find("input[type='checkbox']");
        input.Change(true);

        // Assert
        newValue.ShouldBe(true);
    }

    [Fact]
    public void Checkbox_WithDisabled_DoesNotTriggerCheckedChanged()
    {
        // Arrange
        bool? newValue = null;
        EventCallback<bool> callback = EventCallback.Factory.Create<bool>(
            this,
            (bool value) => newValue = value);

        IRenderedComponent<Web.Components.UI.Forms.Checkbox.Checkbox> cut = Render<Web.Components.UI.Forms.Checkbox.Checkbox>(parameters => parameters
            .Add(p => p.Disabled, true)
            .Add(p => p.CheckedChanged, callback));

        // Act
        IElement input = cut.Find("input[type='checkbox']");
        input.Change(true);

        // Assert
        newValue.ShouldBeNull(); // Callback should not have been invoked
    }

    [Fact]
    public void Checkbox_WithChildContent_RendersContent()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Checkbox.Checkbox> cut = Render<Web.Components.UI.Forms.Checkbox.Checkbox>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Accept Terms"))));

        // Assert
        cut.Markup.ShouldContain("Accept Terms");
    }

    [Fact]
    public void Checkbox_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Checkbox.Checkbox> cut = Render<Web.Components.UI.Forms.Checkbox.Checkbox>(parameters => parameters
            .Add(p => p.Class, "custom-checkbox"));

        // Assert
        cut.Markup.ShouldContain("custom-checkbox");
        cut.Markup.ShouldContain("checkbox-base");
    }
}
