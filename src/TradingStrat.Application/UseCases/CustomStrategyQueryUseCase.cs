using System.Text.Json;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Common;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Query use case for retrieving custom trading strategies.
/// Handles read-only operations with zero side effects.
/// Part of CQRS-lite separation from command operations.
/// Uses helper method pattern to eliminate try-catch boilerplate.
/// </summary>
public class CustomStrategyQueryUseCase : ICustomStrategyQueryUseCase
{
    private readonly ICustomStrategyPort _customStrategyPort;

    public CustomStrategyQueryUseCase(ICustomStrategyPort customStrategyPort)
    {
        _customStrategyPort = customStrategyPort;
    }

    public Task<Result<CustomStrategyResult>> GetStrategyByIdAsync(int strategyId)
        => ExecuteWithErrorHandling(() => GetStrategyByIdCoreAsync(strategyId), ErrorCodes.Strategy.RetrievalFailed);

    public Task<Result<List<CustomStrategyResult>>> GetAllStrategiesAsync(string? category = null)
        => ExecuteWithErrorHandling(() => GetAllStrategiesCoreAsync(category), ErrorCodes.Strategy.RetrievalFailed);

    private static async Task<Result<T>> ExecuteWithErrorHandling<T>(
        Func<Task<Result<T>>> executeCore,
        string errorCode)
    {
        try
        {
            return await executeCore();
        }
        catch (InvalidOperationException ex)
        {
            return Result<T>.Failure(
                Error.BusinessRule($"Failed: {ex.Message}", $"{errorCode}_FAILED"));
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(
                Error.BusinessRule($"Failed: {ex.Message}", $"{errorCode}_FAILED"));
        }
    }

    private async Task<Result<CustomStrategyResult>> GetStrategyByIdCoreAsync(int strategyId)
    {
        CustomStrategy? strategy = await _customStrategyPort.GetByIdAsync(strategyId);
        if (strategy == null)
        {
            return Result<CustomStrategyResult>.Failure(
                Error.NotFound($"Strategy with ID {strategyId} not found", ErrorCodes.Strategy.NotFound));
        }

        return Result<CustomStrategyResult>.Success(MapToResult(strategy));
    }

    private async Task<Result<List<CustomStrategyResult>>> GetAllStrategiesCoreAsync(string? category)
    {
        List<CustomStrategy> strategies = await _customStrategyPort.GetAllAsync(category);
        return Result<List<CustomStrategyResult>>.Success(
            strategies.Select(MapToResult).ToList());
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
