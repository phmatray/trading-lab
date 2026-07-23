using Shouldly;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Infrastructure.Export;

namespace TradingStrat.Infrastructure.Tests.Export;

public class HistoricalDataExportAdapterTests : IDisposable
{
    private readonly HistoricalDataExportAdapter _adapter;
    private readonly string _testDirectory;

    public HistoricalDataExportAdapterTests()
    {
        _adapter = new HistoricalDataExportAdapter();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"TradingStratTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithValidData_CreatesFile()
    {
        // Arrange
        string outputPath = Path.Combine(_testDirectory, "historical_data.csv");
        List<HistoricalPrice> data = CreateTestHistoricalData();

        // Act
        ExportResult result = await _adapter.ExportToCsvAsync(data, "AAPL", outputPath);

        // Assert
        File.Exists(outputPath).ShouldBeTrue();
        result.FilePath.ShouldBe(outputPath);
        result.RecordCount.ShouldBe(3);
        result.FileSizeBytes.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ExportToCsvAsync_ContainsCorrectHeaders()
    {
        // Arrange
        string outputPath = Path.Combine(_testDirectory, "data_with_headers.csv");
        List<HistoricalPrice> data = CreateTestHistoricalData();

        // Act
        await _adapter.ExportToCsvAsync(data, "AAPL", outputPath);

        // Assert
        string[] lines = await File.ReadAllLinesAsync(outputPath);
        lines[0].ShouldContain("Ticker");
        lines[0].ShouldContain("ISIN");
        lines[0].ShouldContain("DateTime");
        lines[0].ShouldContain("Open");
        lines[0].ShouldContain("High");
        lines[0].ShouldContain("Low");
        lines[0].ShouldContain("Close");
        lines[0].ShouldContain("Volume");
    }

    [Fact]
    public async Task ExportToCsvAsync_SortsDataByDate()
    {
        // Arrange
        string outputPath = Path.Combine(_testDirectory, "sorted_data.csv");
        List<HistoricalPrice> data = CreateTestHistoricalData();

        // Act
        await _adapter.ExportToCsvAsync(data, "AAPL", outputPath);

        // Assert
        string[] lines = await File.ReadAllLinesAsync(outputPath);
        lines[1].ShouldContain(DateTime.Today.AddDays(-2).ToString("yyyy-MM-dd"));
        lines[2].ShouldContain(DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd"));
        lines[3].ShouldContain(DateTime.Today.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public async Task ExportToJsonAsync_WithValidData_CreatesFile()
    {
        // Arrange
        string outputPath = Path.Combine(_testDirectory, "historical_data.json");
        List<HistoricalPrice> data = CreateTestHistoricalData();

        // Act
        ExportResult result = await _adapter.ExportToJsonAsync(data, "AAPL", outputPath);

        // Assert
        File.Exists(outputPath).ShouldBeTrue();
        result.FilePath.ShouldBe(outputPath);
        result.RecordCount.ShouldBe(3);
        result.FileSizeBytes.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ExportToJsonAsync_ContainsMetadata()
    {
        // Arrange
        string outputPath = Path.Combine(_testDirectory, "data_with_metadata.json");
        List<HistoricalPrice> data = CreateTestHistoricalData();

        // Act
        await _adapter.ExportToJsonAsync(data, "AAPL", outputPath);

        // Assert
        string json = await File.ReadAllTextAsync(outputPath);
        json.ShouldContain("\"ticker\"");
        json.ShouldContain("\"recordCount\"");
        json.ShouldContain("\"oldestDate\"");
        json.ShouldContain("\"latestDate\"");
        json.ShouldContain("\"exportedAt\"");
        json.ShouldContain("\"data\"");
    }

    [Fact]
    public async Task ExportToJsonAsync_UsesCamelCase()
    {
        // Arrange
        string outputPath = Path.Combine(_testDirectory, "camelcase.json");
        List<HistoricalPrice> data = CreateTestHistoricalData();

        // Act
        await _adapter.ExportToJsonAsync(data, "AAPL", outputPath);

        // Assert
        string json = await File.ReadAllTextAsync(outputPath);
        json.ShouldContain("\"ticker\":");
        json.ShouldNotContain("\"Ticker\":", Case.Sensitive);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithNullData_ThrowsArgumentNullException()
    {
        // Arrange
        string outputPath = Path.Combine(_testDirectory, "data.csv");

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _adapter.ExportToCsvAsync(null!, "AAPL", outputPath));
    }

    [Fact]
    public async Task ExportToJsonAsync_WithNullData_ThrowsArgumentNullException()
    {
        // Arrange
        string outputPath = Path.Combine(_testDirectory, "data.json");

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _adapter.ExportToJsonAsync(null!, "AAPL", outputPath));
    }

    private static List<HistoricalPrice> CreateTestHistoricalData()
    {
        return new List<HistoricalPrice>
        {
            new()
            {
                Ticker = "AAPL",
                ISIN = "US0378331005",
                DateTime = DateTime.Today.AddDays(-2),
                Open = 150.0m,
                High = 155.0m,
                Low = 149.0m,
                Close = 154.0m,
                Volume = 1000000
            },
            new()
            {
                Ticker = "AAPL",
                ISIN = "US0378331005",
                DateTime = DateTime.Today.AddDays(-1),
                Open = 154.0m,
                High = 158.0m,
                Low = 153.0m,
                Close = 157.0m,
                Volume = 1100000
            },
            new()
            {
                Ticker = "AAPL",
                ISIN = "US0378331005",
                DateTime = DateTime.Today,
                Open = 157.0m,
                High = 160.0m,
                Low = 156.0m,
                Close = 159.0m,
                Volume = 1200000
            }
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
