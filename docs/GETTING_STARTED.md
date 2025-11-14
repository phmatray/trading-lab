# Getting Started with TradingBot CLI

This guide will walk you through setting up and using TradingBot CLI for algorithmic trading.

## Table of Contents
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [First Run](#first-run)
- [Tutorial 1: Running Your First Strategy](#tutorial-1-running-your-first-strategy)
- [Tutorial 2: Backtesting a Strategy](#tutorial-2-backtesting-a-strategy)
- [Tutorial 3: Managing Risk Settings](#tutorial-3-managing-risk-settings)
- [Tutorial 4: Viewing Performance](#tutorial-4-viewing-performance)
- [Tutorial 5: Creating a Custom Strategy](#tutorial-5-creating-a-custom-strategy)
- [Common Tasks](#common-tasks)
- [Troubleshooting](#troubleshooting)

## Prerequisites

Before you begin, ensure you have:

- **.NET 9.0 SDK** or later
  - Download from: https://dotnet.microsoft.com/download
  - Verify installation: `dotnet --version`

- **Git** (optional, for cloning the repository)
  - Download from: https://git-scm.com/downloads

- **Terminal/Command Prompt**
  - macOS/Linux: Terminal
  - Windows: PowerShell or Command Prompt

## Installation

### Option 1: Clone from GitHub

```bash
# Clone the repository
git clone https://github.com/phmatray/TradingBot.git
cd TradingBot

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Verify the build
dotnet test
```

### Option 2: Build from Source

1. Download and extract the source code
2. Open a terminal in the extracted directory
3. Run the installation commands:

```bash
dotnet restore
dotnet build
```

### Verify Installation

```bash
# Navigate to the CLI project
cd src/TradingBot.Cli

# Run the CLI
dotnet run -- version

# Expected output:
# TradingBot CLI v1.0.0
```

## First Run

### Initialize the Database

On first run, the application will automatically create a SQLite database:

```bash
dotnet run -- config show
```

This creates `tradingbot.db` in the application directory.

### Configure Basic Settings

Set your initial capital and risk parameters:

```bash
# Set initial capital to $10,000
dotnet run -- config set InitialCapital 10000

# Set maximum position size to 10% of equity
dotnet run -- config set MaxPositionSize 10.0

# Set stop-loss at 2%
dotnet run -- risk set-stoploss 2.0

# Set take-profit at 5%
dotnet run -- risk set-takeprofit 5.0
```

### View Current Configuration

```bash
dotnet run -- config show
```

## Tutorial 1: Running Your First Strategy

Let's run the built-in Momentum strategy on SPY (S&P 500 ETF).

### Step 1: List Available Strategies

```bash
dotnet run -- strategy list
```

You should see:
```
╭────────────┬──────────┬──────────┬─────────╮
│ Name       │ Type     │ Status   │ Symbols │
├────────────┼──────────┼──────────┼─────────┤
│ Momentum   │ Momentum │ Disabled │ SPY     │
│ MeanRev    │ MeanRev  │ Disabled │ AAPL    │
╰────────────┴──────────┴──────────┴─────────╯
```

### Step 2: Enable the Momentum Strategy

```bash
dotnet run -- strategy enable Momentum
```

### Step 3: Start the Strategy Engine

```bash
dotnet run -- strategy start
```

The engine will:
1. Fetch market data from Yahoo Finance
2. Calculate technical indicators (RSI, MACD, SMA)
3. Generate trading signals
4. Automatically execute orders based on signals

### Step 4: View the Dashboard

Open a new terminal and run:

```bash
dotnet run -- dashboard
```

You'll see a live dashboard showing:
- Account balance and equity
- Open positions
- Recent trades
- Performance metrics

### Step 5: Stop the Strategy

Press `Ctrl+C` in the strategy terminal or run:

```bash
dotnet run -- strategy stop
```

## Tutorial 2: Backtesting a Strategy

Test how a strategy would have performed historically.

### Step 1: Run a Backtest

```bash
dotnet run -- backtest run \
  --strategy Momentum \
  --symbols SPY \
  --start-date 2023-01-01 \
  --end-date 2023-12-31 \
  --capital 10000 \
  --commission 1.0 \
  --slippage 0.001
```

The backtest will show a progress bar and results summary:

```
Running backtest for Momentum on SPY...
[████████████████████████] 100%

Results:
  Total Return: +15.2%
  Sharpe Ratio: 1.45
  Max Drawdown: -5.3%
  Win Rate: 58.3%
  Total Trades: 24
```

### Step 2: Generate Detailed Report

```bash
dotnet run -- backtest report --backtest-id <id>
```

This creates an HTML report with:
- Equity curve chart
- Trade history table
- Performance metrics
- Drawdown analysis

### Step 3: Export Results

```bash
dotnet run -- performance export --format csv --output results.csv
```

## Tutorial 3: Managing Risk Settings

Proper risk management is crucial for profitable trading.

### View Current Risk Settings

```bash
dotnet run -- risk show
```

Output:
```
╭─────────────────┬────────╮
│ Setting         │ Value  │
├─────────────────┼────────┤
│ Leverage        │ 1.0x   │
│ Stop-Loss       │ 2.0%   │
│ Take-Profit     │ 5.0%   │
│ Max Position    │ 10.0%  │
│ Daily Loss Limit│ 3.0%   │
│ Max Drawdown    │ 15.0%  │
╰─────────────────┴────────╯
```

### Update Risk Settings

```bash
# Set leverage to 2x
dotnet run -- risk set-leverage 2.0

# Set stop-loss to 1.5%
dotnet run -- risk set-stoploss 1.5

# Set take-profit to 4%
dotnet run -- risk set-takeprofit 4.0

# Set max position size to 5%
dotnet run -- risk set-maxposition 5.0

# Set daily loss limit to 2%
dotnet run -- risk set-dailyloss 2.0

# Set max drawdown to 10%
dotnet run -- risk set-maxdrawdown 10.0
```

### Reset to Defaults

```bash
dotnet run -- risk reset
```

## Tutorial 4: Viewing Performance

Monitor your trading performance and analyze results.

### Show Current Portfolio

```bash
dotnet run -- portfolio show
```

Output:
```
╭────────┬──────┬─────────┬──────────┬──────────────┬──────────╮
│ Symbol │ Qty  │ Entry   │ Current  │     P&L      │ Strategy │
├────────┼──────┼─────────┼──────────┼──────────────┼──────────┤
│ SPY    │  10  │ 450.25  │  455.80  │ +$55.50 (+1.2%)│ Momentum│
│ AAPL   │  15  │ 185.50  │  188.30  │ +$42.00 (+1.5%)│ MeanRev │
╰────────┴──────┴─────────┴──────────┴──────────────┴──────────╯
Total P&L: +$97.50 (+1.35%)
```

### View Trade History

```bash
dotnet run -- portfolio history --days 30
```

### Show Performance Metrics

```bash
dotnet run -- performance show
```

Output:
```
╭────────────────────┬──────────╮
│ Metric             │ Value    │
├────────────────────┼──────────┤
│ Total Return       │ +15.2%   │
│ Annualized Return  │ +18.5%   │
│ Sharpe Ratio       │ 1.45     │
│ Sortino Ratio      │ 2.01     │
│ Max Drawdown       │ -5.3%    │
│ Win Rate           │ 58.3%    │
│ Profit Factor      │ 1.75     │
│ Total Trades       │ 24       │
│ Avg Win            │ +2.1%    │
│ Avg Loss           │ -1.2%    │
╰────────────────────┴──────────╯
```

### Export Performance Data

```bash
# Export to CSV
dotnet run -- performance export --format csv --output performance.csv

# Export to JSON
dotnet run -- performance export --format json --output performance.json

# Export to HTML report
dotnet run -- performance export --format html --output report.html
```

## Tutorial 5: Creating a Custom Strategy

Build your own trading strategy by implementing the `IStrategy` interface.

### Step 1: Create Strategy File

Create `src/TradingBot.Strategies/Implementations/MyStrategy.cs`:

```csharp
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.MarketData;
using TradingBot.Core.Models.Trading;
using TradingBot.Strategies.Base;
using TradingBot.Strategies.Indicators;
using Microsoft.Extensions.Logging;

namespace TradingBot.Strategies.Implementations;

public class MyStrategy : BaseStrategy
{
    private readonly int _shortPeriod;
    private readonly int _longPeriod;

    public MyStrategy(
        ILogger<MyStrategy> logger,
        string name,
        IReadOnlyList<string> symbols,
        string timeframe,
        int shortPeriod = 10,
        int longPeriod = 50)
        : base(logger, name, symbols, timeframe)
    {
        _shortPeriod = shortPeriod;
        _longPeriod = longPeriod;
    }

    public override string Type => "Custom";

    protected override async Task<Signal?> ExecuteStrategyLogicAsync(
        string symbol,
        IReadOnlyList<Candle> data,
        CancellationToken cancellationToken)
    {
        // Validate sufficient data
        if (data.Count < _longPeriod)
        {
            Logger.LogWarning("Insufficient data for {Strategy}: need {Required}, have {Actual}",
                Name, _longPeriod, data.Count);
            return null;
        }

        // Calculate indicators
        var shortMA = IndicatorLibrary.CalculateSMA(data, _shortPeriod);
        var longMA = IndicatorLibrary.CalculateSMA(data, _longPeriod);
        var currentPrice = data.Last().Close;

        // Generate signal
        Signal? signal = null;

        if (shortMA > longMA && currentPrice > shortMA)
        {
            // Bullish signal - short MA above long MA
            signal = new Signal
            {
                Id = Guid.NewGuid(),
                StrategyName = Name,
                Symbol = symbol,
                Type = SignalType.Buy,
                Timestamp = DateTime.UtcNow,
                Confidence = 0.75m,
                SuggestedPrice = currentPrice,
                Metadata = new Dictionary<string, object>
                {
                    ["ShortMA"] = shortMA,
                    ["LongMA"] = longMA,
                    ["Spread"] = shortMA - longMA
                }
            };

            Logger.LogInformation("Buy signal for {Symbol}: Price={Price}, ShortMA={ShortMA}, LongMA={LongMA}",
                symbol, currentPrice, shortMA, longMA);
        }
        else if (shortMA < longMA && currentPrice < shortMA)
        {
            // Bearish signal - short MA below long MA
            signal = new Signal
            {
                Id = Guid.NewGuid(),
                StrategyName = Name,
                Symbol = symbol,
                Type = SignalType.Sell,
                Timestamp = DateTime.UtcNow,
                Confidence = 0.75m,
                SuggestedPrice = currentPrice,
                Metadata = new Dictionary<string, object>
                {
                    ["ShortMA"] = shortMA,
                    ["LongMA"] = longMA,
                    ["Spread"] = longMA - shortMA
                }
            };

            Logger.LogInformation("Sell signal for {Symbol}: Price={Price}, ShortMA={ShortMA}, LongMA={LongMA}",
                symbol, currentPrice, shortMA, longMA);
        }

        return signal;
    }
}
```

### Step 2: Register Your Strategy

In your application startup or CLI command, register the strategy:

```csharp
var myStrategy = new MyStrategy(
    logger,
    name: "MyCustomStrategy",
    symbols: new[] { "TSLA", "NVDA" },
    timeframe: "1d",
    shortPeriod: 10,
    longPeriod: 50);

await strategyEngine.RegisterStrategyAsync(myStrategy);
```

### Step 3: Test Your Strategy

```bash
# Enable your strategy
dotnet run -- strategy enable MyCustomStrategy

# Backtest it
dotnet run -- backtest run \
  --strategy MyCustomStrategy \
  --symbols TSLA \
  --start-date 2023-01-01 \
  --end-date 2023-12-31 \
  --capital 10000
```

## Common Tasks

### Close All Positions

```bash
dotnet run -- portfolio close --all
```

### Close Specific Position

```bash
dotnet run -- portfolio close --symbol SPY
```

### Disable All Strategies

```bash
dotnet run -- strategy disable --all
```

### View Logs

Logs are written to `logs/tradingbot-YYYY-MM-DD.log`:

```bash
# View latest log file (Unix/macOS)
tail -f logs/tradingbot-$(date +%Y-%m-%d).log

# View latest log file (Windows PowerShell)
Get-Content -Path "logs\tradingbot-$(Get-Date -Format yyyy-MM-dd).log" -Wait
```

### Reset Configuration

```bash
# Backup current config
cp ~/.tradingbot/config.json ~/.tradingbot/config.json.backup

# Delete config (will be recreated with defaults)
rm ~/.tradingbot/config.json
```

## Troubleshooting

### "Database locked" Error

SQLite doesn't support concurrent writes. Ensure only one instance is running:

```bash
# Find running instances (Unix/macOS)
ps aux | grep TradingBot

# Kill if necessary
kill <pid>
```

### "Insufficient data" Warnings

The strategy needs enough historical candles. Wait for more data to accumulate or reduce indicator periods.

### Yahoo Finance API Errors

Rate limiting or network issues:
- The application retries automatically (3 attempts)
- If persistent, wait a few minutes before retrying
- Check your internet connection

### No Signals Generated

Possible reasons:
1. Strategy is disabled - enable it with `strategy enable`
2. Insufficient data - wait for more candles
3. Market conditions don't meet criteria - normal behavior

### Performance Issues

For faster backtests:
- Use fewer symbols
- Reduce date range
- Use daily timeframe instead of intraday

## Next Steps

Now that you're familiar with TradingBot CLI:

1. **Experiment** with different strategies and parameters
2. **Backtest** thoroughly before using real money
3. **Monitor** performance regularly
4. **Read** the [Architecture Guide](ARCHITECTURE.md) for deep dive
5. **Contribute** to the project (see [CONTRIBUTING.md](../CONTRIBUTING.md))

## Important Warnings

⚠️ **This software is for educational and research purposes only.**

- Always test strategies thoroughly with backtesting
- Start with small position sizes
- Never risk more than you can afford to lose
- Past performance does not guarantee future results
- The developers are not responsible for financial losses

## Support

- **Issues**: Report bugs on [GitHub Issues](https://github.com/phmatray/TradingBot/issues)
- **Discussions**: Ask questions in [GitHub Discussions](https://github.com/phmatray/TradingBot/discussions)
- **Documentation**: Check the [docs](.) folder

Happy Trading! 🚀
