# Data Model: Interactive Web Application Functionality

**Feature**: 005-web-app-functionality
**Date**: 2025-01-14
**Purpose**: Define database entities, relationships, and validation rules

## Overview

This document defines the data model for storing strategy configurations, risk settings, and backtest results. These entities extend the existing TradingBot database schema while maintaining consistency with the Clean Architecture pattern.

---

## Entity Definitions

### 1. StrategyConfiguration

**Purpose**: Stores user-customized parameters for trading strategies (e.g., moving average periods, RSI thresholds).

**Namespace**: `TradingBot.Core.Models.Configuration`

```csharp
/// <summary>
/// Represents a user-customized configuration for a trading strategy.
/// </summary>
public class StrategyConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this configuration.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the strategy this configuration applies to.
    /// Must match an IStrategy.Name from the registered strategies.
    /// </summary>
    public string StrategyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON-serialized dictionary of parameter key-value pairs.
    /// Example: {"FastPeriod": 12, "SlowPeriod": 26, "SignalPeriod": 9}
    /// </summary>
    public string ParametersJson { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the timestamp when this configuration was last modified.
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
```

**Validation Rules**:
- `Id`: Required, unique (primary key)
- `StrategyName`: Required, max 100 characters, must correspond to a registered strategy
- `ParametersJson`: Required, must be valid JSON, default `"{}"`
- `LastModified`: Required, auto-set on save
- `CreatedAt`: Required, auto-set on creation

**Database Schema**:
```sql
CREATE TABLE StrategyConfigurations (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    StrategyName NVARCHAR(100) NOT NULL,
    ParametersJson NVARCHAR(MAX) NOT NULL DEFAULT '{}',
    LastModified DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    CONSTRAINT UQ_StrategyConfiguration_StrategyName UNIQUE (StrategyName)
);

CREATE INDEX IX_StrategyConfigurations_StrategyName ON StrategyConfigurations(StrategyName);
```

**Relationships**:
- No direct FK relationships (strategy name is validated against in-memory registered strategies)
- One-to-one relationship with IStrategy instances (by name)

---

### 2. RiskSettings

**Purpose**: Stores user's risk management configuration (position sizing, stop-loss, take-profit, limits).

**Namespace**: `TradingBot.Core.Models.Configuration`

```csharp
/// <summary>
/// Represents the risk management settings for the trading account.
/// This is a singleton entity (only one row per database).
/// </summary>
public class RiskSettings
{
    /// <summary>
    /// Gets or sets the unique identifier for this record.
    /// Always uses a fixed GUID to ensure single-row table.
    /// </summary>
    public Guid Id { get; set; } // Fixed value: Guid.Parse("00000000-0000-0000-0000-000000000001")

    /// <summary>
    /// Gets or sets the maximum position size as a percentage of account equity.
    /// Example: 10.0 means max 10% of equity per position.
    /// </summary>
    public decimal MaxPositionSizePercent { get; set; } = 10m;

    /// <summary>
    /// Gets or sets the default stop-loss percentage below entry price.
    /// Example: 2.0 means stop-loss at 2% below entry.
    /// </summary>
    public decimal StopLossPercent { get; set; } = 2m;

    /// <summary>
    /// Gets or sets the default take-profit percentage above entry price.
    /// Example: 5.0 means take-profit at 5% above entry.
    /// </summary>
    public decimal TakeProfitPercent { get; set; } = 5m;

    /// <summary>
    /// Gets or sets the maximum number of concurrent open positions.
    /// </summary>
    public int MaxOpenPositions { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum daily loss limit as a percentage of account equity.
    /// Example: 5.0 means trading halts if daily loss exceeds 5% of equity.
    /// </summary>
    public decimal MaxDailyLossPercent { get; set; } = 5m;

    /// <summary>
    /// Gets or sets the timestamp when these settings were last modified.
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when these settings were created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
```

**Validation Rules**:
- `Id`: Fixed value `00000000-0000-0000-0000-000000000001` (enforces single row)
- `MaxPositionSizePercent`: Required, range 0.1-100.0
- `StopLossPercent`: Required, range 0.1-50.0
- `TakeProfitPercent`: Required, range 0.1-100.0
- `MaxOpenPositions`: Required, range 1-100
- `MaxDailyLossPercent`: Required, range 0.1-100.0
- `LastModified`: Required, auto-set on save
- `CreatedAt`: Required, auto-set on creation

**Database Schema**:
```sql
CREATE TABLE RiskSettings (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT '00000000-0000-0000-0000-000000000001',
    MaxPositionSizePercent DECIMAL(18,2) NOT NULL DEFAULT 10.0 CHECK (MaxPositionSizePercent BETWEEN 0.1 AND 100.0),
    StopLossPercent DECIMAL(18,2) NOT NULL DEFAULT 2.0 CHECK (StopLossPercent BETWEEN 0.1 AND 50.0),
    TakeProfitPercent DECIMAL(18,2) NOT NULL DEFAULT 5.0 CHECK (TakeProfitPercent BETWEEN 0.1 AND 100.0),
    MaxOpenPositions INT NOT NULL DEFAULT 5 CHECK (MaxOpenPositions BETWEEN 1 AND 100),
    MaxDailyLossPercent DECIMAL(18,2) NOT NULL DEFAULT 5.0 CHECK (MaxDailyLossPercent BETWEEN 0.1 AND 100.0),
    LastModified DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);

-- Seed default row
INSERT INTO RiskSettings (Id, MaxPositionSizePercent, StopLossPercent, TakeProfitPercent, MaxOpenPositions, MaxDailyLossPercent, LastModified, CreatedAt)
VALUES ('00000000-0000-0000-0000-000000000001', 10.0, 2.0, 5.0, 5, 5.0, GETUTCDATE(), GETUTCDATE());
```

**Relationships**:
- No FK relationships
- Referenced by RiskManager service for order validation

**Design Note**: This is a singleton entity (single row table) because there's only one account in the system. In a multi-user scenario, this would have an AccountId FK.

---

### 3. BacktestResult (Enhancement)

**Purpose**: Stores complete backtest execution results including trades, equity curve, and performance metrics.

**Namespace**: `TradingBot.Core.Models.Backtest` (already exists - enhance for EF persistence)

```csharp
/// <summary>
/// Represents the results of a completed backtest execution.
/// </summary>
public class BacktestResult
{
    /// <summary>
    /// Gets or sets the unique identifier for this backtest.
    /// Format: "bt_{strategy}_{symbol}_{timestamp}" for readability.
    /// </summary>
    public string BacktestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the strategy that was backtested.
    /// </summary>
    public string StrategyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the symbol that was backtested.
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start date of the backtest period.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date of the backtest period.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Gets or sets the initial capital at the start of the backtest.
    /// </summary>
    public decimal InitialCapital { get; set; }

    /// <summary>
    /// Gets or sets the final equity at the end of the backtest.
    /// </summary>
    public decimal FinalEquity { get; set; }

    /// <summary>
    /// Gets or sets the total return as a percentage.
    /// Calculated as: ((FinalEquity - InitialCapital) / InitialCapital) * 100
    /// </summary>
    public decimal TotalReturn { get; set; }

    /// <summary>
    /// Gets or sets the Sharpe ratio (risk-adjusted return).
    /// Higher is better (>1.0 is good, >2.0 is excellent).
    /// </summary>
    public decimal SharpeRatio { get; set; }

    /// <summary>
    /// Gets or sets the maximum drawdown as a percentage.
    /// Represents the largest peak-to-trough decline.
    /// </summary>
    public decimal MaxDrawdown { get; set; }

    /// <summary>
    /// Gets or sets the win rate as a percentage (0-100).
    /// Calculated as: (winning trades / total trades) * 100
    /// </summary>
    public decimal WinRate { get; set; }

    /// <summary>
    /// Gets or sets the profit factor.
    /// Calculated as: gross profits / gross losses
    /// Values >1.0 indicate profitability.
    /// </summary>
    public decimal ProfitFactor { get; set; }

    /// <summary>
    /// Gets or sets the total number of trades executed.
    /// </summary>
    public int TotalTrades { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized list of all trades.
    /// Each trade includes: symbol, side, entry/exit prices, P&L, timestamps.
    /// </summary>
    public string TradesJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the JSON-serialized equity curve data points.
    /// Format: [{"Date": "2024-01-01", "Equity": 100000}, ...]
    /// </summary>
    public string EquityCurveJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the timestamp when this backtest was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
```

**Validation Rules**:
- `BacktestId`: Required, unique (primary key), max 200 characters
- `StrategyName`: Required, max 100 characters
- `Symbol`: Required, max 10 characters
- `StartDate`: Required
- `EndDate`: Required, must be >= StartDate
- `InitialCapital`: Required, must be > 0
- `FinalEquity`: Required, must be >= 0
- `TotalReturn`: Calculated field
- `SharpeRatio`: Calculated field
- `MaxDrawdown`: Calculated field, range 0-100
- `WinRate`: Calculated field, range 0-100
- `ProfitFactor`: Calculated field, must be >= 0
- `TotalTrades`: Required, must be >= 0
- `TradesJson`: Required, must be valid JSON array
- `EquityCurveJson`: Required, must be valid JSON array
- `CreatedAt`: Required, auto-set on creation

**Database Schema**:
```sql
CREATE TABLE BacktestResults (
    BacktestId NVARCHAR(200) PRIMARY KEY,
    StrategyName NVARCHAR(100) NOT NULL,
    Symbol NVARCHAR(10) NOT NULL,
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    InitialCapital DECIMAL(18,2) NOT NULL CHECK (InitialCapital > 0),
    FinalEquity DECIMAL(18,2) NOT NULL CHECK (FinalEquity >= 0),
    TotalReturn DECIMAL(18,4) NOT NULL,
    SharpeRatio DECIMAL(18,4) NOT NULL,
    MaxDrawdown DECIMAL(18,4) NOT NULL CHECK (MaxDrawdown BETWEEN 0 AND 100),
    WinRate DECIMAL(18,4) NOT NULL CHECK (WinRate BETWEEN 0 AND 100),
    ProfitFactor DECIMAL(18,4) NOT NULL CHECK (ProfitFactor >= 0),
    TotalTrades INT NOT NULL CHECK (TotalTrades >= 0),
    TradesJson NVARCHAR(MAX) NOT NULL DEFAULT '[]',
    EquityCurveJson NVARCHAR(MAX) NOT NULL DEFAULT '[]',
    CreatedAt DATETIME2 NOT NULL
);

CREATE INDEX IX_BacktestResults_CreatedAt ON BacktestResults(CreatedAt DESC);
CREATE INDEX IX_BacktestResults_StrategySymbol ON BacktestResults(StrategyName, Symbol);
```

**Relationships**:
- No FK relationships (strategy name is validated against registered strategies)
- Queried by BacktestService for display and export

---

## Existing Entities (Reference)

For completeness, here are the existing entities this feature interacts with:

### Position (TradingBot.Core.Models.Trading)

```csharp
public class Position
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public PositionSide Side { get; set; } // Long/Short (SmartEnum)
    public decimal Quantity { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public DateTime EntryTime { get; set; }
    public string StrategyName { get; set; }
}
```

**Interactions**:
- Portfolio page displays open positions
- Close position action creates a Market order and updates position

### Trade (TradingBot.Core.Models.Trading)

```csharp
public class Trade
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public OrderSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal ExitPrice { get; set; }
    public DateTime EntryTime { get; set; }
    public DateTime ExitTime { get; set; }
    public decimal RealizedPnL { get; set; }
    public decimal Commission { get; set; }
    public string StrategyName { get; set; }
}
```

**Interactions**:
- Portfolio page displays trade history
- Export to CSV functionality

### Order (TradingBot.Core.Models.Trading)

```csharp
public class Order
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public OrderSide Side { get; set; } // Buy/Sell (SmartEnum)
    public OrderType Type { get; set; } // Market/Limit (SmartEnum)
    public decimal Quantity { get; set; }
    public decimal? LimitPrice { get; set; }
    public OrderStatus Status { get; set; } // Pending/Filled/Cancelled (SmartEnum)
    public DateTime CreatedAt { get; set; }
    public DateTime? FilledAt { get; set; }
    public string StrategyName { get; set; }
}
```

**Interactions**:
- Close position creates a Market order
- Risk validation checks order against RiskSettings

---

## Data Transfer Objects (DTOs)

### SymbolSearchResult (TradingBot.Web.Models)

```csharp
public class SymbolSearchResult
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
}
```

### BacktestRequest (TradingBot.Web.Models)

```csharp
public class BacktestRequest
{
    [Required(ErrorMessage = "Strategy is required")]
    public string StrategyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Symbol is required")]
    [RegularExpression(@"^[A-Z]{1,5}$", ErrorMessage = "Symbol must be 1-5 uppercase letters")]
    public string Symbol { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }

    [Required(ErrorMessage = "Initial capital is required")]
    [Range(1000, 10000000, ErrorMessage = "Initial capital must be between $1,000 and $10,000,000")]
    public decimal InitialCapital { get; set; } = 100000m;
}
```

### StrategyParameterDto (TradingBot.Web.Models)

```csharp
public class StrategyParameterDto
{
    public string ParameterName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ParameterType { get; set; } = "int"; // "int", "decimal", "bool", "string"
    public object CurrentValue { get; set; } = 0;
    public object? MinValue { get; set; }
    public object? MaxValue { get; set; }
}
```

---

## Entity Relationships Diagram

```
┌─────────────────────┐
│ StrategyConfiguration│
│ (per strategy)      │
├─────────────────────┤
│ + Id (PK)           │
│ + StrategyName (UQ) │──────┐
│ + ParametersJson    │      │
│ + LastModified      │      │
│ + CreatedAt         │      │
└─────────────────────┘      │
                             │ referenced by name
                             │
┌─────────────────────┐      │      ┌─────────────────────┐
│   RiskSettings      │      │      │   BacktestResult    │
│   (singleton)       │      │      │   (per backtest)    │
├─────────────────────┤      │      ├─────────────────────┤
│ + Id (PK, fixed)    │      │      │ + BacktestId (PK)   │
│ + MaxPositionSize%  │      ├──────│ + StrategyName      │
│ + StopLoss%         │      │      │ + Symbol            │
│ + TakeProfit%       │      │      │ + StartDate         │
│ + MaxOpenPositions  │      │      │ + EndDate           │
│ + MaxDailyLoss%     │      │      │ + InitialCapital    │
│ + LastModified      │      │      │ + FinalEquity       │
│ + CreatedAt         │      │      │ + TotalReturn       │
└─────────────────────┘      │      │ + SharpeRatio       │
         │                   │      │ + MaxDrawdown       │
         │ referenced by     │      │ + WinRate           │
         │ RiskManager       │      │ + ProfitFactor      │
         ▼                   │      │ + TotalTrades       │
┌─────────────────────┐      │      │ + TradesJson        │
│       Order         │      │      │ + EquityCurveJson   │
│   (existing)        │      │      │ + CreatedAt         │
├─────────────────────┤      │      └─────────────────────┘
│ + Id (PK)           │      │
│ + Symbol            │      │
│ + Side              │      │      ┌─────────────────────┐
│ + Type              │      │      │      Strategy       │
│ + Quantity          │      └──────│   (in-memory)       │
│ + Status            │             ├─────────────────────┤
│ + StrategyName      │─────────────│ + Name              │
└─────────────────────┘             │ + Parameters        │
         │                          └─────────────────────┘
         │ creates
         ▼
┌─────────────────────┐
│      Position       │
│    (existing)       │
├─────────────────────┤
│ + Id (PK)           │
│ + Symbol            │
│ + Side              │
│ + Quantity          │
│ + EntryPrice        │
│ + UnrealizedPnL     │
│ + StrategyName      │
└─────────────────────┘
         │
         │ closes to create
         ▼
┌─────────────────────┐
│       Trade         │
│    (existing)       │
├─────────────────────┤
│ + Id (PK)           │
│ + Symbol            │
│ + Side              │
│ + EntryPrice        │
│ + ExitPrice         │
│ + RealizedPnL       │
│ + StrategyName      │
└─────────────────────┘
```

---

## State Transitions

### StrategyConfiguration Lifecycle

```
[User modifies parameters] → [Validate] → [Serialize to JSON] → [Save to DB] → [Apply to in-memory strategy instance]
```

### RiskSettings Lifecycle

```
[User modifies settings] → [Validate ranges] → [Save to DB (single row UPDATE)] → [Reload in RiskManager]
```

### BacktestResult Lifecycle

```
[User submits backtest request] → [Queue background task] → [Execute backtest] → [Calculate metrics] → [Save result to DB] → [Notify UI via SignalR]
```

### Position Closure Flow

```
[User clicks Close Position] → [Confirm dialog] → [Create Market order] → [Validate vs RiskSettings] → [Execute order] → [Update Position] → [Create Trade record] → [Notify UI via SignalR]
```

---

## Migration Strategy

### Step 1: Create StrategyConfiguration Migration

```bash
dotnet ef migrations add AddStrategyConfigurationsTable \
  --project src/TradingBot.Infrastructure \
  --startup-project src/TradingBot.Cli
```

### Step 2: Create RiskSettings Migration

```bash
dotnet ef migrations add AddRiskSettingsTable \
  --project src/TradingBot.Infrastructure \
  --startup-project src/TradingBot.Cli
```

### Step 3: Create BacktestResult Migration

```bash
dotnet ef migrations add AddBacktestResultsTable \
  --project src/TradingBot.Infrastructure \
  --startup-project src/TradingBot.Cli
```

### Step 4: Apply Migrations

```bash
dotnet ef database update \
  --project src/TradingBot.Infrastructure \
  --startup-project src/TradingBot.Cli
```

---

## Data Seeding

### Default RiskSettings

```csharp
public void Configure(EntityTypeBuilder<RiskSettings> builder)
{
    // Seed default row
    builder.HasData(new RiskSettings
    {
        Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
        MaxPositionSizePercent = 10m,
        StopLossPercent = 2m,
        TakeProfitPercent = 5m,
        MaxOpenPositions = 5,
        MaxDailyLossPercent = 5m,
        LastModified = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow
    });
}
```

---

## Summary

| Entity | Purpose | Cardinality | Key Fields |
|--------|---------|-------------|------------|
| StrategyConfiguration | Store custom strategy parameters | One per strategy | StrategyName (unique), ParametersJson |
| RiskSettings | Store risk management settings | Singleton (one row) | All percentage fields, MaxOpenPositions |
| BacktestResult | Store backtest execution results | Many (one per backtest run) | BacktestId, metrics, TradesJson |

All entities follow:
- ✅ Clean Architecture (Core layer for entities, Infrastructure for persistence)
- ✅ SmartEnum pattern for strongly-typed enums
- ✅ EF Core fluent API configuration (no data annotations in entities)
- ✅ Validation at service layer (not entity layer)
- ✅ UTC timestamps for all date/time fields
- ✅ Nullable reference types enabled
