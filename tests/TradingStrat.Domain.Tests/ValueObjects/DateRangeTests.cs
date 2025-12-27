using Shouldly;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.ValueObjects;

public class DateRangeTests
{
    [Fact]
    public void Constructor_WithValidDates_CreatesInstance()
    {
        // Arrange
        DateTime start = new DateTime(2024, 1, 1);
        DateTime end = new DateTime(2024, 12, 31);

        // Act
        DateRange range = new DateRange(start, end);

        // Assert
        range.Start.ShouldBe(start);
        range.End.ShouldBe(end);
    }

    [Fact]
    public void Constructor_WithStartAfterEnd_ThrowsArgumentException()
    {
        // Arrange
        DateTime start = new DateTime(2024, 12, 31);
        DateTime end = new DateTime(2024, 1, 1);

        // Act & Assert
        Should.Throw<ArgumentException>(() => new DateRange(start, end));
    }

    [Fact]
    public void Constructor_WithSameDates_CreatesValidRange()
    {
        // Arrange
        DateTime date = new DateTime(2024, 6, 15);

        // Act
        DateRange range = new DateRange(date, date);

        // Assert
        range.Start.ShouldBe(date);
        range.End.ShouldBe(date);
    }

    [Fact]
    public void Constructor_WithDateTime_NormalizesToDateOnly()
    {
        // Arrange
        DateTime start = new DateTime(2024, 1, 1, 14, 30, 0);
        DateTime end = new DateTime(2024, 12, 31, 23, 59, 59);

        // Act
        DateRange range = new DateRange(start, end);

        // Assert
        range.Start.ShouldBe(new DateTime(2024, 1, 1));
        range.End.ShouldBe(new DateTime(2024, 12, 31));
    }

    [Fact]
    public void LastDays_CreatesRangeFromToday()
    {
        // Act
        DateRange range = DateRange.LastDays(30);

        // Assert
        range.End.ShouldBe(DateTime.Today);
        range.Start.ShouldBe(DateTime.Today.AddDays(-30));
    }

    [Fact]
    public void LastDays_WithZeroOrNegative_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => DateRange.LastDays(0));
        Should.Throw<ArgumentException>(() => DateRange.LastDays(-10));
    }

    [Fact]
    public void LastYears_CreatesRangeFromToday()
    {
        // Act
        DateRange range = DateRange.LastYears(2);

        // Assert
        range.End.ShouldBe(DateTime.Today);
        range.Start.ShouldBe(DateTime.Today.AddYears(-2));
    }

    [Fact]
    public void LastYears_WithZeroOrNegative_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => DateRange.LastYears(0));
        Should.Throw<ArgumentException>(() => DateRange.LastYears(-1));
    }

    [Fact]
    public void YearToDate_CreatesRangeFromStartOfYear()
    {
        // Act
        DateRange range = DateRange.YearToDate();

        // Assert
        DateTime today = DateTime.Today;
        range.Start.ShouldBe(new DateTime(today.Year, 1, 1));
        range.End.ShouldBe(today);
    }

    [Fact]
    public void LastMonth_CreatesRangeForPreviousMonth()
    {
        // Act
        DateRange range = DateRange.LastMonth();

        // Assert
        DateTime today = DateTime.Today;
        DateTime lastMonthStart = today.AddMonths(-1);
        DateTime expectedStart = new DateTime(lastMonthStart.Year, lastMonthStart.Month, 1);
        DateTime expectedEnd = expectedStart.AddMonths(1).AddDays(-1);

        range.Start.ShouldBe(expectedStart);
        range.End.ShouldBe(expectedEnd);
    }

    [Fact]
    public void TotalDays_CalculatesCorrectly()
    {
        // Arrange
        DateRange range = new DateRange(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 10)
        );

        // Act & Assert
        range.TotalDays.ShouldBe(10); // Inclusive: 1-10 = 10 days
    }

    [Fact]
    public void TotalWeeks_CalculatesCorrectly()
    {
        // Arrange
        DateRange range = new DateRange(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 21)
        );

        // Act & Assert
        range.TotalWeeks.ShouldBe(3); // 21 days / 7 = 3 weeks
    }

    [Fact]
    public void TotalMonths_CalculatesCorrectly()
    {
        // Arrange
        DateRange range = new DateRange(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 6, 30)
        );

        // Act & Assert
        range.TotalMonths.ShouldBe(5); // Jan to Jun = 5 months (0-based)
    }

    [Fact]
    public void Contains_WithDateInRange_ReturnsTrue()
    {
        // Arrange
        DateRange range = new DateRange(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31)
        );
        DateTime dateInRange = new DateTime(2024, 6, 15);

        // Act & Assert
        range.Contains(dateInRange).ShouldBeTrue();
    }

    [Fact]
    public void Contains_WithDateOutsideRange_ReturnsFalse()
    {
        // Arrange
        DateRange range = new DateRange(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31)
        );
        DateTime dateOutside = new DateTime(2025, 1, 1);

        // Act & Assert
        range.Contains(dateOutside).ShouldBeFalse();
    }

    [Fact]
    public void Contains_WithStartDate_ReturnsTrue()
    {
        // Arrange
        DateRange range = new DateRange(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31)
        );

        // Act & Assert
        range.Contains(new DateTime(2024, 1, 1)).ShouldBeTrue();
    }

    [Fact]
    public void Contains_WithEndDate_ReturnsTrue()
    {
        // Arrange
        DateRange range = new DateRange(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31)
        );

        // Act & Assert
        range.Contains(new DateTime(2024, 12, 31)).ShouldBeTrue();
    }

    [Fact]
    public void Overlaps_WithOverlappingRange_ReturnsTrue()
    {
        // Arrange
        DateRange range1 = new DateRange(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 6, 30)
        );
        DateRange range2 = new DateRange(
            new DateTime(2024, 3, 1),
            new DateTime(2024, 9, 30)
        );

        // Act & Assert
        range1.Overlaps(range2).ShouldBeTrue();
        range2.Overlaps(range1).ShouldBeTrue();
    }

    [Fact]
    public void Overlaps_WithNonOverlappingRange_ReturnsFalse()
    {
        // Arrange
        DateRange range1 = new DateRange(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 3, 31)
        );
        DateRange range2 = new DateRange(
            new DateTime(2024, 4, 1),
            new DateTime(2024, 6, 30)
        );

        // Act & Assert
        range1.Overlaps(range2).ShouldBeFalse();
        range2.Overlaps(range1).ShouldBeFalse();
    }

    [Fact]
    public void IsInFuture_WithFutureRange_ReturnsTrue()
    {
        // Arrange
        DateRange range = new DateRange(
            DateTime.Today.AddDays(1),
            DateTime.Today.AddDays(30)
        );

        // Act & Assert
        range.IsInFuture().ShouldBeTrue();
    }

    [Fact]
    public void IsInPast_WithPastRange_ReturnsTrue()
    {
        // Arrange
        DateRange range = new DateRange(
            DateTime.Today.AddDays(-30),
            DateTime.Today.AddDays(-1)
        );

        // Act & Assert
        range.IsInPast().ShouldBeTrue();
    }

    [Fact]
    public void IsCurrent_WithCurrentRange_ReturnsTrue()
    {
        // Arrange
        DateRange range = new DateRange(
            DateTime.Today.AddDays(-10),
            DateTime.Today.AddDays(10)
        );

        // Act & Assert
        range.IsCurrent().ShouldBeTrue();
    }

    [Fact]
    public void SplitByMonths_SplitsRangeCorrectly()
    {
        // Arrange
        DateRange range = new DateRange(
            new DateTime(2024, 1, 15),
            new DateTime(2024, 3, 10)
        );

        // Act
        List<DateRange> months = range.SplitByMonths().ToList();

        // Assert
        months.Count.ShouldBe(3);

        // January partial month
        months[0].Start.ShouldBe(new DateTime(2024, 1, 15));
        months[0].End.ShouldBe(new DateTime(2024, 1, 31));

        // February full month
        months[1].Start.ShouldBe(new DateTime(2024, 2, 1));
        months[1].End.ShouldBe(new DateTime(2024, 2, 29)); // 2024 is leap year

        // March partial month
        months[2].Start.ShouldBe(new DateTime(2024, 3, 1));
        months[2].End.ShouldBe(new DateTime(2024, 3, 10));
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        // Arrange
        DateRange range = new DateRange(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31)
        );

        // Act
        string result = range.ToString();

        // Assert
        result.ShouldBe("2024-01-01 to 2024-12-31");
    }

    [Fact]
    public void ToString_WithFormat_UsesCustomFormat()
    {
        // Arrange
        DateRange range = new DateRange(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31)
        );

        // Act
        string result = range.ToString("MM/dd/yyyy");

        // Assert
        result.ShouldBe("01/01/2024 to 12/31/2024");
    }
}
