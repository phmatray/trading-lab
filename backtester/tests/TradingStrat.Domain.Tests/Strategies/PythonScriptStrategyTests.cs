using Shouldly;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.Tests.Builders;
using TradingStrat.Domain.Tests.TestDoubles;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.Strategies;

public class PythonScriptStrategyTests
{
    private readonly IIndicatorCalculator _indicatorCalculator;

    public PythonScriptStrategyTests()
    {
        _indicatorCalculator = new IndicatorCalculator();
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateStrategy()
    {
        // Arrange
        var fakePythonExecutor = new FakePythonExecutor();
        const string pythonCode = "def generate_signal(index, price, cash, position):\n    return {'action': 'hold', 'quantity': 0, 'reason': 'test'}";

        // Act
        var strategy = new PythonScriptStrategy(
            _indicatorCalculator,
            fakePythonExecutor,
            pythonCode,
            "Test Python Strategy",
            "A test strategy");

        // Assert
        strategy.ShouldNotBeNull();
        strategy.Name.ShouldBe("Test Python Strategy");
        strategy.Description.ShouldBe("A test strategy");
    }

    [Fact]
    public void Constructor_WithNullExecutor_ShouldThrow()
    {
        // Arrange & Act
        Func<PythonScriptStrategy> act = () => new PythonScriptStrategy(
            _indicatorCalculator,
            null!,
            "code",
            "name",
            "description");

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_WithNullCode_ShouldThrow()
    {
        // Arrange
        var fakePythonExecutor = new FakePythonExecutor();

        // Act
        Func<PythonScriptStrategy> act = () => new PythonScriptStrategy(
            _indicatorCalculator,
            fakePythonExecutor,
            null!,
            "name",
            "description");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WithEmptyCode_ShouldThrow()
    {
        // Arrange
        var fakePythonExecutor = new FakePythonExecutor();

        // Act
        Func<PythonScriptStrategy> act = () => new PythonScriptStrategy(
            _indicatorCalculator,
            fakePythonExecutor,
            "   ",
            "name",
            "description");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Initialize_ShouldCallPythonExecutor()
    {
        // Arrange
        var fakePythonExecutor = new FakePythonExecutor();
        const string pythonCode = "def generate_signal(index, price, cash, position):\n    return {'action': 'hold', 'quantity': 0, 'reason': 'test'}";

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m, 103m, 104m)
            .Build();

        var strategy = new PythonScriptStrategy(
            _indicatorCalculator,
            fakePythonExecutor,
            pythonCode,
            "Test Strategy",
            "Description");

        // Act
        strategy.Initialize(prices.AsReadOnly());

        // Assert
        fakePythonExecutor.InitializeWasCalled.ShouldBeTrue();
    }

    [Fact]
    public void GenerateSignal_WithBuyAction_ReturnsBuySignal()
    {
        // Arrange
        var fakePythonExecutor = new FakePythonExecutor(
            new PythonSignalResult("buy", 10, "Python says buy"));

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m, 103m, 104m)
            .Build();

        var strategy = new PythonScriptStrategy(
            _indicatorCalculator,
            fakePythonExecutor,
            "code",
            "Test Strategy",
            "Description");

        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(4, 10000m, 0);

        // Assert
        signal.Type.ShouldBe(SignalType.Buy);
        signal.Quantity.ShouldBe(10);
        signal.Price.ShouldBe(104m);
        signal.Reason.ShouldBe("Python says buy");
    }

    [Fact]
    public void GenerateSignal_WithSellAction_ReturnsSellSignal()
    {
        // Arrange
        var fakePythonExecutor = new FakePythonExecutor(
            new PythonSignalResult("sell", 5, "Python says sell"));

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m, 103m, 104m)
            .Build();

        var strategy = new PythonScriptStrategy(
            _indicatorCalculator,
            fakePythonExecutor,
            "code",
            "Test Strategy",
            "Description");

        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(4, 10000m, 5);

        // Assert
        signal.Type.ShouldBe(SignalType.Sell);
        signal.Quantity.ShouldBe(5);
        signal.Price.ShouldBe(104m);
        signal.Reason.ShouldBe("Python says sell");
    }

    [Fact]
    public void GenerateSignal_WithHoldAction_ReturnsHoldSignal()
    {
        // Arrange
        var fakePythonExecutor = new FakePythonExecutor(
            new PythonSignalResult("hold", 0, "Python says hold"));

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m, 103m, 104m)
            .Build();

        var strategy = new PythonScriptStrategy(
            _indicatorCalculator,
            fakePythonExecutor,
            "code",
            "Test Strategy",
            "Description");

        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(4, 10000m, 0);

        // Assert
        signal.Type.ShouldBe(SignalType.Hold);
        signal.Quantity.ShouldBe(0);
        signal.Reason.ShouldBe("Python says hold");
    }

    [Fact]
    public void GenerateSignal_WithInvalidAction_ShouldThrow()
    {
        // Arrange
        var fakePythonExecutor = new FakePythonExecutor(
            new PythonSignalResult("invalid_action", 0, "Bad action"));

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m, 103m, 104m)
            .Build();

        var strategy = new PythonScriptStrategy(
            _indicatorCalculator,
            fakePythonExecutor,
            "code",
            "Test Strategy",
            "Description");

        strategy.Initialize(prices.AsReadOnly());

        // Act
        Func<TradeSignal> act = () => strategy.GenerateSignal(4, 10000m, 0);

        // Assert
        PythonExecutionException ex = Should.Throw<PythonExecutionException>(act);
        ex.Message.ShouldContain("Invalid action from Python");
        ex.Message.ShouldContain("invalid_action");
    }

    [Fact]
    public void GenerateSignal_WithIndexOutOfRange_ReturnsHold()
    {
        // Arrange
        var fakePythonExecutor = new FakePythonExecutor();

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m)
            .Build();

        var strategy = new PythonScriptStrategy(
            _indicatorCalculator,
            fakePythonExecutor,
            "code",
            "Test Strategy",
            "Description");

        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(999, 10000m, 0);

        // Assert
        signal.Type.ShouldBe(SignalType.Hold);
        signal.Reason.ShouldBe("Insufficient data");
    }

    [Fact]
    public void GenerateSignal_CaseInsensitiveActions_ShouldWork()
    {
        // Arrange - test uppercase, lowercase, mixed case
        var fakePythonExecutor = new FakePythonExecutor(
            new PythonSignalResult("BUY", 10, "Uppercase buy"),
            new PythonSignalResult("Sell", 5, "Title case sell"),
            new PythonSignalResult("HOLD", 0, "Uppercase hold"));

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m, 103m, 104m)
            .Build();

        var strategy = new PythonScriptStrategy(
            _indicatorCalculator,
            fakePythonExecutor,
            "code",
            "Test Strategy",
            "Description");

        strategy.Initialize(prices.AsReadOnly());

        // Act & Assert
        TradeSignal signal1 = strategy.GenerateSignal(2, 10000m, 0);
        signal1.Type.ShouldBe(SignalType.Buy);

        TradeSignal signal2 = strategy.GenerateSignal(3, 10000m, 10);
        signal2.Type.ShouldBe(SignalType.Sell);

        TradeSignal signal3 = strategy.GenerateSignal(4, 10000m, 0);
        signal3.Type.ShouldBe(SignalType.Hold);
    }

    [Fact]
    public void GenerateSignal_WithDynamicLogic_UsesSignalGenerator()
    {
        // Arrange - Create fake executor with custom logic
        var fakePythonExecutor = new FakePythonExecutor((index, price, cash, position) =>
        {
            // Simple logic: buy when price < 102, sell when price > 103
            if (price < 102m && position == 0)
            {
                return new PythonSignalResult("buy", 10, $"Buy at {price}");
            }

            if (price > 103m && position > 0)
            {
                return new PythonSignalResult("sell", position, $"Sell at {price}");
            }

            return new PythonSignalResult("hold", 0, "Waiting");
        });

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m, 103m, 104m)
            .Build();

        var strategy = new PythonScriptStrategy(
            _indicatorCalculator,
            fakePythonExecutor,
            "code",
            "Test Strategy",
            "Description");

        strategy.Initialize(prices.AsReadOnly());

        // Act & Assert
        TradeSignal signal1 = strategy.GenerateSignal(0, 10000m, 0);
        signal1.Type.ShouldBe(SignalType.Buy);
        signal1.Reason.ShouldContain("100");

        TradeSignal signal2 = strategy.GenerateSignal(4, 10000m, 10);
        signal2.Type.ShouldBe(SignalType.Sell);
        signal2.Reason.ShouldContain("104");
    }

    [Fact]
    public void GetParameters_ShouldReturnPythonStrategyInfo()
    {
        // Arrange
        var fakePythonExecutor = new FakePythonExecutor();
        const string pythonCode = "def generate_signal(index, price, cash, position):\n    return {'action': 'hold', 'quantity': 0, 'reason': 'test'}";

        var strategy = new PythonScriptStrategy(
            _indicatorCalculator,
            fakePythonExecutor,
            pythonCode,
            "Test Strategy",
            "Description");

        // Act
        Dictionary<string, object> parameters = strategy.GetParameters();

        // Assert
        parameters.ShouldContainKey("StrategyType");
        parameters["StrategyType"].ShouldBe("Python");

        parameters.ShouldContainKey("CodeLength");
        parameters["CodeLength"].ShouldBe(pythonCode.Length);

        parameters.ShouldContainKey("InitializeTimeout");
        parameters["InitializeTimeout"].ShouldBe("30s");

        parameters.ShouldContainKey("SignalTimeout");
        parameters["SignalTimeout"].ShouldBe("5s");
    }
}
