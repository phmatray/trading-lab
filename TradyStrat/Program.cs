using Microsoft.EntityFrameworkCore;
using TradyStrat.Data;
using TradyStrat.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite($"Data Source={SqlitePathResolver.Expand(builder.Configuration["Database:Path"]!)}"));

var app = builder.Build();
app.MapGet("/", () => "TradyStrat — bootstrap");
app.Run();
