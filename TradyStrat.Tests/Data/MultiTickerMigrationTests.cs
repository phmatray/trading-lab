using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Data;
using Xunit;

namespace TradyStrat.Tests.Data;

public class MultiTickerMigrationTests
{
    // Ordinal sort: 'I' (0x49) < 'N' (0x4E), so COIN < CON3.L.
    private static readonly string[] ExpectedSeededTickers = ["BTC-USD", "COIN", "CON3.L"];

    [Fact]
    public async Task Migration_creates_Instruments_table_with_three_seeded_rows()
    {
        var ct = TestContext.Current.CancellationToken;
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn).Options;

        await using var db = new AppDbContext(opts);
        await db.Database.MigrateAsync(ct);

        var instruments = await db.Instruments.OrderBy(i => i.Ticker).ToListAsync(ct);
        instruments.Select(i => i.Ticker).ShouldBe(ExpectedSeededTickers);
        instruments.Single(i => i.Ticker == "CON3.L").Kind.ShouldBe(InstrumentKind.Held);
        instruments.Single(i => i.Ticker == "COIN").Kind.ShouldBe(InstrumentKind.Watchlist);
        instruments.Single(i => i.Ticker == "BTC-USD").Kind.ShouldBe(InstrumentKind.Watchlist);
    }

    [Fact]
    public async Task Goals_table_no_longer_has_FocusTicker_column()
    {
        var ct = TestContext.Current.CancellationToken;
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn).Options;

        await using var db = new AppDbContext(opts);
        await db.Database.MigrateAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA table_info(Goals);";
        var cols = new List<string>();
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
            cols.Add(rdr.GetString(1)); // column 1 is "name"

        cols.ShouldContain("TargetEur");
        cols.ShouldNotContain("FocusTicker");
    }

    [Fact]
    public async Task FxRates_table_has_Base_Quote_Rate_columns()
    {
        var ct = TestContext.Current.CancellationToken;
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn).Options;

        await using var db = new AppDbContext(opts);
        await db.Database.MigrateAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA table_info(FxRates);";
        var cols = new List<string>();
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
            cols.Add(rdr.GetString(1));

        cols.ShouldContain("Base");
        cols.ShouldContain("Quote");
        cols.ShouldContain("Rate");
        cols.ShouldNotContain("Pair");
        cols.ShouldNotContain("UsdPerEur");
    }
}
