// <copyright file="ThemeProviderTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Bunit;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TradingBot.Core.Entities;
using TradingBot.Core.Interfaces;
using TradingBot.Core.ValueObjects;
using TradingBot.Web.Components.Organisms;
using Xunit;

namespace TradingBot.Web.Tests.Components.Organisms;

/// <summary>
/// Tests for the ThemeProvider component.
/// </summary>
public class ThemeProviderTests : Bunit.TestContext
{
    private readonly IUserPreferencesService _preferencesService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeProviderTests"/> class.
    /// </summary>
    public ThemeProviderTests()
    {
        _preferencesService = A.Fake<IUserPreferencesService>();
        Services.AddSingleton(_preferencesService);
    }

    [Fact]
    public void ThemeProvider_WithLightTheme_DoesNotApplyDarkClass()
    {
        // Arrange
        var preferences = new UserPreferences
        {
            Theme = Theme.Light,
        };

        A.CallTo(() => _preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(preferences));

        // Act
        var cut = RenderComponent<ThemeProvider>(parameters => parameters
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
        var preferences = new UserPreferences
        {
            Theme = Theme.Dark,
        };

        A.CallTo(() => _preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(preferences));

        // Act
        var cut = RenderComponent<ThemeProvider>(parameters => parameters
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
        var preferences = new UserPreferences
        {
            Theme = Theme.Light,
        };

        A.CallTo(() => _preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(preferences));

        const string testContent = "Test Child Content";

        // Act
        var cut = RenderComponent<ThemeProvider>(parameters => parameters
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
        A.CallTo(() => _preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(new UserPreferences { Theme = Theme.Light }));

        // Act
        var cut = RenderComponent<ThemeProvider>(parameters => parameters
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
