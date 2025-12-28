using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Forms.Switch;

/// <summary>
/// Tests for the Switch component.
/// </summary>
public class SwitchTests : BunitTestContext
{
    [Fact]
    public void Switch_RendersAsSpan()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Switch.Switch> cut = Render<Web.Components.UI.Forms.Switch.Switch>();

        // Assert
        IElement span = cut.Find("span.switch-base");
        span.ShouldNotBeNull();
        span.GetAttribute("data-slot").ShouldBe("control");
    }

    [Fact]
    public void Switch_RendersHiddenInput()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Switch.Switch> cut = Render<Web.Components.UI.Forms.Switch.Switch>();

        // Assert
        IElement input = cut.Find("input[type='checkbox']");
        input.ShouldNotBeNull();
        input.ClassList.ShouldContain("sr-only");
    }

    [Fact]
    public void Switch_WithCheckedTrue_AppliesCheckedAttribute()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Switch.Switch> cut = Render<Web.Components.UI.Forms.Switch.Switch>(parameters => parameters
            .Add(p => p.Checked, true));

        // Assert
        IElement input = cut.Find("input[type='checkbox']");
        input.HasAttribute("checked").ShouldBeTrue();
    }

    [Fact]
    public void Switch_WithDisabled_AppliesDisabledAttribute()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Switch.Switch> cut = Render<Web.Components.UI.Forms.Switch.Switch>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        IElement input = cut.Find("input[type='checkbox']");
        input.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Switch_AppliesBaseSwitchClass()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Switch.Switch> cut = Render<Web.Components.UI.Forms.Switch.Switch>();

        // Assert
        IElement span = cut.Find("span.switch-base");
        span.ShouldNotBeNull();
    }

    [Fact]
    public void Switch_RendersKnob()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Switch.Switch> cut = Render<Web.Components.UI.Forms.Switch.Switch>();

        // Assert
        IElement knob = cut.Find("span[aria-hidden='true']");
        knob.ShouldNotBeNull();
        knob.ClassList.ShouldContain("rounded-full");
    }

    [Fact]
    public void Switch_WithCheckedTrue_TranslatesKnob()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Switch.Switch> cut = Render<Web.Components.UI.Forms.Switch.Switch>(parameters => parameters
            .Add(p => p.Checked, true));

        // Assert
        IElement knob = cut.Find("span[aria-hidden='true']");
        knob.ClassList.ShouldContain("!translate-x-4");
    }

    [Theory]
    [InlineData(SwitchColor.DarkZinc)]
    [InlineData(SwitchColor.Blue)]
    [InlineData(SwitchColor.Red)]
    [InlineData(SwitchColor.Green)]
    public void Switch_WithDifferentColors_AppliesCorrectColorClass(SwitchColor color)
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Switch.Switch> cut = Render<Web.Components.UI.Forms.Switch.Switch>(parameters => parameters
            .Add(p => p.Color, color));

        // Assert
        cut.Markup.ShouldContain("switch-base");
    }

    [Fact]
    public void Switch_OnChange_TriggersCheckedChanged()
    {
        // Arrange
        bool? newValue = null;
        EventCallback<bool> callback = EventCallback.Factory.Create<bool>(
            this,
            (bool value) => newValue = value);

        IRenderedComponent<Web.Components.UI.Forms.Switch.Switch> cut = Render<Web.Components.UI.Forms.Switch.Switch>(parameters => parameters
            .Add(p => p.CheckedChanged, callback));

        // Act
        IElement input = cut.Find("input[type='checkbox']");
        input.Change(true);

        // Assert
        newValue.ShouldBe(true);
    }

    [Fact]
    public void Switch_WithDisabled_DoesNotTriggerCheckedChanged()
    {
        // Arrange
        bool? newValue = null;
        EventCallback<bool> callback = EventCallback.Factory.Create<bool>(
            this,
            (bool value) => newValue = value);

        IRenderedComponent<Web.Components.UI.Forms.Switch.Switch> cut = Render<Web.Components.UI.Forms.Switch.Switch>(parameters => parameters
            .Add(p => p.Disabled, true)
            .Add(p => p.CheckedChanged, callback));

        // Act
        IElement input = cut.Find("input[type='checkbox']");
        input.Change(true);

        // Assert
        newValue.ShouldBeNull();
    }

    [Fact]
    public void Switch_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Switch.Switch> cut = Render<Web.Components.UI.Forms.Switch.Switch>(parameters => parameters
            .Add(p => p.Class, "custom-switch"));

        // Assert
        cut.Markup.ShouldContain("custom-switch");
        cut.Markup.ShouldContain("switch-base");
    }
}
