using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingStrat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSourcingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Portfolios",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    StreamId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    EventData = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => new { x.StreamId, x.Version });
                });

            migrationBuilder.CreateTable(
                name: "Snapshots",
                columns: table => new
                {
                    AggregateId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AggregateType = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    SnapshotData = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshots", x => x.AggregateId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_StreamId_Version",
                table: "Events",
                columns: new[] { "StreamId", "Version" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Snapshots");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Portfolios");
        }
    }
}
