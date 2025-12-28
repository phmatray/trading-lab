using TradingStrat.Application.Commands;
using TradingStrat.Application.Common;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.ValueObjects;
using AppValidationResult = TradingStrat.Application.Commands.ValidationResult;
using DomainValidationResult = TradingStrat.Domain.Services.ValidationResult;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Legacy use case for managing custom trading strategies.
/// NOW A FACADE: Delegates to CustomStrategyQueryUseCase and CustomStrategyCommandUseCase.
/// Maintained for backward compatibility with existing Web layer code.
/// Prefer using ICustomStrategyQueryUseCase and ICustomStrategyCommandUseCase directly.
/// </summary>
[Obsolete("Split into CustomStrategyQueryUseCase and CustomStrategyCommandUseCase for better separation of concerns. " +
          "Use ICustomStrategyQueryUseCase for read operations and ICustomStrategyCommandUseCase for write operations.")]
public class CustomStrategyManagementUseCase : ICustomStrategyManagementUseCase
{
    private readonly ICustomStrategyQueryUseCase _queryUseCase;
    private readonly ICustomStrategyCommandUseCase _commandUseCase;
    private readonly StrategyDefinitionValidator _validator;

    public CustomStrategyManagementUseCase(
        ICustomStrategyQueryUseCase queryUseCase,
        ICustomStrategyCommandUseCase commandUseCase,
        StrategyDefinitionValidator validator)
    {
        _queryUseCase = queryUseCase;
        _commandUseCase = commandUseCase;
        _validator = validator;
    }

    // Query operations - delegate to query use case

    public Task<Result<CustomStrategyResult>> GetStrategyByIdAsync(int strategyId)
        => _queryUseCase.GetStrategyByIdAsync(strategyId);

    public Task<Result<List<CustomStrategyResult>>> GetAllStrategiesAsync(string? category = null)
        => _queryUseCase.GetAllStrategiesAsync(category);

    // Command operations - delegate to command use case

    public Task<Result<CustomStrategyResult>> CreateStrategyAsync(CreateCustomStrategyCommand command)
        => _commandUseCase.CreateStrategyAsync(command);

    public Task<Result<CustomStrategyResult>> UpdateStrategyAsync(UpdateCustomStrategyCommand command)
        => _commandUseCase.UpdateStrategyAsync(command);

    public Task<Result<bool>> DeleteStrategyAsync(int strategyId)
        => _commandUseCase.DeleteStrategyAsync(strategyId);

    public Task<Result<CustomStrategyResult>> CloneStrategyAsync(int strategyId, string newName)
        => _commandUseCase.CloneStrategyAsync(strategyId, newName);

    // Validation operation - delegate to domain service

    public Task<Result<AppValidationResult>> ValidateStrategyDefinitionAsync(StrategyDefinition definition)
    {
        try
        {
            DomainValidationResult validation = _validator.Validate(definition);

            // Convert Domain.Services.ValidationResult to Application.Commands.ValidationResult
            AppValidationResult result = new AppValidationResult(
                validation.IsValid,
                validation.Errors
            );

            return Task.FromResult(Result<AppValidationResult>.Success(result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<AppValidationResult>.Failure(
                Error.BusinessRule($"Failed to validate strategy definition: {ex.Message}", ErrorCodes.Strategy.ValidationFailed)));
        }
    }
}
