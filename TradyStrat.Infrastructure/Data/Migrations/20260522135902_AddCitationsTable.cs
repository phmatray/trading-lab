using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradyStrat.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCitationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Suggestions_Instruments_InstrumentId",
                table: "Suggestions");

            migrationBuilder.AlterColumn<string>(
                name: "ThinkingText",
                table: "Suggestions",
                type: "TEXT",
                maxLength: 20000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityHint",
                table: "Suggestions",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PromptVersionHash",
                table: "Suggestions",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxPriceHint",
                table: "Suggestions",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MarketSnapshotJson",
                table: "Suggestions",
                type: "TEXT",
                maxLength: 20000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 8000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EnvelopeHash",
                table: "Suggestions",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CitationsJson",
                table: "Suggestions",
                type: "TEXT",
                maxLength: 8000,
                nullable: true,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 8000);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Suggestions",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "MaxPriceHintCurrency",
                table: "Suggestions",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "MaxPriceHintIsEmpty",
                table: "Suggestions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "QuantityHintIsSpecified",
                table: "Suggestions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Positions",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.CreateTable(
                name: "Citations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Claim = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Indicator = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Ticker = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    SuggestionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Citations_Suggestions_SuggestionId",
                        column: x => x.SuggestionId,
                        principalTable: "Suggestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Citations_SuggestionId",
                table: "Citations",
                column: "SuggestionId");

            // Backfill: parse each Suggestion's CitationsJson into the new Citations
            // table. SQLite's json_each UDF (built-in since 3.38) expands JSON arrays
            // into rows; we insert one Citations row per element.
            migrationBuilder.Sql(@"
                INSERT INTO Citations (SuggestionId, Claim, Indicator, Ticker, Value)
                SELECT s.Id,
                       COALESCE(json_extract(c.value, '$.claim'),     '') AS Claim,
                       COALESCE(json_extract(c.value, '$.indicator'), '') AS Indicator,
                       COALESCE(json_extract(c.value, '$.ticker'),    '') AS Ticker,
                       COALESCE(json_extract(c.value, '$.value'),     '') AS Value
                FROM Suggestions s, json_each(s.CitationsJson) c
                WHERE s.CitationsJson IS NOT NULL
                  AND s.CitationsJson != ''
                  AND s.CitationsJson != '[]';");

            // Backfill VO-shadow columns for existing rows:
            //   * MaxPriceHintCurrency: legacy hints were always EUR (project default).
            //   * MaxPriceHintIsEmpty: rows with a stored non-null price are NOT empty.
            //   * QuantityHintIsSpecified: rows with a stored non-null quantity are specified.
            // The AlterColumn rebuild has already replaced legacy NULL with '0.0'; we
            // can no longer distinguish NULL-from-0 at this point, so we treat the
            // sentinel '0.0' as "absent". This matches how the AI tool returned hints
            // in practice — concrete hints were always > 0.
            migrationBuilder.Sql(@"
                UPDATE Suggestions
                SET MaxPriceHintCurrency = 'EUR',
                    MaxPriceHintIsEmpty = CASE WHEN MaxPriceHint = '0.0' OR MaxPriceHint = '0' THEN 1 ELSE 0 END,
                    QuantityHintIsSpecified = CASE WHEN QuantityHint = '0.0' OR QuantityHint = '0' THEN 0 ELSE 1 END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Citations");

            migrationBuilder.DropColumn(
                name: "MaxPriceHintCurrency",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "MaxPriceHintIsEmpty",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "QuantityHintIsSpecified",
                table: "Suggestions");

            migrationBuilder.AlterColumn<string>(
                name: "ThinkingText",
                table: "Suggestions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20000);

            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityHint",
                table: "Suggestions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "PromptVersionHash",
                table: "Suggestions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxPriceHint",
                table: "Suggestions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "MarketSnapshotJson",
                table: "Suggestions",
                type: "TEXT",
                maxLength: 8000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20000);

            migrationBuilder.AlterColumn<string>(
                name: "EnvelopeHash",
                table: "Suggestions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "CitationsJson",
                table: "Suggestions",
                type: "TEXT",
                maxLength: 8000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 8000,
                oldNullable: true,
                oldDefaultValue: "[]");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Suggestions_Instruments_InstrumentId",
                table: "Suggestions",
                column: "InstrumentId",
                principalTable: "Instruments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
