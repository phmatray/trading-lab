// <copyright file="ConfigSetApiKeyCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Config;

/// <summary>
/// Command to set an API key with interactive, masked input.
/// </summary>
public sealed class ConfigSetApiKeyCommand : AsyncCommand<ConfigSetApiKeyCommand.Settings>
{
    private readonly IConfigurationService _configService;
    private readonly IEncryptionService _encryptionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigSetApiKeyCommand"/> class.
    /// </summary>
    /// <param name="configService">Configuration service.</param>
    /// <param name="encryptionService">Encryption service.</param>
    public ConfigSetApiKeyCommand(
        IConfigurationService configService,
        IEncryptionService encryptionService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            var provider = settings.Provider.ToLowerInvariant();

            AnsiConsole.MarkupLine($"[bold]Setting API key for provider:[/] [cyan]{provider}[/]");
            AnsiConsole.WriteLine();

            // Prompt for API key with masking
            var apiKey = AnsiConsole.Prompt(
                new TextPrompt<string>($"Enter API key for [cyan]{provider}[/]:")
                    .Secret());

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] API key cannot be empty.");
                return 1;
            }

            // Confirm the API key
            var confirm = AnsiConsole.Prompt(
                new TextPrompt<string>("Re-enter API key to confirm:")
                    .Secret());

            if (apiKey != confirm)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] API keys do not match.");
                return 1;
            }

            // Encrypt and save
            var encrypted = _encryptionService.Encrypt(apiKey);
            var configKey = $"ApiKeys:{provider}";

            await _configService.SetAsync(configKey, encrypted);

            AnsiConsole.MarkupLine($"[green]✓[/] API key for [cyan]{provider}[/] saved successfully (encrypted).");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Settings for the config set-api-key command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the API provider name.
        /// </summary>
        [CommandArgument(0, "<PROVIDER>")]
        [Description("API provider name (e.g., 'yahoo', 'alpaca', 'binance')")]
        public string Provider { get; set; } = string.Empty;
    }
}
