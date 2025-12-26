using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingStrat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBacktestRunsAndActivityEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BacktestRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ticker = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StrategyType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StrategyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StrategyParametersJson = table.Column<string>(type: "TEXT", nullable: false),
                    ConfigJson = table.Column<string>(type: "TEXT", nullable: false),
                    ResultsJson = table.Column<string>(type: "TEXT", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ExecutionTimeMs = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BacktestRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEvents_EventType",
                table: "ActivityEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEvents_Timestamp",
                table: "ActivityEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_BacktestRuns_ExecutedAt",
                table: "BacktestRuns",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BacktestRuns_StrategyType",
                table: "BacktestRuns",
                column: "StrategyType");

            migrationBuilder.CreateIndex(
                name: "IX_BacktestRuns_Ticker",
                table: "BacktestRuns",
                column: "Ticker");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityEvents");

            migrationBuilder.DropTable(
                name: "BacktestRuns");
        }
    }
}
