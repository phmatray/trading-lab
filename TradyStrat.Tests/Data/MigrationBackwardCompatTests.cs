using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Data;
using Xunit;

namespace TradyStrat.Tests.Data;

/// <summary>
/// Backward-compatibility test for Plan Task 11. Replaces the original manual
/// "back up live DB, run app, verify dashboard" smoke with an automated
/// equivalent that exercises the same upgrade path: seed a file-based SQLite
/// DB with the pre-Phase-1 schema and representative data, then apply the
/// AddInstrumentsTable + MultiTickerFoundation migrations and verify that the
/// existing user data is preserved/transformed correctly.
///
/// MultiTickerMigrationTests (Task 9) only verifies forward correctness against
/// a fresh :memory: DB, so it can't catch:
///   - Trades.InstrumentId backfill silently failing on existing rows.
///   - FxRates rebuild's 'EUR'/'USD' literal substitution misfiring.
///   - Goals.FocusTicker drop accidentally removing other columns.
/// </summary>
public class MigrationBackwardCompatTests
{
    [Fact]
    public async Task Migrations_preserve_existing_user_data_through_full_upgrade_path()
    {
        var ct = TestContext.Current.CancellationToken;
        var dbPath = Path.Combine(Path.GetTempPath(),
            $"tradystrat-bwd-compat-{Guid.NewGuid()}.db");
        var connStr = $"Data Source={dbPath}";

        try
        {
            // ---- Phase 1: emit the Initial-migration schema directly ----
            // We emit the schema (and a __EFMigrationsHistory row marking
            // 20260506121400_Initial as applied) so EF Core's MigrateAsync in
            // Phase 3 will apply only the two pending migrations on top.
            await using (var conn = new SqliteConnection(connStr))
            {
                await conn.OpenAsync(ct);
                await ExecuteInitialSchemaAsync(conn, ct);

                // ---- Phase 2: insert pre-Phase-1 data ----
                await SeedPrePhase1DataAsync(conn, ct);
            }

            // ---- Phase 3: apply remaining migrations through EF ----
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connStr).Options;
            await using (var db = new AppDbContext(opts))
            {
                await db.Database.MigrateAsync(ct);
            }

            // ---- Phase 4: verify post-migration state ----
            await using (var verify = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connStr).Options))
            {
                // Instruments seeded by MultiTickerFoundation.
                var instruments = await verify.Instruments
                    .OrderBy(i => i.Ticker)
                    .ToListAsync(ct);
                instruments.Count.ShouldBe(3);
                instruments.Single(i => i.Ticker == "CON3.L").Kind.ShouldBe(InstrumentKind.Held);
                var con3Id = instruments.Single(i => i.Ticker == "CON3.L").Id;

                // Trades backfilled to CON3.L. Pre-existing rows must survive.
                var trades = await verify.Trades.ToListAsync(ct);
                trades.Count.ShouldBe(3);
                trades.ShouldAllBe(t => t.InstrumentId == con3Id);

                // FxRates: Base/Quote/Rate populated; Rate matches original UsdPerEur.
                var fxRates = await verify.FxRates
                    .OrderBy(r => r.Date)
                    .ToListAsync(ct);
                fxRates.Count.ShouldBe(5);
                fxRates.ShouldAllBe(r => r.Base == "EUR" && r.Quote == "USD");
                fxRates[0].Rate.ShouldBe(1.0500m);
                fxRates[4].Rate.ShouldBe(1.0900m);

                // Goal: TargetEur/TargetDate preserved; FocusTicker column gone.
                var goal = await verify.Goals.SingleAsync(ct);
                goal.TargetEur.ShouldBe(1_000_000m);
                goal.TargetDate.ShouldBe(new DateOnly(2026, 12, 31));
            }

            // Schema-level check that FocusTicker is truly dropped.
            await using (var conn = new SqliteConnection(connStr))
            {
                await conn.OpenAsync(ct);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA table_info(Goals);";
                var cols = new List<string>();
                await using var rdr = await cmd.ExecuteReaderAsync(ct);
                while (await rdr.ReadAsync(ct))
                    cols.Add(rdr.GetString(1));
                cols.ShouldNotContain("FocusTicker");
                cols.ShouldContain("TargetEur");
            }
        }
        finally
        {
            // Best effort. SQLite may keep the file briefly locked.
            try { File.Delete(dbPath); }
            catch { /* swallow */ }
        }
    }

    /// <summary>
    /// Emits the schema as it existed at the 20260506121400_Initial migration
    /// (before any Phase 1 changes). Mirrors the CREATE TABLE / CREATE INDEX
    /// statements emitted by the actual migration. Also seeds the
    /// __EFMigrationsHistory row so EF Core's MigrateAsync skips Initial and
    /// applies only the two pending migrations.
    /// </summary>
    private static async Task ExecuteInitialSchemaAsync(SqliteConnection conn, CancellationToken ct)
    {
        const string sql = @"
CREATE TABLE ""__EFMigrationsHistory"" (
    ""MigrationId"" TEXT NOT NULL CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY,
    ""ProductVersion"" TEXT NOT NULL
);

INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
VALUES ('20260506121400_Initial', '10.0.7');

CREATE TABLE ""FxRates"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_FxRates"" PRIMARY KEY AUTOINCREMENT,
    ""Date"" TEXT NOT NULL,
    ""Pair"" TEXT NOT NULL,
    ""UsdPerEur"" TEXT NOT NULL,
    ""FetchedAt"" TEXT NOT NULL
);

CREATE TABLE ""Goals"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Goals"" PRIMARY KEY,
    ""TargetEur"" TEXT NOT NULL,
    ""TargetDate"" TEXT NULL,
    ""FocusTicker"" TEXT NOT NULL,
    ""UpdatedAt"" TEXT NOT NULL
);

CREATE TABLE ""PriceBars"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_PriceBars"" PRIMARY KEY AUTOINCREMENT,
    ""Ticker"" TEXT NOT NULL,
    ""Date"" TEXT NOT NULL,
    ""Open"" TEXT NOT NULL,
    ""High"" TEXT NOT NULL,
    ""Low"" TEXT NOT NULL,
    ""Close"" TEXT NOT NULL,
    ""Volume"" INTEGER NOT NULL
);

CREATE TABLE ""Suggestions"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Suggestions"" PRIMARY KEY AUTOINCREMENT,
    ""ForDate"" TEXT NOT NULL,
    ""Action"" INTEGER NOT NULL,
    ""QuantityHint"" TEXT NULL,
    ""MaxPriceHint"" TEXT NULL,
    ""Conviction"" INTEGER NOT NULL,
    ""Rationale"" TEXT NOT NULL,
    ""CitationsJson"" TEXT NOT NULL,
    ""PromptHash"" TEXT NOT NULL,
    ""CreatedAt"" TEXT NOT NULL
);

CREATE TABLE ""Trades"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Trades"" PRIMARY KEY AUTOINCREMENT,
    ""ExecutedOn"" TEXT NOT NULL,
    ""Side"" INTEGER NOT NULL,
    ""Quantity"" TEXT NOT NULL,
    ""PricePerShare"" TEXT NOT NULL,
    ""FeesEur"" TEXT NOT NULL,
    ""Note"" TEXT NULL,
    ""CreatedAt"" TEXT NOT NULL
);

CREATE UNIQUE INDEX ""IX_FxRates_Pair_Date"" ON ""FxRates"" (""Pair"", ""Date"");
CREATE UNIQUE INDEX ""IX_PriceBars_Ticker_Date"" ON ""PriceBars"" (""Ticker"", ""Date"");
CREATE UNIQUE INDEX ""IX_Suggestions_ForDate"" ON ""Suggestions"" (""ForDate"");
CREATE INDEX ""IX_Trades_ExecutedOn"" ON ""Trades"" (""ExecutedOn"");
";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Seeds representative pre-Phase-1 user data: 3 trades, 5 EURUSD FX rates,
    /// 1 goal with FocusTicker='CON3.L'.
    /// </summary>
    private static async Task SeedPrePhase1DataAsync(SqliteConnection conn, CancellationToken ct)
    {
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
INSERT INTO Trades (ExecutedOn, Side, Quantity, PricePerShare, FeesEur, Note, CreatedAt) VALUES
  ('2026-04-02', 1, '42100.0', '1.65', '0.0', NULL, '2026-04-02T00:00:00Z'),
  ('2026-04-09', 1, '5700.0',  '0.35', '0.0', NULL, '2026-04-09T00:00:00Z'),
  ('2026-04-09', 1, '9850.0',  '0.41', '0.0', NULL, '2026-04-09T00:00:00Z');";
            await cmd.ExecuteNonQueryAsync(ct);
        }

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
INSERT INTO FxRates (Date, Pair, UsdPerEur, FetchedAt) VALUES
  ('2026-04-01', 'EURUSD', '1.0500', '2026-04-01T00:00:00Z'),
  ('2026-04-02', 'EURUSD', '1.0600', '2026-04-02T00:00:00Z'),
  ('2026-04-03', 'EURUSD', '1.0700', '2026-04-03T00:00:00Z'),
  ('2026-04-04', 'EURUSD', '1.0800', '2026-04-04T00:00:00Z'),
  ('2026-04-05', 'EURUSD', '1.0900', '2026-04-05T00:00:00Z');";
            await cmd.ExecuteNonQueryAsync(ct);
        }

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
INSERT INTO Goals (Id, TargetEur, TargetDate, FocusTicker, UpdatedAt)
VALUES (1, '1000000', '2026-12-31', 'CON3.L', '2026-04-01T00:00:00Z');";
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
