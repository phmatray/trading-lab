using Shouldly;
using TradyStrat.Data.Sqlite;
using Xunit;

namespace TradyStrat.Tests.Data;

public class SqlitePathResolverTests
{
    [Fact]
    public void Expand_replaces_tilde_with_user_home()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        SqlitePathResolver.Expand("~/db.sqlite").ShouldBe(Path.Combine(home, "db.sqlite"));
    }

    [Fact]
    public void Expand_returns_absolute_path_unchanged()
    {
        SqlitePathResolver.Expand("/var/data/db.sqlite").ShouldBe("/var/data/db.sqlite");
    }

    [Fact]
    public void Expand_throws_when_path_is_null_or_empty()
    {
        Should.Throw<ArgumentException>(() => SqlitePathResolver.Expand(""));
        Should.Throw<ArgumentException>(() => SqlitePathResolver.Expand(null!));
    }
}
