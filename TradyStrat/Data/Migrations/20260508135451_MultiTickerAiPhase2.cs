using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradyStrat.Data.Migrations
{
    /// <inheritdoc />
    public partial class MultiTickerAiPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add nullable InstrumentId column.
            migrationBuilder.AddColumn<int>(
                name: "InstrumentId",
                table: "Suggestions",
                type: "INTEGER",
                nullable: true);

            // 2. Backfill: every existing Suggestion row was for the focus ticker
            //    (CON3.L) at the time this migration was authored — Phase 1
            //    hardcoded the AI loop to the configured focus and there's only
            //    ever been one Suggestion per ForDate. Hardcoded literal matches
            //    the precedent set by the Trades.InstrumentId backfill in
            //    MultiTickerFoundation (Phase 1).
            //
            //    Caveat (see spec §3.4): if the user changed Tickers:Focus
            //    between Phase 1 and Phase 2 *and* generated Suggestions for the
            //    new focus before running this migration, the backfill would
            //    mis-attribute those rows. Accepted as a Phase-2-author-time
            //    assumption; no production guard.
            migrationBuilder.Sql(@"
                UPDATE Suggestions
                   SET InstrumentId = (SELECT Id FROM Instruments WHERE Ticker = 'CON3.L')
                 WHERE InstrumentId IS NULL;");

            // 3. Make NOT NULL + add FK + composite UQ; drop old single-column UQ.
            migrationBuilder.AlterColumn<int>(
                name: "InstrumentId",
                table: "Suggestions",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Suggestions_Instruments_InstrumentId",
                table: "Suggestions",
                column: "InstrumentId",
                principalTable: "Instruments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropIndex(
                name: "IX_Suggestions_ForDate",
                table: "Suggestions");

            migrationBuilder.CreateIndex(
                name: "IX_Suggestions_ForDate_InstrumentId",
                table: "Suggestions",
                columns: new[] { "ForDate", "InstrumentId" },
                unique: true);

            // EF auto-emits a non-unique helper index for the FK column. Kept
            // here to match what the AppDbContextModelSnapshot encodes — without
            // it, the snapshot drifts from the actual schema and a future
            // migration generation will produce phantom diffs.
            migrationBuilder.CreateIndex(
                name: "IX_Suggestions_InstrumentId",
                table: "Suggestions",
                column: "InstrumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
            => throw new NotSupportedException(
                "Phase 2 multi-ticker-AI migration is forward-only. Restore from a pre-migration DB copy.");
    }
}
