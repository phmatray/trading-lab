using Microsoft.EntityFrameworkCore;
using TradingStrat.Data;
using TradingStrat.Services;
using TradingStrat.Utilities;

Console.WriteLine("=== Trading Strategy - Historical Data Fetcher ===\n");

try
{
    // Step 1: Initialize database
    Console.WriteLine("Initializing database...");
    await using var context = new TradingContext();
    await context.Database.EnsureCreatedAsync();
    Console.WriteLine("Database initialized successfully.\n");

    // Step 2: Resolve ISIN to Yahoo ticker(s)
    const string isin = "XS2399367254";
    var possibleTickers = TickerResolver.GetAllTickersForIsin(isin);

    if (possibleTickers == null || !possibleTickers.Any())
    {
        Console.WriteLine($"ERROR: Could not resolve ISIN {isin} to Yahoo ticker");
        return;
    }

    Console.WriteLine($"Resolved ISIN {isin} to ticker(s): {string.Join(", ", possibleTickers)}");

    // Step 3: Initialize services
    var yahooService = new YahooFinanceService();
    var repository = new DataRepository(context);
    var exportService = new ExportService();

    // Step 3.5: Try to find a working ticker
    string? ticker = null;
    foreach (var candidateTicker in possibleTickers)
    {
        Console.WriteLine($"Trying ticker: {candidateTicker}...");
        try
        {
            // Try to fetch just one day of data to test if the ticker works
            var testData = await yahooService.GetHistoricalDataAsync(
                candidateTicker,
                DateTime.Today.AddDays(-7),
                DateTime.Today);

            if (testData.Any())
            {
                ticker = candidateTicker;
                Console.WriteLine($"Successfully connected with ticker: {ticker}\n");
                break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed with {candidateTicker}: {ex.Message}");
        }
    }

    if (ticker == null)
    {
        Console.WriteLine($"\nERROR: Could not fetch data with any available ticker for ISIN {isin}");
        Console.WriteLine("This may be due to:");
        Console.WriteLine("- Yahoo Finance API rate limiting");
        Console.WriteLine("- The security not being available on Yahoo Finance");
        Console.WriteLine("- Temporary Yahoo Finance API issues");
        Console.WriteLine("\nPlease try again later or verify the ticker manually at https://finance.yahoo.com/");
        return;
    }

    // Step 4: Determine date range for fetching
    var latestDate = await repository.GetLatestDataDateAsync(ticker);
    var startDate = latestDate?.AddDays(1) ?? new DateTime(2021, 12, 10); // Security launched on Dec 10, 2021
    var endDate = DateTime.Today;

    if (latestDate.HasValue)
    {
        Console.WriteLine($"Latest data in database: {latestDate:yyyy-MM-dd}");

        if (startDate > endDate)
        {
            Console.WriteLine("Database is up to date. No new data to fetch.\n");

            // Display summary of existing data
            var existingSummary = await repository.GetDataSummaryAsync(ticker);
            DisplayDataSummary(existingSummary);

            // Offer export option
            await HandleExportAsync(repository, exportService, ticker);
            return;
        }

        Console.WriteLine($"Fetching new data from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}...\n");
    }
    else
    {
        Console.WriteLine("No existing data found. Fetching all historical data...");
        Console.WriteLine($"Fetching historical data from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}...\n");
    }

    // Step 5: Fetch data from Yahoo Finance
    Console.WriteLine($"Connecting to Yahoo Finance API...");
    var recordsBefore = await context.HistoricalPrices.CountAsync(p => p.Ticker == ticker);

    var historicalData = await yahooService.GetHistoricalDataAsync(ticker, startDate, endDate);
    Console.WriteLine($"Retrieved {historicalData.Count} records from Yahoo Finance");

    if (historicalData.Count == 0)
    {
        Console.WriteLine("No new data available.\n");
        return;
    }

    // Step 6: Save to database
    Console.WriteLine("Saving to database...");
    await repository.SaveHistoricalDataAsync(ticker, isin, historicalData);

    var recordsAfter = await context.HistoricalPrices.CountAsync(p => p.Ticker == ticker);
    var newRecordsAdded = recordsAfter - recordsBefore;

    Console.WriteLine($"Successfully saved {newRecordsAdded} new records to database\n");

    // Step 7: Display data summary
    var summary = await repository.GetDataSummaryAsync(ticker);
    var summaryWithNewRecords = summary with { NewRecords = newRecordsAdded };
    DisplayDataSummary(summaryWithNewRecords);

    // Step 8: Export option
    await HandleExportAsync(repository, exportService, ticker);
}
catch (Exception ex)
{
    Console.WriteLine($"\nERROR: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Details: {ex.InnerException.Message}");
    }
    Environment.Exit(1);
}

static void DisplayDataSummary(DataSummary summary)
{
    Console.WriteLine("=== Data Summary ===");
    Console.WriteLine($"Ticker:        {summary.Ticker}{(summary.ISIN != null ? $" ({summary.ISIN})" : "")}");
    Console.WriteLine($"Total Records: {summary.TotalRecords:N0}");

    if (summary.NewRecords > 0)
    {
        Console.WriteLine($"New Records:   {summary.NewRecords:N0}");
    }

    if (summary.OldestDate.HasValue && summary.LatestDate.HasValue)
    {
        Console.WriteLine($"Date Range:    {summary.OldestDate:yyyy-MM-dd} to {summary.LatestDate:yyyy-MM-dd}");
    }

    if (summary.MinPrice.HasValue && summary.MaxPrice.HasValue)
    {
        Console.WriteLine($"Price Range:   ${summary.MinPrice:F2} - ${summary.MaxPrice:F2}");
    }

    if (summary.LatestClose.HasValue)
    {
        Console.WriteLine($"Latest Close:  ${summary.LatestClose:F2}");
    }

    Console.WriteLine();
}

static async Task HandleExportAsync(IDataRepository repository, IExportService exportService, string ticker)
{
    Console.Write("Export data? (CSV/JSON/Both/Skip) [Skip]: ");
    var exportChoice = Console.ReadLine()?.Trim().ToUpperInvariant() ?? "SKIP";

    if (exportChoice is "SKIP" or "S" or "")
    {
        Console.WriteLine("Export skipped.");
        return;
    }

    var data = await repository.GetHistoricalDataAsync(ticker);
    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

    switch (exportChoice)
    {
        case "CSV" or "C":
            await exportService.ExportToCsvAsync(data, $"trading_{ticker}_{timestamp}.csv");
            break;

        case "JSON" or "J":
            await exportService.ExportToJsonAsync(data, $"trading_{ticker}_{timestamp}.json");
            break;

        case "BOTH" or "B":
            await exportService.ExportToCsvAsync(data, $"trading_{ticker}_{timestamp}.csv");
            await exportService.ExportToJsonAsync(data, $"trading_{ticker}_{timestamp}.json");
            break;

        default:
            Console.WriteLine("Invalid choice. Export skipped.");
            break;
    }
}