namespace TradingSignal.Llm;

public sealed class LmStudioOptions
{
    public string Endpoint { get; set; } = "http://localhost:1234/v1";

    public string ModelId { get; set; } = "qwen2.5-14b-instruct";

    public int TimeoutSeconds { get; set; } = 60;

    public int MaxFewShot { get; set; } = 3;

    public int MaxOutputTokens { get; set; } = 256;
}
