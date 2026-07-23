using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TradingSignal.Llm.Abstractions;
using TradingSignal.Llm.Strategies;

namespace TradingSignal.Llm;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddLlmCallStrategy(
        this IServiceCollection services,
        LmStudioOptions options,
        ILogger? startupLogger = null)
    {
        string family = (options.ModelFamily ?? "instruct").Trim().ToLowerInvariant();
        if (family != "instruct" && family != "reasoning")
        {
            LogUnknownFamily(startupLogger ?? NullLogger.Instance, options.ModelFamily ?? string.Empty);
            family = "instruct";
        }

        if (family == "reasoning")
        {
            services.AddSingleton<HttpClient>(_ => new HttpClient
            {
                BaseAddress = new Uri(options.Endpoint.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
            });
            services.AddSingleton<ILlmCallStrategy, ReasoningCallStrategy>();
        }
        else
        {
            services.AddSingleton<ILlmCallStrategy, InstructCallStrategy>();
        }

        return services;
    }

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning,
        Message = "Unknown ModelFamily '{Family}', falling back to 'instruct'.")]
    private static partial void LogUnknownFamily(ILogger logger, string family);
}
