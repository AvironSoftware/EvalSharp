using System.Text.Json.Serialization;
using EvalSharp.Models.Enums;

namespace EvalSharp.Models;

internal class VerdictModel
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required VerdictEnum Verdict { get; set; }
    public string? Reason { get; set; }
}