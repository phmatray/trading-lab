using Shouldly;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.ValueObjects;

public class UseCaseProgressTests
{
    [Fact]
    public void Simple_ShouldCreateProgressWithMessageOnly()
    {
        // Act
        var progress = UseCaseProgress.Simple("Processing data");

        // Assert
        progress.Message.ShouldBe("Processing data");
        progress.CurrentStep.ShouldBeNull();
        progress.TotalSteps.ShouldBeNull();
        progress.PercentComplete.ShouldBeNull();
    }

    [Fact]
    public void WithSteps_ShouldCalculatePercentageCorrectly()
    {
        // Act
        var progress = UseCaseProgress.WithSteps("Processing batch", 5, 10);

        // Assert
        progress.Message.ShouldBe("Processing batch");
        progress.CurrentStep.ShouldBe(5);
        progress.TotalSteps.ShouldBe(10);
        progress.PercentComplete.ShouldBe(50);
    }

    [Fact]
    public void WithSteps_WithPartialPercentage_ShouldRoundCorrectly()
    {
        // Act
        var progress1 = UseCaseProgress.WithSteps("Processing", 1, 3);
        var progress2 = UseCaseProgress.WithSteps("Processing", 2, 3);

        // Assert
        progress1.PercentComplete.ShouldBe(33); // 33.33 rounded to 33
        progress2.PercentComplete.ShouldBe(67); // 66.67 rounded to 67
    }

    [Fact]
    public void WithSteps_AtStart_ShouldReturn0Percent()
    {
        // Act
        var progress = UseCaseProgress.WithSteps("Starting", 0, 100);

        // Assert
        progress.PercentComplete.ShouldBe(0);
    }

    [Fact]
    public void WithSteps_AtEnd_ShouldReturn100Percent()
    {
        // Act
        var progress = UseCaseProgress.WithSteps("Complete", 100, 100);

        // Assert
        progress.PercentComplete.ShouldBe(100);
    }

    [Fact]
    public void WithSteps_WithZeroTotal_ShouldReturn0Percent()
    {
        // Act
        var progress = UseCaseProgress.WithSteps("Processing", 5, 0);

        // Assert
        progress.PercentComplete.ShouldBe(0);
    }

    [Fact]
    public void WithPercentage_ShouldCreateProgressWithExplicitPercentage()
    {
        // Act
        var progress = UseCaseProgress.WithPercentage("Analyzing", 75);

        // Assert
        progress.Message.ShouldBe("Analyzing");
        progress.CurrentStep.ShouldBeNull();
        progress.TotalSteps.ShouldBeNull();
        progress.PercentComplete.ShouldBe(75);
    }

    [Fact]
    public void ToString_WithPercentage_ShouldIncludePercentage()
    {
        // Arrange
        var progress = UseCaseProgress.WithPercentage("Fetching data", 42);

        // Act
        string result = progress.ToString();

        // Assert
        result.ShouldBe("Fetching data (42%)");
    }

    [Fact]
    public void ToString_WithSteps_ShouldIncludeStepInfo()
    {
        // Arrange
        var progress = new UseCaseProgress("Processing batch", 5, 10);

        // Act
        string result = progress.ToString();

        // Assert
        result.ShouldBe("Processing batch (5/10)");
    }

    [Fact]
    public void ToString_WithMessageOnly_ShouldReturnMessage()
    {
        // Arrange
        var progress = UseCaseProgress.Simple("Loading");

        // Act
        string result = progress.ToString();

        // Assert
        result.ShouldBe("Loading");
    }

    [Fact]
    public void ToString_WithStepsAndPercentage_ShouldPreferPercentage()
    {
        // Arrange - This would typically come from WithSteps() which includes both
        var progress = UseCaseProgress.WithSteps("Processing", 5, 10);

        // Act
        string result = progress.ToString();

        // Assert
        result.ShouldBe("Processing (50%)");
    }

    [Fact]
    public void RecordEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var progress1 = UseCaseProgress.WithSteps("Processing", 5, 10);
        var progress2 = UseCaseProgress.WithSteps("Processing", 5, 10);

        // Act & Assert
        progress1.ShouldBe(progress2);
        (progress1 == progress2).ShouldBeTrue();
    }

    [Fact]
    public void RecordEquality_WithDifferentMessages_ShouldNotBeEqual()
    {
        // Arrange
        var progress1 = UseCaseProgress.Simple("Message 1");
        var progress2 = UseCaseProgress.Simple("Message 2");

        // Act & Assert
        progress1.ShouldNotBe(progress2);
        (progress1 != progress2).ShouldBeTrue();
    }

    [Fact]
    public void RecordEquality_WithDifferentSteps_ShouldNotBeEqual()
    {
        // Arrange
        var progress1 = UseCaseProgress.WithSteps("Processing", 5, 10);
        var progress2 = UseCaseProgress.WithSteps("Processing", 6, 10);

        // Act & Assert
        progress1.ShouldNotBe(progress2);
    }
}
