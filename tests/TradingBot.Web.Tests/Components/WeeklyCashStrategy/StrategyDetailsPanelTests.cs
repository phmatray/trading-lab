// <copyright file="StrategyDetailsPanelTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Bunit;
using Microsoft.Extensions.DependencyInjection;
using TradingBot.Web.Components.Features.WeeklyCashStrategy;
using TradingBot.Web.Models;

namespace TradingBot.Web.Tests.Components.WeeklyCashStrategy;

/// <summary>
/// Unit tests for StrategyDetailsPanel component.
/// </summary>
public class StrategyDetailsPanelTests
{
    /// <summary>
    /// Tests that component renders strategy configuration section correctly.
    /// </summary>
    [Fact]
    public void Render_StrategyConfiguration_DisplaysAllSettings()
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
            ExecutionDayOfWeek = 5,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            DaysBelowMA20 = 0,
        };

        // Act
        var cut = ctx.Render<StrategyDetailsPanel>(parameters => parameters
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Configuration");
        cut.Markup.ShouldContain("BTCW");
        cut.Markup.ShouldContain("COIN");
        cut.Markup.ShouldContain("Friday");
        cut.Markup.ShouldContain("15%");
        cut.Markup.ShouldContain("25%");
    }

    /// <summary>
    /// Tests that component displays enabled status in configuration.
    /// </summary>
    [Fact]
    public void Render_EnabledStrategy_ShowsEnabledStatus()
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
            ExecutionDayOfWeek = 5,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            DaysBelowMA20 = 0,
        };

        // Act
        var cut = ctx.Render<StrategyDetailsPanel>(parameters => parameters
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Enabled");
    }

    /// <summary>
    /// Tests that component displays market data section with prices.
    /// </summary>
    [Fact]
    public void Render_MarketData_DisplaysPricesAndMA20()
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
            CurrentUnderlyingPrice = 150.50m,
            CurrentEtpPrice = 45.25m,
            CurrentMA20 = 145.00m,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            DaysBelowMA20 = 0,
            ExecutionDayOfWeek = 5,
        };

        // Act
        var cut = ctx.Render<StrategyDetailsPanel>(parameters => parameters
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Market Data");
        cut.Markup.ShouldContain("$150.50");
        cut.Markup.ShouldContain("$45.25");
        cut.Markup.ShouldContain("$145.00");
    }

    /// <summary>
    /// Tests that component displays bullish trend indicator correctly.
    /// </summary>
    [Fact]
    public void Render_BullishTrend_DisplaysUpwardIndicator()
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
        var cut = ctx.Render<StrategyDetailsPanel>(parameters => parameters
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Bullish");
        cut.Markup.ShouldContain("Market Trend");
    }

    /// <summary>
    /// Tests that component displays bearish trend indicator correctly.
    /// </summary>
    [Fact]
    public void Render_BearishTrend_DisplaysDownwardIndicator()
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
        var cut = ctx.Render<StrategyDetailsPanel>(parameters => parameters
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Bearish");
        cut.Markup.ShouldContain("Market Trend");
    }

    /// <summary>
    /// Tests that component shows sell signal warning when days below MA20 >= 2.
    /// </summary>
    [Fact]
    public void Render_DaysBelowMA20GreaterThanOrEqualTwo_ShowsSellConditionTriggered()
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
            DaysBelowMA20 = 2,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            ExecutionDayOfWeek = 5,
        };

        // Act
        var cut = ctx.Render<StrategyDetailsPanel>(parameters => parameters
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Days Below MA20");
        cut.Markup.ShouldContain("Sell condition triggered");
    }

    /// <summary>
    /// Tests that component shows watch message when days below MA20 equals 1.
    /// </summary>
    [Fact]
    public void Render_DaysBelowMA20EqualsOne_ShowsWatchMessage()
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
            DaysBelowMA20 = 1,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            ExecutionDayOfWeek = 5,
        };

        // Act
        var cut = ctx.Render<StrategyDetailsPanel>(parameters => parameters
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Watch for sell signal");
    }

    /// <summary>
    /// Tests that component displays cash ratio visual indicator.
    /// </summary>
    [Fact]
    public void Render_CashRatio_DisplaysVisualProgressBar()
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
        var cut = ctx.Render<StrategyDetailsPanel>(parameters => parameters
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Cash Ratio");
        cut.Markup.ShouldContain("20.0%");
        cut.Markup.ShouldContain("Target Range");
    }

    /// <summary>
    /// Tests that component displays buy condition ready status.
    /// </summary>
    [Fact]
    public void Render_BuyConditionMet_ShowsReadyStatus()
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
        var cut = ctx.Render<StrategyDetailsPanel>(parameters => parameters
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Buy Condition");
        cut.Markup.ShouldContain("✓ Ready");
        cut.Markup.ShouldContain("Price above MA20, sufficient cash");
    }

    /// <summary>
    /// Tests that component displays sell condition triggered status.
    /// </summary>
    [Fact]
    public void Render_SellConditionMet_ShowsTriggeredStatus()
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
        var cut = ctx.Render<StrategyDetailsPanel>(parameters => parameters
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Sell Condition");
        cut.Markup.ShouldContain("✓ Triggered");
        cut.Markup.ShouldContain("2+ days below MA20");
    }

    /// <summary>
    /// Tests that component displays next scheduled execution.
    /// </summary>
    [Fact]
    public void Render_NextScheduledExecution_DisplaysFormattedDate()
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
        var cut = ctx.Render<StrategyDetailsPanel>(parameters => parameters
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Execution Schedule");
        cut.Markup.ShouldContain("Next Scheduled Execution");
        cut.Markup.ShouldContain("Friday, January 17");
    }

    /// <summary>
    /// Tests that component shows message when strategy is disabled (no schedule).
    /// </summary>
    [Fact]
    public void Render_DisabledStrategy_ShowsNoScheduleMessage()
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
            NextScheduledExecution = null,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            DaysBelowMA20 = 0,
            ExecutionDayOfWeek = 5,
        };

        // Act
        var cut = ctx.Render<StrategyDetailsPanel>(parameters => parameters
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Not scheduled (strategy disabled)");
    }

    /// <summary>
    /// Tests that component displays last execution timestamp when available.
    /// </summary>
    [Fact]
    public void Render_LastExecution_DisplaysTimestamp()
    {
        // Arrange
        using var ctx = new BunitContext();
        var lastExecution = new DateTime(2025, 1, 10, 14, 0, 0, DateTimeKind.Utc);
        var strategyState = new StrategyStateDto
        {
            StrategyId = Guid.NewGuid(),
            Name = "Weekly Cash Strategy",
            EtpSymbol = "BTCW",
            UnderlyingSymbol = "COIN",
            IsEnabled = true,
            LastExecutionTimestamp = lastExecution,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            DaysBelowMA20 = 0,
            ExecutionDayOfWeek = 5,
        };

        // Act
        var cut = ctx.Render<StrategyDetailsPanel>(parameters => parameters
            .Add(p => p.State, strategyState));

        // Assert
        cut.Markup.ShouldContain("Last Execution");
        cut.Markup.ShouldContain("Friday, January 10");
    }
}
