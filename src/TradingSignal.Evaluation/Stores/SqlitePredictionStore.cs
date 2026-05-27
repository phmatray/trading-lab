using System.Globalization;
using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using TradingSignal.Core;
using TradingSignal.Core.Abstractions;

namespace TradingSignal.Evaluation.Stores;

public sealed class SqlitePredictionStore : IPredictionStore, IAsyncDisposable
{
    private readonly string _connString;
    private readonly SemaphoreSlim _initGate = new(1, 1);
    private bool _initialized;

    private static readonly JsonSerializerOptions FeaturesJson = new(JsonSerializerDefaults.Web);

    public SqlitePredictionStore(string dbPath)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(dbPath));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        _connString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Cache = SqliteCacheMode.Shared,
        }.ToString();
    }

    private async ValueTask<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        var conn = new SqliteConnection(_connString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        return conn;
    }

    private async ValueTask EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized) return;
        await _initGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_initialized) return;
            await using var conn = new SqliteConnection(_connString);
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await conn.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS predictions (
                    id TEXT PRIMARY KEY,
                    as_of_utc TEXT NOT NULL,
                    symbol TEXT NOT NULL,
                    segment INTEGER NOT NULL,
                    action INTEGER NOT NULL,
                    confidence REAL NOT NULL,
                    reason TEXT NOT NULL,
                    features_json TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ix_predictions_segment ON predictions(segment);
                CREATE TABLE IF NOT EXISTS outcomes (
                    prediction_id TEXT PRIMARY KEY,
                    entry_price REAL NOT NULL,
                    exit_price REAL NOT NULL,
                    realized_return_pct REAL NOT NULL,
                    direction_correct INTEGER NOT NULL,
                    FOREIGN KEY(prediction_id) REFERENCES predictions(id)
                );
                """).ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initGate.Release();
        }
    }

    public async Task SavePredictionAsync(Prediction prediction, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(
            """
            INSERT OR REPLACE INTO predictions
              (id, as_of_utc, symbol, segment, action, confidence, reason, features_json)
            VALUES (@Id, @AsOf, @Symbol, @Segment, @Action, @Confidence, @Reason, @Features);
            """,
            new
            {
                Id = prediction.Id.ToString("N"),
                AsOf = prediction.AsOfUtc.ToString("o", CultureInfo.InvariantCulture),
                prediction.Symbol,
                Segment = prediction.WalkForwardSegment,
                Action = (int)prediction.Signal.Action,
                prediction.Signal.Confidence,
                prediction.Signal.Reason,
                Features = JsonSerializer.Serialize(prediction.Features, FeaturesJson),
            }).ConfigureAwait(false);
    }

    public async Task SaveOutcomeAsync(Outcome outcome, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(
            """
            INSERT OR REPLACE INTO outcomes
              (prediction_id, entry_price, exit_price, realized_return_pct, direction_correct)
            VALUES (@Id, @Entry, @Exit, @Realized, @Dir);
            """,
            new
            {
                Id = outcome.PredictionId.ToString("N"),
                Entry = (double)outcome.EntryPrice,
                Exit = (double)outcome.ExitPrice,
                Realized = outcome.RealizedReturnPct,
                Dir = outcome.DirectionCorrect ? 1 : 0,
            }).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<(Prediction Prediction, Outcome? Outcome)>> GetSegmentAsync(
        int segment, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct).ConfigureAwait(false);
        var rows = await conn.QueryAsync<JoinedRow>(
            """
            SELECT p.id AS Id,
                   p.as_of_utc AS AsOfUtc,
                   p.symbol AS Symbol,
                   p.segment AS Segment,
                   p.action AS Action,
                   p.confidence AS Confidence,
                   p.reason AS Reason,
                   p.features_json AS FeaturesJson,
                   o.entry_price AS EntryPrice,
                   o.exit_price AS ExitPrice,
                   o.realized_return_pct AS RealizedReturnPct,
                   o.direction_correct AS DirectionCorrect
            FROM predictions p
            LEFT JOIN outcomes o ON o.prediction_id = p.id
            WHERE p.segment = @segment
            ORDER BY p.as_of_utc;
            """,
            new { segment }).ConfigureAwait(false);

        var list = new List<(Prediction, Outcome?)>();
        foreach (var r in rows)
        {
            var id = Guid.ParseExact(r.Id, "N");
            var features = JsonSerializer.Deserialize<FeatureSet>(r.FeaturesJson, FeaturesJson)
                ?? throw new InvalidOperationException($"features_json for {r.Id} did not deserialize");
            var prediction = new Prediction(
                Id: id,
                AsOfUtc: DateTime.Parse(r.AsOfUtc, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
                Symbol: r.Symbol,
                Features: features,
                Signal: new RawSignal((TradeAction)r.Action, r.Confidence, r.Reason),
                WalkForwardSegment: r.Segment);

            Outcome? outcome = null;
            if (r.EntryPrice.HasValue)
            {
                outcome = new Outcome(
                    PredictionId: id,
                    EntryPrice: (decimal)r.EntryPrice.Value,
                    ExitPrice: (decimal)r.ExitPrice!.Value,
                    RealizedReturnPct: r.RealizedReturnPct!.Value,
                    DirectionCorrect: r.DirectionCorrect!.Value != 0);
            }

            list.Add((prediction, outcome));
        }

        return list;
    }

    public ValueTask DisposeAsync()
    {
        _initGate.Dispose();
        SqliteConnection.ClearAllPools();
        return ValueTask.CompletedTask;
    }

    private sealed class JoinedRow
    {
        public string Id { get; set; } = "";
        public string AsOfUtc { get; set; } = "";
        public string Symbol { get; set; } = "";
        public int Segment { get; set; }
        public int Action { get; set; }
        public double Confidence { get; set; }
        public string Reason { get; set; } = "";
        public string FeaturesJson { get; set; } = "";
        public double? EntryPrice { get; set; }
        public double? ExitPrice { get; set; }
        public double? RealizedReturnPct { get; set; }
        public int? DirectionCorrect { get; set; }
    }
}
