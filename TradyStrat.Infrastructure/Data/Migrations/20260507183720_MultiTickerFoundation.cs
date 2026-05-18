using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradyStrat.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MultiTickerFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Seed the three known instruments. The Instruments table itself
            //    was created by the prior AddInstrumentsTable migration.
            var seedAt = DateTime.UtcNow;
            migrationBuilder.InsertData(
                table: "Instruments",
                columns: new[] { "Ticker", "Name", "Currency", "Exchange", "TimezoneId", "Kind", "AddedAt" },
                values: new object[,]
                {
                    { "CON3.L",  "Leverage Shares 3x Long Coinbase", "USD", "LSE", "Europe/London",     0, seedAt },
                    { "COIN",    "Coinbase Global, Inc.",            "USD", "NMS", "America/New_York",  1, seedAt },
                    { "BTC-USD", "Bitcoin USD",                      "USD", "CCC", "UTC",               1, seedAt },
                });

            // 2. Trades.InstrumentId — added nullable, backfilled, then made NOT NULL.
            migrationBuilder.AddColumn<int>(
                name: "InstrumentId",
                table: "Trades",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE Trades
                   SET InstrumentId = (SELECT Id FROM Instruments WHERE Ticker = 'CON3.L')
                 WHERE InstrumentId IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "InstrumentId",
                table: "Trades",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trades_InstrumentId_ExecutedOn",
                table: "Trades",
                columns: new[] { "InstrumentId", "ExecutedOn" });

            migrationBuilder.AddForeignKey(
                name: "FK_Trades_Instruments_InstrumentId",
                table: "Trades",
                column: "InstrumentId",
                principalTable: "Instruments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 3. FxRates rebuild — Pair → (Base, Quote); UsdPerEur → Rate.
            //    Existing rows are mapped Base='EUR', Quote='USD'.
            migrationBuilder.DropIndex(
                name: "IX_FxRates_Pair_Date",
                table: "FxRates");

            migrationBuilder.Sql(@"
                CREATE TABLE FxRates_New (
                    Id INTEGER NOT NULL CONSTRAINT PK_FxRates PRIMARY KEY AUTOINCREMENT,
                    Date TEXT NOT NULL,
                    Base TEXT NOT NULL,
                    Quote TEXT NOT NULL,
                    Rate TEXT NOT NULL,
                    FetchedAt TEXT NOT NULL
                );

                INSERT INTO FxRates_New (Id, Date, Base, Quote, Rate, FetchedAt)
                SELECT Id, Date, 'EUR', 'USD', UsdPerEur, FetchedAt FROM FxRates;

                DROP TABLE FxRates;
                ALTER TABLE FxRates_New RENAME TO FxRates;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_FxRates_Base_Quote_Date",
                table: "FxRates",
                columns: new[] { "Base", "Quote", "Date" },
                unique: true);

            // 4. Goals — drop FocusTicker. SQLite 3.35+ supports DROP COLUMN
            //    directly, so no table rebuild is required.
            migrationBuilder.DropColumn(
                name: "FocusTicker",
                table: "Goals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
            => throw new NotSupportedException(
                "Phase 1 multi-ticker migration is forward-only. Restore from a pre-migration DB copy.");
    }
}
