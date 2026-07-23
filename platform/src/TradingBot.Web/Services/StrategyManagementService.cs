// <copyright file="StrategyManagementService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Configuration;
using TradingBot.Web.Hubs;
using TradingBot.Web.Models;

namespace TradingBot.Web.Services;

/// <summary>
/// Service for managing trading strategies.
/// </summary>
public sealed class StrategyManagementService : IStrategyManagementService
{
    private readonly IEnumerable<IStrategy> _strategies;
    private readonly IStrategyConfigurationRepository _configRepository;
    private readonly IHubContext<TradingHub, ITradingClient> _hubContext;
    private readonly ILogger<StrategyManagementService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyManagementService"/> class.
    /// </summary>
    /// <param name="strategies">The collection of registered strategies.</param>
    /// <param name="configRepository">The strategy configuration repository.</param>
    /// <param name="hubContext">The SignalR hub context.</param>
    /// <param name="logger">The logger instance.</param>
    public StrategyManagementService(
        IEnumerable<IStrategy> strategies,
        IStrategyConfigurationRepository configRepository,
        IHubContext<TradingHub, ITradingClient> hubContext,
        ILogger<StrategyManagementService> logger)
    {
        _strategies = strategies;
        _configRepository = configRepository;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all registered trading strategies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all strategies with current enabled status and parameters.</returns>
    public Task<List<IStrategy>> GetAllStrategiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading all strategies");

            var strategiesList = _strategies.ToList();

            _logger.LogInformation("Loaded {Count} strategies", strategiesList.Count);

            return Task.FromResult(strategiesList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading strategies");
            throw;
        }
    }

    /// <summary>
    /// Enables a strategy by name.
    /// </summary>
    /// <param name="strategyName">The name of the strategy to enable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if strategy was enabled successfully, false if strategy not found.</returns>
    public Task<bool> EnableStrategyAsync(string strategyName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Enabling strategy: {StrategyName}", strategyName);

            var strategy = _strategies.FirstOrDefault(s => s.Name == strategyName);

            if (strategy == null)
            {
                _logger.LogWarning("Strategy not found: {StrategyName}", strategyName);
                return Task.FromResult(false);
            }

            strategy.Enable();

            _logger.LogInformation("Strategy enabled: {StrategyName}", strategyName);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling strategy: {StrategyName}", strategyName);
            throw;
        }
    }

    /// <summary>
    /// Disables a strategy by name.
    /// </summary>
    /// <param name="strategyName">The name of the strategy to disable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if strategy was disabled successfully, false if strategy not found.</returns>
    public Task<bool> DisableStrategyAsync(string strategyName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Disabling strategy: {StrategyName}", strategyName);

            var strategy = _strategies.FirstOrDefault(s => s.Name == strategyName);

            if (strategy == null)
            {
                _logger.LogWarning("Strategy not found: {StrategyName}", strategyName);
                return Task.FromResult(false);
            }

            strategy.Disable();

            _logger.LogInformation("Strategy disabled: {StrategyName}", strategyName);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling strategy: {StrategyName}", strategyName);
            throw;
        }
    }

    /// <summary>
    /// Retrieves the configurable parameters for a strategy.
    /// </summary>
    /// <param name="strategyName">The name of the strategy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of parameter descriptors with current values, or empty list if strategy not found.</returns>
    public Task<List<StrategyParameterDto>> GetStrategyParametersAsync(
        string strategyName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving parameters for strategy: {StrategyName}", strategyName);

            var strategy = _strategies.FirstOrDefault(s => s.Name == strategyName);

            if (strategy == null)
            {
                _logger.LogWarning("Strategy not found: {StrategyName}", strategyName);
                return Task.FromResult(new List<StrategyParameterDto>());
            }

            var parameters = ExtractStrategyParameters(strategy);

            _logger.LogInformation(
                "Retrieved {Count} parameters for strategy {StrategyName}",
                parameters.Count,
                strategyName);

            return Task.FromResult(parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving parameters for strategy: {StrategyName}", strategyName);
            throw;
        }
    }

    /// <summary>
    /// Updates the configuration parameters for a strategy.
    /// </summary>
    /// <param name="strategyName">The name of the strategy to configure.</param>
    /// <param name="parameters">Dictionary of parameter names to new values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if configuration was saved successfully, false if strategy not found or validation failed.</returns>
    public async Task<bool> ConfigureStrategyAsync(
        string strategyName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Configuring strategy {StrategyName} with {Count} parameters",
                strategyName,
                parameters.Count);

            var strategy = _strategies.FirstOrDefault(s => s.Name == strategyName);

            if (strategy == null)
            {
                _logger.LogWarning("Strategy not found: {StrategyName}", strategyName);
                return false;
            }

            // Validate parameters against strategy schema
            var validationResult = ValidateParameters(strategy, parameters);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Invalid parameters for strategy {StrategyName}: {Error}",
                    strategyName,
                    validationResult.ErrorMessage);
                return false;
            }

            // Apply parameters to strategy instance
            if (!ApplyParametersToStrategy(strategy, parameters))
            {
                _logger.LogWarning("Failed to apply parameters to strategy {StrategyName}", strategyName);
                return false;
            }

            // Save configuration to database
            var configuration = new StrategyConfiguration
            {
                StrategyName = strategyName,
                ParametersJson = JsonSerializer.Serialize(parameters),
            };

            await _configRepository.UpsertAsync(configuration, cancellationToken);

            _logger.LogInformation("Successfully configured strategy {StrategyName}", strategyName);

            // Publish SignalR event
            await _hubContext.Clients.All.OnStrategyConfigurationChanged(strategyName, parameters);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring strategy: {StrategyName}", strategyName);
            throw;
        }
    }

    /// <summary>
    /// Resets a strategy's configuration to default values.
    /// </summary>
    /// <param name="strategyName">The name of the strategy to reset.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if configuration was reset successfully, false if strategy not found.</returns>
    public async Task<bool> ResetStrategyToDefaultsAsync(
        string strategyName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Resetting strategy to defaults: {StrategyName}", strategyName);

            var strategy = _strategies.FirstOrDefault(s => s.Name == strategyName);

            if (strategy == null)
            {
                _logger.LogWarning("Strategy not found: {StrategyName}", strategyName);
                return false;
            }

            // Delete configuration from database
            await _configRepository.DeleteAsync(strategyName, cancellationToken);

            // Reset strategy to default values by re-initializing
            await strategy.InitializeAsync(cancellationToken);

            _logger.LogInformation("Successfully reset strategy {StrategyName} to defaults", strategyName);

            // Publish SignalR event with empty parameters to indicate reset
            await _hubContext.Clients.All.OnStrategyConfigurationChanged(strategyName, new Dictionary<string, object>());

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting strategy to defaults: {StrategyName}", strategyName);
            throw;
        }
    }

    private static string GetParameterType(Type type)
    {
        if (type == typeof(int))
        {
            return "int";
        }

        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
        {
            return "decimal";
        }

        if (type == typeof(bool))
        {
            return "bool";
        }

        return "string";
    }

    private static object GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type) ?? 0;
        }

        return string.Empty;
    }

    private static string SplitCamelCase(string input)
    {
        return System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1").Trim();
    }

    private static string GetParameterDescription(string paramName, string strategyType)
    {
        // Provide helpful descriptions based on common parameter names
        return paramName switch
        {
            "RsiPeriod" => "Number of periods for RSI calculation",
            "RsiOversold" => "RSI threshold for oversold condition (0-100)",
            "RsiOverbought" => "RSI threshold for overbought condition (0-100)",
            "MacdFast" => "Fast EMA period for MACD calculation",
            "MacdSlow" => "Slow EMA period for MACD calculation",
            "MacdSignal" => "Signal line EMA period for MACD",
            "SmaPeriod" or "FastPeriod" or "SlowPeriod" => $"{SplitCamelCase(paramName)} for moving average",
            "BollingerPeriod" => "Bollinger Bands SMA period",
            "BollingerStdDev" => "Bollinger Bands standard deviation multiplier",
            "AtrPeriod" => "Average True Range period",
            _ => $"Configure {SplitCamelCase(paramName)} for {strategyType} strategy",
        };
    }

#pragma warning disable S1871 // Two branches in a conditional structure should not have exactly the same implementation
    private static void SetNumericBounds(StrategyParameterDto parameter, string paramName, string strategyType)
    {
        // Set reasonable bounds based on parameter name
        switch (paramName)
        {
            case "RsiPeriod":
            case "MacdFast":
                parameter.MinValue = 2;
                parameter.MaxValue = 50;
                break;
            case "RsiOversold":
                parameter.MinValue = 0;
                parameter.MaxValue = 50;
                break;
            case "RsiOverbought":
                parameter.MinValue = 50;
                parameter.MaxValue = 100;
                break;
            case "MacdSlow":
                parameter.MinValue = 10;
                parameter.MaxValue = 100;
                break;
            case "MacdSignal":
            case "SmaPeriod":
            case "FastPeriod":
            case "SlowPeriod":
            case "BollingerPeriod":
            case "AtrPeriod":
                parameter.MinValue = 2;
                parameter.MaxValue = 200;
                break;
            case "BollingerStdDev":
                parameter.MinValue = 0.5m;
                parameter.MaxValue = 5.0m;
                break;
            default:
                // Default bounds for unknown numeric parameters
                if (parameter.ParameterType == "int")
                {
                    parameter.MinValue = 1;
                    parameter.MaxValue = 1000;
                }
                else if (parameter.ParameterType == "decimal")
                {
                    parameter.MinValue = 0m;
                    parameter.MaxValue = 100m;
                }

                break;
        }
    }
#pragma warning restore S1871 // Two branches in a conditional structure should not have exactly the same implementation

#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
    private List<StrategyParameterDto> ExtractStrategyParameters(IStrategy strategy)
    {
        var parameters = new List<StrategyParameterDto>();

        // Use reflection to get the config field from the strategy instance
        var configField = strategy.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(f => f.Name.Contains("config", StringComparison.OrdinalIgnoreCase));

        if (configField == null)
        {
            _logger.LogWarning("No config field found for strategy {StrategyName}", strategy.Name);
            return parameters;
        }

        var config = configField.GetValue(strategy);
        if (config == null)
        {
            _logger.LogWarning("Config field is null for strategy {StrategyName}", strategy.Name);
            return parameters;
        }

        // Get all properties from the config object, excluding common properties
        var configProperties = config.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite &&
                        p.Name != "Name" && p.Name != "Enabled" &&
                        p.Name != "Symbols" && p.Name != "Timeframe");

        foreach (var prop in configProperties)
        {
            var currentValue = prop.GetValue(config);
            var paramType = GetParameterType(prop.PropertyType);

            var parameter = new StrategyParameterDto
            {
                ParameterName = prop.Name,
                DisplayName = SplitCamelCase(prop.Name),
                Description = GetParameterDescription(prop.Name, strategy.Type),
                ParameterType = paramType,
                CurrentValue = currentValue ?? GetDefaultValue(prop.PropertyType),
            };

            // Set min/max values for numeric types
            if (paramType == "int" || paramType == "decimal")
            {
                SetNumericBounds(parameter, prop.Name, strategy.Type);
            }

            parameters.Add(parameter);
        }

        return parameters;
    }

    private bool ApplyParametersToStrategy(IStrategy strategy, Dictionary<string, object> parameters)
    {
        try
        {
            var configField = strategy.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(f => f.Name.Contains("config", StringComparison.OrdinalIgnoreCase));

            if (configField == null)
            {
                return false;
            }

            var config = configField.GetValue(strategy);
            if (config == null)
            {
                return false;
            }

            foreach (var kvp in parameters)
            {
                var property = config.GetType().GetProperty(kvp.Key);
                if (property != null && property.CanWrite)
                {
                    var convertedValue = Convert.ChangeType(kvp.Value, property.PropertyType);
                    property.SetValue(config, convertedValue);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying parameters to strategy {StrategyName}", strategy.Name);
            return false;
        }
    }
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

    private (bool IsValid, string? ErrorMessage) ValidateParameters(
        IStrategy strategy,
        Dictionary<string, object> parameters)
    {
        var validParams = ExtractStrategyParameters(strategy);

        foreach (var kvp in parameters)
        {
            var validParam = validParams.FirstOrDefault(p => p.ParameterName == kvp.Key);
            if (validParam == null)
            {
                return (false, $"Unknown parameter: {kvp.Key}");
            }

            // Validate numeric bounds
            if (validParam.MinValue != null && validParam.MaxValue != null)
            {
                if (validParam.ParameterType == "int")
                {
                    if (kvp.Value is not int intValue)
                    {
                        return (false, $"Parameter {kvp.Key} must be an integer");
                    }

                    var min = Convert.ToInt32(validParam.MinValue);
                    var max = Convert.ToInt32(validParam.MaxValue);

                    if (intValue < min || intValue > max)
                    {
                        return (false, $"Parameter {kvp.Key} must be between {min} and {max}");
                    }
                }
                else if (validParam.ParameterType == "decimal")
                {
                    if (kvp.Value is not decimal and not double and not float)
                    {
                        return (false, $"Parameter {kvp.Key} must be a number");
                    }

                    var decValue = Convert.ToDecimal(kvp.Value);
                    var min = Convert.ToDecimal(validParam.MinValue);
                    var max = Convert.ToDecimal(validParam.MaxValue);

                    if (decValue < min || decValue > max)
                    {
                        return (false, $"Parameter {kvp.Key} must be between {min} and {max}");
                    }
                }
            }
        }

        return (true, null);
    }
}
