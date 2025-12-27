using Shouldly;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Tests.TestDoubles;
using TradingStrat.Application.UseCases;

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
        var result = await _useCase.AddPositionAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldBeGreaterThan(0);
        result.Value.PortfolioId.ShouldBe(portfolio.Id);
        result.Value.Ticker.ShouldBe("AAPL");
        result.Value.Quantity.ShouldBe(100);
        result.Value.EntryPrice.ShouldBe(150.50m);
        result.Value.EntryDate.ShouldBe(new DateTime(2024, 1, 15));
        result.Value.Notes.ShouldBe("Strong buy signal");

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
        var result = await _useCase.AddPositionAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Ticker.ShouldBe("MSFT");
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
        var result = await _useCase.AddPositionAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Notes.ShouldBeNull();
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
        var result1 = await _useCase.AddPositionAsync(command1);
        var result2 = await _useCase.AddPositionAsync(command2);
        var result3 = await _useCase.AddPositionAsync(command3);

        // Assert
        result1.IsSuccess.ShouldBeTrue();
        result2.IsSuccess.ShouldBeTrue();
        result3.IsSuccess.ShouldBeTrue();

        var positions = await _portfolioPort.GetPositionsByPortfolioAsync(portfolio.Id);
        positions.Count.ShouldBe(3);
        positions.Select(p => p.Ticker).ShouldBe(new[] { "AAPL", "GOOGL", "MSFT" }, ignoreOrder: true);
    }

    [Fact]
    public async Task AddPositionAsync_DuplicateTicker_ShouldReturnFailure()
    {
        // Arrange
        var portfolio = await _portfolioPort.CreatePortfolioAsync("Test Portfolio", null, 10000m);
        var command1 = new AddPositionCommand(portfolio.Id, "AAPL", 100, 150m, DateTime.Today, null);
        var command2 = new AddPositionCommand(portfolio.Id, "AAPL", 50, 155m, DateTime.Today, null);

        await _useCase.AddPositionAsync(command1);

        // Act
        var result = await _useCase.AddPositionAsync(command2);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Code.ShouldBe("POSITION_ALREADY_EXISTS");
        result.Errors[0].Message.ShouldContain("AAPL");
    }

    [Fact]
    public async Task AddPositionAsync_WithNonExistentPortfolio_ShouldReturnFailure()
    {
        // Arrange
        var command = new AddPositionCommand(
            PortfolioId: 9999,
            Ticker: "AAPL",
            Quantity: 100,
            EntryPrice: 150m,
            EntryDate: DateTime.Today,
            Notes: null);

        // Act
        var result = await _useCase.AddPositionAsync(command);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Code.ShouldBe("PORTFOLIO_NOT_FOUND");
        result.Errors[0].Message.ShouldContain("9999");
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
        var addResult = await _useCase.AddPositionAsync(addCommand);

        var updateCommand = new UpdatePositionCommand(
            PositionId: addResult.Value.Id,
            Quantity: 150,
            EntryPrice: 155m,
            Notes: "Averaged up");

        // Act
        var result = await _useCase.UpdatePositionAsync(updateCommand);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Quantity.ShouldBe(150);
        result.Value.EntryPrice.ShouldBe(155m);
        result.Value.Notes.ShouldBe("Averaged up");
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
        var addResult = await _useCase.AddPositionAsync(addCommand);

        // Act
        var result = await _useCase.DeletePositionAsync(addResult.Value.Id);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();

        var positions = await _portfolioPort.GetPositionsByPortfolioAsync(portfolio.Id);
        positions.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeletePositionAsync_WithNonExistentId_ShouldReturnFailure()
    {
        // Act
        var result = await _useCase.DeletePositionAsync(9999);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Code.ShouldBe("POSITION_NOT_FOUND");
        result.Errors[0].Message.ShouldContain("9999");
    }
}
