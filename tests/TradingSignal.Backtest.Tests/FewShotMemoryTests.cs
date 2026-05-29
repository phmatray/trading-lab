using System.Collections.Concurrent;
using Shouldly;
using TradingSignal.Adaptation;
using TradingSignal.Core;
using TradingSignal.Core.Abstractions;
using TradingSignal.Indicators;

namespace TradingSignal.Backtest.Tests;

// Few-shot wiring: causality (no look-ahead), disabled-by-default parity, and
// train/test symmetry. All hermetic — no LM Studio, no real LLM.
public sealed class FewShotMemoryTests
{
    private const int CandlesPerDay = 24;
    private const int Horizon = 1;

    private static BacktestOptions Options(int maxFewShot) => new()
    {
        AdaptationDays = 5,
        TestDays = 2,
        StepDays = 2,
        EvaluationHorizonCandles = Horizon,
        CandlesPerDay = CandlesPerDay,
        FeeBps = 10,
        EnableShort = false,
        MaxFewShot = maxFewShot,
    };

    // Captures (decisionIndex, memory) for every GenerateAsync call so tests can
    // assert what the generator actually received. decisionIndex is recovered from
    // the feature's AsOfUtc, which equals candles[i].OpenTimeUtc in Synthetic data.
    private sealed class CapturingGenerator(IReadOnlyList<Candle> candles) : ISignalGenerator
    {
        private readonly Dictionary<DateTime, int> _indexByTime =
            candles.Select((c, j) => (c, j)).ToDictionary(t => t.c.OpenTimeUtc, t => t.j);

        public ConcurrentBag<(int DecisionIndex, IReadOnlyList<FewShotCase> Memory)> Calls { get; } = new();

        public Task<RawSignal> GenerateAsync(
            FeatureSet features, IReadOnlyList<FewShotCase> memory, CancellationToken ct)
        {
            int idx = _indexByTime[features.AsOfUtc];
            // Snapshot the memory — it is built from a reused backing list.
            Calls.Add((idx, memory.ToList()));
            return Task.FromResult(new RawSignal(TradeAction.Buy, 0.9, "cap"));
        }

        public int IndexOf(FeatureSet f) => _indexByTime[f.AsOfUtc];
    }

    [Fact]
    public async Task Memory_At_Decision_i_Never_Includes_A_Bar_Whose_Outcome_Depends_On_Candle_At_Or_After_i()
    {
        IReadOnlyList<Candle> candles = Synthetic.Candles(16 * CandlesPerDay, seed: 13);
        CapturingGenerator gen = new(candles);
        WalkForwardOrchestrator orchestrator = new(
            new FeatureEngine("BTCUSDT"), gen, new NullAdaptation(), store: null, Options(maxFewShot: 5));

        await orchestrator.RunAsync(candles, "BTCUSDT", CancellationToken.None);

        gen.Calls.ShouldNotBeEmpty();
        bool sawAnyMemory = false;
        foreach ((int i, IReadOnlyList<FewShotCase> memory) in gen.Calls)
        {
            foreach (FewShotCase c in memory)
            {
                int j = gen.IndexOf(c.Features);
                // Outcome of bar j is known at decision i iff j + horizon <= i.
                // Any case with j + horizon > i would be a look-ahead leak.
                (j + Horizon).ShouldBeLessThanOrEqualTo(i,
                    $"look-ahead: decision {i} saw a case for bar {j} (needs candle {j + Horizon})");
                sawAnyMemory = true;
            }
        }

        sawAnyMemory.ShouldBeTrue("expected at least one non-empty memory with MaxFewShot=5");
    }

    [Fact]
    public async Task Disabled_MaxFewShot_Feeds_Empty_Memory_Everywhere()
    {
        IReadOnlyList<Candle> candles = Synthetic.Candles(16 * CandlesPerDay, seed: 21);
        CapturingGenerator gen = new(candles);
        WalkForwardOrchestrator orchestrator = new(
            new FeatureEngine("BTCUSDT"), gen, new NullAdaptation(), store: null, Options(maxFewShot: 0));

        await orchestrator.RunAsync(candles, "BTCUSDT", CancellationToken.None);

        gen.Calls.ShouldNotBeEmpty();
        gen.Calls.ShouldAllBe(c => c.Memory.Count == 0);
    }

    [Fact]
    public async Task Memory_Size_Never_Exceeds_MaxFewShot()
    {
        const int max = 3;
        IReadOnlyList<Candle> candles = Synthetic.Candles(16 * CandlesPerDay, seed: 5);
        CapturingGenerator gen = new(candles);
        WalkForwardOrchestrator orchestrator = new(
            new FeatureEngine("BTCUSDT"), gen, new NullAdaptation(), store: null, Options(maxFewShot: max));

        await orchestrator.RunAsync(candles, "BTCUSDT", CancellationToken.None);

        gen.Calls.ShouldAllBe(c => c.Memory.Count <= max);
        gen.Calls.ShouldContain(c => c.Memory.Count == max);
    }

    [Fact]
    public async Task Symmetry_Both_Adaptation_And_Test_Windows_Receive_NonEmpty_Memory()
    {
        // A strategy that calls AdaptationDatasetBuilder (and therefore the generator)
        // over the ADAPTATION window during FitAsync. With MaxFewShot > 0 we expect the
        // generator to receive non-empty memory in BOTH the adaptation window and the
        // test window — proving train/test parity, not a test-only wiring.
        IReadOnlyList<Candle> candles = Synthetic.Candles(16 * CandlesPerDay, seed: 99);
        CapturingGenerator gen = new(candles);
        FeatureEngine fe = new("BTCUSDT");
        ThresholdOptimizer strategy = new(fe, gen);
        BacktestOptions opts = Options(maxFewShot: 5);
        WalkForwardOrchestrator orchestrator = new(fe, gen, strategy, store: null, opts);

        await orchestrator.RunAsync(candles, "BTCUSDT", CancellationToken.None);

        // First segment: adaptation window is [warmup .. testStart), test window starts at
        // adaptationCandles. Classify each captured call by its decision index.
        int testStart = opts.AdaptationCandles;
        var adaptationCalls = gen.Calls.Where(c => c.DecisionIndex < testStart).ToList();
        var testCalls = gen.Calls.Where(c => c.DecisionIndex >= testStart).ToList();

        adaptationCalls.ShouldNotBeEmpty("adaptation window should have produced generator calls");
        testCalls.ShouldNotBeEmpty("test window should have produced generator calls");

        adaptationCalls.ShouldContain(c => c.Memory.Count > 0,
            "adaptation-window calls must receive non-empty few-shot memory (train/test parity)");
        testCalls.ShouldContain(c => c.Memory.Count > 0,
            "test-window calls must receive non-empty few-shot memory");

        // And the cap is respected in both windows.
        adaptationCalls.ShouldAllBe(c => c.Memory.Count <= 5);
        testCalls.ShouldAllBe(c => c.Memory.Count <= 5);
    }

    [Fact]
    public async Task Symmetry_Disabled_Both_Windows_Receive_Empty_Memory()
    {
        IReadOnlyList<Candle> candles = Synthetic.Candles(16 * CandlesPerDay, seed: 77);
        CapturingGenerator gen = new(candles);
        FeatureEngine fe = new("BTCUSDT");
        ThresholdOptimizer strategy = new(fe, gen);
        WalkForwardOrchestrator orchestrator = new(fe, gen, strategy, store: null, Options(maxFewShot: 0));

        await orchestrator.RunAsync(candles, "BTCUSDT", CancellationToken.None);

        gen.Calls.ShouldNotBeEmpty();
        gen.Calls.ShouldAllBe(c => c.Memory.Count == 0);
    }
}
