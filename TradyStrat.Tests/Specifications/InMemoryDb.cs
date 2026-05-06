using Microsoft.EntityFrameworkCore;
using TradyStrat.Data;

namespace TradyStrat.Tests.Specifications;

public static class InMemoryDb
{
    public static AppDbContext Create()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"tradystrat-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(opts);
    }
}
