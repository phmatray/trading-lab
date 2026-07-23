![TradingBot banner](.github/banner.png)

# TradingBot

A powerful, extensible algorithmic trading platform built with .NET 10, featuring automated strategy execution, risk management, and comprehensive order handling. Includes a modern web dashboard built with Blazor Server using Domain-Driven Design patterns.

[![CI/CD](https://github.com/phmatray/TradingBot/actions/workflows/ci.yml/badge.svg)](https://github.com/phmatray/TradingBot/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/phmatray/TradingBot/branch/main/graph/badge.svg)](https://codecov.io/gh/phmatray/TradingBot)

## Features

### Core Trading Engine
- **Automated Strategy Execution**: Run multiple strategies simultaneously
- **Signal-to-Order Pipeline**: Automatic conversion of trading signals to orders
- **Position Management**: Real-time tracking of open positions and P&L
- **Risk Management**: Configurable risk limits and position sizing
- **Order Execution**: Simulated execution with slippage and commission modeling

### Built-in Strategies
- **Momentum Strategy**: Trend-following using moving average crossovers
- **Mean Reversion Strategy**: Counter-trend trading with Bollinger Bands and RSI

### Technical Indicators
- Simple Moving Average (SMA)
- Exponential Moving Average (EMA)
- Relative Strength Index (RSI)
- MACD (Moving Average Convergence Divergence)
- Bollinger Bands
- Average True Range (ATR)

### Market Data
- **Yahoo Finance Integration**: Real-time quote and historical data retrieval
- **Historical Data Cache**: Smart caching to reduce API calls
- **Multiple Timeframes**: Support for 1m, 5m, 15m, 1h, 4h, 1d, 1w, 1mo

### Risk Management
- Position size limits (% of equity)
- Daily loss limits
- Maximum drawdown limits
- Automatic stop-loss orders
- Automatic take-profit orders
- Leverage controls (1-10x)

### Web Dashboard
- **Real-time Trading Dashboard**: Live portfolio metrics, active positions, and recent trades with SignalR updates
- **Performance Analytics**: Equity curves, trade statistics, and comprehensive performance metrics (Sharpe ratio, max drawdown, win rate)
- **Strategy Management**: Enable/disable strategies, configure parameters, monitor performance
- **Backtesting Interface**: Run historical backtests and analyze results with detailed trade lists
- **Risk Settings**: Configure position sizing, stop-loss, take-profit, daily loss limits, and leverage
- **User Settings**: Customizable themes (light/dark), refresh intervals, and notification preferences
- **Responsive Design**: Desktop-optimized interface (minimum 1024px width)
- **Accessibility**: WCAG 2.1 Level AA compliant with full keyboard navigation
- **Domain-Driven Design**: Built using Ardalis.SharedKernel with domain events and aggregate patterns

## Architecture

Clean Architecture with Domain-Driven Design patterns:

```
TradingBot Web Application
├── Core Layer (Domain Models, Aggregates, Domain Events)
├── Strategy Engine (Technical Analysis & Signals)
├── Trading Engine (Order Execution & Portfolio)
├── Infrastructure (Data Access, Event Dispatching, Market Data)
└── Web (Blazor Server UI with SignalR)
```

**DDD Patterns**:
- Domain entities extend `EntityBase<T>` from Ardalis.SharedKernel
- Aggregates implement `IAggregateRoot` (Order, Position, Account, Trade)
- Domain events for state changes (OrderFilledEvent, PositionClosedEvent, etc.)
- MediatR for domain event dispatching
- Repository pattern using Ardalis.Specification

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later
- SQLite (bundled with .NET)

### Installation

```bash
# Clone the repository
git clone https://github.com/phmatray/TradingBot.git
cd TradingBot

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the Web Dashboard
dotnet run --project src/TradingBot.Web
# Navigate to https://localhost:5001 in your browser
```

### Configuration

Configuration is stored in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=tradingbot.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "TradingBot": "Debug"
    }
  },
  "TradingBot": {
    "InitialCapital": 100000,
    "DefaultLeverage": 1.0,
    "MaxPositionSize": 10.0,
    "StopLossPercent": 2.0,
    "TakeProfitPercent": 5.0
  }
}
```

## Usage

### Web Dashboard

Access the web dashboard at `https://localhost:5001` after running the application.

**Main Features**:
- **Dashboard** (`/`): Real-time account summary, active strategies, recent trades
- **Portfolio** (`/portfolio`): Open positions, trade history with advanced filters
- **Strategies** (`/strategies`): Enable/disable strategies, configure parameters
- **Performance** (`/performance`): Equity curves, trade statistics, performance metrics
- **Backtest** (`/backtest`): Run historical backtests and analyze results
- **Risk Settings** (`/risk-settings`): Configure position sizing, limits, stop-loss/take-profit
- **Settings** (`/settings`): Customize theme, refresh intervals, notifications

### Programmatic Usage

#### Register and Execute Strategies

```csharp
using TradingBot.Core.Interfaces;
using TradingBot.Engine;
using TradingBot.Strategies;

// Create strategy engine
var strategyEngine = serviceProvider.GetRequiredService<IStrategyEngine>();

// Register strategies
var momentumStrategy = new MomentumStrategy(
    name: "MomentumSPY",
    symbols: new[] { "SPY" },
    timeframe: "1d",
    shortPeriod: 10,
    longPeriod: 50);

strategyEngine.RegisterStrategy(momentumStrategy);

// Enable strategy
await strategyEngine.EnableStrategyAsync("MomentumSPY");

// Fetch market data
var marketDataService = serviceProvider.GetRequiredService<IMarketDataService>();
var candles = await marketDataService.GetHistoricalDataAsync(
    "SPY",
    DateTime.UtcNow.AddDays(-100),
    DateTime.UtcNow,
    "1d");

// Execute strategy (signals are automatically processed)
var signal = await strategyEngine.ExecuteStrategyAsync(
    "MomentumSPY",
    "SPY",
    candles);
```

#### Signal Processing Pipeline

The SignalProcessor automatically converts signals to orders:

```
Strategy Execution
    ↓
Signal Generated
    ↓
SignalProcessor (event subscriber)
    ↓
Position Sizing (based on risk settings & signal confidence)
    ↓
Risk Validation
    ↓
Order Submission
    ↓
Stop-Loss & Take-Profit Orders Created
    ↓
Portfolio Updated
```

#### Custom Strategy Development

```csharp
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Trading;

public class MyCustomStrategy : IStrategy
{
    public string Name => "MyCustomStrategy";
    public string Type => "Custom";
    public IReadOnlyList<string> Symbols { get; }
    public string Timeframe { get; }
    public bool IsEnabled { get; private set; }

    public async Task<Signal?> GenerateSignalAsync(
        string symbol,
        IReadOnlyList<Candle> data,
        CancellationToken cancellationToken = default)
    {
        // Your strategy logic here
        // Return Signal for buy/sell or null for hold

        if (/* buy condition */)
        {
            return new Signal
            {
                Id = Guid.NewGuid(),
                StrategyName = Name,
                Symbol = symbol,
                Type = SignalType.Buy,
                Timestamp = DateTime.UtcNow,
                Confidence = 0.85m, // 85% confidence
                SuggestedPrice = data.Last().Close
            };
        }

        return null;
    }

    // Implement other interface members...
}
```

## Database Schema

The application uses SQLite with Entity Framework Core 10:

- **Orders**: All submitted orders with status tracking (Aggregate Root)
- **Positions**: Open positions with real-time P&L (Aggregate Root)
- **Trades**: Closed trade history (Aggregate Root)
- **Candles**: Cached historical market data
- **Accounts**: Account state and equity tracking (Aggregate Root)
- **RiskSettings**: Risk management configuration (Aggregate Root)
- **BacktestResults**: Historical backtest results
- **UserPreferences**: User-specific settings (theme, notifications)

## Project Structure

```
TradingBot/
├── src/
│   ├── TradingBot.Core/              # Domain models, aggregates, domain events
│   ├── TradingBot.Engine/            # Trading engine and strategy execution
│   ├── TradingBot.Strategies/        # Built-in trading strategies
│   ├── TradingBot.Infrastructure/    # Data access, repositories, event dispatching
│   ├── TradingBot.Analytics/         # Performance analytics
│   └── TradingBot.Web/               # Blazor Server web dashboard (single entry point)
├── tests/                            # Unit and integration tests
├── specs/                            # Feature specifications
└── .github/workflows/                # CI/CD pipelines
```

## Development

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Code Quality

The project uses:
- **StyleCop Analyzers**: Code style enforcement
- **Roslyn Analyzers**: Code quality checks
- **SonarAnalyzer**: Security and code smell detection

```bash
# Run analyzers
dotnet build /p:RunAnalyzers=true
```

### CI/CD

GitHub Actions automatically:
- Builds on every push
- Runs tests with coverage reporting
- Performs code quality analysis
- Scans for security vulnerabilities

## Risk Warning

⚠️ **This software is for educational and research purposes only.**

Trading financial instruments carries a high level of risk and may not be suitable for all investors. Past performance is not indicative of future results. You should carefully consider your investment objectives, level of experience, and risk appetite before using this software.

The developers of this software are not responsible for any financial losses incurred through the use of this application.

## Roadmap

### Phase 1: Foundation ✅
- [x] Core domain models
- [x] Database infrastructure
- [x] Market data integration
- [x] Basic CLI framework

### Phase 2: Trading Engine ✅
- [x] Order execution service
- [x] Portfolio manager
- [x] Risk manager
- [x] Signal-to-order pipeline

### Phase 3: Strategy Framework ✅
- [x] Technical indicators library
- [x] Base strategy framework
- [x] Momentum strategy
- [x] Mean reversion strategy

### Phase 4: Advanced Features 🚧
- [ ] Live paper trading mode
- [ ] Backtesting engine
- [ ] Performance analytics
- [ ] Real-time dashboard
- [ ] Multiple broker integrations
- [ ] Machine learning strategies

### Phase 5: Production Ready 📅
- [ ] Comprehensive test coverage (80%+)
- [ ] Complete documentation
- [ ] Docker deployment
- [ ] Monitoring and alerts
- [ ] Multi-exchange support

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Market data provided by Yahoo Finance
- Technical indicator calculations inspired by TA-Lib
- Built with .NET 10 and Entity Framework Core 10
- DDD patterns from Ardalis.SharedKernel

## Support

For issues, questions, or suggestions:
- Open an issue on GitHub
- Check the [documentation](./docs/)
- Review the [specifications](./specs/)

---

**Built with ❤️ using .NET 10 and Claude Code**
