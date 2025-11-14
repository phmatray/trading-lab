// <copyright file="BadgeTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Bunit;
using Shouldly;
using TradingBot.Web.Components.Atoms;
using TradingBot.Web.Models;
using Xunit;

namespace TradingBot.Web.Tests.Components.Atoms;

/// <summary>
/// Tests for the Badge component.
/// </summary>
public class BadgeTests : Bunit.TestContext
{
    [Fact]
    public void Badge_Renders_WithDefaultVariant()
    {
        // Arrange & Act
        var cut = RenderComponent<Badge>(parameters => parameters
            .AddChildContent("Test Badge"));

        // Assert
        var span = cut.Find("span");
        span.ClassName.ShouldNotBeNull();
        span.ClassName.ShouldContain("bg-gray-100");
        span.ClassName.ShouldContain("text-gray-800");
    }

    [Fact]
    public void Badge_Renders_WithSuccessVariant()
    {
        // Arrange & Act
        var cut = RenderComponent<Badge>(parameters => parameters
            .Add(p => p.Variant, BadgeVariant.Success)
            .AddChildContent("Success"));

        // Assert
        var span = cut.Find("span");
        span.ClassName.ShouldNotBeNull();
        span.ClassName.ShouldContain("bg-green-100");
        span.ClassName.ShouldContain("text-green-800");
    }

    [Fact]
    public void Badge_Renders_WithErrorVariant()
    {
        // Arrange & Act
        var cut = RenderComponent<Badge>(parameters => parameters
            .Add(p => p.Variant, BadgeVariant.Error)
            .AddChildContent("Error"));

        // Assert
        var span = cut.Find("span");
        span.ClassName.ShouldNotBeNull();
        span.ClassName.ShouldContain("bg-red-100");
        span.ClassName.ShouldContain("text-red-800");
    }

    [Fact]
    public void Badge_Renders_WithWarningVariant()
    {
        // Arrange & Act
        var cut = RenderComponent<Badge>(parameters => parameters
            .Add(p => p.Variant, BadgeVariant.Warning)
            .AddChildContent("Warning"));

        // Assert
        var span = cut.Find("span");
        span.ClassName.ShouldNotBeNull();
        span.ClassName.ShouldContain("bg-yellow-100");
        span.ClassName.ShouldContain("text-yellow-800");
    }

    [Fact]
    public void Badge_Renders_WithInfoVariant()
    {
        // Arrange & Act
        var cut = RenderComponent<Badge>(parameters => parameters
            .Add(p => p.Variant, BadgeVariant.Info)
            .AddChildContent("Info"));

        // Assert
        var span = cut.Find("span");
        span.ClassName.ShouldNotBeNull();
        span.ClassName.ShouldContain("bg-blue-100");
        span.ClassName.ShouldContain("text-blue-800");
    }

    [Fact]
    public void Badge_Renders_WithPrimaryVariant()
    {
        // Arrange & Act
        var cut = RenderComponent<Badge>(parameters => parameters
            .Add(p => p.Variant, BadgeVariant.Primary)
            .AddChildContent("Primary"));

        // Assert
        var span = cut.Find("span");
        span.ClassName.ShouldNotBeNull();
        span.ClassName.ShouldContain("bg-blue-600");
        span.ClassName.ShouldContain("text-white");
    }

    [Fact]
    public void Badge_Renders_WithSmallSize()
    {
        // Arrange & Act
        var cut = RenderComponent<Badge>(parameters => parameters
            .Add(p => p.Size, BadgeSize.Small)
            .AddChildContent("Small"));

        // Assert
        var span = cut.Find("span");
        span.ClassName.ShouldNotBeNull();
        span.ClassName.ShouldContain("px-2");
        span.ClassName.ShouldContain("text-xs");
    }

    [Fact]
    public void Badge_Renders_WithMediumSize()
    {
        // Arrange & Act
        var cut = RenderComponent<Badge>(parameters => parameters
            .Add(p => p.Size, BadgeSize.Medium)
            .AddChildContent("Medium"));

        // Assert
        var span = cut.Find("span");
        span.ClassName.ShouldNotBeNull();
        span.ClassName.ShouldContain("px-2.5");
        span.ClassName.ShouldContain("text-sm");
    }

    [Fact]
    public void Badge_Renders_WithLargeSize()
    {
        // Arrange & Act
        var cut = RenderComponent<Badge>(parameters => parameters
            .Add(p => p.Size, BadgeSize.Large)
            .AddChildContent("Large"));

        // Assert
        var span = cut.Find("span");
        span.ClassName.ShouldNotBeNull();
        span.ClassName.ShouldContain("px-3");
        span.ClassName.ShouldContain("text-base");
    }

    [Fact]
    public void Badge_Renders_WithRoundedCorners_ByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<Badge>(parameters => parameters
            .AddChildContent("Test"));

        // Assert
        var span = cut.Find("span");
        span.ClassName.ShouldNotBeNull();
        span.ClassName.ShouldContain("rounded");
        span.ClassName.ShouldNotContain("rounded-full");
    }

    [Fact]
    public void Badge_Renders_WithPillStyle()
    {
        // Arrange & Act
        var cut = RenderComponent<Badge>(parameters => parameters
            .Add(p => p.Pill, true)
            .AddChildContent("Pill Badge"));

        // Assert
        var span = cut.Find("span");
        span.ClassName.ShouldNotBeNull();
        span.ClassName.ShouldContain("rounded-full");
    }

    [Fact]
    public void Badge_Renders_WithChildContent()
    {
        // Arrange
        const string content = "Test Badge Content";

        // Act
        var cut = RenderComponent<Badge>(parameters => parameters
            .AddChildContent(content));

        // Assert
        var span = cut.Find("span");
        span.TextContent.ShouldBe(content);
    }

    [Fact]
    public void Badge_Renders_WithCustomClass()
    {
        // Arrange
        const string customClass = "my-custom-class";

        // Act
        var cut = RenderComponent<Badge>(parameters => parameters
            .Add(p => p.Class, customClass)
            .AddChildContent("Test"));

        // Assert
        var span = cut.Find("span");
        span.ClassName.ShouldNotBeNull();
        span.ClassName.ShouldContain(customClass);
    }

    [Fact]
    public void Badge_HasBaseClasses()
    {
        // Arrange & Act
        var cut = RenderComponent<Badge>(parameters => parameters
            .AddChildContent("Test"));

        // Assert
        var span = cut.Find("span");
        span.ClassName.ShouldNotBeNull();
        span.ClassName.ShouldContain("inline-flex");
        span.ClassName.ShouldContain("items-center");
        span.ClassName.ShouldContain("font-medium");
    }

    [Fact]
    public void Badge_CombinesMultipleProperties()
    {
        // Arrange
        const string content = "Success Badge";
        const string customClass = "extra-margin";

        // Act
        var cut = RenderComponent<Badge>(parameters => parameters
            .Add(p => p.Variant, BadgeVariant.Success)
            .Add(p => p.Size, BadgeSize.Large)
            .Add(p => p.Pill, true)
            .Add(p => p.Class, customClass)
            .AddChildContent(content));

        // Assert
        var span = cut.Find("span");
        span.TextContent.ShouldBe(content);
        span.ClassName.ShouldNotBeNull();
        span.ClassName.ShouldContain("bg-green-100");
        span.ClassName.ShouldContain("text-green-800");
        span.ClassName.ShouldContain("px-3");
        span.ClassName.ShouldContain("text-base");
        span.ClassName.ShouldContain("rounded-full");
        span.ClassName.ShouldContain(customClass);
    }

    [Fact]
    public void Badge_AllVariants_ApplyCorrectColors()
    {
        // Test all variants apply their respective colors
        var variants = new[]
        {
            (BadgeVariant.Default, "bg-gray-100", "text-gray-800"),
            (BadgeVariant.Success, "bg-green-100", "text-green-800"),
            (BadgeVariant.Error, "bg-red-100", "text-red-800"),
            (BadgeVariant.Warning, "bg-yellow-100", "text-yellow-800"),
            (BadgeVariant.Info, "bg-blue-100", "text-blue-800"),
            (BadgeVariant.Primary, "bg-blue-600", "text-white"),
        };

        foreach (var (variant, expectedBg, expectedText) in variants)
        {
            // Act
            var cut = RenderComponent<Badge>(parameters => parameters
                .Add(p => p.Variant, variant)
                .AddChildContent(variant.ToString()));

            // Assert
            var span = cut.Find("span");
            span.ClassName.ShouldNotBeNull();
            span.ClassName.ShouldContain(expectedBg);
            span.ClassName.ShouldContain(expectedText);
        }
    }
}
