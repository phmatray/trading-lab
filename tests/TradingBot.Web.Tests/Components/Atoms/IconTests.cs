// <copyright file="IconTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Bunit;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;
using TradingBot.Web.Components.Atoms;
using Xunit;

namespace TradingBot.Web.Tests.Components.Atoms;

/// <summary>
/// Tests for the Icon component.
/// </summary>
public class IconTests
{
    [Fact]
    public void Icon_RendersCorrectSvgElement()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, IconName.Home));

        // Assert
        var svg = cut.Find("svg");
        svg.ShouldNotBeNull();
    }

    [Fact]
    public void Icon_AppliesCustomCssClasses()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, IconName.Home)
            .Add(p => p.Class, "w-8 h-8 text-blue-500"));

        // Assert
        var svg = cut.Find("svg");
        svg.ClassList.ShouldContain("w-8");
        svg.ClassList.ShouldContain("h-8");
        svg.ClassList.ShouldContain("text-blue-500");
    }

    [Fact]
    public void Icon_RendersHomeIcon()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, IconName.Home));

        // Assert
        var svg = cut.Find("svg");
        var path = svg.QuerySelector("path");
        path.ShouldNotBeNull();
        path!.GetAttribute("d")?.ShouldContain("M2.25 12l8.954");
    }

    [Fact]
    public void Icon_RendersChartBarIcon()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, IconName.ChartBar));

        // Assert
        var svg = cut.Find("svg");
        var path = svg.QuerySelector("path");
        path.ShouldNotBeNull();
        path!.GetAttribute("d")?.ShouldContain("M3 13.125C3 12.504");
    }

    [Fact]
    public void Icon_RendersWithOutlineVariantByDefault()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, IconName.Home));

        // Assert
        var svg = cut.Find("svg");
        svg.GetAttribute("fill").ShouldBe("none");
    }

    [Fact]
    public void Icon_RendersWithSolidVariant()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, IconName.Home)
            .Add(p => p.Variant, IconVariant.Solid));

        // Assert
        var svg = cut.Find("svg");
        svg.GetAttribute("fill").ShouldBe("currentColor");
    }

    [Fact]
    public void Icon_WithoutAriaLabel_IsAriaHidden()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, IconName.Home));

        // Assert
        var svg = cut.Find("svg");
        svg.GetAttribute("aria-hidden").ShouldBe("true");
    }

    [Fact]
    public void Icon_WithAriaLabel_HasCorrectAriaAttributes()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, IconName.Home)
            .Add(p => p.AriaLabel, "Home page"));

        // Assert
        var svg = cut.Find("svg");
        svg.GetAttribute("aria-label").ShouldBe("Home page");
        svg.GetAttribute("role").ShouldBe("img");
        svg.HasAttribute("aria-hidden").ShouldBeFalse();
    }

    [Theory]
    [InlineData(IconName.Home)]
    [InlineData(IconName.ChartBar)]
    [InlineData(IconName.Briefcase)]
    [InlineData(IconName.Cog)]
    [InlineData(IconName.Beaker)]
    [InlineData(IconName.ChartLine)]
    [InlineData(IconName.Bars3)]
    [InlineData(IconName.XMark)]
    [InlineData(IconName.ChevronLeft)]
    [InlineData(IconName.ChevronRight)]
    public void Icon_RendersAllIconNames(IconName iconName)
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, iconName));

        // Assert
        var svg = cut.Find("svg");
        svg.ShouldNotBeNull();
        var path = svg.QuerySelector("path");
        path.ShouldNotBeNull();
    }

    [Fact]
    public void Icon_AppliesDefaultClasses()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, IconName.Home));

        // Assert
        var svg = cut.Find("svg");
        svg.ClassList.ShouldContain("w-6");
        svg.ClassList.ShouldContain("h-6");
    }

    [Fact]
    public void Icon_HasCorrectStrokeAttributes()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Icon>(parameters => parameters
            .Add(p => p.Name, IconName.Home));

        // Assert
        var svg = cut.Find("svg");
        svg.GetAttribute("stroke-width").ShouldBe("1.5");
        svg.GetAttribute("stroke").ShouldBe("currentColor");
        svg.GetAttribute("viewBox").ShouldBe("0 0 24 24");
    }
}
