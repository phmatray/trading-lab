using Shouldly;
using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.Tests.Common;

public class ResultTests
{
    #region Success Results

    [Fact]
    public void Result_Success_CreatesSuccessResult()
    {
        // Arrange & Act
        var result = Result<int>.Success(42);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Value.ShouldBe(42);
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Result_Success_WithNullValue_AllowsNull()
    {
        // Arrange & Act
        var result = Result<string?>.Success(null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    #endregion

    #region Failure Results

    [Fact]
    public void Result_Failure_CreatesFailureWithSingleError()
    {
        // Arrange
        var error = Error.Validation("Invalid input");

        // Act
        var result = Result<int>.Failure(error);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBe(error);
    }

    [Fact]
    public void Result_Failure_CreatesFailureWithMultipleErrors()
    {
        // Arrange
        var error1 = Error.Validation("Error 1");
        var error2 = Error.Validation("Error 2");

        // Act
        var result = Result<int>.Failure(error1, error2);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(2);
        result.Errors.ShouldContain(error1);
        result.Errors.ShouldContain(error2);
    }

    [Fact]
    public void Result_Failure_AccessingValue_ThrowsInvalidOperationException()
    {
        // Arrange
        var error = Error.Validation("Invalid input");
        var result = Result<int>.Failure(error);

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => result.Value);
        exception.Message.ShouldContain("Cannot access Value on failed result");
    }

    #endregion

    #region Value Access

    [Fact]
    public void Result_Success_ValueProperty_ReturnsValue()
    {
        // Arrange
        Result<string> result = Result<string>.Success("test value");

        // Act
        string value = result.Value;

        // Assert
        value.ShouldBe("test value");
    }

    #endregion
}
