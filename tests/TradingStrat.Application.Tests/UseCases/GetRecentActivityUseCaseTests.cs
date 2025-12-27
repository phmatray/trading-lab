using FakeItEasy;
using Shouldly;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Tests.UseCases;

public class GetRecentActivityUseCaseTests
{
    private readonly IActivityEventPort _activityEventPort;
    private readonly GetRecentActivityUseCase _useCase;

    public GetRecentActivityUseCaseTests()
    {
        _activityEventPort = A.Fake<IActivityEventPort>();
        _useCase = new GetRecentActivityUseCase(_activityEventPort);
    }

    [Fact]
    public async Task ExecuteAsync_WithDefaultLimit_Returns10Events()
    {
        // Arrange
        var expectedEvents = new List<ActivityEvent>
        {
            new() { Id = 1, EventType = "BacktestCompleted", Description = "Backtest completed", Timestamp = DateTime.UtcNow },
            new() { Id = 2, EventType = "PortfolioCreated", Description = "Portfolio created", Timestamp = DateTime.UtcNow },
            new() { Id = 3, EventType = "StrategyCreated", Description = "Strategy created", Timestamp = DateTime.UtcNow }
        };

        A.CallTo(() => _activityEventPort.GetRecentActivityAsync(10)).Returns(expectedEvents);

        // Act
        var resultWrapper = await _useCase.ExecuteAsync();

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        resultWrapper.Value.ShouldBe(expectedEvents);
        A.CallTo(() => _activityEventPort.GetRecentActivityAsync(10)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomLimit_ReturnsSpecifiedNumberOfEvents()
    {
        // Arrange
        int customLimit = 25;
        var expectedEvents = Enumerable.Range(1, 25)
            .Select(i => new ActivityEvent
            {
                Id = i,
                EventType = "Event" + i,
                Description = $"Event {i}",
                Timestamp = DateTime.UtcNow.AddMinutes(-i)
            })
            .ToList();

        A.CallTo(() => _activityEventPort.GetRecentActivityAsync(customLimit)).Returns(expectedEvents);

        // Act
        var resultWrapper = await _useCase.ExecuteAsync(customLimit);

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        var result = resultWrapper.Value;
        result.Count.ShouldBe(25);
        result.ShouldBe(expectedEvents);
        A.CallTo(() => _activityEventPort.GetRecentActivityAsync(customLimit)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoActivity_ReturnsEmptyList()
    {
        // Arrange
        A.CallTo(() => _activityEventPort.GetRecentActivityAsync(10)).Returns(new List<ActivityEvent>());

        // Act
        var resultWrapper = await _useCase.ExecuteAsync();

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        resultWrapper.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithLimitOf1_ReturnsSingleEvent()
    {
        // Arrange
        var singleEvent = new List<ActivityEvent>
        {
            new() { Id = 1, EventType = "BacktestCompleted", Description = "Latest backtest", Timestamp = DateTime.UtcNow }
        };

        A.CallTo(() => _activityEventPort.GetRecentActivityAsync(1)).Returns(singleEvent);

        // Act
        var resultWrapper = await _useCase.ExecuteAsync(1);

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        var result = resultWrapper.Value;
        result.Count.ShouldBe(1);
        result[0].EventType.ShouldBe("BacktestCompleted");
    }

    [Fact]
    public async Task ExecuteAsync_DelegatesToPortCorrectly()
    {
        // Arrange
        A.CallTo(() => _activityEventPort.GetRecentActivityAsync(A<int>._)).Returns(new List<ActivityEvent>());

        // Act
        await _useCase.ExecuteAsync(15);

        // Assert
        A.CallTo(() => _activityEventPort.GetRecentActivityAsync(15)).MustHaveHappenedOnceExactly();
    }
}
