using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

namespace TradingSignal.Llm;

public static class LmStudioChatClientFactory
{
    public static IChatClient Create(LmStudioOptions options)
    {
        var openAi = new OpenAIClient(
            new ApiKeyCredential("lm-studio"),
            new OpenAIClientOptions { Endpoint = new Uri(options.Endpoint) });

        return openAi.GetChatClient(options.ModelId).AsIChatClient();
    }
}
