using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Infrastructure.Assistant;

/// <summary>
/// Infrastructure adapter for Anthropic's Claude API.
/// Implements IAssistantPort to provide LLM capabilities via direct HTTP API calls.
/// Supports both streaming and non-streaming responses.
/// </summary>
public class AnthropicAdapter : IAssistantPort
{
    private readonly HttpClient _httpClient;
    private readonly AssistantConfiguration _config;
    private const string ApiBaseUrl = "https://api.anthropic.com/v1";

    public AnthropicAdapter(IHttpClientFactory httpClientFactory, IOptions<AssistantConfiguration> config)
    {
        _config = config.Value;

        if (string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            throw new InvalidOperationException(
                "Anthropic API key is not configured. " +
                "Please set Trading:Assistant:ApiKey in appsettings.json or via environment variables.");
        }

        _httpClient = httpClientFactory.CreateClient("Anthropic");
        _httpClient.BaseAddress = new Uri(ApiBaseUrl);
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _config.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async IAsyncEnumerable<string> StreamChatResponseAsync(
        string systemPrompt,
        List<ChatMessage> conversationHistory,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<AnthropicMessage> messages = new List<AnthropicMessage>();

        // Convert conversation history
        foreach (ChatMessage msg in conversationHistory)
        {
            messages.Add(new AnthropicMessage
            {
                Role = msg.Role,
                Content = msg.Content
            });
        }

        // Add current user message
        messages.Add(new AnthropicMessage
        {
            Role = "user",
            Content = userMessage
        });

        AnthropicRequest request = new AnthropicRequest
        {
            Model = _config.Model,
            MaxTokens = _config.MaxTokens,
            Temperature = _config.Temperature,
            System = systemPrompt,
            Messages = messages,
            Stream = true
        };

        string jsonRequest = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, "/messages")
        {
            Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json")
        };

        using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using StreamReader reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            string? line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
            {
                continue;
            }

            string data = line.Substring(6); // Remove "data: " prefix
            if (data == "[DONE]")
            {
                break;
            }

            string? textToken = ExtractTextFromStreamEvent(data);
            if (textToken != null)
            {
                yield return textToken;
            }
        }
    }

    private string? ExtractTextFromStreamEvent(string jsonData)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(jsonData);
            if (doc.RootElement.TryGetProperty("type", out JsonElement typeElement))
            {
                string eventType = typeElement.GetString() ?? string.Empty;
                if (eventType == "content_block_delta")
                {
                    if (doc.RootElement.TryGetProperty("delta", out JsonElement delta) &&
                        delta.TryGetProperty("text", out JsonElement text))
                    {
                        return text.GetString();
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Skip invalid JSON lines
        }

        return null;
    }

    public async Task<string> GetChatResponseAsync(
        string systemPrompt,
        List<ChatMessage> conversationHistory,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        List<AnthropicMessage> messages = new List<AnthropicMessage>();

        // Convert conversation history
        foreach (ChatMessage msg in conversationHistory)
        {
            messages.Add(new AnthropicMessage
            {
                Role = msg.Role,
                Content = msg.Content
            });
        }

        // Add current user message
        messages.Add(new AnthropicMessage
        {
            Role = "user",
            Content = userMessage
        });

        AnthropicRequest request = new AnthropicRequest
        {
            Model = _config.Model,
            MaxTokens = _config.MaxTokens,
            Temperature = _config.Temperature,
            System = systemPrompt,
            Messages = messages,
            Stream = false
        };

        try
        {
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/messages", request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            }, cancellationToken);

            response.EnsureSuccessStatusCode();

            AnthropicResponse? anthropicResponse = await response.Content.ReadFromJsonAsync<AnthropicResponse>(
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }, cancellationToken);

            if (anthropicResponse?.Content != null && anthropicResponse.Content.Count > 0)
            {
                return anthropicResponse.Content[0].Text ?? string.Empty;
            }

            return string.Empty;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"Failed to get response from Anthropic API: {ex.Message}",
                ex);
        }
    }

    // Request/Response models for Anthropic API
    private class AnthropicRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("system")]
        public string? System { get; set; }

        [JsonPropertyName("messages")]
        public List<AnthropicMessage> Messages { get; set; } = new();

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private class AnthropicMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class AnthropicResponse
    {
        [JsonPropertyName("content")]
        public List<ContentBlock> Content { get; set; } = new();
    }

    private class ContentBlock
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
