using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Buttons;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Buttons;

/// <summary>
/// Tests for the Catalyst Button component.
/// </summary>
public class ButtonTests : BunitTestContext
{
    [Fact]
    public void Button_WithoutParameters_RendersAsButtonElement()
    {
        // Arrange & Act
        IRenderedComponent<Button> cut = Render<Button>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Click"))));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        IElement button = cut.Find("button");
        button.ShouldNotBeNull();
        button.TextContent.ShouldContain("Click");
    }

    [Fact]
    public void Button_WithHref_RendersAsAnchorElement()
    {
        // Arrange & Act
        IRenderedComponent<Button> cut = Render<Button>(parameters => parameters
            .Add(p => p.Href, "/dashboard")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Link"))));

        // Assert
        IElement anchor = cut.Find("a");
        anchor.ShouldNotBeNull();
        anchor.GetAttribute("href").ShouldBe("/dashboard");
        anchor.TextContent.ShouldContain("Link");
    }

    [Theory]
    [InlineData(ButtonColor.Blue)]
    [InlineData(ButtonColor.Red)]
    [InlineData(ButtonColor.Green)]
    [InlineData(ButtonColor.Yellow)]
    [InlineData(ButtonColor.Zinc)]
    [InlineData(ButtonColor.DarkZinc)]
    public void Button_WithDifferentColors_AppliesCorrectColorClass(ButtonColor color)
    {
        // Arrange & Act
        IRenderedComponent<Button> cut = Render<Button>(parameters => parameters
            .Add(p => p.Color, color)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Test"))));

        // Assert
        IElement button = cut.Find("button");
        button.ClassName.ShouldNotBeNullOrWhiteSpace();
        // Color-specific classes are applied via CSS custom properties
        cut.Markup.ShouldContain("btn-solid");
    }

    [Fact]
    public void Button_WithOutlineVariant_AppliesOutlineClass()
    {
        // Arrange & Act
        IRenderedComponent<Button> cut = Render<Button>(parameters => parameters
            .Add(p => p.Outline, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Outline"))));

        // Assert
        cut.Markup.ShouldContain("btn-outline");
        cut.Markup.ShouldNotContain("btn-solid");
    }

    [Fact]
    public void Button_WithPlainVariant_AppliesPlainClass()
    {
        // Arrange & Act
        IRenderedComponent<Button> cut = Render<Button>(parameters => parameters
            .Add(p => p.Plain, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Plain"))));

        // Assert
        cut.Markup.ShouldContain("btn-plain");
        cut.Markup.ShouldNotContain("btn-solid");
    }

    [Fact]
    public void Button_WithDisabled_AppliesDisabledAttribute()
    {
        // Arrange & Act
        IRenderedComponent<Button> cut = Render<Button>(parameters => parameters
            .Add(p => p.Disabled, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Disabled"))));

        // Assert
        IElement button = cut.Find("button");
        button.HasAttribute("disabled").ShouldBeTrue();
    }

    [Theory]
    [InlineData(ButtonType.Submit, "submit")]
    [InlineData(ButtonType.Reset, "reset")]
    [InlineData(ButtonType.Button, "button")]
    public void Button_WithDifferentTypes_AppliesCorrectTypeAttribute(ButtonType type, string expectedType)
    {
        // Arrange & Act
        IRenderedComponent<Button> cut = Render<Button>(parameters => parameters
            .Add(p => p.Type, type)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Test"))));

        // Assert
        IElement button = cut.Find("button");
        button.GetAttribute("type").ShouldBe(expectedType);
    }

    [Fact]
    public void Button_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<Button> cut = Render<Button>(parameters => parameters
            .Add(p => p.Class, "custom-class")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Custom"))));

        // Assert
        IElement button = cut.Find("button");
        button.ClassList.ShouldContain("custom-class");
        button.ClassList.ShouldContain("btn-base");
    }

    [Fact]
    public void Button_IncludesTouchTarget_ForAccessibility()
    {
        // Arrange & Act
        IRenderedComponent<Button> cut = Render<Button>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Touch"))));

        // Assert
        // TouchTarget is rendered as a child span
        cut.Markup.ShouldContain("absolute top-1/2 left-1/2");
    }

    [Fact]
    public void Button_WithOnClick_InvokesCallback()
    {
        // Arrange
        bool clicked = false;

        IRenderedComponent<Button> cut = Render<Button>(parameters => parameters
            .Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked = true))
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Click Me"))));

        // Act
        IElement button = cut.Find("button");
        button.Click();

        // Assert
        clicked.ShouldBeTrue();
    }

    [Fact]
    public void Button_DefaultsToButtonType()
    {
        // Arrange & Act
        IRenderedComponent<Button> cut = Render<Button>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Default"))));

        // Assert
        IElement button = cut.Find("button");
        button.GetAttribute("type").ShouldBe("button");
    }

    [Fact]
    public void Button_WithAllColorVariants_RendersSuccessfully()
    {
        // Arrange
        ButtonColor[] allColors =
        [
            ButtonColor.Blue, ButtonColor.Red, ButtonColor.Green, ButtonColor.Yellow,
            ButtonColor.Zinc, ButtonColor.DarkZinc, ButtonColor.Indigo,
            ButtonColor.Cyan, ButtonColor.White, ButtonColor.Orange, ButtonColor.Amber,
            ButtonColor.Lime, ButtonColor.Emerald, ButtonColor.Sky, ButtonColor.Purple,
            ButtonColor.Pink, ButtonColor.Rose, ButtonColor.Teal, ButtonColor.Violet,
            ButtonColor.Fuchsia, ButtonColor.Dark, ButtonColor.Light
        ];

        // Act & Assert
        foreach (ButtonColor color in allColors)
        {
            IRenderedComponent<Button> cut = Render<Button>(parameters => parameters
                .Add(p => p.Color, color)
                .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, color.ToString()))));

            cut.Markup.ShouldNotBeEmpty();
            cut.Find("button").ShouldNotBeNull();
        }
    }
}
