using System.Text.Json;
using TradingStrat.Application.Commands;
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
/// </summary>
public class CustomStrategyQueryUseCase : ICustomStrategyQueryUseCase
{
    private readonly ICustomStrategyPort _customStrategyPort;

    public CustomStrategyQueryUseCase(ICustomStrategyPort customStrategyPort)
    {
        _customStrategyPort = customStrategyPort;
    }

    public async Task<Result<CustomStrategyResult>> GetStrategyByIdAsync(int strategyId)
    {
        try
        {
            CustomStrategy? strategy = await _customStrategyPort.GetByIdAsync(strategyId);
            if (strategy == null)
            {
                return Result<CustomStrategyResult>.Failure(
                    Error.NotFound($"Strategy with ID {strategyId} not found", "STRATEGY_NOT_FOUND"));
            }

            return Result<CustomStrategyResult>.Success(MapToResult(strategy));
        }
        catch (Exception ex)
        {
            return Result<CustomStrategyResult>.Failure(
                Error.BusinessRule($"Failed to retrieve strategy: {ex.Message}", "STRATEGY_RETRIEVAL_FAILED"));
        }
    }

    public async Task<Result<List<CustomStrategyResult>>> GetAllStrategiesAsync(string? category = null)
    {
        try
        {
            List<CustomStrategy> strategies = await _customStrategyPort.GetAllAsync(category);
            return Result<List<CustomStrategyResult>>.Success(
                strategies.Select(MapToResult).ToList());
        }
        catch (Exception ex)
        {
            return Result<List<CustomStrategyResult>>.Failure(
                Error.BusinessRule($"Failed to retrieve strategies: {ex.Message}", "STRATEGIES_RETRIEVAL_FAILED"));
        }
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
