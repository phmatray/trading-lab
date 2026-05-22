using TradyStrat.Domain.Suggestions;
using System.ComponentModel;
using Ardalis.Specification;
using Spectre.Console;
using Spectre.Console.Cli;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Application.Settings.Specifications;
using TradyStrat.Domain;

namespace TradyStrat.Cli.Commands;

internal sealed class ReplayCommand(
    ReplaySuggestionsUseCase useCase,
    IReadRepositoryBase<Instrument> instruments) : AsyncCommand<ReplayCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--instrument <TICKER>"), Description("Instrument ticker (e.g. CON3.L)")]
        public required string Instrument { get; init; }

        [CommandOption("--since <YYYY-MM-DD>"), Description("Inclusive start date (default: 90 days back from --until)")]
        public string? Since { get; init; }

        [CommandOption("--until <YYYY-MM-DD>"), Description("Inclusive end date (default: today UTC)")]
        public string? Until { get; init; }

        [CommandOption("--persist"), Description("Write replayed suggestions to DB (default: dry-run)")]
        public bool Persist { get; init; }

        [CommandOption("--force"), Description("With --persist, replace existing rows for the same (instrument, date)")]
        public bool Force { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ct = CancellationToken.None;

        var until = settings.Until is { } u
            ? DateOnly.Parse(u, System.Globalization.CultureInfo.InvariantCulture)
            : DateOnly.FromDateTime(DateTime.UtcNow);
        var since = settings.Since is { } s
            ? DateOnly.Parse(s, System.Globalization.CultureInfo.InvariantCulture)
            : until.AddDays(-90);

        var inst = await instruments.FirstOrDefaultAsync(new InstrumentByTickerSpec(settings.Instrument), ct);
        if (inst is null)
        {
            AnsiConsole.MarkupLine($"[red]Instrument not found:[/] {settings.Instrument}");
            return 2;
        }

        var report = await useCase.ExecuteAsync(
            new ReplaySuggestionsInput(inst.Id, since, until, settings.Persist, settings.Force), ct);

        Render(report, settings);
        return 0;
    }

    private static void Render(ReplayReport r, Settings s)
    {
        var table = new Table().AddColumns("Action", "Count", "Hit-rate %", "Avg fwd ret", "Avg convict");
        foreach (var action in Enum.GetValues<SuggestionAction>())
        {
            if (r.PerAction.TryGetValue(action, out var agg))
                table.AddRow(action.ToString(),
                    agg.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    $"{agg.HitRatePct:F1}",
                    $"{agg.AvgFwdReturnPct:+0.0;-0.0;0.0}%",
                    $"{agg.AvgConviction:F1}");
        }
        table.AddRow("[bold]Overall[/]",
            r.Overall.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            $"{r.Overall.HitRatePct:F1}",
            $"{r.Overall.AvgFwdReturnPct:+0.0;-0.0;0.0}%",
            $"{r.Overall.AvgConviction:F1}");
        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"Conviction-weighted score: [bold]{r.ConvictionWeightedScore:F2}[/]");
        AnsiConsole.MarkupLine($"Prompt versions: {string.Join(", ", r.DistinctPromptVersionHashes.Select(h => h.Length >= 8 ? h[..8] : h))} ({r.DistinctPromptVersionHashes.Count} distinct)");
        AnsiConsole.MarkupLine($"Range: {r.Since:yyyy-MM-dd} → {r.Until:yyyy-MM-dd} · Instrument {r.InstrumentId} · {(s.Persist ? "PERSIST" : "Dry-run")}");
    }
}
