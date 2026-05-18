using Ardalis.Specification.EntityFrameworkCore;

namespace TradyStrat.Infrastructure.Data;

internal sealed class EfRepositoryShim<T>(AppDbContext db) : RepositoryBase<T>(db) where T : class { }
