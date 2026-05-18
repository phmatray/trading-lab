using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradyStrat.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSuggestionMarketSnapshotJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MarketSnapshotJson",
                table: "Suggestions",
                type: "TEXT",
                maxLength: 8000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarketSnapshotJson",
                table: "Suggestions");
        }
    }
}
