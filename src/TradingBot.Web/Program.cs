// <copyright file="Program.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.ResponseCompression;
using Serilog;
using TradingBot.Infrastructure.DependencyInjection;
using TradingBot.Web.Components;
using TradingBot.Web.Hubs;
using TradingBot.Web.Middleware;
using TradingBot.Web.Services;
using TradingBot.Web.Workers;

// Configure Serilog with structured logging and enrichment
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "TradingBot.Web")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/tradingbot-web-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.SignalR", Serilog.Events.LogEventLevel.Information)
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
builder.Services.AddScoped<IRiskSettingsService, RiskSettingsService>();
builder.Services.AddScoped<IBacktestService, BacktestService>();
builder.Services.AddScoped<WeeklyCashStrategyService>();
builder.Services.AddSingleton<IToastService, ToastService>();

// Add UI state and navigation services
builder.Services.AddScoped<UIStateService>();
builder.Services.AddScoped<NavigationService>();

// Add background task queue and worker for long-running operations (backtests)
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<BacktestExecutionWorker>();

// Add real-time update service as hosted service
builder.Services.AddHostedService<RealtimeUpdateService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseResponseCompression();

// Add correlation ID middleware for request tracking
app.UseMiddleware<CorrelationIdMiddleware>();

// Enable Serilog request logging with timing
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        var host = httpContext.Request.Host.Value ?? "unknown";
        var scheme = httpContext.Request.Scheme;
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        diagnosticContext.Set("RequestHost", host);
        diagnosticContext.Set("RequestScheme", scheme);
        diagnosticContext.Set("RemoteIpAddress", remoteIp);
    };
});

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
