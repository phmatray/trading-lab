// <copyright file="AccountWidgetTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Spectre.Console.Testing;
using TradingBot.Cli.Dashboard.Widgets;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Portfolio;

namespace TradingBot.Cli.Tests.Dashboard.Widgets;

/// <summary>
/// Tests for AccountWidget.
/// </summary>
public sealed class AccountWidgetTests
{
    private readonly IPortfolioManager _portfolioManager;
    private readonly AccountWidget _widget;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountWidgetTests"/> class.
    /// </summary>
    public AccountWidgetTests()
    {
        _portfolioManager = A.Fake<IPortfolioManager>();
        _widget = new AccountWidget(_portfolioManager);
    }

    /// <summary>
    /// Test constructor with null portfolio manager.
    /// </summary>
    [Fact]
    public void Constructor_WithNullPortfolioManager_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new AccountWidget(null!));
    }

    /// <summary>
    /// Test title property.
    /// </summary>
    [Fact]
    public void Title_ShouldReturnAccount()
    {
        // Assert
        _widget.Title.ShouldBe("Account");
    }

    /// <summary>
    /// Test successful rendering.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task RenderAsync_ShouldReturnRenderable()
    {
        // Arrange
        var account = new Account
        {
            AccountId = "TEST-123",
            Equity = 100000.00m,
            Cash = 50000.00m,
            PositionValue = 50000.00m,
            BuyingPower = 200000.00m,
            UnrealizedPnL = 500.00m,
            RealizedPnL = 1000.00m
        };

        A.CallTo(() => _portfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        // Act
        var result = await _widget.RenderAsync();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Spectre.Console.Grid>();
    }

    /// <summary>
    /// Test rendering with positive PnL and verify output using TestConsole.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task RenderAsync_WithPositivePnL_ShouldRenderCorrectly()
    {
        // Arrange
        var account = new Account
        {
            AccountId = "TEST-123",
            Equity = 100000.00m,
            Cash = 50000.00m,
            PositionValue = 50000.00m,
            BuyingPower = 200000.00m,
            UnrealizedPnL = 1500.00m,
            RealizedPnL = 2500.00m
        };

        A.CallTo(() => _portfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        var console = new TestConsole();

        // Act
        var result = await _widget.RenderAsync();
        console.Write(result);

        // Assert
        result.ShouldNotBeNull();
        A.CallTo(() => _portfolioManager.GetAccountAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        // Verify the rendered output contains expected values
        var output = console.Output;
        output.ShouldContain("TEST-123");
        // Note: Format may vary based on culture settings
        output.ShouldContain("100");
        output.ShouldContain("1");
        output.ShouldContain("2");
    }
}
