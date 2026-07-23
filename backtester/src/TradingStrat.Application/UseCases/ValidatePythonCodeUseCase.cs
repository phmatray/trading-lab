using Microsoft.Extensions.Logging;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Common;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Services;
using AppPythonValidationResult = TradingStrat.Application.Commands.PythonValidationResult;
using DomainPythonValidationResult = TradingStrat.Domain.Services.PythonValidationResult;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for validating Python strategy code without persisting it.
/// Checks syntax, required functions (generate_signal), and optional functions (initialize).
/// </summary>
public class ValidatePythonCodeUseCase
{
    private readonly IPythonExecutor _pythonExecutor;
    private readonly ILogger<ValidatePythonCodeUseCase> _logger;

    public ValidatePythonCodeUseCase(
        IPythonExecutor pythonExecutor,
        ILogger<ValidatePythonCodeUseCase> logger)
    {
        _pythonExecutor = pythonExecutor;
        _logger = logger;
    }

    public async Task<Result<AppPythonValidationResult>> ExecuteAsync(ValidatePythonCodeCommand command)
    {
        try
        {
            _logger.LogInformation("Validating Python code ({Length} characters)", command.PythonCode.Length);

            // Delegate to Python executor for syntax validation
            DomainPythonValidationResult domainResult = await _pythonExecutor.ValidateSyntaxAsync(command.PythonCode);

            // Convert domain result to application result
            AppPythonValidationResult appResult = new(
                domainResult.IsValid,
                domainResult.Errors
            );

            _logger.LogInformation("Python validation completed: {IsValid}", appResult.IsValid);

            return Result<AppPythonValidationResult>.Success(appResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Python code");
            return Result<AppPythonValidationResult>.Failure(
                Error.BusinessRule($"Python validation failed: {ex.Message}", ErrorCodes.Strategy.ValidationFailed));
        }
    }
}
