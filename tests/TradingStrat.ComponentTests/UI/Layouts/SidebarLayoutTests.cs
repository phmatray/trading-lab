using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Layouts;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Layouts;

public class SidebarLayoutTests : BunitTestContext
{
    public SidebarLayoutTests()
    {
        // Setup JSInterop for Dialog component
        JSInterop.SetupVoid("catalyst.initializeDialog", _ => true);
        JSInterop.SetupVoid("catalyst.cleanupDialog", _ => true);
    }

    [Fact]
    public void SidebarLayout_WithAllSlots_RendersCorrectly()
    {
        // Arrange & Act
        IRenderedComponent<SidebarLayout> cut = Render<SidebarLayout>(parameters => parameters
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
        cut.Markup.ShouldContain("Test Sidebar");
        cut.Markup.ShouldContain("Test Content");
    }

    [Fact]
    public void SidebarLayout_ShouldHaveFixedSidebarOnDesktop()
    {
        // Arrange & Act
        IRenderedComponent<SidebarLayout> cut = Render<SidebarLayout>(parameters => parameters
            .Add(p => p.Sidebar, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "aside");
                builder.AddContent(1, "Sidebar");
                builder.CloseElement();
            })));

        // Assert
        cut.Markup.ShouldContain("fixed inset-y-0 left-0 w-64 max-lg:hidden");
    }

    [Fact]
    public void SidebarLayout_ShouldHaveNavbarOnMobile()
    {
        // Arrange & Act
        IRenderedComponent<SidebarLayout> cut = Render<SidebarLayout>(parameters => parameters
            .Add(p => p.Navbar, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "nav");
                builder.AddContent(1, "Navbar");
                builder.CloseElement();
            })));

        // Assert
        cut.Markup.ShouldContain("lg:hidden");
        cut.Markup.ShouldContain("Open navigation");
    }

    [Fact]
    public void SidebarLayout_ShouldHaveResponsiveBackgroundColors()
    {
        // Arrange & Act
        IRenderedComponent<SidebarLayout> cut = Render<SidebarLayout>();

        // Assert
        cut.Markup.ShouldContain("bg-white");
        cut.Markup.ShouldContain("lg:bg-zinc-100");
        cut.Markup.ShouldContain("dark:bg-zinc-900");
        cut.Markup.ShouldContain("dark:lg:bg-zinc-950");
    }
}
