using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Shouldly;
using TradyStrat.Cli.Mcp.Filters;
using TradyStrat.Domain.Exceptions;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Filters;

public sealed class McpExceptionTranslationFilterTests
{
    // Null context is safe because our test 'next' delegates never dereference it.
    private static readonly RequestContext<CallToolRequestParams> NullContext = null!;

    private static McpRequestHandler<CallToolRequestParams, CallToolResult> NextThrowing(Exception ex)
        => (_, _) => throw ex;

    private static McpRequestHandler<CallToolRequestParams, CallToolResult> NextReturning(CallToolResult result)
        => (_, _) => ValueTask.FromResult(result);

    [Fact]
    public async Task TradyStratException_becomes_McpException_with_same_message()
    {
        var inner = new CsvImportException("bad csv", lineNumber: 5);
        var ex = await Should.ThrowAsync<McpException>(
            () => McpExceptionTranslationFilter.ExecuteAsync(NextThrowing(inner), NullContext, CancellationToken.None).AsTask());
        ex.Message.ShouldBe(inner.Message);
    }

    [Fact]
    public async Task ArgumentException_becomes_McpException()
    {
        var inner = new ArgumentException("bad arg");
        var ex = await Should.ThrowAsync<McpException>(
            () => McpExceptionTranslationFilter.ExecuteAsync(NextThrowing(inner), NullContext, CancellationToken.None).AsTask());
        ex.Message.ShouldBe(inner.Message);
    }

    [Fact]
    public async Task OperationCanceledException_propagates_unchanged()
    {
        var inner = new OperationCanceledException("cancelled");
        await Should.ThrowAsync<OperationCanceledException>(
            () => McpExceptionTranslationFilter.ExecuteAsync(NextThrowing(inner), NullContext, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Arbitrary_exception_propagates_unchanged()
    {
        var inner = new InvalidOperationException("boom");
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => McpExceptionTranslationFilter.ExecuteAsync(NextThrowing(inner), NullContext, CancellationToken.None).AsTask());
        thrown.ShouldBeSameAs(inner);
    }
}
