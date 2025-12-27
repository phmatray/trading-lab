using Shouldly;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Tests.TestDoubles;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Tests.UseCases;

public class ManagePositionsUseCaseTests
{
    private readonly InMemoryPortfolioRepository _portfolioPort;
    private readonly ManagePositionsUseCase _useCase;

    public ManagePositionsUseCaseTests()
    {
        _portfolioPort = new InMemoryPortfolioRepository();
        _useCase = new ManagePositionsUseCase(_portfolioPort);
    }

    [Fact]
    public async Task AddPositionAsync_WithValidData_ShouldAddPosition()
    {
        // Arrange
        var portfolio = await _portfolioPort.CreatePortfolioAsync("Test Portfolio", null, 10000m);
        var command = new AddPositionCommand(
            PortfolioId: portfolio.Id,
            Ticker: "AAPL",
            Quantity: 100,
            EntryPrice: 150.50m,
            EntryDate: new DateTime(2024, 1, 15),
            Notes: "Strong buy signal");

        // Act
        Position result = await _useCase.AddPositionAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBeGreaterThan(0);
        result.PortfolioId.ShouldBe(portfolio.Id);
        result.Ticker.ShouldBe("AAPL");
        result.Quantity.ShouldBe(100);
        result.EntryPrice.ShouldBe(150.50m);
        result.EntryDate.ShouldBe(new DateTime(2024, 1, 15));
        result.Notes.ShouldBe("Strong buy signal");

        var positions = await _portfolioPort.GetPositionsByPortfolioAsync(portfolio.Id);
        positions.Count.ShouldBe(1);
        positions[0].Ticker.ShouldBe("AAPL");
    }

    [Fact]
    public async Task AddPositionAsync_WithLowercaseTicker_ShouldConvertToUppercase()
    {
        // Arrange
        var portfolio = await _portfolioPort.CreatePortfolioAsync("Test Portfolio", null, 10000m);
        var command = new AddPositionCommand(
            PortfolioId: portfolio.Id,
            Ticker: "msft",
            Quantity: 50,
            EntryPrice: 300m,
            EntryDate: DateTime.Today,
            Notes: null);

        // Act
        Position result = await _useCase.AddPositionAsync(command);

        // Assert
        result.Ticker.ShouldBe("MSFT");
    }

    [Fact]
    public async Task AddPositionAsync_WithNullNotes_ShouldAddPosition()
    {
        // Arrange
        var portfolio = await _portfolioPort.CreatePortfolioAsync("Test Portfolio", null, 10000m);
        var command = new AddPositionCommand(
            PortfolioId: portfolio.Id,
            Ticker: "GOOGL",
            Quantity: 10,
            EntryPrice: 2500m,
            EntryDate: DateTime.Today,
            Notes: null);

        // Act
        Position result = await _useCase.AddPositionAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Notes.ShouldBeNull();
    }

    [Fact]
    public async Task AddPositionAsync_MultiplePositionsSamePortfolio_ShouldAddAll()
    {
        // Arrange
        var portfolio = await _portfolioPort.CreatePortfolioAsync("Diversified Portfolio", null, 50000m);
        var command1 = new AddPositionCommand(portfolio.Id, "AAPL", 100, 150m, DateTime.Today, null);
        var command2 = new AddPositionCommand(portfolio.Id, "MSFT", 50, 300m, DateTime.Today, null);
        var command3 = new AddPositionCommand(portfolio.Id, "GOOGL", 25, 2500m, DateTime.Today, null);

        // Act
        await _useCase.AddPositionAsync(command1);
        await _useCase.AddPositionAsync(command2);
        await _useCase.AddPositionAsync(command3);

        // Assert
        var positions = await _portfolioPort.GetPositionsByPortfolioAsync(portfolio.Id);
        positions.Count.ShouldBe(3);
        positions.Select(p => p.Ticker).ShouldBe(new[] { "AAPL", "GOOGL", "MSFT" }, ignoreOrder: true);
    }

    [Fact]
    public async Task AddPositionAsync_DuplicateTicker_ShouldThrow()
    {
        // Arrange
        var portfolio = await _portfolioPort.CreatePortfolioAsync("Test Portfolio", null, 10000m);
        var command1 = new AddPositionCommand(portfolio.Id, "AAPL", 100, 150m, DateTime.Today, null);
        var command2 = new AddPositionCommand(portfolio.Id, "AAPL", 50, 155m, DateTime.Today, null);

        await _useCase.AddPositionAsync(command1);

        // Act & Assert
        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _useCase.AddPositionAsync(command2));
        ex.Message.ShouldContain("Position with ticker AAPL already exists");
    }

    [Fact]
    public async Task AddPositionAsync_WithNonExistentPortfolio_ShouldThrow()
    {
        // Arrange
        var command = new AddPositionCommand(
            PortfolioId: 9999,
            Ticker: "AAPL",
            Quantity: 100,
            EntryPrice: 150m,
            EntryDate: DateTime.Today,
            Notes: null);

        // Act & Assert
        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _useCase.AddPositionAsync(command));
        ex.Message.ShouldContain("Portfolio 9999 not found");
    }

    [Fact]
    public void AddPositionAsync_WithEmptyTicker_ShouldThrow()
    {
        // Act & Assert - validation happens in command constructor
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            new AddPositionCommand(
                PortfolioId: 1,
                Ticker: "",
                Quantity: 100,
                EntryPrice: 150m,
                EntryDate: DateTime.Today,
                Notes: null));

        ex.ParamName.ShouldBe("Ticker");
    }

    [Fact]
    public void AddPositionAsync_WithZeroQuantity_ShouldThrow()
    {
        // Act & Assert - validation happens in command constructor
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            new AddPositionCommand(
                PortfolioId: 1,
                Ticker: "AAPL",
                Quantity: 0,
                EntryPrice: 150m,
                EntryDate: DateTime.Today,
                Notes: null));

        ex.Message.ShouldContain("Quantity must be positive");
        ex.ParamName.ShouldBe("Quantity");
    }

    [Fact]
    public void AddPositionAsync_WithNegativeQuantity_ShouldThrow()
    {
        // Act & Assert - validation happens in command constructor
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            new AddPositionCommand(
                PortfolioId: 1,
                Ticker: "AAPL",
                Quantity: -100,
                EntryPrice: 150m,
                EntryDate: DateTime.Today,
                Notes: null));

        ex.Message.ShouldContain("Quantity must be positive");
        ex.ParamName.ShouldBe("Quantity");
    }

    [Fact]
    public void AddPositionAsync_WithZeroEntryPrice_ShouldThrow()
    {
        // Act & Assert - validation happens in command constructor
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            new AddPositionCommand(
                PortfolioId: 1,
                Ticker: "AAPL",
                Quantity: 100,
                EntryPrice: 0m,
                EntryDate: DateTime.Today,
                Notes: null));

        ex.Message.ShouldContain("Entry price must be positive");
        ex.ParamName.ShouldBe("EntryPrice");
    }

    [Fact]
    public void AddPositionAsync_WithNegativeEntryPrice_ShouldThrow()
    {
        // Act & Assert - validation happens in command constructor
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            new AddPositionCommand(
                PortfolioId: 1,
                Ticker: "AAPL",
                Quantity: 100,
                EntryPrice: -150m,
                EntryDate: DateTime.Today,
                Notes: null));

        ex.Message.ShouldContain("Entry price must be positive");
        ex.ParamName.ShouldBe("EntryPrice");
    }

    [Fact]
    public async Task UpdatePositionAsync_WithValidData_ShouldUpdatePosition()
    {
        // Arrange
        var portfolio = await _portfolioPort.CreatePortfolioAsync("Test Portfolio", null, 10000m);
        var addCommand = new AddPositionCommand(portfolio.Id, "AAPL", 100, 150m, DateTime.Today, "Initial");
        var addedPosition = await _useCase.AddPositionAsync(addCommand);

        var updateCommand = new UpdatePositionCommand(
            PositionId: addedPosition.Id,
            Quantity: 150,
            EntryPrice: 155m,
            Notes: "Averaged up");

        // Act
        Position result = await _useCase.UpdatePositionAsync(updateCommand);

        // Assert
        result.ShouldNotBeNull();
        result.Quantity.ShouldBe(150);
        result.EntryPrice.ShouldBe(155m);
        result.Notes.ShouldBe("Averaged up");
    }

    [Fact]
    public void UpdatePositionAsync_WithZeroQuantity_ShouldThrow()
    {
        // Act & Assert - validation happens in command constructor
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            new UpdatePositionCommand(
                PositionId: 1,
                Quantity: 0,
                EntryPrice: 150m,
                Notes: null));

        ex.Message.ShouldContain("Quantity must be positive");
        ex.ParamName.ShouldBe("Quantity");
    }

    [Fact]
    public void UpdatePositionAsync_WithZeroEntryPrice_ShouldThrow()
    {
        // Act & Assert - validation happens in command constructor
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            new UpdatePositionCommand(
                PositionId: 1,
                Quantity: 100,
                EntryPrice: 0m,
                Notes: null));

        ex.Message.ShouldContain("Entry price must be positive");
        ex.ParamName.ShouldBe("EntryPrice");
    }

    [Fact]
    public async Task DeletePositionAsync_WithValidId_ShouldDeletePosition()
    {
        // Arrange
        var portfolio = await _portfolioPort.CreatePortfolioAsync("Test Portfolio", null, 10000m);
        var addCommand = new AddPositionCommand(portfolio.Id, "AAPL", 100, 150m, DateTime.Today, null);
        var addedPosition = await _useCase.AddPositionAsync(addCommand);

        // Act
        await _useCase.DeletePositionAsync(addedPosition.Id);

        // Assert
        var positions = await _portfolioPort.GetPositionsByPortfolioAsync(portfolio.Id);
        positions.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeletePositionAsync_WithNonExistentId_ShouldNotThrow()
    {
        // Arrange & Act
        await _useCase.DeletePositionAsync(9999);

        // Assert - no exception should be thrown (idempotent delete)
    }
}
