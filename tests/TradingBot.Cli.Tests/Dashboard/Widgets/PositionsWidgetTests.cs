// <copyright file="PositionsWidgetTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using FakeItEasy;
using Shouldly;
using Spectre.Console.Testing;
using TradingBot.Cli.Dashboard.Widgets;
using TradingBot.Core.Enums;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Cli.Tests.Dashboard.Widgets;

/// <summary>
/// Tests for PositionsWidget.
/// </summary>
public sealed class PositionsWidgetTests
{
    private readonly IPortfolioManager _portfolioManager;
    private readonly PositionsWidget _widget;

    /// <summary>
    /// Initializes a new instance of the <see cref="PositionsWidgetTests"/> class.
    /// </summary>
    public PositionsWidgetTests()
    {
        _portfolioManager = A.Fake<IPortfolioManager>();
        _widget = new PositionsWidget(_portfolioManager);
    }

    /// <summary>
    /// Test constructor with null portfolio manager.
    /// </summary>
    [Fact]
    public void Constructor_WithNullPortfolioManager_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new PositionsWidget(null!));
    }

    /// <summary>
    /// Test title property.
    /// </summary>
    [Fact]
    public void Title_ShouldReturnOpenPositions()
    {
        // Assert
        _widget.Title.ShouldBe("Open Positions");
    }

    /// <summary>
    /// Test rendering with no positions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task RenderAsync_WithNoPositions_ShouldRenderEmptyMessage()
    {
        // Arrange
        A.CallTo(() => _portfolioManager.GetPositionsAsync(A<CancellationToken>._))
            .Returns(new List<Position>());

        // Act
        var result = await _widget.RenderAsync();

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Test rendering with positions and verify output using TestConsole.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task RenderAsync_WithPositions_ShouldRenderTable()
    {
        // Arrange
        var positions = new List<Position>
        {
            new Position
            {
                Id = Guid.NewGuid(),
                Symbol = "AAPL",
                Side = OrderSide.Buy,
                Quantity = 100m,
                EntryPrice = 150.00m,
                CurrentPrice = 155.00m,
                OpenedAt = DateTime.UtcNow,
                StrategyName = "TestStrategy",
            }
        };

        A.CallTo(() => _portfolioManager.GetPositionsAsync(A<CancellationToken>._))
            .Returns(positions);

        var console = new TestConsole();

        // Act
        var result = await _widget.RenderAsync();
        console.Write(result);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Spectre.Console.Table>();

        // Verify the rendered output contains expected values
        var output = console.Output;
        output.ShouldContain("AAPL");
        output.ShouldContain("100");
        output.ShouldContain("Symbol"); // Table header
        output.ShouldContain("Qty"); // Table header
    }
}
