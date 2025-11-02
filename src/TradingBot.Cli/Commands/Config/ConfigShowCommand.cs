// <copyright file="ConfigShowCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Config;

/// <summary>
/// Command to display current configuration.
/// </summary>
public sealed class ConfigShowCommand : AsyncCommand
{
    private readonly IConfigurationService _configService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigShowCommand"/> class.
    /// </summary>
    /// <param name="configService">Configuration service.</param>
    public ConfigShowCommand(IConfigurationService configService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var config = await _configService.GetAllAsync();

        if (config.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No configuration values set.[/]");
            AnsiConsole.MarkupLine("[dim]Use 'config set' to add configuration values.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Key[/]")
            .AddColumn("[bold]Value[/]");

        foreach (var (key, value) in config.OrderBy(x => x.Key))
        {
            var displayValue = IsSensitiveKey(key) ? MaskValue(value) : value;
            table.AddRow(key, displayValue);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        if (config.Keys.Any(IsSensitiveKey))
        {
            AnsiConsole.MarkupLine("[dim]Note: Sensitive values (API keys, secrets) are masked.[/]");
        }

        return 0;
    }

    private static bool IsSensitiveKey(string key)
    {
        var lowerKey = key.ToLowerInvariant();
        return lowerKey.Contains("key") ||
               lowerKey.Contains("secret") ||
               lowerKey.Contains("password") ||
               lowerKey.Contains("token");
    }

    private static string MaskValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Length <= 4)
        {
            return new string('*', value.Length);
        }

        var visibleChars = Math.Min(4, value.Length / 4);
        var maskedLength = value.Length - visibleChars;

        return $"{new string('*', maskedLength)}{value.Substring(value.Length - visibleChars)}";
    }
}
