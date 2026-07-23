# Feature Specification: Blazor Server Trading Dashboard

**Feature Branch**: `002-blazor-server-app`
**Created**: 2025-11-07
**Status**: Draft
**Input**: User description: "Build a blazor server application that replicate the behavior of the cli app"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Real-Time Trading Dashboard (Priority: P1)

As a trader, I want to view my real-time trading dashboard in a web browser so that I can monitor my portfolio, positions, and performance metrics without using a command-line interface.

**Why this priority**: This is the core value proposition - providing web-based access to the same trading information currently available only through CLI. It's the foundation that all other features build upon.

**Independent Test**: Can be fully tested by accessing the web dashboard and verifying that all key metrics (account equity, positions, recent trades, performance metrics, risk settings) are displayed and update in real-time. Delivers immediate value by providing browser-based portfolio monitoring.

**Acceptance Scenarios**:

1. **Given** a trader has an active trading account, **When** they navigate to the dashboard URL, **Then** they see their current account equity, cash balance, and buying power displayed prominently
2. **Given** the trader has open positions, **When** the dashboard loads, **Then** all open positions are displayed with symbol, quantity, entry price, current price, and P&L
3. **Given** the trader is viewing the dashboard, **When** position values change, **Then** the dashboard updates automatically without manual refresh within 2 seconds
4. **Given** the trader has completed trades, **When** they view the dashboard, **Then** the 5 most recent trades are displayed with date, symbol, side, quantity, and realized P&L
5. **Given** the trader is viewing the dashboard, **When** they have no open positions, **Then** a clear "No open positions" message is displayed in the positions section

---

### User Story 2 - Portfolio Management Interface (Priority: P2)

As a trader, I want to view my complete portfolio history and close positions through the web interface so that I can manage my trading activity without switching to the CLI.

**Why this priority**: This adds essential portfolio management capabilities beyond just viewing. It enables traders to take action on their positions, making the web app functionally complete for basic trading operations.

**Independent Test**: Can be tested independently by navigating to the portfolio section, viewing trade history with filters, and successfully closing an open position. Delivers value by enabling position management through the web interface.

**Acceptance Scenarios**:

1. **Given** a trader is logged into the dashboard, **When** they navigate to the portfolio history page, **Then** they see all completed trades sorted by date (most recent first)
2. **Given** the trader is viewing portfolio history, **When** they apply a date range filter, **Then** only trades within the selected date range are displayed
3. **Given** the trader is viewing portfolio history, **When** they filter by symbol, **Then** only trades for the selected symbol are displayed
4. **Given** the trader has an open position, **When** they click "Close Position" on a position card, **Then** they see a confirmation dialog showing the position details and estimated proceeds
5. **Given** the trader confirms position closure, **When** the close action is processed, **Then** the position is removed from open positions and appears in trade history with the closing details
6. **Given** the trader is viewing trade history, **When** they export the history, **Then** a downloadable file (CSV or Excel) is generated with all filtered trades

---

### User Story 3 - Performance Analytics Visualization (Priority: P2)

As a trader, I want to view my performance analytics and statistics in visual charts and tables so that I can analyze my trading effectiveness at a glance.

**Why this priority**: Visual analytics provide immediate insight into trading performance that's difficult to parse from CLI output. This significantly enhances the user experience and decision-making capability.

**Independent Test**: Can be tested by navigating to the performance page and verifying that all performance metrics (Sharpe ratio, win rate, drawdown, etc.) are displayed with appropriate visualizations. Delivers value by providing actionable performance insights.

**Acceptance Scenarios**:

1. **Given** a trader has trade history, **When** they navigate to the performance page, **Then** they see key metrics including total return, win rate, Sharpe ratio, Sortino ratio, max drawdown, and profit factor
2. **Given** the trader is viewing performance metrics, **When** performance is positive, **Then** metrics are displayed in green with positive indicators
3. **Given** the trader is viewing performance metrics, **When** performance is negative, **Then** metrics are displayed in red with negative indicators
4. **Given** the trader has completed trades, **When** they view the performance page, **Then** an equity curve chart shows portfolio value over time
5. **Given** the trader wants detailed statistics, **When** they view the trading statistics section, **Then** they see total trades, winning/losing trade counts, average win/loss amounts, and expectancy

---

### User Story 4 - Strategy Management (Priority: P3)

As a trader, I want to view and manage my trading strategies through the web interface so that I can enable, disable, and monitor strategy performance without using CLI commands.

**Why this priority**: While important, strategy management is typically done less frequently than monitoring positions. Traders can fall back to CLI for this functionality initially.

**Independent Test**: Can be tested by viewing the strategies list, enabling/disabling a strategy, and verifying the changes persist. Delivers value by centralizing all trading management in one web interface.

**Acceptance Scenarios**:

1. **Given** a trader has configured strategies, **When** they navigate to the strategies page, **Then** they see all available strategies with their current status (active/disabled)
2. **Given** the trader views a strategy, **When** they see the strategy details, **Then** information includes strategy name, type, associated symbols, timeframe, and current status
3. **Given** the trader wants to enable a strategy, **When** they click the "Enable" button, **Then** the strategy status changes to active and the button changes to "Disable"
4. **Given** the trader wants to disable an active strategy, **When** they click "Disable", **Then** the strategy status changes to disabled and no new signals are generated
5. **Given** strategies are running, **When** a strategy generates a signal, **Then** the trader sees a notification or alert on the dashboard

---

### User Story 5 - Risk Management Configuration (Priority: P3)

As a trader, I want to view and adjust my risk management settings through the web interface so that I can control my exposure and risk parameters without using CLI commands.

**Why this priority**: Risk settings are typically configured once and adjusted infrequently. The viewing aspect is covered in P1 dashboard. Editing capabilities can be added later.

**Independent Test**: Can be tested by navigating to risk settings, modifying a parameter (e.g., stop-loss percentage), and verifying the change is saved and reflected in the dashboard. Delivers value by enabling complete trading setup through the web interface.

**Acceptance Scenarios**:

1. **Given** a trader is on the risk settings page, **When** they view current settings, **Then** they see leverage, stop-loss %, take-profit %, daily loss limit, max drawdown %, and max position size %
2. **Given** the trader wants to change stop-loss, **When** they enter a new percentage and save, **Then** the new stop-loss percentage is applied to future trades
3. **Given** the trader wants to change leverage, **When** they adjust the leverage slider and save, **Then** the new leverage setting is applied and displayed in the dashboard
4. **Given** the trader sets a daily loss limit, **When** daily losses reach the limit, **Then** an alert is displayed on the dashboard and risk status shows "LIMIT REACHED"
5. **Given** risk limits are enabled, **When** the trader views risk settings, **Then** the status shows "ENABLED" in green, otherwise "DISABLED" in red

---

### User Story 6 - Backtesting Results Viewer (Priority: P4)

As a trader, I want to view backtesting results through the web interface so that I can analyze strategy performance across historical data with rich visualizations.

**Why this priority**: Backtesting is an advanced feature typically used during strategy development. The CLI provides adequate functionality for now, and this can be enhanced later with charts and better UX.

**Independent Test**: Can be tested by running a backtest (via CLI or API) and viewing the results through the web interface with performance charts and trade details. Delivers value by providing visual analysis of strategy effectiveness.

**Acceptance Scenarios**:

1. **Given** a trader has run backtests, **When** they navigate to the backtest results page, **Then** they see a list of all completed backtests with strategy name, symbol, date range, and total return
2. **Given** the trader selects a backtest result, **When** the detailed view loads, **Then** they see backtest information (ID, strategy, symbol, period, duration), performance summary (initial capital, final equity, total P&L, return %), and trading statistics
3. **Given** the trader is viewing backtest details, **When** they scroll through results, **Then** they see the top 5 winning trades and top 5 losing trades displayed side-by-side
4. **Given** the backtest has an equity curve, **When** the trader views performance, **Then** an equity curve chart shows portfolio value progression throughout the backtest period
5. **Given** the trader wants to compare backtests, **When** they select multiple results, **Then** they can view key metrics side-by-side in a comparison table

---

### Edge Cases

- What happens when the backend service is unavailable or returns errors?
  - System displays user-friendly error message indicating service unavailability
  - Dashboard shows last known data with a timestamp and warning indicator
  - Automatic reconnection attempts occur in the background

- How does system handle concurrent users viewing the same account?
  - All users see the same real-time data synchronized via SignalR
  - Actions taken by one user (e.g., closing position) immediately reflect for all viewers

- What happens when real-time updates fail or disconnect?
  - System displays a "Disconnected" indicator
  - Automatic reconnection attempts occur every 5 seconds
  - Upon reconnection, dashboard data is refreshed to current state

- How does system handle large trade history (thousands of trades)?
  - Portfolio history implements pagination (25 trades per page by default)
  - Export functionality handles large datasets via background job with download link

- What happens when user has no trading activity (no positions, no trades)?
  - Dashboard displays appropriate empty states with helpful messaging
  - Account information still shows equity, cash, and buying power
  - Help text guides users on how to start trading or configure strategies

- How does system handle invalid input in risk settings?
  - Client-side validation prevents submission of out-of-range values
  - Server-side validation returns clear error messages
  - Form retains user input and highlights specific field with error

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display real-time account information including equity, cash balance, position value, buying power, unrealized P&L, and realized P&L
- **FR-002**: System MUST display all open positions with symbol, side (buy/sell), quantity, entry price, current price, unrealized P&L, P&L percentage, and associated strategy name
- **FR-003**: System MUST update position values and P&L automatically when market prices change without requiring manual refresh
- **FR-004**: System MUST display the 5 most recent completed trades with exit date/time, symbol, side, quantity, realized P&L, and associated strategy
- **FR-005**: System MUST display performance metrics including total return, win rate, total trades, winning/losing trade counts, Sharpe ratio, Sortino ratio, Calmar ratio, max drawdown, and profit factor
- **FR-006**: System MUST display current risk settings including leverage, stop-loss percentage, take-profit percentage, daily loss limit, max drawdown percentage, max position size percentage, and enabled/disabled status
- **FR-007**: System MUST provide a portfolio history view showing all completed trades with filtering by date range, symbol, and strategy
- **FR-008**: System MUST allow users to close open positions with confirmation dialog showing position details
- **FR-009**: System MUST display trading strategies with their name, type, associated symbols, timeframe, and active/disabled status
- **FR-010**: System MUST allow users to enable or disable trading strategies
- **FR-011**: System MUST allow users to modify risk settings including stop-loss percentage, take-profit percentage, leverage, daily loss limit, max drawdown, and max position size
- **FR-012**: System MUST display backtest results including backtest ID, strategy name, symbol, date range, duration, initial capital, final equity, total P&L, total return, and performance statistics
- **FR-013**: System MUST display equity curve visualization showing portfolio value over time for backtests
- **FR-014**: System MUST show the top 5 winning and top 5 losing trades for each backtest
- **FR-015**: System MUST export portfolio history to downloadable file format
- **FR-016**: System MUST authenticate users before allowing access to trading data
- **FR-017**: System MUST maintain real-time data synchronization across multiple browser tabs/windows for the same user
- **FR-018**: System MUST display appropriate empty states when no data is available (no positions, no trades, etc.)
- **FR-019**: System MUST validate all user input on risk settings and provide clear error messages for invalid values
- **FR-020**: System MUST maintain connection state and display disconnection warnings when real-time updates are interrupted
- **FR-021**: System MUST automatically attempt to reconnect when real-time connection is lost
- **FR-022**: System MUST implement pagination for large data sets (trade history, backtest results)
- **FR-023**: System MUST use color coding for financial metrics (green for positive, red for negative, yellow for warnings)
- **FR-024**: System MUST display timestamps showing when data was last updated
- **FR-025**: System MUST provide responsive layout that works on desktop browsers (mobile optimization is out of scope)

### Key Entities

- **Account**: Represents trading account with equity, cash, position value, buying power, unrealized P&L, realized P&L, and account ID
- **Position**: Represents open trading position with symbol, side (buy/sell), quantity, entry price, current price, unrealized P&L, strategy association, and position ID
- **Trade**: Represents completed trade with entry time, exit time, symbol, side, quantity, entry price, exit price, realized P&L, commission, strategy association, and trade ID
- **Strategy**: Represents trading strategy with name, type, associated symbols, timeframe, enabled status, and parameters
- **Performance Metrics**: Aggregated statistics including total return, win rate, Sharpe/Sortino/Calmar ratios, max drawdown, profit factor, total/winning/losing trade counts, average win/loss
- **Risk Settings**: Configuration for risk management including leverage, stop-loss percentage, take-profit percentage, daily loss limit, max drawdown percentage, max position size percentage, and enabled status
- **Backtest Result**: Historical simulation results including backtest ID, strategy, symbol, date range, initial capital, final equity, total P&L, return percentage, equity curve data, and trade list
- **User Session**: Web session information for authentication and real-time connection management

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Traders can view their complete dashboard (account, positions, trades, performance, risk) within 2 seconds of page load
- **SC-002**: Position values and P&L update in real-time with maximum 2-second lag from market data changes
- **SC-003**: Traders can close a position through the web interface with confirmation in under 10 seconds from click to completion
- **SC-004**: Portfolio history page loads and displays up to 1000 trades with pagination in under 3 seconds
- **SC-005**: Performance metrics page renders all statistics and charts within 3 seconds
- **SC-006**: Real-time connection automatically reconnects within 10 seconds of disconnection without user intervention
- **SC-007**: System supports at least 50 concurrent users viewing dashboards without performance degradation
- **SC-008**: All risk setting changes are saved and reflected in the dashboard within 1 second
- **SC-009**: 95% of users can successfully navigate to and view their portfolio without assistance on first use
- **SC-010**: Dashboard remains responsive (page interactions under 100ms) even when displaying live-updating data
- **SC-011**: Export functionality generates downloadable files for up to 10,000 trades within 30 seconds
- **SC-012**: All empty states provide clear messaging that helps users understand next steps with 90% comprehension rate in user testing

## Assumptions

- Users access the web application using modern browsers (Chrome, Edge, Firefox, Safari - last 2 versions)
- The existing TradingBot.Core, TradingBot.Infrastructure, and TradingBot.Engine layers will be reused without modification
- Users are already authenticated through an external authentication mechanism (authentication UI is out of scope for this feature)
- Real-time updates will use ASP.NET Core SignalR for WebSocket communication
- The same SQLite database used by the CLI will be shared with the Blazor app
- Desktop-only responsive design is sufficient; mobile optimization is out of scope
- Users will access the dashboard from a trusted network (additional security hardening beyond basic authentication is out of scope)
- The Blazor app will run on the same machine as the trading engine for initial deployment
- Performance requirements assume typical retail trading volumes (up to 1000 trades per month per user)
- Chart visualizations will use standard charting libraries available in the Blazor ecosystem
- The application will use Blazor Server rendering mode (not Blazor WebAssembly or static SSR)

## Dependencies

- Existing TradingBot.Core domain models and interfaces must remain unchanged
- TradingBot.Infrastructure database access layer for reading account, position, trade, and strategy data
- TradingBot.Engine for risk management and portfolio operations
- TradingBot.Analytics for performance metrics calculations
- Market data provider (Yahoo Finance integration) for current price updates
- ASP.NET Core SignalR for real-time updates
- Blazor Server framework (.NET 9)
- Charting library for equity curve and performance visualizations

## Out of Scope

- Mobile application or mobile-responsive layout
- User registration and account creation (authentication assumed to exist)
- Strategy creation or editing interfaces (strategy configuration remains CLI-only)
- Direct order placement through the web interface (order execution is strategy-driven only)
- Multi-user account management or admin interfaces
- Historical chart analysis tools (candlestick charts, technical indicators)
- Alerts and notification system beyond basic connection status
- Theme customization or user preference settings
- API exposure for third-party integration
- Real-time chat or collaboration features
- Advanced backtesting configuration through UI (backtest execution remains CLI-only for now)
