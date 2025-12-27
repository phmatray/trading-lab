using Shouldly;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Tests.TestDoubles;
using TradingStrat.Application.UseCases;

namespace TradingStrat.Application.Tests.UseCases;

public class CreatePortfolioUseCaseTests
{
    private readonly InMemoryPortfolioRepository _portfolioPort;
    private readonly CreatePortfolioUseCase _useCase;

    public CreatePortfolioUseCaseTests()
    {
        _portfolioPort = new InMemoryPortfolioRepository();
        _useCase = new CreatePortfolioUseCase(_portfolioPort);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidData_ShouldCreatePortfolio()
    {
        // Arrange
        var command = new CreatePortfolioCommand(
            Name: "Growth Portfolio",
            Description: "Long-term growth investments",
            InitialCash: 10000m);

        // Act
        var result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.PortfolioId.ShouldBeGreaterThan(0);
        result.Value.Name.ShouldBe("Growth Portfolio");
        result.Value.InitialCash.ShouldBe(10000m);
        result.Value.CreatedAt.ShouldNotBe(default);

        var savedPortfolio = await _portfolioPort.GetPortfolioByIdAsync(result.Value.PortfolioId);
        savedPortfolio.ShouldNotBeNull();
        savedPortfolio.Name.ShouldBe("Growth Portfolio");
        savedPortfolio.Description.ShouldBe("Long-term growth investments");
        savedPortfolio.Cash.ShouldBe(10000m);
    }

    [Fact]
    public async Task ExecuteAsync_WithZeroInitialCash_ShouldCreatePortfolio()
    {
        // Arrange
        var command = new CreatePortfolioCommand(
            Name: "Empty Portfolio",
            Description: null,
            InitialCash: 0m);

        // Act
        var result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.InitialCash.ShouldBe(0m);

        var savedPortfolio = await _portfolioPort.GetPortfolioByIdAsync(result.Value.PortfolioId);
        savedPortfolio.ShouldNotBeNull();
        savedPortfolio.Cash.ShouldBe(0m);
    }

    [Fact]
    public async Task ExecuteAsync_WithLargeInitialCash_ShouldCreatePortfolio()
    {
        // Arrange
        var command = new CreatePortfolioCommand(
            Name: "Retirement Portfolio",
            Description: "401k rollover",
            InitialCash: 1_000_000m);

        // Act
        var result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.InitialCash.ShouldBe(1_000_000m);

        var savedPortfolio = await _portfolioPort.GetPortfolioByIdAsync(result.Value.PortfolioId);
        savedPortfolio.ShouldNotBeNull();
        savedPortfolio.Cash.ShouldBe(1_000_000m);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullDescription_ShouldCreatePortfolio()
    {
        // Arrange
        var command = new CreatePortfolioCommand(
            Name: "Simple Portfolio",
            Description: null,
            InitialCash: 5000m);

        // Act
        var result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();

        var savedPortfolio = await _portfolioPort.GetPortfolioByIdAsync(result.Value.PortfolioId);
        savedPortfolio.ShouldNotBeNull();
        savedPortfolio.Description.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_CreateMultiplePortfolios_ShouldHaveUniqueIds()
    {
        // Arrange
        var command1 = new CreatePortfolioCommand("Portfolio 1", null, 1000m);
        var command2 = new CreatePortfolioCommand("Portfolio 2", null, 2000m);
        var command3 = new CreatePortfolioCommand("Portfolio 3", null, 3000m);

        // Act
        var result1 = await _useCase.ExecuteAsync(command1);
        var result2 = await _useCase.ExecuteAsync(command2);
        var result3 = await _useCase.ExecuteAsync(command3);

        // Assert
        result1.IsSuccess.ShouldBeTrue();
        result2.IsSuccess.ShouldBeTrue();
        result3.IsSuccess.ShouldBeTrue();
        result1.Value.PortfolioId.ShouldNotBe(result2.Value.PortfolioId);
        result2.Value.PortfolioId.ShouldNotBe(result3.Value.PortfolioId);
        result1.Value.PortfolioId.ShouldNotBe(result3.Value.PortfolioId);

        var allPortfolios = await _portfolioPort.GetAllPortfoliosAsync();
        allPortfolios.Count.ShouldBe(3);
    }

    [Fact]
    public void ExecuteAsync_WithEmptyName_ShouldThrow()
    {
        // Act & Assert - validation happens in command constructor
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            new CreatePortfolioCommand(
                Name: "",
                Description: null,
                InitialCash: 1000m));

        ex.ParamName.ShouldBe("Name");
    }

    [Fact]
    public void ExecuteAsync_WithWhitespaceName_ShouldThrow()
    {
        // Act & Assert - validation happens in command constructor
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            new CreatePortfolioCommand(
                Name: "   ",
                Description: null,
                InitialCash: 1000m));

        ex.ParamName.ShouldBe("Name");
    }

    [Fact]
    public void ExecuteAsync_WithNegativeInitialCash_ShouldThrow()
    {
        // Act & Assert - validation happens in command constructor
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            new CreatePortfolioCommand(
                Name: "Invalid Portfolio",
                Description: null,
                InitialCash: -1000m));

        ex.Message.ShouldContain("Initial cash cannot be negative");
        ex.ParamName.ShouldBe("InitialCash");
    }

    [Fact]
    public async Task ExecuteAsync_WithLongName_ShouldCreatePortfolio()
    {
        // Arrange
        string longName = new string('A', 200);
        var command = new CreatePortfolioCommand(
            Name: longName,
            Description: "Test",
            InitialCash: 1000m);

        // Act
        var result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe(longName);

        var savedPortfolio = await _portfolioPort.GetPortfolioByIdAsync(result.Value.PortfolioId);
        savedPortfolio.ShouldNotBeNull();
        savedPortfolio.Name.ShouldBe(longName);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSetCreatedAtTimestamp()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;
        var command = new CreatePortfolioCommand("Test", null, 1000m);

        // Act
        var result = await _useCase.ExecuteAsync(command);
        var afterCreate = DateTime.UtcNow;

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CreatedAt.ShouldBeGreaterThanOrEqualTo(beforeCreate);
        result.Value.CreatedAt.ShouldBeLessThanOrEqualTo(afterCreate);
    }
}
