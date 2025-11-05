// <copyright file="ConfigSetCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Config;

/// <summary>
/// Command to set a configuration value.
/// </summary>
public sealed class ConfigSetCommand : AsyncCommand<ConfigSetCommand.Settings>
{
    private readonly IConfigurationService _configService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigSetCommand"/> class.
    /// </summary>
    /// <param name="configService">Configuration service.</param>
    public ConfigSetCommand(IConfigurationService configService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            await _configService.SetAsync(settings.Key, settings.Value);

            AnsiConsole.MarkupLine($"[green]✓[/] Configuration updated: [bold]{settings.Key}[/]");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Settings for the config set command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the configuration key.
        /// </summary>
        [CommandArgument(0, "<KEY>")]
        [Description("Configuration key to set")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the configuration value.
        /// </summary>
        [CommandArgument(1, "<VALUE>")]
        [Description("Configuration value")]
        public string Value { get; set; } = string.Empty;
    }
}
