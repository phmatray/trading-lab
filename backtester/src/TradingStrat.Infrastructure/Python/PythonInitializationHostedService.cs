using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TradingStrat.Infrastructure.Python;

/// <summary>
/// Background service that initializes Python runtime on application startup.
/// Ensures Python environment is ready before any strategies execute.
/// Shuts down Python runtime gracefully on application stop.
/// </summary>
public class PythonInitializationHostedService : IHostedService
{
    private readonly PythonEnvironmentManager _pythonEnv;
    private readonly ILogger<PythonInitializationHostedService> _logger;

    public PythonInitializationHostedService(
        PythonEnvironmentManager pythonEnv,
        ILogger<PythonInitializationHostedService> logger)
    {
        _pythonEnv = pythonEnv ?? throw new ArgumentNullException(nameof(pythonEnv));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Called when the application starts.
    /// Initializes Python runtime synchronously to ensure it's ready before requests.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting Python initialization...");
            _pythonEnv.Initialize();
            _logger.LogInformation("Python initialization completed");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Python runtime during startup");
            // Don't throw - allow application to start even if Python fails
            // Python strategies will fail gracefully with helpful error messages
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Called when the application stops.
    /// Shuts down Python runtime gracefully.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Shutting down Python runtime...");
            _pythonEnv.Dispose();
            _logger.LogInformation("Python runtime shut down successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during Python runtime shutdown");
        }

        return Task.CompletedTask;
    }
}
