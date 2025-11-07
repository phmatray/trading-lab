// <copyright file="Program.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.ResponseCompression;
using Serilog;
using TradingBot.Infrastructure.DependencyInjection;
using TradingBot.Web.Components;
using TradingBot.Web.Hubs;
using TradingBot.Web.Services;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/tradingbot-web-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add SignalR with MessagePack and performance tuning
builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        options.MaximumReceiveMessageSize = 102400; // 100 KB
        options.StreamBufferCapacity = 10;
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.HandshakeTimeout = TimeSpan.FromSeconds(15);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    })
    .AddMessagePackProtocol();

// Add response compression for SignalR
builder.Services.AddResponseCompression(options =>
{
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

// Add existing TradingBot services from Infrastructure layer
builder.Services.AddTradingBotServices(builder.Configuration);

// Add Web application services
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IPerformanceService, PerformanceService>();
builder.Services.AddScoped<IStrategyManagementService, StrategyManagementService>();
builder.Services.AddSingleton<IRiskSettingsService, RiskSettingsService>();
builder.Services.AddScoped<IBacktestService, BacktestService>();
builder.Services.AddSingleton<IToastService, ToastService>();

// Add real-time update service as hosted service
builder.Services.AddHostedService<RealtimeUpdateService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseResponseCompression();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map SignalR hub
app.MapHub<TradingHub>("/hubs/trading");

try
{
    Log.Information("Starting TradingBot Web application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
