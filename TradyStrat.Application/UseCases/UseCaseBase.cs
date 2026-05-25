using System.Diagnostics;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Application.UseCases;

public abstract class UseCaseBase<TInput, TOutput>(ILogger logger)
    : IUseCase<TInput, TOutput>
{
    public async Task<TOutput> ExecuteAsync(TInput input, CancellationToken ct)
    {
        var name = GetType().Name;
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await ExecuteCore(input, ct);
            UseCaseLog.Ok(logger, name, sw.ElapsedMilliseconds);
            return result;
        }
        catch (TradyStratException)
        {
            throw;
        }
        catch (Exception ex)
        {
            UseCaseLog.Failed(logger, ex, name);
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
