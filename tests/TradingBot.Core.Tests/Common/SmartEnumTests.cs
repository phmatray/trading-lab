// <copyright file="SmartEnumTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Shouldly;
using TradingBot.Core.Common;

namespace TradingBot.Core.Tests.Common;

/// <summary>
/// Unit tests for the SmartEnum base class.
/// </summary>
public sealed class SmartEnumTests
{
    [Fact]
    public void SmartEnum_WhenCreated_ShouldSetNameAndValue()
    {
        // Assert
        TestEnum.First.Name.ShouldBe("First");
        TestEnum.First.Value.ShouldBe(1);
        TestEnum.Second.Name.ShouldBe("Second");
        TestEnum.Second.Value.ShouldBe(2);
    }

    [Fact]
    public void GetAll_ShouldReturnAllEnumValues()
    {
        // Act
        var all = TestEnum.GetAll().ToList();

        // Assert
        all.Count.ShouldBe(3);
        all.ShouldContain(TestEnum.First);
        all.ShouldContain(TestEnum.Second);
        all.ShouldContain(TestEnum.Third);
    }

    [Fact]
    public void FromValue_WithValidValue_ShouldReturnCorrectEnum()
    {
        // Act
        var result = TestEnum.FromValue(2);

        // Assert
        result.ShouldBe(TestEnum.Second);
        result.Name.ShouldBe("Second");
    }

    [Fact]
    public void FromValue_WithInvalidValue_ShouldThrowException()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => TestEnum.FromValue(999))
            .Message.ShouldContain("No TestEnum with value 999 found");
    }

    [Fact]
    public void TryFromValue_WithValidValue_ShouldReturnTrueAndEnum()
    {
        // Act
        var success = TestEnum.TryFromValue(3, out var result);

        // Assert
        success.ShouldBeTrue();
        result.ShouldBe(TestEnum.Third);
    }

    [Fact]
    public void TryFromValue_WithInvalidValue_ShouldReturnFalse()
    {
        // Act
        var success = TestEnum.TryFromValue(999, out var result);

        // Assert
        success.ShouldBeFalse();
        result.ShouldBeNull();
    }

    [Fact]
    public void FromName_WithValidName_ShouldReturnCorrectEnum()
    {
        // Act
        var result = TestEnum.FromName("First");

        // Assert
        result.ShouldBe(TestEnum.First);
        result.Value.ShouldBe(1);
    }

    [Fact]
    public void FromName_IsCaseInsensitive()
    {
        // Act
        var result1 = TestEnum.FromName("SECOND");
        var result2 = TestEnum.FromName("second");
        var result3 = TestEnum.FromName("Second");

        // Assert
        result1.ShouldBe(TestEnum.Second);
        result2.ShouldBe(TestEnum.Second);
        result3.ShouldBe(TestEnum.Second);
    }

    [Fact]
    public void FromName_WithInvalidName_ShouldThrowException()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => TestEnum.FromName("Invalid"))
            .Message.ShouldContain("No TestEnum with name 'Invalid' found");
    }

    [Fact]
    public void TryFromName_WithValidName_ShouldReturnTrueAndEnum()
    {
        // Act
        var success = TestEnum.TryFromName("Third", out var result);

        // Assert
        success.ShouldBeTrue();
        result.ShouldBe(TestEnum.Third);
    }

    [Fact]
    public void TryFromName_WithInvalidName_ShouldReturnFalse()
    {
        // Act
        var success = TestEnum.TryFromName("Invalid", out var result);

        // Assert
        success.ShouldBeFalse();
        result.ShouldBeNull();
    }

    [Fact]
    public void ToString_ShouldReturnName()
    {
        // Act
        var result = TestEnum.Second.ToString();

        // Assert
        result.ShouldBe("Second");
    }

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var enum1 = TestEnum.First;
        var enum2 = TestEnum.FromValue(1);

        // Act & Assert
        enum1.Equals(enum2).ShouldBeTrue();
        enum1.Equals((object)enum2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Act & Assert
        TestEnum.First.Equals(TestEnum.Second).ShouldBeFalse();
        TestEnum.First.Equals((object)TestEnum.Second).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Act & Assert
        TestEnum.First.Equals(null).ShouldBeFalse();
        TestEnum.First.Equals((object?)null).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithSameReference_ShouldReturnTrue()
    {
        // Arrange
        var enum1 = TestEnum.First;

        // Act & Assert
        enum1.Equals(enum1).ShouldBeTrue();
    }

    [Fact]
    public void GetHashCode_ForEqualValues_ShouldBeSame()
    {
        // Arrange
        var enum1 = TestEnum.First;
        var enum2 = TestEnum.FromValue(1);

        // Act & Assert
        enum1.GetHashCode().ShouldBe(enum2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ForDifferentValues_ShouldBeDifferent()
    {
        // Act & Assert
        TestEnum.First.GetHashCode().ShouldNotBe(TestEnum.Second.GetHashCode());
    }

    [Fact]
    public void OperatorEquals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var enum1 = TestEnum.First;
        var enum2 = TestEnum.FromValue(1);

        // Act & Assert
        (enum1 == enum2).ShouldBeTrue();
    }

    [Fact]
    public void OperatorEquals_WithDifferentValues_ShouldReturnFalse()
    {
        // Act & Assert
        (TestEnum.First == TestEnum.Second).ShouldBeFalse();
    }

    [Fact]
    public void OperatorEquals_WithBothNull_ShouldReturnTrue()
    {
        // Arrange
        TestEnum? enum1 = null;
        TestEnum? enum2 = null;

        // Act & Assert
        (enum1 == enum2).ShouldBeTrue();
    }

    [Fact]
    public void OperatorEquals_WithOneNull_ShouldReturnFalse()
    {
        // Arrange
        TestEnum? enum1 = TestEnum.First;
        TestEnum? enum2 = null;

        // Act & Assert
        (enum1 == enum2).ShouldBeFalse();
        (enum2 == enum1).ShouldBeFalse();
    }

    [Fact]
    public void OperatorNotEquals_WithDifferentValues_ShouldReturnTrue()
    {
        // Act & Assert
        (TestEnum.First != TestEnum.Second).ShouldBeTrue();
    }

    [Fact]
    public void OperatorNotEquals_WithSameValues_ShouldReturnFalse()
    {
        // Arrange
        var enum1 = TestEnum.First;
        var enum2 = TestEnum.FromValue(1);

        // Act & Assert
        (enum1 != enum2).ShouldBeFalse();
    }

    [Fact]
    public void CompareTo_WithSmallerValue_ShouldReturnPositive()
    {
        // Act
        var result = TestEnum.Second.CompareTo(TestEnum.First);

        // Assert
        result.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_WithLargerValue_ShouldReturnNegative()
    {
        // Act
        var result = TestEnum.First.CompareTo(TestEnum.Third);

        // Assert
        result.ShouldBeLessThan(0);
    }

    [Fact]
    public void CompareTo_WithEqualValue_ShouldReturnZero()
    {
        // Arrange
        var enum1 = TestEnum.Second;
        var enum2 = TestEnum.FromValue(2);

        // Act
        var result = enum1.CompareTo(enum2);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public void CompareTo_WithNull_ShouldReturnPositive()
    {
        // Act
        var result = TestEnum.First.CompareTo(null);

        // Assert
        result.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void OperatorLessThan_ShouldWorkCorrectly()
    {
        // Arrange
        var first = TestEnum.First;
        var second = TestEnum.Second;

        // Act & Assert
        (first < second).ShouldBeTrue();
        (second < first).ShouldBeFalse();
        (first < TestEnum.FromValue(1)).ShouldBeFalse();
    }

    [Fact]
    public void OperatorLessThanOrEqual_ShouldWorkCorrectly()
    {
        // Arrange
        var first = TestEnum.First;
        var second = TestEnum.Second;

        // Act & Assert
        (first <= second).ShouldBeTrue();
        (first <= TestEnum.FromValue(1)).ShouldBeTrue();
        (second <= first).ShouldBeFalse();
    }

    [Fact]
    public void OperatorGreaterThan_ShouldWorkCorrectly()
    {
        // Arrange
        var second = TestEnum.Second;
        var third = TestEnum.Third;

        // Act & Assert
        (third > second).ShouldBeTrue();
        (second > third).ShouldBeFalse();
        (second > TestEnum.FromValue(2)).ShouldBeFalse();
    }

    [Fact]
    public void OperatorGreaterThanOrEqual_ShouldWorkCorrectly()
    {
        // Arrange
        var second = TestEnum.Second;
        var third = TestEnum.Third;

        // Act & Assert
        (third >= second).ShouldBeTrue();
        (second >= TestEnum.FromValue(2)).ShouldBeTrue();
        (second >= third).ShouldBeFalse();
    }

    [Fact]
    public void ImplicitConversion_ShouldConvertToValue()
    {
        // Act
        int value = TestEnum.Second;

        // Assert
        value.ShouldBe(2);
    }

    [Fact]
    public void SmartEnum_WithStringValue_ShouldWorkCorrectly()
    {
        // Act & Assert
        StringEnum.Alpha.Name.ShouldBe("Alpha");
        StringEnum.Alpha.Value.ShouldBe("A");
        StringEnum.FromValue("B").ShouldBe(StringEnum.Beta);
        StringEnum.FromName("Gamma").Value.ShouldBe("C");
    }

    private sealed class TestEnum : SmartEnum<TestEnum, int>
    {
        public static readonly TestEnum First = new(nameof(First), 1);
        public static readonly TestEnum Second = new(nameof(Second), 2);
        public static readonly TestEnum Third = new(nameof(Third), 3);

        private TestEnum(string name, int value)
            : base(name, value)
        {
        }
    }

    private sealed class StringEnum : SmartEnum<StringEnum, string>
    {
        public static readonly StringEnum Alpha = new(nameof(Alpha), "A");
        public static readonly StringEnum Beta = new(nameof(Beta), "B");
        public static readonly StringEnum Gamma = new(nameof(Gamma), "C");

        private StringEnum(string name, string value)
            : base(name, value)
        {
        }
    }
}
