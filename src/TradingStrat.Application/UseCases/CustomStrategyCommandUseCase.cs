using System.Text.Json;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Common;
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
/// Uses helper method pattern to eliminate try-catch boilerplate.
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

    public Task<Result<CustomStrategyResult>> CreateStrategyAsync(CreateCustomStrategyCommand command)
        => ExecuteWithErrorHandling(() => CreateStrategyCoreAsync(command), ErrorCodes.Strategy.CreateFailed);

    public Task<Result<CustomStrategyResult>> UpdateStrategyAsync(UpdateCustomStrategyCommand command)
        => ExecuteWithErrorHandling(() => UpdateStrategyCoreAsync(command), ErrorCodes.Strategy.UpdateFailed);

    public Task<Result<bool>> DeleteStrategyAsync(int strategyId)
        => ExecuteWithErrorHandling(() => DeleteStrategyCoreAsync(strategyId), ErrorCodes.Strategy.DeleteFailed);

    public Task<Result<CustomStrategyResult>> CloneStrategyAsync(int strategyId, string newName)
        => ExecuteWithErrorHandling(() => CloneStrategyCoreAsync(strategyId, newName), ErrorCodes.Strategy.CloneFailed);

    private static async Task<Result<T>> ExecuteWithErrorHandling<T>(
        Func<Task<Result<T>>> executeCore,
        string errorCode)
    {
        try
        {
            return await executeCore();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Result<T>.Failure(
                Error.NotFound(ex.Message, ErrorCodes.Strategy.NotFound));
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(
                Error.BusinessRule($"Failed: {ex.Message}", $"{errorCode}_FAILED"));
        }
    }

    private async Task<Result<CustomStrategyResult>> CreateStrategyCoreAsync(CreateCustomStrategyCommand command)
    {
        // Validate definition for RuleBased strategies only
        if (command.StrategyType == CustomStrategyType.RuleBased && command.Definition != null)
        {
            DomainValidationResult validation = _validator.Validate(command.Definition);
            if (!validation.IsValid)
            {
                return Result<CustomStrategyResult>.Failure(
                    Error.Validation(
                        $"Invalid strategy definition: {string.Join(", ", validation.Errors)}",
                        ErrorCodes.Strategy.InvalidDefinition));
            }
        }

        // Create entity
        CustomStrategy strategy = new()
        {
            Name = command.Name,
            Description = command.Description,
            Author = command.Author,
            Category = command.Category,
            StrategyType = command.StrategyType,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            DefinitionJson = command.Definition != null ? SerializeDefinition(command.Definition) : string.Empty,
            PythonCode = command.PythonCode,
            PythonCodeVersion = command.PythonCode != null ? 1 : null
        };

        // Persist
        CustomStrategy created = await _customStrategyPort.CreateAsync(strategy);

        // Map to result
        return Result<CustomStrategyResult>.Success(MapToResult(created));
    }

    private async Task<Result<CustomStrategyResult>> UpdateStrategyCoreAsync(UpdateCustomStrategyCommand command)
    {
        // Validate definition for RuleBased strategies only
        if (command.StrategyType == CustomStrategyType.RuleBased && command.Definition != null)
        {
            DomainValidationResult validation = _validator.Validate(command.Definition);
            if (!validation.IsValid)
            {
                return Result<CustomStrategyResult>.Failure(
                    Error.Validation(
                        $"Invalid strategy definition: {string.Join(", ", validation.Errors)}",
                        ErrorCodes.Strategy.InvalidDefinition));
            }
        }

        // Load existing strategy
        CustomStrategy? existing = await _customStrategyPort.GetByIdAsync(command.Id);
        if (existing is null)
        {
            return Result<CustomStrategyResult>.Failure(
                Error.NotFound($"Strategy with ID {command.Id} not found", ErrorCodes.Strategy.NotFound));
        }

        // Update fields
        existing.Name = command.Name;
        existing.Description = command.Description;
        existing.Category = command.Category;
        existing.StrategyType = command.StrategyType;
        existing.DefinitionJson = command.Definition != null ? SerializeDefinition(command.Definition) : string.Empty;
        existing.PythonCode = command.PythonCode;
        existing.PythonCodeVersion = command.PythonCode != null ? (existing.PythonCodeVersion ?? 0) + 1 : null;
        existing.LastUpdatedAt = DateTime.UtcNow;

        // Persist
        CustomStrategy updated = await _customStrategyPort.UpdateAsync(existing);

        // Map to result
        return Result<CustomStrategyResult>.Success(MapToResult(updated));
    }

    private async Task<Result<bool>> DeleteStrategyCoreAsync(int strategyId)
    {
        await _customStrategyPort.DeleteAsync(strategyId);
        return Result<bool>.Success(true);
    }

    private async Task<Result<CustomStrategyResult>> CloneStrategyCoreAsync(int strategyId, string newName)
    {
        // Load original strategy
        CustomStrategy? original = await _customStrategyPort.GetByIdAsync(strategyId);
        if (original is null)
        {
            return Result<CustomStrategyResult>.Failure(
                Error.NotFound($"Strategy with ID {strategyId} not found", ErrorCodes.Strategy.NotFound));
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
        // Deserialize definition only for RuleBased strategies
        StrategyDefinition? definition = strategy.StrategyType == CustomStrategyType.RuleBased
            ? DeserializeDefinition(strategy.DefinitionJson)
            : null;

        return new CustomStrategyResult(
            strategy.Id,
            strategy.Name,
            strategy.Description,
            strategy.Author,
            strategy.Category,
            strategy.CreatedAt,
            strategy.LastUpdatedAt,
            strategy.StrategyType,
            definition,
            strategy.PythonCode,
            strategy.PythonCodeVersion,
            strategy.TimesUsed,
            strategy.LastBacktestReturn,
            strategy.LastBacktestDate
        );
    }
}
