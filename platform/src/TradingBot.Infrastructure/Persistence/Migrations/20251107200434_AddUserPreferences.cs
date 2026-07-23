using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Theme = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DashboardRefreshInterval = table.Column<int>(type: "INTEGER", nullable: false),
                    NotificationDuration = table.Column<int>(type: "INTEGER", nullable: false),
                    ShowSuccessNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowErrorNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowInfoNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowWarningNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    CustomSettings = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                table: "UserPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPreferences");
        }
    }
}
