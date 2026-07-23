using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingStrat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPythonStrategySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PythonCode",
                table: "CustomStrategies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PythonCodeVersion",
                table: "CustomStrategies",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StrategyType",
                table: "CustomStrategies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CustomStrategies_StrategyType",
                table: "CustomStrategies",
                column: "StrategyType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomStrategies_StrategyType",
                table: "CustomStrategies");

            migrationBuilder.DropColumn(
                name: "PythonCode",
                table: "CustomStrategies");

            migrationBuilder.DropColumn(
                name: "PythonCodeVersion",
                table: "CustomStrategies");

            migrationBuilder.DropColumn(
                name: "StrategyType",
                table: "CustomStrategies");
        }
    }
}
