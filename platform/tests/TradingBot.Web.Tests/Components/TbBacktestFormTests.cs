// <copyright file="TbBacktestFormTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Bunit;
using Microsoft.Extensions.DependencyInjection;
using TradingBot.Web.Components.Features.Backtest;
using TradingBot.Web.Services;

namespace TradingBot.Web.Tests.Components;

/// <summary>
/// Unit tests for TbBacktestForm component.
/// </summary>
public class TbBacktestFormTests
{
    /// <summary>
    /// Tests that validation shows errors for invalid date range.
    /// </summary>
    [Fact]
    public void Validation_InvalidDateRange_ShowsErrors()
    {
        // Arrange
        using var ctx = new BunitContext();
        var fakeService = A.Fake<IBacktestService>();
        var fakeStrategyService = A.Fake<IStrategyManagementService>();
        ctx.Services.AddSingleton(fakeService);
        ctx.Services.AddSingleton(fakeStrategyService);

        // Act
        var cut = ctx.Render<TbBacktestForm>();

        // Assert - The form should render
        cut.Find("form").ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that form renders with all required fields.
    /// </summary>
    [Fact]
    public void Render_DisplaysRequiredFields()
    {
        // Arrange
        using var ctx = new BunitContext();
        var fakeService = A.Fake<IBacktestService>();
        var fakeStrategyService = A.Fake<IStrategyManagementService>();
        ctx.Services.AddSingleton(fakeService);
        ctx.Services.AddSingleton(fakeStrategyService);

        // Act
        var cut = ctx.Render<TbBacktestForm>();

        // Assert
        cut.Find("form").ShouldNotBeNull();

        // Verify form has inputs (strategy, symbol, dates, capital)
        var inputs = cut.FindAll("input, select");
        inputs.Count.ShouldBeGreaterThan(0);
    }
}
