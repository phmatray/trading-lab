using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Infrastructure.Data;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Data;

public class MultiTickerAiPhase2MigrationTests
{
    [Fact]
    public async Task Migration_creates_Suggestions_InstrumentId_and_swaps_unique_index()
    {
        await using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync(TestContext.Current.CancellationToken);
        var opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;

        await using var db = new AppDbContext(opts);
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

        // PRAGMA introspection — confirm the column is present and FK exists.
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info(Suggestions);";
            var cols = new List<string>();
            await using var rdr = await cmd.ExecuteReaderAsync(TestContext.Current.CancellationToken);
            while (await rdr.ReadAsync(TestContext.Current.CancellationToken))
                cols.Add(rdr.GetString(1));
            cols.ShouldContain("InstrumentId");
        }

        // Confirm the new composite unique index exists and the old one is gone.
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA index_list(Suggestions);";
            var indices = new List<(string Name, bool Unique)>();
            await using var rdr = await cmd.ExecuteReaderAsync(TestContext.Current.CancellationToken);
            while (await rdr.ReadAsync(TestContext.Current.CancellationToken))
                indices.Add((rdr.GetString(1), rdr.GetBoolean(2)));
            indices.ShouldContain(i => i.Name == "IX_Suggestions_ForDate_InstrumentId" && i.Unique);
            indices.ShouldNotContain(i => i.Name == "IX_Suggestions_ForDate");
        }
    }
}
