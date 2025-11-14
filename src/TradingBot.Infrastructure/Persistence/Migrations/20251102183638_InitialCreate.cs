using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    account_id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    equity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    cash = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    position_value = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    buying_power = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    leverage = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false, defaultValue: 1m),
                    unrealized_pnl = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    realized_pnl = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.account_id);
                });

            migrationBuilder.CreateTable(
                name: "candles",
                columns: table => new
                {
                    symbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    timeframe = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    open = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    high = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    low = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    close = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    volume = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candles", x => new { x.symbol, x.timestamp, x.timeframe });
                });

            migrationBuilder.CreateTable(
                name: "equity_points",
                columns: table => new
                {
                    timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    equity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    cumulative_return = table.Column<decimal>(type: "TEXT", precision: 10, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equity_points", x => x.timestamp);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    symbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    side = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    limit_price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    stop_price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    filled_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    filled_quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false, defaultValue: 0m),
                    average_fill_price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    commission = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    strategy_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    signal_id = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    symbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    side = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    entry_price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    current_price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    opened_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    stop_loss = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    take_profit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    strategy_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_positions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quotes",
                columns: table => new
                {
                    symbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    bid = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ask = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    volume = table.Column<long>(type: "INTEGER", nullable: false),
                    change = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    change_percent = table.Column<decimal>(type: "TEXT", precision: 10, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotes", x => new { x.symbol, x.timestamp });
                });

            migrationBuilder.CreateTable(
                name: "trades",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    symbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    side = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    entry_price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    exit_price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    entry_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    exit_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    commission = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    strategy_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trades", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_candles_symbol",
                table: "candles",
                column: "symbol");

            migrationBuilder.CreateIndex(
                name: "idx_candles_symbol_timeframe",
                table: "candles",
                columns: new[] { "symbol", "timeframe" });

            migrationBuilder.CreateIndex(
                name: "idx_candles_timestamp",
                table: "candles",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "idx_equity_points_timestamp",
                table: "equity_points",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "idx_orders_created_at",
                table: "orders",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_orders_status",
                table: "orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_orders_strategy",
                table: "orders",
                column: "strategy_name");

            migrationBuilder.CreateIndex(
                name: "idx_orders_symbol",
                table: "orders",
                column: "symbol");

            migrationBuilder.CreateIndex(
                name: "idx_positions_opened_at",
                table: "positions",
                column: "opened_at");

            migrationBuilder.CreateIndex(
                name: "idx_positions_strategy",
                table: "positions",
                column: "strategy_name");

            migrationBuilder.CreateIndex(
                name: "idx_positions_symbol",
                table: "positions",
                column: "symbol");

            migrationBuilder.CreateIndex(
                name: "idx_quotes_symbol",
                table: "quotes",
                column: "symbol");

            migrationBuilder.CreateIndex(
                name: "idx_quotes_timestamp",
                table: "quotes",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "idx_trades_entry_time",
                table: "trades",
                column: "entry_time");

            migrationBuilder.CreateIndex(
                name: "idx_trades_exit_time",
                table: "trades",
                column: "exit_time");

            migrationBuilder.CreateIndex(
                name: "idx_trades_strategy",
                table: "trades",
                column: "strategy_name");

            migrationBuilder.CreateIndex(
                name: "idx_trades_symbol",
                table: "trades",
                column: "symbol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accounts");

            migrationBuilder.DropTable(
                name: "candles");

            migrationBuilder.DropTable(
                name: "equity_points");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "positions");

            migrationBuilder.DropTable(
                name: "quotes");

            migrationBuilder.DropTable(
                name: "trades");
        }
    }
}
