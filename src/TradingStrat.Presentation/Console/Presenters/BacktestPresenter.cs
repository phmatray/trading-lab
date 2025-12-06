using Spectre.Console;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Presentation.Console.Presenters;

public static class BacktestPresenter
{
    public static void DisplayResults(BacktestResult result)
    {
        DisplayHeader(result);
        AnsiConsole.WriteLine();
        DisplayPerformanceSummary(result.Metrics);
        AnsiConsole.WriteLine();
        DisplayTradeHistory(result.Trades);
        AnsiConsole.WriteLine();
        DisplayEquityCurveSummary(result.EquityCurve);
    }

    private static void DisplayHeader(BacktestResult result)
    {
        var panel = new Panel(
            $"[bold]{result.StrategyName}[/]\n" +
            $"[dim]{result.StrategyDescription}[/]\n\n" +
            $"[yellow]Ticker:[/] {result.Ticker}\n" +
            $"[yellow]Period:[/] {result.StartDate:yyyy-MM-dd} to {result.EndDate:yyyy-MM-dd}\n" +
            $"[yellow]Initial Capital:[/] ${result.InitialCapital:N2}")
        {
            Header = new PanelHeader("[cyan]Backtest Results[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse("cyan")
        };

        AnsiConsole.Write(panel);
    }

    private static void DisplayPerformanceSummary(PerformanceMetrics metrics)
    {
        AnsiConsole.Write(new Rule("[yellow]Performance Summary[/]").LeftJustified());
        AnsiConsole.WriteLine();

        Table table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[yellow]Metric[/]").LeftAligned())
            .AddColumn(new TableColumn("[cyan]Value[/]").RightAligned());

        table.AddRow("Initial Capital", FormatCurrency(metrics.InitialCapital));
        table.AddRow("Final Equity", FormatCurrency(metrics.FinalEquity));
        table.AddRow("Total Return", FormatProfitLoss(metrics.TotalReturn));
        table.AddRow("Total Return %", FormatPercentage(metrics.TotalReturnPercentage));
        table.AddRow("Annualized Return", FormatPercentage(metrics.AnnualizedReturn));

        table.AddEmptyRow();
        table.AddRow("[bold]Trade Statistics[/]", "");
        table.AddRow("Total Trades", $"[cyan]{metrics.TotalTrades}[/]");
        table.AddRow("Winning Trades", $"[green]{metrics.WinningTrades}[/]");
        table.AddRow("Losing Trades", $"[red]{metrics.LosingTrades}[/]");
        table.AddRow("Win Rate", FormatPercentage(metrics.WinRate));

        table.AddEmptyRow();
        table.AddRow("[bold]Trade Analysis[/]", "");
        table.AddRow("Average Win", FormatCurrency(metrics.AverageWin));
        table.AddRow("Average Loss", FormatCurrency(metrics.AverageLoss));
        table.AddRow("Largest Win", FormatCurrency(metrics.LargestWin));
        table.AddRow("Largest Loss", FormatCurrency(metrics.LargestLoss));
        table.AddRow("Profit Factor", $"[cyan]{metrics.ProfitFactor:F2}[/]");

        table.AddEmptyRow();
        table.AddRow("[bold]Streaks[/]", "");
        table.AddRow("Max Consecutive Wins", $"[green]{metrics.MaxConsecutiveWins}[/]");
        table.AddRow("Max Consecutive Losses", $"[red]{metrics.MaxConsecutiveLosses}[/]");

        table.AddEmptyRow();
        table.AddRow("[bold]Risk Metrics[/]", "");
        table.AddRow("Max Drawdown", FormatCurrency(metrics.MaxDrawdown, true));
        table.AddRow("Max Drawdown %", FormatPercentage(metrics.MaxDrawdownPercentage, true));
        table.AddRow("Sharpe Ratio", $"[cyan]{metrics.SharpeRatio:F2}[/]");
        table.AddRow("Volatility", FormatPercentage(metrics.Volatility));

        table.AddEmptyRow();
        table.AddRow("[bold]Market Exposure[/]", "");
        table.AddRow("Total Days", $"[cyan]{metrics.TotalDays}[/]");
        table.AddRow("Days in Market", $"[cyan]{metrics.DaysInMarket}[/]");
        table.AddRow("Market Exposure", FormatPercentage(metrics.MarketExposurePercentage));

        AnsiConsole.Write(table);
    }

    private static void DisplayTradeHistory(List<Trade> trades)
    {
        if (trades.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No trades executed[/]");
            return;
        }

        AnsiConsole.Write(new Rule("[yellow]Trade History[/]").LeftJustified());
        AnsiConsole.WriteLine();

        Table table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[yellow]#[/]").RightAligned())
            .AddColumn(new TableColumn("[yellow]Date[/]").LeftAligned())
            .AddColumn(new TableColumn("[yellow]Type[/]").LeftAligned())
            .AddColumn(new TableColumn("[yellow]Price[/]").RightAligned())
            .AddColumn(new TableColumn("[yellow]Qty[/]").RightAligned())
            .AddColumn(new TableColumn("[yellow]Commission[/]").RightAligned())
            .AddColumn(new TableColumn("[yellow]P&L[/]").RightAligned())
            .AddColumn(new TableColumn("[yellow]Reason[/]").LeftAligned());

        List<Trade> tradesToDisplay = trades.Count <= 20
            ? trades
            : trades.Take(10).Concat(trades.Skip(trades.Count - 10)).ToList();

        int tradeNumber = 1;
        for (int i = 0; i < tradesToDisplay.Count; i++)
        {
            if (i == 10 && trades.Count > 20)
            {
                table.AddRow("[dim]...[/]", "[dim]...[/]", "[dim]...[/]", "[dim]...[/]", "[dim]...[/]", "[dim]...[/]", "[dim]...[/]", "[dim]...[/]");
                tradeNumber = trades.Count - 9;
                continue;
            }

            Trade trade = tradesToDisplay[i];
            string typeColor = trade.Type == TradeType.Buy ? "green" : "red";
            string plText = trade.ProfitLoss.HasValue ? FormatProfitLoss(trade.ProfitLoss.Value) : "[dim]-[/]";

            table.AddRow(
                $"[dim]{tradeNumber}[/]",
                $"[dim]{trade.DateTime:yyyy-MM-dd}[/]",
                $"[{typeColor}]{trade.Type}[/]",
                $"[cyan]${trade.Price:F2}[/]",
                $"[cyan]{trade.Quantity}[/]",
                $"[dim]${trade.Commission:F2}[/]",
                plText,
                $"[dim]{trade.Reason?.EscapeMarkup()}[/]"
            );
            tradeNumber++;
        }

        AnsiConsole.Write(table);

        if (trades.Count > 20)
        {
            AnsiConsole.MarkupLine($"[dim]Showing first 10 and last 10 of {trades.Count} total trades[/]");
        }
    }

    private static void DisplayEquityCurveSummary(List<EquityPoint> equityCurve)
    {
        if (equityCurve.Count == 0)
        {
            return;
        }

        AnsiConsole.Write(new Rule("[yellow]Equity Curve Summary[/]").LeftJustified());
        AnsiConsole.WriteLine();

        decimal startEquity = equityCurve[0].Equity;
        decimal endEquity = equityCurve[^1].Equity;
        decimal maxEquity = equityCurve.Max(e => e.Equity);
        decimal minEquity = equityCurve.Min(e => e.Equity);

        AnsiConsole.MarkupLine($"[yellow]Start:[/] {FormatCurrency(startEquity)}");
        AnsiConsole.MarkupLine($"[yellow]End:[/] {FormatCurrency(endEquity)}");
        AnsiConsole.MarkupLine($"[yellow]Peak:[/] {FormatCurrency(maxEquity)}");
        AnsiConsole.MarkupLine($"[yellow]Trough:[/] {FormatCurrency(minEquity)}");
    }

    private static string FormatCurrency(decimal value, bool isNegative = false)
    {
        string color = isNegative ? "red" : "cyan";
        return $"[{color}]${Math.Abs(value):N2}[/]";
    }

    private static string FormatProfitLoss(decimal value)
    {
        if (value > 0)
        {
            return $"[green]+${value:N2}[/]";
        }
        else if (value < 0)
        {
            return $"[red]-${Math.Abs(value):N2}[/]";
        }
        else
        {
            return $"[dim]$0.00[/]";
        }
    }

    private static string FormatPercentage(decimal value, bool isNegative = false)
    {
        string color = value > 0 && !isNegative ? "green" :
                    value < 0 || isNegative ? "red" : "dim";
        string sign = value > 0 && !isNegative ? "+" : value < 0 || isNegative ? "-" : "";
        return $"[{color}]{sign}{Math.Abs(value):F2}%[/]";
    }
}
