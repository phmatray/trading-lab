using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Forms.Radio;

/// <summary>
/// Tests for the Radio component.
/// </summary>
public class RadioTests : BunitTestContext
{
    [Fact]
    public void Radio_RendersAsLabel()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Radio.Radio> cut = Render<Web.Components.UI.Forms.Radio.Radio>();

        // Assert
        IElement label = cut.Find("label");
        label.ShouldNotBeNull();
        label.ClassList.ShouldContain("group");
    }

    [Fact]
    public void Radio_RendersHiddenInput()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Radio.Radio> cut = Render<Web.Components.UI.Forms.Radio.Radio>();

        // Assert
        IElement input = cut.Find("input[type='radio']");
        input.ShouldNotBeNull();
        input.ClassList.ShouldContain("sr-only");
    }

    [Fact]
    public void Radio_WithCheckedTrue_AppliesCheckedAttribute()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Radio.Radio> cut = Render<Web.Components.UI.Forms.Radio.Radio>(parameters => parameters
            .Add(p => p.Checked, true));

        // Assert
        IElement input = cut.Find("input[type='radio']");
        input.HasAttribute("checked").ShouldBeTrue();
    }

    [Fact]
    public void Radio_WithDisabled_AppliesDisabledAttribute()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Radio.Radio> cut = Render<Web.Components.UI.Forms.Radio.Radio>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        IElement input = cut.Find("input[type='radio']");
        input.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Radio_WithName_AppliesNameAttribute()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Radio.Radio> cut = Render<Web.Components.UI.Forms.Radio.Radio>(parameters => parameters
            .Add(p => p.Name, "radio-group"));

        // Assert
        IElement input = cut.Find("input[type='radio']");
        input.GetAttribute("name").ShouldBe("radio-group");
    }

    [Fact]
    public void Radio_WithValue_AppliesValueAttribute()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Radio.Radio> cut = Render<Web.Components.UI.Forms.Radio.Radio>(parameters => parameters
            .Add(p => p.Value, "option1"));

        // Assert
        IElement input = cut.Find("input[type='radio']");
        input.GetAttribute("value").ShouldBe("option1");
    }

    [Fact]
    public void Radio_AppliesBaseRadioClass()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Radio.Radio> cut = Render<Web.Components.UI.Forms.Radio.Radio>();

        // Assert
        IElement span = cut.Find("span.radio-base");
        span.ShouldNotBeNull();
    }

    [Fact]
    public void Radio_RendersIndicatorDot()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Radio.Radio> cut = Render<Web.Components.UI.Forms.Radio.Radio>();

        // Assert
        IElement indicator = cut.Find("span.rounded-full");
        indicator.ShouldNotBeNull();
        indicator.ClassList.ShouldContain("border-[4.5px]");
    }

    [Theory]
    [InlineData(RadioColor.DarkZinc)]
    [InlineData(RadioColor.Blue)]
    [InlineData(RadioColor.Red)]
    [InlineData(RadioColor.Green)]
    public void Radio_WithDifferentColors_AppliesCorrectColorClass(RadioColor color)
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Radio.Radio> cut = Render<Web.Components.UI.Forms.Radio.Radio>(parameters => parameters
            .Add(p => p.Color, color));

        // Assert
        cut.Markup.ShouldContain("radio-base");
    }

    [Fact]
    public void Radio_OnChange_TriggersCheckedChanged()
    {
        // Arrange
        bool? newValue = null;
        EventCallback<bool> callback = EventCallback.Factory.Create<bool>(
            this,
            (bool value) => newValue = value);

        IRenderedComponent<Web.Components.UI.Forms.Radio.Radio> cut = Render<Web.Components.UI.Forms.Radio.Radio>(parameters => parameters
            .Add(p => p.CheckedChanged, callback));

        // Act
        IElement input = cut.Find("input[type='radio']");
        input.Change(true);

        // Assert
        newValue.ShouldBe(true);
    }

    [Fact]
    public void Radio_WithDisabled_DoesNotTriggerCheckedChanged()
    {
        // Arrange
        bool? newValue = null;
        EventCallback<bool> callback = EventCallback.Factory.Create<bool>(
            this,
            (bool value) => newValue = value);

        IRenderedComponent<Web.Components.UI.Forms.Radio.Radio> cut = Render<Web.Components.UI.Forms.Radio.Radio>(parameters => parameters
            .Add(p => p.Disabled, true)
            .Add(p => p.CheckedChanged, callback));

        // Act
        IElement input = cut.Find("input[type='radio']");
        input.Change(true);

        // Assert
        newValue.ShouldBeNull();
    }

    [Fact]
    public void Radio_WithChildContent_RendersContent()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Radio.Radio> cut = Render<Web.Components.UI.Forms.Radio.Radio>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Option A"))));

        // Assert
        cut.Markup.ShouldContain("Option A");
    }

    [Fact]
    public void Radio_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Forms.Radio.Radio> cut = Render<Web.Components.UI.Forms.Radio.Radio>(parameters => parameters
            .Add(p => p.Class, "custom-radio"));

        // Assert
        cut.Markup.ShouldContain("custom-radio");
        cut.Markup.ShouldContain("radio-base");
    }
}
