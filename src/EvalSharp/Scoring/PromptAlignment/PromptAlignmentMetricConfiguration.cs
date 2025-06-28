namespace EvalSharp.Scoring;

/// <summary>
/// Configuration for the Prompt Alignment Metric.
/// </summary>
public record PromptAlignmentMetricConfiguration : LLMAsAJudgeMetricConfiguration
{
    /// <summary>
    /// List of strings that represent instructions to validate against the model's response.
    /// </summary>
    public List<string> PromptInstructions { get; set; } = new();
}