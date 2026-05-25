using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Cli.Mcp.Filters;

internal static class McpExceptionTranslationFilter
{
    /// <summary>
    /// Returns an <see cref="McpRequestFilter{TParams,TResult}"/> that translates domain exceptions
    /// into <see cref="McpException"/> so the SDK can surface them as JSON-RPC errors.
    /// </summary>
    public static McpRequestFilter<CallToolRequestParams, CallToolResult> AsFilter()
        => next => (context, ct) => ExecuteAsync(next, context, ct);

    // Internal helper — tested directly so tests don't need to construct RequestContext<>.
    internal static async ValueTask<CallToolResult> ExecuteAsync(
        McpRequestHandler<CallToolRequestParams, CallToolResult> next,
        RequestContext<CallToolRequestParams> context,
        CancellationToken ct)
    {
        try
        {
            return await next(context, ct);
        }
        catch (McpException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TradyStratException ex)
        {
            throw new McpException(ex.Message);
        }
        catch (ArgumentException ex)
        {
            throw new McpException(ex.Message);
        }
        // All other exceptions propagate unchanged.
    }
}
