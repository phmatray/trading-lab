using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TradingSignal.Core;
using TradingSignal.Llm.Abstractions;
using TradingSignal.Llm.Parsing;
using TradingSignal.Llm.Prompts;

namespace TradingSignal.Llm.Strategies;

internal sealed partial class ReasoningCallStrategy : ILlmCallStrategy
{
    private const string StricterReminder =
        "Return ONLY a single JSON object on the final line with action, confidence, reason. No prose after the JSON.";

    private readonly HttpClient _http;
    private readonly LmStudioOptions _options;
    private readonly ILogger<ReasoningCallStrategy> _logger;

    public ReasoningCallStrategy(
        HttpClient http,
        LmStudioOptions options,
        ILogger<ReasoningCallStrategy>? logger = null)
    {
        _http = http;
        _options = options;
        _logger = logger ?? NullLogger<ReasoningCallStrategy>.Instance;
    }

    public string SystemPrompt => PromptBuilder.SystemPromptReasoning;

    public async Task<LlmCallOutcome> GenerateAsync(
        string systemPrompt, string userMessage, CancellationToken ct)
    {
        (RawSignal? signal, string? trace) first = await CallOnceAsync(systemPrompt, userMessage, stricter: false, ct).ConfigureAwait(false);
        if (first.signal is not null)
            return new LlmCallOutcome(first.signal, first.trace);

        (RawSignal? signal, string? trace) second = await CallOnceAsync(systemPrompt, userMessage, stricter: true, ct).ConfigureAwait(false);
        string? lastTrace = second.trace ?? first.trace;
        return new LlmCallOutcome(
            second.signal ?? new RawSignal(TradeAction.Hold, 0d, "parse_failure"),
            lastTrace);
    }

    private async Task<(RawSignal? Signal, string? Trace)> CallOnceAsync(
        string systemPrompt, string userMessage, bool stricter, CancellationToken ct)
    {
        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt },
            new { role = "user",   content = userMessage },
        };
        if (stricter)
        {
            messages.Add(new { role = "user", content = StricterReminder });
        }

        var body = new
        {
            model = _options.ModelId,
            messages = messages.ToArray(),
            max_tokens = _options.MaxOutputTokens,
            temperature = 0.2,
            reasoning_effort = _options.ReasoningEffort,
        };

        try
        {
            using HttpResponseMessage response = await _http.PostAsJsonAsync("chat/completions", body, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                LogHttpError(_logger, (int)response.StatusCode);
                return (null, null);
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using JsonDocument doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

            JsonElement message = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message");

            string content = message.TryGetProperty("content", out JsonElement c) && c.ValueKind == JsonValueKind.String
                ? c.GetString() ?? string.Empty
                : string.Empty;
            string? trace = message.TryGetProperty("reasoning_content", out JsonElement r) && r.ValueKind == JsonValueKind.String
                ? r.GetString()
                : null;

            if (SignalResponseParser.TryParse(content, out RawSignal parsed))
                return (parsed, trace);

            if (_logger.IsEnabled(LogLevel.Warning))
                LogParseFailure(_logger, stricter, Truncate(content, 400));
            return (null, trace);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            LogTimeout(_logger, ex);
            return (null, null);
        }
        catch (HttpRequestException ex)
        {
            LogTransport(_logger, ex);
            return (null, null);
        }
        catch (JsonException ex)
        {
            LogMalformed(_logger, ex);
            return (null, null);
        }
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...";

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Reasoning LLM HTTP error: status {Status}")]
    private static partial void LogHttpError(ILogger logger, int status);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Reasoning LLM parse failure (stricter={Stricter}). Body: {Body}")]
    private static partial void LogParseFailure(ILogger logger, bool stricter, string body);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Reasoning LLM call timed out (HttpClient internal timeout)")]
    private static partial void LogTimeout(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Reasoning LLM transport error")]
    private static partial void LogTransport(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Reasoning LLM returned malformed JSON")]
    private static partial void LogMalformed(ILogger logger, Exception ex);
}
