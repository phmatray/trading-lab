namespace TradyStrat.Infrastructure.Data.Sqlite;

public static class SqlitePathResolver
{
    public static string Expand(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Database path is required.", nameof(raw));

        if (raw.StartsWith("~/", StringComparison.Ordinal) || raw == "~")
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return raw.Length == 1 ? home : Path.Combine(home, raw[2..]);
        }

        return raw;
    }
}
