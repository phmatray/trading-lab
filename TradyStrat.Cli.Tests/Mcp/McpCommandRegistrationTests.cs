using Shouldly;
using Spectre.Console.Cli;
using TradyStrat.Cli.Commands;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp;

public class McpCommandRegistrationTests
{
    [Fact]
    public void Mcp_command_registers_without_throwing()
    {
        var app = new CommandApp();
        Should.NotThrow(() =>
        {
            app.Configure(c => c.AddCommand<McpCommand>("mcp"));
        });
    }
}
