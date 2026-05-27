namespace TradingSignal.Core.Abstractions;

public interface ISignalGenerator
{
    Task<RawSignal> GenerateAsync(
        FeatureSet features,
        IReadOnlyList<FewShotCase> memory,
        CancellationToken ct);
}
