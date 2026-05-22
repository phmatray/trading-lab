using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradyStrat.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalMoneyColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FxRates_Base_Quote_Date",
                table: "FxRates");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Suggestions",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Positions",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Instruments",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "TargetDate",
                table: "Goals",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetCurrency",
                table: "Goals",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.AddColumn<bool>(
                name: "TargetIsEmpty",
                table: "Goals",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE Goals SET TargetCurrency = 'EUR' WHERE TargetCurrency = '' OR TargetCurrency IS NULL;");
            migrationBuilder.Sql("UPDATE Goals SET TargetDate = '0001-01-01' WHERE TargetDate IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetCurrency",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "TargetIsEmpty",
                table: "Goals");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Suggestions",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Positions",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Instruments",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "TargetDate",
                table: "Goals",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_FxRates_Base_Quote_Date",
                table: "FxRates",
                columns: new[] { "Base", "Quote", "Date" },
                unique: true);
        }
    }
}
