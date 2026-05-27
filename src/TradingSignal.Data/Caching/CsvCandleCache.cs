using System.Globalization;
using System.Text;
using TradingSignal.Core;

namespace TradingSignal.Data.Caching;

// File-per-(symbol,interval) cache. On read returns the slice that covers the
// requested range; on write merges new candles with whatever is on disk and
// rewrites a deduplicated, sorted file. Time-range is filtered in-memory after
// loading — the on-disk artifact is the full known history for that pair.
public sealed class CsvCandleCache : ICandleCache
{
    private const string Header = "OpenTimeUtc,Open,High,Low,Close,Volume";
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    private readonly string _root;

    public CsvCandleCache(string root)
    {
        _root = root;
        Directory.CreateDirectory(_root);
    }

    public async Task<IReadOnlyList<Candle>?> TryReadAsync(
        string symbol, TimeSpan interval, DateTime startUtc, DateTime endUtc, CancellationToken ct)
    {
        var path = PathFor(symbol, interval);
        if (!File.Exists(path)) return null;

        await FileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var all = await ReadAllAsync(path, ct).ConfigureAwait(false);
            if (all.Count == 0) return null;
            if (all[0].OpenTimeUtc > startUtc || all[^1].OpenTimeUtc + interval < endUtc) return null;

            var slice = all.Where(c => c.OpenTimeUtc >= startUtc && c.OpenTimeUtc < endUtc).ToList();
            return slice;
        }
        finally
        {
            FileLock.Release();
        }
    }

    public async Task WriteAsync(
        string symbol, TimeSpan interval, IReadOnlyList<Candle> candles, CancellationToken ct)
    {
        if (candles.Count == 0) return;

        var path = PathFor(symbol, interval);
        await FileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var merged = File.Exists(path)
                ? MergeSorted(await ReadAllAsync(path, ct).ConfigureAwait(false), candles)
                : candles.OrderBy(c => c.OpenTimeUtc).ToList();

            await using var writer = new StreamWriter(path, append: false, Encoding.UTF8);
            await writer.WriteLineAsync(Header).ConfigureAwait(false);
            foreach (var c in merged)
            {
                await writer.WriteLineAsync(FormatRow(c).AsMemory(), ct).ConfigureAwait(false);
            }
        }
        finally
        {
            FileLock.Release();
        }
    }

    private string PathFor(string symbol, TimeSpan interval)
    {
        var intervalLabel = FormatInterval(interval);
        return Path.Combine(_root, $"{symbol}-{intervalLabel}.csv");
    }

    private static string FormatInterval(TimeSpan i)
    {
        if (i.TotalDays >= 1) return $"{(int)i.TotalDays}d";
        if (i.TotalHours >= 1) return $"{(int)i.TotalHours}h";
        return $"{(int)i.TotalMinutes}m";
    }

    private static async Task<List<Candle>> ReadAllAsync(string path, CancellationToken ct)
    {
        var result = new List<Candle>();
        using var reader = new StreamReader(path, Encoding.UTF8);
        var headerLine = await reader.ReadLineAsync(ct).ConfigureAwait(false);
        if (headerLine is null) return result;

        while (await reader.ReadLineAsync(ct).ConfigureAwait(false) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            result.Add(ParseRow(line));
        }
        return result;
    }

    private static Candle ParseRow(string line)
    {
        var parts = line.Split(',');
        if (parts.Length != 6) throw new FormatException($"Bad cache row: {line}");
        return new Candle(
            DateTime.Parse(parts[0], Inv, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
            decimal.Parse(parts[1], Inv),
            decimal.Parse(parts[2], Inv),
            decimal.Parse(parts[3], Inv),
            decimal.Parse(parts[4], Inv),
            decimal.Parse(parts[5], Inv));
    }

    private static string FormatRow(Candle c)
        => string.Create(Inv, $"{c.OpenTimeUtc:o},{c.Open},{c.High},{c.Low},{c.Close},{c.Volume}");

    private static List<Candle> MergeSorted(IReadOnlyList<Candle> existing, IReadOnlyList<Candle> incoming)
    {
        var seen = new SortedDictionary<DateTime, Candle>();
        foreach (var c in existing) seen[c.OpenTimeUtc] = c;
        foreach (var c in incoming) seen[c.OpenTimeUtc] = c; // incoming wins on dupes
        return seen.Values.ToList();
    }
}
