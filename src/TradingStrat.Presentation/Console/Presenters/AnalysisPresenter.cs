using Spectre.Console;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Presentation.Console.Presenters;

public static class AnalysisPresenter
{
    public static void DisplayAnalysis(LiveAnalysisResult result)
    {
        DisplayHeader(result);
        AnsiConsole.WriteLine();

        if (result.DataFreshnessWarning != null)
        {
            DisplayDataWarning(result.DataFreshnessWarning);
            AnsiConsole.WriteLine();
        }

        DisplayCurrentMarketData(result);
        AnsiConsole.WriteLine();

        DisplayPrediction(result);
        AnsiConsole.WriteLine();

        DisplayFeatureBreakdown(result.CurrentFeatures);
        AnsiConsole.WriteLine();

        DisplayModelMetadata(result);
    }

    private static void DisplayHeader(LiveAnalysisResult result)
    {
        var panel = new Panel(
            $"[bold cyan]Live ML Analysis[/]\n\n" +
            $"[yellow]Ticker:[/] {result.Ticker}\n" +
            $"[yellow]Analysis Time:[/] {result.AnalysisDateTime:yyyy-MM-dd HH:mm:ss}\n" +
            $"[yellow]Latest Data:[/] {result.LatestDataDate:yyyy-MM-dd}" +
            (result.IsDataFresh ? " [green]✓ FRESH[/]" : " [yellow]⚠ STALE[/]"))
        {
            Header = new PanelHeader("[cyan bold]ML POSITION ANALYSIS[/]"),
            Border = BoxBorder.Double,
            BorderStyle = Style.Parse("cyan")
        };

        AnsiConsole.Write(panel);
    }

    private static void DisplayDataWarning(string warning)
    {
        AnsiConsole.Write(new Panel($"[yellow]{warning.EscapeMarkup()}[/]")
        {
            Header = new PanelHeader("[yellow]⚠ Data Warning[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse("yellow")
        });
    }

    private static void DisplayCurrentMarketData(LiveAnalysisResult result)
    {
        AnsiConsole.Write(new Rule("[yellow]Current Market Data[/]").LeftJustified());
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[yellow]Metric[/]").LeftAligned())
            .AddColumn(new TableColumn("[cyan]Value[/]").RightAligned());

        table.AddRow("Current Price", $"[cyan bold]${result.CurrentPrice:F2}[/]");
        table.AddRow("Previous Close", $"[dim]${result.PreviousClose:F2}[/]");
        table.AddRow("Daily Change", FormatPriceChange(result.DailyChange));
        table.AddRow("Daily Change %", FormatPercentChange(result.DailyChangePercent));

        AnsiConsole.Write(table);
    }

    private static void DisplayPrediction(LiveAnalysisResult result)
    {
        AnsiConsole.Write(new Rule("[yellow]ML Prediction (Next Trading Day)[/]").LeftJustified());
        AnsiConsole.WriteLine();

        string signalColor = result.PredictedSignal switch
        {
            SignalType.Buy => "green",
            SignalType.Sell => "red",
            _ => "yellow"
        };

        string signalEmoji = result.PredictedSignal switch
        {
            SignalType.Buy => "📈",
            SignalType.Sell => "📉",
            _ => "➖"
        };

        var predictionPanel = new Panel(
            $"[{signalColor} bold]{signalEmoji} {result.PredictedSignal.ToString().ToUpper()}[/]\n\n" +
            $"[yellow]Predicted Return:[/] {FormatPredictedReturn(result.PredictedReturn)}\n" +
            $"[yellow]Confidence:[/] {CalculateConfidence(result.PredictedReturn)}\n\n" +
            $"[dim]{result.PredictionReason.EscapeMarkup()}[/]")
        {
            Header = new PanelHeader($"[{signalColor} bold]TRADING SIGNAL[/]"),
            Border = BoxBorder.Heavy,
            BorderStyle = Style.Parse(signalColor)
        };

        AnsiConsole.Write(predictionPanel);
    }

    private static void DisplayFeatureBreakdown(MarketFeatures features)
    {
        AnsiConsole.Write(new Rule("[yellow]Feature Analysis (26 Indicators)[/]").LeftJustified());
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[yellow]Category[/]").LeftAligned())
            .AddColumn(new TableColumn("[yellow]Feature[/]").LeftAligned())
            .AddColumn(new TableColumn("[cyan]Value[/]").RightAligned());

        // Price-based (5)
        table.AddRow("[bold]Price-Based[/]", "Daily Return", FormatFloat(features.DailyReturn, true));
        table.AddRow("", "Log Return", FormatFloat(features.LogReturn, true));
        table.AddRow("", "High-Low Range", FormatFloat(features.HighLowRange));
        table.AddRow("", "Open-Close Range", FormatFloat(features.OpenCloseRange, true));
        table.AddRow("", "Price Position", FormatFloat(features.PricePosition));

        // Moving Averages (6)
        table.AddEmptyRow();
        table.AddRow("[bold]Moving Averages[/]", "SMA 5", FormatFloat(features.SMA_5));
        table.AddRow("", "SMA 10", FormatFloat(features.SMA_10));
        table.AddRow("", "SMA 20", FormatFloat(features.SMA_20));
        table.AddRow("", "EMA 12", FormatFloat(features.EMA_12));
        table.AddRow("", "EMA 26", FormatFloat(features.EMA_26));
        table.AddRow("", "Price to SMA20", FormatFloat(features.PriceToSMA20, true));

        // Momentum (4)
        table.AddEmptyRow();
        table.AddRow("[bold]Momentum[/]", "RSI 14", FormatFloat(features.RSI_14));
        table.AddRow("", "Momentum 5", FormatFloat(features.Momentum_5));
        table.AddRow("", "ROC 10", FormatFloat(features.ROC_10, true));
        table.AddRow("", "Stochastic RSI", FormatFloat(features.StochRSI));

        // MACD (3)
        table.AddEmptyRow();
        table.AddRow("[bold]MACD[/]", "MACD Line", FormatFloat(features.MACD, true));
        table.AddRow("", "Signal Line", FormatFloat(features.MACDSignal, true));
        table.AddRow("", "Histogram", FormatFloat(features.MACDHistogram, true));

        // Volatility (4)
        table.AddEmptyRow();
        table.AddRow("[bold]Volatility[/]", "StdDev 10", FormatFloat(features.StdDev_10));
        table.AddRow("", "StdDev 20", FormatFloat(features.StdDev_20));
        table.AddRow("", "ATR 14", FormatFloat(features.ATR_14));
        table.AddRow("", "Bollinger Position", FormatFloat(features.BollingerPosition));

        // Volume (4)
        table.AddEmptyRow();
        table.AddRow("[bold]Volume[/]", "Volume Change", FormatFloat(features.VolumeChange, true));
        table.AddRow("", "Volume MA 10", FormatFloat(features.VolumeMA_10));
        table.AddRow("", "Volume Ratio", FormatFloat(features.VolumeRatio));
        table.AddRow("", "Price-Volume Corr", FormatFloat(features.PriceVolumeCorrelation, true));

        AnsiConsole.Write(table);
    }

    private static void DisplayModelMetadata(LiveAnalysisResult result)
    {
        AnsiConsole.Write(new Rule("[yellow]Model Information[/]").LeftJustified());
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine($"[yellow]Algorithm:[/] [cyan]FastTree Gradient Boosting[/]");
        AnsiConsole.MarkupLine($"[yellow]Training Data:[/] [cyan]{result.TrainingDataPoints:N0}[/] bars");
        AnsiConsole.MarkupLine($"[yellow]Training Period:[/] [cyan]{result.OldestTrainingDate:yyyy-MM-dd}[/] to [cyan]{result.LatestDataDate:yyyy-MM-dd}[/]");
        AnsiConsole.MarkupLine($"[yellow]Features:[/] [cyan]26[/] technical indicators");
    }

    private static string FormatPriceChange(decimal change)
    {
        if (change > 0)
        {
            return $"[green]+${change:F2}[/]";
        }
        else if (change < 0)
        {
            return $"[red]${change:F2}[/]";
        }
        else
        {
            return $"[dim]$0.00[/]";
        }
    }

    private static string FormatPercentChange(decimal percent)
    {
        if (percent > 0)
        {
            return $"[green]+{percent:F2}%[/]";
        }
        else if (percent < 0)
        {
            return $"[red]{percent:F2}%[/]";
        }
        else
        {
            return $"[dim]0.00%[/]";
        }
    }

    private static string FormatPredictedReturn(float returnValue)
    {
        float percent = returnValue * 100;
        if (percent > 0)
        {
            return $"[green bold]+{percent:F2}%[/]";
        }
        else if (percent < 0)
        {
            return $"[red bold]{percent:F2}%[/]";
        }
        else
        {
            return $"[yellow]0.00%[/]";
        }
    }

    private static string CalculateConfidence(float returnValue)
    {
        float absReturn = Math.Abs(returnValue);

        if (absReturn >= 0.02f)  // 2%+
        {
            return "[green bold]HIGH[/]";
        }
        else if (absReturn >= 0.01f)  // 1-2%
        {
            return "[yellow]MEDIUM[/]";
        }
        else if (absReturn >= 0.005f)  // 0.5-1%
        {
            return "[yellow]LOW[/]";
        }
        else
        {
            return "[red]VERY LOW[/]";
        }
    }

    private static string FormatFloat(float value, bool colorCode = false)
    {
        if (!colorCode)
        {
            return $"[cyan]{value:F4}[/]";
        }

        if (value > 0)
        {
            return $"[green]+{value:F4}[/]";
        }
        else if (value < 0)
        {
            return $"[red]{value:F4}[/]";
        }
        else
        {
            return $"[dim]{value:F4}[/]";
        }
    }
}
