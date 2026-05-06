using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.Abstractions;
using TradyStrat.Shared.Exceptions;
using Xunit;

namespace TradyStrat.Tests.Application;

public class UseCaseBaseTests
{
    private sealed class OkUseCase : UseCaseBase<int, int>
    {
        public OkUseCase() : base(NullLogger<OkUseCase>.Instance) { }
        protected override Task<int> ExecuteCore(int input, CancellationToken ct)
            => Task.FromResult(input * 2);
    }

    private sealed class FailingUseCase : UseCaseBase<int, int>
    {
        public FailingUseCase() : base(NullLogger<FailingUseCase>.Instance) { }
        protected override Task<int> ExecuteCore(int input, CancellationToken ct)
            => throw new TradeValidationException("nope");
    }

    [Fact]
    public async Task Returns_result_from_ExecuteCore()
    {
        (await new OkUseCase().ExecuteAsync(21, TestContext.Current.CancellationToken)).ShouldBe(42);
    }

    [Fact]
    public async Task Domain_exception_propagates_unwrapped()
    {
        await Should.ThrowAsync<TradeValidationException>(() =>
            new FailingUseCase().ExecuteAsync(0, TestContext.Current.CancellationToken));
    }
}
