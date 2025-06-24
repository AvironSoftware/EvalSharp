using System.Text.Json.Serialization;

namespace EvalSharp.Models;

internal class TopLogprobs
{
    [JsonPropertyName("token")]
    public required string Token { get; set; }

    [JsonPropertyName("logprob")]
    public required double Logprob { get; set; }

    [JsonPropertyName("bytes")]
    public required List<int> Bytes { get; set; }
}