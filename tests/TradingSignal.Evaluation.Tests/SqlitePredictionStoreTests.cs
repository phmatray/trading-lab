using Microsoft.Data.Sqlite;
using Shouldly;
using TradingSignal.Core;
using TradingSignal.Evaluation.Stores;

namespace TradingSignal.Evaluation.Tests;

public sealed class SqlitePredictionStoreTests : IDisposable
{
    private readonly string _dbPath;

    public SqlitePredictionStoreTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"tsig-store-{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { /* best effort */ }
        }
    }

    private static Prediction MakePrediction(Guid id, int segment, TradeAction action, DateTime asOf)
    {
        var features = new FeatureSet(asOf, "BTCUSDT", 50_000m, 50, 1, 0.5, 0.5, 49_900, 49_500, 100, 0.001, 0.002, 1.2);
        return new Prediction(id, asOf, "BTCUSDT", features, new RawSignal(action, 0.65, "x"), segment);
    }

    [Fact]
    public async Task RoundTrips_Prediction_And_Outcome()
    {
        await using var store = new SqlitePredictionStore(_dbPath);
        var asOf = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();
        var pred = MakePrediction(id, segment: 3, action: TradeAction.Buy, asOf);
        var outcome = new Outcome(id, 100m, 101m, 0.01, true);

        await store.SavePredictionAsync(pred, CancellationToken.None);
        await store.SaveOutcomeAsync(outcome, CancellationToken.None);

        var rows = await store.GetSegmentAsync(3, CancellationToken.None);
        rows.Count.ShouldBe(1);
        rows[0].Prediction.Id.ShouldBe(id);
        rows[0].Prediction.Symbol.ShouldBe("BTCUSDT");
        rows[0].Prediction.Signal.Action.ShouldBe(TradeAction.Buy);
        rows[0].Prediction.Features.Rsi14.ShouldBe(50d);
        rows[0].Outcome.ShouldNotBeNull();
        rows[0].Outcome!.EntryPrice.ShouldBe(100m);
        rows[0].Outcome!.RealizedReturnPct.ShouldBe(0.01);
    }

    [Fact]
    public async Task Prediction_Without_Outcome_Returns_Null_Outcome()
    {
        await using var store = new SqlitePredictionStore(_dbPath);
        var pred = MakePrediction(Guid.NewGuid(), segment: 1, action: TradeAction.Hold, DateTime.UtcNow);
        await store.SavePredictionAsync(pred, CancellationToken.None);

        var rows = await store.GetSegmentAsync(1, CancellationToken.None);

        rows.Count.ShouldBe(1);
        rows[0].Outcome.ShouldBeNull();
    }

    [Fact]
    public async Task GetSegmentAsync_Filters_By_Segment()
    {
        await using var store = new SqlitePredictionStore(_dbPath);
        var asOf = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        await store.SavePredictionAsync(MakePrediction(Guid.NewGuid(), 1, TradeAction.Buy, asOf), CancellationToken.None);
        await store.SavePredictionAsync(MakePrediction(Guid.NewGuid(), 1, TradeAction.Sell, asOf.AddHours(1)), CancellationToken.None);
        await store.SavePredictionAsync(MakePrediction(Guid.NewGuid(), 2, TradeAction.Hold, asOf.AddHours(2)), CancellationToken.None);

        var s1 = await store.GetSegmentAsync(1, CancellationToken.None);
        var s2 = await store.GetSegmentAsync(2, CancellationToken.None);

        s1.Count.ShouldBe(2);
        s2.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetSegmentAsync_Orders_By_AsOf()
    {
        await using var store = new SqlitePredictionStore(_dbPath);
        var t0 = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Insert out of order
        await store.SavePredictionAsync(MakePrediction(Guid.NewGuid(), 7, TradeAction.Buy, t0.AddHours(5)), CancellationToken.None);
        await store.SavePredictionAsync(MakePrediction(Guid.NewGuid(), 7, TradeAction.Buy, t0.AddHours(1)), CancellationToken.None);
        await store.SavePredictionAsync(MakePrediction(Guid.NewGuid(), 7, TradeAction.Buy, t0.AddHours(3)), CancellationToken.None);

        var rows = await store.GetSegmentAsync(7, CancellationToken.None);

        rows.Count.ShouldBe(3);
        for (var i = 1; i < rows.Count; i++)
            rows[i].Prediction.AsOfUtc.ShouldBeGreaterThan(rows[i - 1].Prediction.AsOfUtc);
    }

    [Fact]
    public async Task FeatureSet_Survives_Round_Trip_Through_Json()
    {
        await using var store = new SqlitePredictionStore(_dbPath);
        var asOf = new DateTime(2024, 3, 14, 9, 26, 53, DateTimeKind.Utc);
        var rich = new FeatureSet(asOf, "ETHUSDT", 3_456.78m,
            Rsi14: 67.5, MacdLine: 0.123, MacdSignal: 0.10, MacdHistogram: 0.023,
            Ema20: 3_400, Ema50: 3_300, Atr14: 50,
            Return1: 0.001, Return5: 0.005, VolatilityPct: 2.3);

        var id = Guid.NewGuid();
        var pred = new Prediction(id, asOf, "ETHUSDT", rich, new RawSignal(TradeAction.Buy, 0.81, "rich"), 4);

        await store.SavePredictionAsync(pred, CancellationToken.None);
        var rows = await store.GetSegmentAsync(4, CancellationToken.None);

        rows.Count.ShouldBe(1);
        rows[0].Prediction.Features.ShouldBe(rich);
    }

    [Fact]
    public async Task Round_Trips_Prediction_With_Reasoning()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"tsig-pred-{Guid.NewGuid():N}.db");
        try
        {
            await using SqlitePredictionStore sut = new(dbPath);
            Prediction p = MakePredictionWithReasoning(reasoning: "long thinking trace");

            await sut.SavePredictionAsync(p, TestContext.Current.CancellationToken);
            IReadOnlyList<(Prediction Prediction, Outcome? Outcome)> got = await sut.GetSegmentAsync(p.WalkForwardSegment, TestContext.Current.CancellationToken);

            got.Count.ShouldBe(1);
            got[0].Prediction.Signal.Reasoning.ShouldBe("long thinking trace");
        }
        finally { CleanupDb(dbPath); }
    }

    [Fact]
    public async Task Migrates_PreExisting_Predictions_Db_Without_Reasoning_Column()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"tsig-pred-{Guid.NewGuid():N}.db");
        try
        {
            await using (SqliteConnection conn = new($"Data Source={dbPath}"))
            {
                await conn.OpenAsync(TestContext.Current.CancellationToken);
                await using SqliteCommand cmd = conn.CreateCommand();
                cmd.CommandText = """
                    CREATE TABLE predictions (
                        id TEXT PRIMARY KEY,
                        as_of_utc TEXT NOT NULL,
                        symbol TEXT NOT NULL,
                        segment INTEGER NOT NULL,
                        action INTEGER NOT NULL,
                        confidence REAL NOT NULL,
                        reason TEXT NOT NULL,
                        features_json TEXT NOT NULL
                    );
                    """;
                await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            }
            SqliteConnection.ClearAllPools();

            await using SqlitePredictionStore sut = new(dbPath);
            Prediction p = MakePredictionWithReasoning(reasoning: "trace");
            await sut.SavePredictionAsync(p, TestContext.Current.CancellationToken);
            IReadOnlyList<(Prediction Prediction, Outcome? Outcome)> got = await sut.GetSegmentAsync(p.WalkForwardSegment, TestContext.Current.CancellationToken);

            got[0].Prediction.Signal.Reasoning.ShouldBe("trace");
        }
        finally { CleanupDb(dbPath); }
    }

    private static Prediction MakePredictionWithReasoning(string? reasoning)
    {
        FeatureSet features = new(
            AsOfUtc: new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc),
            Symbol: "BTCUSDT",
            Close: 65_000m,
            Rsi14: 55, MacdLine: 12, MacdSignal: 10, MacdHistogram: 2,
            Ema20: 64_500, Ema50: 64_000, Atr14: 800,
            Return1: 0.002, Return5: 0.01, VolatilityPct: 1.4);
        RawSignal signal = new(TradeAction.Buy, 0.7, "short", reasoning);
        return new Prediction(Guid.NewGuid(), features.AsOfUtc, features.Symbol, features, signal, WalkForwardSegment: 0);
    }

    private static void CleanupDb(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath)) File.Delete(dbPath);
    }
}
