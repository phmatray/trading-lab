using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradyStrat.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AiSuggestionPhase3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnvelopeHash",
                table: "Suggestions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptVersionHash",
                table: "Suggestions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThinkingText",
                table: "Suggestions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnvelopeHash",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "PromptVersionHash",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "ThinkingText",
                table: "Suggestions");
        }
    }
}
