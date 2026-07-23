using Microsoft.Extensions.Logging;
using TradingSignal.ConsoleApp.Configuration;
using TradingSignal.ConsoleApp.Reports;

namespace TradingSignal.ConsoleApp.Commands;

public sealed partial class ReportCommand(
    AppConfig config,
    ILogger<ReportCommand> logger)
{
    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        if (!File.Exists(config.Output.ReportPath))
        {
            LogMissingReport(logger, config.Output.ReportPath);
            return 2;
        }

        RunReport report = await ReportWriter.ReadAsync(config.Output.ReportPath, ct).ConfigureAwait(false);
        ReportPrinter.Print(report, Console.Out);
        return 0;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "No report at {Path} — run `run` first")]
    private static partial void LogMissingReport(ILogger logger, string path);
}
