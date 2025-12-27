using System.Text.Json;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.ValueObjects;
using DomainValidationResult = TradingStrat.Domain.Services.ValidationResult;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Command use case for managing custom trading strategies.
/// Handles write operations (Create, Update, Delete, Clone) with validation.
/// Part of CQRS-lite separation from query operations.
/// Uses StrategyDefinitionValidator domain service for business rule validation.
/// </summary>
public class CustomStrategyCommandUseCase : ICustomStrategyCommandUseCase
{
    private readonly ICustomStrategyPort _customStrategyPort;
    private readonly StrategyDefinitionValidator _validator;

    public CustomStrategyCommandUseCase(
        ICustomStrategyPort customStrategyPort,
        StrategyDefinitionValidator validator)
    {
        _customStrategyPort = customStrategyPort;
        _validator = validator;
    }

    public async Task<Result<CustomStrategyResult>> CreateStrategyAsync(CreateCustomStrategyCommand command)
    {
        try
        {
            // Validate definition using domain service
            DomainValidationResult validation = _validator.Validate(command.Definition);
            if (!validation.IsValid)
            {
                return Result<CustomStrategyResult>.Failure(
                    Error.Validation(
                        $"Invalid strategy definition: {string.Join(", ", validation.Errors)}",
                        "INVALID_STRATEGY_DEFINITION"));
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
            return Result<CustomStrategyResult>.Success(MapToResult(created));
        }
        catch (Exception ex)
        {
            return Result<CustomStrategyResult>.Failure(
                Error.BusinessRule($"Failed to create strategy: {ex.Message}", "STRATEGY_CREATE_FAILED"));
        }
    }

    public async Task<Result<CustomStrategyResult>> UpdateStrategyAsync(UpdateCustomStrategyCommand command)
    {
        try
        {
            // Validate definition using domain service
            DomainValidationResult validation = _validator.Validate(command.Definition);
            if (!validation.IsValid)
            {
                return Result<CustomStrategyResult>.Failure(
                    Error.Validation(
                        $"Invalid strategy definition: {string.Join(", ", validation.Errors)}",
                        "INVALID_STRATEGY_DEFINITION"));
            }

            // Load existing strategy
            CustomStrategy? existing = await _customStrategyPort.GetByIdAsync(command.Id);
            if (existing == null)
            {
                return Result<CustomStrategyResult>.Failure(
                    Error.NotFound($"Strategy with ID {command.Id} not found", "STRATEGY_NOT_FOUND"));
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
            return Result<CustomStrategyResult>.Success(MapToResult(updated));
        }
        catch (Exception ex)
        {
            return Result<CustomStrategyResult>.Failure(
                Error.BusinessRule($"Failed to update strategy: {ex.Message}", "STRATEGY_UPDATE_FAILED"));
        }
    }

    public async Task<Result<bool>> DeleteStrategyAsync(int strategyId)
    {
        try
        {
            await _customStrategyPort.DeleteAsync(strategyId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(
                Error.BusinessRule($"Failed to delete strategy: {ex.Message}", "STRATEGY_DELETE_FAILED"));
        }
    }

    public async Task<Result<CustomStrategyResult>> CloneStrategyAsync(int strategyId, string newName)
    {
        try
        {
            // Load original strategy
            CustomStrategy? original = await _customStrategyPort.GetByIdAsync(strategyId);
            if (original == null)
            {
                return Result<CustomStrategyResult>.Failure(
                    Error.NotFound($"Strategy with ID {strategyId} not found", "STRATEGY_NOT_FOUND"));
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
            return Result<CustomStrategyResult>.Success(MapToResult(created));
        }
        catch (Exception ex)
        {
            return Result<CustomStrategyResult>.Failure(
                Error.BusinessRule($"Failed to clone strategy: {ex.Message}", "STRATEGY_CLONE_FAILED"));
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
