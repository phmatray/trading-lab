using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Shouldly;
using TradyStrat.Cli.Mcp.Filters;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Filters;

public sealed class McpTimeoutFilterTests : IDisposable
{
    // Null context is safe because our test 'next' delegates never dereference it.
    private static readonly RequestContext<CallToolRequestParams> NullContext = null!;

    public void Dispose()
    {
        // Reset Budget after each test so state doesn't leak.
        McpTimeoutFilter.Budget = TimeSpan.FromSeconds(30);
    }

    [Fact]
    public async Task Fast_next_passes_through_within_budget()
    {
        McpTimeoutFilter.Budget = TimeSpan.FromSeconds(1);
        var expected = new CallToolResult();
        McpRequestHandler<CallToolRequestParams, CallToolResult> next =
            (_, _) => ValueTask.FromResult(expected);

        var result = await McpTimeoutFilter.ExecuteAsync(next, NullContext, CancellationToken.None);

        result.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task Slow_next_throws_McpException_with_timeout_message()
    {
        McpTimeoutFilter.Budget = TimeSpan.FromMilliseconds(50);
        McpRequestHandler<CallToolRequestParams, CallToolResult> next =
            async (_, ct) =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
                return new CallToolResult();
            };

        var ex = await Should.ThrowAsync<McpException>(
            () => McpTimeoutFilter.ExecuteAsync(next, NullContext, CancellationToken.None).AsTask());

        ex.Message.ShouldContain("timeout");
    }

    [Fact]
    public async Task Caller_cancellation_propagates_OperationCanceled()
    {
        McpTimeoutFilter.Budget = TimeSpan.FromSeconds(30); // budget never fires
        using var callerCts = new CancellationTokenSource();
        McpRequestHandler<CallToolRequestParams, CallToolResult> next =
            async (_, ct) =>
            {
                // Immediately cancel the caller token, then wait for cancellation to propagate.
                callerCts.Cancel();
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                return new CallToolResult();
            };

        await Should.ThrowAsync<OperationCanceledException>(
            () => McpTimeoutFilter.ExecuteAsync(next, NullContext, callerCts.Token).AsTask());
    }
}
