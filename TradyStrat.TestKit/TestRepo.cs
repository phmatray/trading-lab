using Ardalis.Specification.EntityFrameworkCore;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.TestKit;

/// <summary>
/// Generic test repository over an in-memory <see cref="AppDbContext"/>.
/// Reused across feature tests that need an Ardalis IRepositoryBase / IReadRepositoryBase.
/// </summary>
public sealed class TestRepo<T>(AppDbContext db) : RepositoryBase<T>(db) where T : class { }
