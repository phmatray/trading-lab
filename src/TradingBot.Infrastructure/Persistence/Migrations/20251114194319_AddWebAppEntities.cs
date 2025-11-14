using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWebAppEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BacktestResults",
                columns: table => new
                {
                    BacktestId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StrategyName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    InitialCapital = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    FinalEquity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SharpeRatio = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    MaxDrawdown = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    WinRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    ProfitFactor = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    TotalTrades = table.Column<int>(type: "INTEGER", nullable: false),
                    TradesJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    EquityCurveJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BacktestResults", x => x.BacktestId);
                });

            migrationBuilder.CreateTable(
                name: "RiskSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MaxPositionSizePercent = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 10m),
                    StopLossPercent = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 2m),
                    TakeProfitPercent = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 5m),
                    MaxOpenPositions = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 5),
                    MaxDailyLossPercent = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 5m),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StrategyConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StrategyName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ParametersJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyConfigurations", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "RiskSettings",
                columns: new[] { "Id", "CreatedAt", "LastModified", "MaxDailyLossPercent", "MaxOpenPositions", "MaxPositionSizePercent", "StopLossPercent", "TakeProfitPercent" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5m, 5, 10m, 2m, 5m });

            migrationBuilder.CreateIndex(
                name: "IX_BacktestResults_CreatedAt",
                table: "BacktestResults",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_BacktestResults_StrategySymbol",
                table: "BacktestResults",
                columns: new[] { "StrategyName", "Symbol" });

            migrationBuilder.CreateIndex(
                name: "IX_StrategyConfigurations_StrategyName",
                table: "StrategyConfigurations",
                column: "StrategyName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BacktestResults");

            migrationBuilder.DropTable(
                name: "RiskSettings");

            migrationBuilder.DropTable(
                name: "StrategyConfigurations");
        }
    }
}
