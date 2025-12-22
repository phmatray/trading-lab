using Shouldly;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Tests.TestDoubles;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Tests.UseCases;

public class CustomStrategyManagementUseCaseTests
{
    private readonly InMemoryCustomStrategyRepository _repository;
    private readonly CustomStrategyManagementUseCase _useCase;

    public CustomStrategyManagementUseCaseTests()
    {
        _repository = new InMemoryCustomStrategyRepository();
        _useCase = new CustomStrategyManagementUseCase(_repository);
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
        CustomStrategyResult result = await _useCase.CreateStrategyAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBeGreaterThan(0);
        result.Name.ShouldBe("Test RSI Strategy");
        result.Author.ShouldBe("Test User");
        result.Category.ShouldBe("Momentum");
        result.Definition.ShouldNotBeNull();
        result.Definition.EntryRules.Count.ShouldBe(1);
        result.Definition.ExitRules.Count.ShouldBe(1);
        result.TimesUsed.ShouldBe(0);
        result.LastBacktestReturn.ShouldBeNull();

        _repository.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreateStrategyAsync_WithInvalidDefinition_ThrowsException()
    {
        // Arrange - Definition with no entry rules (invalid)
        StrategyDefinition invalidDefinition = new(
            EntryRules: new List<StrategyRule>(), // Empty!
            ExitRules: new List<StrategyRule>
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
            SizingMode: PositionSizingMode.FixedPercentage,
            SizingParameters: new Dictionary<string, decimal> { ["Percentage"] = 0.95m }
        );

        CreateCustomStrategyCommand command = new(
            "Invalid Strategy",
            "Missing entry rules",
            "Test User",
            "Test",
            invalidDefinition
        );

        // Act & Assert
        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _useCase.CreateStrategyAsync(command));

        ex.Message.ShouldContain("Invalid strategy definition");
        ex.Message.ShouldContain("at least one entry rule");

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

        CustomStrategyResult created = await _useCase.CreateStrategyAsync(createCommand);

        // Modify the definition
        StrategyDefinition modifiedDefinition = new(
            EntryRules: new List<StrategyRule>
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
            ExitRules: created.Definition.ExitRules,
            SizingMode: created.Definition.SizingMode,
            SizingParameters: created.Definition.SizingParameters
        );

        UpdateCustomStrategyCommand updateCommand = new(
            created.Id,
            "Updated Name",
            "Updated Description",
            "Updated Category",
            modifiedDefinition
        );

        // Act
        CustomStrategyResult result = await _useCase.UpdateStrategyAsync(updateCommand);

        // Assert
        result.Id.ShouldBe(created.Id);
        result.Name.ShouldBe("Updated Name");
        result.Description.ShouldBe("Updated Description");
        result.Category.ShouldBe("Updated Category");
        result.Definition.EntryRules[0].ConstantValue.ShouldBe(25);
        result.LastUpdatedAt.ShouldBeGreaterThan(created.LastUpdatedAt);
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

        // Act & Assert
        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _useCase.UpdateStrategyAsync(command));

        ex.Message.ShouldContain("not found");
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

        CustomStrategyResult created = await _useCase.CreateStrategyAsync(command);
        _repository.Count.ShouldBe(1);

        // Act
        await _useCase.DeleteStrategyAsync(created.Id);

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

        CustomStrategyResult created = await _useCase.CreateStrategyAsync(command);

        // Act
        CustomStrategyResult result = await _useCase.GetStrategyByIdAsync(created.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(created.Id);
        result.Name.ShouldBe("Test Strategy");
    }

    [Fact]
    public async Task GetStrategyByIdAsync_WithNonExistentId_ThrowsException()
    {
        // Act & Assert
        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _useCase.GetStrategyByIdAsync(999));

        ex.Message.ShouldContain("not found");
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
        List<CustomStrategyResult> results = await _useCase.GetAllStrategiesAsync();

        // Assert
        results.Count.ShouldBe(3);
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
        List<CustomStrategyResult> results = await _useCase.GetAllStrategiesAsync("Momentum");

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldAllBe(r => r.Category == "Momentum");
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

        CustomStrategyResult original = await _useCase.CreateStrategyAsync(command);

        // Act
        CustomStrategyResult clone = await _useCase.CloneStrategyAsync(original.Id, "Cloned Strategy");

        // Assert
        clone.Id.ShouldNotBe(original.Id);
        clone.Name.ShouldBe("Cloned Strategy");
        clone.Description.ShouldContain("Cloned from");
        clone.Author.ShouldBe(original.Author);
        clone.Category.ShouldBe(original.Category);
        clone.Definition.EntryRules.Count.ShouldBe(original.Definition.EntryRules.Count);
        clone.Definition.ExitRules.Count.ShouldBe(original.Definition.ExitRules.Count);
        clone.TimesUsed.ShouldBe(0); // Clone starts fresh

        _repository.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ValidateStrategyDefinitionAsync_WithValidDefinition_ReturnsValid()
    {
        // Arrange
        StrategyDefinition definition = CreateValidRSIDefinition();

        // Act
        ValidationResult result = await _useCase.ValidateStrategyDefinitionAsync(definition);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateStrategyDefinitionAsync_WithMissingEntryRules_ReturnsInvalid()
    {
        // Arrange
        StrategyDefinition definition = new(
            EntryRules: new List<StrategyRule>(), // Empty
            ExitRules: new List<StrategyRule>
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
            SizingMode: PositionSizingMode.FixedPercentage,
            SizingParameters: new Dictionary<string, decimal> { ["Percentage"] = 0.95m }
        );

        // Act
        ValidationResult result = await _useCase.ValidateStrategyDefinitionAsync(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("at least one entry rule"));
    }

    [Fact]
    public async Task ValidateStrategyDefinitionAsync_WithMissingExitRules_ReturnsInvalid()
    {
        // Arrange
        StrategyDefinition definition = new(
            EntryRules: new List<StrategyRule>
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
            ExitRules: new List<StrategyRule>(), // Empty
            SizingMode: PositionSizingMode.FixedPercentage,
            SizingParameters: new Dictionary<string, decimal> { ["Percentage"] = 0.95m }
        );

        // Act
        ValidationResult result = await _useCase.ValidateStrategyDefinitionAsync(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("at least one exit rule"));
    }

    [Fact]
    public async Task ValidateStrategyDefinitionAsync_WithMissingConstantValue_ReturnsInvalid()
    {
        // Arrange - Rule with constant comparison but no value
        StrategyDefinition definition = new(
            EntryRules: new List<StrategyRule>
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
            ExitRules: new List<StrategyRule>
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
            SizingMode: PositionSizingMode.FixedPercentage,
            SizingParameters: new Dictionary<string, decimal> { ["Percentage"] = 0.95m }
        );

        // Act
        ValidationResult result = await _useCase.ValidateStrategyDefinitionAsync(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("ConstantValue"));
    }

    [Fact]
    public async Task ValidateStrategyDefinitionAsync_WithInvalidPositionSizing_ReturnsInvalid()
    {
        // Arrange - Missing required parameter for sizing mode
        StrategyDefinition definition = new(
            EntryRules: new List<StrategyRule>
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
            ExitRules: new List<StrategyRule>
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
            SizingMode: PositionSizingMode.FixedPercentage,
            SizingParameters: new Dictionary<string, decimal>() // Missing "Percentage"!
        );

        // Act
        ValidationResult result = await _useCase.ValidateStrategyDefinitionAsync(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("Percentage"));
    }

    #region Helper Methods

    private StrategyDefinition CreateValidRSIDefinition()
    {
        return new StrategyDefinition(
            EntryRules: new List<StrategyRule>
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
            ExitRules: new List<StrategyRule>
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
            SizingMode: PositionSizingMode.FixedPercentage,
            SizingParameters: new Dictionary<string, decimal> { ["Percentage"] = 0.95m }
        );
    }

    #endregion
}
