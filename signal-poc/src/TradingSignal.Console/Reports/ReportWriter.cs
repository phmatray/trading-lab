using System.Text.Json;

namespace TradingSignal.ConsoleApp.Reports;

public static class ReportWriter
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public static async Task WriteAsync(RunReport report, string path, CancellationToken ct)
    {
        string? dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await using FileStream fs = File.Create(path);
        await JsonSerializer.SerializeAsync(fs, report, Options, ct).ConfigureAwait(false);
    }

    public static async Task<RunReport> ReadAsync(string path, CancellationToken ct)
    {
        await using FileStream fs = File.OpenRead(path);
        RunReport? r = await JsonSerializer.DeserializeAsync<RunReport>(fs, Options, ct).ConfigureAwait(false);
        return r ?? throw new InvalidOperationException($"Could not deserialize report at {path}");
    }
}
