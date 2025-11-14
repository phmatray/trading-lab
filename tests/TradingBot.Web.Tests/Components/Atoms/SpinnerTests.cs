// <copyright file="SpinnerTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Bunit;
using TradingBot.Web.Components.Atoms;

namespace TradingBot.Web.Tests.Components.Atoms;

/// <summary>
/// Tests for the Spinner component.
/// </summary>
public class SpinnerTests
{
    [Fact]
    public void Spinner_Renders_WithDefaultSize()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbSpinner>();

        // Assert
        var svg = cut.Find("svg");
        svg.ClassName.ShouldNotBeNull();
        svg.ClassName.ShouldContain("w-6 h-6");
    }

    [Fact]
    public void Spinner_Renders_WithSmallSize()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbSpinner>(parameters => parameters
            .Add(p => p.Size, SpinnerSize.Small));

        // Assert
        var svg = cut.Find("svg");
        svg.ClassName.ShouldNotBeNull();
        svg.ClassName.ShouldContain("w-4 h-4");
    }

    [Fact]
    public void Spinner_Renders_WithLargeSize()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbSpinner>(parameters => parameters
            .Add(p => p.Size, SpinnerSize.Large));

        // Assert
        var svg = cut.Find("svg");
        svg.ClassName.ShouldNotBeNull();
        svg.ClassName.ShouldContain("w-8 h-8");
    }

    [Fact]
    public void Spinner_Renders_WithExtraLargeSize()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbSpinner>(parameters => parameters
            .Add(p => p.Size, SpinnerSize.ExtraLarge));

        // Assert
        var svg = cut.Find("svg");
        svg.ClassName.ShouldNotBeNull();
        svg.ClassName.ShouldContain("w-12 h-12");
    }

    [Fact]
    public void Spinner_Renders_WithPrimaryColor()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbSpinner>(parameters => parameters
            .Add(p => p.Color, SpinnerColor.Primary));

        // Assert
        var svg = cut.Find("svg");
        svg.ClassName.ShouldNotBeNull();
        svg.ClassName.ShouldContain("text-blue-600");
    }

    [Fact]
    public void Spinner_Renders_WithSuccessColor()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbSpinner>(parameters => parameters
            .Add(p => p.Color, SpinnerColor.Success));

        // Assert
        var svg = cut.Find("svg");
        svg.ClassName.ShouldNotBeNull();
        svg.ClassName.ShouldContain("text-green-600");
    }

    [Fact]
    public void Spinner_Renders_WithWarningColor()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbSpinner>(parameters => parameters
            .Add(p => p.Color, SpinnerColor.Warning));

        // Assert
        var svg = cut.Find("svg");
        svg.ClassName.ShouldNotBeNull();
        svg.ClassName.ShouldContain("text-yellow-600");
    }

    [Fact]
    public void Spinner_Renders_WithDangerColor()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbSpinner>(parameters => parameters
            .Add(p => p.Color, SpinnerColor.Danger));

        // Assert
        var svg = cut.Find("svg");
        svg.ClassName.ShouldNotBeNull();
        svg.ClassName.ShouldContain("text-red-600");
    }

    [Fact]
    public void Spinner_Renders_WithWhiteColor()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbSpinner>(parameters => parameters
            .Add(p => p.Color, SpinnerColor.White));

        // Assert
        var svg = cut.Find("svg");
        svg.ClassName.ShouldNotBeNull();
        svg.ClassName.ShouldContain("text-white");
    }

    [Fact]
    public void Spinner_AppliesAnimateSpinClass()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbSpinner>();

        // Assert
        var svg = cut.Find("svg");
        svg.ClassName.ShouldNotBeNull();
        svg.ClassName.ShouldContain("animate-spin");
    }

    [Fact]
    public void Spinner_Renders_WithLabel()
    {
        // Arrange
        using var ctx = new BunitContext();
        const string label = "Loading data...";

        // Act
        var cut = ctx.Render<TbSpinner>(parameters => parameters
            .Add(p => p.Label, label));

        // Assert
        cut.Markup.ShouldContain(label);
        var span = cut.Find("span");
        span.TextContent.ShouldBe(label);
    }

    [Fact]
    public void Spinner_Renders_WithoutLabel_WhenNotProvided()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbSpinner>();

        // Assert
        var spans = cut.FindAll("span");
        spans.Count.ShouldBe(0);
    }

    [Fact]
    public void Spinner_Renders_WithCustomClass()
    {
        // Arrange
        using var ctx = new BunitContext();
        const string customClass = "my-custom-class";

        // Act
        var cut = ctx.Render<TbSpinner>(parameters => parameters
            .Add(p => p.Class, customClass));

        // Assert
        var svg = cut.Find("svg");
        svg.ClassName.ShouldNotBeNull();
        svg.ClassName.ShouldContain(customClass);
    }

    [Fact]
    public void Spinner_HasCorrectAriaAttributes()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbSpinner>();

        // Assert
        var container = cut.Find("div");
        container.GetAttribute("role").ShouldBe("status");
        container.GetAttribute("aria-label").ShouldBe("Loading");
    }

    [Fact]
    public void Spinner_Label_HasCorrectColorClass()
    {
        // Arrange
        using var ctx = new BunitContext();
        const string label = "Loading...";

        // Act
        var cut = ctx.Render<TbSpinner>(parameters => parameters
            .Add(p => p.Label, label)
            .Add(p => p.Color, SpinnerColor.Success));

        // Assert
        var span = cut.Find("span");
        span.ClassName.ShouldNotBeNull();
        span.ClassName.ShouldContain("text-green-600");
    }

    [Fact]
    public void Spinner_CombinesMultipleProperties()
    {
        // Arrange
        using var ctx = new BunitContext();
        const string label = "Processing...";
        const string customClass = "extra-margin";

        // Act
        var cut = ctx.Render<TbSpinner>(parameters => parameters
            .Add(p => p.Size, SpinnerSize.Large)
            .Add(p => p.Color, SpinnerColor.Warning)
            .Add(p => p.Label, label)
            .Add(p => p.Class, customClass));

        // Assert
        var svg = cut.Find("svg");
        svg.ClassName.ShouldNotBeNull();
        svg.ClassName.ShouldContain("w-8 h-8");
        svg.ClassName.ShouldContain("text-yellow-600");
        svg.ClassName.ShouldContain("animate-spin");
        svg.ClassName.ShouldContain(customClass);

        var span = cut.Find("span");
        span.TextContent.ShouldBe(label);
        span.ClassName.ShouldNotBeNull();
        span.ClassName.ShouldContain("text-yellow-600");
    }
}
