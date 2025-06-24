using Microsoft.Extensions.AI;
using OpenAI;

namespace EvalSharp.Console;

public static class ChatClient
{
    public static IChatClient GetInstance()
    {
        var openAiClient = new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
        return openAiClient.GetChatClient("gpt-4.1-mini").AsIChatClient();
    }
}