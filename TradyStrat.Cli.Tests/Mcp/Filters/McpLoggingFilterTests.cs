using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Shouldly;
using TradyStrat.Cli.Mcp.Filters;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Filters;

/// <summary>Minimal in-memory logger that records log entries for assertions.</summary>
internal sealed class RecordingLogger : ILogger
{
    public sealed record Entry(LogLevel Level, string Message, Exception? Exception);

    private readonly List<Entry> _entries = [];
    public IReadOnlyList<Entry> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _entries.Add(new Entry(logLevel, formatter(state, exception), exception));
    }
}

public sealed class McpLoggingFilterTests
{
    // Null context is safe because ExecuteAsync receives the logger and tool name explicitly.
    private static readonly RequestContext<CallToolRequestParams> NullContext = null!;

    private static McpRequestHandler<CallToolRequestParams, CallToolResult> NextReturning(CallToolResult result)
        => (_, _) => ValueTask.FromResult(result);

    private static McpRequestHandler<CallToolRequestParams, CallToolResult> NextThrowing(Exception ex)
        => (_, _) => throw ex;

    [Fact]
    public async Task Logs_information_with_outcome_ok_on_success()
    {
        var logger = new RecordingLogger();
        var expected = new CallToolResult();

        var result = await McpLoggingFilter.ExecuteAsync(
            NextReturning(expected), NullContext, logger, "my_tool", CancellationToken.None);

        result.ShouldBeSameAs(expected);
        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].Level.ShouldBe(LogLevel.Information);
        logger.Entries[0].Message.ShouldContain("my_tool");
        logger.Entries[0].Message.ShouldContain("ok");
    }

    [Fact]
    public async Task Logs_information_with_outcome_cancelled_on_cancel()
    {
        var logger = new RecordingLogger();

        await Should.ThrowAsync<OperationCanceledException>(
            () => McpLoggingFilter.ExecuteAsync(
                NextThrowing(new OperationCanceledException("caller cancelled")),
                NullContext, logger, "my_tool", CancellationToken.None).AsTask());

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].Level.ShouldBe(LogLevel.Information);
        logger.Entries[0].Message.ShouldContain("cancelled");
    }

    [Fact]
    public async Task Logs_warning_with_mcp_error_on_McpException()
    {
        var logger = new RecordingLogger();
        var mcpEx = new McpException("tool error");

        await Should.ThrowAsync<McpException>(
            () => McpLoggingFilter.ExecuteAsync(
                NextThrowing(mcpEx), NullContext, logger, "my_tool", CancellationToken.None).AsTask());

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].Level.ShouldBe(LogLevel.Warning);
        logger.Entries[0].Message.ShouldContain("mcp_error");
        logger.Entries[0].Exception.ShouldBeSameAs(mcpEx);
    }

    [Fact]
    public async Task Logs_error_with_unexpected_on_other_exception()
    {
        var logger = new RecordingLogger();
        var unexpected = new InvalidOperationException("boom");

        await Should.ThrowAsync<InvalidOperationException>(
            () => McpLoggingFilter.ExecuteAsync(
                NextThrowing(unexpected), NullContext, logger, "my_tool", CancellationToken.None).AsTask());

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].Level.ShouldBe(LogLevel.Error);
        logger.Entries[0].Message.ShouldContain("unexpected");
        logger.Entries[0].Exception.ShouldBeSameAs(unexpected);
    }
}
