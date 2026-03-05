using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyCashManagedStrategy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "weekly_cash_managed_strategies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    etp_symbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    underlying_symbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    is_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    min_cash_ratio = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: false),
                    max_cash_ratio = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: false),
                    weekly_buy_ratio = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: false),
                    weekly_sell_ratio = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: false),
                    execution_day_of_week = table.Column<int>(type: "INTEGER", nullable: false),
                    days_below_ma20 = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    last_execution_timestamp = table.Column<DateTime>(type: "TEXT", nullable: true),
                    last_daily_update_timestamp = table.Column<DateTime>(type: "TEXT", nullable: true),
                    current_ma20 = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    current_underlying_price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    current_etp_price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    breakout_rule_config_json = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_cash_managed_strategies", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_wcm_strategy_etp_symbol",
                table: "weekly_cash_managed_strategies",
                column: "etp_symbol");

            migrationBuilder.CreateIndex(
                name: "idx_wcm_strategy_is_enabled",
                table: "weekly_cash_managed_strategies",
                column: "is_enabled");

            migrationBuilder.CreateIndex(
                name: "idx_wcm_strategy_last_execution",
                table: "weekly_cash_managed_strategies",
                column: "last_execution_timestamp");

            migrationBuilder.CreateIndex(
                name: "idx_wcm_strategy_name",
                table: "weekly_cash_managed_strategies",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_wcm_strategy_underlying_symbol",
                table: "weekly_cash_managed_strategies",
                column: "underlying_symbol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "weekly_cash_managed_strategies");
        }
    }
}
