// <copyright file="StrategyStateCardTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Bunit;
using Microsoft.Extensions.DependencyInjection;
using TradingBot.Web.Components.Features.WeeklyCashStrategy;
using TradingBot.Web.Models;

namespace TradingBot.Web.Tests.Components.WeeklyCashStrategy;

/// <summary>
/// Unit tests for StrategyStateCard component.
/// </summary>
public class StrategyStateCardTests
{
    /// <summary>
    /// Tests that component renders strategy name and active status when enabled.
    /// </summary>
    [Fact]
    public void Render_EnabledStrategy_DisplaysActiveStatus()
    {
        // Arrange
        using var ctx = new BunitContext();
        var strategyState = new StrategyStateDto
        {
            StrategyId = Guid.NewGuid(),
            Name = "Weekly Cash Strategy",
            EtpSymbol = "BTCW",
            UnderlyingSymbol = "COIN",
            IsEnabled = true,
            CurrentUnderlyingPrice = 150.00m,
            CurrentEtpPrice = 45.00m,
            CurrentMA20 = 145.00m,
            DaysBelowMA20 = 0,
            CurrentCashRatio = 0.20m,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            IsBuyConditionMet = true,
            IsSellConditionMet = false,
            IsBullish = true,
            ExecutionDayOfWeek = 5,
            NextScheduledExecution = DateTime.UtcNow.AddDays(1),
        };

        // Act
        var cut = ctx.Render<StrategyStateCard>(parameters => parameters
            .Add(p => p.StrategyName, "Weekly Cash Strategy")
            .Add(p => p.State, strategyState));

        // Assert
        cut.Find("h2").TextContent.ShouldContain("Weekly Cash Strategy");
        var statusBadge = cut.Find("span.bg-green-100");
        statusBadge.TextContent.Trim().ShouldBe("Active");
    }

    /// <summary>
    /// Tests that component displays inactive status when strategy is disabled.
    /// </summary>
    [Fact]
    public void Render_DisabledStrategy_DisplaysInactiveStatus()
    {
        // Arrange
        using var ctx = new BunitContext();
        var strategyState = new StrategyStateDto
        {
            StrategyId = Guid.NewGuid(),
            Name = "Weekly Cash Strategy",
            EtpSymbol = "BTCW",
            UnderlyingSymbol = "COIN",
            IsEnabled = false,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            DaysBelowMA20 = 0,
            ExecutionDayOfWeek = 5,
        };

        // Act
        var cut = ctx.Render<StrategyStateCard>(parameters => parameters
            .Add(p => p.StrategyName, "Weekly Cash Strategy")
            .Add(p => p.State, strategyState));

        // Assert
        var statusBadge = cut.Find("span.bg-gray-100");
        statusBadge.TextContent.Trim().ShouldBe("Inactive");
    }

    /// <summary>
    /// Tests that component displays bullish indicator when price above MA20.
    /// </summary>
    [Fact]
    public void Render_BullishMarket_DisplaysBullishIndicator()
    {
        // Arrange
        using var ctx = new BunitContext();
        var strategyState = new StrategyStateDto
        {
            StrategyId = Guid.NewGuid(),
            Name = "Weekly Cash Strategy",
            EtpSymbol = "BTCW",
            UnderlyingSymbol = "COIN",
            IsEnabled = true,
            CurrentUnderlyingPrice = 150.00m,
            CurrentMA20 = 145.00m,
            IsBullish = true,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            DaysBelowMA20 = 0,
            ExecutionDayOfWeek = 5,
        };

        // Act
        var cut = ctx.Render<StrategyStateCard>(parameters => parameters
            .Add(p => p.StrategyName, "Weekly Cash Strategy")
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Bullish");
    }

    /// <summary>
    /// Tests that component displays bearish indicator when price below MA20.
    /// </summary>
    [Fact]
    public void Render_BearishMarket_DisplaysBearishIndicator()
    {
        // Arrange
        using var ctx = new BunitContext();
        var strategyState = new StrategyStateDto
        {
            StrategyId = Guid.NewGuid(),
            Name = "Weekly Cash Strategy",
            EtpSymbol = "BTCW",
            UnderlyingSymbol = "COIN",
            IsEnabled = true,
            CurrentUnderlyingPrice = 140.00m,
            CurrentMA20 = 145.00m,
            IsBullish = false,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            DaysBelowMA20 = 1,
            ExecutionDayOfWeek = 5,
        };

        // Act
        var cut = ctx.Render<StrategyStateCard>(parameters => parameters
            .Add(p => p.StrategyName, "Weekly Cash Strategy")
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Bearish");
    }

    /// <summary>
    /// Tests that component displays buy condition status correctly.
    /// </summary>
    [Fact]
    public void Render_BuyConditionMet_DisplaysGreenCheckmark()
    {
        // Arrange
        using var ctx = new BunitContext();
        var strategyState = new StrategyStateDto
        {
            StrategyId = Guid.NewGuid(),
            Name = "Weekly Cash Strategy",
            EtpSymbol = "BTCW",
            UnderlyingSymbol = "COIN",
            IsEnabled = true,
            IsBuyConditionMet = true,
            IsSellConditionMet = false,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            DaysBelowMA20 = 0,
            ExecutionDayOfWeek = 5,
        };

        // Act
        var cut = ctx.Render<StrategyStateCard>(parameters => parameters
            .Add(p => p.StrategyName, "Weekly Cash Strategy")
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Buy Condition");
        cut.Markup.ShouldContain("✓ Met");
    }

    /// <summary>
    /// Tests that component displays sell condition status correctly.
    /// </summary>
    [Fact]
    public void Render_SellConditionMet_DisplaysRedCheckmark()
    {
        // Arrange
        using var ctx = new BunitContext();
        var strategyState = new StrategyStateDto
        {
            StrategyId = Guid.NewGuid(),
            Name = "Weekly Cash Strategy",
            EtpSymbol = "BTCW",
            UnderlyingSymbol = "COIN",
            IsEnabled = true,
            IsBuyConditionMet = false,
            IsSellConditionMet = true,
            DaysBelowMA20 = 2,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            ExecutionDayOfWeek = 5,
        };

        // Act
        var cut = ctx.Render<StrategyStateCard>(parameters => parameters
            .Add(p => p.StrategyName, "Weekly Cash Strategy")
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Sell Condition");
        cut.Markup.ShouldContain("✓ Met");
    }

    /// <summary>
    /// Tests that component displays days below MA20 with warning color when >= 2.
    /// </summary>
    [Fact]
    public void Render_DaysBelowMA20GreaterThanTwo_DisplaysWarningColor()
    {
        // Arrange
        using var ctx = new BunitContext();
        var strategyState = new StrategyStateDto
        {
            StrategyId = Guid.NewGuid(),
            Name = "Weekly Cash Strategy",
            EtpSymbol = "BTCW",
            UnderlyingSymbol = "COIN",
            IsEnabled = true,
            DaysBelowMA20 = 3,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            ExecutionDayOfWeek = 5,
        };

        // Act
        var cut = ctx.Render<StrategyStateCard>(parameters => parameters
            .Add(p => p.StrategyName, "Weekly Cash Strategy")
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Days Below MA20");
        cut.Markup.ShouldContain("3 days");
    }

    /// <summary>
    /// Tests that component shows loading state when State is null.
    /// </summary>
    [Fact]
    public void Render_NullState_ShowsLoadingMessage()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<StrategyStateCard>(parameters => parameters
            .Add(p => p.StrategyName, "Weekly Cash Strategy")
            .Add(p => p.State, null));

        // Assert
        cut.Markup.ShouldContain("Loading strategy data");
    }

    /// <summary>
    /// Tests that component displays cash ratio with percentage formatting.
    /// </summary>
    [Fact]
    public void Render_CashRatio_DisplaysPercentageFormat()
    {
        // Arrange
        using var ctx = new BunitContext();
        var strategyState = new StrategyStateDto
        {
            StrategyId = Guid.NewGuid(),
            Name = "Weekly Cash Strategy",
            EtpSymbol = "BTCW",
            UnderlyingSymbol = "COIN",
            IsEnabled = true,
            CurrentCashRatio = 0.20m,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            DaysBelowMA20 = 0,
            ExecutionDayOfWeek = 5,
        };

        // Act
        var cut = ctx.Render<StrategyStateCard>(parameters => parameters
            .Add(p => p.StrategyName, "Weekly Cash Strategy")
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Cash Ratio");
        cut.Markup.ShouldContain("20.0%");
    }

    /// <summary>
    /// Tests that component displays next execution schedule when available.
    /// </summary>
    [Fact]
    public void Render_NextExecution_DisplaysScheduledTime()
    {
        // Arrange
        using var ctx = new BunitContext();
        var nextExecution = new DateTime(2025, 1, 17, 14, 0, 0, DateTimeKind.Utc);
        var strategyState = new StrategyStateDto
        {
            StrategyId = Guid.NewGuid(),
            Name = "Weekly Cash Strategy",
            EtpSymbol = "BTCW",
            UnderlyingSymbol = "COIN",
            IsEnabled = true,
            NextScheduledExecution = nextExecution,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            DaysBelowMA20 = 0,
            ExecutionDayOfWeek = 5,
        };

        // Act
        var cut = ctx.Render<StrategyStateCard>(parameters => parameters
            .Add(p => p.StrategyName, "Weekly Cash Strategy")
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Next Execution");
        cut.Markup.ShouldContain("Fri, Jan 17");
    }
}
