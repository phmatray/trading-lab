// <copyright file="CorrelationIdMiddleware.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Serilog.Context;

namespace TradingBot.Web.Middleware;

/// <summary>
/// Middleware that adds correlation IDs to each request for distributed tracing.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Processes the HTTP request and adds a correlation ID.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Try to get correlation ID from request header, or generate a new one
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        // Add correlation ID to response headers
        context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);

        // Push correlation ID to Serilog LogContext for structured logging
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
