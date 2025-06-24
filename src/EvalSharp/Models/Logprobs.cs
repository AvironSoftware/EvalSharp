using OpenAI.Chat;
using System.Text.Json.Serialization;

namespace EvalSharp.Models;

internal class Logprobs
{
    [JsonPropertyName("content")]
    public required List<ChatTokenLogProbabilityDetails> Content { get; set; }
}