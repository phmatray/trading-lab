using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradyStrat.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioAndPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trades_Instruments_InstrumentId",
                table: "Trades");

            migrationBuilder.RenameColumn(
                name: "PricePerShare",
                table: "Trades",
                newName: "PricePerShareAmount");

            migrationBuilder.RenameColumn(
                name: "FeesEur",
                table: "Trades",
                newName: "FeesAmount");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Trades",
                type: "TEXT",
                maxLength: 2000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Trades",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "FeesCurrency",
                table: "Trades",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "FeesIsEmpty",
                table: "Trades",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PositionId",
                table: "Trades",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PricePerShareCurrency",
                table: "Trades",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PricePerShareIsEmpty",
                table: "Trades",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "QuantityIsSpecified",
                table: "Trades",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Portfolios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portfolios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    InstrumentId = table.Column<int>(type: "INTEGER", nullable: false),
                    PortfolioId = table.Column<int>(type: "INTEGER", nullable: true),
                    RealizedPnLAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    RealizedPnLCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    RealizedPnLIsEmpty = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PositionLots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OpenedOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    QuantityIsSpecified = table.Column<bool>(type: "INTEGER", nullable: false),
                    UnitCostAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitCostCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    UnitCostIsEmpty = table.Column<bool>(type: "INTEGER", nullable: false),
                    PositionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PositionLots_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trades_PositionId",
                table: "Trades",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionLots_PositionId",
                table: "PositionLots",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_PortfolioId",
                table: "Positions",
                column: "PortfolioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trades_Positions_PositionId",
                table: "Trades",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // ---- Backfill (Phase 2 spec §7) ----
            // Singleton Portfolio
            migrationBuilder.Sql("INSERT INTO Portfolios (Id) VALUES (1);");

            // One Position per distinct InstrumentId from existing Trades.
            // Currency assumed EUR for legacy single-portfolio flow; will be
            // refined when Trades begin to record per-instrument-currency in
            // the new shape. RealizedPnL starts at zero EUR.
            migrationBuilder.Sql(@"
                INSERT INTO Positions (Id, PortfolioId, InstrumentId, RealizedPnLAmount, RealizedPnLCurrency, RealizedPnLIsEmpty)
                SELECT
                    (ROW_NUMBER() OVER (ORDER BY InstrumentId)) AS Id,
                    1 AS PortfolioId,
                    InstrumentId,
                    '0' AS RealizedPnLAmount,
                    'EUR' AS RealizedPnLCurrency,
                    0 AS RealizedPnLIsEmpty
                FROM (SELECT DISTINCT InstrumentId FROM Trades) AS d;");

            // Link each Trade to its Position via the denormalized InstrumentId column.
            migrationBuilder.Sql(@"
                UPDATE Trades SET PositionId = (
                    SELECT p.Id FROM Positions p WHERE p.InstrumentId = Trades.InstrumentId
                );");

            // Populate the new owned-Money columns with EUR convention for legacy data.
            migrationBuilder.Sql("UPDATE Trades SET PricePerShareCurrency = 'EUR' WHERE PricePerShareCurrency = '';");
            migrationBuilder.Sql("UPDATE Trades SET FeesCurrency = 'EUR' WHERE FeesCurrency = '';");
            migrationBuilder.Sql("UPDATE Trades SET QuantityIsSpecified = 1;");

            // Trade.Id is reassigned sequentially per-position by EfPortfolioRepository.GetAsync's
            // rehydration path (Task 25) — runs once when _openLots is empty but _trades is non-empty
            // (true for every position after this migration).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trades_Positions_PositionId",
                table: "Trades");

            migrationBuilder.DropTable(
                name: "PositionLots");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "Portfolios");

            migrationBuilder.DropIndex(
                name: "IX_Trades_PositionId",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "FeesCurrency",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "FeesIsEmpty",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "PositionId",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "PricePerShareCurrency",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "PricePerShareIsEmpty",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "QuantityIsSpecified",
                table: "Trades");

            migrationBuilder.RenameColumn(
                name: "PricePerShareAmount",
                table: "Trades",
                newName: "PricePerShare");

            migrationBuilder.RenameColumn(
                name: "FeesAmount",
                table: "Trades",
                newName: "FeesEur");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Trades",
                type: "TEXT",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Trades",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddForeignKey(
                name: "FK_Trades_Instruments_InstrumentId",
                table: "Trades",
                column: "InstrumentId",
                principalTable: "Instruments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
