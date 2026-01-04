using FakeItEasy;
using Microsoft.Extensions.Logging;
using Shouldly;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Services;
using TradingStrat.Application.Tests.TestDoubles;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.ValueObjects;
using PositionSizingMode = TradingStrat.Domain.ValueObjects.PositionSizingMode;
using ValidationResult = TradingStrat.Application.Commands.ValidationResult;

namespace TradingStrat.Application.Tests.UseCases;

public class CustomStrategyManagementUseCaseTests
{
    private readonly InMemoryCustomStrategyRepository _repository;

#pragma warning disable CS0618 // Type or member is obsolete
    private readonly CustomStrategyManagementUseCase _useCase;
#pragma warning restore CS0618 // Type or member is obsolete

    public CustomStrategyManagementUseCaseTests()
    {
        _repository = new InMemoryCustomStrategyRepository();

        // Create the domain validator
        var validator = new StrategyDefinitionValidator();

        // Create the split use cases
        var queryUseCase = new CustomStrategyQueryUseCase(_repository);
        var commandUseCase = new CustomStrategyCommandUseCase(_repository, validator);

        // Create fake dependencies for Python use cases
        var fakePythonExecutor = A.Fake<IPythonExecutor>();
        var fakeHistoricalDataPort = new InMemoryHistoricalDataRepository();
        var fakeIndicatorCalculator = A.Fake<IIndicatorCalculator>();
        var fakeBacktestEngine = new BacktestEngine(fakeHistoricalDataPort, new PerformanceCalculator());
        var fakeValidateLogger = A.Fake<ILogger<ValidatePythonCodeUseCase>>();
        var fakeDryRunLogger = A.Fake<ILogger<DryRunPythonStrategyUseCase>>();

        // Create Python use cases
        var validatePythonCodeUseCase = new ValidatePythonCodeUseCase(fakePythonExecutor, fakeValidateLogger);
        var dryRunPythonStrategyUseCase = new DryRunPythonStrategyUseCase(
            fakePythonExecutor,
            fakeHistoricalDataPort,
            fakeBacktestEngine,
            fakeIndicatorCalculator,
            fakeDryRunLogger);

        // Create the facade for backward compatibility
#pragma warning disable CS0618 // Type or member is obsolete
        _useCase = new CustomStrategyManagementUseCase(
            queryUseCase,
            commandUseCase,
            validator,
            validatePythonCodeUseCase,
            dryRunPythonStrategyUseCase);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Fact]
    public async Task CreateStrategyAsync_WithValidDefinition_CreatesStrategy()
    {
        // Arrange
        CreateCustomStrategyCommand command = new(
            Name: "Test RSI Strategy",
            Description: "Buy when RSI < 30, sell when RSI > 70",
            Author: "Test User",
            Category: "Momentum",
            Definition: CreateValidRSIDefinition()
        );

        // Act
        Result<CustomStrategyResult> result = await _useCase.CreateStrategyAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Value.Id.ShouldBeGreaterThan(0);
        result.Value.Name.ShouldBe("Test RSI Strategy");
        result.Value.Author.ShouldBe("Test User");
        result.Value.Category.ShouldBe("Momentum");
        result.Value.Definition.ShouldNotBeNull();
        result.Value.Definition.EntryRules.Count.ShouldBe(1);
        result.Value.Definition.ExitRules.Count.ShouldBe(1);
        result.Value.TimesUsed.ShouldBe(0);
        result.Value.LastBacktestReturn.ShouldBeNull();

        _repository.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreateStrategyAsync_WithInvalidDefinition_ThrowsException()
    {
        // Arrange - Definition with no entry rules (invalid)
        StrategyDefinition invalidDefinition = new(
            entryRules: new List<StrategyRule>(), // Empty!
            exitRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.GreaterThan,
                    RuleValueType.Constant,
                    70,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            sizingMode: PositionSizingMode.FixedPercentage,
            sizingParameters: new Dictionary<string, decimal> { ["Percentage"] = 0.95m }
        );

        CreateCustomStrategyCommand command = new(
            "Invalid Strategy",
            "Missing entry rules",
            "Test User",
            "Test",
            invalidDefinition
        );

        // Act
        Result<CustomStrategyResult> result = await _useCase.CreateStrategyAsync(command);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == "INVALID_STRATEGY_DEFINITION");
        result.Errors.First().Message.ShouldContain("Invalid strategy definition");
        result.Errors.First().Message.ShouldContain("at least one entry rule");

        _repository.Count.ShouldBe(0);
    }

    [Fact]
    public async Task UpdateStrategyAsync_WithValidChanges_UpdatesStrategy()
    {
        // Arrange - Create a strategy first
        CreateCustomStrategyCommand createCommand = new(
            "Original Name",
            "Original Description",
            "Test User",
            "Original Category",
            CreateValidRSIDefinition()
        );

        Result<CustomStrategyResult> created = await _useCase.CreateStrategyAsync(createCommand);

        // Modify the definition
        StrategyDefinition modifiedDefinition = new(
            entryRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.LessThan,
                    RuleValueType.Constant,
                    25, // Changed from 30 to 25
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            exitRules: created.Value.Definition!.ExitRules,
            sizingMode: created.Value.Definition!.SizingMode,
            sizingParameters: created.Value.Definition!.SizingParameters
        );

        UpdateCustomStrategyCommand updateCommand = new(
            created.Value.Id,
            "Updated Name",
            "Updated Description",
            "Updated Category",
            modifiedDefinition
        );

        // Act
        Result<CustomStrategyResult> result = await _useCase.UpdateStrategyAsync(updateCommand);

        // Assert
        result.Value.Id.ShouldBe(created.Value.Id);
        result.Value.Name.ShouldBe("Updated Name");
        result.Value.Description.ShouldBe("Updated Description");
        result.Value.Category.ShouldBe("Updated Category");
        result.Value.Definition!.EntryRules[0].ConstantValue.ShouldBe(25);
        result.Value.LastUpdatedAt.ShouldBeGreaterThan(created.Value.LastUpdatedAt);
    }

    [Fact]
    public async Task UpdateStrategyAsync_WithNonExistentId_ThrowsException()
    {
        // Arrange
        UpdateCustomStrategyCommand command = new(
            999, // Non-existent ID
            "Updated Name",
            "Updated Description",
            "Category",
            CreateValidRSIDefinition()
        );

        // Act
        Result<CustomStrategyResult> result = await _useCase.UpdateStrategyAsync(command);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == "STRATEGY_NOT_FOUND");
        result.Errors.First().Message.ShouldContain("not found");
    }

    [Fact]
    public async Task DeleteStrategyAsync_RemovesStrategy()
    {
        // Arrange
        CreateCustomStrategyCommand command = new(
            "Strategy to Delete",
            "Description",
            "Test User",
            "Category",
            CreateValidRSIDefinition()
        );

        Result<CustomStrategyResult> created = await _useCase.CreateStrategyAsync(command);
        _repository.Count.ShouldBe(1);

        // Act
        await _useCase.DeleteStrategyAsync(created.Value.Id);

        // Assert
        _repository.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetStrategyByIdAsync_WithExistingId_ReturnsStrategy()
    {
        // Arrange
        CreateCustomStrategyCommand command = new(
            "Test Strategy",
            "Description",
            "Test User",
            "Category",
            CreateValidRSIDefinition()
        );

        Result<CustomStrategyResult> created = await _useCase.CreateStrategyAsync(command);

        // Act
        Result<CustomStrategyResult> result = await _useCase.GetStrategyByIdAsync(created.Value.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Value.Id.ShouldBe(created.Value.Id);
        result.Value.Name.ShouldBe("Test Strategy");
    }

    [Fact]
    public async Task GetStrategyByIdAsync_WithNonExistentId_ThrowsException()
    {
        // Act
        Result<CustomStrategyResult> result = await _useCase.GetStrategyByIdAsync(999);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == "STRATEGY_NOT_FOUND");
        result.Errors.First().Message.ShouldContain("not found");
    }

    [Fact]
    public async Task GetAllStrategiesAsync_ReturnsAllStrategies()
    {
        // Arrange - Create multiple strategies
        await _useCase.CreateStrategyAsync(new(
            "Strategy 1", "Desc 1", "User", "Momentum", CreateValidRSIDefinition()));
        await _useCase.CreateStrategyAsync(new(
            "Strategy 2", "Desc 2", "User", "Trend", CreateValidRSIDefinition()));
        await _useCase.CreateStrategyAsync(new(
            "Strategy 3", "Desc 3", "User", "Momentum", CreateValidRSIDefinition()));

        // Act
        Result<List<CustomStrategyResult>> results = await _useCase.GetAllStrategiesAsync();

        // Assert
        results.Value.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetAllStrategiesAsync_WithCategoryFilter_ReturnsFilteredStrategies()
    {
        // Arrange
        await _useCase.CreateStrategyAsync(new(
            "Strategy 1", "Desc", "User", "Momentum", CreateValidRSIDefinition()));
        await _useCase.CreateStrategyAsync(new(
            "Strategy 2", "Desc", "User", "Trend", CreateValidRSIDefinition()));
        await _useCase.CreateStrategyAsync(new(
            "Strategy 3", "Desc", "User", "Momentum", CreateValidRSIDefinition()));

        // Act
        Result<List<CustomStrategyResult>> results = await _useCase.GetAllStrategiesAsync("Momentum");

        // Assert
        results.Value.Count.ShouldBe(2);
        results.Value.ShouldAllBe(r => r.Category == "Momentum");
    }

    [Fact]
    public async Task CloneStrategyAsync_CreatesExactCopy()
    {
        // Arrange
        CreateCustomStrategyCommand command = new(
            "Original Strategy",
            "Original Description",
            "Test User",
            "Category",
            CreateValidRSIDefinition()
        );

        Result<CustomStrategyResult> originalResult = await _useCase.CreateStrategyAsync(command);

        // Act
        Result<CustomStrategyResult> cloneResult2 = await _useCase.CloneStrategyAsync(originalResult.Value.Id, "Cloned Strategy");

        // Assert
        cloneResult2.Value.Id.ShouldNotBe(originalResult.Value.Id);
        cloneResult2.Value.Name.ShouldBe("Cloned Strategy");
        cloneResult2.Value.Description.ShouldContain("Cloned from");
        cloneResult2.Value.Author.ShouldBe(originalResult.Value.Author);
        cloneResult2.Value.Category.ShouldBe(originalResult.Value.Category);
        cloneResult2.Value.Definition!.EntryRules.Count.ShouldBe(originalResult.Value.Definition!.EntryRules.Count);
        cloneResult2.Value.Definition!.ExitRules.Count.ShouldBe(originalResult.Value.Definition!.ExitRules.Count);
        cloneResult2.Value.TimesUsed.ShouldBe(0); // Clone starts fresh

        _repository.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ValidateStrategyDefinitionAsync_WithValidDefinition_ReturnsValid()
    {
        // Arrange
        StrategyDefinition definition = CreateValidRSIDefinition();

        // Act
        Result<ValidationResult> result = await _useCase.ValidateStrategyDefinitionAsync(definition);

        // Assert
        result.Value.IsValid.ShouldBeTrue();
        result.Value.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateStrategyDefinitionAsync_WithMissingEntryRules_ReturnsInvalid()
    {
        // Arrange
        StrategyDefinition definition = new(
            entryRules: new List<StrategyRule>(), // Empty
            exitRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.GreaterThan,
                    RuleValueType.Constant,
                    70,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            sizingMode: PositionSizingMode.FixedPercentage,
            sizingParameters: new Dictionary<string, decimal> { ["Percentage"] = 0.95m }
        );

        // Act
        Result<ValidationResult> result = await _useCase.ValidateStrategyDefinitionAsync(definition);

        // Assert
        result.Value.IsValid.ShouldBeFalse();
        result.Value.Errors.ShouldContain(e => e.Contains("at least one entry rule"));
    }

    [Fact]
    public async Task ValidateStrategyDefinitionAsync_WithMissingExitRules_ReturnsInvalid()
    {
        // Arrange
        StrategyDefinition definition = new(
            entryRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.LessThan,
                    RuleValueType.Constant,
                    30,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            exitRules: new List<StrategyRule>(), // Empty
            sizingMode: PositionSizingMode.FixedPercentage,
            sizingParameters: new Dictionary<string, decimal> { ["Percentage"] = 0.95m }
        );

        // Act
        Result<ValidationResult> result = await _useCase.ValidateStrategyDefinitionAsync(definition);

        // Assert
        result.Value.IsValid.ShouldBeFalse();
        result.Value.Errors.ShouldContain(e => e.Contains("at least one exit rule"));
    }

    [Fact]
    public async Task ValidateStrategyDefinitionAsync_WithMissingConstantValue_ReturnsInvalid()
    {
        // Arrange - Rule with constant comparison but no value
        StrategyDefinition definition = new(
            entryRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.LessThan,
                    RuleValueType.Constant,
                    null, // Missing!
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            exitRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.GreaterThan,
                    RuleValueType.Constant,
                    70,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            sizingMode: PositionSizingMode.FixedPercentage,
            sizingParameters: new Dictionary<string, decimal> { ["Percentage"] = 0.95m }
        );

        // Act
        Result<ValidationResult> result = await _useCase.ValidateStrategyDefinitionAsync(definition);

        // Assert
        result.Value.IsValid.ShouldBeFalse();
        result.Value.Errors.ShouldContain(e => e.Contains("ConstantValue"));
    }

    [Fact]
    public async Task ValidateStrategyDefinitionAsync_WithInvalidPositionSizing_ReturnsInvalid()
    {
        // Arrange - Missing required parameter for sizing mode
        StrategyDefinition definition = new(
            entryRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.LessThan,
                    RuleValueType.Constant,
                    30,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            exitRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.GreaterThan,
                    RuleValueType.Constant,
                    70,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            sizingMode: PositionSizingMode.FixedPercentage,
            sizingParameters: new Dictionary<string, decimal>() // Missing "Percentage"!
        );

        // Act
        Result<ValidationResult> result = await _useCase.ValidateStrategyDefinitionAsync(definition);

        // Assert
        result.Value.IsValid.ShouldBeFalse();
        result.Value.Errors.ShouldContain(e => e.Contains("Percentage"));
    }

    #region Helper Methods

    private StrategyDefinition CreateValidRSIDefinition()
    {
        return new StrategyDefinition(
            entryRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.LessThan,
                    RuleValueType.Constant,
                    30,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            exitRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.GreaterThan,
                    RuleValueType.Constant,
                    70,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            sizingMode: PositionSizingMode.FixedPercentage,
            sizingParameters: new Dictionary<string, decimal> { ["Percentage"] = 0.95m }
        );
    }

    #endregion
}
