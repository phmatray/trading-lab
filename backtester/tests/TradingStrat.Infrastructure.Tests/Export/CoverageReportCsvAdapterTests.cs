using Shouldly;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Infrastructure.Export;

namespace TradingStrat.Infrastructure.Tests.Export;

public class CoverageReportCsvAdapterTests : IDisposable
{
    private readonly CoverageReportCsvAdapter _adapter;
    private readonly string _testDirectory;

    public CoverageReportCsvAdapterTests()
    {
        _adapter = new CoverageReportCsvAdapter();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"TradingStratTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithValidData_CreatesFile()
    {
        // Arrange
        string outputPath = Path.Combine(_testDirectory, "coverage_report.csv");
        List<TickerCoverageData> data = CreateTestCoverageData();

        // Act
        ExportResult result = await _adapter.ExportToCsvAsync(data, outputPath);

        // Assert
        File.Exists(outputPath).ShouldBeTrue();
        result.FilePath.ShouldBe(outputPath);
        result.RecordCount.ShouldBe(3);
        result.FileSizeBytes.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithValidData_ContainsCorrectHeaders()
    {
        // Arrange
        string outputPath = Path.Combine(_testDirectory, "coverage_with_headers.csv");
        List<TickerCoverageData> data = CreateTestCoverageData();

        // Act
        await _adapter.ExportToCsvAsync(data, outputPath);

        // Assert
        string[] lines = await File.ReadAllLinesAsync(outputPath);
        lines[0].ShouldContain("Ticker");
        lines[0].ShouldContain("ISIN");
        lines[0].ShouldContain("TimeFrame");
        lines[0].ShouldContain("RecordCount");
        lines[0].ShouldContain("OldestDate");
        lines[0].ShouldContain("LatestDate");
        lines[0].ShouldContain("CoveragePercentage");
        lines[0].ShouldContain("GapCount");
        lines[0].ShouldContain("Status");
    }

    [Fact]
    public async Task ExportToCsvAsync_WithValidData_ContainsCorrectData()
    {
        // Arrange
        string outputPath = Path.Combine(_testDirectory, "coverage_with_data.csv");
        List<TickerCoverageData> data = CreateTestCoverageData();

        // Act
        await _adapter.ExportToCsvAsync(data, outputPath);

        // Assert
        string[] lines = await File.ReadAllLinesAsync(outputPath);
        lines.Length.ShouldBe(4); // 1 header + 3 data rows
        lines[1].ShouldContain("AAPL");
        lines[1].ShouldContain("95.50");
        lines[1].ShouldContain("Complete");
        lines[2].ShouldContain("GOOGL");
        lines[2].ShouldContain("85.00");
        lines[2].ShouldContain("Partial");
        lines[3].ShouldContain("MSFT");
        lines[3].ShouldContain("70.00");
        lines[3].ShouldContain("Gaps");
    }

    [Fact]
    public async Task ExportToCsvAsync_WithEmptyData_CreatesEmptyFile()
    {
        // Arrange
        string outputPath = Path.Combine(_testDirectory, "empty_coverage.csv");
        List<TickerCoverageData> data = new();

        // Act
        ExportResult result = await _adapter.ExportToCsvAsync(data, outputPath);

        // Assert
        File.Exists(outputPath).ShouldBeTrue();
        result.RecordCount.ShouldBe(0);
        string[] lines = await File.ReadAllLinesAsync(outputPath);
        lines.Length.ShouldBe(1); // Only header
    }

    [Fact]
    public async Task ExportToCsvAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        string nestedPath = Path.Combine(_testDirectory, "nested", "folder", "coverage.csv");
        List<TickerCoverageData> data = CreateTestCoverageData();

        // Act
        await _adapter.ExportToCsvAsync(data, nestedPath);

        // Assert
        File.Exists(nestedPath).ShouldBeTrue();
    }

    [Fact]
    public async Task ExportToCsvAsync_WithNullData_ThrowsArgumentNullException()
    {
        // Arrange
        string outputPath = Path.Combine(_testDirectory, "coverage.csv");

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _adapter.ExportToCsvAsync(null!, outputPath));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExportToCsvAsync_WithInvalidPath_ThrowsArgumentException(string? invalidPath)
    {
        // Arrange
        List<TickerCoverageData> data = CreateTestCoverageData();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _adapter.ExportToCsvAsync(data, invalidPath!));
    }

    [Fact]
    public async Task ExportToCsvAsync_WithNullISIN_HandlesCorrectly()
    {
        // Arrange
        string outputPath = Path.Combine(_testDirectory, "coverage_null_isin.csv");
        List<TickerCoverageData> data = new()
        {
            new TickerCoverageData(
                "AAPL",
                null,
                new TimeFrame { Unit = TimeFrameUnit.D1 },
                100,
                DateTime.Today.AddDays(-100),
                DateTime.Today,
                95.5m,
                0,
                "Complete"
            )
        };

        // Act
        await _adapter.ExportToCsvAsync(data, outputPath);

        // Assert
        string[] lines = await File.ReadAllLinesAsync(outputPath);
        lines[1].ShouldContain("AAPL,,D1"); // Empty ISIN field
    }

    private static List<TickerCoverageData> CreateTestCoverageData()
    {
        return new List<TickerCoverageData>
        {
            new(
                "AAPL",
                "US0378331005",
                new TimeFrame { Unit = TimeFrameUnit.D1 },
                100,
                DateTime.Today.AddDays(-100),
                DateTime.Today,
                95.5m,
                0,
                "Complete"
            ),
            new(
                "GOOGL",
                "US02079K3059",
                new TimeFrame { Unit = TimeFrameUnit.D1 },
                80,
                DateTime.Today.AddDays(-100),
                DateTime.Today,
                85.0m,
                2,
                "Partial"
            ),
            new(
                "MSFT",
                "US5949181045",
                new TimeFrame { Unit = TimeFrameUnit.H1 },
                50,
                DateTime.Today.AddDays(-50),
                DateTime.Today,
                70.0m,
                5,
                "Gaps"
            )
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
