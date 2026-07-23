// <copyright file="MarketDataRefreshJob.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;

namespace TradingBot.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job to refresh market data for active symbols.
/// </summary>
public sealed class MarketDataRefreshJob : IJob
{
    private readonly ILogger<MarketDataRefreshJob> _logger;
    private readonly IPortfolioManager _portfolioManager;
    private readonly IMarketDataService _marketDataService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketDataRefreshJob"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="portfolioManager">Portfolio manager.</param>
    /// <param name="marketDataService">Market data service.</param>
    public MarketDataRefreshJob(
        ILogger<MarketDataRefreshJob> logger,
        IPortfolioManager portfolioManager,
        IMarketDataService marketDataService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _portfolioManager = portfolioManager ?? throw new ArgumentNullException(nameof(portfolioManager));
        _marketDataService = marketDataService ?? throw new ArgumentNullException(nameof(marketDataService));
    }

    /// <inheritdoc/>
    public string JobName => "Market Data Refresh";

    /// <inheritdoc/>
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Get all open positions to know which symbols to refresh
            var positions = await _portfolioManager.GetPositionsAsync(cancellationToken);

            if (positions.Count == 0)
            {
                _logger.LogDebug("No open positions - skipping market data refresh");
                return;
            }

            var symbols = positions.Select(p => p.Symbol).Distinct().ToList();
            _logger.LogInformation("Refreshing market data for {Count} symbols", symbols.Count);

            // Fetch current quotes for all symbols
            var quotes = new Dictionary<string, decimal>();
            foreach (var symbol in symbols)
            {
                try
                {
                    var quote = await _marketDataService.GetQuoteAsync(symbol, cancellationToken);
                    quotes[symbol] = quote.Price;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch quote for {Symbol}", symbol);
                }
            }

            // Update position prices
            foreach (var position in positions)
            {
                if (quotes.TryGetValue(position.Symbol, out var price))
                {
                    position.CurrentPrice = price;
                    _logger.LogDebug(
                        "Updated {Symbol} price: ${Price:F2} (P&L: {PnL:C2})",
                        position.Symbol,
                        price,
                        position.UnrealizedPnL);
                }
            }

            _logger.LogInformation("Market data refresh completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during market data refresh: {Message}", ex.Message);
            throw;
        }
    }
}
