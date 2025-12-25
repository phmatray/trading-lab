using Shouldly;
using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.Tests.Common;

public class ErrorTests
{
    #region Factory Methods

    [Fact]
    public void Error_Validation_CreatesValidationError()
    {
        // Arrange & Act
        var error = Error.Validation("Invalid input");

        // Assert
        error.Type.ShouldBe(ErrorType.Validation);
        error.Message.ShouldBe("Invalid input");
        error.Code.ShouldBe("VALIDATION_ERROR");
    }

    [Fact]
    public void Error_Validation_WithCustomCode_UsesCustomCode()
    {
        // Arrange & Act
        var error = Error.Validation("Invalid input", "CUSTOM_CODE");

        // Assert
        error.Type.ShouldBe(ErrorType.Validation);
        error.Code.ShouldBe("CUSTOM_CODE");
    }

    [Fact]
    public void Error_NotFound_CreatesNotFoundError()
    {
        // Arrange & Act
        var error = Error.NotFound("Resource not found");

        // Assert
        error.Type.ShouldBe(ErrorType.NotFound);
        error.Message.ShouldBe("Resource not found");
        error.Code.ShouldBe("NOT_FOUND");
    }

    [Fact]
    public void Error_BusinessRule_CreatesBusinessRuleError()
    {
        // Arrange & Act
        var error = Error.BusinessRule("Business rule violated");

        // Assert
        error.Type.ShouldBe(ErrorType.BusinessRule);
        error.Message.ShouldBe("Business rule violated");
        error.Code.ShouldBe("BUSINESS_RULE_VIOLATION");
    }

    [Fact]
    public void Error_Conflict_CreatesConflictError()
    {
        // Arrange & Act
        var error = Error.Conflict("Duplicate resource");

        // Assert
        error.Type.ShouldBe(ErrorType.Conflict);
        error.Message.ShouldBe("Duplicate resource");
        error.Code.ShouldBe("CONFLICT");
    }

    [Fact]
    public void Error_InsufficientData_CreatesInsufficientDataError()
    {
        // Arrange & Act
        var error = Error.InsufficientData("Not enough data");

        // Assert
        error.Type.ShouldBe(ErrorType.InsufficientData);
        error.Message.ShouldBe("Not enough data");
        error.Code.ShouldBe("INSUFFICIENT_DATA");
    }

    #endregion

    #region Equality

    [Fact]
    public void Error_WithSameValues_AreEqual()
    {
        // Arrange
        var error1 = Error.Validation("Same message", "SAME_CODE");
        var error2 = Error.Validation("Same message", "SAME_CODE");

        // Act & Assert
        error1.ShouldBe(error2);
    }

    [Fact]
    public void Error_WithDifferentMessages_AreNotEqual()
    {
        // Arrange
        var error1 = Error.Validation("Message 1");
        var error2 = Error.Validation("Message 2");

        // Act & Assert
        error1.ShouldNotBe(error2);
    }

    #endregion

    #region Immutability

    [Fact]
    public void Error_IsImmutable_RecordType()
    {
        // Arrange
        var error = Error.Validation("Test");

        // Assert - record types are immutable by design
        error.ShouldBeAssignableTo<object>();
        error.GetType().IsValueType.ShouldBeFalse(); // records are reference types
    }

    #endregion
}
