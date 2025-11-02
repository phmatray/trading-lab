// <copyright file="VersionCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace TradingBot.Cli.Commands;

/// <summary>
/// Command to display application version information.
/// </summary>
public sealed class VersionCommand : Command
{
    /// <inheritdoc/>
    public override int Execute(CommandContext context)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
        var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Property[/]")
            .AddColumn("[bold]Value[/]");

        table.AddRow("Product", fileVersionInfo.ProductName ?? "TradingBot CLI");
        table.AddRow("Version", version);
        table.AddRow("Framework", ".NET 9.0");
        table.AddRow("Copyright", fileVersionInfo.LegalCopyright ?? "© 2025 TradingBot");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[dim]For more information, visit: https://github.com/tradingbot/tradingbot-cli[/]");

        return 0;
    }
}
