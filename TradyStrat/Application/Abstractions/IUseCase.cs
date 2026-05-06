namespace TradyStrat.Application.Abstractions;

public interface IUseCase<in TInput, TOutput>
{
    Task<TOutput> ExecuteAsync(TInput input, CancellationToken ct);
}
