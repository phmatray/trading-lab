using Spectre.Console;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Presentation.Console.Presenters;

public static class ParameterOptimizationPresenter
{
    public static void DisplayResults(ParameterOptimizationResult result)
    {
        DisplayHeader(result.Comparison);
        AnsiConsole.WriteLine();
        DisplayWinner(result.Comparison);
        AnsiConsole.WriteLine();
        DisplayMetricComparison(result.Comparison);
        AnsiConsole.WriteLine();
        DisplayRankingBreakdown(result.Comparison.Ranking);
        AnsiConsole.WriteLine();
        DisplayExecutionTime(result.ExecutionTime);
    }

    private static void DisplayHeader(StrategyComparison comparison)
    {
        Panel panel = new Panel(
            $"[bold cyan]A/B Parameter Optimization Results[/]\n\n" +
            $"[yellow]Ticker:[/] {comparison.Ticker}\n" +
            $"[yellow]Period:[/] {comparison.ResultA.StartDate:yyyy-MM-dd} to {comparison.ResultA.EndDate:yyyy-MM-dd}\n" +
            $"[yellow]Initial Capital:[/] ${comparison.ResultA.InitialCapital:N2}\n" +
            $"[yellow]Comparison Date:[/] {comparison.ComparisonDate:yyyy-MM-dd HH:mm:ss}")
        {
            Header = new PanelHeader("[cyan bold]Strategy Comparison[/]"),
            Border = BoxBorder.Double,
            BorderStyle = Style.Parse("cyan")
        };

        AnsiConsole.Write(panel);
    }

    private static void DisplayWinner(StrategyComparison comparison)
    {
        if (comparison.Winner == 0)
        {
            AnsiConsole.Write(new Panel(
                "[yellow bold]Results are too close to declare a clear winner (difference < 5%)[/]")
            {
                Border = BoxBorder.Rounded,
                BorderStyle = Style.Parse("yellow")
            });
        }
        else
        {
            StrategyVariant winnerVariant = comparison.WinningVariant!;
            string winnerColor = comparison.Winner == 1 ? "green" : "blue";
            decimal winnerScore = comparison.Winner == 1 ? comparison.Ranking.VariantAScore : comparison.Ranking.VariantBScore;
            decimal loserScore = comparison.Winner == 1 ? comparison.Ranking.VariantBScore : comparison.Ranking.VariantAScore;

            AnsiConsole.Write(new Panel(
                $"[{winnerColor} bold]WINNER: {winnerVariant.DisplayName}[/]\n\n" +
                $"Overall Score: [{winnerColor}]{winnerScore:F3}[/] vs [dim]{loserScore:F3}[/]")
            {
                Header = new PanelHeader($"[{winnerColor} bold]Winner[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = Style.Parse(winnerColor)
            });
        }
    }

    private static void DisplayMetricComparison(StrategyComparison comparison)
    {
        AnsiConsole.Write(new Rule("[yellow]Performance Metrics Comparison[/]").LeftJustified());
        AnsiConsole.WriteLine();

        Table table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[yellow]Metric[/]").LeftAligned())
            .AddColumn(new TableColumn($"[green]{comparison.VariantA.Label}[/]").RightAligned())
            .AddColumn(new TableColumn($"[blue]{comparison.VariantB.Label}[/]").RightAligned())
            .AddColumn(new TableColumn("[yellow]Difference[/]").RightAligned())
            .AddColumn(new TableColumn("[yellow]Better[/]").Centered());

        PerformanceMetrics metricsA = comparison.ResultA.Metrics;
        PerformanceMetrics metricsB = comparison.ResultB.Metrics;

        // Key ranking metrics (highlighted)
        table.AddRow(
            "[bold]Sharpe Ratio[/]",
            FormatMetric(metricsA.SharpeRatio, "F2"),
            FormatMetric(metricsB.SharpeRatio, "F2"),
            FormatDifference(metricsA.SharpeRatio - metricsB.SharpeRatio, "F2", "+"),
            FormatWinner(metricsA.SharpeRatio, metricsB.SharpeRatio, true));

        table.AddRow(
            "[bold]Annualized Return %[/]",
            FormatPercentage(metricsA.AnnualizedReturn),
            FormatPercentage(metricsB.AnnualizedReturn),
            FormatDifference(metricsA.AnnualizedReturn - metricsB.AnnualizedReturn, "F2", "+"),
            FormatWinner(metricsA.AnnualizedReturn, metricsB.AnnualizedReturn, true));

        table.AddRow(
            "[bold]Max Drawdown %[/]",
            FormatPercentage(metricsA.MaxDrawdownPercentage, isNegative: true),
            FormatPercentage(metricsB.MaxDrawdownPercentage, isNegative: true),
            FormatDifference(metricsA.MaxDrawdownPercentage - metricsB.MaxDrawdownPercentage, "F2", "+"),
            FormatWinner(metricsA.MaxDrawdownPercentage, metricsB.MaxDrawdownPercentage, false, isDrawdown: true));

        table.AddRow(
            "[bold]Win Rate %[/]",
            FormatPercentage(metricsA.WinRate),
            FormatPercentage(metricsB.WinRate),
            FormatDifference(metricsA.WinRate - metricsB.WinRate, "F2", "+"),
            FormatWinner(metricsA.WinRate, metricsB.WinRate, true));

        table.AddEmptyRow();

        // Additional metrics
        table.AddRow(
            "Total Return %",
            FormatPercentage(metricsA.TotalReturnPercentage),
            FormatPercentage(metricsB.TotalReturnPercentage),
            FormatDifference(metricsA.TotalReturnPercentage - metricsB.TotalReturnPercentage, "F2", "+"),
            FormatWinner(metricsA.TotalReturnPercentage, metricsB.TotalReturnPercentage, true));

        table.AddRow(
            "Profit Factor",
            FormatMetric(metricsA.ProfitFactor, "F2"),
            FormatMetric(metricsB.ProfitFactor, "F2"),
            FormatDifference(metricsA.ProfitFactor - metricsB.ProfitFactor, "F2", "+"),
            FormatWinner(metricsA.ProfitFactor, metricsB.ProfitFactor, true));

        table.AddRow(
            "Total Trades",
            FormatMetric(metricsA.TotalTrades, "N0"),
            FormatMetric(metricsB.TotalTrades, "N0"),
            FormatDifference(metricsA.TotalTrades - metricsB.TotalTrades, "N0", "+"),
            "[dim]-[/]");

        table.AddRow(
            "Volatility %",
            FormatPercentage(metricsA.Volatility),
            FormatPercentage(metricsB.Volatility),
            FormatDifference(metricsA.Volatility - metricsB.Volatility, "F2", "+"),
            FormatWinner(metricsA.Volatility, metricsB.Volatility, false));

        AnsiConsole.Write(table);
    }

    private static void DisplayRankingBreakdown(ComparisonRanking ranking)
    {
        AnsiConsole.Write(new Rule("[yellow]Ranking Score Breakdown[/]").LeftJustified());
        AnsiConsole.WriteLine();

        Table table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[yellow]Metric[/]").LeftAligned())
            .AddColumn(new TableColumn("[yellow]Weight[/]").RightAligned())
            .AddColumn(new TableColumn("[green]Variant A Points[/]").RightAligned())
            .AddColumn(new TableColumn("[blue]Variant B Points[/]").RightAligned());

        foreach ((string metricName, MetricComparison comparison) in ranking.MetricBreakdown)
        {
            if (comparison.VariantAPoints > 0 || comparison.VariantBPoints > 0)
            {
                decimal weight = comparison.VariantAPoints > 0
                    ? comparison.VariantAPoints
                    : comparison.VariantBPoints;

                table.AddRow(
                    metricName,
                    $"{weight * 100:F0}%",
                    comparison.VariantAPoints > 0 ? $"[green]{comparison.VariantAPoints:F2}[/]" : "[dim]0.00[/]",
                    comparison.VariantBPoints > 0 ? $"[blue]{comparison.VariantBPoints:F2}[/]" : "[dim]0.00[/]");
            }
        }

        table.AddEmptyRow();
        table.AddRow(
            "[bold]Total Score[/]",
            "",
            $"[green bold]{ranking.VariantAScore:F3}[/]",
            $"[blue bold]{ranking.VariantBScore:F3}[/]");

        AnsiConsole.Write(table);
    }

    private static void DisplayExecutionTime(TimeSpan executionTime)
    {
        AnsiConsole.MarkupLine($"[dim]Execution time: {executionTime.TotalSeconds:F1}s[/]");
    }

    private static string FormatMetric(decimal value, string format)
    {
        return $"[cyan]{value.ToString(format)}[/]";
    }

    private static string FormatMetric(int value, string format)
    {
        return $"[cyan]{value.ToString(format)}[/]";
    }

    private static string FormatPercentage(decimal value, bool isNegative = false)
    {
        string color = value > 0 && !isNegative ? "green" :
                       value < 0 || isNegative ? "red" : "dim";
        string sign = value > 0 && !isNegative ? "+" : "";
        return $"[{color}]{sign}{value:F2}%[/]";
    }

    private static string FormatDifference(decimal value, string format, string positiveSign = "")
    {
        if (value > 0)
        {
            return $"[green]{positiveSign}{value.ToString(format)}[/]";
        }
        else if (value < 0)
        {
            return $"[red]{value.ToString(format)}[/]";
        }
        else
        {
            return "[dim]0.00[/]";
        }
    }

    private static string FormatWinner(decimal valueA, decimal valueB, bool higherIsBetter, bool isDrawdown = false)
    {
        if (Math.Abs(valueA - valueB) < 0.01m)
        {
            return "[dim]-[/]";
        }

        // For drawdown (negative values), higher (closer to zero) is better
        if (isDrawdown)
        {
            return valueA > valueB ? "[green]A[/]" : "[blue]B[/]";
        }

        if (higherIsBetter)
        {
            return valueA > valueB ? "[green]A[/]" : "[blue]B[/]";
        }
        else
        {
            return valueA < valueB ? "[green]A[/]" : "[blue]B[/]";
        }
    }
}
