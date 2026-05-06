using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Application.Abstractions;

public abstract class UseCaseBase<TInput, TOutput> : IUseCase<TInput, TOutput>
{
    private readonly ILogger _logger;

    protected UseCaseBase(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<TOutput> ExecuteAsync(TInput input, CancellationToken ct)
    {
        var name = GetType().Name;
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await ExecuteCore(input, ct);
            UseCaseLog.Ok(_logger, name, sw.ElapsedMilliseconds);
            return result;
        }
        catch (TradyStratException)
        {
            throw;
        }
        catch (Exception ex)
        {
            UseCaseLog.Failed(_logger, ex, name);
            throw;
        }
    }

    protected abstract Task<TOutput> ExecuteCore(TInput input, CancellationToken ct);
}

internal static partial class UseCaseLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "{UseCase} ok in {Ms}ms")]
    public static partial void Ok(ILogger logger, string useCase, long ms);

    [LoggerMessage(Level = LogLevel.Error, Message = "{UseCase} failed")]
    public static partial void Failed(ILogger logger, Exception ex, string useCase);
}
