using Bunit;
using Shouldly;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the DataSummaryCard component.
/// </summary>
public class DataSummaryCardTests : BunitTestContext
{
    [Fact]
    public void DataSummaryCard_WithNullSummary_RendersNothing()
    {
        // Arrange & Act
        var cut = Render<DataSummaryCard>(parameters => parameters
            .Add(p => p.Summary, null));

        // Assert
        cut.Markup.ShouldBeEmpty();
    }

    [Fact]
    public void DataSummaryCard_WithSummary_DisplaysHeader()
    {
        // Arrange
        var summary = new DataSummaryResult("AAPL", "US0378331005", 100, 10, null, null, null, null, null);

        // Act
        var cut = Render<DataSummaryCard>(parameters => parameters
            .Add(p => p.Summary, summary));

        // Assert
        var header = cut.Find("h2");
        header.TextContent.ShouldBe("Data Summary");
    }

    [Fact]
    public void DataSummaryCard_WithSummary_DisplaysTickerAndRecords()
    {
        // Arrange
        var summary = new DataSummaryResult("MSFT", null, 250, 25, null, null, null, null, null);

        // Act
        var cut = Render<DataSummaryCard>(parameters => parameters
            .Add(p => p.Summary, summary));

        // Assert
        cut.Markup.ShouldContain("MSFT");
        cut.Markup.ShouldContain("250");
        cut.Markup.ShouldContain("25");
    }

    [Fact]
    public void DataSummaryCard_WithISIN_DisplaysISIN()
    {
        // Arrange
        var summary = new DataSummaryResult("GOOGL", "US02079K3059", 100, 0, null, null, null, null, null);

        // Act
        var cut = Render<DataSummaryCard>(parameters => parameters
            .Add(p => p.Summary, summary));

        // Assert
        cut.Markup.ShouldContain("ISIN");
        cut.Markup.ShouldContain("US02079K3059");
    }

    [Fact]
    public void DataSummaryCard_WithoutISIN_DoesNotDisplayISINSection()
    {
        // Arrange
        var summary = new DataSummaryResult("TSLA", null, 100, 0, null, null, null, null, null);

        // Act
        var cut = Render<DataSummaryCard>(parameters => parameters
            .Add(p => p.Summary, summary));

        // Assert
        var dtElements = cut.FindAll("dt");
        var isinLabel = dtElements.FirstOrDefault(dt => dt.TextContent == "ISIN");
        isinLabel.ShouldBeNull();
    }

    [Fact]
    public void DataSummaryCard_WithDateRange_DisplaysOldestAndLatestDates()
    {
        // Arrange
        var oldestDate = new DateTime(2023, 1, 1);
        var latestDate = new DateTime(2023, 12, 31);
        var summary = new DataSummaryResult("AAPL", null, 100, 0, oldestDate, latestDate, null, null, null);

        // Act
        var cut = Render<DataSummaryCard>(parameters => parameters
            .Add(p => p.Summary, summary));

        // Assert
        cut.Markup.ShouldContain("Oldest Date");
        cut.Markup.ShouldContain("Latest Date");
        cut.Markup.ShouldContain("2023-01-01");
        cut.Markup.ShouldContain("2023-12-31");
    }

    [Fact]
    public void DataSummaryCard_WithPriceStatistics_DisplaysMinMaxAndLatestClose()
    {
        // Arrange
        var summary = new DataSummaryResult(
            "AAPL",
            null,
            100,
            0,
            new DateTime(2023, 1, 1),
            new DateTime(2023, 12, 31),
            MinPrice: 120.50m,
            MaxPrice: 185.75m,
            LatestClose: 175.25m
        );

        // Act
        var cut = Render<DataSummaryCard>(parameters => parameters
            .Add(p => p.Summary, summary));

        // Assert
        cut.Markup.ShouldContain("Min Price");
        cut.Markup.ShouldContain("Max Price");
        cut.Markup.ShouldContain("Latest Close");

        // Prices should be displayed (format may vary by culture)
        string markup = cut.Markup;
        markup.ShouldContain("120"); // At least the whole number part
        markup.ShouldContain("185");
        markup.ShouldContain("175");
    }

    [Fact]
    public void DataSummaryCard_WithCompleteData_DisplaysAllSections()
    {
        // Arrange
        var summary = new DataSummaryResult(
            Ticker: "AAPL",
            ISIN: "US0378331005",
            TotalRecords: 500,
            NewRecords: 50,
            OldestDate: new DateTime(2022, 1, 1),
            LatestDate: new DateTime(2023, 12, 31),
            MinPrice: 120.00m,
            MaxPrice: 200.00m,
            LatestClose: 180.00m
        );

        // Act
        var cut = Render<DataSummaryCard>(parameters => parameters
            .Add(p => p.Summary, summary));

        // Assert
        // Verify all major sections are present
        var definitionTerms = cut.FindAll("dt");
        definitionTerms.Count.ShouldBeGreaterThan(5);

        // Verify ticker section
        cut.Markup.ShouldContain("Ticker");
        cut.Markup.ShouldContain("AAPL");

        // Verify ISIN section
        cut.Markup.ShouldContain("ISIN");
        cut.Markup.ShouldContain("US0378331005");

        // Verify records section
        cut.Markup.ShouldContain("Total Records");
        cut.Markup.ShouldContain("New Records");

        // Verify date range section
        cut.Markup.ShouldContain("Oldest Date");
        cut.Markup.ShouldContain("Latest Date");

        // Verify price statistics section
        cut.Markup.ShouldContain("Min Price");
        cut.Markup.ShouldContain("Max Price");
        cut.Markup.ShouldContain("Latest Close");
    }

    [Fact]
    public void DataSummaryCard_FormatsNumbersCorrectly()
    {
        // Arrange
        var summary = new DataSummaryResult("AAPL", null, 1000, 100, null, null, null, null, null);

        // Act
        var cut = Render<DataSummaryCard>(parameters => parameters
            .Add(p => p.Summary, summary));

        // Assert
        // Total records should be displayed with thousand separator (N0 format)
        // Accept both comma (1,000) and period (1.000) as thousand separators depending on culture
        bool hasCommaSeparator = cut.Markup.Contains("1,000");
        bool hasPeriodSeparator = cut.Markup.Contains("1.000");
        (hasCommaSeparator || hasPeriodSeparator).ShouldBeTrue("Expected to find 1000 formatted with thousand separator");
    }
}
