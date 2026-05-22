namespace TradyStrat.Domain.Suggestions;

public sealed record PromptFingerprint
{
    public string PromptHash        { get; private set; } = "";
    public string EnvelopeHash      { get; private set; } = "";
    public string PromptVersionHash { get; private set; } = "";

    private PromptFingerprint() { }   // EF

    private PromptFingerprint(string promptHash, string envelopeHash, string promptVersionHash)
    {
        PromptHash        = promptHash;
        EnvelopeHash      = envelopeHash;
        PromptVersionHash = promptVersionHash;
    }

    public static PromptFingerprint Of(string promptHash, string? envelopeHash, string? promptVersionHash)
    {
        if (string.IsNullOrWhiteSpace(promptHash))
            throw new ArgumentException("PromptHash is required.", nameof(promptHash));
        return new PromptFingerprint(promptHash, envelopeHash ?? "", promptVersionHash ?? "");
    }
}
