using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Buttons;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Buttons;

/// <summary>
/// Tests for the TouchTarget accessibility component.
/// </summary>
public class TouchTargetTests : BunitTestContext
{
    [Fact]
    public void TouchTarget_RendersSuccessfully()
    {
        // Arrange & Act
        IRenderedComponent<TouchTarget> cut = Render<TouchTarget>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Content"))));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
    }

    [Fact]
    public void TouchTarget_RendersAccessibilitySpan()
    {
        // Arrange & Act
        IRenderedComponent<TouchTarget> cut = Render<TouchTarget>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Test"))));

        // Assert
        IElement span = cut.Find("span[aria-hidden='true']");
        span.ShouldNotBeNull();
        span.ClassList.ShouldContain("absolute");
    }

    [Fact]
    public void TouchTarget_RendersChildContent()
    {
        // Arrange & Act
        IRenderedComponent<TouchTarget> cut = Render<TouchTarget>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Child Content"))));

        // Assert
        cut.Markup.ShouldContain("Child Content");
    }

    [Fact]
    public void TouchTarget_HasCorrectAccessibilityAttributes()
    {
        // Arrange & Act
        IRenderedComponent<TouchTarget> cut = Render<TouchTarget>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Test"))));

        // Assert
        IElement span = cut.Find("span[aria-hidden='true']");
        span.GetAttribute("aria-hidden").ShouldBe("true");
    }
}
