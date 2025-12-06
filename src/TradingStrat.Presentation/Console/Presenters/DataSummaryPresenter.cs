using Spectre.Console;
using TradingStrat.Application.Ports.Outbound;

namespace TradingStrat.Presentation.Console.Presenters;

public static class DataSummaryPresenter
{
    public static void Display(DataSummaryResult summary)
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
}
