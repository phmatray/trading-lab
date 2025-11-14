// <copyright file="ThemeProviderTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Bunit;
using Microsoft.Extensions.DependencyInjection;
using TradingBot.Core.Entities;
using TradingBot.Core.Interfaces;
using TradingBot.Core.ValueObjects;
using TradingBot.Web.Components.Organisms;

namespace TradingBot.Web.Tests.Components.Organisms;

/// <summary>
/// Tests for the ThemeProvider component.
/// </summary>
public class ThemeProviderTests
{
    [Fact]
    public void ThemeProvider_WithLightTheme_DoesNotApplyDarkClass()
    {
        // Arrange
        using var ctx = new BunitContext();

        var preferencesService = A.Fake<IUserPreferencesService>();
        var preferences = new UserPreferences
        {
            Theme = Theme.Light,
        };

        A.CallTo(() => preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(preferences));

        ctx.Services.AddSingleton(preferencesService);

        // Act
        var cut = ctx.Render<TbThemeProvider>(parameters => parameters
            .Add(p => p.ChildContent, builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, "Test Content");
                builder.CloseElement();
            }));

        // Assert
        var wrapper = cut.Find("div");
        wrapper.ClassName.ShouldNotBeNull();
        wrapper.ClassName.ShouldNotContain("dark");
    }

    [Fact]
    public void ThemeProvider_WithDarkTheme_AppliesDarkClass()
    {
        // Arrange
        using var ctx = new BunitContext();

        var preferencesService = A.Fake<IUserPreferencesService>();
        var preferences = new UserPreferences
        {
            Theme = Theme.Dark,
        };

        A.CallTo(() => preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(preferences));

        ctx.Services.AddSingleton(preferencesService);

        // Act
        var cut = ctx.Render<TbThemeProvider>(parameters => parameters
            .Add(p => p.ChildContent, builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, "Test Content");
                builder.CloseElement();
            }));

        // Assert
        var wrapper = cut.Find("div");
        wrapper.ClassName.ShouldNotBeNull();
        wrapper.ClassName.ShouldContain("dark");
    }

    [Fact]
    public void ThemeProvider_RendersChildContent()
    {
        // Arrange
        using var ctx = new BunitContext();

        var preferencesService = A.Fake<IUserPreferencesService>();
        var preferences = new UserPreferences
        {
            Theme = Theme.Light,
        };

        A.CallTo(() => preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(preferences));

        ctx.Services.AddSingleton(preferencesService);

        const string testContent = "Test Child Content";

        // Act
        var cut = ctx.Render<TbThemeProvider>(parameters => parameters
            .Add(p => p.ChildContent, builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, testContent);
                builder.CloseElement();
            }));

        // Assert
        cut.Markup.ShouldContain(testContent);
    }

    [Fact]
    public void ThemeProvider_WithNullPreferences_DefaultsToLightTheme()
    {
        // Arrange
        using var ctx = new BunitContext();

        var preferencesService = A.Fake<IUserPreferencesService>();
        A.CallTo(() => preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(new UserPreferences { Theme = Theme.Light }));

        ctx.Services.AddSingleton(preferencesService);

        // Act
        var cut = ctx.Render<TbThemeProvider>(parameters => parameters
            .Add(p => p.ChildContent, builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, "Test Content");
                builder.CloseElement();
            }));

        // Assert
        var wrapper = cut.Find("div");
        wrapper.ClassName.ShouldNotBeNull();
        wrapper.ClassName.ShouldNotContain("dark");
    }
}
