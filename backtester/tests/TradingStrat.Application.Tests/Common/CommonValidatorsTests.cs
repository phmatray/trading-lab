using Shouldly;
using TradingStrat.Application.Common;

namespace TradingStrat.Application.Tests.Common;

/// <summary>
/// Tests for CommonValidators utility class.
/// Verifies that validation helpers correctly validate and reject invalid inputs.
/// </summary>
public class CommonValidatorsTests
{
    #region ValidateDateRange Tests

    [Fact]
    public void ValidateDateRange_WithValidRange_DoesNotThrow()
    {
        // Arrange
        DateTime startDate = DateTime.Today.AddMonths(-1);
        DateTime endDate = DateTime.Today;

        // Act & Assert
        Should.NotThrow(() => CommonValidators.ValidateDateRange(startDate, endDate));
    }

    [Fact]
    public void ValidateDateRange_WithNullDates_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => CommonValidators.ValidateDateRange(null, null));
    }

    [Fact]
    public void ValidateDateRange_WithStartDateInFuture_ThrowsArgumentException()
    {
        // Arrange
        DateTime startDate = DateTime.Today.AddDays(1);
        DateTime endDate = DateTime.Today.AddDays(2);

        // Act & Assert
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            CommonValidators.ValidateDateRange(startDate, endDate));
        ex.Message.ShouldContain("Start date cannot be in the future");
    }

    [Fact]
    public void ValidateDateRange_WithEndDateInFuture_ThrowsArgumentException()
    {
        // Arrange
        DateTime startDate = DateTime.Today.AddMonths(-1);
        DateTime endDate = DateTime.Today.AddDays(1);

        // Act & Assert
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            CommonValidators.ValidateDateRange(startDate, endDate));
        ex.Message.ShouldContain("End date cannot be in the future");
    }

    [Fact]
    public void ValidateDateRange_WithStartDateAfterEndDate_ThrowsArgumentException()
    {
        // Arrange
        DateTime startDate = DateTime.Today.AddMonths(-1);
        DateTime endDate = DateTime.Today.AddMonths(-2);

        // Act & Assert
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            CommonValidators.ValidateDateRange(startDate, endDate));
        ex.Message.ShouldContain("Start date must be before or equal to end date");
    }

    #endregion

    #region NormalizeTicker Tests

    [Fact]
    public void NormalizeTicker_WithValidTicker_ReturnsUppercaseTrimmed()
    {
        // Arrange
        string ticker = "  aapl  ";

        // Act
        string result = CommonValidators.NormalizeTicker(ticker);

        // Assert
        result.ShouldBe("AAPL");
    }

    [Fact]
    public void NormalizeTicker_WithUppercaseTicker_ReturnsUnchanged()
    {
        // Arrange
        string ticker = "MSFT";

        // Act
        string result = CommonValidators.NormalizeTicker(ticker);

        // Assert
        result.ShouldBe("MSFT");
    }

    [Fact]
    public void NormalizeTicker_WithNullTicker_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            CommonValidators.NormalizeTicker(null!));
    }

    [Fact]
    public void NormalizeTicker_WithEmptyTicker_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            CommonValidators.NormalizeTicker(string.Empty));
    }

    [Fact]
    public void NormalizeTicker_WithWhitespaceTicker_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            CommonValidators.NormalizeTicker("   "));
    }

    #endregion

    #region ValidateCommission Tests

    [Fact]
    public void ValidateCommission_WithValidValues_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            CommonValidators.ValidateCommission(0.001m, 1.0m));
    }

    [Fact]
    public void ValidateCommission_WithZeroValues_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            CommonValidators.ValidateCommission(0m, 0m));
    }

    [Fact]
    public void ValidateCommission_WithNegativePercentage_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            CommonValidators.ValidateCommission(-0.001m, 1.0m));
        ex.Message.ShouldContain("Commission percentage cannot be negative");
    }

    [Fact]
    public void ValidateCommission_WithPercentageGreaterThanOne_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            CommonValidators.ValidateCommission(1.0m, 1.0m));
        ex.Message.ShouldContain("Commission percentage must be less than 100%");
    }

    [Fact]
    public void ValidateCommission_WithNegativeMinimumCommission_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            CommonValidators.ValidateCommission(0.001m, -1.0m));
        ex.Message.ShouldContain("Minimum commission cannot be negative");
    }

    #endregion

    #region ValidateCapital Tests

    [Fact]
    public void ValidateCapital_WithPositiveValue_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            CommonValidators.ValidateCapital(10000m));
    }

    [Fact]
    public void ValidateCapital_WithZero_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            CommonValidators.ValidateCapital(0m));
        ex.Message.ShouldContain("Capital must be positive");
    }

    [Fact]
    public void ValidateCapital_WithNegativeValue_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            CommonValidators.ValidateCapital(-1000m));
        ex.Message.ShouldContain("Capital must be positive");
    }

    [Fact]
    public void ValidateCapital_WithCustomParamName_UsesCustomNameInError()
    {
        // Act & Assert
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            CommonValidators.ValidateCapital(0m, "InitialCash"));
        ex.Message.ShouldContain("InitialCash must be positive");
    }

    #endregion

    #region ValidatePercentage Tests

    [Fact]
    public void ValidatePercentage_WithValidValue_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            CommonValidators.ValidatePercentage(50m));
    }

    [Fact]
    public void ValidatePercentage_WithZero_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            CommonValidators.ValidatePercentage(0m));
    }

    [Fact]
    public void ValidatePercentage_WithHundred_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            CommonValidators.ValidatePercentage(100m));
    }

    [Fact]
    public void ValidatePercentage_WithNegativeValue_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            CommonValidators.ValidatePercentage(-1m));
        ex.Message.ShouldContain("Percentage cannot be negative");
    }

    [Fact]
    public void ValidatePercentage_WithValueGreaterThanHundred_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            CommonValidators.ValidatePercentage(101m));
        ex.Message.ShouldContain("Percentage cannot exceed 100");
    }

    #endregion

    #region ValidateRatio Tests

    [Fact]
    public void ValidateRatio_WithValidValue_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            CommonValidators.ValidateRatio(0.5m));
    }

    [Fact]
    public void ValidateRatio_WithZero_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            CommonValidators.ValidateRatio(0m));
    }

    [Fact]
    public void ValidateRatio_WithOne_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            CommonValidators.ValidateRatio(1m));
    }

    [Fact]
    public void ValidateRatio_WithNegativeValue_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            CommonValidators.ValidateRatio(-0.1m));
        ex.Message.ShouldContain("Ratio must be between 0 and 1");
    }

    [Fact]
    public void ValidateRatio_WithValueGreaterThanOne_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            CommonValidators.ValidateRatio(1.1m));
        ex.Message.ShouldContain("Ratio must be between 0 and 1");
    }

    #endregion

    #region ValidatePositive Tests

    [Fact]
    public void ValidatePositive_WithPositiveValue_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            CommonValidators.ValidatePositive(100m));
    }

    [Fact]
    public void ValidatePositive_WithZero_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            CommonValidators.ValidatePositive(0m));
        ex.Message.ShouldContain("Value must be positive");
    }

    [Fact]
    public void ValidatePositive_WithNegativeValue_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            CommonValidators.ValidatePositive(-10m));
        ex.Message.ShouldContain("Value must be positive");
    }

    #endregion

    #region ValidateNonNegative Tests

    [Fact]
    public void ValidateNonNegative_WithPositiveValue_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            CommonValidators.ValidateNonNegative(100m));
    }

    [Fact]
    public void ValidateNonNegative_WithZero_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            CommonValidators.ValidateNonNegative(0m));
    }

    [Fact]
    public void ValidateNonNegative_WithNegativeValue_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            CommonValidators.ValidateNonNegative(-10m));
        ex.Message.ShouldContain("Value cannot be negative");
    }

    #endregion
}
