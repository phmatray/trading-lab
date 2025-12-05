using Microsoft.EntityFrameworkCore;
using Spectre.Console;
using TradingStrat.Data;
using TradingStrat.Services;
using TradingStrat.Utilities;

// Display header
AnsiConsole.Write(
    new FigletText("Trading Strategy")
        .LeftJustified()
        .Color(Color.Cyan1));

AnsiConsole.MarkupLine("[grey]Historical Data Fetcher powered by Yahoo Finance[/]\n");

try
{
    TradingContext context = null!;

    // Step 1: Initialize database
    await AnsiConsole.Status()
        .StartAsync("Initializing database...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Dots);
            ctx.SpinnerStyle(Style.Parse("green"));

            context = new TradingContext();
            await context.Database.EnsureCreatedAsync();

            ctx.Status("[green]Database initialized successfully[/]");
            await Task.Delay(300);
        });

    AnsiConsole.MarkupLine("[green]✓[/] Database ready\n");

    // Step 2: Resolve ISIN to Yahoo ticker(s)
    const string isin = "XS2399367254";
    var possibleTickers = TickerResolver.GetAllTickersForIsin(isin);

    if (possibleTickers == null || !possibleTickers.Any())
    {
        AnsiConsole.MarkupLine($"[red]✗ ERROR:[/] Could not resolve ISIN {isin} to Yahoo ticker");
        return;
    }

    AnsiConsole.MarkupLine($"[yellow]ISIN:[/] {isin}");
    AnsiConsole.MarkupLine($"[yellow]Possible tickers:[/] [cyan]{string.Join(", ", possibleTickers)}[/]\n");

    // Step 3: Initialize services
    var yahooService = new YahooFinanceService();
    var repository = new DataRepository(context);
    var exportService = new ExportService();

    // Step 3.5: Try to find a working ticker
    string? ticker = null;

    await AnsiConsole.Status()
        .StartAsync("Testing ticker connections...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Dots);
            ctx.SpinnerStyle(Style.Parse("yellow"));

            foreach (var candidateTicker in possibleTickers)
            {
                ctx.Status($"Testing [cyan]{candidateTicker}[/]...");
                try
                {
                    var testData = await yahooService.GetHistoricalDataAsync(
                        candidateTicker,
                        DateTime.Today.AddDays(-7),
                        DateTime.Today);

                    if (testData.Any())
                    {
                        ticker = candidateTicker;
                        ctx.Status($"[green]Successfully connected with {ticker}[/]");
                        await Task.Delay(300);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[dim]  Failed with {candidateTicker}: {ex.Message.EscapeMarkup()}[/]");
                }
            }
        });

    if (ticker == null)
    {
        var panel = new Panel(
            "[red]Could not fetch data with any available ticker[/]\n\n" +
            "[yellow]This may be due to:[/]\n" +
            "• Yahoo Finance API rate limiting\n" +
            "• The security not being available\n" +
            "• Temporary API issues\n\n" +
            "[dim]Please try again later or verify at https://finance.yahoo.com/[/]")
        {
            Header = new PanelHeader("[red]Connection Error[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse("red")
        };

        AnsiConsole.Write(panel);
        return;
    }

    AnsiConsole.MarkupLine($"[green]✓[/] Connected to ticker [cyan]{ticker}[/]\n");

    // Step 4: Determine date range for fetching
    var latestDate = await repository.GetLatestDataDateAsync(ticker);
    var startDate = latestDate?.AddDays(1) ?? new DateTime(2021, 12, 10);
    var endDate = DateTime.Today;

    if (latestDate.HasValue)
    {
        AnsiConsole.MarkupLine($"[dim]Latest data in database: {latestDate:yyyy-MM-dd}[/]");

        if (startDate > endDate)
        {
            AnsiConsole.MarkupLine("[green]✓[/] Database is up to date\n");

            var existingSummary = await repository.GetDataSummaryAsync(ticker);
            DisplayDataSummary(existingSummary);

            await HandleExportAsync(repository, exportService, ticker);
            return;
        }

        AnsiConsole.MarkupLine($"[yellow]→[/] Fetching new data from [cyan]{startDate:yyyy-MM-dd}[/] to [cyan]{endDate:yyyy-MM-dd}[/]\n");
    }
    else
    {
        AnsiConsole.MarkupLine("[yellow]→[/] No existing data found. Fetching all historical data");
        AnsiConsole.MarkupLine($"[dim]Date range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}[/]\n");
    }

    // Step 5: Fetch data from Yahoo Finance
    var recordsBefore = await context.HistoricalPrices.CountAsync(p => p.Ticker == ticker);
    IReadOnlyList<HistoricalDataPoint> historicalData = [];

    await AnsiConsole.Status()
        .StartAsync("Fetching data from Yahoo Finance...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.BouncingBall);
            ctx.SpinnerStyle(Style.Parse("cyan"));

            historicalData = await yahooService.GetHistoricalDataAsync(ticker, startDate, endDate);

            ctx.Status($"[green]Retrieved {historicalData.Count} records[/]");
            await Task.Delay(500);
        });

    if (historicalData.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No new data available[/]\n");
        return;
    }

    AnsiConsole.MarkupLine($"[green]✓[/] Retrieved [cyan]{historicalData.Count}[/] records from Yahoo Finance\n");

    // Step 6: Save to database
    await AnsiConsole.Status()
        .StartAsync("Saving to database...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Dots);
            ctx.SpinnerStyle(Style.Parse("green"));

            await repository.SaveHistoricalDataAsync(ticker, isin, historicalData);

            ctx.Status("[green]Data saved successfully[/]");
            await Task.Delay(300);
        });

    var recordsAfter = await context.HistoricalPrices.CountAsync(p => p.Ticker == ticker);
    var newRecordsAdded = recordsAfter - recordsBefore;

    AnsiConsole.MarkupLine($"[green]✓[/] Saved [cyan]{newRecordsAdded}[/] new records to database\n");

    // Step 7: Display data summary
    var summary = await repository.GetDataSummaryAsync(ticker);
    var summaryWithNewRecords = summary with { NewRecords = newRecordsAdded };
    DisplayDataSummary(summaryWithNewRecords);

    // Step 8: Export option
    await HandleExportAsync(repository, exportService, ticker);
}
catch (Exception ex)
{
    var errorPanel = new Panel(
        $"[red]{ex.Message.EscapeMarkup()}[/]\n\n" +
        (ex.InnerException != null ? $"[dim]Details: {ex.InnerException.Message.EscapeMarkup()}[/]" : ""))
    {
        Header = new PanelHeader("[red bold]Error[/]"),
        Border = BoxBorder.Rounded,
        BorderStyle = Style.Parse("red")
    };

    AnsiConsole.Write(errorPanel);
    Environment.Exit(1);
}

static void DisplayDataSummary(DataSummary summary)
{
    AnsiConsole.Write(new Rule("[yellow]Data Summary[/]").LeftJustified());
    AnsiConsole.WriteLine();

    var table = new Table()
        .Border(TableBorder.Rounded)
        .BorderColor(Color.Grey)
        .AddColumn(new TableColumn("[yellow]Property[/]").LeftAligned())
        .AddColumn(new TableColumn("[cyan]Value[/]").RightAligned());

    table.AddRow("Ticker", $"[cyan]{summary.Ticker}[/]");

    if (summary.ISIN != null)
    {
        table.AddRow("ISIN", $"[dim]{summary.ISIN}[/]");
    }

    table.AddRow("Total Records", $"[cyan]{summary.TotalRecords:N0}[/]");

    if (summary.NewRecords > 0)
    {
        table.AddRow("New Records", $"[green]{summary.NewRecords:N0}[/]");
    }

    if (summary is { OldestDate: not null, LatestDate: not null })
    {
        table.AddRow("Date Range", $"[dim]{summary.OldestDate:yyyy-MM-dd}[/] → [cyan]{summary.LatestDate:yyyy-MM-dd}[/]");
    }

    if (summary is { MinPrice: not null, MaxPrice: not null })
    {
        table.AddRow("Price Range", $"[dim]${summary.MinPrice:F2}[/] → [cyan]${summary.MaxPrice:F2}[/]");
    }

    if (summary.LatestClose.HasValue)
    {
        table.AddRow("Latest Close", $"[green bold]${summary.LatestClose:F2}[/]");
    }

    AnsiConsole.Write(table);
    AnsiConsole.WriteLine();
}

static async Task HandleExportAsync(IDataRepository repository, IExportService exportService, string ticker)
{
    string exportChoice;

    if (!AnsiConsole.Profile.Capabilities.Interactive)
    {
        // Non-interactive mode: default to Skip
        exportChoice = "Skip";
        AnsiConsole.MarkupLine("[dim]Non-interactive mode: Export skipped[/]");
    }
    else
    {
        exportChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]Export data?[/]")
                .PageSize(10)
                .AddChoices("Skip", "CSV", "JSON", "Both (CSV + JSON)"));
    }

    if (exportChoice == "Skip")
    {
        return;
    }

    var data = await repository.GetHistoricalDataAsync(ticker);
    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

    await AnsiConsole.Status()
        .StartAsync("Exporting data...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Star);
            ctx.SpinnerStyle(Style.Parse("yellow"));

            switch (exportChoice)
            {
                case "CSV":
                    await exportService.ExportToCsvAsync(data, $"trading_{ticker}_{timestamp}.csv");
                    break;

                case "JSON":
                    await exportService.ExportToJsonAsync(data, $"trading_{ticker}_{timestamp}.json");
                    break;

                case "Both (CSV + JSON)":
                    await exportService.ExportToCsvAsync(data, $"trading_{ticker}_{timestamp}.csv");
                    await exportService.ExportToJsonAsync(data, $"trading_{ticker}_{timestamp}.json");
                    break;
            }

            await Task.Delay(300);
        });

    AnsiConsole.MarkupLine("[green]✓[/] Export completed successfully");
}