using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Layouts;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Layouts;

public class AuthLayoutTests : BunitTestContext
{
    [Fact]
    public void AuthLayout_WithChildContent_RendersCorrectly()
    {
        // Arrange & Act
        IRenderedComponent<AuthLayout> cut = Render<AuthLayout>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, "Sign In Form");
                builder.CloseElement();
            })));

        // Assert
        cut.Markup.ShouldContain("Sign In Form");
    }

    [Fact]
    public void AuthLayout_ShouldHaveCenteredLayout()
    {
        // Arrange & Act
        IRenderedComponent<AuthLayout> cut = Render<AuthLayout>();

        // Assert
        cut.Markup.ShouldContain("flex");
        cut.Markup.ShouldContain("items-center");
        cut.Markup.ShouldContain("justify-center");
    }

    [Fact]
    public void AuthLayout_ShouldHaveMinimumViewportHeight()
    {
        // Arrange & Act
        IRenderedComponent<AuthLayout> cut = Render<AuthLayout>();

        // Assert
        cut.Markup.ShouldContain("min-h-dvh");
    }

    [Fact]
    public void AuthLayout_ShouldHaveResponsiveStyling()
    {
        // Arrange & Act
        IRenderedComponent<AuthLayout> cut = Render<AuthLayout>();

        // Assert
        cut.Markup.ShouldContain("lg:rounded-lg");
        cut.Markup.ShouldContain("lg:bg-white");
        cut.Markup.ShouldContain("lg:shadow-xs");
        cut.Markup.ShouldContain("dark:lg:bg-zinc-900");
    }

    [Fact]
    public void AuthLayout_ShouldRenderMainElement()
    {
        // Arrange & Act
        IRenderedComponent<AuthLayout> cut = Render<AuthLayout>();

        // Assert
        cut.Markup.ShouldContain("<main");
        cut.Markup.ShouldContain("</main>");
    }
}
