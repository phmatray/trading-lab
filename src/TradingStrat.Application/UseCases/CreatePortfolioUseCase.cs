using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;

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
    public async Task<CreatePortfolioResult> ExecuteAsync(CreatePortfolioCommand command)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("Portfolio name is required", nameof(command));
        }

        if (command.InitialCash < 0)
        {
            throw new ArgumentException("Initial cash cannot be negative", nameof(command));
        }

        // Create portfolio via repository
        var portfolio = await _portfolioPort.CreatePortfolioAsync(
            command.Name,
            command.Description,
            command.InitialCash);

        // Return result
        return new CreatePortfolioResult(
            portfolio.Id,
            portfolio.Name,
            portfolio.Cash,
            portfolio.CreatedAt);
    }
}
