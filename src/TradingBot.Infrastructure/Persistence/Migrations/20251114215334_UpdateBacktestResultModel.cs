using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBacktestResultModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TakeProfitPercent",
                table: "RiskSettings",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 5m);

            migrationBuilder.AlterColumn<decimal>(
                name: "StopLossPercent",
                table: "RiskSettings",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 2m);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxPositionSizePercent",
                table: "RiskSettings",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 10m);

            migrationBuilder.AlterColumn<int>(
                name: "MaxOpenPositions",
                table: "RiskSettings",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 5);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxDailyLossPercent",
                table: "RiskSettings",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 5m);

            migrationBuilder.UpdateData(
                table: "RiskSettings",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "LastModified" },
                values: new object[] { new DateTime(2025, 1, 14, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 14, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TakeProfitPercent",
                table: "RiskSettings",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 5m,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "StopLossPercent",
                table: "RiskSettings",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 2m,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxPositionSizePercent",
                table: "RiskSettings",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 10m,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "MaxOpenPositions",
                table: "RiskSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 5,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxDailyLossPercent",
                table: "RiskSettings",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 5m,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.UpdateData(
                table: "RiskSettings",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "LastModified" },
                values: new object[] { new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }
    }
}
