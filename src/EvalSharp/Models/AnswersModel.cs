using System.Text.Json.Serialization;

namespace EvalSharp.Models;

internal class AnswersModel
{
    [JsonPropertyName("answers")]
    public required string[] Answers { get; set; }
}