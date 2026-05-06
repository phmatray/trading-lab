using Ardalis.Specification.EntityFrameworkCore;
using TradyStrat.Data;

namespace TradyStrat.Tests.Fx;

/// <summary>
/// Generic test repository over an in-memory <see cref="AppDbContext"/>.
/// Reused across feature tests that need an Ardalis IRepositoryBase / IReadRepositoryBase.
/// </summary>
public sealed class TestRepo<T>(AppDbContext db) : RepositoryBase<T>(db) where T : class { }
