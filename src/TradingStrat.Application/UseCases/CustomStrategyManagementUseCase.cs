using System.Text.Json;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for managing custom trading strategies.
/// Orchestrates CRUD operations, validation, and serialization of strategy definitions.
/// </summary>
public class CustomStrategyManagementUseCase : ICustomStrategyManagementUseCase
{
    private readonly ICustomStrategyPort _customStrategyPort;

    public CustomStrategyManagementUseCase(ICustomStrategyPort customStrategyPort)
    {
        _customStrategyPort = customStrategyPort;
    }

    public async Task<CustomStrategyResult> CreateStrategyAsync(CreateCustomStrategyCommand command)
    {
        // Validate definition before persisting
        ValidationResult validation = await ValidateStrategyDefinitionAsync(command.Definition);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                $"Invalid strategy definition: {string.Join(", ", validation.Errors)}");
        }

        // Create entity
        CustomStrategy strategy = new()
        {
            Name = command.Name,
            Description = command.Description,
            Author = command.Author,
            Category = command.Category,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            DefinitionJson = SerializeDefinition(command.Definition)
        };

        // Persist
        CustomStrategy created = await _customStrategyPort.CreateAsync(strategy);

        // Map to result
        return MapToResult(created);
    }

    public async Task<CustomStrategyResult> UpdateStrategyAsync(UpdateCustomStrategyCommand command)
    {
        // Validate definition
        ValidationResult validation = await ValidateStrategyDefinitionAsync(command.Definition);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                $"Invalid strategy definition: {string.Join(", ", validation.Errors)}");
        }

        // Load existing strategy
        CustomStrategy? existing = await _customStrategyPort.GetByIdAsync(command.Id);
        if (existing == null)
        {
            throw new InvalidOperationException($"Strategy with ID {command.Id} not found");
        }

        // Update fields
        existing.Name = command.Name;
        existing.Description = command.Description;
        existing.Category = command.Category;
        existing.DefinitionJson = SerializeDefinition(command.Definition);
        existing.LastUpdatedAt = DateTime.UtcNow;

        // Persist
        CustomStrategy updated = await _customStrategyPort.UpdateAsync(existing);

        // Map to result
        return MapToResult(updated);
    }

    public async Task DeleteStrategyAsync(int strategyId)
    {
        await _customStrategyPort.DeleteAsync(strategyId);
    }

    public async Task<CustomStrategyResult> GetStrategyByIdAsync(int strategyId)
    {
        CustomStrategy? strategy = await _customStrategyPort.GetByIdAsync(strategyId);
        if (strategy == null)
        {
            throw new InvalidOperationException($"Strategy with ID {strategyId} not found");
        }

        return MapToResult(strategy);
    }

    public async Task<List<CustomStrategyResult>> GetAllStrategiesAsync(string? category = null)
    {
        List<CustomStrategy> strategies = await _customStrategyPort.GetAllAsync(category);
        return strategies.Select(MapToResult).ToList();
    }

    public async Task<CustomStrategyResult> CloneStrategyAsync(int strategyId, string newName)
    {
        // Load original strategy
        CustomStrategy? original = await _customStrategyPort.GetByIdAsync(strategyId);
        if (original == null)
        {
            throw new InvalidOperationException($"Strategy with ID {strategyId} not found");
        }

        // Create clone with new name
        CustomStrategy clone = new()
        {
            Name = newName,
            Description = $"Cloned from {original.Name}",
            Author = original.Author,
            Category = original.Category,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            DefinitionJson = original.DefinitionJson // Same definition
        };

        CustomStrategy created = await _customStrategyPort.CreateAsync(clone);
        return MapToResult(created);
    }

    public Task<ValidationResult> ValidateStrategyDefinitionAsync(StrategyDefinition definition)
    {
        var errors = new List<string>();

        // Validate entry rules
        if (definition.EntryRules.Count == 0)
        {
            errors.Add("Strategy must have at least one entry rule");
        }

        // Validate exit rules
        if (definition.ExitRules.Count == 0)
        {
            errors.Add("Strategy must have at least one exit rule");
        }

        // Validate each entry rule
        foreach (StrategyRule rule in definition.EntryRules)
        {
            ValidateRule(rule, errors);
        }

        // Validate each exit rule
        foreach (StrategyRule rule in definition.ExitRules)
        {
            ValidateRule(rule, errors);
        }

        // Validate position sizing
        ValidatePositionSizing(definition, errors);

        return Task.FromResult(new ValidationResult(errors.Count == 0, errors));
    }

    private void ValidateRule(StrategyRule rule, List<string> errors)
    {
        // Validate indicator name
        if (string.IsNullOrWhiteSpace(rule.IndicatorName))
        {
            errors.Add("Rule must specify an indicator name");
        }

        // Validate constant comparisons
        if (rule.ValueType == RuleValueType.Constant && rule.ConstantValue == null)
        {
            errors.Add($"Rule with constant comparison must provide ConstantValue (Indicator: {rule.IndicatorName})");
        }

        // Validate indicator comparisons
        if (rule.ValueType == RuleValueType.Indicator)
        {
            if (string.IsNullOrWhiteSpace(rule.SecondIndicatorName))
            {
                errors.Add($"Rule comparing two indicators must provide SecondIndicatorName (Indicator: {rule.IndicatorName})");
            }

            if (rule.SecondIndicatorParameters == null || rule.SecondIndicatorParameters.Count == 0)
            {
                errors.Add($"Rule comparing two indicators must provide SecondIndicatorParameters (Indicator: {rule.IndicatorName})");
            }
        }

        // Validate indicator parameters
        if (rule.IndicatorParameters == null || rule.IndicatorParameters.Count == 0)
        {
            errors.Add($"Rule must provide IndicatorParameters (Indicator: {rule.IndicatorName})");
        }
    }

    private void ValidatePositionSizing(StrategyDefinition definition, List<string> errors)
    {
        switch (definition.SizingMode)
        {
            case PositionSizingMode.FixedPercentage:
                if (!definition.SizingParameters.ContainsKey("Percentage"))
                {
                    errors.Add("FixedPercentage sizing mode requires 'Percentage' parameter");
                }
                else
                {
                    decimal percentage = definition.SizingParameters["Percentage"];
                    if (percentage <= 0 || percentage > 1)
                    {
                        errors.Add("Percentage must be between 0 and 1 (e.g., 0.95 for 95%)");
                    }
                }
                break;

            case PositionSizingMode.FixedQuantity:
                if (!definition.SizingParameters.ContainsKey("Quantity"))
                {
                    errors.Add("FixedQuantity sizing mode requires 'Quantity' parameter");
                }
                else
                {
                    decimal quantity = definition.SizingParameters["Quantity"];
                    if (quantity <= 0)
                    {
                        errors.Add("Quantity must be greater than 0");
                    }
                }
                break;

            case PositionSizingMode.RiskBased:
                if (!definition.SizingParameters.ContainsKey("RiskPercentage"))
                {
                    errors.Add("RiskBased sizing mode requires 'RiskPercentage' parameter");
                }
                else
                {
                    decimal riskPercentage = definition.SizingParameters["RiskPercentage"];
                    if (riskPercentage <= 0 || riskPercentage > 0.1m)
                    {
                        errors.Add("RiskPercentage must be between 0 and 0.1 (e.g., 0.02 for 2%)");
                    }
                }
                break;
        }
    }

    private string SerializeDefinition(StrategyDefinition definition)
    {
        return JsonSerializer.Serialize(definition, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private StrategyDefinition DeserializeDefinition(string json)
    {
        return JsonSerializer.Deserialize<StrategyDefinition>(json)
            ?? throw new InvalidOperationException("Failed to deserialize strategy definition");
    }

    private CustomStrategyResult MapToResult(CustomStrategy strategy)
    {
        StrategyDefinition definition = DeserializeDefinition(strategy.DefinitionJson);

        return new CustomStrategyResult(
            strategy.Id,
            strategy.Name,
            strategy.Description,
            strategy.Author,
            strategy.Category,
            strategy.CreatedAt,
            strategy.LastUpdatedAt,
            definition,
            strategy.TimesUsed,
            strategy.LastBacktestReturn,
            strategy.LastBacktestDate
        );
    }
}
