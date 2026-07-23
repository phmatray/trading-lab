using Shouldly;
using TradyStrat.Application.PredictionMarkets;
using Xunit;

namespace TradyStrat.Application.Tests.PredictionMarkets;

public class PolymarketRelevanceTests
{
    [Theory]
    // BTC / Bitcoin price targets — multiple wordings Polymarket actually uses
    [InlineData("Will Bitcoin close above $100,000 on Dec 31, 2026?")]
    [InlineData("Will BTC reach $150K in 2026?")]
    [InlineData("Will Bitcoin trade below $50,000 at any point in Q3 2026?")]
    [InlineData("Will BTC hit $200k by EOY?")]
    // ETH / Ethereum price targets
    [InlineData("Will Ethereum close above $5,000 on Dec 31, 2026?")]
    [InlineData("Will ETH cross $10K in 2026?")]
    // Coinbase corporate events
    [InlineData("Will Coinbase beat Q3 2026 EPS estimates?")]
    [InlineData("Will Coinbase miss Q4 earnings?")]
    [InlineData("Will Coinbase be added to the S&P 500 in 2026?")]
    // ETF / ETP approvals
    [InlineData("Will the SEC approve a spot Solana ETF in 2026?")]
    [InlineData("Will a Bitcoin ETF launch in Hong Kong in 2026?")]
    // Macro
    [InlineData("Will the Fed cut rates in September 2026?")]
    [InlineData("Will FOMC raise rates at the next meeting?")]
    public void IsRelevant_returns_true_for_relevant_question(string question)
        => PolymarketRelevance.IsRelevant(question).ShouldBeTrue($"expected relevant: {question}");

    [Theory]
    // Crypto-tagged but irrelevant junk that Polymarket actually returns
    [InlineData("Will Trump tweet about Bitcoin this week?")]
    [InlineData("Will Tether collapse before 2027?")]
    [InlineData("Top NFT collection by floor price in December?")]
    [InlineData("Will an Elon Musk crypto launch in 2026?")]
    [InlineData("Will Binance CEO be charged in 2026?")]
    [InlineData("Will any US state ban crypto mining in 2026?")]
    // Random political/election crossovers tagged as crypto
    [InlineData("Will Donald Trump win the 2028 election?")]
    [InlineData("Will the next US president own crypto?")]
    // Memecoin pump bets
    [InlineData("Will DOGE flip XRP by market cap in 2026?")]
    public void IsRelevant_returns_false_for_irrelevant_question(string question)
        => PolymarketRelevance.IsRelevant(question).ShouldBeFalse($"expected irrelevant: {question}");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IsRelevant_returns_false_for_empty_question(string question)
        => PolymarketRelevance.IsRelevant(question).ShouldBeFalse();
}
