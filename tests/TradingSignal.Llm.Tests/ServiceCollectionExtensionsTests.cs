using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradingSignal.Llm;
using TradingSignal.Llm.Abstractions;
using TradingSignal.Llm.Caching;
using TradingSignal.Llm.Strategies;
using TradingSignal.Llm.Tests.Fakes;

namespace TradingSignal.Llm.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void ModelFamily_Instruct_Registers_InstructCallStrategy()
    {
        ServiceCollection services = new();
        LmStudioOptions options = new() { ModelFamily = "instruct", ModelId = "m" };
        services.AddSingleton(options);
        services.AddSingleton<IChatClient>(new FakeChatClient());
        services.AddSingleton<ILlmResponseCache>(NullLlmResponseCache.Instance);

        services.AddLlmCallStrategy(options);

        using ServiceProvider sp = services.BuildServiceProvider();
        ILlmCallStrategy strategy = sp.GetRequiredService<ILlmCallStrategy>();
        strategy.ShouldBeOfType<InstructCallStrategy>();
    }

    [Fact]
    public void ModelFamily_Reasoning_Registers_ReasoningCallStrategy_With_Configured_HttpClient()
    {
        ServiceCollection services = new();
        LmStudioOptions options = new()
        {
            ModelFamily = "reasoning",
            Endpoint = "http://localhost:9999/v1",
            TimeoutSeconds = 30,
        };
        services.AddSingleton(options);
        services.AddSingleton<ILlmResponseCache>(NullLlmResponseCache.Instance);

        services.AddLlmCallStrategy(options);

        using ServiceProvider sp = services.BuildServiceProvider();
        ILlmCallStrategy strategy = sp.GetRequiredService<ILlmCallStrategy>();
        strategy.ShouldBeOfType<ReasoningCallStrategy>();

        HttpClient http = sp.GetRequiredService<HttpClient>();
        http.BaseAddress!.ToString().ShouldBe("http://localhost:9999/v1/");
        http.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Unknown_ModelFamily_Falls_Back_To_Instruct_With_Warning()
    {
        ServiceCollection services = new();
        LmStudioOptions options = new() { ModelFamily = "thinking-deluxe-2000", ModelId = "m" };
        services.AddSingleton(options);
        services.AddSingleton<IChatClient>(new FakeChatClient());
        services.AddSingleton<ILlmResponseCache>(NullLlmResponseCache.Instance);

        CapturingLogger logger = new();
        services.AddLlmCallStrategy(options, logger);

        using ServiceProvider sp = services.BuildServiceProvider();
        sp.GetRequiredService<ILlmCallStrategy>().ShouldBeOfType<InstructCallStrategy>();
        logger.Warnings.ShouldNotBeEmpty();
        logger.Warnings[0].ShouldContain("thinking-deluxe-2000");
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<string> Warnings { get; } = new();
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning) Warnings.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
