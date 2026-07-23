using TradyStrat.Domain.Suggestions;
using System.Text.RegularExpressions;

namespace TradyStrat.Application.PredictionMarkets;

/// <summary>
/// Question-text keyword filter. Drops markets that pass the tag pre-filter
/// but ask irrelevant questions (Trump tweets, NFT events, memecoin pumps,
/// political crossovers). A market is relevant only if its question matches
/// one of: BTC/ETH price targets, Coinbase corporate events, crypto ETF
/// approvals, or Fed/FOMC rate decisions.
/// </summary>
public static partial class PolymarketRelevance
{
    public static bool IsRelevant(string question)
    {
        if (string.IsNullOrWhiteSpace(question)) return false;
        return BtcPrice().IsMatch(question)
            || EthPrice().IsMatch(question)
            || CoinbaseEvent().IsMatch(question)
            || EtfApproval().IsMatch(question)
            || FedRates().IsMatch(question);
    }

    // BTC/Bitcoin paired with a price-action verb and a $-figure.
    [GeneratedRegex(
        @"\b(?:bitcoin|btc)\b.*?\b(?:above|below|reach|cross|hit|touch|close above|close below)\b.*?\$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BtcPrice();

    // ETH/Ethereum paired with a price-action verb and a $-figure.
    [GeneratedRegex(
        @"\b(?:ethereum|eth)\b.*?\b(?:above|below|reach|cross|hit|touch|close above|close below)\b.*?\$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EthPrice();

    // Coinbase paired with a corporate-event verb.
    [GeneratedRegex(
        @"\bcoinbase\b.*?\b(?:beat|miss|earnings|eps|revenue|q[1-4]|added to|s&p|listing|listed)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CoinbaseEvent();

    // Crypto ETF/ETP approvals/launches/rejections — order-independent.
    [GeneratedRegex(
        @"^(?=.*\b(?:etf|etp)\b)(?=.*\b(?:approv|launch|reject))(?=.*\b(?:bitcoin|btc|ethereum|eth|solana|sol|spot)\b)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EtfApproval();

    // Fed/FOMC rate decisions.
    [GeneratedRegex(
        @"\b(?:fed|fomc|federal reserve)\b.*?\b(?:cut|hike|raise|hold|rate)\b|\b(?:rate cut|rate hike|interest rate)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FedRates();
}
