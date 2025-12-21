namespace TradingStrat.Web.Models;

public record DataAnalysisResult(
    string Sentiment,              // "BULLISH" or "BEARISH"
    string Summary,                // 1-2 sentence analysis
    List<string> Recommendations,  // Bullet points
    decimal Confidence,            // 0-100 percentage
    string RobotImage              // Path to robot image (e.g., "/img/robot-neutral.png")
);
