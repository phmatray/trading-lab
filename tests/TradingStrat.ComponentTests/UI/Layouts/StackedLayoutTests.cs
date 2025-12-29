using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Layouts;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Layouts;

public class StackedLayoutTests : BunitTestContext
{
    public StackedLayoutTests()
    {
        // Setup JSInterop for Dialog component
        JSInterop.SetupVoid("catalyst.initializeDialog", _ => true);
        JSInterop.SetupVoid("catalyst.cleanupDialog", _ => true);
    }

    [Fact]
    public void StackedLayout_WithAllSlots_RendersCorrectly()
    {
        // Arrange & Act
        IRenderedComponent<StackedLayout> cut = Render<StackedLayout>(parameters => parameters
            .Add(p => p.Navbar, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "nav");
                builder.AddContent(1, "Test Navbar");
                builder.CloseElement();
            }))
            .Add(p => p.Sidebar, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "aside");
                builder.AddContent(1, "Test Sidebar");
                builder.CloseElement();
            }))
            .Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, "Test Content");
                builder.CloseElement();
            })));

        // Assert
        cut.Markup.ShouldContain("Test Navbar");
        cut.Markup.ShouldContain("Test Content");
        // Note: Sidebar is inside a closed dialog, so it won't be in the initial markup
    }

    [Fact]
    public void StackedLayout_ShouldHaveNavbarInHeader()
    {
        // Arrange & Act
        IRenderedComponent<StackedLayout> cut = Render<StackedLayout>(parameters => parameters
            .Add(p => p.Navbar, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "nav");
                builder.AddContent(1, "Navbar");
                builder.CloseElement();
            })));

        // Assert
        cut.Markup.ShouldContain("<header");
        cut.Markup.ShouldContain("Navbar");
    }

    [Fact]
    public void StackedLayout_ShouldHaveMobileMenuButton()
    {
        // Arrange & Act
        IRenderedComponent<StackedLayout> cut = Render<StackedLayout>();

        // Assert
        cut.Markup.ShouldContain("lg:hidden");
        cut.Markup.ShouldContain("Open navigation");
    }

    [Fact]
    public void StackedLayout_ShouldHaveFlexColumnLayout()
    {
        // Arrange & Act
        IRenderedComponent<StackedLayout> cut = Render<StackedLayout>();

        // Assert
        cut.Markup.ShouldContain("flex-col");
        cut.Markup.ShouldContain("min-h-svh");
    }

    [Fact]
    public void StackedLayout_ShouldHaveResponsiveBackgroundColors()
    {
        // Arrange & Act
        IRenderedComponent<StackedLayout> cut = Render<StackedLayout>();

        // Assert
        cut.Markup.ShouldContain("bg-white");
        cut.Markup.ShouldContain("lg:bg-zinc-100");
        cut.Markup.ShouldContain("dark:bg-zinc-900");
        cut.Markup.ShouldContain("dark:lg:bg-zinc-950");
    }
}
