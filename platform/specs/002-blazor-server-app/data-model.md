# Data Model: Blazor Server Trading Dashboard

**Feature**: 002-blazor-server-app
**Date**: 2025-11-07
**Purpose**: Define data models, DTOs, and view models for the Blazor web application

## Overview

The Blazor Server application will primarily use existing domain models from TradingBot.Core with minimal additional view models for UI-specific concerns. This document outlines how existing entities are mapped to the web interface and identifies any new view models needed.

---

## Existing Domain Models (TradingBot.Core)

These models are already defined and will be reused unchanged:

### Account

**Source**: `TradingBot.Core.Models.Account`

**Purpose**: Represents trading account state and equity

**Fields**:
- `AccountId` (Guid): Unique account identifier
- `Equity` (decimal): Total account value (cash + position value)
- `Cash` (decimal): Available cash balance
- `PositionValue` (decimal): Total value of open positions
- `BuyingPower` (decimal): Available buying power (cash × leverage)
- `UnrealizedPnL` (decimal): Total unrealized profit/loss from open positions
- `RealizedPnL` (decimal): Total realized profit/loss from closed trades
- `CreatedAt` (DateTime): Account creation timestamp
- `UpdatedAt` (DateTime): Last update timestamp

**Validation Rules**:
- Equity must be non-negative
- Cash must be non-negative
- Buying power calculated as Cash × Leverage

**State Transitions**: N/A (continuously updated)

**Usage in UI**:
- Dashboard: Account Summary widget
- All pages: Top navigation bar (equity display)

---

### Position

**Source**: `TradingBot.Core.Models.Position`

**Purpose**: Represents an open trading position

**Fields**:
- `PositionId` (Guid): Unique position identifier
- `AccountId` (Guid): Associated account
- `Symbol` (string): Trading symbol (e.g., "AAPL", "TSLA")
- `Side` (PositionSide): Buy or Sell (SmartEnum)
- `Quantity` (decimal): Number of shares/contracts
- `EntryPrice` (decimal): Average entry price
- `CurrentPrice` (decimal): Current market price
- `UnrealizedPnL` (decimal): Calculated unrealized P&L
- `UnrealizedPnLPercent` (decimal): P&L as percentage of position value
- `StrategyName` (string?): Name of strategy that opened position
- `OpenedAt` (DateTime): Position open timestamp
- `UpdatedAt` (DateTime): Last price update timestamp

**Relationships**:
- BelongsTo: Account (via AccountId)
- AssociatedWith: Strategy (via StrategyName)

**Validation Rules**:
- Quantity must be positive
- Entry price must be positive
- Symbol must be valid ticker format

**Calculated Fields**:
- UnrealizedPnL = (CurrentPrice - EntryPrice) × Quantity × Side.Multiplier
- UnrealizedPnLPercent = (UnrealizedPnL / (EntryPrice × Quantity)) × 100

**Usage in UI**:
- Dashboard: Position List widget (top 10 positions)
- Portfolio page: All open positions with close action

---

### Trade

**Source**: `TradingBot.Core.Models.Trade`

**Purpose**: Represents a completed trade (closed position)

**Fields**:
- `TradeId` (Guid): Unique trade identifier
- `AccountId` (Guid): Associated account
- `Symbol` (string): Trading symbol
- `Side` (OrderSide): Buy or Sell (SmartEnum)
- `Quantity` (decimal): Number of shares/contracts
- `EntryPrice` (decimal): Entry price
- `ExitPrice` (decimal): Exit price
- `EntryTime` (DateTime): Position opened timestamp
- `ExitTime` (DateTime): Position closed timestamp
- `RealizedPnL` (decimal): Realized profit/loss
- `RealizedPnLPercent` (decimal): P&L as percentage
- `Commission` (decimal): Total commission paid
- `StrategyName` (string?): Strategy that executed trade
- `Duration` (TimeSpan): Time between entry and exit

**Relationships**:
- BelongsTo: Account (via AccountId)
- AssociatedWith: Strategy (via StrategyName)

**Validation Rules**:
- Exit time must be after entry time
- Entry and exit prices must be positive

**Calculated Fields**:
- RealizedPnL = (ExitPrice - EntryPrice) × Quantity × Side.Multiplier - Commission
- RealizedPnLPercent = (RealizedPnL / (EntryPrice × Quantity)) × 100
- Duration = ExitTime - EntryTime

**Usage in UI**:
- Dashboard: Recent Trades widget (last 5 trades)
- Portfolio page: Complete trade history with filtering
- Performance page: Trade statistics analysis

---

### Strategy

**Source**: `TradingBot.Core.Models.Strategy` (interface: `IStrategy`)

**Purpose**: Represents a trading strategy configuration

**Fields**:
- `StrategyId` (Guid): Unique strategy identifier
- `Name` (string): Strategy display name (e.g., "Momentum Strategy")
- `Type` (string): Strategy type (e.g., "MomentumStrategy", "MeanReversionStrategy")
- `Symbols` (List<string>): Associated trading symbols
- `Timeframe` (string): Trading timeframe (e.g., "1D", "1H")
- `IsEnabled` (bool): Active/disabled status
- `Parameters` (Dictionary<string, object>): Strategy-specific parameters
- `CreatedAt` (DateTime): Strategy creation timestamp
- `UpdatedAt` (DateTime): Last modification timestamp

**Validation Rules**:
- Name must be non-empty and unique
- Symbols list must contain at least one valid ticker
- Parameters must match strategy type requirements

**State Transitions**:
- Disabled → Enabled: User enables strategy
- Enabled → Disabled: User disables strategy

**Usage in UI**:
- Dashboard: Active strategies indicator
- Strategies page: Strategy list with enable/disable actions

---

### PerformanceMetrics

**Source**: `TradingBot.Analytics` (calculated from trade history)

**Purpose**: Aggregated performance statistics

**Fields**:
- `TotalReturn` (decimal): Total return percentage
- `TotalReturnAmount` (decimal): Total return in currency
- `WinRate` (decimal): Percentage of winning trades
- `TotalTrades` (int): Total number of completed trades
- `WinningTrades` (int): Number of profitable trades
- `LosingTrades` (int): Number of losing trades
- `AverageWin` (decimal): Average profit per winning trade
- `AverageLoss` (decimal): Average loss per losing trade
- `ProfitFactor` (decimal): Gross profit / Gross loss
- `SharpeRatio` (decimal): Risk-adjusted return metric
- `SortinoRatio` (decimal): Downside risk-adjusted return
- `CalmarRatio` (decimal): Return / Maximum drawdown
- `MaxDrawdown` (decimal): Largest peak-to-trough decline
- `MaxDrawdownPercent` (decimal): Max drawdown as percentage
- `Expectancy` (decimal): Expected value per trade
- `StartDate` (DateTime): First trade date
- `EndDate` (DateTime): Last trade date

**Validation Rules**:
- All ratio calculations handle division by zero
- Percentages are bounded between -100% and +∞

**Usage in UI**:
- Dashboard: Performance Metrics widget
- Performance page: Detailed metrics with charts

---

### RiskSettings

**Source**: `TradingBot.Core.Models.RiskSettings`

**Purpose**: Risk management configuration

**Fields**:
- `RiskSettingsId` (Guid): Unique identifier
- `AccountId` (Guid): Associated account
- `Leverage` (decimal): Account leverage multiplier (default: 1.0)
- `StopLossPercent` (decimal): Default stop-loss percentage (default: 2.0%)
- `TakeProfitPercent` (decimal): Default take-profit percentage (default: 5.0%)
- `DailyLossLimit` (decimal?): Maximum loss per day (optional)
- `MaxDrawdownPercent` (decimal?): Maximum allowed drawdown (optional)
- `MaxPositionSizePercent` (decimal): Maximum position size as % of equity (default: 10%)
- `IsEnabled` (bool): Risk limits enabled/disabled
- `UpdatedAt` (DateTime): Last modification timestamp

**Validation Rules**:
- Leverage must be between 1.0 and 10.0
- Stop-loss must be between 0.1% and 50%
- Take-profit must be between 0.1% and 100%
- Max position size must be between 1% and 100%

**Usage in UI**:
- Dashboard: Risk Settings widget (read-only)
- Risk Settings page: Full configuration form with validation

---

### BacktestResult

**Source**: `TradingBot.Analytics` (generated from backtesting engine)

**Purpose**: Historical strategy simulation results

**Fields**:
- `BacktestId` (Guid): Unique backtest identifier
- `StrategyName` (string): Strategy used for backtest
- `Symbol` (string): Trading symbol tested
- `StartDate` (DateTime): Backtest period start
- `EndDate` (DateTime): Backtest period end
- `Duration` (TimeSpan): Total backtest duration
- `InitialCapital` (decimal): Starting capital
- `FinalEquity` (decimal): Ending equity
- `TotalPnL` (decimal): Total profit/loss
- `TotalReturn` (decimal): Return percentage
- `EquityCurve` (List<EquityDataPoint>): Portfolio value over time
- `Trades` (List<Trade>): All trades executed during backtest
- `Metrics` (PerformanceMetrics): Performance statistics
- `CreatedAt` (DateTime): Backtest execution timestamp

**Relationships**:
- HasMany: Trades
- HasOne: PerformanceMetrics
- AssociatedWith: Strategy

**Usage in UI**:
- Backtest page: List of backtest runs
- Backtest detail page: Full results with charts and trade list

---

## New View Models (TradingBot.Web specific)

These models are specific to the web UI and not part of the core domain:

### DashboardViewModel

**Purpose**: Aggregates all dashboard data for efficient loading

**Fields**:
- `Account` (Account): Current account state
- `OpenPositions` (List<Position>): Top 10 positions by value
- `RecentTrades` (List<Trade>): Last 5 completed trades
- `PerformanceMetrics` (PerformanceMetrics): Current performance stats
- `RiskSettings` (RiskSettings): Current risk configuration
- `ActiveStrategies` (List<Strategy>): Enabled strategies
- `ConnectionStatus` (string): SignalR connection status ("connected", "reconnecting", "disconnected")
- `LastUpdated` (DateTime): Last data refresh timestamp

**Usage**: Index.razor (Dashboard page) loads this single model

---

### PortfolioHistoryFilter

**Purpose**: Filtering criteria for portfolio history page

**Fields**:
- `StartDate` (DateTime?): Filter trades after this date
- `EndDate` (DateTime?): Filter trades before this date
- `Symbol` (string?): Filter by specific symbol
- `StrategyName` (string?): Filter by strategy
- `MinPnL` (decimal?): Minimum P&L filter
- `MaxPnL` (decimal?): Maximum P&L filter
- `Side` (OrderSide?): Filter by buy/sell side
- `PageNumber` (int): Pagination page (default: 1)
- `PageSize` (int): Items per page (default: 25)

**Usage**: Portfolio.razor for filtering trade history

---

### PortfolioHistoryResult

**Purpose**: Paginated trade history with metadata

**Fields**:
- `Trades` (List<Trade>): Trades for current page
- `TotalCount` (int): Total trades matching filter
- `PageNumber` (int): Current page
- `PageSize` (int): Items per page
- `TotalPages` (int): Total number of pages
- `HasPreviousPage` (bool): Navigation helper
- `HasNextPage` (bool): Navigation helper

**Usage**: Portfolio.razor pagination

---

### EquityCurveDataPoint

**Purpose**: Single point on equity curve chart

**Fields**:
- `Timestamp` (DateTime): Point in time
- `EquityValue` (decimal): Account equity at timestamp

**Usage**: ApexCharts equity curve visualization

---

### ConnectionStatusViewModel

**Purpose**: SignalR connection state for UI display

**Fields**:
- `Status` (string): "connected", "reconnecting", "disconnected"
- `LastConnected` (DateTime?): Last successful connection time
- `ReconnectAttempts` (int): Number of reconnection attempts
- `IsConnected` (bool): Helper property

**Usage**: All pages for connection status badge

---

## Data Flow

### Dashboard Load Flow

1. User navigates to `/` (Index.razor)
2. Page calls `DashboardService.GetDashboardDataAsync()`
3. Service aggregates data from:
   - `IPortfolioManager.GetAccountAsync()`
   - `IPositionRepository.GetOpenPositionsAsync()` → Top 10
   - `ITradeRepository.GetRecentTradesAsync()` → Last 5
   - `IPerformanceService.GetCurrentMetricsAsync()`
   - `IRiskSettingsRepository.GetCurrentSettingsAsync()`
   - `IStrategyRepository.GetActiveStrategiesAsync()`
4. Service returns `DashboardViewModel`
5. Page renders components with data
6. SignalR connection established for real-time updates

### Real-Time Update Flow

1. `RealtimeUpdateService` (background service) runs every 100ms
2. Service fetches current account state from `IPortfolioManager`
3. Service broadcasts via `ITradingClient.ReceiveAccountUpdate(account)`
4. Blazor components subscribed to hub receive update
5. Components call `StateHasChanged()` to re-render

### Position Close Flow

1. User clicks "Close Position" button on position card
2. Confirmation dialog displays with position details
3. User confirms closure
4. Page calls `PortfolioService.ClosePositionAsync(positionId)`
5. Service calls `IPortfolioManager.ClosePositionAsync()`
6. Position removed from database, trade created
7. SignalR broadcasts position removal and new trade
8. UI updates automatically

### Portfolio History Filter Flow

1. User changes filter criteria (date range, symbol, etc.)
2. Page updates `PortfolioHistoryFilter` model
3. Page calls `PortfolioService.GetTradeHistoryAsync(filter)`
4. Service queries `ITradeRepository` with filter criteria
5. Service returns `PortfolioHistoryResult` with paginated data
6. Page renders filtered trade list

---

## Entity-Relationship Diagram

```
Account (1) ──< (N) Position
    │
    └──< (N) Trade
    │
    └──< (1) RiskSettings

Strategy (1) ──< (N) Position (via StrategyName)
    │
    └──< (N) Trade (via StrategyName)

BacktestResult (1) ──< (N) Trade
    │
    └──< (1) PerformanceMetrics
```

---

## Validation Summary

| Model | Client Validation | Server Validation | Source |
|-------|-------------------|-------------------|--------|
| Account | N/A (read-only) | EF Core validation | TradingBot.Core |
| Position | N/A (read-only) | EF Core validation | TradingBot.Core |
| Trade | N/A (read-only) | EF Core validation | TradingBot.Core |
| Strategy | N/A (management via CLI) | Business logic | TradingBot.Core |
| RiskSettings | EditForm + DataAnnotations | Business logic + DB constraints | TradingBot.Core |
| PortfolioHistoryFilter | Form validation | N/A (UI only) | TradingBot.Web |

---

## Database Schema

All entities use existing database schema from `TradingBotDbContext` (TradingBot.Infrastructure). No schema changes required for this feature.

**Existing Tables**:
- `Accounts`
- `Positions`
- `Trades`
- `Strategies` (if persisted)
- `RiskSettings`
- `Candles` (market data cache)

**Indexes** (already exist):
- `IX_Positions_AccountId`
- `IX_Positions_Symbol`
- `IX_Trades_AccountId`
- `IX_Trades_Symbol`
- `IX_Trades_ExitTime` (for recent trades query)

---

## Summary

- **Reused Models**: Account, Position, Trade, Strategy, PerformanceMetrics, RiskSettings, BacktestResult (from TradingBot.Core)
- **New View Models**: DashboardViewModel, PortfolioHistoryFilter, PortfolioHistoryResult, EquityCurveDataPoint, ConnectionStatusViewModel
- **No Database Changes**: All entities already defined in TradingBot.Infrastructure
- **Validation Strategy**: Use existing validation logic from Core layer, add client-side validation for user input (risk settings)
- **Real-Time Updates**: SignalR broadcasts domain model changes directly (no DTOs needed for simple properties)

This approach minimizes duplication and maintains a clean separation between domain models (Core) and view models (Web).