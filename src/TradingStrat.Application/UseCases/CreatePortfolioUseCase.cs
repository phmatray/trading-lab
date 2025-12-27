using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for creating a new portfolio.
/// </summary>
public class CreatePortfolioUseCase : ICreatePortfolioUseCase
{
    private readonly IPortfolioPort _portfolioPort;

    public CreatePortfolioUseCase(IPortfolioPort portfolioPort)
    {
        _portfolioPort = portfolioPort ?? throw new ArgumentNullException(nameof(portfolioPort));
    }

    /// <inheritdoc />
    public async Task<Result<CreatePortfolioResult>> ExecuteAsync(CreatePortfolioCommand command)
    {
        // Command validation happens in constructor - command is guaranteed to be valid here

        try
        {
            // Create portfolio via repository
            var portfolio = await _portfolioPort.CreatePortfolioAsync(
                command.Name,
                command.Description,
                command.InitialCash);

            // Return successful result
            var result = new CreatePortfolioResult(
                portfolio.Id,
                portfolio.Name,
                portfolio.Cash,
                portfolio.CreatedAt);

            return Result<CreatePortfolioResult>.Success(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            // Portfolio with same name already exists
            return Result<CreatePortfolioResult>.Failure(
                Error.Conflict($"Portfolio with name '{command.Name}' already exists", "PORTFOLIO_NAME_CONFLICT"));
        }
        catch (Exception ex)
        {
            // Unexpected error
            return Result<CreatePortfolioResult>.Failure(
                Error.BusinessRule($"Failed to create portfolio: {ex.Message}", "PORTFOLIO_CREATION_FAILED"));
        }
    }
}
