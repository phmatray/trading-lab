using System.Globalization;
using System.Text;
using TradingSignal.Core;

namespace TradingSignal.Llm.Prompts;

public static class PromptBuilder
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public const string SystemPromptInstruct =
        """
        You are a disciplined crypto trading signal generator.
        You will receive a snapshot of pre-computed technical indicators for a single asset
        and you must respond with exactly ONE JSON object matching the provided schema:
          { "action": "BUY" | "SELL" | "HOLD", "confidence": 0.0..1.0, "reason": "string" }

        Rules:
        - Output JSON only. No prose, no markdown, no code fences.
        - "confidence" must be in [0, 1] and reflect your subjective probability that the
          chosen action will be profitable net of typical transaction fees over the next bar.
        - Prefer HOLD when signals conflict or are weak. Doing nothing is a valid action.
        - "reason" is one short sentence. Do not reveal chain-of-thought.
        """;

    public const string SystemPromptReasoning =
        """
        You are a disciplined crypto trading signal generator.
        You may think step-by-step about the indicators before answering. After your
        reasoning, output exactly ONE JSON object on the final line matching:
          { "action": "BUY" | "SELL" | "HOLD", "confidence": 0.0..1.0, "reason": "string" }

        Rules:
        - The final line must be valid JSON only — no prose, no markdown, no code fences after the JSON.
        - "confidence" must be in [0, 1] and reflect your subjective probability that the
          chosen action will be profitable net of typical transaction fees over the next bar.
        - Prefer HOLD when signals conflict or are weak. Doing nothing is a valid action.
        - "reason" is one short sentence summarizing your conclusion.
        """;

    public static string BuildUserMessage(FeatureSet features, IReadOnlyList<FewShotCase> memory, int maxFewShot)
    {
        var sb = new StringBuilder(1024);

        if (memory.Count > 0 && maxFewShot > 0)
        {
            sb.AppendLine("Recent observations and the action that turned out profitable in hindsight:");
            sb.AppendLine();
            var examples = memory.Take(maxFewShot).ToList();
            for (var i = 0; i < examples.Count; i++)
            {
                var ex = examples[i];
                sb.Append("Example ").Append(i + 1).AppendLine(":");
                AppendFeatures(sb, ex.Features);
                sb.Append("  best_action_in_hindsight: ").AppendLine(ex.ActualBestAction.ToString().ToUpperInvariant());
                sb.Append("  realized_return_pct: ").AppendLine(ex.RealizedReturnPct.ToString("F4", Inv));
                sb.AppendLine();
            }
        }

        sb.AppendLine("Current snapshot:");
        AppendFeatures(sb, features);
        sb.AppendLine();
        sb.AppendLine("Respond with the JSON object only.");

        return sb.ToString();
    }

    private static void AppendFeatures(StringBuilder sb, FeatureSet f)
    {
        sb.Append("  symbol: ").AppendLine(f.Symbol);
        sb.Append("  as_of_utc: ").AppendLine(f.AsOfUtc.ToString("o", Inv));
        sb.Append("  close: ").AppendLine(f.Close.ToString(Inv));
        sb.Append("  rsi14: ").AppendLine(f.Rsi14.ToString("F4", Inv));
        sb.Append("  macd_line: ").AppendLine(f.MacdLine.ToString("F6", Inv));
        sb.Append("  macd_signal: ").AppendLine(f.MacdSignal.ToString("F6", Inv));
        sb.Append("  macd_histogram: ").AppendLine(f.MacdHistogram.ToString("F6", Inv));
        sb.Append("  ema20: ").AppendLine(f.Ema20.ToString("F4", Inv));
        sb.Append("  ema50: ").AppendLine(f.Ema50.ToString("F4", Inv));
        sb.Append("  atr14: ").AppendLine(f.Atr14.ToString("F4", Inv));
        sb.Append("  return1: ").AppendLine(f.Return1.ToString("F6", Inv));
        sb.Append("  return5: ").AppendLine(f.Return5.ToString("F6", Inv));
        sb.Append("  volatility_pct: ").AppendLine(f.VolatilityPct.ToString("F6", Inv));
        // Pre-computed derived features. The reasoning model was observed to misorder
        // numerically close EMAs when comparing them inline ("price > EMA20 > EMA50" when
        // actually EMA20 < EMA50). Surfacing the comparisons as categorical strings removes
        // that arithmetic load from the prompt and prevents the model from acting on a
        // wrong premise.
        sb.Append("  ema_cross: ").AppendLine(EmaCross(f));
        sb.Append("  price_vs_emas: ").AppendLine(PriceVsEmas(f));
    }

    internal static string EmaCross(FeatureSet f) =>
        f.Ema20 > f.Ema50 ? "bullish (EMA20 > EMA50)" :
        f.Ema20 < f.Ema50 ? "bearish (EMA20 < EMA50)" :
        "neutral (EMA20 == EMA50)";

    internal static string PriceVsEmas(FeatureSet f)
    {
        bool aboveEma20 = (double)f.Close > f.Ema20;
        bool aboveEma50 = (double)f.Close > f.Ema50;
        return (aboveEma20, aboveEma50) switch
        {
            (true,  true)  => "price above both",
            (false, false) => "price below both",
            (true,  false) => "price above EMA20, below EMA50",
            (false, true)  => "price below EMA20, above EMA50",
        };
    }
}
