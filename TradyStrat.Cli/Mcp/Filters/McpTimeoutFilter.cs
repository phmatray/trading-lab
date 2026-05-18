using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace TradyStrat.Cli.Mcp.Filters;

internal static class McpTimeoutFilter
{
    /// <summary>Maximum duration for a single tool call. Settable in tests via <c>internal set</c>.</summary>
    internal static TimeSpan Budget { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Returns an <see cref="McpRequestFilter{TParams,TResult}"/> that enforces <see cref="Budget"/>.
    /// </summary>
    public static McpRequestFilter<CallToolRequestParams, CallToolResult> AsFilter()
        => next => (context, ct) => ExecuteAsync(next, context, ct);

    // Internal helper — tested directly so tests don't need to construct RequestContext<>.
    internal static async ValueTask<CallToolResult> ExecuteAsync(
        McpRequestHandler<CallToolRequestParams, CallToolResult> next,
        RequestContext<CallToolRequestParams> context,
        CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(Budget);
        try
        {
            return await next(context, cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new McpException($"Tool call exceeded {Budget.TotalSeconds:F0}s timeout.");
        }
        // Caller cancellation (ct was cancelled) falls through and re-throws OperationCanceledException.
    }
}
