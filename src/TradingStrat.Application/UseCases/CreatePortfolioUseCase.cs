using TradingStrat.Application.Commands;
using TradingStrat.Application.Common;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for creating a new portfolio.
/// Uses BaseUseCase to eliminate try-catch boilerplate.
/// </summary>
public class CreatePortfolioUseCase : BaseUseCase<CreatePortfolioCommand, CreatePortfolioResult>, ICreatePortfolioUseCase
{
    private readonly IPortfolioPort _portfolioPort;

    public CreatePortfolioUseCase(IPortfolioPort portfolioPort)
    {
        _portfolioPort = portfolioPort ?? throw new ArgumentNullException(nameof(portfolioPort));
    }

    /// <inheritdoc />
    public Task<Result<CreatePortfolioResult>> ExecuteAsync(CreatePortfolioCommand command)
        => base.ExecuteAsync(command, ExecuteCoreAsync, ErrorCodes.Portfolio.CreationFailed);

    private async Task<CreatePortfolioResult> ExecuteCoreAsync(CreatePortfolioCommand command)
    {
        // Create portfolio via repository
        var portfolio = await _portfolioPort.CreatePortfolioAsync(
            command.Name,
            command.Description,
            command.InitialCash);

        // Return result (BaseUseCase will wrap in Result.Success)
        return new CreatePortfolioResult(
            portfolio.Id,
            portfolio.Name,
            portfolio.Cash,
            portfolio.CreatedAt);
    }
}
