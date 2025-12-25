using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Services;

/// <summary>
/// Service for managing AI insights with caching to minimize redundant calculations.
/// Provides market regime detection and trading recommendations for portfolios and tickers.
/// </summary>
public class AiInsightsService
{
    private readonly MarketRegimeDetector _regimeDetector;
    private readonly AiRecommendationService _recommendationService;
    private readonly PortfolioStateService _portfolioState;
    private readonly LocalStorageService _localStorage;
    private readonly IPortfolioPort _portfolioPort;

    // Cache keys
    private const string REGIME_CACHE_KEY = "ai_insights_regime";
    private const string RECOMMENDATION_CACHE_KEY = "ai_insights_recommendation";
    private const string CACHE_TIMESTAMP_KEY = "ai_insights_timestamp";
    private const int CACHE_EXPIRY_MINUTES = 15; // Refresh every 15 minutes

    public AiInsightsService(
        MarketRegimeDetector regimeDetector,
        AiRecommendationService recommendationService,
        PortfolioStateService portfolioState,
        LocalStorageService localStorage,
        IPortfolioPort portfolioPort)
    {
        _regimeDetector = regimeDetector;
        _recommendationService = recommendationService;
        _portfolioState = portfolioState;
        _localStorage = localStorage;
        _portfolioPort = portfolioPort;
    }

    /// <summary>
    /// Gets the current market regime for the selected portfolio with caching.
    /// </summary>
    public async Task<MarketRegime> GetCurrentRegimeAsync(bool forceRefresh = false)
    {
        // Check cache expiry
        if (!forceRefresh && await IsCacheValidAsync())
        {
            var cachedRegime = await _localStorage.GetItemAsync<MarketRegime?>(REGIME_CACHE_KEY);
            if (cachedRegime != null)
            {
                return cachedRegime;
            }
        }

        // Calculate new regime
        int? portfolioId = await _portfolioState.GetSelectedPortfolioIdAsync();
        if (!portfolioId.HasValue)
        {
            return new MarketRegime("NEUTRAL", null);
        }

        try
        {
            List<string> portfolio = await GetPortfolioTickersAsync(portfolioId.Value);
            if (portfolio.Count == 0)
            {
                return new MarketRegime("NEUTRAL", null);
            }

            string regimeValue = await _regimeDetector.DetectPortfolioRegimeAsync(portfolio);
            var regime = new MarketRegime(regimeValue, portfolioId.Value);

            // Cache the result
            await _localStorage.SetItemAsync(REGIME_CACHE_KEY, regime);
            await UpdateCacheTimestampAsync();

            return regime;
        }
        catch
        {
            return new MarketRegime("NEUTRAL", portfolioId.Value);
        }
    }

    /// <summary>
    /// Gets the current trading recommendation for the selected portfolio with caching.
    /// </summary>
    public async Task<PortfolioRecommendation> GetCurrentRecommendationAsync(bool forceRefresh = false)
    {
        // Check cache expiry
        if (!forceRefresh && await IsCacheValidAsync())
        {
            var cachedRec = await _localStorage.GetItemAsync<PortfolioRecommendation?>(RECOMMENDATION_CACHE_KEY);
            if (cachedRec != null)
            {
                return cachedRec;
            }
        }

        // Calculate new recommendation
        int? portfolioId = await _portfolioState.GetSelectedPortfolioIdAsync();
        if (!portfolioId.HasValue)
        {
            return new PortfolioRecommendation("HOLD", 50, new List<string> { "No portfolio selected" });
        }

        try
        {
            List<string> portfolio = await GetPortfolioTickersAsync(portfolioId.Value);
            if (portfolio.Count == 0)
            {
                return new PortfolioRecommendation("HOLD", 50, new List<string> { "No positions in portfolio" });
            }

            Dictionary<string, RecommendationResult> recommendations = await _recommendationService.GeneratePortfolioRecommendationsAsync(portfolio);

            // Aggregate recommendations
            PortfolioRecommendation aggregated = AggregateRecommendations(recommendations);

            // Cache the result
            await _localStorage.SetItemAsync(RECOMMENDATION_CACHE_KEY, aggregated);
            await UpdateCacheTimestampAsync();

            return aggregated;
        }
        catch (Exception ex)
        {
            return new PortfolioRecommendation("HOLD", 0, new List<string> { $"Error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Clears the AI insights cache, forcing a refresh on next request.
    /// </summary>
    public async Task ClearCacheAsync()
    {
        await _localStorage.RemoveItemAsync(REGIME_CACHE_KEY);
        await _localStorage.RemoveItemAsync(RECOMMENDATION_CACHE_KEY);
        await _localStorage.RemoveItemAsync(CACHE_TIMESTAMP_KEY);
    }

    private async Task<List<string>> GetPortfolioTickersAsync(int portfolioId)
    {
        try
        {
            Domain.Entities.Portfolio? portfolio = await _portfolioPort.GetPortfolioByIdAsync(portfolioId);
            if (portfolio == null)
            {
                return new List<string>();
            }

            return portfolio.Positions.Select(p => p.Ticker).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private PortfolioRecommendation AggregateRecommendations(
        Dictionary<string, RecommendationResult> recommendations)
    {
        if (recommendations.Count == 0)
        {
            return new PortfolioRecommendation("HOLD", 50, new List<string> { "No recommendations available" });
        }

        int buyCount = recommendations.Values.Count(r => r.Action == "BUY");
        int sellCount = recommendations.Values.Count(r => r.Action == "SELL");
        int holdCount = recommendations.Values.Count(r => r.Action == "HOLD");

        int avgConfidence = (int)recommendations.Values.Average(r => r.Confidence);

        List<string> topReasons = recommendations.Values
            .SelectMany(r => r.Reasons)
            .Take(3)
            .ToList();

        string action;
        if (buyCount > sellCount && buyCount > holdCount)
        {
            action = "BUY";
        }
        else if (sellCount > buyCount && sellCount > holdCount)
        {
            action = "SELL";
        }
        else
        {
            action = "HOLD";
        }

        return new PortfolioRecommendation(action, avgConfidence, topReasons);
    }

    private async Task<bool> IsCacheValidAsync()
    {
        DateTime? timestamp = await _localStorage.GetItemAsync<DateTime?>(CACHE_TIMESTAMP_KEY);
        if (!timestamp.HasValue)
        {
            return false;
        }

        return DateTime.UtcNow - timestamp.Value < TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES);
    }

    private async Task UpdateCacheTimestampAsync()
    {
        await _localStorage.SetItemAsync(CACHE_TIMESTAMP_KEY, DateTime.UtcNow);
    }
}

/// <summary>
/// Market regime for a portfolio.
/// </summary>
/// <param name="Regime">Market regime: "BULLISH", "BEARISH", or "NEUTRAL".</param>
/// <param name="PortfolioId">Portfolio ID this regime applies to.</param>
public record MarketRegime(string Regime, int? PortfolioId);

/// <summary>
/// Aggregated portfolio recommendation.
/// </summary>
/// <param name="Action">Recommended action: "BUY", "SELL", or "HOLD".</param>
/// <param name="Confidence">Average confidence level (0-100).</param>
/// <param name="Reasons">Top reasons for the recommendation.</param>
public record PortfolioRecommendation(string Action, int Confidence, List<string> Reasons);
