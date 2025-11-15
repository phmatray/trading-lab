using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DddRefactoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DailyLossLimit",
                table: "RiskSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Leverage",
                table: "RiskSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDrawdownPercent",
                table: "RiskSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "RiskLimitsEnabled",
                table: "RiskSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "drawdown",
                table: "equity_points",
                type: "TEXT",
                precision: 10,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "peak",
                table: "equity_points",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "return_percent",
                table: "equity_points",
                type: "TEXT",
                precision: 10,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "RiskSettings",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "DailyLossLimit", "Leverage", "MaxDrawdownPercent", "RiskLimitsEnabled" },
                values: new object[] { 1000m, 1.0m, 10.0m, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyLossLimit",
                table: "RiskSettings");

            migrationBuilder.DropColumn(
                name: "Leverage",
                table: "RiskSettings");

            migrationBuilder.DropColumn(
                name: "MaxDrawdownPercent",
                table: "RiskSettings");

            migrationBuilder.DropColumn(
                name: "RiskLimitsEnabled",
                table: "RiskSettings");

            migrationBuilder.DropColumn(
                name: "drawdown",
                table: "equity_points");

            migrationBuilder.DropColumn(
                name: "peak",
                table: "equity_points");

            migrationBuilder.DropColumn(
                name: "return_percent",
                table: "equity_points");
        }
    }
}
