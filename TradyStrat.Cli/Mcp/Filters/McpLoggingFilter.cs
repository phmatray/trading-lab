using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace TradyStrat.Cli.Mcp.Filters;

/// <summary>Used as the ILogger&lt;T&gt; category type so the log category reads clearly in output.</summary>
internal sealed class McpLoggingFilterMarker;

internal static partial class McpLoggingFilter
{
    [LoggerMessage(Level = LogLevel.Information, Message = "MCP tool {Tool} ok in {Ms}ms")]
    private static partial void LogOk(ILogger logger, string tool, long ms);

    [LoggerMessage(Level = LogLevel.Information, Message = "MCP tool {Tool} cancelled after {Ms}ms")]
    private static partial void LogCancelled(ILogger logger, string tool, long ms);

    [LoggerMessage(Level = LogLevel.Warning, Message = "MCP tool {Tool} mcp_error after {Ms}ms")]
    private static partial void LogMcpError(ILogger logger, Exception ex, string tool, long ms);

    [LoggerMessage(Level = LogLevel.Error, Message = "MCP tool {Tool} unexpected after {Ms}ms")]
    private static partial void LogUnexpected(ILogger logger, Exception ex, string tool, long ms);

    /// <summary>
    /// Returns an <see cref="McpRequestFilter{TParams,TResult}"/> that emits a structured log record
    /// for every tool call. Exceptions are always rethrown — this filter only observes.
    /// </summary>
    public static McpRequestFilter<CallToolRequestParams, CallToolResult> AsFilter()
        => next => (context, ct) =>
        {
            var logger = (context.Services ?? throw new InvalidOperationException("context.Services is null."))
                .GetRequiredService<ILogger<McpLoggingFilterMarker>>();
            var toolName = context.Params?.Name ?? "(unknown)";
            return ExecuteAsync(next, context, logger, toolName, ct);
        };

    // Internal helper — tested directly using a RecordingLogger so tests don't need to construct
    // a RequestContext<> (which requires a real McpServer instance).
    // CancellationToken is last per CA1068.
    internal static async ValueTask<CallToolResult> ExecuteAsync(
        McpRequestHandler<CallToolRequestParams, CallToolResult> next,
        RequestContext<CallToolRequestParams> context,
        ILogger logger,
        string toolName,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await next(context, ct);
            sw.Stop();
            LogOk(logger, toolName, sw.ElapsedMilliseconds);
            return result;
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            LogCancelled(logger, toolName, sw.ElapsedMilliseconds);
            throw;
        }
        catch (McpException ex)
        {
            sw.Stop();
            LogMcpError(logger, ex, toolName, sw.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogUnexpected(logger, ex, toolName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
