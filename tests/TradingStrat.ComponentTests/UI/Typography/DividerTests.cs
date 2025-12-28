using AngleSharp.Dom;
using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Typography;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Typography;

/// <summary>
/// Tests for the Divider component.
/// </summary>
public class DividerTests : BunitTestContext
{
    [Fact]
    public void Divider_RendersAsHrElement()
    {
        // Arrange & Act
        IRenderedComponent<Divider> cut = Render<Divider>();

        // Assert
        IElement hr = cut.Find("hr");
        hr.ShouldNotBeNull();
    }

    [Fact]
    public void Divider_AppliesVerticalMargin()
    {
        // Arrange & Act
        IRenderedComponent<Divider> cut = Render<Divider>();

        // Assert
        cut.Markup.ShouldContain("my-8");
    }

    [Fact]
    public void Divider_AppliesTopBorder()
    {
        // Arrange & Act
        IRenderedComponent<Divider> cut = Render<Divider>();

        // Assert
        cut.Markup.ShouldContain("border-t");
    }

    [Fact]
    public void Divider_AppliesDefaultBorderColor()
    {
        // Arrange & Act
        IRenderedComponent<Divider> cut = Render<Divider>();

        // Assert
        cut.Markup.ShouldContain("border-zinc-950/10");
    }

    [Fact]
    public void Divider_AppliesDarkModeBorderColor()
    {
        // Arrange & Act
        IRenderedComponent<Divider> cut = Render<Divider>();

        // Assert
        cut.Markup.ShouldContain("dark:border-white/10");
    }

    [Fact]
    public void Divider_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<Divider> cut = Render<Divider>(parameters => parameters
            .Add(p => p.Class, "custom-divider"));

        // Assert
        IElement hr = cut.Find("hr");
        hr.ClassList.ShouldContain("custom-divider");
        hr.ClassList.ShouldContain("my-8");
    }
}
