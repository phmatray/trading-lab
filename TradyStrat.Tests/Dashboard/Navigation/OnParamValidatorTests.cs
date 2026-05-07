using Shouldly;
using TradyStrat.Features.Dashboard.Navigation;
using Xunit;

namespace TradyStrat.Tests.Dashboard.Navigation;

public class OnParamValidatorTests
{
    private static readonly DateOnly Earliest = new(2026, 4, 13); // Mon
    private static readonly DateOnly Latest   = new(2026, 4, 20); // Mon
    private static readonly DateOnly Fri17    = new(2026, 4, 17);
    private static readonly DateOnly Sun19    = new(2026, 4, 19); // closed

    private sealed class FakeNav : IEntryNavigationService
    {
        public Task<DateOnly>  EarliestAsync(CancellationToken ct) => Task.FromResult(Earliest);
        public Task<DateOnly>  LatestAsync(CancellationToken ct)   => Task.FromResult(Latest);
        public Task<DateOnly?> PreviousAsync(DateOnly current, CancellationToken ct) => throw new NotImplementedException();
        public Task<DateOnly?> NextAsync(DateOnly current, CancellationToken ct)     => throw new NotImplementedException();

        public Task<DateOnly> ResolveOrFallbackAsync(DateOnly requested, CancellationToken ct)
        {
            // Only Apr 13–17 and Apr 20 are trading days; Sun 19 falls back to Fri 17.
            DateOnly[] tradingDays = [Earliest, new(2026,4,14), new(2026,4,15), new(2026,4,16), Fri17, Latest];
            for (int i = tradingDays.Length - 1; i >= 0; i--)
                if (tradingDays[i] <= requested) return Task.FromResult(tradingDays[i]);
            throw new InvalidOperationException("test setup: nothing earlier");
        }
    }

    private static Task<ValidationResult> ValidateAsync(string? onParam) =>
        OnParamValidator.Validate(onParam, new FakeNav(), TestContext.Current.CancellationToken);

    [Fact] public async Task Null_input_is_Live()  => (await ValidateAsync(null)).ShouldBeOfType<ValidationResult.Live>();
    [Fact] public async Task Empty_input_is_Live() => (await ValidateAsync("")).ShouldBeOfType<ValidationResult.Live>();

    [Fact]
    public async Task Unparsable_input_redirects_to_root()
    {
        var r = await ValidateAsync("foo");
        var redirect = r.ShouldBeOfType<ValidationResult.RedirectTo>();
        redirect.Url.ShouldBe("/");
    }

    [Fact]
    public async Task After_latest_redirects_to_root()
    {
        var r = await ValidateAsync("2026-04-25");
        var redirect = r.ShouldBeOfType<ValidationResult.RedirectTo>();
        redirect.Url.ShouldBe("/");
    }

    [Fact]
    public async Task Before_earliest_redirects_to_earliest()
    {
        var r = await ValidateAsync("2026-04-10");
        var redirect = r.ShouldBeOfType<ValidationResult.RedirectTo>();
        redirect.Url.ShouldBe("/?on=2026-04-13");
    }

    [Fact]
    public async Task Closed_day_redirects_to_nearest_earlier()
    {
        var r = await ValidateAsync("2026-04-19"); // Sunday
        var redirect = r.ShouldBeOfType<ValidationResult.RedirectTo>();
        redirect.Url.ShouldBe("/?on=2026-04-17");
    }

    [Fact]
    public async Task Valid_trading_day_returns_Historical()
    {
        var r = await ValidateAsync("2026-04-15");
        var hist = r.ShouldBeOfType<ValidationResult.Historical>();
        hist.Date.ShouldBe(new DateOnly(2026, 4, 15));
    }
}
