using System.Text.Json.Serialization;

namespace EvalSharp.Models;

/// <summary>
/// Represents a tool call with its associated metadata, reasoning, output, and input parameters.
/// </summary>
public class ToolCall
{
    /// <summary>
    /// Gets or sets the name of the tool.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the tool.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reasoning behind the tool call.
    /// </summary>
    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the output of the tool call.
    /// </summary>
    [JsonPropertyName("output")]
    public object Output { get; set; } = new();

    /// <summary>
    /// Gets or sets the input parameters for the tool call.
    /// </summary>
    [JsonPropertyName("input_parameters")]
    public Dictionary<string, object?> InputParameters { get; set; } = new();
}