using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TradingSignal.Core;
using TradingSignal.Llm.Abstractions;
using TradingSignal.Llm.Parsing;
using TradingSignal.Llm.Prompts;
using TradingSignal.Llm.Schemas;

namespace TradingSignal.Llm.Strategies;

internal sealed partial class InstructCallStrategy : ILlmCallStrategy
{
    private readonly IChatClient _chatClient;
    private readonly LmStudioOptions _options;
    private readonly ILogger<InstructCallStrategy> _logger;

    public InstructCallStrategy(
        IChatClient chatClient,
        LmStudioOptions options,
        ILogger<InstructCallStrategy>? logger = null)
    {
        _chatClient = chatClient;
        _options = options;
        _logger = logger ?? NullLogger<InstructCallStrategy>.Instance;
    }

    public string SystemPrompt => PromptBuilder.SystemPromptInstruct;

    public async Task<LlmCallOutcome> GenerateAsync(
        string systemPrompt, string userMessage, CancellationToken ct)
    {
        RawSignal? signal = await TryOnceAsync(systemPrompt, userMessage, useSchema: true, stricterReminder: false, ct).ConfigureAwait(false);
        if (signal is not null) return new LlmCallOutcome(signal, null);

        signal = await TryOnceAsync(systemPrompt, userMessage, useSchema: false, stricterReminder: true, ct).ConfigureAwait(false);
        return new LlmCallOutcome(signal ?? new RawSignal(TradeAction.Hold, 0d, "parse_failure"), null);
    }

    private async Task<RawSignal?> TryOnceAsync(
        string systemPrompt, string userMessage, bool useSchema, bool stricterReminder, CancellationToken ct)
    {
        List<ChatMessage> messages = new()
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userMessage),
        };
        if (stricterReminder)
        {
            messages.Add(new ChatMessage(ChatRole.User,
                "Return ONLY valid JSON matching the schema. No prose, no markdown."));
        }

        ChatOptions chatOptions = new()
        {
            Temperature = 0.2f,
            MaxOutputTokens = _options.MaxOutputTokens,
            ResponseFormat = useSchema
                ? ChatResponseFormat.ForJsonSchema(
                    SignalResponseSchema.Element,
                    SignalResponseSchema.SchemaName,
                    SignalResponseSchema.SchemaDescription)
                : null,
        };

        try
        {
            ChatResponse response = await _chatClient.GetResponseAsync(messages, chatOptions, ct).ConfigureAwait(false);
            string text = response.Text ?? string.Empty;
            if (SignalResponseParser.TryParse(text, out RawSignal parsed)) return parsed;

            if (_logger.IsEnabled(LogLevel.Warning))
                LogParseFailure(_logger, useSchema, Truncate(text, 400));
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogCallFailed(_logger, useSchema, ex);
            return null;
        }
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...";

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "LLM parse failure (schema={UseSchema}). Body: {Body}")]
    private static partial void LogParseFailure(ILogger logger, bool useSchema, string body);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "LLM call failed (schema={UseSchema})")]
    private static partial void LogCallFailed(ILogger logger, bool useSchema, Exception ex);
}
