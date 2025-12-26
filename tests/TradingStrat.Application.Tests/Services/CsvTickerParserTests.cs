using Shouldly;
using TradingStrat.Application.Services;

namespace TradingStrat.Application.Tests.Services;

public class CsvTickerParserTests : IDisposable
{
    private readonly CsvTickerParser _parser;
    private readonly string _testDirectory;

    public CsvTickerParserTests()
    {
        _parser = new CsvTickerParser();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"TradingStratTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void ParseTickers_WithCommaSeparated_ReturnsValidTickers()
    {
        // Arrange
        string csvContent = "AAPL,GOOGL,MSFT";

        // Act
        TickerImportResult result = _parser.ParseTickers(csvContent);

        // Assert
        result.ValidTickers.Count.ShouldBe(3);
        result.ValidTickers.ShouldContain("AAPL");
        result.ValidTickers.ShouldContain("GOOGL");
        result.ValidTickers.ShouldContain("MSFT");
        result.InvalidTickers.ShouldBeEmpty();
    }

    [Fact]
    public void ParseTickers_WithNewlineSeparated_ReturnsValidTickers()
    {
        // Arrange
        string csvContent = "AAPL\nGOOGL\nMSFT";

        // Act
        TickerImportResult result = _parser.ParseTickers(csvContent);

        // Assert
        result.ValidTickers.Count.ShouldBe(3);
        result.ValidTickers.ShouldContain("AAPL");
        result.ValidTickers.ShouldContain("GOOGL");
        result.ValidTickers.ShouldContain("MSFT");
    }

    [Fact]
    public void ParseTickers_WithSemicolonSeparated_ReturnsValidTickers()
    {
        // Arrange
        string csvContent = "AAPL;GOOGL;MSFT";

        // Act
        TickerImportResult result = _parser.ParseTickers(csvContent);

        // Assert
        result.ValidTickers.Count.ShouldBe(3);
    }

    [Fact]
    public void ParseTickers_WithMixedSeparators_ReturnsValidTickers()
    {
        // Arrange
        string csvContent = "AAPL,GOOGL\nMSFT;TSLA";

        // Act
        TickerImportResult result = _parser.ParseTickers(csvContent);

        // Assert
        result.ValidTickers.Count.ShouldBe(4);
    }

    [Fact]
    public void ParseTickers_WithWhitespace_TrimsCorrectly()
    {
        // Arrange
        string csvContent = "  AAPL  ,  GOOGL  ,  MSFT  ";

        // Act
        TickerImportResult result = _parser.ParseTickers(csvContent);

        // Assert
        result.ValidTickers.Count.ShouldBe(3);
        result.ValidTickers.ShouldContain("AAPL");
    }

    [Fact]
    public void ParseTickers_WithQuotes_RemovesQuotes()
    {
        // Arrange
        string csvContent = "\"AAPL\",\"GOOGL\",\"MSFT\"";

        // Act
        TickerImportResult result = _parser.ParseTickers(csvContent);

        // Assert
        result.ValidTickers.Count.ShouldBe(3);
        result.ValidTickers.ShouldContain("AAPL");
    }

    [Fact]
    public void ParseTickers_WithLowercase_ConvertsToUppercase()
    {
        // Arrange
        string csvContent = "aapl,googl,msft";

        // Act
        TickerImportResult result = _parser.ParseTickers(csvContent);

        // Assert
        result.ValidTickers.ShouldContain("AAPL");
        result.ValidTickers.ShouldContain("GOOGL");
        result.ValidTickers.ShouldContain("MSFT");
    }

    [Fact]
    public void ParseTickers_WithDuplicates_RemovesDuplicates()
    {
        // Arrange
        string csvContent = "AAPL,GOOGL,AAPL,MSFT,GOOGL";

        // Act
        TickerImportResult result = _parser.ParseTickers(csvContent);

        // Assert
        result.ValidTickers.Count.ShouldBe(3);
        result.ValidTickers.ShouldContain("AAPL");
        result.ValidTickers.ShouldContain("GOOGL");
        result.ValidTickers.ShouldContain("MSFT");
    }

    [Fact]
    public void ParseTickers_WithEmptyString_ReturnsEmptyResult()
    {
        // Arrange
        string csvContent = "";

        // Act
        TickerImportResult result = _parser.ParseTickers(csvContent);

        // Assert
        result.ValidTickers.ShouldBeEmpty();
        result.InvalidTickers.ShouldBeEmpty();
        result.TotalLines.ShouldBe(0);
    }

    [Fact]
    public void ParseTickers_WithNullString_ReturnsEmptyResult()
    {
        // Arrange
        string? csvContent = null;

        // Act
        TickerImportResult result = _parser.ParseTickers(csvContent!);

        // Assert
        result.ValidTickers.ShouldBeEmpty();
    }

    [Fact]
    public void ParseTickers_WithInvalidTickers_ReturnsInvalidList()
    {
        // Arrange
        string csvContent = "AAPL,VERYLONGINVALIDTICKER123,MSFT,!!!,GOOGL";

        // Act
        TickerImportResult result = _parser.ParseTickers(csvContent);

        // Assert
        result.ValidTickers.Count.ShouldBe(3);
        result.InvalidTickers.Count.ShouldBe(2);
        result.InvalidTickers.ShouldContain("VERYLONGINVALIDTICKER123");
        result.InvalidTickers.ShouldContain("!!!");
    }

    [Fact]
    public void ParseTickers_WithDotsAndHyphens_AcceptsValid()
    {
        // Arrange
        string csvContent = "BRK.B,CON3.L,3COI.DE,BRK-B";

        // Act
        TickerImportResult result = _parser.ParseTickers(csvContent);

        // Assert
        result.ValidTickers.Count.ShouldBe(4);
        result.ValidTickers.ShouldContain("BRK.B");
        result.ValidTickers.ShouldContain("CON3.L");
        result.ValidTickers.ShouldContain("3COI.DE");
        result.ValidTickers.ShouldContain("BRK-B");
    }

    [Fact]
    public void ParseTickers_WithEmptyLines_SkipsEmptyLines()
    {
        // Arrange
        string csvContent = "AAPL\n\n\nGOOGL\n\nMSFT";

        // Act
        TickerImportResult result = _parser.ParseTickers(csvContent);

        // Assert
        result.ValidTickers.Count.ShouldBe(3);
    }

    [Fact]
    public async Task ParseTickersFromFileAsync_WithValidFile_ReturnsValidTickers()
    {
        // Arrange
        string filePath = Path.Combine(_testDirectory, "tickers.csv");
        await File.WriteAllTextAsync(filePath, "AAPL,GOOGL,MSFT");

        // Act
        TickerImportResult result = await _parser.ParseTickersFromFileAsync(filePath);

        // Assert
        result.ValidTickers.Count.ShouldBe(3);
    }

    [Fact]
    public async Task ParseTickersFromFileAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        string filePath = Path.Combine(_testDirectory, "nonexistent.csv");

        // Act & Assert
        await Should.ThrowAsync<FileNotFoundException>(async () =>
            await _parser.ParseTickersFromFileAsync(filePath));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ParseTickersFromFileAsync_WithInvalidPath_ThrowsArgumentException(string? invalidPath)
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _parser.ParseTickersFromFileAsync(invalidPath!));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
