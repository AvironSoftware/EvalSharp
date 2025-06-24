namespace EvalSharp.Scoring;

/// <summary>
/// Configuration for the Prompt Alignment Metric.
/// </summary>
public class PromptAlignmentMetricConfiguration : MetricConfiguration
{
    /// <summary>
    /// List of strings that represent instructions to validate against the model's response.
    /// </summary>
    public List<string> PromptInstructions { get; set; } = new();

    /// <summary>
    /// A boolean that, when set to True, makes an extra LLM call to provide a reason for the metric score. Default is True.
    /// </summary>
    public bool IncludeReason { get; set; } = true;
}