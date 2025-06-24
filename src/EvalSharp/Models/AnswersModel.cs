using System.Text.Json.Serialization;
using EvalSharp.Models.Enums;

namespace EvalSharp.Models;

internal class AnswersModel
{
    [JsonPropertyName("answers")]
    public required string[] Answers { get; set; }
}