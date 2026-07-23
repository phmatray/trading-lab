using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingStrat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Ticker = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomStrategies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Author = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DefinitionJson = table.Column<string>(type: "TEXT", nullable: false),
                    TimesUsed = table.Column<int>(type: "INTEGER", nullable: false),
                    LastBacktestReturn = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    LastBacktestDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomStrategies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HistoricalPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ticker = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ISIN = table.Column<string>(type: "TEXT", maxLength: 12, nullable: true),
                    TimeFrame = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    DateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Open = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: true),
                    High = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: true),
                    Low = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: true),
                    Close = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: true),
                    AdjustedClose = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: true),
                    Volume = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricalPrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Portfolios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Cash = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portfolios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Securities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ticker = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ISIN = table.Column<string>(type: "TEXT", maxLength: 12, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    SecurityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Exchange = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Securities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StrategyRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ticker = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StrategyType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    Recommendation = table.Column<string>(type: "TEXT", nullable: false),
                    ActionItems = table.Column<string>(type: "TEXT", nullable: false),
                    Confidence = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyRecommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CashTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PortfolioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashTransactions_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PortfolioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Ticker = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    EntryPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    EntryDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_Portfolio_Date",
                table: "CashTransactions",
                columns: new[] { "PortfolioId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_Session_Timestamp",
                table: "ChatMessages",
                columns: new[] { "SessionId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomStrategies_Author_CreatedAt",
                table: "CustomStrategies",
                columns: new[] { "Author", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomStrategies_Category",
                table: "CustomStrategies",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalPrices_ISIN",
                table: "HistoricalPrices",
                column: "ISIN");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalPrices_Ticker_TimeFrame",
                table: "HistoricalPrices",
                columns: new[] { "Ticker", "TimeFrame" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalPrices_Ticker_TimeFrame_DateTime",
                table: "HistoricalPrices",
                columns: new[] { "Ticker", "TimeFrame", "DateTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Portfolios_Name",
                table: "Portfolios",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_Portfolio_Ticker",
                table: "Positions",
                columns: new[] { "PortfolioId", "Ticker" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Securities_ISIN",
                table: "Securities",
                column: "ISIN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Securities_Ticker",
                table: "Securities",
                column: "Ticker",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrategyRecommendations_Ticker_CreatedAt",
                table: "StrategyRecommendations",
                columns: new[] { "Ticker", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashTransactions");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "CustomStrategies");

            migrationBuilder.DropTable(
                name: "HistoricalPrices");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "Securities");

            migrationBuilder.DropTable(
                name: "StrategyRecommendations");

            migrationBuilder.DropTable(
                name: "Portfolios");
        }
    }
}
