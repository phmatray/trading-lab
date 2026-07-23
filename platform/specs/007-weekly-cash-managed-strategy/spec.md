# Feature Specification: Weekly Cash-Managed Trading Strategy

**Feature Branch**: `007-weekly-cash-managed-strategy`
**Created**: 2025-01-16
**Status**: Draft
**Input**: User description: "Implement weekly cash-managed trading strategy with MA20 indicator, configurable buy/sell ratios, and automated cash buffer management"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Configure and Enable Weekly Cash-Managed Strategy (Priority: P1)

As a trader, I want to configure and activate a weekly cash-managed strategy with customizable parameters so that I can automate ETP trading based on the underlying asset's MA20 trend while maintaining a healthy cash buffer.

**Why this priority**: This is the core functionality - users must be able to configure and enable the strategy before any automated trading can occur. Without this, no other features can function.

**Independent Test**: Can be fully tested by navigating to the strategy configuration page, entering valid parameters (cash ratios, buy/sell ratios, symbol mappings), enabling the strategy, and verifying the configuration is saved and the strategy appears as "active" in the dashboard.

**Acceptance Scenarios**:

1. **Given** I am on the strategy configuration page, **When** I configure the weekly cash-managed strategy with MIN_CASH_RATIO=0.15, MAX_CASH_RATIO=0.25, WEEKLY_BUY_RATIO=0.05, WEEKLY_SELL_RATIO=0.10, ETP symbol "BTCW", and underlying symbol "COIN", **Then** the system saves my configuration and displays a confirmation message.

2. **Given** I have configured the strategy parameters, **When** I toggle the strategy to "enabled", **Then** the strategy status changes to "active" and appears in my list of running strategies.

3. **Given** I attempt to enable the strategy with invalid parameters (e.g., MIN_CASH_RATIO > MAX_CASH_RATIO or ratios outside 0-1 range), **When** I submit the configuration, **Then** the system displays validation errors and prevents activation.

4. **Given** the strategy is active, **When** I navigate to the strategy details page, **Then** I see current configuration values, current state (cash ratio, days below MA20, position size), and the next scheduled execution time.

---

### User Story 2 - Automated Weekly Buy Execution Based on MA20 Trend (Priority: P2)

As a trader with an active weekly cash-managed strategy, I want the system to automatically purchase ETP shares when the underlying asset is above its 20-day moving average and I have sufficient cash buffer, so that I can systematically accumulate positions during bullish trends.

**Why this priority**: This implements the core buying logic. It depends on P1 (strategy configuration) but can be tested independently once configuration exists.

**Independent Test**: Can be tested by setting up a strategy with known parameters, mocking or using historical data where COIN > MA20, ensuring cash_ratio > MIN_CASH_RATIO, triggering the weekly routine (simulating end-of-week), and verifying that the system calculates correct buy amount and executes a market order for ETP shares.

**Acceptance Scenarios**:

1. **Given** it is the end of the trading week (Friday), the underlying COIN price is $150 and MA20 is $140 (COIN > MA20), my portfolio has $100,000 total equity with $20,000 cash (20% cash ratio), and WEEKLY_BUY_RATIO is 0.05, **When** the weekly routine executes, **Then** the system invests $5,000 (5% of $100,000) in ETP shares at current market price.

2. **Given** it is the end of the week, COIN > MA20, but my cash ratio is exactly 15% (MIN_CASH_RATIO), **When** the weekly routine executes, **Then** the system does NOT execute a buy order (cash ratio is not above minimum threshold).

3. **Given** it is the end of the week, COIN > MA20, my cash ratio is 20%, but I only have $2,000 cash available and the calculated buy amount is $5,000, **When** the weekly routine executes, **Then** the system invests only $2,000 (all available cash) in ETP shares.

4. **Given** it is mid-week (Wednesday), COIN > MA20, and cash ratio is healthy, **When** the daily routine runs, **Then** the system does NOT execute any buy orders (buys only occur on weekly schedule).

---

### User Story 3 - Automated Weekly Sell Execution Based on MA20 Breakdown (Priority: P2)

As a trader with an active strategy and open ETP positions, I want the system to automatically sell a portion of my holdings when the underlying asset stays below its MA20 for 2 consecutive days, so that I can systematically reduce exposure during bearish trends.

**Why this priority**: This implements the core selling/risk management logic. It depends on P1 but can be tested independently with mock position data and price scenarios.

**Independent Test**: Can be tested by setting up a strategy with an existing ETP position, mocking daily price data where COIN < MA20 for 2+ consecutive days, triggering the weekly routine on Friday, and verifying the system calculates correct sell amount (10% of position) and executes a market sell order.

**Acceptance Scenarios**:

1. **Given** I hold 100 ETP shares, the underlying COIN has been below MA20 for 2 consecutive days, it is Friday, and WEEKLY_SELL_RATIO is 0.10, **When** the weekly routine executes, **Then** the system sells 10 shares (10% of position) at current market price and adds proceeds to cash.

2. **Given** I hold ETP shares, COIN has been below MA20 for only 1 day, it is Friday, **When** the weekly routine executes, **Then** the system does NOT execute a sell order (threshold is 2 days).

3. **Given** I hold ETP shares, COIN was below MA20 for 2 days but then crossed back above MA20 on the 3rd day, **When** the weekly routine executes, **Then** the days_below_ma20 counter resets to 0 and no sell order is executed.

4. **Given** I hold 100 ETP shares, COIN has been below MA20 for 2+ days, but it is Wednesday (mid-week), **When** the daily routine runs, **Then** the system updates the days_below_ma20 counter but does NOT execute sell orders (sells only occur on weekly schedule).

---

### User Story 4 - Automated Cash Buffer Rebalancing (Priority: P3)

As a trader with an active strategy, I want the system to automatically maintain my cash buffer between 15% and 25% of total equity by making additional small buys or sells at week-end, so that I always have sufficient liquidity for opportunities and risk management.

**Why this priority**: This is a refinement feature that ensures portfolio health but is not critical for basic strategy operation. Can be added after core buy/sell logic (P2) is working.

**Independent Test**: Can be tested by setting up scenarios where cash ratio falls below 15% or exceeds 25% after the primary buy/sell logic executes, triggering the weekly routine, and verifying the system makes corrective trades to bring cash ratio back into range.

**Acceptance Scenarios**:

1. **Given** after primary weekly buy/sell logic executes, my cash ratio is 12% (below 15% minimum), I hold ETP shares, and WEEKLY_SELL_RATIO is 0.10, **When** the cash buffer adjustment runs, **Then** the system sells 10% of my ETP position to rebuild the cash buffer.

2. **Given** after primary logic, my cash ratio is 30% (above 25% maximum) and COIN > MA20, **When** the cash buffer adjustment runs, **Then** the system invests additional cash (5% of equity) in ETP shares to reduce excess cash.

3. **Given** after primary logic, my cash ratio is 30% but COIN is below MA20, **When** the cash buffer adjustment runs, **Then** the system does NOT make additional purchases (only buys if underlying is bullish).

4. **Given** my cash ratio is within the 15-25% range after primary logic, **When** the cash buffer adjustment runs, **Then** the system makes no additional trades.

---

### User Story 5 - Daily MA20 Tracking and Status Monitoring (Priority: P3)

As a trader monitoring my active strategy, I want to see daily updates to the MA20 indicator, consecutive days below MA20, current prices, and strategy state in the web dashboard, so that I can understand what the strategy is observing and anticipate upcoming actions.

**Why this priority**: This is a monitoring/visibility feature that enhances user experience but is not required for the strategy to function. Can be implemented after core strategy logic (P1-P2) works.

**Independent Test**: Can be tested by activating the strategy, navigating to the strategy details/dashboard page, and verifying that real-time or daily-refreshed data displays: current COIN price, current ETP price, MA20 value, days_below_ma20 counter, current cash ratio, and next scheduled action.

**Acceptance Scenarios**:

1. **Given** I have an active weekly cash-managed strategy, **When** I view the strategy details page, **Then** I see current values for: ETP symbol, COIN symbol, COIN price, ETP price, MA20 value, days below MA20, current cash ratio, total equity, position size, and next execution date.

2. **Given** the daily routine runs and updates COIN price and MA20, **When** I refresh the strategy details page, **Then** the displayed values update to reflect the latest data.

3. **Given** COIN crosses below MA20 for the first time today, **When** the daily routine completes, **Then** the "days below MA20" counter increments from 0 to 1 and is visible in the dashboard.

4. **Given** I am viewing multiple active strategies, **When** I navigate to the strategies overview page, **Then** I see a summary card for the weekly cash-managed strategy showing key metrics (cash ratio, position size, status) without needing to drill down.

---

### User Story 6 - Optional Breakout Rule for Accelerated Buying (Priority: P4)

As a trader wanting to capitalize on strong momentum, I want the option to enable a breakout rule that doubles the weekly buy amount when the underlying asset shows significant price increase (+10% weekly) and high volume, so that I can increase exposure during confirmed breakouts.

**Why this priority**: This is an optional enhancement/advanced feature that can be disabled by default. It's not essential for MVP and can be added later once core strategy proves stable.

**Independent Test**: Can be tested by enabling the breakout rule in configuration, setting up a scenario where COIN > MA20 AND weekly price increase > 10% AND volume > average, triggering the weekly routine, and verifying the system doubles the buy ratio (e.g., invests 10% instead of 5%).

**Acceptance Scenarios**:

1. **Given** I have enabled the breakout rule, it is Friday, COIN > MA20, COIN has increased 12% this week, and volume is 150% of 20-day average, **When** the weekly routine executes, **Then** the system invests 10% of equity (double the normal 5% WEEKLY_BUY_RATIO) in ETP shares.

2. **Given** the breakout rule is enabled, COIN > MA20, but weekly price increase is only 8% (below 10% threshold), **When** the weekly routine executes, **Then** the system invests the normal 5% (no doubling).

3. **Given** the breakout rule is enabled, COIN > MA20, weekly price increase is 12%, but volume is below average, **When** the weekly routine executes, **Then** the system invests the normal 5% (all conditions must be met for doubling).

4. **Given** I have disabled the breakout rule in configuration, **When** breakout conditions are met, **Then** the system ignores the breakout logic and uses standard buy ratio.

---

### Edge Cases

- What happens when the ETP market is closed or trading is halted during the weekly routine execution?
  - System should log the attempt, queue the order for next available trading session, and notify the user of the delay.

- What happens when the MA20 calculation fails due to insufficient historical data (e.g., newly listed asset)?
  - System should prevent strategy activation and display error message indicating minimum 20 days of historical data required.

- What happens when cash ratio drops below MIN_CASH_RATIO due to market movements (unrealized losses) between weekly executions?
  - System should wait for next weekly routine to rebalance via the cash buffer adjustment logic; no intra-week intervention.

- What happens when the user manually closes positions or withdraws cash while the strategy is active?
  - System should recalculate cash ratio and position size at next daily/weekly routine based on current actual state, adjusting actions accordingly.

- What happens if COIN data or ETP data is unavailable (API failure, delisted asset)?
  - System should skip the routine execution, log an error, notify user, and retry at next scheduled time.

- What happens when calculated buy/sell amounts are below the minimum tradeable quantity (e.g., fractional shares not supported)?
  - System should round down to nearest whole share for sells, round up to nearest whole share for buys (within available cash), or skip trade if below 1 share.

- What happens if the user changes configuration parameters (cash ratios, buy/sell ratios) mid-week while strategy is active?
  - System should apply new parameters at the next weekly routine execution; no retroactive recalculation of past actions.

- What happens during extreme market volatility when ETP price diverges significantly from underlying COIN price?
  - System continues to execute based on configured logic (COIN MA20 and ratios); user should monitor and can disable strategy manually if divergence is unacceptable.

## Requirements *(mandatory)*

### Functional Requirements

#### Strategy Configuration

- **FR-001**: System MUST allow users to configure a weekly cash-managed strategy with the following parameters: MIN_CASH_RATIO (decimal 0-1), MAX_CASH_RATIO (decimal 0-1), WEEKLY_BUY_RATIO (decimal 0-1), WEEKLY_SELL_RATIO (decimal 0-1), ETP symbol (string), underlying asset symbol (string).

- **FR-002**: System MUST validate that MIN_CASH_RATIO < MAX_CASH_RATIO and both are within range [0, 1].

- **FR-003**: System MUST validate that WEEKLY_BUY_RATIO and WEEKLY_SELL_RATIO are within range [0, 1].

- **FR-004**: System MUST allow users to enable or disable the strategy via a toggle control.

- **FR-005**: System MUST persist strategy configuration to the database and restore it on application restart.

- **FR-006**: System MUST allow users to optionally enable a breakout rule with configurable thresholds: weekly price increase percentage (default 10%), volume multiplier (default 1.5x average), and accelerated buy ratio multiplier (default 2x).

#### Daily Routine

- **FR-007**: System MUST execute a daily routine that retrieves current prices for the configured underlying asset (COIN) and ETP.

- **FR-008**: System MUST calculate the 20-day simple moving average (MA20) of the underlying asset's closing price each day.

- **FR-009**: System MUST track consecutive days the underlying asset price is below MA20 by incrementing a counter when COIN < MA20 and resetting counter to 0 when COIN >= MA20.

- **FR-010**: Daily routine MUST NOT execute buy or sell orders (only updates state: prices, MA20, days_below_ma20).

#### Weekly Routine

- **FR-011**: System MUST execute a weekly routine on the configured day of the week (default: Friday, configurable to any weekday).

- **FR-012**: Weekly routine MUST calculate current total equity as: cash + (ETP shares held × current ETP price).

- **FR-013**: Weekly routine MUST calculate current cash ratio as: cash / total equity.

#### Buy Logic

- **FR-014**: System MUST execute a buy order IF all conditions are met:
  - It is the weekly execution day (e.g., Friday)
  - Underlying asset price > MA20
  - Current cash ratio > MIN_CASH_RATIO
  - Available cash > 0

- **FR-015**: Buy order amount MUST be calculated as: min(WEEKLY_BUY_RATIO × total_equity, available_cash).

- **FR-016**: Buy order MUST be a market order for ETP shares, quantity = buy_amount / current_ETP_price (rounded down to nearest tradeable unit).

- **FR-017**: After buy execution, system MUST update cash (decrease) and ETP shares held (increase) based on actual fill price and quantity.

#### Sell Logic

- **FR-018**: System MUST execute a sell order IF all conditions are met:
  - It is the weekly execution day (e.g., Friday)
  - days_below_ma20 >= 2
  - ETP shares held > 0

- **FR-019**: Sell order quantity MUST be calculated as: WEEKLY_SELL_RATIO × ETP_shares_held (rounded down to nearest tradeable unit).

- **FR-020**: Sell order MUST be a market order for ETP shares at current market price.

- **FR-021**: After sell execution, system MUST update cash (increase) and ETP shares held (decrease) based on actual fill price and quantity.

#### Cash Buffer Adjustment

- **FR-022**: After primary buy/sell logic executes, system MUST recalculate total equity and cash ratio.

- **FR-023**: IF cash ratio < MIN_CASH_RATIO AND ETP shares held > 0, system MUST sell WEEKLY_SELL_RATIO × ETP_shares_held to rebuild cash buffer.

- **FR-024**: IF cash ratio > MAX_CASH_RATIO AND underlying asset price > MA20, system MUST buy additional ETP shares using WEEKLY_BUY_RATIO × total_equity (capped at available cash).

- **FR-025**: Cash buffer adjustment MUST execute AFTER primary buy/sell logic in the same weekly routine.

#### Optional Breakout Rule

- **FR-026**: IF breakout rule is enabled, system MUST check breakout conditions before executing buy logic:
  - Underlying asset price > MA20
  - Weekly price increase > configured threshold (default 10%)
  - Current volume > configured volume multiplier × 20-day average volume (default 1.5x)

- **FR-027**: IF breakout conditions are met, system MUST multiply WEEKLY_BUY_RATIO by the configured accelerated multiplier (default 2x) for that week's buy calculation only.

#### Strategy State Management

- **FR-028**: System MUST persist strategy state including: current cash balance, ETP shares held, days_below_ma20 counter, last execution timestamp.

- **FR-029**: System MUST reset days_below_ma20 counter to 0 whenever underlying asset price crosses above MA20.

- **FR-030**: System MUST prevent strategy activation if insufficient historical data exists to calculate MA20 (minimum 20 trading days required).

#### Order Execution & Risk Management

- **FR-031**: All buy and sell orders MUST go through the existing OrderExecutionService with simulated slippage and commission as per project standards.

- **FR-032**: System MUST respect existing risk limits configured in RiskManager (position size limits, max drawdown, etc.) when executing strategy orders.

- **FR-033**: IF an order is rejected by RiskManager, system MUST log the rejection, notify user, and continue to next scheduled routine without halting strategy.

- **FR-034**: System MUST create domain events (OrderFilledEvent, PositionOpenedEvent, PositionClosedEvent) for all strategy-generated trades following DDD patterns.

#### Web Dashboard Integration

- **FR-035**: Web dashboard MUST display weekly cash-managed strategy in the strategies list with status indicator (active/inactive).

- **FR-036**: Strategy details page MUST show: configuration parameters, current state (cash ratio, position size, days below MA20), current prices (COIN, ETP, MA20), and next scheduled execution time.

- **FR-037**: Dashboard MUST update in real-time (via SignalR) when the weekly routine executes and trades are placed.

- **FR-038**: Users MUST be able to manually disable the strategy from the web dashboard, which prevents future weekly routine executions but does not close existing positions.

- **FR-039**: Strategy configuration form MUST provide input validation with clear error messages for invalid parameters.

### Key Entities

- **WeeklyCashManagedStrategy**: Represents the strategy configuration and state
  - **Attributes**:
    - Configuration: MIN_CASH_RATIO, MAX_CASH_RATIO, WEEKLY_BUY_RATIO, WEEKLY_SELL_RATIO, ETP symbol, underlying symbol, execution day of week, breakout rule settings
    - State: is_enabled, days_below_ma20, last_execution_timestamp
    - Relationships: Associated with Account (which strategy runs on), generates Orders, maintains Position (ETP holdings)

- **StrategyState** (extends existing Account/Portfolio concepts): Tracks current portfolio composition for the strategy
  - **Attributes**: cash_balance, etp_shares_held, calculated_total_equity, calculated_cash_ratio
  - **Relationships**: Linked to WeeklyCashManagedStrategy, updated by OrderFilled events

- **MA20Indicator**: Represents the 20-day moving average calculation
  - **Attributes**: underlying_symbol, current_value, calculation_date, historical_prices (last 20 days)
  - **Relationships**: Calculated from Candle/market data, referenced by WeeklyCashManagedStrategy

- **BreakoutRuleConfig** (optional): Stores breakout rule parameters
  - **Attributes**: enabled, weekly_price_increase_threshold, volume_multiplier, buy_ratio_multiplier
  - **Relationships**: Child configuration of WeeklyCashManagedStrategy

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can configure and activate the weekly cash-managed strategy in under 3 minutes with all required parameters.

- **SC-002**: Strategy correctly executes buy orders when underlying asset is above MA20 and cash ratio permits, verified across 20+ simulated weekly cycles with 100% accuracy.

- **SC-003**: Strategy correctly executes sell orders when underlying asset remains below MA20 for 2+ consecutive days, verified across 20+ simulated weekly cycles with 100% accuracy.

- **SC-004**: Cash buffer remains within configured MIN-MAX range (15-25%) after weekly routine executes, verified across 50+ simulated scenarios including market volatility.

- **SC-005**: System calculates MA20 correctly with less than 0.01% deviation from industry-standard calculations across 1000+ data points.

- **SC-006**: Strategy state (cash ratio, position size, days_below_ma20) updates are visible in web dashboard within 2 seconds of weekly routine completion via SignalR.

- **SC-007**: All strategy-generated orders pass through existing risk management validation with 0% bypass rate.

- **SC-008**: Strategy persists configuration and state correctly across application restarts with 100% data integrity.

- **SC-009**: Breakout rule (when enabled) correctly identifies qualifying scenarios and doubles buy amount with 100% accuracy across 30+ test cases.

- **SC-010**: System handles edge cases (missing data, API failures, below-minimum trade amounts) gracefully without crashing, logging errors in 100% of failure scenarios.

- **SC-011**: Strategy achieves minimum 80% code coverage with all critical paths (buy logic, sell logic, cash buffer adjustment) at 100% coverage.

- **SC-012**: Weekly routine completes execution (all calculations and order placements) in under 10 seconds under normal market conditions.

## Assumptions

1. **Execution Timing**: Weekly routine executes after market close on the configured day (default Friday) to ensure all daily price data is available. If Friday is a market holiday, execution shifts to the previous trading day.

2. **Fractional Shares**: Assuming fractional share trading is NOT supported; all buy/sell quantities are rounded down to nearest whole share. If the project later adds fractional share support, calculations will use exact decimal quantities.

3. **Market Data Availability**: Yahoo Finance API provides reliable daily OHLCV data for both the underlying asset (COIN) and ETP. System assumes data is available by end of trading day.

4. **Single Strategy Instance Per Symbol Pair**: Each ETP-underlying symbol pair can have only one active weekly cash-managed strategy at a time. Users cannot run two configurations simultaneously for the same pair.

5. **Position Ownership**: The strategy exclusively manages the ETP position it creates. Users should not manually trade the same ETP symbol while the strategy is active (manual trades will affect cash ratio and position size calculations).

6. **Cash Segregation**: Cash used by the strategy is part of the main account cash pool (not segregated). The strategy calculates cash ratio based on total account cash and equity.

7. **Commission & Slippage**: All orders go through existing OrderExecutionService which applies configured commission rates and simulated slippage. Strategy logic does not account for slippage/commission in pre-trade calculations (uses current market price).

8. **Volume Data**: For the optional breakout rule, volume data is assumed to be available from market data provider. If volume is unavailable, breakout rule is automatically disabled with a warning logged.

9. **Holiday Handling**: System uses a trading calendar to identify valid trading days. Non-trading days are skipped; the daily routine does not execute on weekends/holidays.

10. **Strategy Initialization**: When strategy is first enabled, days_below_ma20 counter starts at 0. The system does not backfill historical consecutive days below MA20.

11. **Price Data Timing**: Daily routine uses closing price of the underlying asset for MA20 calculation and comparison. Intraday price movements are ignored.

12. **Web Dashboard Access**: Users interact with the strategy via the existing Blazor Server web dashboard. No CLI commands or direct API access for strategy management (consistent with project direction after CLI removal).

13. **DDD Compliance**: Strategy implementation follows existing DDD patterns - WeeklyCashManagedStrategy is an aggregate root, generates domain events, persists via repository pattern, and does not access DbContext directly.

14. **Real-time Updates**: SignalR hub broadcasts strategy state changes to connected clients. Users not actively viewing the dashboard receive updates when they next navigate to the strategy page (no push notifications).

15. **Testing Approach**: Unit tests use FakeItEasy for mocking dependencies, integration tests use in-memory SQLite database, and backtest simulations verify strategy behavior across historical data scenarios.